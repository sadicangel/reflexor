using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Reflexor.Tests.Helpers;

public readonly record struct Document(string FileName, string Content);

internal static class TestHelper
{
    public static readonly ImmutableArray<string> TrackingNames = [.. typeof(ProxyGenerator)
        .Assembly
        .GetType("AvroSourceGenerator.Parsing.TrackingNames", throwOnError: true)!
        .GetFields()
        .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
        .Select(x => (string?)x.GetRawConstantValue()!)
        .Where(x => !string.IsNullOrEmpty(x))];

    public static SettingsTask VerifySourceCode(string source, ProjectConfig? config = null)
    {
        var (diagnostics, documents) = GenerateOutput([source], [], config);

        if (diagnostics.Length > 0)
        {
            Assert.Fail(string.Join(
                Environment.NewLine,
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage(CultureInfo.InvariantCulture)}")));
        }

        return Verify(documents.Skip(1).Select(document => new Target("txt", document.Content)));
    }

    public static SettingsTask VerifyDiagnostic(string source, ProjectConfig? config = null)
    {
        var (diagnostics, _) = GenerateOutput([source], [], config);

        return Verify(Assert.Single(diagnostics));
    }

    public static ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(GeneratorDriverRunResult result) =>
        result.Results[0].TrackedSteps.Where(x => TrackingNames.Contains(x.Key)).ToImmutableDictionary();

    public static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<Document> Documents) GenerateOutput(
        ImmutableArray<string> sourceTexts,
        ImmutableArray<string> additionalTexts,
        ProjectConfig? projectConfig = null)
    {
        var (parseOptions, optionsProvider, compilation, generatorDriver) =
            GeneratorSetup.Create(sourceTexts, additionalTexts, projectConfig);

        generatorDriver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);

        var analyzerDiagnostics = compilation
            .WithAnalyzers(DiagnosticAnalyzers.Analyzers, new AnalyzerOptions([], optionsProvider))
            .GetAllDiagnosticsAsync().GetAwaiter().GetResult()
            .RemoveAll(x => x.DefaultSeverity < DiagnosticSeverity.Warning);

        diagnostics = diagnostics.AddRange(analyzerDiagnostics);

        var documents = compilation.SyntaxTrees
            .Where(st => !string.IsNullOrEmpty(st.FilePath))
            .Select(st => new Document(st.FilePath.Replace('\\', '/'), st.ToString()))
            .ToImmutableArray();

        return (diagnostics, documents);
    }
}
