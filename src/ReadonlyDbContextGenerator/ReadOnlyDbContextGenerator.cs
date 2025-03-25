using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadonlyDbContextGenerator.Helpers;
using ReadonlyDbContextGenerator.Model;
using System.Collections.Immutable;
using System.Linq;

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

        var entityConfigs = classesWithBaseListAndCompilation
            .Select(static (pair, _) =>
            {
                var (classDecl, compilationInfo) = pair;

                return compilationInfo.DbContextSymbol == null
                    ? null
                    : ExtractEntityConfigInfo(classDecl, compilationInfo);
            })
            .Where(eci => eci != null);

        var aggregatedInfo = dbContextProviders.Collect()
            .Combine(entityConfigs.Collect())
            .Select(static (combined, _) =>
            {
                var dbContexts = combined.Left.Select(x => x.DbContextInfo).ToImmutableArray();

                var compilationInfo = combined.Left.Select(x => x.CompilationInfo).FirstOrDefault();

                var configs = combined.Right.GroupBy(x => x.EntityType, SymbolEqualityComparer.Default)
                    .Select(g => g.First())
                    .ToImmutableArray();

                var aggregatedEntities = dbContexts.SelectMany(db => db.Entities).ToImmutableArray();

                return new AggregatedInfo(dbContexts!, configs, aggregatedEntities, compilationInfo!.Compilation);
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
                var ei = ExtractEntityInfo(entityType);

                if (ei != null)
                {
                    ei.DbSetProperty = prop.Name;
                }

                return ei;
                //return new EntityInfo
                //{
                //    Type = entityType,
                //    DbSetProperty = prop.Name
                //};
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

        var classDecl = SyntaxHelper.FindEntityClassOrInterface(entityType);

        return new EntityInfo
        {
            Type = entityType,
            NavigationProperties = navigationProperties,
            SyntaxNode = classDecl
        };
    }
}