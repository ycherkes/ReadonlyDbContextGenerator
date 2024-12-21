using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyDbContextGenerator.Model;

internal class EntityInfo
{
    public ITypeSymbol Type { get; set; }
    public List<IPropertySymbol> NavigationProperties { get; set; } = [];
    public string DbSetProperty { get; set; }
    public ClassDeclarationSyntax SyntaxNode { get; set; }
}