# Technology Stack

> This document distinguishes technologies used by the v0.9.1 release from longer-term technology ideas.

---

## Implemented stack

| Area | Technology | Current use |
| --- | --- | --- |
| Runtime | .NET 8 target framework | All production and test projects target `net8.0`. |
| Language | C# | Primary implementation language. |
| Desktop UI | Avalonia UI | Cross-platform-capable desktop presentation framework. |
| Presentation pattern | MVVM | Separates view state and commands from application and scanner logic. |
| MVVM support | CommunityToolkit.Mvvm | Observable view models and commands. |
| Infrastructure | Microsoft.Extensions dependency injection and logging | Service composition and diagnostic logging. |
| Testing | xUnit, Microsoft.NET.Test.Sdk, coverlet collector | Automated unit and integration test coverage. |
| Documentation | Markdown and Mermaid | Repository documentation and architecture diagrams. |
| Version control | Git | Local repository history and collaboration workflow. |

The repository pins an SDK version in the root [global.json](../../../global.json). The pinned SDK may be newer than the .NET 8 target framework; the application runtime target remains .NET 8.

## Current non-adoptions

The v0.9.1 release does not use the following as implemented product capabilities:

- SQLite or another database; bounded schema-2 scan snapshots and named query text use separate JSON stores, while comparison is in memory.
- Full-text or vector search.
- Embeddings or cloud AI APIs. Ollama is the single optional provider for validated review-only suggestions.
- OCR or format-specific content readers.
- Plugin loading or a plugin marketplace.
- Python, PySide, or other legacy desktop stack components.

## Future technology considerations

Readers, OCR, additional AI providers, database/index storage, semantic search, reports, and plugins are future architectural ideas. Deterministic metadata search and bounded JSON application data are implemented. A technology named in a future architecture document is not a dependency or delivered capability until a dedicated proposal and implementation add it.

Future technology selection should continue to prioritize local-first privacy, user control, maintainability, and explicit safety boundaries for any feature that could affect user files.

## Related documents

- [System Overview](../00_System/00_Overview.md)
- [Release Status](../../RELEASE_STATUS.md)
- [Roadmap](../../roadmap.md)
