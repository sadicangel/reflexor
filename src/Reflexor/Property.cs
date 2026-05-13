using Microsoft.CodeAnalysis;

namespace Reflexor;

internal sealed record class Property(
    string Name,
    string MetadataName,
    string Type,
    string AccessorTargetType,
    string AccessorDisplayTargetType,
    bool IsGetPublic,
    bool IsSetPublic,
    bool IsStatic,
    bool IsReadOnly,
    bool IsUnsafe)
{
    public static Property FromSymbol(IPropertySymbol propertySymbol)
    {
        return new Property(
            Name: propertySymbol.GetSafeIdentifier(),
            MetadataName: propertySymbol.Name,
            Type: propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedNullableFormat),
            AccessorTargetType: propertySymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedNullableFormat),
            AccessorDisplayTargetType: propertySymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            IsGetPublic: propertySymbol.GetMethod?.DeclaredAccessibility is Accessibility.Public,
            IsSetPublic: propertySymbol.SetMethod is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false },
            IsStatic: propertySymbol.IsStatic,
            IsReadOnly: propertySymbol.IsReadOnly,
            IsUnsafe: propertySymbol.Type is IPointerTypeSymbol);
    }
}
