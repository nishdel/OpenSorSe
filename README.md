# OpenSorSe

> Open Sort & Search — a local-first, read-only desktop tool for analyzing selected folders safely.

## Project status

OpenSorSe v0.3 is implemented on the local `v0.3` branch.

- Clean isolated restore and build succeed.
- The automated suite contains 251 passing tests.
- In-place validation is blocked by this workspace's access-denied generated `obj` folders; no source files were removed to work around it.
- The Desktop workflow remains read-only with respect to selected user files.

OpenSorSe does not rename, move, delete, overwrite, or otherwise modify selected user files. Optional local Ollama suggestions are review-only; the release does not run OCR, semantic search, content readers, or automatic organization.

## What the application does today

| Area | Available in v0.3 |
| --- | --- |
| Folder analysis | Select local folders, traverse recursively, report progress, continue after recoverable issues, and support cancellation. |
| File analysis | Read filesystem metadata, calculate SHA-256 hashes, apply deterministic classification, and detect exact duplicates. |
| Results review | Explore one completed in-memory scan with filtering, sorting, bounded paging, and read-only file details. |
| Duplicate review | Inspect exact SHA-256 duplicate groups and a conservative theoretical reclaimable-space estimate without exposing hashes or recommending an action. |
| Ranked search | Search filenames, paths, extensions, deterministic categories, and accepted session tags with deterministic ranking and match explanations. This is not semantic search. |
| Optional local AI | Test an Ollama endpoint, discover/select a model, and request validated rename, tag, category, destination, and bounded folder-structure previews. |
| Preference adaptation | Record local accept/reject/edit decisions and optionally reuse concise approved patterns. It does not train or fine-tune a model. |
| Application workflow | Use the Dashboard, Scan, Results, Rules, Settings, Diagnostics, and Operation History surfaces. |
| Safety | Keep scan results process-local and avoid execution, shell-launch, persistence of results, or file-modification controls. |

Planned operations, AI suggestions, and operation-history information are presentation-only in the Desktop application; they are not executed from the current workflow.

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
  OpenSorSe.Executor/      Execution and undo components; not exposed by the current Desktop workflow
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
| v0.3 | Implemented | Optional local Ollama suggestions, local decision history, metadata-aware ranked search, session tags, and product-quality improvements while preserving read-only safety. |
| Future releases | Ideas only | Content readers, OCR, embedding-based semantic search, persistent catalogs/tags, safe execution workflows, and plugins. |

See the [roadmap](docs/roadmap.md) and [release status](docs/RELEASE_STATUS.md) for details.

## Manual validation checklist

1. Launch the desktop application and verify that the main window opens.
2. Scan a small, non-critical local folder and confirm progress, results, and warnings are understandable.
3. Filter, sort, page, use ranked search, and inspect completed results without opening or changing a file.
4. Use a controlled duplicate fixture to review exact duplicate groups and return to their filtered result rows.
5. Cancel a second scan and confirm the application remains responsive.
6. Compare a before/after manifest of the test folder and confirm that no selected user file changed.

## Optional Ollama setup

Ollama is optional. Open **Settings**, enable optional AI assistance, keep the default local endpoint (`http://127.0.0.1:11434`) or enter your own endpoint, use **Test connection**, then **Discover models**, select a model, and save Settings.

When Ollama is unavailable, disabled, has no models, times out, is cancelled, or returns invalid JSON, scanning and every non-AI feature continue to work. Suggestions are validated previews only: accepting, rejecting, or editing one records a local decision but never changes a file or folder.

The default endpoint is local. A custom remote endpoint can receive only the bounded request metadata OpenSorSe supplies (filenames, extensions, deterministic categories, selected folder names, and concise local preferences); it never sends document contents. See the [v0.3 specifications](docs/Implementation_Spec/v0.3/00_v0.3_Release_Proposal.md) for the exact boundary.

## Contributing

Keep contributions focused, preserve the read-only safety boundary, update affected documentation, and run the build and test commands above. v0.3 authorizes only the optional, validated, review-only Ollama integration documented here; it does not authorize file modification, OCR, semantic search, or automatic execution.

## License

OpenSorSe is licensed under the [MIT License](LICENSE).
