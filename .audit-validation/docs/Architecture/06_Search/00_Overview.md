# Search Overview

> This document provides an overview of the Search subsystem, which is responsible for retrieving relevant documents using keyword, semantic, and metadata-based search techniques.

---

## Implementation Status

v0.3 implements deterministic metadata-aware ranked search over the current in-memory results snapshot. v0.5 reuses the same filename, path, extension, deterministic-category, and accepted-tag semantics across opt-in saved catalog snapshots. v0.6 adds explicit user-managed tags to those inputs, and v0.7 stores up to 25 names/query-text presets that rerun the same current-catalog search. v0.8 adds snapshot-name context to hits without changing ranking. v0.9 comparison is a separate pure analysis service, not search or an index. Search hits and comparison results are never persisted. There is no extracted-text index, semantic search, embedding, or persistent search index. The broader design below remains future architecture unless a current release specification says otherwise.

---

## Purpose

The Search subsystem enables users to efficiently discover and retrieve documents managed by OpenSorSe.

It combines multiple search strategies—including keyword search, semantic search, metadata filtering, and intelligent ranking—to provide accurate and relevant search results regardless of how a document was originally processed.

The Search subsystem provides retrieval capabilities only. It does not create or modify document information.

---

# Responsibilities

The Search subsystem is responsible for:

* Searching document content.
* Searching metadata.
* Performing semantic search.
* Filtering search results.
* Ranking results by relevance.
* Managing search indexes.
* Returning unified search results.

---

# Scope

### In Scope

* Keyword search
* Semantic search
* Metadata search
* Filtering
* Ranking
* Search indexes
* Result retrieval

### Out of Scope

The Search subsystem is **not** responsible for:

* AI inference
* Embedding generation
* Metadata extraction
* Database persistence
* Rule execution
* User interface rendering

These responsibilities belong to other architectural subsystems.

---

# Architectural Overview

The Search subsystem retrieves information from the Database using multiple search strategies and presents unified search results.

```mermaid id="mh18qa"
flowchart LR

Database["Database"]

Keyword["Keyword Search"]

Semantic["Semantic Search"]

Filtering["Filtering"]

Ranking["Ranking"]

Results["Search Results"]

Database --> Keyword
Database --> Semantic

Keyword --> Filtering
Semantic --> Filtering

Filtering --> Ranking

Ranking --> Results
```

The Search subsystem combines multiple retrieval techniques into a single, consistent search experience.

---

# Search Components

The Search subsystem consists of several specialized components.

| Component       | Responsibility                                 |
| --------------- | ---------------------------------------------- |
| Keyword Search  | Searches textual content and metadata.         |
| Semantic Search | Retrieves documents based on meaning.          |
| Filtering       | Narrows result sets using structured criteria. |
| Ranking         | Orders results by relevance.                   |
| Tagging         | Supports tag-based organization and retrieval. |
| Indexing        | Maintains searchable data structures.          |

Each component is documented separately within this section.

---

# Search Workflow

A typical search operation consists of the following stages:

1. Receive a user query.
2. Determine the applicable search strategies.
3. Execute keyword and semantic searches where appropriate.
4. Apply filters.
5. Rank candidate results.
6. Return the final result set.

Individual search strategies may be combined depending on the query.

---

# Search Sources

The Search subsystem may retrieve information from:

* Document metadata.
* Extracted document text.
* AI-generated summaries.
* AI classifications.
* Tags.
* Embeddings.
* File properties.

The origin of information should remain transparent to the user whenever practical.

---

# Design Principles

The Search subsystem should remain:

* Unified.
* Fast.
* Extensible.
* Provider-independent.
* Scalable.
* Independent of AI implementation.

Search should focus on retrieving information rather than generating it.

---

# Future Considerations

The architecture should support future enhancements, including:

* Hybrid search strategies.
* Natural language search.
* Saved queries containing full filter combinations beyond v0.7's query-text-only presets.
* Personalized ranking.
* Federated search.
* Plugin-defined search providers.

These enhancements should preserve the Search subsystem's primary responsibility of retrieving relevant information.

---

# Related Documents

* [Keyword Search](01_Keyword_Search.md)
* [Semantic Search](02_Semantic_Search.md)
* [Filtering](03_Filtering.md)
* [Ranking](04_Ranking.md)
* [Tagging](05_Tagging.md)
* [Indexing](06_Indexing.md)
* [Database Overview](../05_Database/00_Overview.md)
