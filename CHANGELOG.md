# Changelog

All notable changes to this project will be documented in this file.

## v0.1.6
- Updated DbContext analysis in the source generator to detect owned entity types referenced from EF Core fluent API calls (`OwnsOne`, `OwnsMany`, and `Owned`) and include them in read-only entity generation.
- Ensured fluent API type rewriting in generated read-only DbContext works for owned type mappings (for example, `OwnsOne<ShippingAddress>` becomes `OwnsOne<ReadOnlyShippingAddress>`).
