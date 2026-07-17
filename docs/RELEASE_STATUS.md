# Release Status

| Release | Status | Validation | Scope |
| --- | --- | --- | --- |
| v0.1 Foundation | Complete | Restore, build, automated tests, and manual UI validation complete. | Read-only scan pipeline, metadata, hashing, deterministic rules, Dashboard, Settings, Diagnostics, and supporting application infrastructure. |
| v0.2 Results Exploration | Complete | Restore, build, 233 automated tests, and manual UI validation complete. | Immutable result snapshots, Results Explorer, filtering, sorting, paging, details, and exact-duplicate review. |

## Current product boundary

OpenSorSe is currently a safe, local-first, read-only file analysis application. It scans selected folders and presents in-memory analysis results; it does not change selected user files.

The current Desktop workflow does not:

- Rename, move, delete, overwrite, or otherwise modify user files.
- Execute planned operations or undo operations.
- Run AI, OCR, semantic search, content readers, or automatic organization.
- Persist scan results, search indexes, or result history.

## Validation baseline

The validated v0.2 branch is `coding/v0.2`. Validation includes successful restore and build, 233 passing automated tests, and completed manual UI validation of the read-only scan and results-review workflow.

## Documentation status

The architecture directory contains both current implementation documentation and longer-term design material. Documents for Readers, AI, Database, Search, Reports, and Plugins describe future architectural intent unless explicitly identified as implemented in the current release documentation.

## Next release

v0.3 is planned for usability improvements and workflow enhancements. Its exact scope will be defined by future proposals; no future feature is implied to be implemented today.
