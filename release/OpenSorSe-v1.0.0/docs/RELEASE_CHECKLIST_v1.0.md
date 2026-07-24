# OpenSorSe 1.0 Release Checklist

This checklist covers preparation of the local v1.0 release candidate. It does not authorize a merge, remote push, GitHub release publication, or installation on another user’s system.

## Repository

- [x] Current branch is `v1.0`.
- [x] The v1.0 branch preserves completed v0.9.1 history.
- [x] `main` has not been merged into or changed by this work.
- [ ] Working tree is intentionally uncommitted for the current release-preparation review.
- [x] No `bin`, `obj`, IDE cache, test result, secret, or local model file is tracked.
- [x] Nothing was pushed without explicit authorization.

## Product behavior

- [x] Results controls remain fixed while result rows scroll.
- [x] Files table/details resizing is bounded, keyboard accessible, and persisted.
- [x] Duplicate details keep groups visible and expose no deletion command.
- [x] AI and Advanced controls remain independent and centrally enforced.
- [x] Enabling AI alone never invokes a provider.
- [x] OCR is local, page-aware, bounded, cancellable, and honestly reports unavailable states.
- [x] Tesseract version and configured English/German language data are validated before recognition.
- [x] AI extracted-text interpretation has a separate default-off gate and remains review-only.
- [x] Metadata readers are defensive and preserve provenance.
- [x] Generated tags remain distinguishable and require review where applicable.
- [x] Meaning Search Beta is local, bounded, cancellable, and explains matches.
- [x] Restructuring remains deterministic, preview-first, root-confined, conflict checked, and separately confirmed.
- [x] AI workflows remain suggestion-only and cannot enter file operations.

## Persistence, privacy, and safety

- [x] Existing v0.9.1 settings load with safe v1.0 defaults.
- [x] New stores are versioned, bounded, atomic, and recover safely from malformed data.
- [x] Existing catalogs, snapshots, tags, and saved searches remain readable.
- [x] Settings and stores contain no credentials or model data.
- [x] OCR, metadata extraction, tagging, and meaning indexing are local.
- [x] Raw OCR/extracted text and vectors are excluded from ordinary logs.
- [x] Rename/folder AI requests exclude file contents.
- [x] Extracted text can be sent only through its separate opt-in and explicit request.
- [x] Source files are opened read-only by scanners and extractors.
- [x] Apply operations reject overwrite, traversal, reparse, changed-preview, and missing-source conditions.
- [x] Duplicate actions do not delete or mutate files.

## Accessibility and usability

- [ ] Keyboard focus order is manually verified.
- [ ] Drawer close and Escape behavior are manually verified.
- [ ] Toggle labels and help text are manually verified with accessibility tooling.
- [ ] Status is manually confirmed not to rely only on color.
- [ ] Long names and paths are manually checked at common window sizes.
- [ ] High-DPI behavior is manually checked at supported Windows scaling levels.
- [x] Large lists, page results, prompts, responses, and diagrams have deterministic bounds.
- [x] Empty, unavailable, cancelled, partial, and failed states have explicit presentation.

## Documentation

- [x] Release proposals, specifications, decisions, migration, architecture, and version notes are current.
- [x] Public README contains official branding, quick links, features, privacy, installation, roadmap, contribution, and license guidance.
- [x] README screenshot slots point to `docs/images/` and contain no generated screenshots or placeholder images.
- [x] Windows install, update, uninstall, checksum, Ollama, and Tesseract guidance is documented.
- [x] OCR/Tesseract and AI limitations are stated accurately.
- [x] FOSS policy, dependency inventory, license, and third-party notices are current.
- [x] No documentation claims autonomous AI filesystem control.

## Windows distribution

- [x] Windows x64 publish is self-contained and untrimmed.
- [x] Public apphost is named `OpenSorSe.exe`.
- [x] File version is `1.0.0.0`; product version is `1.0.0`.
- [x] Official icon and product metadata are embedded.
- [x] README, license, notices, changelog, installation guide, release notes, dependency inventory, and documentation are included.
- [x] Portable ZIP and SHA-256 checksum are generated locally.
- [x] Published executable opens a responsive `OpenSorSe` window.
- [ ] Signed MSIX or conventional installer is available; deferred because identity/signing policy and installer toolchains are absent.

## Automated validation

- [x] `dotnet restore .\OpenSorSe.sln`
- [x] Current-source Debug build succeeds with zero warnings/errors.
- [x] Current-source Debug tests pass.
- [x] Current-source Release build succeeds with zero warnings/errors.
- [x] Current-source Release tests pass.
- [x] Self-contained Windows x64 publish succeeds.
- [x] Package metadata, required files, icon, self-contained runtime, archive, and checksum are inspected.
- [x] Tests run against current source rather than stale binaries.
- [x] `git diff --check`
- [x] Static inspection covers XAML bindings, dependency registration, cancellation, serialization, navigation gates, and file-operation safety.

## Manual release hold

- [ ] All checks in `docs/MANUAL_TESTING_v0.9.1.md` are complete.
- [ ] All checks in `docs/MANUAL_TESTING_v1.0.md` are complete.
- [ ] Scanning is exercised from the packaged executable.
- [ ] Live OCR is exercised on the intended release machine.
- [ ] Live Ollama/File Assistant behavior is exercised with an installed model.
- [ ] Meaning Search, Saved scans, folder plans, and settings persistence are exercised from the package.
- [ ] Supported non-Windows behavior is tested or its limitation remains documented.
- [ ] Manual testing is complete before considering merge into `main` or remote release publication.
