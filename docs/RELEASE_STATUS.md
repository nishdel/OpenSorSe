# Release Status

| Release | Status | Validation | Scope |
| --- | --- | --- | --- |
| v0.1 Foundation | Complete | Restore, build, automated tests, and manual UI validation complete. | Read-only scan pipeline, metadata, hashing, deterministic rules, Dashboard, Settings, Diagnostics, and supporting application infrastructure. |
| v0.2 Results Exploration | Complete | Restore, build, 233 automated tests, and manual UI validation complete. | Immutable result snapshots, Results Explorer, filtering, sorting, paging, details, and exact-duplicate review. |
| v0.3 Local Suggestions and Ranked Exploration | Complete | Clean isolated restore/build and 251 automated tests passed. Existing repository `obj` folders blocked direct in-place validation. | Optional Ollama integration, validated read-only suggestions, local decision history, session tags, deterministic ranked search, and product polish. |
| v0.4 Opt-in Local Catalog | Complete | Clean isolated restore/build and 260 automated tests passed. Existing repository `obj` folders blocked direct in-place validation. | Bounded opt-in application-data JSON catalog, historical snapshot reopening, accepted-tag restoration, and catalog safety controls. |
| v0.5 Catalog Search and Maintenance | Complete | Clean isolated restore/build and 267 automated tests passed. Existing repository `obj` folders blocked direct in-place validation. | Deterministic catalog-wide metadata search, historical-hit opening, selected entry removal, and two-step clear of application-owned catalog data. |
| v0.6 User-Managed Tags | Complete | Clean isolated restore/build and 274 automated tests passed; zero build warnings/errors. | Bounded manual tag add/remove, protected deterministic tags, immediate search integration, and catalog-backed persistence. |
| v0.7 Saved Catalog Searches | Complete | Clean isolated restore/build and 283 automated tests passed; zero build warnings/errors. | Bounded atomic named queries, current-catalog rerun, selected removal, two-step corruption-recovery reset, and no persisted hits. |
| v0.8 Snapshot Identity and Scope | Complete | Clean isolated restore/build and 290 automated tests passed; zero build warnings/errors. | Catalog schema 2, schema-1 read compatibility, bounded names/source roots, and Saved Catalog/search identity context. |
| v0.9 Historical Snapshot Comparison | Complete | Post-implementation audit: clean isolated restore/build and 330 automated tests passed; zero build warnings/errors. | Bounded deterministic metadata/tag comparison, scope compatibility, cancellation, filters, historical opening, stale-state hardening, and persistence/safety hardening. |
| v0.9.1 Optional AI and Feature Controls | Implementation and corrective pass complete; manual UI verification pending | Redirected-artifact restore/build and 453 automated tests passed; zero build warnings/errors. Direct in-place generated folders remain host-write-protected. | Default-off AI/advanced controls, constrained suggestions, hardened Ollama transport/setup, bounded diagnostics, contextual Help, consistent statuses, corrected Catalog Search, and responsive Duplicate View. |
| v1.0 Integrated Local Understanding | Final implementation and automated validation complete; manual UI/OCR/platform verification pending | Current-source redirected restore, Debug/Release builds, and 531 automated tests passed in both configurations; zero build warnings/errors. Direct in-place generated folders remain host-write-protected. | Fixed Results toolbar, duplicate drawer, shell feature controls, page-aware scanned/mixed-PDF OCR, provenance tags, Semantic Search Beta, optional bounded AI text proposals, deterministic restructuring, FOSS inventory, repeat protection, and structure history/diagrams. |

## Current product boundary

OpenSorSe 1.0 is a safe, local-first desktop application for understanding and organizing explicitly selected folders. AI and Advanced mode are independent and disabled by default. OCR and Semantic Search Beta are also separate local opt-ins and never activate AI.

The current Desktop workflow does not:

- Let AI rename, move, delete, overwrite, create, or otherwise modify source files. Optional Ollama can generate only capability-specific validated rename, logical folder-structure, or bounded document-text interpretation previews; document text requires a separate opt-in and explicit one-file request.
- Expose the historical generic Executor through the Desktop, execute rules, delete duplicates, write document metadata/tags, or run autonomous organization.
- Contact Ollama when AI is disabled or merely because the global AI switch is enabled. Provider requests additionally require an enabled capability, valid context, endpoint, and model.
- Treat catalog or structure comparison as certainty. Stored snapshots and semantic similarities are bounded review aids, not live filesystem truth.

Scanning, duplicate review, extraction, OCR, tagging, indexing/search, comparison, diagrams, and AI suggestions are non-mutating. The one source-location mutation is a deterministic restructuring plan that is previewed and separately confirmed against an exact preview identity. It is bounded to 500 moves under one approved root, revalidates the root before work, rejects traversal/reparse/missing/conflicting/overwrite conditions, never deletes, and records or rolls back outcomes. AI output cannot enter this service.

Duplicate View may, only after an explicit user command, pass a validated current-scan path to the operating-system shell. Each action is capped at five targets, uses no constructed shell command, reports partial failures, and performs no OpenSorSe filesystem mutation.

OpenSorSe-owned bounded JSON stores may retain settings, logs, AI review decisions, optional catalog snapshots/tags, saved queries, extracted native/OCR text, deterministic semantic vectors, and structure history under local application data. Current persistence, mutation, and network boundaries are detailed in [Safety and Privacy](SAFETY_AND_PRIVACY.md).

## Validation baseline

The v1.0 tree restored successfully through the repository-local ignored `.artifacts` output root. Current-source Debug and Release builds both succeeded with zero warnings and zero errors. The full current-source suite passed 531 tests with none skipped in each configuration: Core 26, Scanner 56, Rules 68, Executor 36, Application 215, and Desktop 130. The suite includes a generated real-PDF PDFium rendering test, page-level OCR/failure/cleanup fakes, AI provider-boundary checks, settings/cache migration checks, and an exact 133-package FOSS inventory check. The Debug build compiled every Avalonia view, and the Desktop composition-root tests validated the production dependency graph and feature-gated navigation.

The exact standard restore and Debug build commands were also attempted. Both were blocked because this host denies writes to pre-existing generated repository `obj` files (`project.assets.json`, generated editor configuration, and file lists), including with approved elevated execution. The standard `--no-build` test command exited successfully against the old locked `bin` outputs but exposed only the 453-test v0.9.1 baseline, so it is recorded as stale and is not used as v1.0 evidence. No source, user file, or protected generated directory was removed to work around the host ACL.

Tesseract is not installed or discoverable in this development environment, so live recognition was not claimed. Automated tests cover version/language detection, argument construction, cancellation, timeout, empty/oversized output, missing languages, mixed-page coordination, cleanup, and provider isolation through fakes. The PDF renderer itself was exercised in process against a generated real PDF.

## Documentation status

The architecture directory contains both current implementation documentation and longer-term design material. The 1.0 overview documents identify the delivered bounded content, OCR, semantic, AI, JSON-store, GUI, and structure-history components. Rich media/archive readers, relational database architecture, reports, plugins, broad localization, cloud indexing, and packaging remain design material unless a release specification explicitly marks them implemented.

## Current release

OpenSorSe 1.0 implementation and automated validation are complete on the local `v1.0` branch; inherited v0.9.1 and v1.0 GUI/OCR/platform verification remain required before considering integration. See the [implementation-specification index](Implementation_Spec/README.md), [v1.0 release proposal](Implementation_Spec/v1.0/00_v1.0_Release_Proposal.md), [integrated specification 048](Implementation_Spec/v1.0/048_v1.0_Integrated_Release.md), [final-completion specification 049](Implementation_Spec/v1.0/049_Final_Product_Completion.md), [implementation decisions](Implementation_Spec/v1.0/IMPLEMENTATION_DECISIONS.md), [migration guide](MIGRATION_v1.0.md), [version notes](VERSION_NOTES_v1.0.md), and [140-step manual checklist](MANUAL_TESTING_v1.0.md). Do not merge into `main` until manual v0.9.1 and v1.0 verification is complete.
