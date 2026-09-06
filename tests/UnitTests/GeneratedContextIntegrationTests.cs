using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace UnitTests;

public sealed class RuntimeEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class RuntimeDbContext(DbContextOptions<RuntimeDbContext> options) : DbContext(options)
{
    public DbSet<RuntimeEntity> Entities { get; set; } = null!;
}

public class GeneratedContextIntegrationTests
{
    [Fact]
    public async Task GeneratedContextBuildsItsModelQueriesDataAndBlocksAllSaveOperations()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<Generated.ReadOnlyRuntimeDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new Generated.ReadOnlyRuntimeDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("INSERT INTO \"Entities\" (\"Id\", \"Name\") VALUES (1, 'Read model')");

        var entity = await context.Entities.SingleAsync();

        Assert.Equal("Read model", entity.Name);
        Assert.Throws<NotSupportedException>(() => context.SaveChanges());
        Assert.Throws<NotSupportedException>(() => context.SaveChanges(acceptAllChangesOnSuccess: false));
        await Assert.ThrowsAsync<NotSupportedException>(() => context.SaveChangesAsync());
        await Assert.ThrowsAsync<NotSupportedException>(() => context.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken: default));
    }
}
