<img width="1254" height="1254" alt="1000166839" src="https://github.com/user-attachments/assets/52c9a11b-76e7-421a-9576-e77566ddfe45" />
# 🧠 OpenSorSe

> **A privacy-first, AI-powered desktop application that intelligently
> understands, organizes, renames, categorizes, and searches your files
> using local Large Language Models.**

> **Status:** 🚧 Early Development

------------------------------------------------------------------------

## 📖 Overview

OpenSorSe is an open-source desktop application designed to help users
regain control of their digital files.

Unlike traditional file organizers that rely on filenames or extensions
alone, OpenSorSe uses local AI models to understand the actual content of
documents, images, and other files. It can suggest meaningful filenames,
build logical folder structures, detect duplicates, summarize documents,
and provide semantic search---all while keeping your data on your own
computer.

**Privacy is the default.** No cloud services are required.

------------------------------------------------------------------------

## ✨ Core Features

### 🧠 AI Document Understanding

-   Read PDFs
-   Read Microsoft Office documents
-   Read text files
-   OCR for scanned PDFs and images
-   Generate summaries
-   Detect categories
-   Extract metadata
-   Detect important dates
-   Detect companies, people and locations

### 📂 Intelligent Organization

-   Recursive folder scanning
-   Batch processing
-   Smart folder suggestions
-   Configurable include/exclude filters
-   Preview every operation
-   Automatic duplicate detection
-   Similar document grouping

### ✏️ Smart File Renaming

Rename files using their contents instead of cryptic filenames.

**Example**

Before:

``` text
Scan00045.pdf
```

After:

``` text
2025 Vehicle Insurance - Allianz.pdf
```

Custom naming templates are fully supported.

------------------------------------------------------------------------

## 🔒 Privacy First

-   Local AI inference
-   No telemetry
-   No tracking
-   No subscriptions
-   Your files never leave your computer

------------------------------------------------------------------------

## 🤖 AI Providers

### Local

-   Ollama
-   llama.cpp
-   LM Studio
-   OpenAI-compatible local servers

### Cloud (Planned)

-   OpenAI
-   Anthropic
-   Google Gemini
-   Azure OpenAI
-   OpenRouter
-   Custom OpenAI-compatible APIs

------------------------------------------------------------------------

## 📄 Supported File Types

  Category            Formats
  ------------------- ----------------------------------------------------
  Documents           PDF, DOC, DOCX, ODT, TXT, RTF, Markdown, HTML, XML
  Spreadsheets        XLS, XLSX, CSV, ODS
  Presentations       PPT, PPTX, ODP
  Images              PNG, JPG, JPEG, TIFF, WEBP, HEIC
  Archives            ZIP, 7Z, TAR, GZ
  Audio *(planned)*   MP3, WAV, FLAC, M4A
  Video *(planned)*   MP4, MKV, AVI, MOV

Unsupported formats are skipped gracefully and reported.

------------------------------------------------------------------------

## 🔍 Semantic Search

Search naturally instead of remembering filenames.

Examples:

-   Find my passport
-   Show invoices from 2023
-   Find documents mentioning Mercedes
-   Show climbing photos
-   Find receipts from Italy

------------------------------------------------------------------------

## 🧩 AI Assistant (Planned)

Examples:

-   Where is my passport?
-   Organize my Downloads folder.
-   Summarize my tax documents.
-   Find duplicate files.
-   Explain why this file was classified as Insurance.

------------------------------------------------------------------------

## 🛡️ Safety Features

-   Preview mode
-   Dry-run mode
-   Undo support
-   Automatic backups
-   Transaction-based operations
-   Rollback on failure
-   Collision detection
-   Detailed logging
-   Resume interrupted scans

------------------------------------------------------------------------

## ⚡ Performance

-   Multi-threaded scanning
-   GPU acceleration where available
-   AI result caching
-   Parallel OCR
-   Incremental rescans
-   Large collections (100,000+ files)

------------------------------------------------------------------------

## 🔌 Plugin Architecture

Planned plugin support for:

-   AI providers
-   OCR engines
-   File readers
-   Search providers
-   Exporters
-   Importers
-   Custom actions

------------------------------------------------------------------------

## 🏗️ Technology Stack

-   Python
-   PySide6
-   Ollama
-   SQLite
-   PyMuPDF
-   python-docx
-   openpyxl
-   OCR Engine
-   GitHub Actions

------------------------------------------------------------------------

## 🗺️ Roadmap

### Version 0.1

-   [ ] Folder scanning
-   [ ] PDF support
-   [ ] DOCX support
-   [ ] Excel support
-   [ ] Local AI integration
-   [ ] Rename suggestions
-   [ ] Preview window

### Version 0.2

-   [ ] Folder suggestions
-   [ ] Bulk organization
-   [ ] Duplicate detection
-   [ ] OCR support

### Version 0.3

-   [ ] AI learns from user decisions
-   [ ] Automatic tagging
-   [ ] Semantic search

### Version 1.0

-   [ ] Plugin system
-   [ ] Cross-platform support
-   [ ] Multi-language support
-   [ ] Stable release

------------------------------------------------------------------------

## 🎯 Long-Term Vision

OpenSorSe aims to become more than a document organizer.

The long-term vision is a privacy-first AI File Manager that understands
relationships between documents, photos, videos, projects, companies and
people, allowing users to search and organize their digital life using
natural language while remaining fully in control of their data.

------------------------------------------------------------------------

## ❤️ Contributing

Contributions of every size are welcome.

Whether you're interested in AI, OCR, desktop development, UI/UX,
testing, accessibility, indexing, documentation or performance
optimisation, we'd love your help build OpenSorSe.

------------------------------------------------------------------------

## 📜 License

**TBD**

MIT or GPLv3 are currently being considered.
A license has not yet been selected. Until one is added, all rights are reserved by default. An open-source license will be chosen before the first stable release.
