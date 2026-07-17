# Component Map

> The map below reflects the implemented v0.3 component relationships. Longer-term components are listed separately as future design intent.

---

## Implemented components

```mermaid
flowchart TB
    Desktop["OpenSorSe.Desktop\nAvalonia + MVVM"]
    Application["OpenSorSe.Application\nOrchestration + sessions + result snapshots"]
    AI["OpenSorSe.AI\nOllama transport + local decision history"]
    Core["OpenSorSe.Core\nConfiguration + logging + lifecycle"]
    Scanner["OpenSorSe.Scanner\nRead-only analysis"]
    Rules["OpenSorSe.Rules\nEvaluation + planning"]
    Executor["OpenSorSe.Executor\nNot exposed by Desktop"]

    Desktop --> Application
    Desktop --> AI
    Desktop --> Core
    Application --> Scanner
    Application --> Rules
    Application --> Core
    AI --> Application
    AI --> Core
    Rules -. produces planned operations .-> Executor
```

| Component | Implemented responsibility | Current safety boundary |
| --- | --- | --- |
| Desktop | Presents scan and review workflows, including Results Explorer and exact-duplicate review. | Contains no file-operation control. |
| Application | Coordinates the completed processing pipeline and projects immutable in-memory results. | Does not persist results or access files outside the scanner pipeline. |
| AI | Implements optional Ollama transport and local decision-history persistence behind application-owned contracts. | Does not expose transport DTOs to Desktop and cannot mutate files. |
| Scanner | Traverses selected folders, reads metadata, hashes files, classifies deterministically, and detects exact duplicates. | Read-only filesystem access. |
| Rules | Evaluates supplied rules and produces display-only plans and conflict resolution. | Does not execute plans. |
| Core | Provides shared infrastructure and local application configuration/logging support. | Does not create a user-file mutation path. |
| Executor | Contains execution and undo infrastructure from the foundation work. | Not invoked or surfaced by the validated Desktop workflow. |

## Future design areas

Readers, Database, Reports, and Plugins remain future architectural design areas. Search and AI have only the narrow v0.3 capabilities documented in their subsystem overviews: metadata-aware in-memory ranked search and optional validated Ollama suggestions. They are not content-reader, persistent-index, or semantic-search implementations.

Future additions should use the implemented boundaries above rather than bypassing the Application layer or coupling UI code directly to scanner models.

## Related documents

- [System Overview](00_Overview.md)
- [Data Flow](04_Data_Flow.md)
- [Release Status](../../RELEASE_STATUS.md)
