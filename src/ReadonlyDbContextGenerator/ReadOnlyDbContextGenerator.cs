using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadonlyDbContextGenerator.Diagnostics;
using ReadonlyDbContextGenerator.Helpers;
using ReadonlyDbContextGenerator.Model;
using System;
using System.Collections.Generic;
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

        context.RegisterSourceOutput(aggregatedInfo, static (spc, aggregated) =>
        {
            try
            {
                CodeGenerator.GenerateReadOnlyCode(spc, aggregated);
            }
            catch (Exception e)
            {
                CrashDiagnosticsReporter.Report(spc, e);
#if DEBUG
                throw;
#endif
            }
        });
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

        var externalEntities = new List<ExternalEntityInfo>();

        var entities = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(prop => prop.Type is INamedTypeSymbol { IsGenericType: true } gns && SymbolEqualityComparer.Default.Equals(gns.ConstructedFrom, compilationInfo.DbSetSymbol))
            .Select(prop =>
            {
                var entityTypeSymbol = ((INamedTypeSymbol)prop.Type).TypeArguments[0] as INamedTypeSymbol;
                if (entityTypeSymbol == null)
                {
                    externalEntities.Add(new ExternalEntityInfo
                    {
                        DbSetProperty = prop.Name,
                        Location = prop.Locations.FirstOrDefault()
                    });

                    return null;
                }

                var entitySyntax = SyntaxHelper.FindEntityClassOrInterface(entityTypeSymbol);

                if (entitySyntax == null)
                {
                    externalEntities.Add(new ExternalEntityInfo
                    {
                        DbSetProperty = prop.Name,
                        TypeSymbol = entityTypeSymbol,
                        Location = prop.Locations.FirstOrDefault()
                    });

                    return null;
                }

                var ei = ExtractEntityInfo(entityTypeSymbol, entitySyntax);
                ei.DbSetProperty = prop.Name;

                return ei;
            })
            .Where(ei => ei != null)
            .ToList();

        var ownedEntities = ExtractOwnedEntityInfos(classDecl, semanticModel, entities);
        entities.AddRange(ownedEntities);

        var entityTypes = entities.Select(e => e.Type).ToImmutableHashSet(SymbolEqualityComparer.Default);

        return new DbContextInfo
        {
            Identifier = classDecl.Identifier,
            TypeSymbol = typeSymbol,
            Namespace = typeSymbol.ContainingNamespace,
            Entities = entities,
            ExternalEntities = externalEntities,
            EntityTypes = entityTypes,
            SyntaxNode = classDecl
        };
    }

    private static List<EntityInfo> ExtractOwnedEntityInfos(ClassDeclarationSyntax dbContextClass,
        SemanticModel semanticModel,
        IReadOnlyCollection<EntityInfo> existingEntities)
    {
        var knownEntityTypes = existingEntities
            .Select(e => e.Type)
            .Where(t => t != null)
            .ToImmutableHashSet(SymbolEqualityComparer.Default);

        var ownedEntityTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var invocation in dbContextClass.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            symbol ??= semanticModel.GetSymbolInfo(invocation).CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

            if (symbol == null || !symbol.IsGenericMethod)
            {
                continue;
            }

            if (symbol.Name is not ("OwnsOne" or "OwnsMany" or "Owned"))
            {
                continue;
            }

            if (symbol.ContainingNamespace?.ToDisplayString() is not string symbolNamespace ||
                !symbolNamespace.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var typeArgument in symbol.TypeArguments.OfType<INamedTypeSymbol>())
            {
                if (knownEntityTypes.Contains(typeArgument))
                {
                    continue;
                }

                ownedEntityTypes.Add(typeArgument);
            }
        }

        var ownedEntities = new List<EntityInfo>();

        foreach (var ownedEntityType in ownedEntityTypes)
        {
            var ownedEntitySyntax = SyntaxHelper.FindEntityClassOrInterface(ownedEntityType);
            if (ownedEntitySyntax == null)
            {
                continue;
            }

            var ownedEntityInfo = ExtractEntityInfo(ownedEntityType, ownedEntitySyntax);
            if (ownedEntityInfo != null)
            {
                ownedEntities.Add(ownedEntityInfo);
            }
        }

        return ownedEntities;
    }

    private static EntityInfo ExtractEntityInfo(ITypeSymbol entityType, TypeDeclarationSyntax entitySyntax = null)
    {
        if (entityType == null)
            return null;

        var navigationProperties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(SymbolHelper.IsNavigationProperty)
            .ToList();

        var classDecl = entitySyntax ?? SyntaxHelper.FindEntityClassOrInterface(entityType);

        return new EntityInfo
        {
            Type = entityType,
            NavigationProperties = navigationProperties,
            SyntaxNode = classDecl
        };
    }
}
