using Microsoft.CodeAnalysis;
using ReadonlyDbContextGenerator.Extensions;
using System.Linq;

namespace ReadonlyDbContextGenerator.Helpers;

public class SymbolHelper
{
    public static bool IsNavigationProperty(IPropertySymbol prop)
    {
        return IsNavigationType(prop.Type);
    }

    public static bool IsNavigationType(ISymbol type)
    {
        return type is INamedTypeSymbol { IsValueType: false, SpecialType: SpecialType.None };
    }

    public static bool IsCollection(ITypeSymbol typeSymbol)
    {
        return typeSymbol.IsImmutableArray(out _)
               || typeSymbol.IsList(out _)
               || typeSymbol.IsArray(out _)
               || typeSymbol.IsCollection(out _)
               || typeSymbol.IsEnumerable(out _)
               || typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType
                   && namedType.AllInterfaces.Any(interfaceType =>
                       interfaceType.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                       && interfaceType.TypeArguments.Length == 1);
    }
}
