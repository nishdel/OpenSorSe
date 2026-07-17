# OpenSorSe

OpenSorSe is a local-first Avalonia desktop application for inspecting selected folders safely. The v0.2 desktop workflow discovers files and folders, reads filesystem metadata, calculates SHA-256 hashes, applies deterministic classification and duplicate analysis, and provides bounded read-only exploration of completed results without changing the selected files.

## v0.2 scope

- Select one or more accessible local folders with the native folder picker or an absolute path.
- Recursively discover files and directories across multiple roots.
- Skip symbolic links, junctions, and other reparse points.
- Report progress and allow cancellation.
- Continue after recoverable filesystem issues and present user-safe status.
- Read filesystem metadata, hash regular files, classify by metadata, and detect exact SHA-256 duplicates.
- Display discovered files, directories, warnings, duplicate information, and read-only planned-operation results.
- Filter, sort, page, and inspect one completed in-memory scan without rescanning.
- Review exact SHA-256 duplicate groups and their historical theoretical reclaimable-space estimate without displaying hashes or recommending an action.

The v0.1 desktop UI is intentionally read-only. It does not invoke the executor, move, rename, delete, overwrite, or modify selected user files. Configuration and optional local log files are stored only under the application's local-data directory.

## Prerequisites

- .NET SDK `9.0.315` (pinned in [global.json](global.json)); the projects target `net8.0`.
- A supported Avalonia desktop platform. The current environment validates Windows; other platforms require local verification.

## Build, test, and run

From the repository root:

```powershell
dotnet restore .\OpenSorSe.sln
dotnet build .\OpenSorSe.sln --configuration Debug --no-restore
dotnet test .\OpenSorSe.sln --configuration Debug --no-build
dotnet run --project .\src\OpenSorSe.Desktop\OpenSorSe.Desktop.csproj
```

Create a Release build with:

```powershell
dotnet build .\OpenSorSe.sln --configuration Release
```

## Manual smoke test

1. Launch the desktop application and verify the main window opens without an error.
2. Open **Scan**, choose a small non-critical folder with **Browse...**, and start the scan.
3. Confirm progress changes, then filter, sort, page, and inspect the completed results without opening or changing a file.
4. For a controlled duplicate fixture, open **Exact duplicates**, inspect group members, and return to the filtered group files.
5. Start a second scan and use **Cancel scan** while it is active.
6. Confirm the application remains responsive, closes cleanly, and no source file in the selected test folder changed.

## Current limitations

- No AI classification, semantic search, embedded-document readers, rule persistence, or execution UI is included in this release workflow.
- Planned operations are displayed for review only; the desktop app does not execute them.
- A native folder picker requires a platform storage provider. Absolute local paths remain available when it is unavailable.

## Contributing

Keep changes focused, preserve the read-only scan boundary, and run the build and test commands above before opening a pull request.

## License

OpenSorSe is licensed under the [MIT License](LICENSE).
