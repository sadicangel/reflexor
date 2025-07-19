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
    ImmutableArray<Property> Properties,
    ImmutableArray<Method> Methods)
{
    public bool IsStatic => Modifiers.HasFlag(Modifiers.Static);
}

[Flags]
public enum Modifiers
{
    None = 0,
    Static = 1 << 0,
    ReadOnly = 1 << 1,
    Unsafe = 1 << 2,
    Ref = 1 << 3,
    Partial = 1 << 4,
}

public static class ModifiersExtensions
{
    public static string ToModifierString(this Modifiers modifiers)
    {
        var sb = new StringBuilder();
        if (modifiers.HasFlag(Modifiers.Static))
            sb.Append("static ");
        if (modifiers.HasFlag(Modifiers.ReadOnly))
            sb.Append("readonly ");
        if (modifiers.HasFlag(Modifiers.Unsafe))
            sb.Append("unsafe ");
        if (modifiers.HasFlag(Modifiers.Ref))
            sb.Append("ref ");
        if (modifiers.HasFlag(Modifiers.Partial))
            sb.Append("partial ");
        return sb.ToString();
    }
}

public readonly record struct Property(string Name, string Type, Modifiers Modifiers)
{
    public bool IsStatic => Modifiers.HasFlag(Modifiers.Static);
    public bool IsReadOnly => Modifiers.HasFlag(Modifiers.ReadOnly);
    public bool IsUnsafe => Modifiers.HasFlag(Modifiers.Static);
}

public readonly record struct Parameter(string Name, string Type, string Ref);
public readonly record struct Method(string Name, string ReturnType, bool IsStatic, ImmutableArray<Parameter> Parameters);

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
                                    methodSymbol.Name,
                                    methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    methodSymbol.IsStatic,
                                    [.. methodSymbol.Parameters.Select(x => new Parameter(
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
                        Modifiers: GetModifiers(targetType),
                        Properties: [.. properties.Values],
                        Methods: [.. methods]);

                    return proxy;
                });
        //.WithTrackingName(TrackingNames.Proxy);

        context.RegisterImplementationSourceOutput(proxyProvider, (context, proxy) =>
        {
            if (proxy.IsStatic)
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

    private static bool CanBeProxied(IPropertySymbol property)
    {
        return property.Type switch
        {
            IPointerTypeSymbol { PointedAtType: var pointedAtType } => TypeCanBeProxied(pointedAtType),
            _ => TypeCanBeProxied(property.Type)
        };

        static bool TypeCanBeProxied(ITypeSymbol type) =>
            type.DeclaredAccessibility is Accessibility.Public;
    }

    private static Modifiers GetModifiers(INamedTypeSymbol type)
    {
        var modifiers = Modifiers.Partial;

        if (type.IsStatic)
        {
            modifiers |= Modifiers.Static;
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

    private static bool CanBeProxied(IMethodSymbol method)
    {
        if (method.MethodKind is not MethodKind.Ordinary || !SyntaxFacts.IsValidIdentifier(method.Name))
            return false;

        if (!method.ReturnsVoid && method.ReturnType.DeclaredAccessibility is not Accessibility.Public)
            return false;

        if (method.TypeParameters.Any(x => x.ConstraintTypes.Any(t => t.DeclaredAccessibility is not Accessibility.Public)))
            return false;

        if (method.Parameters.Any(x => x.Type.DeclaredAccessibility is not Accessibility.Public))
            return false;

        return true;
    }
}
