using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Reflexor;

public readonly record struct Proxy(string Name, string? Namespace, string TargetType, ImmutableArray<Property> Properties);

public readonly record struct Property(string Name, string Type);

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
                    var symbol = Unsafe.As<INamedTypeSymbol>(context.TargetSymbol);
                    var properties = new HashSet<Property>(PropertyComparer.Instance);
                    foreach (var propertySymbol in symbol.GetMembers().OfType<IPropertySymbol>())
                    {
                        var property = new Property(propertySymbol.Name, propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        properties.Remove(property);
                        if (!propertySymbol.IsReadOnly)
                        {
                            properties.Add(property);
                        }
                    }

                    var proxy = new Proxy(
                        Name: $"{symbol.Name}Proxy",
                        Namespace: symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        TargetType: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        Properties: [.. properties]);

                    return proxy;
                });
        //.WithTrackingName(TrackingNames.Proxy);

        context.RegisterImplementationSourceOutput(proxyProvider, (context, proxy) =>
        {
            var property = proxy.Properties[0];
            context.AddSource($"{proxy.Name}.g.cs", SourceText.From($$""""
                namespace {{proxy.Namespace}}
                {
                    public partial struct {{proxy.Name}}
                    {
                        private readonly {{proxy.TargetType}} _target;

                        public {{proxy.Name}}({{proxy.TargetType}} target)
                        {
                            _target = target ?? throw new System.ArgumentNullException(nameof(target));
                        }

                        private void ThrowInvalidOperationIfNotInitialized()
                        {
                            if (_target is null)
                            {
                                throw new global::System.InvalidOperationException("Proxy for '{{proxy.TargetType[8..]}}' is uninitialized");
                            }
                        }
                        {{string.Join("\n", proxy.Properties.Select(property => $$"""

                                public {{property.Type}} {{property.Name}}
                                {
                                    get
                                    {
                                        ThrowInvalidOperationIfNotInitialized();
                                        return _target.{{property.Name}};
                                    }
                                    set
                                    {
                                        ThrowInvalidOperationIfNotInitialized();
                                        Set{{property.Name}}(_target, value);

                                        [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_{{property.Name}}")]
                                        extern static void Set{{property.Name}}({{proxy.TargetType}} target, {{property.Type}} value);
                                    }
                                }
                        """))}}
                    }
                }
                """",
                Encoding.UTF8));
        });
    }

    private sealed class PropertyComparer : IEqualityComparer<Property>
    {
        public static readonly PropertyComparer Instance = new();

        public bool Equals(Property x, Property y) => x.Name == y.Name;
        public int GetHashCode(Property obj) => obj.Name?.GetHashCode() ?? 0;
    }
}
