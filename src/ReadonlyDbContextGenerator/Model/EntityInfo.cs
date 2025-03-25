using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ReadonlyDbContextGenerator.Model;

internal class EntityInfo
{
    public ITypeSymbol Type { get; set; }
    public List<IPropertySymbol> NavigationProperties { get; set; } = [];
    public string DbSetProperty { get; set; }
    public TypeDeclarationSyntax SyntaxNode { get; set; }
}