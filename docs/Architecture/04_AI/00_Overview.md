# AI Overview

> This document provides an overview of the Artificial Intelligence subsystem, which is responsible for enriching extracted document information through intelligent analysis and reasoning.

---

## Implementation Status

v0.9.1 narrows and hardens the optional local-AI slice. AI is disabled by default, with independent rename and folder-structure capability switches. Application-owned feature gates reject disabled, invalid, or unconfigured calls before `IAiSuggestionProvider` can run. `AiPromptBuilder` produces capability-specific, deterministic, bounded, metadata-only prompts; `AiResponseParser` and `AiSuggestionValidator` reject malformed, invented, duplicate, unsafe, or excessive output as a whole. The only results are immutable, unverified review proposals and bounded local review decisions. No proposal mutates a filesystem.

Provider configuration, connection testing, model discovery, and review-history reset are advanced controls that appear only when both AI and advanced mode are enabled. Manual tags, deterministic classification, search, scanning, catalog functions, and saved queries do not require AI. Content enrichment, Readers integration, embeddings, semantic search, caching, cloud providers, automatic organization, monitoring, agents, plugins, and filesystem mutation remain future architecture.

---

## Purpose

The AI subsystem transforms extracted document information into meaningful knowledge.

Using local and optional cloud-based language models, the AI subsystem analyzes documents, generates structured insights, classifies content, produces summaries, suggests filenames and folder locations, and creates semantic representations for search.

The AI subsystem operates on information extracted by the Readers subsystem. It does not read files directly.

### v0.9.1 concrete boundary

The delivered provider is optional and receives only bounded result metadata. `OpenSorSe.Application.AI` owns gates, provider-neutral contracts, prompts, parsing, validation, and review coordination; `OpenSorSe.AI` owns Ollama HTTP DTOs and local JSON decision history. The Desktop never calls HTTP directly. Model output remains untrusted until the complete response passes validation and becomes an application-owned preview. See [v0.9.1 specification 046](../../Implementation_Spec/v0.9.1/046_Optional_AI_and_Advanced_Feature_Controls.md).

---

# Responsibilities

The AI subsystem is responsible for:

* Managing AI providers.
* Coordinating AI requests.
* Building prompts.
* Classifying documents.
* Generating summaries.
* Suggesting filenames.
* Suggesting folder locations.
* Generating embeddings.
* Supporting semantic search.
* Managing AI response caching.

---

# Scope

### In Scope

* Large Language Model integration
* Prompt generation
* Document understanding
* Semantic enrichment
* Embedding generation
* AI-assisted recommendations
* Response caching

### Out of Scope

The AI subsystem is **not** responsible for:

* Reading files
* Filesystem access
* Search indexing
* Database persistence
* Rule execution
* User interface rendering

These responsibilities belong to other architectural subsystems.

---

# Architectural Overview

The AI subsystem receives normalized document representations from the Readers subsystem and enriches them with intelligent analysis before forwarding the results to the Database.

```mermaid
flowchart LR

Readers["Readers"]

AIManager["AI Manager"]

PromptEngine["Prompt Engine"]

ModelProviders["Model Providers"]

AIFeatures["AI Features"]

Database["Database"]

Readers --> AIManager

AIManager --> PromptEngine

PromptEngine --> ModelProviders

ModelProviders --> AIFeatures

AIFeatures --> Database
```

---

# AI Components

The AI subsystem consists of several independent components.

| Component               | Responsibility                                         |
| ----------------------- | ------------------------------------------------------ |
| AI Manager              | Coordinates all AI operations.                         |
| Model Providers         | Provides access to local and optional cloud AI models. |
| Prompt Engine           | Constructs prompts for AI interactions.                |
| Document Classification | Categorizes and labels documents.                      |
| Summarization           | Generates document summaries.                          |
| Renaming                | Suggests meaningful filenames.                         |
| Folder Suggestions      | Recommends appropriate storage locations.              |
| Embeddings              | Generates semantic vector representations.             |
| Vector Search           | Supports semantic retrieval.                           |
| Caching                 | Stores reusable AI results.                            |

Each component is documented separately within this section.

---

# AI Processing Pipeline

A typical AI operation follows these stages:

1. Receive a normalized document representation.
2. Determine the required AI operations.
3. Build the appropriate prompt.
4. Select the configured AI provider.
5. Execute the AI request.
6. Validate and normalize the response.
7. Enrich the document representation.
8. Forward the enriched document to the Database subsystem.

---

# Design Principles

The AI subsystem follows several architectural principles:

* AI-assisted, not AI-controlled.
* Provider independent.
* Local-first.
* Explainable where practical.
* Extensible.
* Modular.
* Deterministic where possible.

The AI subsystem should support multiple providers without requiring changes to higher-level application components.

---

# AI Providers

The architecture should support multiple AI providers through a common abstraction.

Examples include:

* Local language models
* Optional cloud language models
* Local embedding models
* Cloud embedding providers

The rest of the application should remain unaware of the specific provider being used.

---

# Future Considerations

The architecture should support future enhancements, including:

* Multi-model orchestration.
* Vision-language models.
* Speech models.
* Agent-based workflows.
* Fine-tuned local models.
* Plugin-defined AI providers.

These enhancements should integrate with the existing architecture while preserving subsystem boundaries.

---

# Related Documents

* [AI Manager](01_AI_Manager.md)
* [Model Providers](02_Model_Providers.md)
* [Prompt Engine](03_Prompt_Engine.md)
* [Document Classification](04_Document_Classification.md)
* [Summarization](05_Summarization.md)
* [Renaming](06_Renaming.md)
* [Folder Suggestions](07_Folder_Suggestions.md)
* [Embeddings](08_Embeddings.md)
* [Vector Search](09_Vector_Search.md)
* [Caching](10_Caching.md)
* [Database Overview](../05_Database/00_Overview.md)
