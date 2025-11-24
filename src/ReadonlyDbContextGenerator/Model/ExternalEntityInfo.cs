using Microsoft.CodeAnalysis;

namespace ReadonlyDbContextGenerator.Model;

internal class ExternalEntityInfo
{
    public string DbSetProperty { get; set; }
    public INamedTypeSymbol TypeSymbol { get; set; }
    public Location Location { get; set; }
}
