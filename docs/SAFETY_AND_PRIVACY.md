# Safety and Privacy

OpenSorSe 0.9.1 is a local-first, read-only analysis application. The current Desktop workflow reads only folders the user selects and does not expose a command that mutates a scanned file.

## Scanned user files

The current workflow does not rename, move, delete, overwrite, copy, edit, execute, change permissions, change timestamps, or create sidecars beside scanned files. Scanner, metadata, and hashing services skip filesystem reparse points and symbolic links. Results search, catalog search, and snapshot comparison operate on already captured metadata and never re-open a stored path.

Duplicate View provides explicit **Open file** and **Open containing folder** comparison actions only for paths in the current in-memory scan. OpenSorSe validates the target, caps a multi-open action at five, and passes each path directly to the operating-system shell without constructing a command string. Opening an external application may allow the user to edit a file there, but OpenSorSe itself does not request or perform that edit. Historical catalog paths are never launch targets.

The repository retains historical `OpenSorSe.Executor` move/copy/undo components and their isolated tests. They are not registered by the v0.9.1 Desktop, are not consumed by the Application pipeline, and have no user-facing entry point. AI application and provider services do not reference Executor APIs.

## OpenSorSe-owned writes

By default, runtime writes are below `Environment.SpecialFolder.LocalApplicationData/OpenSorSe`:

| Data | File/location | Bound and behavior |
| --- | --- | --- |
| Settings | `settings.json` | At most 1 MiB; validated and atomically replaced. |
| Diagnostic logs | `Logs/opensorse-owned-YYYY-MM-DD.log` | At most 10 MiB each plus configured daily-file retention; every managed file has an ownership marker. |
| AI review decisions | `decision-history.json` | At most 1,000 bounded metadata-only records and 4 MiB. |
| Opt-in catalog | `catalog.json` | At most 10 snapshots, 2,000 files per snapshot, 12 accepted tags per file, and 128 MiB encoded. |
| Saved catalog searches | `saved-catalog-searches.json` | At most 25 name/query records and 256 KiB; hits are not stored. |

Process diagnostic events and opt-in raw AI request diagnostics are session-only memory buffers, bounded to 500 and 20 records respectively. They are not added to settings, the catalog, saved searches, or decision history. Disabling AI request diagnostics, AI, or advanced mode clears the raw AI diagnostic buffer.

The user may explicitly configure another absolute diagnostic-log directory. OpenSorSe appends to or retains only exact daily filenames carrying its ownership marker. A name collision with an unowned file fails closed and preserves that file.

Temporary sibling files are used for atomic JSON replacement and removed after success, cancellation, or failure. Explicit catalog clear, saved-search reset, and decision-history reset affect only their respective OpenSorSe-owned files.

If `settings.json` is malformed, semantically invalid, inaccessible, or oversized, OpenSorSe preserves it, starts with safe defaults, and shows a recovery warning on Settings. Saving a corrected draft is the explicit action that replaces the invalid owned file.

## Persisted and non-persisted data

Catalog snapshots can contain paths, filenames, filesystem metadata, deterministic categories, duplicate membership, planned-operation previews, warnings, accepted tags, snapshot names, and captured source roots. OpenSorSe does not persist file contents, extracted text, excerpts, hashes in the Results snapshot, search hits, comparison rows, embeddings, or semantic indexes.

Normal diagnostic messages retain a bounded safe exception summary but omit stack traces, credentials, and file contents. The advanced, explicitly enabled AI request diagnostic viewer can contain exact filenames and relative logical folder metadata because it captures the bounded model request and response; the UI warns about that content, redacts credential-like values, and keeps the records in memory only. Paths may appear in result/catalog application data because they are required for the explicit local review workflow; they are never uploaded automatically.

## Network and telemetry

Core scanning and catalog workflows perform no network communication and no telemetry is present. AI is disabled by default. While disabled, AI controls are hidden, provider detection is not performed, and provider/model requests are rejected at the application boundary before transport. Optional suggestions use an explicitly enabled, user-configured Ollama-compatible HTTP or HTTPS endpoint and a separately enabled capability.

Rename requests include only the selected exact display filename, extension, safe deterministic metadata, bounded nearby names, one request-local identity, and optional concise preferences. Folder-structure requests include only a deterministic maximum of 25 selected metadata records identified as `item-NNN`, bounded existing logical folder names, and optional concise preferences. Prompts report included and omitted counts; each included folder item must return exactly once. Absolute paths and file contents are not sent. A non-local endpoint receives this bounded metadata over the configured connection. Prompts are limited to 128 KiB, responses to 1 MiB, and model discovery to 100 validated exact identifiers. Model output is untrusted and rejected as a whole unless its strict JSON contract and safety rules pass. Accepted or edited suggestions only create local review-decision records; they never invoke a filesystem operation.

## Recovery

Malformed, unsupported, or oversized owned JSON is reported rather than silently overwritten. Invalid settings recover to safe in-memory defaults as described above. Catalog corruption requires manual restore/removal because catalog clear validates before deletion. Saved-query and AI-decision reset actions are explicit recovery operations and do not affect other stores or scanned files.
