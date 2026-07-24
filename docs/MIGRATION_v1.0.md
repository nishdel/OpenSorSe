# OpenSorSe 1.0 Migration Strategy

1. Load v0.9.1 settings with missing OCR, metadata, semantic, and history properties defaulted off or to conservative bounds.
2. Keep catalog schema 1 and 2 readers unchanged; do not require catalog deletion or eager rewrite.
3. Project existing accepted user/deterministic tags into the provenance-aware runtime model.
4. Preserve saved-search schema and query behavior.
5. Treat absent content cache, semantic index, or structure history as valid empty state.
6. Reject unsupported or corrupt optional content/index envelopes with a rebuild action; preserve structure-history corruption for explicit recovery.
7. Write new stores atomically under the existing OpenSorSe local application-data directory.

Migration never reads beyond explicitly selected source scope and never modifies a source file. Representative v0.9.1 JSON fixtures are covered by tests.
