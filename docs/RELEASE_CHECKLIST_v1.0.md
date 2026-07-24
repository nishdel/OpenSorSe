# OpenSorSe 1.0 Release Checklist

This checklist covers preparation of the local v1.0 release candidate. It does not authorize a merge, remote push, package publication, or installation on a user system.

## Repository

- [ ] Current branch is `v1.0`.
- [ ] The v1.0 branch was created from completed v0.9.1 state.
- [ ] `main` has not been merged into or changed by this work.
- [ ] Existing v0.9.1 work and history remain intact.
- [ ] Working tree is clean.
- [ ] No `bin`, `obj`, `.artifacts`, IDE cache, test result, secret, or local model file is tracked.
- [ ] Local commits are logical and reviewed.
- [ ] Nothing was pushed without explicit authorization.

## Product behavior

- [ ] Results toolbar remains fixed while result rows scroll.
- [ ] Duplicate details use a right-side drawer and keep groups visible.
- [ ] Shell AI and Advanced toggles remain visible and synchronize with Settings.
- [ ] Enabling AI alone never invokes a provider.
- [ ] OCR is local, capability-detected, bounded, cancellable, and honestly reports unavailable states.
- [ ] Metadata readers are defensive and preserve provenance.
- [ ] User, metadata, OCR, and generated tags remain distinguishable.
- [ ] Semantic Search Beta is local, bounded, incremental, cancellable, and explains matches.
- [ ] Semantic index clear/rebuild operations do not modify source files.
- [ ] Restructuring preview creates no active repeat-protection record.
- [ ] Only a successful, explicitly confirmed apply activates repeat protection.
- [ ] Repeat protection allows explicit override and incremental organization.
- [ ] Structure History stores bounded source, proposed, and applied snapshots.
- [ ] Structure diagrams are read-only and have textual summaries.
- [ ] AI workflows remain suggestion-only.

## Persistence and migration

- [ ] Existing v0.9.1 settings load with safe v1.0 defaults.
- [ ] New stores are versioned, bounded, written atomically, and recover safely from malformed data.
- [ ] Existing catalog, snapshots, tags, and saved searches remain readable.
- [ ] OCR/content and semantic stores can be cleared and rebuilt independently.
- [ ] Structure history pruning preserves the newest relevant records.
- [ ] Settings and stores do not contain credentials or model data.

## Privacy and safety

- [ ] OCR, metadata extraction, tagging, and semantic indexing are local.
- [ ] Raw OCR/extracted text and vectors are not written to ordinary logs.
- [ ] File contents are not sent to Ollama by deterministic v1.0 features.
- [ ] Error messages do not expose unnecessary full paths.
- [ ] Source files are opened read-only by scanners and extractors.
- [ ] Apply operations are confined to the approved root and reject overwrite, traversal, and missing-source conditions.
- [ ] Duplicate actions do not delete or mutate files.

## Accessibility and usability

- [ ] Keyboard focus order is coherent.
- [ ] Drawer close and Escape behavior work.
- [ ] Toggle labels and help text are accessible.
- [ ] Status is not conveyed only by color.
- [ ] Long names and paths wrap or trim without hiding required actions.
- [ ] Large lists and structure diagrams remain bounded and responsive.
- [ ] Empty, unavailable, cancelled, partial, and failed states are explicit.

## Documentation

- [ ] Release proposal, specification, decisions, data model, migration guide, version notes, and architecture notes are current.
- [ ] README, roadmap, changelog, release status, safety/privacy, settings, Help, and implementation index describe the final behavior.
- [ ] OCR engine and PDF rasterizer availability limitations are stated accurately.
- [ ] Semantic Search is labeled Beta and described as local deterministic similarity search.
- [ ] No documentation claims autonomous AI filesystem control.
- [ ] Manual testing guide contains all 121 checks.

## Automated validation

- [ ] `dotnet restore .\OpenSorSe.sln`
- [ ] `dotnet build .\OpenSorSe.sln --configuration Debug --no-restore`
- [ ] `dotnet test .\OpenSorSe.sln --configuration Debug --no-build`
- [ ] Release build where the environment supports it.
- [ ] Tests run against current source, not stale binaries.
- [ ] Test count, skipped tests, warnings, and environment limitations are recorded.
- [ ] `git diff --check`
- [ ] Static inspection covers XAML bindings, dependency registration, namespaces, cancellation, serialization compatibility, navigation fallbacks, and file-operation safety.

## Manual validation

- [ ] All checks in `docs/MANUAL_TESTING_v0.9.1.md` are complete.
- [ ] All 121 checks in `docs/MANUAL_TESTING_v1.0.md` are complete.
- [ ] Windows smoke test is complete.
- [ ] Supported non-Windows behavior is tested or its limitation is documented.
- [ ] OCR unavailable, provider unavailable, malformed content, cancellation, and partial failure paths are exercised.

## Release hold

- [ ] Manual v0.9.1 and v1.0 testing is complete before considering a merge into `main`.
- [ ] Remaining limitations are release-appropriate and do not weaken safety claims.
- [ ] Final report names the exact local commits, validation results, upstream state, and push status.
