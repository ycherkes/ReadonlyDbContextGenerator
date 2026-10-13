using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using VerifyCS = UnitTests.CSharpSourceGeneratorVerifier<ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator>;


namespace UnitTests;

public class ReadonlyDbContextGeneratorTests
{
    [Theory]
    [InlineData("new OwnedDocument", false)]
    [InlineData("new", false)]
    [InlineData("new OwnedDocument", true)]
    [InlineData("new", true)]
    public void RewritesOwnedEntityTypesInNestedPropertyInitializers(string creation, bool separateConfiguration)
    {
        var configuration = separateConfiguration
            ? """
                public class RootEntityConfiguration : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<RootEntity>
                {
                    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<RootEntity> builder)
                    {
                        builder.OwnsOne(entity => entity.Document, document => document.OwnsOne(value => value.Nested));
                        builder.Ignore(s => s.ExplicitDefault);
                        builder.Ignore(s => s.Label);
                    }
                }
                """
            : "";
        var modelBody = separateConfiguration
            ? "builder.ApplyConfiguration(new RootEntityConfiguration());"
            : """
                builder.Entity<RootEntity>().OwnsOne(entity => entity.Document,
                    document => document.OwnsOne(value => value.Nested));
                builder.Entity<RootEntity>().Ignore(entity => entity.ExplicitDefault);
                builder.Entity<RootEntity>().Ignore(entity => entity.Label);
                """;
        var source = /* lang=c#-test */ $$"""
            using Microsoft.EntityFrameworkCore;
            using System.Collections.Generic;
            using System.Linq;
            using static System.Array;
            using NestedAlias = Sample.NestedValue;
            using EntityAlias = Sample.RootEntity;

            namespace Sample
            {
                public class RootEntity : ExternalBase
                {
                    public int Id { get; set; }
                    public OwnedDocument Document { get; set; } = {{creation}}()
                    {
                        Nested = new NestedAlias { Enabled = true }
                    };
                    public OwnedDocument ExplicitDefault { get; set; } = new Sample.OwnedDocument
                    {
                        Nested = new Sample.NestedValue()
                    };
                    public string Label { get; set; } = new string('x', 2);
                    public string MethodLabel { get; set; } = RootEntity.CreateLabel();
                    public string QualifiedLabel { get; set; } = Sample.RootEntity.CreateLabel();
                    public string AliasedLabel { get; set; } = EntityAlias.CreateLabel<string>();
                    public ScalarValue Value { get; set; } = new ScalarValue();
                    public ScalarValue? OptionalValue { get; set; } = new Sample.ScalarValue();
                    public ScalarRecord RecordValue { get; set; } = new ScalarRecord();
                    public ICollection<NestedValue> Children { get; set; } = new NestedValue[0];
                    public ICollection<NestedValue> QualifiedChildren { get; set; } = new Sample.NestedValue[RootEntity.GetArrayLength()];
                    public ICollection<NestedValue> AliasedChildren { get; set; } = new NestedAlias[] { new NestedAlias { Enabled = true } };
                    public int[] Numbers { get; set; } = new int[RootEntity.GetArrayLength()];
                    public NestedValue DefaultChild { get; set; } = default(NestedValue);
                    public NestedValue CastChild { get; set; } = (Sample.NestedValue)RootEntity.GetObject();
                    public NestedValue AsChild { get; set; } = RootEntity.GetObject() as NestedAlias;
                    public bool IsChild { get; set; } = RootEntity.GetObject() is NestedAlias;
                    public ScalarValue DefaultValue { get; set; } = default(ScalarValue);
                    public ICollection<NestedValue> EmptyChildren { get; set; } = System.Array.Empty<NestedValue>();
                    public IEnumerable<NestedValue> EnumerableChildren { get; set; } = Enumerable.Empty<Sample.NestedValue>().ToArray();
                    public ICollection<NestedValue> UnqualifiedChildren { get; set; } = Empty<NestedAlias>();
                    public ICollection<NestedValue> FactoryChildren { get; set; } = EntityAlias.CreateArray<NestedAlias>();
                    public ScalarValue[] EmptyValues { get; set; } = System.Array.Empty<ScalarValue>();
                    public NestedValue PatternChild { get; set; } = RootEntity.GetObject() is NestedValue value ? value : new NestedValue();
                    public NestedValue SwitchChild { get; set; } = RootEntity.GetObject() switch
                    {
                        Sample.NestedValue value => value,
                        _ => new NestedValue()
                    };
                    public NestedValue RecursiveChild { get; set; } = RootEntity.GetObject() is NestedAlias { Enabled: true } value ? value : new NestedValue();
                    public bool TypePattern { get; set; } = RootEntity.GetObject() switch { NestedAlias => true, _ => false };
                    public bool ScalarPattern { get; set; } = RootEntity.GetObject() is ScalarValue value && value.Number == 0;
                    public System.Func<object> LocalFactory { get; set; } = () =>
                    {
                        Sample.NestedValue value = new NestedAlias();
                        foreach (NestedAlias child in new NestedValue[] { value })
                            return child;
                        return value;
                    };
                    public System.Func<object> QueryFactory { get; set; } = () =>
                        (from NestedAlias child in new NestedValue[] { new NestedValue() }
                         join Sample.NestedValue other in new NestedValue[] { new NestedValue() }
                             on child.Enabled equals other.Enabled
                         select child).ToArray();
                    public NestedValue StaticChild { get; set; } = NestedValue.Default;
                    public NestedValue QualifiedStaticChild { get; set; } = Sample.NestedValue.Default;
                    public NestedValue AliasedStaticChild { get; set; } = NestedAlias.Default;
                    public System.Type NavigationType { get; set; } = typeof(NestedAlias);
                    public System.Type QualifiedNavigationType { get; set; } = typeof(Sample.NestedValue);
                    public System.Type ScalarType { get; set; } = typeof(ScalarValue);
                    public string ExternalLabel { get; set; } = RootEntity.DefaultLabel;
                    public string InheritedLabel { get; set; } = NestedValue.SourceLabel;
                    public NestedValue LambdaChild { get; set; } =
                        new System.Func<NestedValue, NestedValue>((NestedAlias value) => value)(new NestedValue());
                    public NestedValue AnonymousChild { get; set; } =
                        new System.Func<NestedValue, NestedValue>(delegate(Sample.NestedValue value) { return value; })(new NestedValue());
                    public NestedValue LocalFunctionChild { get; set; } = new System.Func<NestedValue>(() =>
                    {
                        NestedAlias Make(Sample.NestedValue value) => value ?? new NestedValue();
                        return Make(new NestedValue());
                    })();
                    public string EntityName { get; set; } = nameof(NestedAlias);
                    public string QualifiedEntityName { get; set; } = nameof(Sample.NestedValue);
                    public string ScalarName { get; set; } = nameof(ScalarValue);
                    public string MethodName { get; set; } = nameof(RootEntity.GetObject);
                    public enum State { Active }
                    public State Status { get; set; } = RootEntity.State.Active;
                    public State QualifiedStatus { get; set; } = Sample.RootEntity.State.Active;
                    public State AliasedStatus { get; set; } = EntityAlias.State.Active;
                    public NestedValue TypedLambdaChild { get; set; } =
                        new System.Func<NestedValue>(Sample.NestedValue () => new NestedAlias())();
                    public NestedValue OutChild { get; set; } = new System.Func<NestedValue>(() =>
                    {
                        bool Try(out NestedValue value) { value = new NestedValue(); return true; }
                        Try(out NestedAlias result);
                        (NestedValue first, NestedAlias second) = (result, new NestedValue());
                        return first;
                    })();
                    public NestedValue ConstrainedChild { get; set; } = new System.Func<NestedValue>(() =>
                    {
                        T Make<T>() where T : NestedAlias, new() => new T();
                        return Make<NestedValue>();
                    })();
                    public static T[] CreateArray<T>() => System.Array.Empty<T>();
                    public static object GetObject() => null;
                    public static int GetArrayLength() => 0;
                    public static string CreateLabel() => "label";
                    public static string CreateLabel<T>() => typeof(T).Name;
                }

                public class OwnedDocument
                {
                    public NestedValue Nested { get; set; }
                    private NestedValue _lazyValue;
                    public NestedValue LazyValue
                    {
                        get => _lazyValue ??= new NestedAlias();
                        set => _lazyValue = value;
                    }
                    private NestedValue _explicitValue = new NestedAlias();
                    public NestedValue ExplicitValue => _explicitValue;
                }

                public class SourceBase
                {
                    public static string SourceLabel { get; } = "source";
                }

                public class NestedValue : SourceBase
                {
                    public bool Enabled { get; set; }
                    public static NestedValue Default { get; } = new NestedValue();
                }

                public struct ScalarValue
                {
                    public int Number { get; set; }
                }

                public record struct ScalarRecord(int Number);

                public class ApplicationContext : DbContext
                {
                    public DbSet<RootEntity> Entities { get; set; }
                    protected override void OnModelCreating(ModelBuilder builder)
                    {
                        {{modelBody}}
                    }
                }
                {{configuration}}
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Concat([MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)]);
        var externalCompilation = CSharpCompilation.Create("ExternalInitializerBase",
            [CSharpSyntaxTree.ParseText(/* lang=c#-test */ """
                public class ExternalBase
                {
                    public static string DefaultLabel { get; } = "external";
                }
                """)], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var externalAssembly = new MemoryStream();
        var externalResult = externalCompilation.Emit(externalAssembly);
        Assert.True(externalResult.Success, string.Join(Environment.NewLine, externalResult.Diagnostics));
        references = references.Append(MetadataReference.CreateFromImage(externalAssembly.ToArray()));
        var compilation = CSharpCompilation.Create("OwnedInitializerRegression",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(driver.GetRunResult().Results.Single().GeneratedSources,
            result => result.HintName == "ReadOnlyRootEntity.g.cs").SourceText.ToString();
        Assert.Contains("new ReadOnlyNestedValue", generated);
        Assert.Contains("new ReadOnlyOwnedDocument", generated);
        var scalarInitializer = CSharpSyntaxTree.ParseText(generated).GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
            .Single(property => property.Identifier.ValueText == "Label").Initializer;
        Assert.True(SyntaxFactory.AreEquivalent(
            SyntaxFactory.ParseExpression("new string('x', 2)"), scalarInitializer!.Value));
        Assert.Contains("RootEntity.CreateLabel()", generated);
        Assert.Contains("Sample.RootEntity.CreateLabel()", generated);
        Assert.Contains("EntityAlias.CreateLabel<string>()", generated);
        Assert.DoesNotContain("ReadOnlyRootEntity.CreateLabel", generated);
        Assert.Contains("new ScalarValue()", generated);
        Assert.Contains("new Sample.ScalarValue()", generated);
        Assert.Contains("new ScalarRecord()", generated);
        Assert.Contains("new ReadOnlyNestedValue[0]", generated);
        Assert.Contains("new ReadOnlyNestedValue[RootEntity.GetArrayLength()]", generated);
        Assert.Contains("new ReadOnlyNestedValue[]", generated);
        Assert.Contains("new int[RootEntity.GetArrayLength()]", generated);
        Assert.DoesNotContain("ReadOnlyRootEntity.GetArrayLength", generated);
        Assert.Contains("default(ReadOnlyNestedValue)", generated);
        Assert.Contains("(ReadOnlyNestedValue)RootEntity.GetObject()", generated);
        Assert.Contains("RootEntity.GetObject() as ReadOnlyNestedValue", generated);
        Assert.Contains("RootEntity.GetObject() is ReadOnlyNestedValue", generated);
        Assert.Contains("default(ScalarValue)", generated);
        Assert.DoesNotContain("ReadOnlyRootEntity.GetObject", generated);
        Assert.Contains("System.Array.Empty<ReadOnlyNestedValue>()", generated);
        Assert.Contains("Enumerable.Empty<ReadOnlyNestedValue>().ToArray()", generated);
        Assert.Contains("= Empty<ReadOnlyNestedValue>()", generated);
        Assert.Contains("EntityAlias.CreateArray<ReadOnlyNestedValue>()", generated);
        Assert.Contains("System.Array.Empty<ScalarValue>()", generated);
        Assert.DoesNotContain("ReadOnlyRootEntity.CreateArray", generated);
        Assert.Contains("is ReadOnlyNestedValue value ? value : new ReadOnlyNestedValue()", generated);
        Assert.Contains("ReadOnlyNestedValue value => value", generated);
        Assert.Contains("is ReadOnlyNestedValue { Enabled: true } value", generated);
        Assert.Contains("ReadOnlyNestedValue => true", generated);
        Assert.Contains("is ScalarValue value", generated);
        Assert.Contains("ReadOnlyNestedValue value = new ReadOnlyNestedValue()", generated);
        Assert.Contains("foreach (ReadOnlyNestedValue child in", generated);
        Assert.Contains("from ReadOnlyNestedValue child in", generated);
        Assert.Contains("join ReadOnlyNestedValue other in", generated);
        Assert.Contains("= ReadOnlyNestedValue.Default", generated);
        Assert.DoesNotContain("= NestedValue.Default", generated);
        Assert.DoesNotContain("= Sample.NestedValue.Default", generated);
        Assert.DoesNotContain("= NestedAlias.Default", generated);
        Assert.Contains("typeof(ReadOnlyNestedValue)", generated);
        Assert.DoesNotContain("typeof(NestedAlias)", generated);
        Assert.DoesNotContain("typeof(Sample.NestedValue)", generated);
        Assert.Contains("typeof(ScalarValue)", generated);
        Assert.Contains("= RootEntity.DefaultLabel", generated);
        Assert.Contains("= ReadOnlyNestedValue.SourceLabel", generated);
        Assert.Contains("(ReadOnlyNestedValue value) => value", generated);
        Assert.Contains("delegate (ReadOnlyNestedValue value)", generated);
        Assert.Contains("ReadOnlyNestedValue Make(ReadOnlyNestedValue value)", generated);
        Assert.Contains("nameof(ReadOnlyNestedValue)", generated);
        Assert.DoesNotContain("nameof(NestedAlias)", generated);
        Assert.DoesNotContain("nameof(Sample.NestedValue)", generated);
        Assert.Contains("nameof(ScalarValue)", generated);
        Assert.Contains("nameof(RootEntity.GetObject)", generated);
        Assert.Contains("= ReadOnlyRootEntity.State.Active", generated);
        Assert.DoesNotContain("= RootEntity.State.Active", generated);
        Assert.DoesNotContain("= Sample.RootEntity.State.Active", generated);
        Assert.DoesNotContain("= EntityAlias.State.Active", generated);
        var typedLambda = Assert.Single(CSharpSyntaxTree.ParseText(generated).GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedLambdaExpressionSyntax>(),
            lambda => lambda.ReturnType != null);
        Assert.Equal("ReadOnlyNestedValue", typedLambda.ReturnType!.ToString());
        Assert.Contains("out ReadOnlyNestedValue result", generated);
        Assert.Contains("(ReadOnlyNestedValue first, ReadOnlyNestedValue second)", generated);
        Assert.Contains("where T : ReadOnlyNestedValue, new()", generated);
        Assert.DoesNotContain(driver.GetRunResult().Results.Single().GeneratedSources,
            result => result.HintName.Contains("ReadOnlyScalar", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DoesNotGenerateOrCrashWhenNoDbContextIsPresent()
    {
        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { "namespace Sample; public sealed class PlainType { }" }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public void GeneratesForAContextThatIndirectlyInheritsDbContext()
    {
        var source = CSharpSyntaxTree.ParseText("""
            using Microsoft.EntityFrameworkCore;

            public class Entity { public int Id { get; set; } }
            public abstract class ApplicationContextBase : DbContext { }
            public class ApplicationContext : ApplicationContextBase
            {
                public DbSet<Entity> Entities { get; set; } = null!;
            }
            """);

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorRegression",
            syntaxTrees: [source],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator().AsSourceGenerator());
        driver = driver.RunGenerators(compilation);

        var generatedSources = driver.GetRunResult().Results.Single().GeneratedSources;
        Assert.Contains(generatedSources, sourceResult => sourceResult.HintName == "ReadOnlyApplicationContext.g.cs");
    }

    [Fact]
    public void GeneratedInterfaceCompilesForExpressionBodiedDbSetProperty()
    {
        const string source = /* lang=c#-test */ """
            using Microsoft.EntityFrameworkCore;

            public class Entity
            {
                public int Id { get; set; }
            }

            public class ApplicationContext : DbContext
            {
                public DbSet<Entity> Entities => Set<Entity>();
            }
            """;

        var compilation = CSharpCompilation.Create(
            assemblyName: "ExpressionBodiedDbSetRegression",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generatedInterface = Assert.Single(driver.GetRunResult().Results.Single().GeneratedSources,
            sourceResult => sourceResult.HintName == "IReadOnlyApplicationContext.g.cs");
        Assert.DoesNotContain("=>", generatedInterface.SourceText.ToString());
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), diagnostic => diagnostic.Id == "CS8057");
    }

    [Fact]
    public void GeneratesDistinctTypesForEntitiesWithTheSameShortNameAndMergesPartialDeclarations()
    {
        var source = CSharpSyntaxTree.ParseText("""
            using Microsoft.EntityFrameworkCore;

            namespace Sales
            {
                public partial class Customer { public int Id { get; set; } }
                public partial class Customer { public string Name { get; set; } = string.Empty; }
            }

            namespace Support
            {
                public class Customer { public int Id { get; set; } }
            }

            namespace Application
            {
                public class ApplicationContext : DbContext
                {
                    public DbSet<Sales.Customer> SalesCustomers { get; set; } = null!;
                    public DbSet<Support.Customer> SupportCustomers { get; set; } = null!;
                }
            }
            """);

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorRegression",
            syntaxTrees: [source],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator().AsSourceGenerator());
        driver = driver.RunGenerators(compilation);

        var generatedSources = driver.GetRunResult().Results.Single().GeneratedSources;
        var salesCustomers = generatedSources.Where(result => result.HintName.StartsWith("Sales_ReadOnlyCustomer_Sales_", StringComparison.Ordinal)).ToArray();
        var supportCustomer = Assert.Single(generatedSources.Where(result => result.HintName == "Support_ReadOnlyCustomer_Support.g.cs"));
        var context = Assert.Single(generatedSources.Where(result => result.HintName == "ReadOnlyApplicationContext.g.cs"));

        Assert.Equal(2, salesCustomers.Length);
        Assert.Contains(salesCustomers, result => result.SourceText.ToString().Contains("string Name { get; init; }", StringComparison.Ordinal));
        Assert.Contains("class ReadOnlyCustomer_Support", supportCustomer.SourceText.ToString());
        Assert.Contains("DbSet<ReadOnlyCustomer_Sales> SalesCustomers", context.SourceText.ToString());
        Assert.Contains("DbSet<ReadOnlyCustomer_Support> SupportCustomers", context.SourceText.ToString());

    }

    [Fact]
    public void RewritesFullyQualifiedEntityTypesInConfigurations()
    {
        const string source = /* lang=c#-test */ """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;

            namespace Sample.Entities
            {
                public class Widget
                {
                    public int Id { get; set; }
                }
            }

            namespace Sample
            {
                public class ApplicationContext : DbContext
                {
                    public DbSet<Entities.Widget> Widgets { get; set; } = null!;
                }
            }

            namespace Sample.Configurations
            {
                public class WidgetConfiguration : IEntityTypeConfiguration<Sample.Entities.Widget>
                {
                    public void Configure(EntityTypeBuilder<Sample.Entities.Widget> builder)
                    {
                        builder.HasFilter($"{nameof(Sample.Entities.Widget.Id)} IS NOT NULL");
                    }
                }
            }
            """;

        var compilation = CSharpCompilation.Create(
            assemblyName: "FullyQualifiedConfigurationRegression",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator().AsSourceGenerator());
        driver = driver.RunGenerators(compilation);

        var generatedConfiguration = Assert.Single(driver.GetRunResult().Results.Single().GeneratedSources,
            sourceResult => sourceResult.SourceText.ToString().Contains("class ReadOnlyWidgetConfiguration", StringComparison.Ordinal));
        var generatedSource = generatedConfiguration.SourceText.ToString();

        Assert.Contains("IEntityTypeConfiguration<ReadOnlyWidget>", generatedSource);
        Assert.Contains("EntityTypeBuilder<ReadOnlyWidget>", generatedSource);
        Assert.Contains("nameof(ReadOnlyWidget.Id)", generatedSource);
        Assert.DoesNotContain("Sample.Entities.ReadOnlyWidget", generatedSource);
    }

    [Fact]
    public void RewritesEntityConfigurationClassReferencesInsideOnModelCreating()
    {
        const string source = /* lang=c#-test */ """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;

            namespace Sample.Entities
            {
                public class Widget
                {
                    public int Id { get; set; }
                }
            }

            namespace Sample.Configurations
            {
                public class WidgetConfiguration : IEntityTypeConfiguration<Sample.Entities.Widget>
                {
                    public void Configure(EntityTypeBuilder<Sample.Entities.Widget> builder)
                    {
                        builder.HasKey(x => x.Id);
                    }
                }
            }

            namespace Sample
            {
                public class ApplicationContext : DbContext
                {
                    public DbSet<Entities.Widget> Widgets { get; set; } = null!;

                    protected override void OnModelCreating(ModelBuilder modelBuilder)
                    {
                        modelBuilder.ApplyConfiguration(new Configurations.WidgetConfiguration());
                    }
                }
            }
            """;

        var compilation = CSharpCompilation.Create(
            assemblyName: "EntityConfigReferenceRegression",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator().AsSourceGenerator());
        driver = driver.RunGenerators(compilation);

        var generatedDbContext = Assert.Single(driver.GetRunResult().Results.Single().GeneratedSources,
            sourceResult => sourceResult.SourceText.ToString().Contains("class ReadOnlyApplicationContext", StringComparison.Ordinal));
        var generatedSource = generatedDbContext.SourceText.ToString();

        // The generated readonly DbContext must apply the generated readonly entity configuration,
        // not the original mutable one, otherwise the readonly entity ends up unconfigured.
        Assert.Contains("new ReadOnlyWidgetConfiguration()", generatedSource);
        Assert.DoesNotContain("new WidgetConfiguration()", generatedSource);
        Assert.DoesNotContain("new Configurations.WidgetConfiguration()", generatedSource);
        Assert.DoesNotContain("new Configurations.ReadOnlyWidgetConfiguration()", generatedSource);
    }

    [Fact]
    public void ReusesSyntaxCandidatesAfterAnUnrelatedSyntaxEdit()
    {
        var contextTree = CSharpSyntaxTree.ParseText("""
            using Microsoft.EntityFrameworkCore;

            public class Entity { public int Id { get; set; } }
            public class ApplicationContext : DbContext
            {
                public DbSet<Entity> Entities { get; set; } = null!;
            }
            """);
        var unrelatedTree = CSharpSyntaxTree.ParseText("public class Unrelated { public int Value => 1; }");
        var compilation = CSharpCompilation.Create(
            assemblyName: "IncrementalRegression",
            syntaxTrees: [contextTree, unrelatedTree],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);

        var updatedUnrelatedTree = CSharpSyntaxTree.ParseText("public class Unrelated { public int Value => 2; }");
        var updatedCompilation = compilation.ReplaceSyntaxTree(unrelatedTree, updatedUnrelatedTree);
        driver = driver.RunGenerators(updatedCompilation);

        var trackedSteps = driver.GetRunResult().Results.Single().TrackedSteps;
        var candidateOutputs = trackedSteps["ClassCandidates"].SelectMany(step => step.Outputs).ToArray();

        Assert.NotEmpty(candidateOutputs);
        Assert.All(candidateOutputs, output => Assert.True(
            output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));
    }

    [Fact]
    public async Task GeneratesReadOnlyEntitiesAndDbContext()
    {
        // Input source code
        var inputSource = """
                          #nullable enable
                          using System.Collections.Generic;
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public class User
                              {
                                  public int Id { get; set; }
                                  public string Name { get; set; }
                                  public ICollection<Order> Orders { get; set; }
                                  
                                  public static User Create(int id, string name, ICollection<Order> orders)
                                  {
                                      return new User
                                      {
                                          Id = id,
                                          Name = name,
                                          Orders = orders
                                      };
                                  }
                              }
                          
                              public class Order
                              {
                                  public int Id { get; set; }
                                  public string Description { get; set; }
                              }
                          
                              public class MyDbContext : DbContext
                              {
                                  public DbSet<User> Users { get; set; }
                              }
                          }

                          """;

        // Expected generated output for the ReadOnly entity
        var expectedUserReadOnlySource = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyUser
    {
        public int Id { get; init; }

        public string Name { get; init; }

        public IReadOnlyCollection<ReadOnlyOrder> Orders { get; init; }
    }
}
""";

        var expectedOrderReadOnlySource = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyOrder
    {
        public int Id { get; init; }

        public string Description { get; init; }
    }
}
""";

        // Expected generated output for the ReadonlyDbContext
        var expectedReadonlyDbContextSource = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyUser> Users { get; set; }

        public sealed override int SaveChanges()
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        IQueryable<ReadOnlyUser> IReadOnlyMyDbContext.Users => Users;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        // Expected generated output for the IReadOnlyDbContext interface
        var expectedIReadOnlyDbContextSource = """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlyUser> Users { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        // Configure the test
        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    // Verify the generated sources
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyUser.g.cs", expectedUserReadOnlySource),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyOrder.g.cs", expectedOrderReadOnlySource),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedReadonlyDbContextSource),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedIReadOnlyDbContextSource)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var options = (CSharpCompilationOptions)project.CompilationOptions!;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        // Run the test
        await test.RunAsync();
    }

    [Fact]
    public async Task RewritesValueComparerToReadonlyCollectionAndReadonlyElement()
    {
        var inputSource = """
                          using System.Collections.Generic;
                          using System.Text.Json;
                          using Microsoft.EntityFrameworkCore;
                          using Microsoft.EntityFrameworkCore.ChangeTracking;
                          using Microsoft.EntityFrameworkCore.Metadata.Builders;
                          
                          namespace MyApp.Entities
                          {
                              public class Translation
                              {
                                  public string LanguageCode { get; set; }
                                  public string Value { get; set; }
                              }
                          
                              public class ParameterSetting
                              {
                                  public List<Translation> NameTranslations { get; set; }
                              }
                          
                              public class MyDbContext : DbContext
                              {
                                  public DbSet<ParameterSetting> ParameterSettings { get; set; }
                              }
                          
                              public static class JsonOptions
                              {
                                  public static JsonSerializerOptions IgnoreNulls { get; } = new JsonSerializerOptions();
                              }
                          
                              public class ParameterSettingConfiguration : IEntityTypeConfiguration<ParameterSetting>
                              {
                                  public void Configure(EntityTypeBuilder<ParameterSetting> builder)
                                  {
                                      var valueComparer = new ValueComparer<List<Translation>>(true);
                                      builder.Property(e => e.NameTranslations).HasConversion(v => JsonSerializer.Serialize(v, JsonOptions.IgnoreNulls), v => JsonSerializer.Deserialize<List<Translation>>(v, JsonOptions.IgnoreNulls) ?? new()).Metadata.SetValueComparer(valueComparer);
                                  }
                              }
                          }
                          """;

        var expectedTranslation = """
                                  using MyApp.Entities;
                                  using System;
                                  using System.Collections.Generic;
                                  
                                  namespace MyApp.Entities.Generated
                                  {
                                      public class ReadOnlyTranslation
                                      {
                                          public string LanguageCode { get; init; }
                                  
                                          public string Value { get; init; }
                                      }
                                  }
                                  """;

        var expectedParameterSetting = """
                                       using MyApp.Entities;
                                       using System;
                                       using System.Collections.Generic;
                                       
                                       namespace MyApp.Entities.Generated
                                       {
                                           public class ReadOnlyParameterSetting
                                           {
                                               public IReadOnlyCollection<ReadOnlyTranslation> NameTranslations { get; init; }
                                           }
                                       }
                                       """;

        var expectedConfig = """
                             using Microsoft.EntityFrameworkCore;
                             using Microsoft.EntityFrameworkCore.ChangeTracking;
                             using Microsoft.EntityFrameworkCore.Metadata.Builders;
                             using MyApp.Entities;
                             using System;
                             using System.Collections.Generic;
                             using System.Text.Json;
                             
                             namespace MyApp.Entities.Generated
                             {
                                 public class ReadOnlyParameterSettingConfiguration : IEntityTypeConfiguration<ReadOnlyParameterSetting>
                                 {
                                     public void Configure(EntityTypeBuilder<ReadOnlyParameterSetting> builder)
                                     {
                                         var valueComparer = new ValueComparer<IReadOnlyCollection<ReadOnlyTranslation>>(true);
                                         builder.Property(e => e.NameTranslations).HasConversion(v => JsonSerializer.Serialize(v, JsonOptions.IgnoreNulls), v => JsonSerializer.Deserialize<List<ReadOnlyTranslation>>(v, JsonOptions.IgnoreNulls) ?? new()).Metadata.SetValueComparer(valueComparer);
                                     }
                                 }
                             }
                             """;

        var expectedDbContext = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyParameterSetting> ParameterSettings { get; set; }

        public sealed override int SaveChanges()
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        IQueryable<ReadOnlyParameterSetting> IReadOnlyMyDbContext.ParameterSettings => ParameterSettings;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
                                using Microsoft.EntityFrameworkCore;
                                using Microsoft.EntityFrameworkCore.Infrastructure;
                                using MyApp.Entities;
                                using System;
                                using System.Collections.Generic;
                                using System.Linq;
                                
                                namespace MyApp.Entities.Generated
                                {
                                    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
                                    {
                                        IQueryable<ReadOnlyParameterSetting> ParameterSettings { get; }
                                
                                        IQueryable<TEntity> Set<TEntity>()
                                            where TEntity : class;
                                        DatabaseFacade Database { get; }
                                    }
                                }
                                """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyTranslation.g.cs", expectedTranslation),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyParameterSetting.g.cs", expectedParameterSetting),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyParameterSettingConfiguration.g.cs", expectedConfig),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedDbContext),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedInterface)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var options = (CSharpCompilationOptions)project.CompilationOptions!;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        await test.RunAsync();
    }

    [Fact]
    public async Task SelfReferencingCollectionsBecomeReadOnlyCollectionsWithReadonlyElement()
    {
        var inputSource = """
                          #nullable enable
                          using System.Collections.Generic;
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public class Department
                              {
                                  public int Id { get; set; }
                                  public string Name { get; set; }
                                  public Department? Parent { get; set; }
                                  public List<Department>? Children { get; set; }
                              }

                              public class MyDbContext : DbContext
                              {
                                  public DbSet<Department> Departments { get; set; }
                              }
                          }
                          """;

        var expectedDepartment = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyDepartment
    {
        public int Id { get; init; }

        public string Name { get; init; }

        public ReadOnlyDepartment? Parent { get; init; }

        public IReadOnlyCollection<ReadOnlyDepartment>? Children { get; init; }
    }
}
""";

        var expectedDbContext = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyDepartment> Departments { get; set; }

        public sealed override int SaveChanges()
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        IQueryable<ReadOnlyDepartment> IReadOnlyMyDbContext.Departments => Departments;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlyDepartment> Departments { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyDepartment.g.cs", expectedDepartment),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedDbContext),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedInterface)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var options = (CSharpCompilationOptions)project.CompilationOptions!;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        await test.RunAsync();
    }

    [Fact]
    public async Task NullableNavigationCollectionBecomesNullableReadOnlyCollectionWithReadonlyElement()
    {
        var inputSource = """
                          #nullable enable
                          using System.Collections.Generic;
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public class Translation
                              {
                                  public string LanguageCode { get; set; }
                                  public string Value { get; set; }
                              }

                              public class SettingKey
                              {
                                  public IList<Translation>? Link { get; set; }
                              }

                              public class MyDbContext : DbContext
                              {
                                  public DbSet<SettingKey> SettingKeys { get; set; }
                              }
                          }
                          """;

        var expectedTranslation = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyTranslation
    {
        public string LanguageCode { get; init; }

        public string Value { get; init; }
    }
}
""";

        var expectedSettingKey = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlySettingKey
    {
        public IReadOnlyCollection<ReadOnlyTranslation>? Link { get; init; }
    }
}
""";

        var expectedDbContext = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlySettingKey> SettingKeys { get; set; }

        public sealed override int SaveChanges()
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        IQueryable<ReadOnlySettingKey> IReadOnlyMyDbContext.SettingKeys => SettingKeys;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlySettingKey> SettingKeys { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlySettingKey.g.cs", expectedSettingKey),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyTranslation.g.cs", expectedTranslation),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedDbContext),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedInterface)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var options = (CSharpCompilationOptions)project.CompilationOptions!;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        await test.RunAsync();
    }

    [Fact]
    public async Task GeneratesReadOnlyOwnedTypeConfiguredInOnModelCreating()
    {
        var inputSource = """
                          #nullable enable
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public class Order
                              {
                                  public int Id { get; set; }
                              }

                              public class ShippingAddress
                              {
                                  public string Street { get; set; }
                              }

                              public class MyDbContext : DbContext
                              {
                                  public DbSet<Order> Orders { get; set; }

                                  protected override void OnModelCreating(ModelBuilder modelBuilder)
                                  {
                                      modelBuilder.Entity<Order>().OwnsOne<ShippingAddress>("ShippingAddress");
                                  }
                              }
                          }
                          """;

        var expectedOrder = """
#nullable enable
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyOrder
    {
        public int Id { get; init; }
    }
}
""";

        var expectedShippingAddress = """
#nullable enable
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public class ReadOnlyShippingAddress
    {
        public string Street { get; init; }
    }
}
""";

        var expectedDbContext = """
#nullable enable
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyOrder> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReadOnlyOrder>().OwnsOne<ReadOnlyShippingAddress>("ShippingAddress");
        }

        public sealed override int SaveChanges()
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        IQueryable<ReadOnlyOrder> IReadOnlyMyDbContext.Orders => Orders;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlyOrder> Orders { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyOrder.g.cs", expectedOrder),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyShippingAddress.g.cs", expectedShippingAddress),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedDbContext),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedInterface)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var options = (CSharpCompilationOptions)project.CompilationOptions!;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        await test.RunAsync();
    }

    [Fact]
    public async Task GeneratesReadOnlyEntityForRecordDbSetEntity()
    {
        var inputSource = """
                          using Microsoft.EntityFrameworkCore;

                          namespace MyApp.Entities
                          {
                              public record Reservation
                              {
                                  public int Id { get; set; }

                                  public Reservation(int id)
                                  {
                                      Id = id;
                                  }
                              }

                              public class MyDbContext : DbContext
                              {
                                  public DbSet<Reservation> Reservations { get; set; }

                                  protected override void OnModelCreating(ModelBuilder modelBuilder)
                                  {
                                      modelBuilder.Entity<Reservation>().HasData(new Reservation(7) { Id = 1 });
                                  }
                              }
                          }
                          """;

        var expectedReservation = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Collections.Generic;

namespace MyApp.Entities.Generated
{
    public record ReadOnlyReservation
    {
        public int Id { get; init; }

        public ReadOnlyReservation(int id)
        {
            Id = id;
        }
    }
}
""";

        var expectedDbContext = """
using Microsoft.EntityFrameworkCore;
using MyApp.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Entities.Generated
{
    public partial class ReadOnlyMyDbContext : DbContext, IReadOnlyMyDbContext
    {
        public DbSet<ReadOnlyReservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReadOnlyReservation>().HasData(new ReadOnlyReservation(7) { Id = 1 });
        }

        public sealed override int SaveChanges()
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Saving changes is not supported by a readonly db context.");
        }

        IQueryable<ReadOnlyReservation> IReadOnlyMyDbContext.Reservations => Reservations;
        IQueryable<TEntity> IReadOnlyMyDbContext.Set<TEntity>()
            where TEntity : class => Set<TEntity>();
    }
}
""";

        var expectedInterface = """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Entities;
using System;
using System.Linq;

namespace MyApp.Entities.Generated
{
    public partial interface IReadOnlyMyDbContext : IDisposable, IAsyncDisposable
    {
        IQueryable<ReadOnlyReservation> Reservations { get; }

        IQueryable<TEntity> Set<TEntity>()
            where TEntity : class;
        DatabaseFacade Database { get; }
    }
}
""";

        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources = { inputSource },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                },
                GeneratedSources =
                {
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyReservation.g.cs", expectedReservation),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "ReadOnlyMyDbContext.g.cs", expectedDbContext),
                    (typeof(ReadonlyDbContextGenerator.ReadOnlyDbContextGenerator), "IReadOnlyMyDbContext.g.cs", expectedInterface)
                }
            },
        };

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var options = (CSharpCompilationOptions)project.CompilationOptions!;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(new Dictionary<string, ReportDiagnostic>
            {
                ["CS8618"] = ReportDiagnostic.Suppress
            }));
            return project.WithCompilationOptions(options).Solution;
        });

        await test.RunAsync();
    }
}
