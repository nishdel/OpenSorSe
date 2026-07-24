# OpenSorSe 1.0 Release Checklist

This checklist covers preparation of the local v1.0 release candidate. It does not authorize a merge, remote push, package publication, or installation on a user system.

## Repository

- [x] Current branch is `v1.0`.
- [x] The v1.0 branch was created from completed v0.9.1 state.
- [x] `main` has not been merged into or changed by this work.
- [x] Existing v0.9.1 work and history remain intact.
- [x] Working tree is clean after the final documentation commit.
- [x] No `bin`, `obj`, `.artifacts`, IDE cache, test result, secret, or local model file is tracked.
- [x] Local commits are logical and reviewed.
- [x] Nothing was pushed without explicit authorization.

## Product behavior

- [x] Results toolbar remains fixed while result rows scroll.
- [x] Duplicate details use a right-side drawer and keep groups visible.
- [x] Shell AI and Advanced toggles remain visible and synchronize with Settings.
- [x] Enabling AI alone never invokes a provider.
- [x] OCR is local, capability-detected, page-aware, bounded, cancellable, and honestly reports unavailable states.
- [x] Built-in PDFium rendering handles scanned/mixed PDFs and only sends insufficient rendered pages to external Tesseract.
- [x] Tesseract version and configured English/German language data are validated before recognition.
- [x] AI extracted-text interpretation has a separate default-off gate and remains one-document, bounded, validated, and review-only.
- [x] Metadata readers are defensive and preserve provenance.
- [x] User, metadata, OCR, and generated tags remain distinguishable.
- [x] Semantic Search Beta is local, bounded, incremental, cancellable, and explains matches.
- [x] Semantic index clear/rebuild operations do not modify source files.
- [x] Restructuring preview creates no active repeat-protection record.
- [x] Only a successful, explicitly confirmed apply activates repeat protection.
- [x] Repeat protection allows explicit override and incremental organization.
- [x] Structure History stores bounded source, proposed, and applied snapshots.
- [x] Structure diagrams are read-only and have textual summaries.
- [x] AI workflows remain suggestion-only.

## Persistence and migration

- [x] Existing v0.9.1 settings load with safe v1.0 defaults.
- [x] New stores are versioned, bounded, written atomically, and recover safely from malformed data.
- [x] Existing catalog, snapshots, tags, and saved searches remain readable.
- [x] OCR/content and semantic stores can be cleared and rebuilt independently.
- [x] Structure history pruning preserves the newest relevant records.
- [x] Settings and stores do not contain credentials or model data.

## Privacy and safety

- [x] OCR, metadata extraction, tagging, and semantic indexing are local.
- [x] Raw OCR/extracted text and vectors are not written to ordinary logs.
- [x] Rename/folder AI requests exclude file contents; extracted text can be sent only through its separate opt-in and explicit one-file request.
- [x] Error messages do not expose unnecessary full paths.
- [x] Source files are opened read-only by scanners and extractors.
- [x] PDF-render temporary workspaces are application-owned, bounded, and cleaned on success, failure, timeout, and cancellation.
- [x] Apply operations are confined to the approved root and reject overwrite, traversal, and missing-source conditions.
- [x] Duplicate actions do not delete or mutate files.

## Accessibility and usability

- [ ] Keyboard focus order is coherent.
- [ ] Drawer close and Escape behavior work.
- [ ] Toggle labels and help text are accessible.
- [ ] Status is not conveyed only by color.
- [ ] Long names and paths wrap or trim without hiding required actions.
- [ ] Large lists and structure diagrams remain bounded and responsive.
- [ ] Empty, unavailable, cancelled, partial, and failed states are explicit.

## Documentation

- [x] Release proposal, specification, decisions, data model, migration guide, version notes, and architecture notes are current.
- [x] README, roadmap, changelog, release status, safety/privacy, settings, Help, and implementation index describe the final behavior.
- [x] OCR engine and PDF rasterizer availability limitations are stated accurately.
- [x] FOSS policy, exact resolved package inventory, and third-party redistribution notices are current.
- [x] Semantic Search is labeled Beta and described as local deterministic similarity search.
- [x] No documentation claims autonomous AI filesystem control.
- [x] Manual testing guide contains all 140 checks.

## Automated validation

- [ ] `dotnet restore .\OpenSorSe.sln` — attempted; blocked by host ACLs on pre-existing repository `obj` files.
- [ ] `dotnet build .\OpenSorSe.sln --configuration Debug --no-restore` — attempted in and outside the sandbox; blocked by the same host ACLs.
- [ ] `dotnet test .\OpenSorSe.sln --configuration Debug --no-build` — exited successfully but used the stale locked 453-test v0.9.1 output, so it is not accepted as v1.0 evidence.
- [x] Redirected `.artifacts` restore and current-source Debug build succeeded.
- [x] Redirected `.artifacts` current-source Debug and Release test suites each passed 531 tests with none skipped.
- [x] Redirected `.artifacts` Release build succeeded.
- [x] Tests run against current source, not stale binaries.
- [x] Test count, skipped tests, warnings, and environment limitations are recorded.
- [x] `git diff --check`
- [x] Static inspection covers XAML bindings, dependency registration, namespaces, cancellation, serialization compatibility, navigation fallbacks, and file-operation safety.

## Manual validation

- [ ] All checks in `docs/MANUAL_TESTING_v0.9.1.md` are complete.
- [ ] All 140 checks in `docs/MANUAL_TESTING_v1.0.md` are complete.
- [ ] Windows smoke test is complete.
- [ ] Supported non-Windows behavior is tested or its limitation is documented.
- [ ] OCR unavailable, provider unavailable, malformed content, cancellation, and partial failure paths are exercised.

## Release hold

- [ ] Manual v0.9.1 and v1.0 testing is complete before considering a merge into `main`.
- [x] Remaining limitations are release-appropriate and do not weaken safety claims.
- [x] Final report names the exact local commits, validation results, upstream state, and push status.
