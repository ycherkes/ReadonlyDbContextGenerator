using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ReadonlyDbContextGenerator.Model;

internal class AggregatedInfo(
    DbContextInfo dbContext,
    ImmutableArray<EntityInfo> entities,
    ImmutableArray<EntityConfigInfo> configurations,
    Compilation compilation)
{
    public DbContextInfo DbContext { get; set; } = dbContext;
    public ImmutableArray<EntityInfo> Entities { get; set; } = entities;
    public ImmutableArray<EntityConfigInfo> Configurations { get; set; } = configurations;
    public Compilation Compilation { get; } = compilation;
}