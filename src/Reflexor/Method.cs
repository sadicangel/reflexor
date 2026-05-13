using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Reflexor;

internal readonly record struct Method(
    string Name,
    string MetadataName,
    string ReturnType,
    string AccessorTargetType,
    string AccessorDisplayTargetType,
    bool IsStatic,
    bool IsOverride,
    bool IsReadOnly,
    bool IsUnsafe,
    bool ReturnsByRef,
    bool ReturnsByRefReadonly,
    ImmutableArray<GenericType> GenericTypes,
    ImmutableArray<Parameter> Parameters)
{
    public static Method FromSymbol(IMethodSymbol methodSymbol)
    {
        return new Method(
            Name: methodSymbol.GetSafeIdentifier(),
            MetadataName: methodSymbol.Name,
            ReturnType: methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedNullableFormat),
            AccessorTargetType: methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedNullableFormat),
            AccessorDisplayTargetType: methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            IsStatic: methodSymbol.IsStatic,
            IsOverride: CanBeProxyOverride(methodSymbol),
            IsReadOnly: !methodSymbol.IsStatic,
            IsUnsafe: methodSymbol.ReturnType is IPointerTypeSymbol ||
            methodSymbol.Parameters.Any(static x => x.Type is IPointerTypeSymbol),
            ReturnsByRef: methodSymbol.RefKind is RefKind.Ref,
            ReturnsByRefReadonly: methodSymbol.RefKind is RefKind.RefReadOnly,
            GenericTypes: methodSymbol.GetGenericTypes(),
            Parameters:
            [
                .. methodSymbol.Parameters.Select(static x => new Parameter(
                    x.GetSafeIdentifier(),
                    x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedNullableFormat),
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
}
