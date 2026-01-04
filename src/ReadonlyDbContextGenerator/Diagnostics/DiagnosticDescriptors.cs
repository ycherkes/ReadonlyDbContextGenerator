using Microsoft.CodeAnalysis;

namespace ReadonlyDbContextGenerator.Diagnostics;

// Borrowed from: https://github.com/themidnightgospel/Imposter

public static class DiagnosticDescriptors
{
    private const string CrashIssueUrl =
        "https://github.com/ycherkes/ReadonlyDbContextGenerator/issues/new?labels=bug&title=Generator%20crash:%20RDCTX002";

    public static readonly DiagnosticDescriptor GeneratorCrash = new(
        "RDCTX002",
        "Generator crash",
        "Unhandled exception while generating readonly DbContext: '{0}'",
        "SourceGeneration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An unexpected exception bubbled out of the source generator.",
        helpLinkUri: CrashIssueUrl
    );

    public static readonly DiagnosticDescriptor SkippedExternalDbSet = new(
        id: "RDCTX001",
        title: "Readonly DbContext skipping external DbSet",
        messageFormat: "DbContext '{0}' DbSet '{1}' uses external entity type '{2}' and is skipped from readonly generation.",
        category: "SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
