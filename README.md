# OpenSorSe

> Open Sort & Search — a local-first, review-oriented desktop tool for understanding and organizing selected folders safely.

## Project status

OpenSorSe 1.0.0 is implemented on the local `v1.0` branch as a final manual-validation candidate. It builds on the completed v0.9.1 baseline and adds local metadata extraction, page-aware optional OCR Beta for images and PDFs, provenance-aware tags, local Semantic Search Beta, and durable folder-structure history.

- AI and Advanced interface features remain independently disabled by default.
- OCR and Semantic Search are separate opt-ins; neither requires or activates AI.
- Scanning, duplicate review, extraction, indexing, diagrams, and AI suggestions do not modify source files.
- AI remains suggestion-only and blocked from provider communication when disabled. Rename/folder prompts are metadata-only; extracted document text requires a separate default-off capability and an explicit one-file request.
- The only new source-file mutation is a deterministic folder-restructuring plan that the user previews and then confirms separately. It is confined to one root, bounded, conflict checked, never overwrites or deletes, and records its result.
- Restore, build, test, and environment results are recorded in [Release Status](docs/RELEASE_STATUS.md).

Do not merge the release candidate into `main` until the inherited v0.9.1 and complete [v1.0 manual checklist](docs/MANUAL_TESTING_v1.0.md) have been completed.

## What 1.0 provides

| Area | Implemented behavior |
| --- | --- |
| Folder analysis | Select roots, scan recursively, report progress, isolate recoverable errors, support cancellation, hash supported files, classify metadata, and detect exact duplicates. Scanning is read-only. |
| Results | Keep search/filter/status controls fixed while independently scrolling bounded result rows; retain sorting, filters, paging, details, tags, and match explanations. |
| Duplicate View | Keep duplicate groups visible while selected-group details open in a responsive right drawer. Known paths can be opened through a capped launcher; no delete or cleanup command exists. |
| Metadata | Extract bounded filesystem metadata, defensive PDF fields/native text, Open XML document metadata/native text, and image dimensions with provenance. Malformed files fail per item. |
| OCR Beta | Use built-in bounded PDFium rendering plus an optional detected local Tesseract CLI for PNG, JPEG, TIFF, scanned PDFs, and only the insufficient pages of mixed PDFs. Native PDF text is extracted page by page and retained when reliable. |
| Tags | Distinguish user-approved, deterministic, embedded-metadata, OCR, semantic, file-type, date, folder-context, AI, and preference provenance. Generated candidates can be accepted or rejected locally. |
| Semantic Search Beta | Build a bounded, local, rebuildable index using deterministic feature hashing and hybrid filename/tag/metadata/native-text/OCR signals. Results explain why they matched; indexes never modify source files. |
| Saved catalog | Opt in to bounded historical snapshots, names, source scope, accepted tags, saved searches, and deterministic snapshot comparison. |
| Optional AI | Explicitly enable metadata-only rename/folder proposals or, through a separate default-off control, bounded extracted-text interpretation for one selected indexed document. Strict structured validation keeps every result unverified and review-only. |
| Structure history | Preview deterministic extension-group proposals, review source/proposed/applied/current structures, inspect Added/Removed/Moved/Renamed/Unchanged labels, and retain bounded local history. |
| Repeat protection | Only a successful confirmed restructuring apply suppresses a redundant full proposal. New root-level files receive an incremental proposal; material changes are reported; an explicit override remains available. |
| Interface modes | Global shell switches keep AI and Advanced controls available from every page. Advanced mode adds comparison, diagnostics, operation internals, and Structure history without resetting hidden values. |
| Help and diagnostics | Every major page has local contextual Help. Diagnostics and raw AI request capture remain bounded, separated, and privacy-aware. |

## Safety and privacy

OpenSorSe prefers metadata-only, local processing and treats model output and malformed documents as untrusted input.

- AI is disabled by default. Disabled AI blocks provider discovery and requests at the service boundary.
- OCR, extracted text, semantic vectors, and structure history stay in OpenSorSe application data.
- Ordinary logs do not include raw OCR/document text, vectors, credentials, or raw AI payloads.
- A custom Ollama endpoint can be remote. Rename/folder requests contain bounded metadata; extracted text is sent only after the separate document-text capability and an explicit one-file request are enabled.
- AI suggestions never enter the restructuring executor. Folder restructuring is deterministic and separately confirmed.
- Every restructuring destination must remain under the selected root; traversal, reparse destinations, missing sources, changed previews, conflicts, duplicates, and overwrites are rejected.
- Clear-index, clear-cache, clear-history, catalog, saved-search, and diagnostic actions affect only OpenSorSe-owned data.

See [Safety and Privacy](docs/SAFETY_AND_PRIVACY.md) and the [final v1.0 completion specification](docs/Implementation_Spec/v1.0/049_Final_Product_Completion.md).

## Technology

- .NET 8 and C#.
- Avalonia UI with MVVM and CommunityToolkit.Mvvm.
- Microsoft dependency injection and logging.
- PdfPig for page-aware native PDF text and PDFtoImage/PDFium for bounded page rendering.
- An optional externally installed Tesseract 5 CLI with `eng` and/or `deu` language data.
- Atomic bounded JSON stores for settings, catalog/search definitions, content cache, semantic index, AI decisions, and structure history.
- xUnit tests with fake providers/engines and disposable filesystem fixtures.

The SDK is pinned in [global.json](global.json). No installer or published package is included. Dependency policy, exact resolved versions, and redistribution notes are recorded in [the FOSS policy](docs/FOSS_DEPENDENCY_POLICY.md), [machine-readable inventory](docs/dependency-licenses.json), and [third-party notices](THIRD_PARTY_NOTICES.md).

## Build from source

```powershell
dotnet restore .\OpenSorSe.sln
dotnet build .\OpenSorSe.sln --configuration Debug --no-restore
dotnet test .\OpenSorSe.sln --configuration Debug --no-build
dotnet run --project .\src\OpenSorSe.Desktop\OpenSorSe.Desktop.csproj
```

Some existing checkout-generated `bin`/`obj` files can be host-write-protected. The repository-local ignored validation route is:

```powershell
dotnet restore .\OpenSorSe.sln --artifacts-path .\.artifacts
dotnet build .\OpenSorSe.sln --configuration Debug --no-restore --artifacts-path .\.artifacts
dotnet test .\OpenSorSe.sln --configuration Debug --no-build --artifacts-path .\.artifacts
```

For a current-source release build:

```powershell
dotnet build .\OpenSorSe.sln --configuration Release --no-restore --artifacts-path .\.artifacts
```

## Project structure

```text
src/
  OpenSorSe.Core/          Configuration, logging, lifecycle, events, state, and tasks
  OpenSorSe.Scanner/       Read-only discovery, metadata, hashing, classification, and duplicates
  OpenSorSe.Rules/         Deterministic rule evaluation, planning, and conflict resolution
  OpenSorSe.Executor/      Historical generic executor/undo library; not registered in the Desktop
  OpenSorSe.Application/   Orchestration, results, extraction, tags, semantic index, and structure history
  OpenSorSe.AI/            Ollama transport and bounded decision-history persistence
  OpenSorSe.Desktop/       Avalonia views, ViewModels, navigation, Help, and composition
tests/                     Automated tests for every project
docs/                      Architecture, specifications, migration, safety, release, and manual validation
```

## Release history

Releases v0.1 through v0.9.1 are complete and preserved. OpenSorSe 1.0 integrates the fixed Results toolbar, Duplicate drawer, global shell toggles, local content pipeline, provenance tags, Semantic Search Beta, and Structure history. Plugins, broad localization, packaging/release automation, cloud indexing, live monitoring, reports/export, autonomous AI operations, and general rule execution remain deferred.

See the [roadmap](docs/roadmap.md), [changelog](docs/CHANGELOG.md), [version notes](docs/VERSION_NOTES_v1.0.md), and [release checklist](docs/RELEASE_CHECKLIST_v1.0.md).

## Manual verification

Use disposable data for restructuring tests. The [v1.0 guide](docs/MANUAL_TESTING_v1.0.md) covers Results, the Duplicate drawer, global toggles, mixed/scanned-PDF OCR, metadata, optional AI text interpretation, tags, semantic search, restructuring protection, diagrams, migration, and inherited regressions. Also complete the [v0.9.1 checklist](docs/MANUAL_TESTING_v0.9.1.md).

## Optional Ollama

Ollama is optional and externally managed. Enable AI, check the configured endpoint, discover exact installed model identifiers, select one, enable an individual capability, and save. Enabling the shell switch alone performs no provider detection or communication. Essential endpoint/model controls remain available with AI; raw request diagnostics additionally require Advanced mode and explicit opt-in. The extracted-document-text capability is separate from rename/folder suggestions and warns when the configured endpoint is not local.

Unavailable endpoints/models, timeout, cancellation, malformed or oversized output, and unsafe values return controlled states without affecting scanning or other non-AI features. The local deterministic Semantic Search Beta and folder restructuring service do not use Ollama.

## License

OpenSorSe is licensed under the [MIT License](LICENSE).
