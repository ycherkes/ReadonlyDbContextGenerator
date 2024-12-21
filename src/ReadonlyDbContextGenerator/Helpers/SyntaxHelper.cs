using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyDbContextGenerator.Helpers;

public class SyntaxHelper
{
    public static ClassDeclarationSyntax FindEntityClass(ISymbol entityType)
    {
        return entityType?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ClassDeclarationSyntax;
    }

    public static ClassDeclarationSyntax FindEntityClass(TypeSyntax entityType, Compilation compilation)
    {
        var sm = compilation.GetSemanticModel(entityType.SyntaxTree);
        var typeSymbol = sm.GetSymbolInfo(entityType).Symbol;

        return FindEntityClass(typeSymbol);
    }

    public static ClassDeclarationSyntax FindEntityClass(BaseTypeSyntax entityType, Compilation compilation)
    {
        var sm = compilation.GetSemanticModel(entityType.SyntaxTree);
        var typeSymbol = sm.GetSymbolInfo(entityType.Type).Symbol;

        return FindEntityClass(typeSymbol);
    }

    public static bool IsClassWithBaseList(SyntaxNode context)
    {
        return context is ClassDeclarationSyntax { BaseList.Types.Count: > 0 };
    }
}