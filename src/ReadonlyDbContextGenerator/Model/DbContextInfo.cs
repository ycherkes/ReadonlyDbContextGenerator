using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyDbContextGenerator.Model;

internal class DbContextInfo
{
    public SyntaxToken Identifier { get; set; }
    public INamespaceSymbol Namespace { get; set; }
    public List<EntityInfo> Entities { get; set; } = [];
    public ClassDeclarationSyntax SyntaxNode { get; set; }
    public INamedTypeSymbol TypeSymbol { get; set; }
    public ImmutableHashSet<ISymbol> EntityTypes { get; set; }
}