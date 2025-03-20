using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ReadonlyDbContextGenerator.Model;

internal class AggregatedInfo(
    ImmutableArray<DbContextInfo> dbContexts,
    ImmutableArray<EntityConfigInfo> configurations,
    ImmutableArray<EntityInfo> entities,
    Compilation compilation)
{
    public ImmutableArray<DbContextInfo> DbContexts { get; set; } = dbContexts;
    public ImmutableArray<EntityInfo> Entities { get; } = entities;
    public ImmutableArray<EntityConfigInfo> Configurations { get; set; } = configurations;
    public Compilation Compilation { get; } = compilation;
}