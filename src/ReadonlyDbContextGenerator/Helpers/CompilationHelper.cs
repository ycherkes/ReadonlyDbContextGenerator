using Microsoft.CodeAnalysis;
using ReadonlyDbContextGenerator.Model;

namespace ReadonlyDbContextGenerator.Helpers;

internal static class CompilationHelper
{
    public static CompilationContext LoadEfCoreContext(Compilation compilation)
    {
        return new CompilationContext
        {
            Compilation = compilation,
            DbContextSymbol = TryLoadSymbol(compilation, "Microsoft.EntityFrameworkCore.DbContext"),
            EntityConfigurationSymbol = TryLoadSymbol(compilation, "Microsoft.EntityFrameworkCore.IEntityTypeConfiguration`1"),
            DbSetSymbol = TryLoadSymbol(compilation, "Microsoft.EntityFrameworkCore.DbSet`1")
        };
    }

    private static INamedTypeSymbol TryLoadSymbol(Compilation compilation, string symbolName)
    {
        return compilation.GetTypeByMetadataName(symbolName)?.OriginalDefinition;
    }
}