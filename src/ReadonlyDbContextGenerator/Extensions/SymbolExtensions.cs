using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadonlyDbContextGenerator.Extensions;

// Original source is https://github.com/MassTransit/MassTransit/blob/develop/src/MassTransit.Analyzers/CommonExpressions.cs
// Under Apache 2.0 license
internal static class SymbolExtensions
{
    public static bool IsImmutableArray(this ITypeSymbol type, out ITypeSymbol typeArgument)
    {
        if (type.TypeKind == TypeKind.Struct &&
            type.Name == "ImmutableArray" &&
            type.ContainingNamespace.ToString() == "System.Collections.Immutable" &&
            type is INamedTypeSymbol immutableArrayType &&
            immutableArrayType.IsGenericType &&
            immutableArrayType.TypeArguments.Length == 1)
        {
            typeArgument = immutableArrayType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    public static bool IsCollection(this ITypeSymbol type, out ITypeSymbol typeArgument)
    {
        if (type.TypeKind == TypeKind.Interface &&
            type.Name == "ICollection" &&
            type.ContainingNamespace.ToString() == "System.Collections.Generic" &&
            type is INamedTypeSymbol collectionType &&
            collectionType.IsGenericType &&
            collectionType.TypeArguments.Length == 1)
        {
            typeArgument = collectionType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    public static bool IsEnumerable(this ITypeSymbol type, out ITypeSymbol typeArgument)
    {
        if (type.TypeKind == TypeKind.Interface &&
            type.Name == "IEnumerable" &&
            type.ContainingNamespace.ToString() == "System.Collections.Generic" &&
            type is INamedTypeSymbol collectionType &&
            collectionType.IsGenericType &&
            collectionType.TypeArguments.Length == 1)
        {
            typeArgument = collectionType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    public static bool IsList(this ITypeSymbol type, out ITypeSymbol typeArgument)
    {
        if ((type.TypeKind == TypeKind.Class && type.Name == "List"
             || type.TypeKind.IsClassOrInterface() && type.Name == "IReadOnlyList"
             || type.TypeKind.IsClassOrInterface() && type.Name == "IList")
            && type.ContainingNamespace.ToString() == "System.Collections.Generic"
            && type is INamedTypeSymbol listType
            && listType.IsGenericType
            && listType.TypeArguments.Length == 1)
        {
            typeArgument = listType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    public static bool IsDictionary(this ITypeSymbol type, out ITypeSymbol keyType, out ITypeSymbol valueType)
    {
        if ((type.TypeKind == TypeKind.Class && type.Name == "Dictionary"
             || type.TypeKind.IsClassOrInterface() && type.Name == "IReadOnlyDictionary"
             || type.TypeKind.IsClassOrInterface() && type.Name == "IDictionary")
            && type.ContainingNamespace.ToString() == "System.Collections.Generic"
            && type is INamedTypeSymbol dictionaryType
            && dictionaryType.IsGenericType
            && dictionaryType.TypeArguments.Length == 2)
        {
            keyType = dictionaryType.TypeArguments[0];
            valueType = dictionaryType.TypeArguments[1];
            return true;
        }

        keyType = null;
        valueType = null;
        return false;
    }

    public static bool IsNullable(this ITypeSymbol type, out ITypeSymbol typeArgument)
    {
        if (type.TypeKind == TypeKind.Struct &&
            type.Name == "Nullable" &&
            type.ContainingNamespace.Name == "System" &&
            type is INamedTypeSymbol nullableType &&
            nullableType.IsGenericType &&
            nullableType.TypeArguments.Length == 1)
        {
            typeArgument = nullableType.TypeArguments[0];
            return true;
        }

        typeArgument = null;
        return false;
    }

    public static bool IsArray(this ITypeSymbol type, out ITypeSymbol elementType)
    {
        if (type.TypeKind == TypeKind.Array &&
            type is IArrayTypeSymbol arrayTypeSymbol)
        {
            elementType = arrayTypeSymbol.ElementType;
            return true;
        }

        elementType = null;
        return false;
    }

    public static IEnumerable<INamedTypeSymbol> GetAllInterfaces(this ITypeSymbol type)
    {
        ImmutableArray<INamedTypeSymbol> allInterfaces = type.AllInterfaces;
        if (type is INamedTypeSymbol namedType && namedType.TypeKind.IsClassOrInterface() && !allInterfaces.Contains(namedType))
        {
            var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1) { namedType };
            result.AddRange(allInterfaces);
            return result;
        }

        return allInterfaces;
    }

    public static IEnumerable<ITypeSymbol> GetAllTypes(this ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    public static bool IsClassOrInterface(this TypeKind typeKind)
    {
        return typeKind == TypeKind.Interface || typeKind == TypeKind.Class;
    }

    public static bool ImplementsInterface(this ITypeSymbol symbol, ITypeSymbol type)
    {
        return symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, type));
    }

    public static bool InheritsFromType(this ITypeSymbol symbol, ITypeSymbol type)
    {
        return symbol.GetAllTypes().Any(x => SymbolEqualityComparer.Default.Equals(x, type));
    }

    public static bool ImplementsType(this ITypeSymbol type, ITypeSymbol otherType)
    {
        IEnumerable<ITypeSymbol> types = type.GetAllTypes();
        IEnumerable<INamedTypeSymbol> interfaces = type.GetAllInterfaces();

        return types.Any(baseType => SymbolEqualityComparer.Default.Equals(baseType, otherType))
               || interfaces.Any(baseInterfaceType => SymbolEqualityComparer.Default.Equals(baseInterfaceType, otherType));
    }
}