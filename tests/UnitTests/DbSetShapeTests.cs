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

    private static void AssertContains(string expected, string actual)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            Assert.Fail($"Expected to find:{Environment.NewLine}{expected}{Environment.NewLine}in:{Environment.NewLine}{actual}");
        }
    }
}
