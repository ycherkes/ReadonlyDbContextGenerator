using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnitTests;

/// <summary>
/// Runs the generator over in-memory sources and checks that the resulting compilation
/// (user sources + generated sources) is free of errors.
/// </summary>
internal static class GeneratorHarness
{
    private static readonly Lazy<MetadataReference[]> References = new(LoadReferences);

    public static GeneratorDriver CreateDriver(bool trackSteps = false) =>
        CSharpGeneratorDriver.Create(
            generators: [new ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: trackSteps));

    public static CSharpCompilation CreateCompilation(string assemblyName, params SyntaxTree[] trees) =>
        CSharpCompilation.Create(
            assemblyName,
            trees,
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    public static SyntaxTree Parse(string source) =>
        CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

    /// <summary>
    /// Runs the generator, asserts it reported no error diagnostics and that the output compiles.
    /// </summary>
    public static GeneratorRunResult RunAndAssertCompiles(string source, string assemblyName = "GeneratorTest")
    {
        var compilation = CreateCompilation(assemblyName, Parse(source));
        AssertNoErrors(compilation.GetDiagnostics(), "input source");

        var driver = CreateDriver().RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
        AssertNoErrors(generatorDiagnostics, "generator");
        AssertNoErrors(outputCompilation.GetDiagnostics(), "generated output", outputCompilation);

        return driver.GetRunResult().Results.Single();
    }

    private static void AssertNoErrors(IEnumerable<Diagnostic> diagnostics, string stage, Compilation? compilation = null)
    {
        // CS8785: the generator threw (Debug builds rethrow). RDCTX002: crash reported as a diagnostic.
        var errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error || d.Id is "CS8785" or "RDCTX002")
            .ToArray();
        if (errors.Length == 0)
        {
            return;
        }

        var message = $"Errors in {stage}:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(e => e.ToString()))}";
        if (compilation != null)
        {
            var generated = compilation.SyntaxTrees.Where(t => t.FilePath.EndsWith(".g.cs", StringComparison.Ordinal));
            message += Environment.NewLine + string.Join(Environment.NewLine + "----" + Environment.NewLine,
                generated.Select(t => $"// {t.FilePath}{Environment.NewLine}{t}"));
        }

        Assert.Fail(message);
    }

    private static MetadataReference[] LoadReferences()
    {
        // The test host's trusted platform assemblies include the BCL and every package the
        // test project references (EF Core, relational, SQLite provider).
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        return tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
