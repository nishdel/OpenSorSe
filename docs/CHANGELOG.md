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
- A bounded, newest-first 500-event process-session diagnostic viewer with severity/category filters, safe details, and copy support.
- Optional session-only AI request diagnostics, bounded to 20 redacted records and available only when AI, advanced mode, and the explicit diagnostic switch are all enabled.
- Contextual Help from every major page, with topic-specific workflow, safety, error, and related-topic guidance.
- A responsive **Duplicate View** with per-file details and explicitly requested, capped opening of known files or containing folders through a testable launcher abstraction.
- Reusable severity-labelled status presentation for Settings, AI, Diagnostics, Catalog Search, and Duplicate View.

### Changed

- Raw provider/request diagnostics, detailed logging, historical comparison, detailed diagnostics, and operation-history internals are classified as advanced.
- Essential Ollama endpoint, connection check, model discovery/selection, timeout, and capability controls are visible whenever AI is enabled; only raw request inspection and other technical detail require advanced mode.
- Ollama endpoint normalization accepts safe HTTP(S) base paths and strips known `/api`, `/api/tags`, and `/api/generate` suffixes before building request URIs.
- Provider operations use one request-scoped timeout from 5 through 300 seconds instead of competing `HttpClient` and request timeouts.
- Model discovery preserves the configured exact model and reports it unavailable instead of silently selecting another model.
- AI requests report typed progress stages and preflight the selected exact model before generation.
- Folder prompts use deterministic request-local `item-NNN` identities, report included/omitted counts, and require every included item exactly once.
- Catalog Search now prioritizes search, has one result/status surface, supports clear and rename workflows, and separates saved-search maintenance.
- Settings preserves the current scroll offset across visibility-driven layout changes.
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
- Quoted JSON authorization values are redacted from opt-in AI diagnostics.
- Raw AI diagnostic capture can no longer bypass the advanced-mode requirement through an application-service call.
- Diagnostic event capture and clipboard failures remain isolated from scanning and other primary workflows.

## v0.9

See the preserved [v0.9 release proposal](Implementation_Spec/v0.9/00_v0.9_Release_Proposal.md) and [audit corrections](Implementation_Spec/v0.9/AUDIT_CORRECTIONS.md) for the historical snapshot-comparison release.
