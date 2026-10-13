# Changelog

All notable changes to this project will be documented in this file.

## v0.2.5
- Rewrite entity type references in property initializers, including nested owned objects, aliases, and fully qualified types, so generated read-only properties are initialized with their read-only counterparts.
- Preserve static method receivers in property initializers while rewriting explicit type operands because methods are not copied to generated entities.
- Rewrite entity types in `default`, casts, and `as`/`is` type expressions within property initializers.
- Rewrite entity type arguments in generic initializer calls without changing method names or static receivers.
- Rewrite entity types in declaration, type, and recursive patterns within conditional and switch initializers.
- Rewrite local, foreach, and explicitly typed query declarations inside initializer lambdas.
- Rewrite receivers for copied static properties while preserving receivers for removed methods.
- Rewrite entity operands in `typeof` initializers while preserving scalar value types.
- Rewrite entity array creation initializers, including aliased and fully qualified element types, while preserving dimension expressions.
- Exclude value types from navigation discovery so struct and record-struct properties retain their original types and initializers.

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
