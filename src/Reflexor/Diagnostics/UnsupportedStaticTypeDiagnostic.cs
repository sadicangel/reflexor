using Microsoft.CodeAnalysis;

namespace Reflexor.Diagnostics;

internal static class UnsupportedStaticTypeDiagnostic
{
    private static readonly DiagnosticDescriptor s_descriptor = new(
        id: "RFL0001",
        title: "Unsupported Static Type",
        messageFormat: "Unsupported static type '{0}'. Static types will be supported from .NET 10 onwards.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic Create(Location location, string targetType) =>
        Diagnostic.Create(s_descriptor, location, targetType);
}
