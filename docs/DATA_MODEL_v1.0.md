# OpenSorSe 1.0 Data Model

## Extracted content

`ExtractedMetadataValue` stores key, display value, normalized value, provenance, optional confidence, and source detail. `ContentExtractionRecord` stores file identity, source fingerprint, MIME/type facts, native/OCR availability, bounded normalized text, fields, warnings, and timestamps.

## Tags

`ProvenanceTag` stores identity, file identity, display/normalized name, source, state, confidence, timestamps, source fingerprint, and provenance detail.

## Semantic index

`SemanticIndexDocument` stores file identity, source fingerprint, display name/path, category, tags with states/sources, bounded metadata/text tokens, dates, 256-dimensional normalized vector, and indexed time. No raw file bytes are stored.

## Folder structure

`FolderStructureSnapshot` contains root identity, normalized root path, captured time, stable structure hash, and bounded nodes. Each node has a relative path, kind, optional file identity/fingerprint, size, and status. `RestructuringHistoryRecord` contains source/proposed/applied snapshots, approval/result state, proposal source, counts, per-item outcomes, application/schema versions, and current-match state.

## Compatibility

v0.9.1 settings and catalog models retain their existing JSON names. New properties are optional. Existing accepted tags project into the v1 provenance model without rewriting catalog data merely by reading it.
