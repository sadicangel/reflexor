using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Reflexor.Tests.Helpers;

internal readonly record struct GeneratorSetup(CSharpParseOptions ParseOptions, AnalyzerConfigOptionsProvider OptionsProvider, Compilation Compilation, GeneratorDriver GeneratorDriver)
{
    public static GeneratorSetup Create(
        ImmutableArray<string> sourceTexts,
        ImmutableArray<string> additionalTexts,
        ProjectConfig? projectConfig = null)
    {
        projectConfig ??= ProjectConfig.Default;
        var parseOptions = CreateParseOptions(projectConfig);
        var optionsProvider = CreateOptionsProvider(projectConfig);
        var generatorDriver = CreateGeneratorDriver(additionalTexts, parseOptions, optionsProvider);
        var compilation = CreateCompilation(sourceTexts, parseOptions);
        return new GeneratorSetup(parseOptions, optionsProvider, compilation, generatorDriver);
    }

    public static CSharpParseOptions CreateParseOptions(ProjectConfig projectConfig) => new CSharpParseOptions(projectConfig.LanguageVersion);

    public static AnalyzerConfigOptionsProvider CreateOptionsProvider(ProjectConfig projectConfig) =>
        new AnalyzerConfigOptionsProviderImplementation(projectConfig.GlobalOptions);

    public static Compilation CreateCompilation(
        ImmutableArray<string> sourceTexts,
        CSharpParseOptions parseOptions)
    {
        var syntaxTrees = sourceTexts.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions));
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(ProxyGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GeneratedCodeAttribute).Assembly.Location),
            ]);

        var compilation = CSharpCompilation.Create(
            "GeneratorAssemblyName",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                warningLevel: int.MaxValue,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Annotations));

        return compilation;
    }

    public static CSharpGeneratorDriver CreateGeneratorDriver(
        ImmutableArray<string> additionalTexts,
        CSharpParseOptions parseOptions,
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        var generatorDriver = CSharpGeneratorDriver.Create(
            generators: [new ProxyGenerator().AsSourceGenerator()],
            additionalTexts: null,
            parseOptions: parseOptions,
            optionsProvider: optionsProvider,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        return generatorDriver;
    }

    private sealed class AnalyzerConfigOptionsProviderImplementation(IEnumerable<KeyValuePair<string, string>> globalOptions)
        : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new AnalyzerConfigOptionsImplementation(globalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class AnalyzerConfigOptionsImplementation(IEnumerable<KeyValuePair<string, string>> options)
            : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> _options = new Dictionary<string, string>([.. options.Select(kvp => new KeyValuePair<string, string>($"build_property.{kvp.Key}", kvp.Value))]);

            public override bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
                => _options.TryGetValue(key, out value);
        }
    }
}
