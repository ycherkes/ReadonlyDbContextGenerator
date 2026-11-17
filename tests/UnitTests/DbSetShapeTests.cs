namespace UnitTests;

/// <summary>
/// DbSet properties can be declared in several syntactic shapes; all of them must produce
/// a compiling readonly context and a matching interface member.
/// </summary>
public class DbSetShapeTests
{
    [Theory]
    [InlineData("DbSet<Widget>", "{ get; set; } = null!;")]
    [InlineData("DbSet<Widget>?", "{ get; set; }")]
    [InlineData("Microsoft.EntityFrameworkCore.DbSet<Widget>", "{ get; set; } = null!;")]
    [InlineData("global::Microsoft.EntityFrameworkCore.DbSet<Widget>", "{ get; set; } = null!;")]
    [InlineData("Microsoft.EntityFrameworkCore.DbSet<Widget>?", "{ get; set; }")]
    [InlineData("DbSet<Widget>", "=> Set<Widget>();")]
    public void GeneratesCompilingContextAndInterfaceForDbSetShape(string dbSetType, string body)
    {
        var source = $$"""
            using Microsoft.EntityFrameworkCore;

            namespace Sample
            {
                public class Widget
                {
                    public int Id { get; set; }
                }

                public class ApplicationContext : DbContext
                {
                    public {{dbSetType}} Widgets {{body}}
                }
            }
            """;

        var result = GeneratorHarness.RunAndAssertCompiles(source);

        var context = Assert.Single(result.GeneratedSources, s => s.HintName == "ReadOnlyApplicationContext.g.cs").SourceText.ToString();
        var contextInterface = Assert.Single(result.GeneratedSources, s => s.HintName == "IReadOnlyApplicationContext.g.cs").SourceText.ToString();

        AssertContains("IQueryable<ReadOnlyWidget> IReadOnlyApplicationContext.Widgets => Widgets;", context);
        AssertContains("IQueryable<ReadOnlyWidget> Widgets { get; }", contextInterface);
    }

    [Fact]
    public void EscapesKeywordDbSetPropertyNames()
    {
        var source = """
            using Microsoft.EntityFrameworkCore;

            namespace Sample
            {
                public class Widget
                {
                    public int Id { get; set; }
                }

                public class ApplicationContext : DbContext
                {
                    public DbSet<Widget> @event { get; set; } = null!;
                }
            }
            """;

        var result = GeneratorHarness.RunAndAssertCompiles(source);

        var context = Assert.Single(result.GeneratedSources, s => s.HintName == "ReadOnlyApplicationContext.g.cs").SourceText.ToString();
        var contextInterface = Assert.Single(result.GeneratedSources, s => s.HintName == "IReadOnlyApplicationContext.g.cs").SourceText.ToString();

        AssertContains("IQueryable<ReadOnlyWidget> IReadOnlyApplicationContext.@event => @event;", context);
        AssertContains("IQueryable<ReadOnlyWidget> @event { get; }", contextInterface);
    }

    [Fact]
    public void IncludesDbSetsDeclaredInAnotherPartialDeclaration()
    {
        var source = """
            using Microsoft.EntityFrameworkCore;

            namespace Sample
            {
                public class Widget
                {
                    public int Id { get; set; }
                }

                public partial class ApplicationContext : DbContext
                {
                }

                public partial class ApplicationContext
                {
                    public DbSet<Widget> Widgets { get; set; } = null!;
                }
            }
            """;

        var result = GeneratorHarness.RunAndAssertCompiles(source);

        var context = Assert.Single(result.GeneratedSources, s => s.HintName == "ReadOnlyApplicationContext.g.cs").SourceText.ToString();
        var contextInterface = Assert.Single(result.GeneratedSources, s => s.HintName == "IReadOnlyApplicationContext.g.cs").SourceText.ToString();

        AssertContains("IQueryable<ReadOnlyWidget> IReadOnlyApplicationContext.Widgets => Widgets;", context);
        AssertContains("IQueryable<ReadOnlyWidget> Widgets { get; }", contextInterface);
    }

    [Fact]
    public void KeepsUsingAliasesScopedToTheirPartialContextFile()
    {
        var first = """
            using Microsoft.EntityFrameworkCore;
            using Value = System.Int32;

            namespace Sample
            {
                public class Widget
                {
                    public int Id { get; set; }
                }

                public partial class ApplicationContext : DbContext
                {
                    public DbSet<Widget> Widgets { get; set; } = null!;

                    public Value Number => 1;
                }
            }
            """;

        var second = """
            using Value = System.String;

            namespace Sample
            {
                public partial class ApplicationContext
                {
                    public Value Text => "text";
                }
            }
            """;

        GeneratorHarness.RunAndAssertCompiles([first, second]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KeepsNamespaceScopedImportsInAdditionalPartialContextFiles(bool fileScopedNamespace)
    {
        var first = """
            using Microsoft.EntityFrameworkCore;

            namespace Sample
            {
                public class Widget
                {
                    public int Id { get; set; }
                }

                public partial class ApplicationContext : DbContext
                {
                    public DbSet<Widget> Widgets { get; set; } = null!;
                }
            }
            """;

        var second = fileScopedNamespace ? """
            using Value = System.Int32;

            namespace Sample;

            using Value = System.String;
            using System.Text;
            using static System.Math;

            public partial class ApplicationContext
            {
                public Value Text => "text";
                public StringBuilder Buffer => new StringBuilder();
                public double Number => Sqrt(4);
            }
            """ : """
            using Value = System.Int32;

            namespace Sample
            {
                using Value = System.String;
                using System.Text;
                using static System.Math;

                public partial class ApplicationContext
                {
                    public Value Text => "text";
                    public StringBuilder Buffer => new StringBuilder();
                    public double Number => Sqrt(4);
                }
            }
            """;

        GeneratorHarness.RunAndAssertCompiles([first, second]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreservesRelativeImportTargetsWhenRelocatingAdditionalPartialContexts(bool fileScopedNamespace)
    {
        var first = """
            using Microsoft.EntityFrameworkCore;

            namespace Sample.Models
            {
                public class Widget
                {
                    public int Id { get; set; }
                }
            }

            namespace Sample.Data
            {
                public partial class ApplicationContext : DbContext
                {
                    public DbSet<Sample.Models.Widget> Widgets { get; set; } = null!;
                }
            }

            namespace Sample.Data.Helpers
            {
                public class Helper { }
                public static class Constants
                {
                    public const int Number = 42;
                }
            }

            namespace Sample.Data.Unused
            {
                public class Marker { }
            }
            """;

        var namespaceHeader = fileScopedNamespace ? "namespace Sample.Data;" : "namespace Sample.Data {";
        var namespaceFooter = fileScopedNamespace ? string.Empty : "}";
        var second = $$"""
            {{namespaceHeader}}
            using Unused;
            using Helpers;
            using Value = Helpers.Helper;
            using HelperNamespace = Helpers;
            using static Helpers.Constants;

            public partial class ApplicationContext
            {
                public Helper Helper => new Helper();
                public Value AliasedHelper => new Value();
                public HelperNamespace.Helper NamespaceAliasedHelper => new HelperNamespace.Helper();
                public int Answer => Number;
            }
            {{namespaceFooter}}
            """;

        GeneratorHarness.RunAndAssertCompiles([first, second]);
    }

    private static void AssertContains(string expected, string actual)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            Assert.Fail($"Expected to find:{Environment.NewLine}{expected}{Environment.NewLine}in:{Environment.NewLine}{actual}");
        }
    }
}
