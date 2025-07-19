using System.Collections.Immutable;
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

    public static SettingsTask Verify(string source, ProjectConfig? config = null)
    {
        var (diagnostics, documents) = GenerateOutput([source], [], config);

        return diagnostics switch
        {
            [] => Verifier.Verify(documents.Select(document => new Target("txt", document.Content))),
            [var diagnostic] => Verifier.Verify(diagnostic.ToString()),
            _ => Fail(diagnostics)
        };

        static SettingsTask Fail(ImmutableArray<Diagnostic> diagnostics)
        {
            Assert.Fail(string.Join(Environment.NewLine, diagnostics));

            return null!; // This line is unreachable but required for compilation.
        }
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
