using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Reflexor;

[Generator]
public sealed class ProxyGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat s_fullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

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

                    foreach (var type in EnumerateTargetAndBaseTypes(targetType))
                    {
                        foreach (var member in type.GetMembers())
                        {
                            switch (member)
                            {
                                case IPropertySymbol propertySymbol when CanBeProxied(propertySymbol) &&
                                    !properties.ContainsKey(propertySymbol.Name):
                                    properties[propertySymbol.Name] = CreateProperty(propertySymbol, properties);
                                    break;

                                case IMethodSymbol methodSymbol when CanBeProxied(methodSymbol):
                                    var key = GetMethodKey(methodSymbol);
                                    if (!methods.ContainsKey(key))
                                    {
                                        methods[key] = CreateMethod(methodSymbol);
                                    }

                                    break;
                            }
                        }
                    }

                    return new Proxy(
                        Name: $"{targetType.Name}Proxy",
                        Namespace: targetType.ContainingNamespace.IsGlobalNamespace
                            ? null
                            : targetType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        Accessibility: targetType.DeclaredAccessibility,
                        TargetType: targetType.ToDisplayString(s_fullyQualifiedNullableFormat),
                        DisplayTargetType: targetType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        IsStatic: targetType.IsStatic,
                        IsRefLike: targetType.IsRefLikeType,
                        GenericTypes: GetGenericTypes(targetType.TypeParameters),
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

    private static Property CreateProperty(
        IPropertySymbol propertySymbol,
        Dictionary<string, Property> properties)
    {
        var isReadOnly = propertySymbol.IsReadOnly ||
            (properties.TryGetValue(propertySymbol.Name, out var existing) && existing.IsReadOnly);

        return new Property(
            Name: propertySymbol.Name,
            Type: propertySymbol.Type.ToDisplayString(s_fullyQualifiedNullableFormat),
            AccessorTargetType: propertySymbol.ContainingType.ToDisplayString(s_fullyQualifiedNullableFormat),
            AccessorDisplayTargetType: propertySymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            IsStatic: propertySymbol.IsStatic,
            IsReadOnly: isReadOnly,
            IsUnsafe: propertySymbol.Type is IPointerTypeSymbol);
    }

    private static Method CreateMethod(IMethodSymbol methodSymbol)
    {
        return new Method(
            Name: methodSymbol.Name,
            ReturnType: methodSymbol.ReturnType.ToDisplayString(s_fullyQualifiedNullableFormat),
            AccessorTargetType: methodSymbol.ContainingType.ToDisplayString(s_fullyQualifiedNullableFormat),
            AccessorDisplayTargetType: methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            IsStatic: methodSymbol.IsStatic,
            IsOverride: CanBeProxyOverride(methodSymbol),
            IsReadOnly: !methodSymbol.IsStatic,
            IsUnsafe: methodSymbol.ReturnType is IPointerTypeSymbol ||
            methodSymbol.Parameters.Any(static x => x.Type is IPointerTypeSymbol),
            ReturnsByRef: methodSymbol.RefKind is RefKind.Ref,
            ReturnsByRefReadonly: methodSymbol.RefKind is RefKind.RefReadOnly,
            GenericTypes: GetGenericTypes(methodSymbol),
            Parameters:
            [
                .. methodSymbol.Parameters.Select(static x => new Parameter(
                    x.Name,
                    x.Type.ToDisplayString(s_fullyQualifiedNullableFormat),
                    x.RefKind switch
                    {
                        RefKind.Out => "out ",
                        RefKind.Ref => "ref ",
                        RefKind.In => "in ",
                        RefKind.RefReadOnlyParameter => "ref readonly ",
                        _ => string.Empty
                    }))
            ]);
    }

    private static bool CanBeProxyOverride(IMethodSymbol methodSymbol) =>
        methodSymbol is { IsOverride: true, OverriddenMethod.ContainingType.SpecialType: SpecialType.System_Object };

    private static IEnumerable<INamedTypeSymbol> EnumerateTargetAndBaseTypes(INamedTypeSymbol targetType)
    {
        for (var type = targetType; type is not null && type.SpecialType is not SpecialType.System_Object; type = type.BaseType)
        {
            yield return type;
        }
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
            builder.Append(parameter.Type.ToDisplayString(s_fullyQualifiedNullableFormat));
            builder.Append(';');
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static bool CanBeProxied(ITypeSymbol type)
    {
        return type switch
        {
            { SpecialType: SpecialType.System_Void } => true,
            ITypeParameterSymbol { ConstraintTypes: var constraintTypes } => constraintTypes.All(CanBeProxied),
            IPointerTypeSymbol { PointedAtType: var pointedAtType } => CanBeProxied(pointedAtType),
            _ => type.DeclaredAccessibility is Accessibility.Public,
        };
    }

    private static bool CanBeProxied(IPropertySymbol property) => CanBeProxied(property.Type);

    private static bool CanBeProxied(IMethodSymbol method)
    {
        return method.MethodKind is MethodKind.Ordinary
            && SyntaxFacts.IsValidIdentifier(method.Name)
            && CanBeProxied(method.ReturnType)
            && method.TypeParameters.All(static x => x.ConstraintTypes.All(CanBeProxied))
            && method.Parameters.All(static x => CanBeProxied(x.Type));
    }

    private static ImmutableArray<GenericType> GetGenericTypes(IMethodSymbol method) =>
        GetGenericTypes(method.TypeParameters);

    private static ImmutableArray<GenericType> GetGenericTypes(ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (typeParameters.Length is 0)
        {
            return [];
        }

        var constraints = ImmutableArray.CreateBuilder<GenericType>(typeParameters.Length);

        foreach (var typeParameter in typeParameters)
        {
            constraints.Add(
                new GenericType(
                    Name: typeParameter.Name,
                    Constraints: [.. EnumerateConstraints(typeParameter)]));
        }

        return constraints.MoveToImmutable();

        static IEnumerable<string> EnumerateConstraints(ITypeParameterSymbol typeParameter)
        {
            if (typeParameter is { HasValueTypeConstraint: true, HasUnmanagedTypeConstraint: false })
            {
                yield return "struct";
            }

            if (typeParameter.HasReferenceTypeConstraint)
            {
                yield return typeParameter.NullableAnnotation is NullableAnnotation.Annotated ? "class?" : "class";
            }

            if (typeParameter.HasNotNullConstraint)
            {
                yield return "notnull";
            }

            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                yield return "unmanaged";
            }

            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                yield return constraintType.ToDisplayString(s_fullyQualifiedNullableFormat);
            }

            if (typeParameter.HasConstructorConstraint)
            {
                yield return "new()";
            }
        }
    }
}
