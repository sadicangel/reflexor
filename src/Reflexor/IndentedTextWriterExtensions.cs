using System.CodeDom.Compiler;
using System.Collections.Immutable;

namespace Reflexor;

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

        writer.WriteInstanceMembers(proxy);

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

    public static void WriteInstanceMembers(this IndentedTextWriter writer, Proxy proxy)
    {
        if (!proxy.IsStatic)
        {
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
        }
    }

    public static void WriteProperty(this IndentedTextWriter writer, string targetType, Property property)
    {
        var target = property.IsStatic ? "null!" : "_target";

        var accessorKind = property.IsStatic
            ? "global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod"
            : "global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method";

        var modifiers = property.IsStatic ? "static " : string.Empty;

        writer.WriteLine($"public {modifiers}{property.Type} {property.Name}");
        writer.WriteLine("{");
        writer.Indent++;

        writer.WriteLine("get");
        writer.WriteLine("{");
        writer.Indent++;
        if (!property.IsStatic)
        {
            writer.WriteLine("ThrowInvalidOperationIfNotInitialized();");
        }
        writer.WriteLine($"return Get{property.Name}({target});");
        writer.WriteLine();
        writer.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor({accessorKind}, Name = \"get_{property.Name}\")]");
        writer.WriteLine($"extern static {property.Type} Get{property.Name}({targetType} target);");
        writer.Indent--;
        writer.WriteLine("}");

        if (!property.IsReadOnly)
        {
            writer.WriteLine();
            writer.WriteLine("set");
            writer.WriteLine("{");
            writer.Indent++;
            if (!property.IsStatic)
            {
                writer.WriteLine("ThrowInvalidOperationIfNotInitialized();");
            }
            writer.WriteLine($"Set{property.Name}({target}, value);");
            writer.WriteLine();
            writer.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor({accessorKind}, Name = \"set_{property.Name}\")]");
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
