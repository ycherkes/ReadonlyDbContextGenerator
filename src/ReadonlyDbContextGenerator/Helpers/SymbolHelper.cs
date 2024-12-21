﻿using Microsoft.CodeAnalysis;
using ReadonlyDbContextGenerator.Extensions;

namespace ReadonlyDbContextGenerator.Helpers;

public class SymbolHelper
{
    public static bool IsNavigationProperty(IPropertySymbol prop)
    {
        return prop.Type is INamedTypeSymbol { IsValueType: false, SpecialType: SpecialType.None };
    }

    public static bool IsCollection(ITypeSymbol typeSymbol)
    {
        return typeSymbol.IsImmutableArray(out _)
               || typeSymbol.IsList(out _)
               || typeSymbol.IsArray(out _)
               || typeSymbol.IsCollection(out _)
               || typeSymbol.IsEnumerable(out _);
    }
}