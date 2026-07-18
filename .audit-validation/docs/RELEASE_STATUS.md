# Release Status

| Release | Status | Validation | Scope |
| --- | --- | --- | --- |
| v0.1 Foundation | Complete | Restore, build, automated tests, and manual UI validation complete. | Read-only scan pipeline, metadata, hashing, deterministic rules, Dashboard, Settings, Diagnostics, and supporting application infrastructure. |
| v0.2 Results Exploration | Complete | Restore, build, 233 automated tests, and manual UI validation complete. | Immutable result snapshots, Results Explorer, filtering, sorting, paging, details, and exact-duplicate review. |
| v0.3 Local Suggestions and Ranked Exploration | Complete | Clean isolated restore/build and 251 automated tests passed. Existing repository `obj` folders blocked direct in-place validation. | Optional Ollama integration, validated read-only suggestions, local decision history, session tags, deterministic ranked search, and product polish. |
| v0.4 Opt-in Local Catalog | Complete | Clean isolated restore/build and 260 automated tests passed. Existing repository `obj` folders blocked direct in-place validation. | Bounded opt-in application-data JSON catalog, historical snapshot reopening, accepted-tag restoration, and catalog safety controls. |
| v0.5 Catalog Search and Maintenance | Complete | Clean isolated restore/build and 267 automated tests passed. Existing repository `obj` folders blocked direct in-place validation. | Deterministic catalog-wide metadata search, historical-hit opening, selected entry removal, and two-step clear of application-owned catalog data. |
| v0.6 User-Managed Tags | Complete | Clean isolated restore/build and 274 automated tests passed; zero build warnings/errors. | Bounded manual tag add/remove, protected deterministic tags, immediate search integration, and catalog-backed persistence. |
| v0.7 Saved Catalog Searches | Complete | Clean isolated restore/build and 283 automated tests passed; zero build warnings/errors. | Bounded atomic named queries, current-catalog rerun, selected removal, two-step corruption-recovery reset, and no persisted hits. |
| v0.8 Snapshot Identity and Scope | Complete | Clean isolated restore/build and 290 automated tests passed; zero build warnings/errors. | Catalog schema 2, schema-1 read compatibility, bounded names/source roots, and Saved Catalog/search identity context. |
| v0.9 Historical Snapshot Comparison | Complete | Post-implementation audit: clean isolated restore/build and 330 automated tests passed; zero build warnings/errors. | Bounded deterministic metadata/tag comparison, scope compatibility, cancellation, filters, historical opening, stale-state hardening, and persistence/safety hardening. |

## Current product boundary

OpenSorSe is currently a safe, local-first, read-only file analysis application. It scans selected folders and presents in-memory analysis results; it does not change selected user files.

The current Desktop workflow does not:

- Rename, move, delete, overwrite, or otherwise modify user files.
- Execute planned operations or undo operations.
- Execute AI suggestions, OCR, semantic search, content readers, or automatic organization. Optional local Ollama can generate validated previews only.
- Persist scan results or tags unless the user explicitly enables the bounded local catalog. It has no persistent full-text/semantic search index. It persists settings, separate local AI review decisions, optionally schema-2 `catalog.json`, and up to 25 named query presets in `saved-catalog-searches.json` in OpenSorSe application data. Comparison results and filters are never persisted.
- Treat catalog comparison as live filesystem state. It compares historical stored metadata only and never opens a stored path.

Mutation-capable Executor types remain historical/internal test infrastructure but are not registered by the Desktop composition root. Current owned persistence and network behavior are detailed in [Safety and Privacy](SAFETY_AND_PRIVACY.md).

## Validation baseline

The audited v0.9 working tree was restored, built, and tested successfully in a clean isolated copy: 330 tests passed with zero build warnings and errors. This environment denied writes to pre-existing generated `obj` folders in the repository itself, so direct in-place restore/build/test failed before compilation. No source or user files were removed to work around that host restriction.

## Documentation status

The architecture directory contains both current implementation documentation and longer-term design material. Documents for Readers, AI, Database, Search, Reports, and Plugins describe future architectural intent unless explicitly identified as implemented in the current release documentation.

## Next release

v0.9 is complete and has received a senior corrective audit. See the [implementation-specification index](Implementation_Spec/README.md), [v0.8 proposal](Implementation_Spec/v0.8/00_v0.8_Release_Proposal.md), [v0.9 proposal](Implementation_Spec/v0.9/00_v0.9_Release_Proposal.md), and [audit corrections](Implementation_Spec/v0.9/AUDIT_CORRECTIONS.md) for current identity, migration, historical-comparison, persistence, and safety boundaries.
