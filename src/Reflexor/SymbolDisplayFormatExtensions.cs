using Microsoft.CodeAnalysis;

namespace Reflexor;

internal static class SymbolDisplayFormatExtensions
{
    private static readonly SymbolDisplayFormat s_fullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    extension(SymbolDisplayFormat)
    {
        public static SymbolDisplayFormat FullyQualifiedNullableFormat => s_fullyQualifiedNullableFormat;
    }
}
