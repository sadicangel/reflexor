using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Reflexor;

public readonly record struct Proxy(
    string Name,
    string? Namespace,
    string TargetType,
    ImmutableArray<Property> Properties,
    ImmutableArray<Method> Methods);

public readonly record struct Property(string Name, string Type, bool IsReadOnly);
public readonly record struct Parameter(string Name, string Type, string Ref);
public readonly record struct Method(string Name, string ReturnType, ImmutableArray<Parameter> Parameters);

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
                                    propertySymbol.IsReadOnly
                                        || (properties.TryGetValue(propertySymbol.Name, out var existing) && existing.IsReadOnly));
                                break;

                            case IMethodSymbol methodSymbol when CanBeProxied(methodSymbol):
                                methods.Add(new Method(
                                    methodSymbol.Name,
                                    methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
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
                        Properties: [.. properties.Values],
                        Methods: [.. methods]);

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
            writer.WriteProperty(proxy.TargetType, property);
            writer.WriteLine();
        }

        foreach (var method in proxy.Methods)
        {
            writer.WriteMethod(proxy.TargetType, method);
            writer.WriteLine();
        }

#if DECLARE_IMPLICIT_CONVERSION // Replace with csproj property
        writer.WriteLine($"public static implicit operator {proxy.Name}({proxy.TargetType} target) => new {proxy.Name}(target);");
        writer.WriteLine();
#endif

        writer.Indent--;
        writer.WriteLine("}");

        if (!string.IsNullOrEmpty(proxy.Namespace))
        {
            writer.Indent--;
            writer.Write("}");
        }
    }

    public static void WriteProperty(this IndentedTextWriter writer, string targetType, Property property)
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
        writer.WriteLine($"extern static {property.Type} Get{property.Name}({targetType} target);");
        writer.Indent--;
        writer.WriteLine("}");

        if (!property.IsReadOnly)
        {
            writer.WriteLine();
            writer.WriteLine("set");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("ThrowInvalidOperationIfNotInitialized();");
            writer.WriteLine($"Set{property.Name}(_target, value);");
            writer.WriteLine();
            writer.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_{property.Name}\")]");
            writer.WriteLine($"extern static void Set{property.Name}({targetType} target, {property.Type} value);");
            writer.Indent--;
            writer.WriteLine("}");
        }

        writer.Indent--;
        writer.WriteLine("}");
    }

    public static void WriteMethod(this IndentedTextWriter writer, string targetType, Method method)
    {
        writer.Write("public ");
        if (IsOverride(method))
            writer.Write("override ");
        writer.Write(method.ReturnType);
        writer.Write(" ");
        writer.Write(method.Name);
        writer.Write("(");
        writer.WriteParameters(method.Parameters, prependComma: false);
        writer.WriteLine(")");
        writer.WriteLine("{");
        writer.Indent++;
        if (method.ReturnType is not "void")
            writer.Write("return ");
        writer.Write($"Call{method.Name}(_target");
        writer.WriteArguments(method.Parameters, prependComma: true);
        writer.WriteLine(");");
        writer.WriteLine();
        writer.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"{method.Name}\")]");
        writer.Write($"extern static {method.ReturnType} Call{method.Name}({targetType} target");
        writer.WriteParameters(method.Parameters, prependComma: true);
        writer.WriteLine(");");
        writer.Indent--;
        writer.WriteLine("}");

        static bool IsOverride(Method method)
        {
            return method.Name switch
            {
                nameof(ToString) => method.ReturnType is "string" && method.Parameters is [],
                nameof(Equals) => method.ReturnType is "bool" && method.Parameters is [{ Type: "object" }],
                nameof(GetHashCode) => method.ReturnType is "int" && method.Parameters is [],
                _ => false,
            };
        }
    }


    public static void WriteParameters(this IndentedTextWriter writer, ImmutableArray<Parameter> parameters, bool prependComma)
    {
        var isFirst = !prependComma;
        foreach (var parameter in parameters)
        {
            if (!isFirst) writer.Write(", ");
            else isFirst = false;
            writer.Write(parameter.Ref);
            writer.Write(parameter.Type);
            writer.Write(" ");
            writer.Write(parameter.Name);
        }
    }

    public static void WriteArguments(this IndentedTextWriter writer, ImmutableArray<Parameter> parameters, bool prependComma)
    {
        var isFirst = !prependComma;
        foreach (var parameter in parameters)
        {
            if (!isFirst) writer.Write(", ");
            else isFirst = false;
            writer.Write(parameter.Ref);
            writer.Write(parameter.Name);
        }
    }
}
