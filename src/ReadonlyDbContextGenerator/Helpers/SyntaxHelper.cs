using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace ReadonlyDbContextGenerator.Helpers;

public class SyntaxHelper
{
    public static TypeDeclarationSyntax FindEntityClassOrInterface(ISymbol entityType)
    {
        var syntax = entityType?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        return syntax as TypeDeclarationSyntax;
    }

    public static TypeDeclarationSyntax MergePartialDeclarations(INamedTypeSymbol typeSymbol)
    {
        var declarations = typeSymbol?.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .ToArray();

        if (declarations is not { Length: > 0 })
        {
            return null;
        }

        if (declarations.Length == 1)
        {
            return declarations[0];
        }

        return declarations[0].WithMembers(
            SyntaxFactory.List(declarations.SelectMany(declaration => declaration.Members)));
    }

    public static TypeDeclarationSyntax FindEntityClassOrInterface(BaseTypeSyntax entityType, Compilation compilation)
    {
        var sm = compilation.GetSemanticModel(entityType.SyntaxTree);
        var typeSymbol = sm.GetSymbolInfo(entityType.Type).Symbol;

        return FindEntityClassOrInterface(typeSymbol);
    }

    public static bool IsClassWithBaseList(SyntaxNode context)
    {
        return context is ClassDeclarationSyntax { BaseList.Types.Count: > 0 };
    }
}
