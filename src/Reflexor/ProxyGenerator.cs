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
    bool IsStatic,
    ImmutableArray<Property> Properties,
    ImmutableArray<Method> Methods);

public readonly record struct Property(string Name, string Type, bool IsStatic, bool IsReadOnly);
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
                                    propertySymbol.Name,
                                    propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    propertySymbol.IsStatic,
                                    propertySymbol.IsReadOnly
                                        || (properties.TryGetValue(propertySymbol.Name, out var existing) && existing.IsReadOnly));
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
                        IsStatic: targetType.IsStatic,
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

    private static bool CanBeProxied(IPropertySymbol property) =>
        property.Type.DeclaredAccessibility is Accessibility.Public;

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
