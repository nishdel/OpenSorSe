# OpenSorSe v1.0 Implementation Decisions

## One local hybrid search implementation

Semantic search uses a deterministic feature-hashing embedding provider bundled in managed code. Exact filename, confirmed tag, metadata, path, native/OCR text, date, category, and cosine-similarity signals are combined with explicit weights. This works without AI, a network service, or a downloaded model. The provider abstraction permits a future local model without changing the index or UI contracts.

## Capability-detected OCR

OCR is behind `IOcrEngine` and `IOcrService`. The first integration uses an explicitly configured or PATH-resolved local Tesseract CLI for PNG, JPEG, and TIFF. It is never downloaded or started in the background. Scanned PDF OCR reports that a local page rasterizer is unavailable rather than pretending success. Unit tests use fakes and require no native engine.

## Metadata without document execution

PDF metadata is read through bounded byte/text inspection. DOCX and XLSX metadata is read from bounded ZIP/XML parts with DTD and external resolution disabled. Image dimensions are parsed from bounded PNG/JPEG headers. Macros, formulas, embedded programs, links, and remote resources are never executed or fetched.

## Provenance-first tags

All generated and user tags share one provenance-aware model. Existing v0.9.1 accepted tags map to confirmed user or deterministic records. Inferred tags remain suggestions until accepted. Rejected generated tags are suppressed until their source fingerprint changes or the user resets generated decisions.

## Preview-first restructuring

Structure planning, repeat protection, history, comparison, and diagrams are independent of file mutation. Applying a plan requires an exact preview ID and explicit confirmation, validates every source/destination under one root, rejects overwrite and traversal, performs only bounded directory creation and moves, and records success, partial, failure, or cancellation. Preview-only and failed records never activate repeat protection.

## Bounded JSON persistence

OCR cache, semantic index, and structure history use separate versioned atomic JSON stores in OpenSorSe application data. Optional corrupt caches fail closed and can be rebuilt. Catalog schema compatibility remains intact; large extracted text is never added to catalog snapshots or ordinary diagnostics.

## Shell toggle save model

Global AI and Advanced toggles save immediately through the existing configuration service because they are explicit global controls. Settings drafts are refreshed from the committed configuration after a shell change. Enabling AI never probes Ollama; disabling a feature performs the existing navigation and request-cancellation cleanup.
