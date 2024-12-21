using Microsoft.CodeAnalysis;

namespace ReadonlyDbContextGenerator.Model;

internal class CompilationContext
{
    public INamedTypeSymbol DbContextSymbol { get; set; }
    public INamedTypeSymbol EntityConfigurationSymbol { get; set; }
    public Compilation Compilation { get; set; }
    public INamedTypeSymbol DbSetSymbol { get; set; }
}