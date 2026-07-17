# OpenSorSe Roadmap

> This roadmap distinguishes completed releases from future ideas. It is not a commitment to implement every future capability.

## Current direction

OpenSorSe is a local-first, read-only desktop application for analyzing selected folders and reviewing the results safely. The project is implemented in .NET 8, C#, Avalonia UI, and MVVM.

The current application does not modify selected user files. It provides optional local Ollama suggestions and metadata-aware ranked search, but not OCR, content readers, embeddings, or semantic search.

## Completed releases

### v0.1 — Read-only processing foundation

Complete.

- Folder scanning and recursive directory traversal.
- Read-only metadata extraction and SHA-256 hashing.
- Deterministic file classification and exact duplicate detection.
- Rule evaluation, planning, and conflict-resolution infrastructure.
- Dashboard, Scan workflow, Settings, Diagnostics, and Operation History surfaces.
- Application orchestration, in-memory sessions, logging, error handling, and automated test coverage.

### v0.2 — Read-only results exploration

Complete.

- Immutable in-memory results snapshots.
- Results Explorer with text filtering, categorical filters, deterministic sorting, and bounded paging.
- Read-only result details.
- Exact SHA-256 duplicate-group review and theoretical reclaimable-space presentation.
- Safe group-to-results navigation and clear empty, loading, limitation, and error states.

## v0.3 — Usability improvements and workflow enhancements

Implemented. v0.3 retains the read-only workflow while adding optional local assistance only through validated, review-only proposals.

- Optional Ollama endpoint configuration, health check, installed-model discovery, model selection, timeout, cancellation, redacted diagnostics, and safe unavailable states.
- Validated rename, tag, category, destination, and bounded folder-structure previews using minimal metadata context.
- Local JSON decision history and optional approved-pattern reuse; no model training or fine-tuning.
- Application-owned in-memory tags and deterministic ranked search over filename, path, extension, deterministic category, and accepted tags.
- Results and Settings product-polish pass with match explanations, explicit safety text, and clearer status/disabled states.

The release does not enable filesystem mutation, persistent scan catalogs, content extraction, embedding-based semantic search, or automatic organization.

## Future release ideas

The following are longer-term ideas, not current capabilities or committed release scope:

- PDF, DOCX, and Excel content readers.
- OCR.
- Content readers and OCR.
- Embedding-based semantic search and vector indexes.
- Persistent scan catalogs and tags.
- Safe, explicitly authorized operation execution.
- Plugin system.

Any future feature that could modify user files requires a separate safety design covering explicit authorization, live preflight, preview, failure handling, and recovery expectations.

## Release principles

- Preserve the current read-only safety boundary unless a dedicated release explicitly changes it.
- Keep local-first privacy and transparent user control central to every proposal.
- Treat the architecture documents for unimplemented subsystems as design intent, not evidence of a shipped feature.
- Validate build, tests, and relevant UI behavior before declaring a release complete.

## Related documentation

- [Release Status](RELEASE_STATUS.md)
- [Project Philosophy](project_philosophy.md)
- [System Overview](Architecture/00_System/00_Overview.md)
- [Technology Stack](Architecture/99_Appendix/Technology_Stack.md)
