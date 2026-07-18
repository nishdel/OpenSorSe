# Safety and Privacy

OpenSorSe 0.9 is a local-first, read-only analysis application. The current Desktop workflow reads only folders the user selects and does not expose a command that mutates a scanned file.

## Scanned user files

The current workflow does not rename, move, delete, overwrite, copy, edit, execute, open, reveal, change permissions, change timestamps, or create sidecars beside scanned files. Scanner, metadata, and hashing services skip filesystem reparse points and symbolic links. Results search, catalog search, and snapshot comparison operate on already captured metadata and never re-open a stored path.

The repository retains historical `OpenSorSe.Executor` move/copy/undo components and their isolated tests. They are not registered by the v0.9 Desktop, are not consumed by the Application pipeline, and have no user-facing entry point.

## OpenSorSe-owned writes

By default, runtime writes are below `Environment.SpecialFolder.LocalApplicationData/OpenSorSe`:

| Data | File/location | Bound and behavior |
| --- | --- | --- |
| Settings | `settings.json` | At most 1 MiB; validated and atomically replaced. |
| Diagnostic logs | `Logs/opensorse-owned-YYYY-MM-DD.log` | At most 10 MiB each plus configured daily-file retention; every managed file has an ownership marker. |
| AI review decisions | `decision-history.json` | At most 1,000 bounded metadata-only records and 4 MiB. |
| Opt-in catalog | `catalog.json` | At most 10 snapshots, 2,000 files per snapshot, 12 accepted tags per file, and 128 MiB encoded. |
| Saved catalog searches | `saved-catalog-searches.json` | At most 25 name/query records and 256 KiB; hits are not stored. |

The user may explicitly configure another absolute diagnostic-log directory. OpenSorSe appends to or retains only exact daily filenames carrying its ownership marker. A name collision with an unowned file fails closed and preserves that file.

Temporary sibling files are used for atomic JSON replacement and removed after success, cancellation, or failure. Explicit catalog clear, saved-search reset, and decision-history reset affect only their respective OpenSorSe-owned files.

If `settings.json` is malformed, semantically invalid, inaccessible, or oversized, OpenSorSe preserves it, starts with safe defaults, and shows a recovery warning on Settings. Saving a corrected draft is the explicit action that replaces the invalid owned file.

## Persisted and non-persisted data

Catalog snapshots can contain paths, filenames, filesystem metadata, deterministic categories, duplicate membership, planned-operation previews, warnings, accepted tags, snapshot names, and captured source roots. OpenSorSe does not persist file contents, extracted text, excerpts, hashes in the Results snapshot, search hits, comparison rows, embeddings, or semantic indexes.

Diagnostic messages omit raw exception details and file contents. Paths may appear in result/catalog application data because they are required for the explicit local review workflow; they are never uploaded automatically.

## Network and telemetry

Core scanning and catalog workflows perform no network communication and no telemetry is present. Optional AI suggestions use an explicitly enabled, user-configured Ollama-compatible HTTP or HTTPS endpoint. Those requests contain metadata-only prompts bounded to 128 KiB, not file contents; a non-local endpoint receives that metadata over the configured connection. Response bodies are limited to 1 MiB and model discovery publishes at most 100 validated identifiers. AI output remains review-only.

## Recovery

Malformed, unsupported, or oversized owned JSON is reported rather than silently overwritten. Invalid settings recover to safe in-memory defaults as described above. Catalog corruption requires manual restore/removal because catalog clear validates before deletion. Saved-query and AI-decision reset actions are explicit recovery operations and do not affect other stores or scanned files.
