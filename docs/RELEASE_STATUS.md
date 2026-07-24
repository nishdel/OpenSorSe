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
| v0.9.1 Optional AI and Feature Controls | Implementation and corrective pass complete; manual UI verification pending | Redirected-artifact restore/build and 453 automated tests passed; zero build warnings/errors. Direct in-place generated folders remain host-write-protected. | Default-off AI/advanced controls, constrained suggestions, hardened Ollama transport/setup, bounded diagnostics, contextual Help, consistent statuses, corrected Catalog Search, and responsive Duplicate View. |

## Current product boundary

OpenSorSe v0.9.1 is a safe, local-first, read-only file analysis application. It scans selected folders and presents in-memory analysis results; it does not change selected user files. AI and advanced mode are independent and disabled by default.

The current Desktop workflow does not:

- Rename, move, delete, overwrite, or otherwise modify user files.
- Execute planned operations or undo operations.
- Execute AI suggestions, OCR, semantic search, content readers, or automatic organization. Optional Ollama can generate only capability-specific, validated rename and logical folder-structure previews.
- Persist scan results or tags unless the user explicitly enables the bounded local catalog. It has no persistent full-text/semantic search index. It persists settings, separate local AI review decisions, optionally schema-2 `catalog.json`, and up to 25 named query presets in `saved-catalog-searches.json` in OpenSorSe application data. Comparison results and filters are never persisted.
- Treat catalog comparison as live filesystem state. It compares historical stored metadata only and never opens a stored path.

Duplicate View may, only after an explicit user command, pass a validated path from the current in-memory scan to the operating-system shell to open a file or its containing folder. Each action is capped at five targets, uses no constructed shell command, reports partial failures, and performs no OpenSorSe filesystem mutation.

Mutation-capable Executor types remain historical/internal test infrastructure but are not registered by the Desktop composition root. Current owned persistence and network behavior are detailed in [Safety and Privacy](SAFETY_AND_PRIVACY.md).

## Validation baseline

The v0.9.1 tree restored and built successfully using the repository-local ignored `.artifacts` output root. The full suite passed 453 tests: Core 26, Scanner 56, Rules 68, Executor 36, Application 153, and Desktop 114. The Debug build produced zero warnings and zero errors, compiled every Avalonia view, and validated/resolved the production dependency graph in a composition-root test. This environment still denies writes to pre-existing generated `obj` files in the repository itself, including after an unsandboxed retry, so the standard in-place restore/build cannot complete; this is a host artifact ACL limitation rather than a compilation or test failure. No source or user file was removed to work around it.

## Documentation status

The architecture directory contains both current implementation documentation and longer-term design material. Documents for Readers, AI, Database, Search, Reports, and Plugins describe future architectural intent unless explicitly identified as implemented in the current release documentation.

## Current release

v0.9.1 implementation and automated validation are complete; local GUI verification remains required before considering integration. See the [implementation-specification index](Implementation_Spec/README.md), [v0.9.1 proposal](Implementation_Spec/v0.9.1/00_v0.9.1_Release_Proposal.md), [specification 046](Implementation_Spec/v0.9.1/046_Optional_AI_and_Advanced_Feature_Controls.md), [correction specification 047](Implementation_Spec/v0.9.1/047_Correction_Reliability_and_Usability_Pass.md), and [manual checklist](MANUAL_TESTING_v0.9.1.md). v1.0 plugins, broad localization, packaging, content understanding, and automatic file operations remain deferred.
