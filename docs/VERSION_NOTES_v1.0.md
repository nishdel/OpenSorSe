# OpenSorSe 1.0.0 Version Notes

OpenSorSe 1.0.0 is the first integrated local-understanding release candidate.

- Results filtering remains visible while rows scroll.
- Duplicate details open in a responsive right drawer.
- AI and Advanced switches are available throughout the shell.
- Bounded metadata extraction is local and read-only.
- OCR is Beta: PdfPig reads native PDF text by page, PDFtoImage/PDFium renders only insufficient pages, and an optional detected Tesseract CLI recognizes images or rendered pages.
- Tesseract language capability is checked explicitly for configured English (`eng`) and/or German (`deu`) data; Tesseract remains externally installed.
- Optional bounded AI interpretation of extracted document text has its own default-off gate and creates only an unverified review proposal.
- Semantic Search is Beta, local, deterministic, explainable, and independent of AI.
- Provenance-aware tags connect metadata, OCR, search, and organization.
- Structure history, repeat protection, and read-only diagrams retain organization context.
- A separately confirmed deterministic restructuring plan can move only reviewed root-confined files; it never uses AI output, overwrites, or deletes.

Index quality may evolve and indexes may need rebuilding after future upgrades. OCR, AI interpretation, and indexing never modify source files. The cache fingerprint includes OCR settings and engine/rasterizer versions so legacy records are safely reprocessed. GPU acceleration, a bundled Tesseract distribution, live Tesseract recognition, and cross-platform packaging are not claimed as verified by this Windows development environment.
