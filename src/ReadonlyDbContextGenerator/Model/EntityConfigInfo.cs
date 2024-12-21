using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyDbContextGenerator.Model;

internal class EntityConfigInfo
{
    public ITypeSymbol EntityType { get; set; }
    public ClassDeclarationSyntax SyntaxNode { get; set; }
}