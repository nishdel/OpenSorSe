# Changelog

## v0.9.1

Focused optional-AI and interface-complexity refinement; this is not the v1.0 milestone.

### Added

- Default-off global **Enable AI features** and **Show advanced features** settings.
- Independent default-off file-rename and folder-structure suggestion capabilities.
- Central feature requirements shared by navigation, views, commands, Settings, and application services.
- Capability-specific deterministic metadata-only prompt builders with explicit size bounds.
- Strict JSON response contracts, parsing, identity/graph/count/confidence checks, and portable filename/path validation.
- Review, edit, accept, and reject proposal workflow that records local decisions without executing them.
- Typed Ollama missing-model, timeout, cancellation, unsupported-response, malformed/empty/oversized-response, and connection failure handling.

### Changed

- Provider configuration, provider diagnostics, detailed logging, historical comparison, detailed diagnostics, and operation-history internals are classified as advanced.
- The earlier mixed AI organization proposal is narrowed to rename only; AI no longer proposes tags, deterministic categories, or file destinations in v0.9.1.
- About and assembly versions report `0.9.1`.
- Generated validation directories are ignored and removed from source control.

### Safety

- AI is disabled by default, and disabled or invalid requests are rejected before provider invocation.
- Ollama remains optional, local-first, and externally managed; a custom endpoint may be remote.
- Requests exclude file content and absolute paths, and model output is always treated as untrusted.
- No AI result renames, moves, creates, deletes, overwrites, or edits a file or folder.

### Fixed

- Hidden-page navigation now rejects stale/direct access and falls back safely when the selected page becomes unavailable.
- Changing Results context now cancels in-flight AI work and clears stale proposals before they can be reviewed against another file.
- A rename edited back to the current filename is treated as no change and is not saved as an accepted decision.
- Provider-diagnostic transport failures are normalized instead of escaping the application boundary.
- Folder validation rejects reserved system-directory names and duplicate logical paths rather than silently normalizing them.
- Undefined internal suggestion kinds and invalid provider timeouts are blocked before network transport.

## v0.9

See the preserved [v0.9 release proposal](Implementation_Spec/v0.9/00_v0.9_Release_Proposal.md) and [audit corrections](Implementation_Spec/v0.9/AUDIT_CORRECTIONS.md) for the historical snapshot-comparison release.
