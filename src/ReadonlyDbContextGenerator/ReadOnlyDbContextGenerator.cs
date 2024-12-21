using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadonlyDbContextGenerator.Helpers;
using ReadonlyDbContextGenerator.Model;

namespace ReadonlyDbContextGenerator;

[Generator]
public class ReadOnlyDbContextGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationInfoFromProvider = context.CompilationProvider
            .Select((c, _) => CompilationHelper.LoadEfCoreContext(c));

        var classesWithBaseList = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (s, _) => SyntaxHelper.IsClassWithBaseList(s),
            transform: static (generatorSyntaxContext, _) => generatorSyntaxContext
        );

        var classesWithBaseListAndCompilation = classesWithBaseList.Combine(compilationInfoFromProvider);

        var dbContextProviders = classesWithBaseListAndCompilation
            .Select((pair, _) =>
            {
                var (classDecl, compilationInfo) = pair;
                if (compilationInfo.DbContextSymbol == null)
                {
                    return (DbContextInfo: null, CompilationInfo: compilationInfo);
                }

                var dbContextInfo = ExtractDbContext(classDecl, compilationInfo);
                return (DbContextInfo: dbContextInfo, CompilationInfo: compilationInfo);
            })
            .Where(x => x.DbContextInfo != null);

        var entities = dbContextProviders
            .SelectMany((pair, _) =>
            {
                var (dbContext, compilationInfo) = pair;
                return dbContext.Entities.Select(entity => ExtractEntityInfo(entity.Type));
            })
            .Where(ei => ei != null);

        var entityConfigs = classesWithBaseListAndCompilation
            .Select(static (pair, _) =>
            {
                var (classDecl, compilationInfo) = pair;

                return compilationInfo.DbContextSymbol == null 
                    ? null 
                    : ExtractEntityConfigInfo(classDecl, compilationInfo);
            })
            .Where(eci => eci != null);

        var aggregatedInfo = dbContextProviders
            .Combine(entities.Collect())
            .Combine(entityConfigs.Collect())
            .Select(static (combined, _) =>
            {
                var ((dbContext, compilationInfo), entities) = combined.Left;

                var configs = combined.Right.GroupBy(x => x.EntityType, SymbolEqualityComparer.Default)
                    .Select(g => g.First())
                    .ToImmutableArray();

                return new AggregatedInfo(dbContext!, entities, configs, compilationInfo.Compilation);
            });

        context.RegisterSourceOutput(aggregatedInfo, CodeGenerator.GenerateReadOnlyCode);
    }

    private static EntityConfigInfo ExtractEntityConfigInfo(GeneratorSyntaxContext context, CompilationContext compilationInfo)
    {
        var semanticModel = context.SemanticModel;
        if (semanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol typeSymbol ||
            !typeSymbol.AllInterfaces.Any(interfaceSymbol => SymbolEqualityComparer.Default.Equals(interfaceSymbol.ConstructedFrom, compilationInfo.EntityConfigurationSymbol)))
        {
            return null;
        }

        var classDecl = (ClassDeclarationSyntax)context.Node;

        var entityType = typeSymbol.AllInterfaces
            .First(interfaceSymbol => SymbolEqualityComparer.Default.Equals(interfaceSymbol.ConstructedFrom, compilationInfo.EntityConfigurationSymbol))
            .TypeArguments[0];

        return new EntityConfigInfo
        {
            EntityType = entityType,
            SyntaxNode = classDecl
        };
    }

    private static DbContextInfo ExtractDbContext(GeneratorSyntaxContext context, CompilationContext compilationInfo)
    {
        var semanticModel = context.SemanticModel;
        if (semanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol typeSymbol ||
            !SymbolEqualityComparer.Default.Equals(typeSymbol.BaseType, compilationInfo.DbContextSymbol))
        {
            return null;
        }

        var classDecl = (ClassDeclarationSyntax)context.Node;

        var entities = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(prop => prop.Type is INamedTypeSymbol { IsGenericType: true } gns && SymbolEqualityComparer.Default.Equals(gns.ConstructedFrom, compilationInfo.DbSetSymbol))
            .Select(prop =>
            {
                var entityType = ((INamedTypeSymbol)prop.Type).TypeArguments[0];
                return new EntityInfo
                {
                    Type = entityType,
                    DbSetProperty = prop.Name
                };
            })
            .ToList();

        var entityTypes = entities.Select(e => e.Type).ToImmutableHashSet(SymbolEqualityComparer.Default);

        return new DbContextInfo
        {
            Identifier = classDecl.Identifier,
            TypeSymbol = typeSymbol,
            Namespace = typeSymbol.ContainingNamespace,
            Entities = entities,
            EntityTypes = entityTypes,
            SyntaxNode = classDecl
        };
    }

    private static EntityInfo ExtractEntityInfo(ITypeSymbol entityType)
    {
        if (entityType == null)
            return null;

        var navigationProperties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(SymbolHelper.IsNavigationProperty)
            .ToList();

        var classDecl = SyntaxHelper.FindEntityClass(entityType);

        return new EntityInfo
        {
            Type = entityType,
            NavigationProperties = navigationProperties,
            SyntaxNode = classDecl
        };
    }
}