using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadonlyDbContextGenerator.Helpers;
using ReadonlyDbContextGenerator.Model;

namespace ReadonlyDbContextGenerator;

public class CodeGenerator
{
    private static MemberDeclarationSyntax[] _readonlyDbContextMethods;

    internal static void GenerateReadOnlyCode(SourceProductionContext context, AggregatedInfo info)
    {
        var commonNamespace = GetCommonRootNamespace(info);

        var types = new HashSet<string>(GenerateReadOnlyEntities(context, info, commonNamespace));

        foreach (var config in info.Configurations)
        {
            var configSyntax = ModifyEntityConfigSyntax(info.DbContext, config.SyntaxNode!, info.Compilation, commonNamespace);
            var readonlyEntityConfigTypeName = GetReadonlyTypeName(config.EntityType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            context.AddSource($"{readonlyEntityConfigTypeName}Configuration.g.cs", configSyntax);
            types.Add(config.SyntaxNode.Identifier.Text);
        }

        types.Add(info.DbContext.Identifier.ToString());

        var readOnlyDbContextCode = ModifyDbContextSyntax(info.DbContext, info.DbContext.SyntaxNode!, info.Compilation, commonNamespace, types.ToImmutableHashSet());
        var readonlyDbContextFileName = GetReadonlyTypeName(info.DbContext.Identifier.Text);
        context.AddSource($"{readonlyDbContextFileName}.g.cs", readOnlyDbContextCode.NormalizeWhitespace().ToFullString());

        var readOnlyInterfaceCode = GenerateReadOnlyDbContextInterface(readOnlyDbContextCode, info.DbContext, commonNamespace);
        context.AddSource($"I{readonlyDbContextFileName}.g.cs", readOnlyInterfaceCode);
    }

    private static string GetCommonRootNamespace(AggregatedInfo info)
    {
        var allNamespaces = new List<string>
        {
            info.DbContext.Namespace?.ToString()
        };

        allNamespaces.AddRange(info.Entities.Select(e => GetNamespace(e.SyntaxNode)?.Name.ToString()));
        allNamespaces.AddRange(info.Configurations.Select(c => GetNamespace(c.SyntaxNode)?.Name.ToString()));
        var distinctNamespaces = allNamespaces.Where(n => n != null).Distinct().ToArray();

        if (!distinctNamespaces.Any())
            return "Generated";

        var splitNamespaces = distinctNamespaces
            .Select(ns => ns.Split('.'))
            .ToArray();

        var minLength = splitNamespaces.Min(parts => parts.Length);
        var commonRoot = new List<string>();

        for (var i = 0; i < minLength; i++)
        {
            var part = splitNamespaces[0][i];
            if (splitNamespaces.All(parts => parts[i] == part))
                commonRoot.Add(part);
            else
                break;
        }

        var prefix = string.Join(".", commonRoot);

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return "Generated";
        }

        return prefix + ".Generated";
    }

    private static string[] GenerateReadOnlyEntities(SourceProductionContext context, AggregatedInfo info, string commonNamespace)
    {
        var processedEntities = new HashSet<string>();
        var additionalEntitiesToProcess = new List<EntityInfo>();

        foreach (var entity in info.Entities)
        {
            if (processedEntities.Contains(entity.SyntaxNode.Identifier.Text))
            {
                continue;
            }

            var readOnlyEntityCode = ModifyEntitySyntax(entity, entity.SyntaxNode!, processedEntities, info.Entities, additionalEntitiesToProcess, info.Compilation, commonNamespace);
            var readonlyFileName = GetReadonlyTypeName(entity.SyntaxNode.Identifier.Text);
            context.AddSource($"{readonlyFileName}.g.cs", readOnlyEntityCode);
        }

        while (additionalEntitiesToProcess.Any())
        {
            var toProcess = additionalEntitiesToProcess.ToList();
            additionalEntitiesToProcess.Clear();

            foreach (var entity in toProcess)
            {
                if (!processedEntities.Contains(entity.SyntaxNode.Identifier.Text))
                {
                    var readOnlyEntityCode = ModifyEntitySyntax(entity, entity.SyntaxNode!, processedEntities, info.Entities, additionalEntitiesToProcess, info.Compilation, commonNamespace);
                    var readonlyFileName = GetReadonlyTypeName(entity.SyntaxNode.Identifier.Text);
                    context.AddSource($"{readonlyFileName}.g.cs", readOnlyEntityCode);
                }
            }
        }

        return processedEntities.ToArray();
    }

    private static string ModifyEntitySyntax(EntityInfo entity,
        ClassDeclarationSyntax entitySyntax,
        HashSet<string> processedEntities,
        ImmutableArray<EntityInfo> allEntities,
        List<EntityInfo> additionalEntitiesToProcess,
        Compilation compilation, 
        string commonNamespace)
    {
        
        processedEntities.Add(entity.SyntaxNode.Identifier.Text);
        var sm = compilation.GetSemanticModel(entitySyntax.SyntaxTree);
        
        // Convert properties to init-only and navigation properties to IReadOnlyCollection<ReadOnlyEntity>
        var modifiedMembers = entitySyntax.Members
            .Select(member =>
            {
                if (member is PropertyDeclarationSyntax prop)
                {
                    // Handle navigation properties
                    if (entity.NavigationProperties?.Any(p => p.Name == prop.Identifier.Text) == true)
                    {
                        var navigationType = GetGenericType(prop.Type);

                        var readOnlyNavigationType = GetReadonlyTypeName(navigationType.ToString());

                        // If the navigation target is another entity, ensure it's added to the additional processing list
                        if (!processedEntities.Contains(navigationType.ToString()) && !allEntities.Any(x => x.SyntaxNode.Identifier.Text == navigationType.ToString()))
                        {
                            // Dynamically find the entity class across all syntax trees
                            var referencedEntityClass = SyntaxHelper.FindEntityClass(navigationType, compilation);
                            if (referencedEntityClass != null)
                            {
                                var referencedEntityInfo = CreateEntityInfoFromSyntaxTree(referencedEntityClass, compilation);
                                if (!allEntities.Contains(referencedEntityInfo))
                                    additionalEntitiesToProcess.Add(referencedEntityInfo);
                            }
                        }

                        if (prop.Type is GenericNameSyntax)
                        {
                            var type = sm.GetTypeInfo(prop.Type);
                            if (SymbolHelper.IsCollection(type.Type?.OriginalDefinition))
                            {
                                prop = prop.WithType(
                                    SyntaxFactory.ParseTypeName($"IReadOnlyCollection<{readOnlyNavigationType}>"));
                            }
                        }
                        else
                        {
                            prop = prop.WithType(SyntaxFactory.ParseTypeName(readOnlyNavigationType));
                        }
                    }

                    // Handle scalar properties and make them init-only
                    var setAccessor = prop.AccessorList?.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);

                    if (setAccessor != null)
                    {
                        var initAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                            .WithModifiers(setAccessor.Modifiers)
                            .WithBody(setAccessor.Body)
                            .WithExpressionBody(setAccessor.ExpressionBody)
                            .WithSemicolonToken(setAccessor.SemicolonToken);

                        var accessorsWithoutSet = prop.AccessorList.Accessors
                            .Where(a => a.Kind() != SyntaxKind.SetAccessorDeclaration)
                            .Concat([initAccessor]);

                        return prop.WithAccessorList(
                            SyntaxFactory.AccessorList(
                                SyntaxFactory.List(accessorsWithoutSet)));
                    }
                }

                return member; // Leave unchanged if not a property
            });

        // Remove methods from the entity (DDD approach often use them in domain rich model)
        modifiedMembers = modifiedMembers.Where(m => m is not MethodDeclarationSyntax);

        // Update the class name to append "ReadOnly"
        var readonlyClassName = GetReadonlyTypeName(entity.SyntaxNode.Identifier.Text);
        var newIdentifier = SyntaxFactory.Identifier(readonlyClassName);

        // Create a new class declaration with the updated name and members
        var newEntitySyntax = entitySyntax
            .WithIdentifier(newIdentifier)
            .WithMembers(SyntaxFactory.List(modifiedMembers));

        var baseList = entitySyntax.BaseList ?? SyntaxFactory.BaseList();

        if (baseList.Types.Count > 0)
        {
            foreach (var type in baseList.Types.ToArray())
            {
                var typeInfo = sm.GetTypeInfo(type.Type);
                var existingAdditionalEntity = additionalEntitiesToProcess.FirstOrDefault(ae => SymbolEqualityComparer.Default.Equals(ae.Type, typeInfo.Type));

                if (existingAdditionalEntity == null)
                {
                    
                    var referencedEntityClass = SyntaxHelper.FindEntityClass(type, compilation);
                    if (referencedEntityClass == null) continue;

                    var referencedEntityInfo = CreateEntityInfoFromSyntaxTree(referencedEntityClass, compilation);
                    additionalEntitiesToProcess.Add(referencedEntityInfo);
                }

                baseList = baseList.RemoveNode(type, SyntaxRemoveOptions.KeepNoTrivia)!;
                var readonlyName = GetReadonlyTypeName(type.ToString());
                baseList = baseList.AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(readonlyName)));
            }

            newEntitySyntax = newEntitySyntax.WithBaseList(baseList);
        }

        string[] requiredUsings = ["System", "System.Collections.Generic"];
        var combinedUsings = CombineUsings(entitySyntax, requiredUsings);

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(commonNamespace))
            .AddMembers(newEntitySyntax);

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(combinedUsings.ToArray())
            .AddMembers(namespaceDeclaration);

        // Return the modified entity code
        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    private static List<UsingDirectiveSyntax> CombineUsings(SyntaxNode entitySyntax, string[] requiredUsings)
    {
        var oldUsings = GetCompilationUnit(entitySyntax).Usings;
        var entityNameSpace = GetNamespace(entitySyntax)?.Name.ToString();

        var usings = entityNameSpace == null 
            ? requiredUsings 
            : requiredUsings.Concat([entityNameSpace]);
        
        var missingUsings = usings
            .Where(u => oldUsings.All(existing => existing.Name != null && existing.Name.ToString() != u))
            .Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u)))
            .ToList();

        missingUsings.AddRange(oldUsings);
        missingUsings.Sort((a, b) => string.Compare(a.Name?.ToString(), b.Name?.ToString(), StringComparison.Ordinal));

        return missingUsings;
    }

    private static BaseNamespaceDeclarationSyntax GetNamespace(SyntaxNode node)
    {
        var currentNode = node;

        while (currentNode != null)
        {
            switch (currentNode)
            {
                case NamespaceDeclarationSyntax namespaceDeclaration:
                    return namespaceDeclaration;
                case FileScopedNamespaceDeclarationSyntax fileScopedNamespace:
                    return fileScopedNamespace;
                default:
                    currentNode = currentNode.Parent;
                    break;
            }
        }

        return null; // No namespace found
    }

    private static CompilationUnitSyntax GetCompilationUnit(SyntaxNode node)
    {
        SyntaxNode currentNode = node;

        while (currentNode != null)
        {
            if (currentNode is CompilationUnitSyntax compilationUnit)
            {
                return compilationUnit;
            }

            currentNode = currentNode.Parent;
        }

        return null;
    }

    public class TypeReferenceRewriter(ImmutableHashSet<string> typeNames, SemanticModel model) : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Check if the identifier matches the old type name
            if (typeNames.Contains(node.Identifier.Text))
            {
                if (model.GetSymbolInfo(node).Symbol is not INamedTypeSymbol)
                    return node;

                // Replace with the new type name
                var readonlyName = GetReadonlyTypeName(node.Identifier.Text);
                return SyntaxFactory.IdentifierName(readonlyName)
                    .WithTriviaFrom(node); // Preserve the trivia (whitespace/comments)
            }

            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (!typeNames.Contains(node.Name.Identifier.Text))
            {
                return base.VisitMemberAccessExpression(node);
            }

            if (model.GetSymbolInfo(node.Name).Symbol is not INamedTypeSymbol)
            {
                return node;
            }

            var readonlyName = GetReadonlyTypeName(node.Name.Identifier.Text);

            return SyntaxFactory.IdentifierName(readonlyName)
                .WithTriviaFrom(node);
        }

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (!typeNames.Contains(node.Right.Identifier.Text))
            {
                return base.VisitQualifiedName(node);
            }

            var readonlyName = GetReadonlyTypeName(node.Right.Identifier.Text);

            return SyntaxFactory.IdentifierName(readonlyName)
                .WithTriviaFrom(node);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (!typeNames.Contains(node.Identifier.Text))
            {
                return base.VisitConstructorDeclaration(node);
            }

            var readonlyName = GetReadonlyTypeName(node.Identifier.Text);

            node = node.WithIdentifier(SyntaxFactory.Identifier(readonlyName)
                .WithTriviaFrom(node.Identifier));

            return base.VisitConstructorDeclaration(node);
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            

            // Check if the generic type matches the old class name
            var updatedArguments = node.TypeArgumentList.Arguments
                .Select(arg =>
                {
                    if (arg is IdentifierNameSyntax identifierName)
                    {
                        if (!typeNames.Contains(identifierName.Identifier.Text))
                        {
                            return arg;
                        }

                        var readonlyName = GetReadonlyTypeName(identifierName.Identifier.Text);
                        return SyntaxFactory.IdentifierName(readonlyName).WithTriviaFrom(arg);
                    }
                    return arg;
                })
                .ToArray();

            // Return the updated generic name
            if (!updatedArguments.SequenceEqual(node.TypeArgumentList.Arguments))
            {
                return node.WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(updatedArguments)))
                    .WithTriviaFrom(node);
            }

            return base.VisitGenericName(node);
        }
    }

    private static string GetReadonlyTypeName(string typeName)
    {
        return $"ReadOnly{typeName}";
    }

    private static TypeSyntax GetGenericType(TypeSyntax type)
    {
        return type is GenericNameSyntax gns ? gns.TypeArgumentList.Arguments[0] : type;
    }

    private static EntityInfo CreateEntityInfoFromSyntaxTree(ClassDeclarationSyntax entityClass, Compilation compilation)
    {
        var model = compilation.GetSemanticModel(entityClass.SyntaxTree);
        var type = model.GetDeclaredSymbol(entityClass)?.OriginalDefinition;

        var navigationProperties = type?.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(SymbolHelper.IsNavigationProperty)
            .ToList();

        return new EntityInfo
        {
            Type = type,
            NavigationProperties = navigationProperties,
            SyntaxNode = entityClass
        };
    }

    private static CompilationUnitSyntax ModifyDbContextSyntax(DbContextInfo dbContext,
        ClassDeclarationSyntax dbContextSyntax, Compilation compilation, string commonNamespace,
        ImmutableHashSet<string> types)
    {
        // Add the IReadOnlyDbContext interface to the BaseList
        var baseList = dbContextSyntax.BaseList ?? SyntaxFactory.BaseList();
        var readonlyDbContextIdentifier = GetReadonlyTypeName(dbContext.Identifier.Text);
        var newBaseList = baseList.AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"I{readonlyDbContextIdentifier}")));
        
        var sm = compilation.GetSemanticModel(dbContextSyntax.SyntaxTree);

        var typeReferenceRewriter = new TypeReferenceRewriter(types, sm);
        
        var newDbContextSyntax = (ClassDeclarationSyntax)typeReferenceRewriter.Visit(dbContextSyntax);

        var withoutSaveMethods = newDbContextSyntax.Members
            .Where(member => member is not MethodDeclarationSyntax { Identifier.Text: "SaveChanges" or "SaveChangesAsync"});

        var newMethods = GetReadonlyDbContextMethods();

        var allMembers = withoutSaveMethods.Concat(newMethods).ToArray();

        var newIdentifier = SyntaxFactory.Identifier(readonlyDbContextIdentifier);

        newDbContextSyntax = AddPartialKeyword(newDbContextSyntax)
            .WithIdentifier(newIdentifier)
            .WithMembers(SyntaxFactory.List(allMembers))
            .WithBaseList(newBaseList);

        string[] requiredUsings = ["Microsoft.EntityFrameworkCore", "System.Threading", "System.Threading.Tasks", "System"];
        var combinedUsings = CombineUsings(dbContextSyntax, requiredUsings).ToArray();

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(commonNamespace))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            .AddMembers(newDbContextSyntax)
            .NormalizeWhitespace();

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(combinedUsings)
            .AddMembers(namespaceDeclaration);

        // Return the modified DbContext code and the DbSet property strings
        return compilationUnit;
    }

    private static ClassDeclarationSyntax AddPartialKeyword(ClassDeclarationSyntax classDeclaration)
    {
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            // Add the partial modifier
            classDeclaration = classDeclaration
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
        }

        return classDeclaration;
    }

    public static IEnumerable<MemberDeclarationSyntax> GetReadonlyDbContextMethods()
    {
        if (_readonlyDbContextMethods != null)
        {
            return _readonlyDbContextMethods;
        }

        // Define the methods
        var methods = new[]
        {
            @"public sealed override int SaveChanges()
              {
                  throw new NotImplementedException(""Do not call SaveChanges on a readonly db context."");
              }",
            @"public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
              {
                  throw new NotImplementedException(""Do not call SaveChangesAsync on a readonly db context."");
              }",
            @"public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
              {
                  throw new NotImplementedException(""Do not call SaveChangesAsync on a readonly db context."");
              }"
        };

        return _readonlyDbContextMethods = methods.Select(m => SyntaxFactory.ParseMemberDeclaration(m)?.NormalizeWhitespace()).ToArray();
    }

    private static string GenerateReadOnlyDbContextInterface(CompilationUnitSyntax dbContextInfo,
        DbContextInfo dbContext, string commonNamespace)
    {
        var classDeclaration = dbContextInfo.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        var interfaceName = $"I{classDeclaration.Identifier.Text}";

        var interfaceMembers = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(prop => dbContext.Entities.Any(e => e.DbSetProperty == prop.Identifier.Text))
            .Select(property =>
            {
                // Create a read-only version of the property
                var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                var accessorList = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getAccessor));

                return property
                    .WithAccessorList(accessorList)
                    .WithModifiers(SyntaxFactory.TokenList()); // Remove modifiers like `public`
            })
            .Cast<MemberDeclarationSyntax>()
            .ToList();

        const string setMethodText = "DbSet<TEntity> Set<TEntity>() where TEntity : class;";
        var setMethod = SyntaxFactory.ParseMemberDeclaration(setMethodText)!.NormalizeWhitespace();
        interfaceMembers.Add(setMethod);

        var newBaseList = SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(
        [
            (BaseTypeSyntax)SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IDisposable"))!,
            SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IAsyncDisposable"))!
        ]));

        // Create the interface declaration
        var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(interfaceName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            .WithBaseList(newBaseList)
            .AddMembers(interfaceMembers.ToArray());

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(commonNamespace))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            .AddMembers(interfaceDeclaration)
            .NormalizeWhitespace();

        var combinedUsings = CombineUsings(dbContext.SyntaxNode, ["Microsoft.EntityFrameworkCore", "System"]);

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(combinedUsings.ToArray())
            .AddMembers(namespaceDeclaration);

        // Convert the syntax tree to a string
        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    private static string ModifyEntityConfigSyntax(DbContextInfo dbContext,
        ClassDeclarationSyntax configSyntax, Compilation compilation, string commonNamespace)
    {
        var readonlyEntityConfigTypeName = GetReadonlyTypeName(configSyntax.Identifier.Text);
        var newIdentifier = SyntaxFactory.Identifier(readonlyEntityConfigTypeName);
        var sm = compilation.GetSemanticModel(configSyntax.SyntaxTree);
        var entityTypes = dbContext.Entities.Select(e => e.Type)
            .Concat([dbContext.TypeSymbol])
            .Select(x => x.Name)
            .ToImmutableHashSet();

        var typeReferenceRewriter = new TypeReferenceRewriter(entityTypes, sm);
        
        var newConfigSyntax = (ClassDeclarationSyntax)typeReferenceRewriter.Visit(configSyntax);

        // Update class declaration with new base type
        newConfigSyntax = newConfigSyntax.WithIdentifier(newIdentifier);

        var combinedUsings = CombineUsings(configSyntax, []);

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(commonNamespace))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            .AddMembers(newConfigSyntax)
            .NormalizeWhitespace();

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(combinedUsings.ToArray())
            .AddMembers(namespaceDeclaration);

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }
}