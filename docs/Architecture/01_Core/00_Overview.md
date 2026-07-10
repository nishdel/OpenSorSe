# TidyMind System Overview

# TidyMind System Overview

This diagram represents the highest level architecture of TidyMind.

Every major subsystem is shown here.

Each subsystem will later receive its own architecture document.

```mermaid
flowchart TD

    User["👤 User"]

    Core["⚙️ Core System"]

    Scanner["📂 Scanner"]

    Readers["📄 File Readers"]

    AI["🧠 AI Engine"]

    Search["🔍 Search Engine"]

    Database["🗄️ Database"]

    Rules["📋 Rule Engine"]

    Reports["📊 Reports"]

    GUI["🖥️ User Interface"]

    Plugins["🔌 Plugin System"]


    User --> GUI

    GUI --> Core

    Core --> Scanner

    Scanner --> Readers

    Readers --> AI

    AI --> Rules

    Rules --> Database

    Database --> Search

    Search --> GUI

    Database --> Reports

    Plugins --> Core

    Plugins --> Readers

    Plugins --> AI

    Plugins --> Search
```

---

## Responsibilities

### Core

Coordinates every subsystem.

---

### Scanner

Discovers folders and files.

---

### Readers

Extracts information from supported file types.

---

### AI

Understands document contents.

Generates summaries.

Suggests filenames.

Suggests folder structures.

---

### Rule Engine

Applies user-defined rules.

---

### Database

Stores metadata, history, settings and cached AI results.

---

### Search

Provides keyword and semantic search.

---

### Reports

Generates cleanup statistics and reports.

---

### GUI

Displays information to the user.

Allows interaction with every subsystem.

---

### Plugins

Allows extension of TidyMind without modifying the core application.


```mermaid
mindmap
  root((TidyMind))

    Core

      Configuration
      Logging
      Settings
      Startup
      Dependency Injection

    Scanner

      Folder Traversal
      File Enumeration
      Metadata Extraction
      Duplicate Detection
      Progress Tracking

    Readers

      PDF
      DOCX
      Excel
      Images
      Audio
      Video

    AI

      Providers
      Classification
      Summarization
      Renaming
      Folder Suggestions
      Embeddings

    Search

      Keyword Search
      Semantic Search
      Filters
      Indexing

    Database

      Metadata
      History
      Cache
      Settings

    GUI

      Dashboard
      Scanner
      Preview
      History
      Reports
      Settings

    Reports

      Statistics
      Cleanup Reports
      Logs

    Plugins

      Readers
      AI Providers
      OCR
      Exporters
```