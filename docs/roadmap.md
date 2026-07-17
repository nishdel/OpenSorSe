# OpenSorSe Roadmap

> This roadmap distinguishes completed releases from future ideas. It is not a commitment to implement every future capability.

## Current direction

OpenSorSe is a local-first, read-only desktop application for analyzing selected folders and reviewing the results safely. The project is implemented in .NET 8, C#, Avalonia UI, and MVVM.

The current application does not modify selected user files and does not provide AI, OCR, semantic search, or content-reader functionality.

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

Planned. The release will focus on improving the existing read-only workflow rather than expanding into unapproved file modification or AI capabilities.

Potential areas for proposal and prioritization include:

- User-interface and accessibility improvements.
- Performance and responsiveness improvements for realistic result sets.
- Better read-only search and duplicate-review workflows.
- User-feedback and workflow clarity improvements.

Specific features will be documented and approved before implementation.

## Future release ideas

The following are longer-term ideas, not current capabilities or committed release scope:

- PDF, DOCX, and Excel content readers.
- OCR.
- Local or optional AI providers.
- Rename suggestions and folder suggestions.
- Semantic search.
- Automatic tagging.
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
