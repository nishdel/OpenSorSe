# System Goals

> These goals guide the current read-only product and future proposals for OpenSorSe.

---

## Current release goals

### 1. Analyze selected folders safely

OpenSorSe discovers files and folders, reads filesystem metadata, calculates hashes, classifies deterministically, and detects exact duplicates without changing selected user files.

### 2. Make completed scans understandable

Users can review a completed in-memory scan through the Results Explorer, filtering, sorting, paging, details, warnings, and exact-duplicate review.

### 3. Preserve user control and privacy

The Desktop workflow is local-first and read-only. It does not execute planned operations, invoke undo, open files, reveal paths, or send scan data to external services.

### 4. Remain reliable and maintainable

The implementation uses clear project boundaries, dependency injection, MVVM, immutable result snapshots, cancellation, user-safe errors, and comprehensive automated tests.

### 5. Keep the UI responsive

Long-running processing reports progress and supports cancellation. Result review uses bounded paging and versioned local query work rather than rendering an unbounded result set.

### 6. Make bounded history understandable

Opt-in catalog snapshots can be named, retain captured source scope, and be compared deterministically from stored metadata. Historical workflows remain bounded, cancellable, and explicitly separate from live filesystem state.

## Future product goals

The project may later explore content readers, OCR, AI assistance, richer search, reporting, plugins, and carefully designed workflow improvements. Those ideas are not current capabilities and must not weaken the safety, privacy, or user-control principles above.

## Current non-goals

The validated v0.9 release does not:

- Modify, delete, move, rename, or overwrite selected user files.
- Perform OCR, semantic search, content extraction, live monitoring, or automatic operation execution. Optional Ollama provides validated previews only.
- Persist unbounded result history, comparison reports, extracted content, or scan/search indexes. The opt-in JSON catalog remains bounded application data.
- Replace the operating system file manager, provide cloud storage, or act as a document editor.

## Related documents

- [System Overview](00_Overview.md)
- [Release Status](../../RELEASE_STATUS.md)
- [Roadmap](../../roadmap.md)
