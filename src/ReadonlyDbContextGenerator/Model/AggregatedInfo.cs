using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ReadonlyDbContextGenerator.Model;

internal class AggregatedInfo(
    DbContextInfo dbContext,
    ImmutableArray<EntityConfigInfo> configurations,
    Compilation compilation)
{
    public DbContextInfo DbContext { get; set; } = dbContext;
    public ImmutableArray<EntityConfigInfo> Configurations { get; set; } = configurations;
    public Compilation Compilation { get; } = compilation;
}