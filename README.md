# OpenSorSe

> Open Sort & Search — a local-first, read-only desktop tool for analyzing selected folders safely.

## Project status

OpenSorSe v0.1 Foundation and v0.2 Results Exploration are complete on the validated `coding/v0.2` branch.

- .NET restore and build succeed.
- The automated suite contains 233 passing tests.
- Manual UI validation has been completed.
- The Desktop workflow remains read-only with respect to selected user files.

OpenSorSe does not rename, move, delete, overwrite, or otherwise modify selected user files. It does not run AI, OCR, semantic search, or content readers in the current release.

## What the application does today

| Area | Available in v0.2 |
| --- | --- |
| Folder analysis | Select local folders, traverse recursively, report progress, continue after recoverable issues, and support cancellation. |
| File analysis | Read filesystem metadata, calculate SHA-256 hashes, apply deterministic classification, and detect exact duplicates. |
| Results review | Explore one completed in-memory scan with filtering, sorting, bounded paging, and read-only file details. |
| Duplicate review | Inspect exact SHA-256 duplicate groups and a conservative theoretical reclaimable-space estimate without exposing hashes or recommending an action. |
| Application workflow | Use the Dashboard, Scan, Results, Rules, Settings, Diagnostics, and Operation History surfaces. |
| Safety | Keep scan results process-local and avoid execution, shell-launch, persistence of results, or file-modification controls. |

Planned operations and operation-history information are presentation-only in the Desktop application; they are not executed from the current workflow.

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
  OpenSorSe.Desktop/       Avalonia UI and MVVM presentation layer
tests/                     Automated unit and integration tests for each implemented project
docs/                      Architecture, specifications, release status, and roadmap documentation
```

## Release roadmap

| Release | Status | Scope |
| --- | --- | --- |
| v0.1 Foundation | Complete | Scanning, metadata, hashing, deterministic rules, configuration, diagnostics, and Dashboard foundation. |
| v0.2 Results Exploration | Complete | Results Explorer, filtering, sorting, paging, details, and exact-duplicate review. |
| v0.3 | Planned | Usability improvements and workflow enhancements, scoped through future proposals. |
| Future releases | Ideas only | Content readers, OCR, AI providers, rename or folder suggestions, semantic search, automatic tagging, and plugins. |

See the [roadmap](docs/roadmap.md) and [release status](docs/RELEASE_STATUS.md) for details.

## Manual validation checklist

1. Launch the desktop application and verify that the main window opens.
2. Scan a small, non-critical local folder and confirm progress, results, and warnings are understandable.
3. Filter, sort, page, and inspect completed results without opening or changing a file.
4. Use a controlled duplicate fixture to review exact duplicate groups and return to their filtered result rows.
5. Cancel a second scan and confirm the application remains responsive.
6. Compare a before/after manifest of the test folder and confirm that no selected user file changed.

## Contributing

Keep contributions focused, preserve the read-only safety boundary, update affected documentation, and run the build and test commands above. The current release does not authorize file-modification, AI, OCR, or semantic-search features without a dedicated proposal.

## License

OpenSorSe is licensed under the [MIT License](LICENSE).
