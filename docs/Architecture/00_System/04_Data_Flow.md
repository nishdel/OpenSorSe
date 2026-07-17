# Data Flow

> This document describes the current read-only v0.2 data flow. Future architecture documents for storage, readers, AI, and search are not part of this flow.

---

## Implemented flow

```mermaid
flowchart LR
    User["Selected local folders"] --> Scan["Scanner\nDiscovery + metadata"]
    Scan --> Hash["SHA-256 hashing"]
    Hash --> Classify["Deterministic classification"]
    Classify --> Duplicates["Exact duplicate detection"]
    Duplicates --> Rules["Rule evaluation + planning"]
    Rules --> Snapshot["Immutable in-memory results snapshot"]
    Snapshot --> Explorer["Results Explorer + duplicate review"]
```

## Data ownership and lifetime

| Data | Producer | Consumer | Lifetime |
| --- | --- | --- | --- |
| Scan entries and recoverable issues | Scanner | Application pipeline | Processing session. |
| Metadata, hashes, classifications, and duplicate groups | Scanner enrichers | Application pipeline and results snapshot | Processing session. |
| Rule decisions and planned operations | Rules | Results snapshot | Processing session; display-only in Desktop. |
| Results snapshot | Application | Desktop Results Explorer and duplicate review | Process memory until replaced or application exit. |
| Settings and diagnostic logs | Core | Desktop and supporting services | Local application-data scope, independent of scan results. |

## Safety constraints

- The Scanner reads selected filesystem information; it does not modify selected user files.
- The Results Explorer filters and sorts already-projected in-memory data; it does not rescan, open, reveal, or validate paths.
- Duplicate review consumes existing exact-hash groups and does not recalculate hashes or recommend deletion.
- The current Desktop workflow does not invoke `IActionExecutor` or `IUndoEngine`.

## Deferred flows

Persistent databases, content readers, OCR, AI, keyword or semantic search indexes, reporting, and plugins are not part of the current flow. Their architecture documents describe possible future work only.

## Related documents

- [System Overview](00_Overview.md)
- [Component Map](03_Component_Map.md)
- [Results Explorer Specification](../../Implementation_Spec/v0.2/030_ResultsExplorer.md)
