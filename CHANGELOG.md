# Changelog

All notable changes to this project will be documented in this file.

## v0.2.4
- Fixed the explicit `IReadOnly…DbContext` property implementations keeping the original property initializer. `public DbSet<X> Xs { get; set; } = null!;` generated `IQueryable<ReadOnlyX> IReadOnlyContext.Xs => Xs = null!;`, so reading the property through the interface replaced the `DbSet` with `null`.
- Fixed generator crashes (`InvalidCastException`) for nullable (`DbSet<X>?`) and fully qualified (`Microsoft.EntityFrameworkCore.DbSet<X>`) `DbSet` properties.
- Fixed a generated interface that did not compile for expression-bodied `DbSet` properties (`public DbSet<X> Xs => Set<X>();`).
- Fixed contexts declared in the global namespace generating an invalid `namespace <global namespace>.Generated`.
- The generated interface and its explicit implementation are now built from a single list of queryable sets, so they can no longer drift apart.
- Made generator implementation types `internal`. Removed dead code. The package is no longer packed on every build.
- Tests: the new `GeneratorHarness` compiles the generated output against the full EF Core reference set and fails on any error or generator crash.

## v0.2.3
- Fixed generated readonly `DbContext` classes calling the original (non-readonly) `IEntityTypeConfiguration<T>` classes instead of their generated readonly counterparts (e.g. `new FooEntityConfig()` instead of `new ReadOnlyFooEntityConfig()`). This caused readonly entities to be left without their EF Core relationship/index configuration, leading to `InvalidOperationException: Unable to determine the relationship...` at runtime whenever an entity configuration class was referenced by name inside `OnModelCreating` (directly or via a helper method).

## v0.2.2
- Rewrites fully qualified entity member references in generated EF Core configurations, including `nameof(...)` expressions.

## v0.2.1
- Fixed fully qualified entity references in generated EF Core configurations so they point to the generated readonly entity type.

## v0.2.0
- Blocked all `SaveChanges` overloads in generated contexts, including `SaveChanges(bool)`.
- Generated `NotSupportedException` for blocked save operations.
- Avoided generator failures in projects with no discovered `DbContext` and added support for indirect `DbContext` inheritance.
- Corrected crash diagnostic truncation and package README casing.
- Refactored incremental discovery so class syntax candidates are cached independently from compilation-dependent semantic analysis.

## v0.1.7
- Fixed constructor handling in readonly entities to properly update constructor identifiers to match the readonly class name.
- Simplified syntax helper to use `as TypeDeclarationSyntax` instead of explicit type checking, improving support for record entities.
- Added support for C# record entities in readonly entity generation, allowing records with constructors to be properly converted to readonly records.

## v0.1.6
- Updated DbContext analysis in the source generator to detect owned entity types referenced from EF Core fluent API calls (`OwnsOne`, `OwnsMany`, and `Owned`) and include them in read-only entity generation.
- Ensured fluent API type rewriting in generated read-only DbContext works for owned type mappings (for example, `OwnsOne<ShippingAddress>` becomes `OwnsOne<ReadOnlyShippingAddress>`).
