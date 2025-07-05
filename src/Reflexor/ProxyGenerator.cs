using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Reflexor;

public readonly record struct Proxy(string Name, string? Namespace, string TargetType, ImmutableArray<Property> Properties);

public readonly record struct Property(string Name, string Type, bool IsReadOnly);

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
                    foreach (var member in targetType.GetMembers())
                    {
                        switch (member)
                        {
                            case IPropertySymbol propertySymbol:
                                properties[propertySymbol.Name] = new Property(
                                    propertySymbol.Name,
                                    propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    propertySymbol.IsReadOnly
                                        || (properties.TryGetValue(propertySymbol.Name, out var existing) && existing.IsReadOnly));
                                break;

                            case IMethodSymbol methodSymbol:
                                break;
                        }
                    }

                    var proxy = new Proxy(
                        Name: $"{targetType.Name}Proxy",
                        Namespace: targetType.ContainingNamespace.IsGlobalNamespace ? null : targetType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        TargetType: targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        Properties: [.. properties.Values]);

                    return proxy;
                });
        //.WithTrackingName(TrackingNames.Proxy);

        context.RegisterImplementationSourceOutput(proxyProvider, (context, proxy) =>
        {
            using var stream = new StringWriter();
            using var writer = new IndentedTextWriter(stream);
            writer.WriteProxy(proxy);
            writer.Flush();
            context.AddSource($"{proxy.Name}.g.cs", SourceText.From(stream.ToString(), Encoding.UTF8));
        });
    }
}

public static class IndentedTextWriterExtensions
{
    public static void WriteProxy(this IndentedTextWriter writer, Proxy proxy)
    {
        if (!string.IsNullOrEmpty(proxy.Namespace))
        {
            writer.WriteLine($"namespace {proxy.Namespace}");
            writer.WriteLine("{");
            writer.Indent++;
        }

        writer.WriteLine($"public partial struct {proxy.Name}");
        writer.WriteLine("{");
        writer.Indent++;
        writer.WriteLine($"private readonly {proxy.TargetType} _target;");
        writer.WriteLine();

        writer.WriteLine($"public {proxy.Name}({proxy.TargetType} target)");
        writer.WriteLine("{");
        writer.Indent++;
        writer.WriteLine("_target = target ?? throw new System.ArgumentNullException(nameof(target));");
        writer.Indent--;
        writer.WriteLine("}");
        writer.WriteLine();

        writer.WriteLine("private void ThrowInvalidOperationIfNotInitialized()");
        writer.WriteLine("{");
        writer.Indent++;
        writer.WriteLine("if (_target is null)");
        writer.WriteLine("{");
        writer.Indent++;
        writer.WriteLine($"throw new global::System.InvalidOperationException(\"Proxy for '{proxy.TargetType[8..]}' is uninitialized\");");
        writer.Indent--;
        writer.WriteLine("}");
        writer.Indent--;
        writer.WriteLine("}");
        writer.WriteLine();

        foreach (var property in proxy.Properties)
        {
            writer.WriteProperty(property, proxy);
            writer.WriteLine();
        }

        writer.WriteLine($"public static implicit operator {proxy.Name}({proxy.TargetType} target) => new {proxy.Name}(target);");
        writer.WriteLine();

        writer.Indent--;
        writer.WriteLine("}");

        if (!string.IsNullOrEmpty(proxy.Namespace))
        {
            writer.Indent--;
            writer.Write("}");
        }
    }

    public static void WriteProperty(this IndentedTextWriter writer, Property property, Proxy proxy)
    {
        writer.WriteLine($"public {property.Type} {property.Name}");
        writer.WriteLine("{");
        writer.Indent++;

        writer.WriteLine("get");
        writer.WriteLine("{");
        writer.Indent++;
        writer.WriteLine("ThrowInvalidOperationIfNotInitialized();");
        writer.WriteLine($"return Get{property.Name}(_target);");
        writer.WriteLine();
        writer.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"get_{property.Name}\")]");
        writer.WriteLine($"extern static {property.Type} Get{property.Name}({proxy.TargetType} target);");
        writer.Indent--;
        writer.WriteLine("}");
        writer.WriteLine();

        if (!property.IsReadOnly)
        {
            writer.WriteLine("set");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("ThrowInvalidOperationIfNotInitialized();");
            writer.WriteLine($"Set{property.Name}(_target, value);");
            writer.WriteLine();
            writer.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_{property.Name}\")]");
            writer.WriteLine($"extern static void Set{property.Name}({proxy.TargetType} target, {property.Type} value);");
            writer.Indent--;
            writer.WriteLine("}");
        }

        writer.Indent--;
        writer.WriteLine("}");
    }
}
