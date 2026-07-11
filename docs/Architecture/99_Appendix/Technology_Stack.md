# Technology Stack

> This document defines the primary technologies, frameworks, libraries, and platforms used throughout the TidyMind project.

---

# Purpose

The Technology Stack document provides an overview of the technologies selected for building, running, and extending TidyMind.

Its purpose is to communicate the project's technical foundation, promote consistency across contributions, and explain the role of each major technology within the architecture.

This document describes the intended technology choices rather than implementation details.

---

# Guiding Principles

Technology selection should prioritize:

* Simplicity.
* Maintainability.
* Performance.
* Cross-platform compatibility.
* Open standards where practical.
* Long-term sustainability.

Technologies should be adopted because they solve real architectural needs rather than because they are popular.

---

# Core Platform

| Technology | Purpose                                   |
| ---------- | ----------------------------------------- |
| .NET       | Primary application platform and runtime. |
| C#         | Primary programming language.             |
| MVVM       | User interface architectural pattern.     |

---

# User Interface

| Technology  | Purpose                                          |
| ----------- | ------------------------------------------------ |
| Avalonia UI | Cross-platform desktop user interface framework. |

---

# Data Storage

| Technology | Purpose                                                         |
| ---------- | --------------------------------------------------------------- |
| SQLite     | Embedded relational database for application data and metadata. |

---

# Artificial Intelligence

| Technology             | Purpose                                                                    |
| ---------------------- | -------------------------------------------------------------------------- |
| Ollama                 | Local model management and inference.                                      |
| ONNX Runtime           | Efficient execution of supported machine learning models where applicable. |
| OpenAI-Compatible APIs | Optional support for cloud-based AI providers through a common interface.  |

The architecture remains provider-independent.

---

# Search

| Technology                    | Purpose                                                                      |
| ----------------------------- | ---------------------------------------------------------------------------- |
| SQLite Full-Text Search (FTS) | Keyword search.                                                              |
| Vector Search Provider        | Semantic search using document embeddings through interchangeable providers. |

Specific vector database implementations may evolve without changing the architecture.

---

# Document Processing

| Technology              | Purpose                                                                   |
| ----------------------- | ------------------------------------------------------------------------- |
| Native .NET Libraries   | General file handling where appropriate.                                  |
| Format-Specific Readers | Extraction of document content from supported file types.                 |
| OCR Providers           | Text extraction from scanned documents through interchangeable providers. |

---

# Plugin System

| Technology | Purpose                                                |
| ---------- | ------------------------------------------------------ |
| Plugin API | Stable extension interface for third-party developers. |

Plugin implementations remain independent of the core application.

---

# Testing

| Technology             | Purpose                                     |
| ---------------------- | ------------------------------------------- |
| Unit Testing Framework | Automated testing of individual components. |
| Integration Testing    | Validation of subsystem interactions.       |

Specific testing frameworks may be selected as the project evolves.

---

# Build & Tooling

| Technology     | Purpose                                         |
| -------------- | ----------------------------------------------- |
| Git            | Version control.                                |
| GitHub         | Source code hosting and collaboration.          |
| GitHub Actions | Continuous Integration and automated workflows. |

Equivalent tooling may be adopted if project requirements change.

---

# Documentation

| Technology | Purpose                             |
| ---------- | ----------------------------------- |
| Markdown   | Documentation format.               |
| Mermaid    | Architecture and workflow diagrams. |

---

# Dependency Philosophy

The project should:

* Prefer mature libraries.
* Minimize unnecessary dependencies.
* Favor actively maintained projects.
* Avoid vendor lock-in where practical.
* Abstract external technologies behind interfaces.

External dependencies should be replaceable whenever practical.

---

# Future Considerations

The technology stack may evolve to include:

* Alternative AI providers.
* Additional vector search implementations.
* Cloud synchronization providers.
* Mobile or web frontends.
* Additional build tooling.

Technology choices should continue to support the project's architectural goals rather than dictate them.

---

# Related Documents

* [Architecture Decision Records](03_Architecture_Decision_Records.md)
* [Coding Standards](01_Coding_Standards.md)
* [Plugins Overview](../10_Plugins/00_Overview.md)
* [AI Overview](../04_AI/00_Overview.md)
