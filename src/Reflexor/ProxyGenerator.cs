using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Reflexor.Diagnostics;

namespace Reflexor;

public readonly record struct Proxy(
    string Name,
    string? Namespace,
    string TargetType,
    Location TargetLocation,
    Modifiers Modifiers,
    ImmutableArray<string> GenericTypes,
    ImmutableArray<Property> Properties,
    ImmutableArray<Method> Methods);

[Flags]
public enum Modifiers
{
    None = 0,
    Static = 1 << 0,
    Override = 1 << 1,
    ReadOnly = 1 << 2,
    Unsafe = 1 << 3,
    RefStruct = 1 << 4,
    Partial = 1 << 5,
    Ref = 1 << 6,
    RefReadOnly = 1 << 7,
}

public readonly record struct Property(string Name, string Type, Modifiers Modifiers);

public readonly record struct Parameter(string Name, string Type, string Ref);
public readonly record struct Method(string Name, string ReturnType, Modifiers Modifiers, ImmutableArray<Parameter> Parameters);

[Generator]
public sealed class ProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var proxyProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName("Reflexor.GenerateProxyAttribute",
                predicate: (syntax, _) => syntax.IsKind(SyntaxKind.ClassDeclaration) || syntax.IsKind(SyntaxKind.RecordDeclaration),
                transform: (context, _) =>
                {
                    var targetType = Unsafe.As<INamedTypeSymbol>(context.TargetSymbol);
                    var properties = new Dictionary<string, Property>();
                    var methods = new List<Method>();
                    foreach (var member in targetType.GetMembers())
                    {
                        switch (member)
                        {
                            case IPropertySymbol propertySymbol when CanBeProxied(propertySymbol):
                                properties[propertySymbol.Name] = new Property(
                                    Name: propertySymbol.Name,
                                    Type: propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    Modifiers: GetModifiers(propertySymbol));
                                break;

                            case IMethodSymbol methodSymbol when CanBeProxied(methodSymbol):
                                methods.Add(new Method(
                                    Name: methodSymbol.Name,
                                    ReturnType: methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    Modifiers: GetModifiers(methodSymbol),
                                    Parameters: [.. methodSymbol.Parameters.Select(x => new Parameter(
                                        x.Name,
                                        x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                        x.RefKind switch {
                                            RefKind.Out => "out ",
                                            RefKind.Ref => "ref ",
                                            RefKind.In => "in ",
                                            RefKind.RefReadOnlyParameter => "ref readonly ",
                                            _ => string.Empty
                                        }))]));
                                break;
                        }
                    }

                    var proxy = new Proxy(
                        Name: $"{targetType.Name}Proxy",
                        Namespace: targetType.ContainingNamespace.IsGlobalNamespace ? null : targetType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        TargetType: targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        TargetLocation: targetType.Locations.First(), // TODO: Use all locations here
                        GenericTypes: [.. targetType.TypeParameters.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))],
                        Modifiers: GetModifiers(targetType),
                        Properties: [.. properties.Values],
                        Methods: [.. methods]);

                    return proxy;
                });
        //.WithTrackingName(TrackingNames.Proxy);

        context.RegisterImplementationSourceOutput(proxyProvider, (context, proxy) =>
        {
            if (proxy.Modifiers.HasFlag(Modifiers.Static))
            {
                context.ReportDiagnostic(UnsupportedStaticTypeDiagnostic.Create(proxy.TargetLocation, proxy.TargetType[8..]));
                return;
            }

            using var stream = new StringWriter();
            using var writer = new IndentedTextWriter(stream);
            writer.WriteProxy(proxy);
            writer.Flush();
            context.AddSource($"{proxy.Name}.g.cs", SourceText.From(stream.ToString(), Encoding.UTF8));
        });
    }

    private static bool CanBeProxied(ITypeSymbol type)
    {
        return type switch
        {
            { SpecialType: SpecialType.System_Void } => true,
            ITypeParameterSymbol { ConstraintTypes: var constraintTypes } => constraintTypes.All(CanBeProxied),
            IPointerTypeSymbol { PointedAtType: var pointedAtType } => TypeCanBeProxied(pointedAtType),
            _ => TypeCanBeProxied(type)
        };

        static bool TypeCanBeProxied(ITypeSymbol type) =>
            type.DeclaredAccessibility is Accessibility.Public;
    }

    private static bool CanBeProxied(IPropertySymbol property) => CanBeProxied(property.Type);

    private static bool CanBeProxied(IMethodSymbol method)
    {
        if (method.MethodKind is not MethodKind.Ordinary || !SyntaxFacts.IsValidIdentifier(method.Name))
            return false;

        if (!CanBeProxied(method.ReturnType))
            return false;

        if (method.TypeParameters.Any(x => x.ConstraintTypes.Any(t => !CanBeProxied(t))))
            return false;

        if (method.Parameters.Any(x => !CanBeProxied(x.Type)))
            return false;

        return true;
    }

    private static Modifiers GetModifiers(INamedTypeSymbol type)
    {
        var modifiers = Modifiers.Partial;

        if (type.IsStatic)
        {
            modifiers |= Modifiers.Static;
        }

        if (type.IsRefLikeType)
        {
            modifiers |= Modifiers.RefStruct;
        }

        return modifiers;
    }

    private static Modifiers GetModifiers(IPropertySymbol property)
    {
        var modifiers = Modifiers.None;

        if (property.IsStatic)
        {
            modifiers |= Modifiers.Static;
        }

        if (property.IsReadOnly)
        {
            modifiers |= Modifiers.ReadOnly;
        }

        if (property.Type is IPointerTypeSymbol)
        {
            modifiers |= Modifiers.Unsafe;
        }

        return modifiers;
    }

    private static Modifiers GetModifiers(IMethodSymbol method)
    {
        var modifiers = method.IsStatic ? Modifiers.Static : Modifiers.ReadOnly;

        if (method.ReturnType is IPointerTypeSymbol)
        {
            modifiers |= Modifiers.Unsafe;
        }

        if (method.RefKind is RefKind.Ref)
        {
            modifiers |= Modifiers.Ref;
        }

        if (method.RefKind is RefKind.RefReadOnly)
        {
            modifiers |= Modifiers.RefReadOnly;
        }

        return modifiers;
    }
}
