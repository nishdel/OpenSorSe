# Release Status

| Release | Status | Validation | Scope |
| --- | --- | --- | --- |
| v0.1 Foundation | Complete | Restore, build, automated tests, and manual UI validation complete. | Read-only scan pipeline, metadata, hashing, deterministic rules, Dashboard, Settings, Diagnostics, and supporting application infrastructure. |
| v0.2 Results Exploration | Complete | Restore, build, 233 automated tests, and manual UI validation complete. | Immutable result snapshots, Results Explorer, filtering, sorting, paging, details, and exact-duplicate review. |
| v0.3 Local Suggestions and Ranked Exploration | Complete | Clean isolated restore/build and 251 automated tests passed. Existing repository `obj` folders blocked direct in-place validation. | Optional Ollama integration, validated read-only suggestions, local decision history, session tags, deterministic ranked search, and product polish. |

## Current product boundary

OpenSorSe is currently a safe, local-first, read-only file analysis application. It scans selected folders and presents in-memory analysis results; it does not change selected user files.

The current Desktop workflow does not:

- Rename, move, delete, overwrite, or otherwise modify user files.
- Execute planned operations or undo operations.
- Execute AI suggestions, OCR, semantic search, content readers, or automatic organization. Optional local Ollama can generate validated previews only.
- Persist scan results, search indexes, session tags, or result history. It persists settings and separate local AI review decisions only.

## Validation baseline

The v0.3 working tree was restored, built, and tested successfully in a clean isolated copy: 251 tests passed. This environment denied writes to pre-existing generated `obj` folders in the repository itself, so direct in-place restore/build/test failed before compilation. No source or user files were removed to work around that host restriction.

## Documentation status

The architecture directory contains both current implementation documentation and longer-term design material. Documents for Readers, AI, Database, Search, Reports, and Plugins describe future architectural intent unless explicitly identified as implemented in the current release documentation.

## Next release

v0.3 is complete. See [v0.3 implementation specifications](Implementation_Spec/v0.3/00_v0.3_Release_Proposal.md) for the actual optional-AI and ranked-search boundary.
