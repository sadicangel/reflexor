using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Reflexor;

internal static class SymbolExtensions
{
    extension(ISymbol symbol)
    {
        public string GetSafeIdentifier()
        {
            var name = symbol.Name;
            return SyntaxFacts.GetKeywordKind(name) is SyntaxKind.None
                ? name
                : $"@{name}";
        }
    }

    extension(IMethodSymbol method)
    {
        public ImmutableArray<GenericType> GetGenericTypes() =>
            GetGenericTypes(method.TypeParameters);

        public bool CanBeProxied()
        {
            return method.MethodKind is MethodKind.Ordinary
                && CanBeIdentifier(method.Name)
                && method.ReturnType.CanBeProxied()
                && method.TypeParameters.All(static x => x.ConstraintTypes.All(CanBeProxied))
                && method.Parameters.All(static x => x.Type.CanBeProxied());

            static bool CanBeIdentifier(string name) =>
                SyntaxFacts.IsValidIdentifier(name) || SyntaxFacts.GetKeywordKind(name) is not SyntaxKind.None;
        }
    }

    extension(IPropertySymbol property)
    {
        public bool CanBeProxied() => property.Type.CanBeProxied();
    }


    extension(ITypeSymbol type)
    {
        public bool CanBeProxied()
        {
            return type switch
            {
                { SpecialType: SpecialType.System_Void } => true,
                ITypeParameterSymbol { ConstraintTypes: var constraintTypes } => constraintTypes.All(CanBeProxied),
                IPointerTypeSymbol { PointedAtType: var pointedAtType } => pointedAtType.CanBeProxied(),
                _ => type.DeclaredAccessibility is Accessibility.Public,
            };
        }
    }

    extension(INamedTypeSymbol type)
    {
        public IEnumerable<INamedTypeSymbol> EnumerateSelfAndAncestors()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            for (; type is not null && type.SpecialType is not SpecialType.System_Object; type = type.BaseType!)
            {
                yield return type;
            }
        }

        public ImmutableArray<GenericType> GetGenericTypes() =>
            GetGenericTypes(type.TypeParameters);
    }

    extension(INamespaceSymbol namespaceSymbol)
    {
        public string? GetSafeNamespace()
        {
            if (namespaceSymbol.IsGlobalNamespace)
            {
                return null;
            }

            var names = new Stack<string>();
            for (var current = namespaceSymbol; !current.IsGlobalNamespace; current = current.ContainingNamespace)
            {
                names.Push(current.GetSafeIdentifier());
            }

            return string.Join(".", names);
        }
    }

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
                    Name: typeParameter.GetSafeIdentifier(),
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
                yield return constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedNullableFormat);
            }

            if (typeParameter.HasConstructorConstraint)
            {
                yield return "new()";
            }
        }
    }
}
