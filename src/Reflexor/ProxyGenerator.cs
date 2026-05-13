using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Reflexor;

[Generator]
public sealed class ProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var proxyProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Reflexor.ReflexorAttribute",
                predicate: static (syntax, _) => syntax.IsKind(SyntaxKind.ClassDeclaration) || syntax.IsKind(SyntaxKind.RecordDeclaration),
                transform: static (context, _) =>
                {
                    var targetType = Unsafe.As<INamedTypeSymbol>(context.TargetSymbol);
                    var properties = new Dictionary<string, Property>();
                    var methods = new Dictionary<string, Method>();

                    foreach (var type in targetType.EnumerateSelfAndAncestors())
                    {
                        foreach (var member in type.GetMembers())
                        {
                            switch (member)
                            {
                                case IPropertySymbol propertySymbol when propertySymbol.CanBeProxied() && !properties.ContainsKey(propertySymbol.Name):
                                    properties[propertySymbol.Name] = Property.FromSymbol(propertySymbol);
                                    break;

                                case IMethodSymbol methodSymbol when methodSymbol.CanBeProxied():
                                    var key = GetMethodKey(methodSymbol);
                                    if (!methods.ContainsKey(key))
                                    {
                                        methods[key] = Method.FromSymbol(methodSymbol);
                                    }

                                    break;
                            }
                        }
                    }

                    return new Proxy(
                        Name: $"{targetType.Name}Proxy",
                        Namespace: targetType.ContainingNamespace.GetSafeNamespace(),
                        Accessibility: targetType.DeclaredAccessibility,
                        TargetType: targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedNullableFormat),
                        DisplayTargetType: targetType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        IsStatic: targetType.IsStatic,
                        IsRefLike: targetType.IsRefLikeType,
                        GenericTypes: targetType.GetGenericTypes(),
                        Properties: [.. properties.Values],
                        Methods: [.. methods.Values]);
                });

        context.RegisterImplementationSourceOutput(
            proxyProvider,
            static (context, proxy) =>
            {
                var writer = new IndentedStringBuilder();
                writer.WriteProxy(proxy);
                context.AddSource($"{proxy.Name}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
            });

        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource(
                "ReflexorAttribute.g.cs",
                SourceText.From(
                    """
                    namespace Reflexor
                    {
                        [global::System.AttributeUsage(global::System.AttributeTargets.Class)]
                        internal sealed class ReflexorAttribute : global::System.Attribute { }
                    }
                    """,
                    Encoding.UTF8));
        });
    }

    private static string GetMethodKey(IMethodSymbol methodSymbol)
    {
        var builder = new StringBuilder();
        builder.Append(methodSymbol.Name);
        builder.Append('`');
        builder.Append(methodSymbol.TypeParameters.Length);
        builder.Append('(');

        foreach (var parameter in methodSymbol.Parameters)
        {
            builder.Append(parameter.RefKind);
            builder.Append(' ');
            builder.Append(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedNullableFormat));
            builder.Append(';');
        }

        builder.Append(')');
        return builder.ToString();
    }
}
