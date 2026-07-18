# OpenSorSe

> Open Sort & Search — a local-first, read-only desktop tool for analyzing selected folders safely.

## Project status

OpenSorSe v0.9 is implemented on the local `v0.9` branch.

- Clean isolated restore and build succeed.
- The automated suite contains 330 passing tests.
- In-place validation is blocked by this workspace's access-denied generated `obj` folders; no source files were removed to work around it.
- The Desktop workflow remains read-only with respect to selected user files.

OpenSorSe does not rename, move, delete, overwrite, or otherwise modify selected user files. Optional Ollama-compatible suggestions are review-only and use only an explicitly configured endpoint. The opt-in local catalog stores display-safe snapshot metadata in OpenSorSe application data only; its remove/clear controls never affect selected user folders. See [Safety and Privacy](docs/SAFETY_AND_PRIVACY.md) for the complete storage, network, and dormant-executor boundary.

## What the application does today

| Area | Available in v0.9 |
| --- | --- |
| Folder analysis | Select local folders, traverse recursively, report progress, continue after recoverable issues, and support cancellation. |
| File analysis | Read filesystem metadata, calculate SHA-256 hashes, apply deterministic classification, and detect exact duplicates. |
| Results review | Explore one completed in-memory scan with filtering, sorting, bounded paging, and read-only file details. |
| Duplicate review | Inspect exact SHA-256 duplicate groups and a conservative theoretical reclaimable-space estimate without exposing hashes or recommending an action. |
| Ranked search | Search filenames, paths, extensions, deterministic categories, and accepted session tags with deterministic ranking and match explanations. This is not semantic search. |
| Saved catalog | Opt in to retain up to ten bounded completed snapshots locally, label them, review captured source scope, reopen them after restart, search their stored metadata, and explicitly remove application-owned catalog entries. |
| User-managed tags | Add or remove up to twelve accepted OpenSorSe metadata tags for a selected result without AI or any file-metadata change; catalog-backed tags remain searchable after restart. |
| Saved searches | Retain up to 25 named catalog query presets locally, rerun them against current snapshots, and explicitly remove or reset query text without persisting hits. |
| Snapshot comparison | Select two distinct saved snapshots and review added, removed, modified, and unchanged stored metadata with scope warnings, tag-change detection, cancellation, filters, and bounded rows. No stored path is accessed. |
| Optional AI | Test an explicitly configured Ollama-compatible endpoint, discover/select a model, and request validated rename, tag, category, destination, and bounded folder-structure previews. The default endpoint is local; a custom endpoint can be remote. |
| Preference adaptation | Record local accept/reject/edit decisions and optionally reuse concise approved patterns. It does not train or fine-tune a model. |
| Application workflow | Use Dashboard, Scan, Results, Saved catalog, Catalog search, Compare snapshots, Settings, Diagnostics, and About. Rules and Operation history are clearly labelled review-only foundations with no current creation/execution workflow. |
| Safety | Avoid execution, shell-launch, and any modification of selected user files. Catalog persistence is local, opt-in, bounded, and separate from user folders. |

Planned operations and AI suggestions are presentation-only in the Desktop application. Rules and Operation history accept only caller-supplied in-memory data; the production shell supplies none and exposes no execution or undo action.

## Technology stack

- .NET 8 target framework and C#.
- Avalonia UI desktop application.
- MVVM with CommunityToolkit.Mvvm.
- Microsoft dependency injection and logging abstractions.
- xUnit automated tests.

The repository currently pins the .NET SDK in [global.json](global.json). See the [technology stack document](docs/Architecture/99_Appendix/Technology_Stack.md) for current and future technology boundaries.

## Build from source

There is no installer or published package in this repository. To build from source, install the SDK version specified in [global.json](global.json), then run:

```powershell
dotnet restore .\OpenSorSe.sln
dotnet build .\OpenSorSe.sln --configuration Debug --no-restore
dotnet test .\OpenSorSe.sln --configuration Debug --no-build
dotnet run --project .\src\OpenSorSe.Desktop\OpenSorSe.Desktop.csproj
```

For a Release build:

```powershell
dotnet build .\OpenSorSe.sln --configuration Release
```

## Project structure

```text
src/
  OpenSorSe.Core/          Shared infrastructure, configuration, events, logging, state, and tasks
  OpenSorSe.Scanner/       Read-only scan, metadata, hashing, classification, and duplicate detection
  OpenSorSe.Rules/         Deterministic rule evaluation, planning, and conflict resolution
  OpenSorSe.Executor/      Dormant historical execution/undo components; not registered by the current Desktop
  OpenSorSe.Application/   Read-only processing orchestration, sessions, and result snapshots
  OpenSorSe.AI/            Ollama HTTP transport and local decision-history persistence
  OpenSorSe.Desktop/       Avalonia UI and MVVM presentation layer
tests/                     Automated unit and integration tests for each implemented project
docs/                      Architecture, specifications, release status, and roadmap documentation
```

## Release roadmap

| Release | Status | Scope |
| --- | --- | --- |
| v0.1 Foundation | Complete | Scanning, metadata, hashing, deterministic rules, configuration, diagnostics, and Dashboard foundation. |
| v0.2 Results Exploration | Complete | Results Explorer, filtering, sorting, paging, details, and exact-duplicate review. |
| v0.3 | Complete | Optional local Ollama suggestions, local decision history, metadata-aware ranked search, session tags, and product-quality improvements while preserving read-only safety. |
| v0.4 Local Catalog | Complete | Opt-in bounded JSON persistence for display-safe completed snapshots and accepted tags, plus historical snapshot review. |
| v0.5 Catalog Search and Maintenance | Complete | Deterministic catalog-wide metadata search, historical-hit opening, selected-entry removal, and two-step clear of application-owned catalog data. |
| v0.6 User-Managed Tags | Complete | Explicit bounded add/remove controls for application-owned result tags, immediate ranked-search integration, and existing opt-in catalog persistence. |
| v0.7 Saved Catalog Searches | Complete | Atomic bounded named query persistence, explicit rerun/remove/reset workflows, corruption recovery, and no stored hits. |
| v0.8 Snapshot Identity and Scope | Complete | Backward-compatible catalog schema 2, optional snapshot names, captured source roots, and explicit legacy unknown-scope behavior. |
| v0.9 Historical Snapshot Comparison | Complete | Deterministic bounded metadata/tag comparison, scope compatibility, filters, cancellation, and baseline/current historical opening. |
| Future releases | Ideas only | Content readers, OCR, embedding-based semantic search, database-backed indexes, safe execution workflows, and plugins. |

See the [roadmap](docs/roadmap.md) and [release status](docs/RELEASE_STATUS.md) for details.

## Manual validation checklist

1. Launch the desktop application and verify that the main window opens.
2. Scan a small, non-critical local folder and confirm progress, results, and warnings are understandable.
3. Filter, sort, page, use ranked search, and inspect completed results without opening or changing a file.
4. Use a controlled duplicate fixture to review exact duplicate groups and return to their filtered result rows.
5. Cancel a second scan and confirm the application remains responsive.
6. Select a result, add a user tag, search for it, remove it, and confirm the selected file metadata did not change.
7. In Settings, enable the local catalog, complete a small test scan, reopen it from **Saved catalog**, and search it from **Catalog search**. Confirm results are explicitly historical.
8. Save a named catalog query, restart, refresh saved searches, and rerun it. Confirm query text—but no hits—is retained.
9. On disposable application data, remove one saved snapshot and use the separate confirmation actions to clear catalog snapshots and reset saved-query text; confirm no selected user file changed.
10. Compare a before/after manifest of the test folder and confirm that no selected user file changed.
11. Name a saved snapshot, restart, and confirm its name and captured source roots remain visible; clear the name and confirm the snapshot remains.
12. Save a second scan of the same disposable roots, open **Compare snapshots**, select distinct baseline/current entries, and verify change totals, source-scope text, filters, cancellation, and historical opening.

## Optional Ollama setup

Ollama is optional. Open **Settings**, enable optional AI assistance, keep the default local endpoint (`http://127.0.0.1:11434`) or enter your own endpoint, use **Test connection**, then **Discover models**, select a model, and save Settings.

When Ollama is unavailable, disabled, has no models, times out, is cancelled, or returns invalid JSON, scanning and every non-AI feature continue to work. Suggestions are validated previews only: accepting, rejecting, or editing one records a local decision but never changes a file or folder.

The default endpoint is local. A custom remote endpoint can receive only the bounded request metadata OpenSorSe supplies (filenames, extensions, deterministic categories, selected folder names, and concise local preferences); it never sends document contents. See the [v0.3 specifications](docs/Implementation_Spec/v0.3/00_v0.3_Release_Proposal.md) for the exact boundary.

## Contributing

Keep contributions focused, preserve the read-only safety boundary, update affected documentation, and run the build and test commands above. v0.9 authorizes only bounded OpenSorSe catalog identity, source provenance, and in-memory historical comparison on top of v0.7; it does not authorize live verification, file modification, OCR, semantic search, monitoring, report export, or automatic execution.

## License

OpenSorSe is licensed under the [MIT License](LICENSE).
