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
        return syntax is ClassDeclarationSyntax or InterfaceDeclarationSyntax ? syntax as TypeDeclarationSyntax : null;
    }

    public static TypeDeclarationSyntax FindEntityClassOrInterface(TypeSyntax entityType, Compilation compilation)
    {
        var sm = compilation.GetSemanticModel(entityType.SyntaxTree);
        var typeSymbol = sm.GetSymbolInfo(entityType).Symbol;

        return FindEntityClassOrInterface(typeSymbol);
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