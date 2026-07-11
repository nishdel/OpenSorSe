# Implementation Specification

| Property | Value |
|----------|-------|
| Spec ID | 024 |
| Component | Scan Orchestrator |
| Project | TidyMind.Application |
| Version | 1.0 |
| Target Release | v0.1 |
| Status | Draft |

---

# Purpose

The Scan Orchestrator coordinates the complete TidyMind processing pipeline.

It executes each processing stage in the correct order and manages communication between the UI and backend services.

---

# Why

The user should only need to perform one action ("Start Scan").

The Scan Orchestrator hides the complexity of the processing pipeline by coordinating all required components.

---

# Responsibilities

The Scan Orchestrator shall:

- Receive scan requests.
- Execute pipeline components in the correct order.
- Report overall progress.
- Handle cancellation.
- Stop processing on fatal errors.
- Return the final processing result.

---

# Does NOT

The Scan Orchestrator must NOT:

- Scan files itself.
- Read metadata.
- Calculate hashes.
- Execute rules.
- Move files.
- Contain business logic.
- Modify the filesystem directly.

---

# Inputs

- Scan request.
- User configuration.

---

# Outputs

The component returns:

- ScanResult.
- Progress updates.
- Processing summary.

---

# Workflow

1. Receive scan request.
2. Start File Scanner.
3. Start Metadata Extraction.
4. Start File Hasher.
5. Start File Classifier.
6. Start Duplicate Detector.
7. Start Rule Engine.
8. Start Move Planner.
9. Start Conflict Resolver.
10. Return results to the UI.

---

# Assumptions

- All required services are available.
- Configuration has been loaded.
- User input has been validated.

---

# Acceptance Criteria

The implementation is complete when:

- Pipeline executes in the correct order.
- Progress is reported.
- Cancellation works.
- Fatal errors stop the pipeline safely.
- Results are returned successfully.
- Unit tests pass.

---

# Future

Not part of v0.1:

- Parallel pipeline execution.
- Pipeline plugins.
- Custom processing stages.
- Distributed processing.

---

# Dependencies

Depends on:

- 001–008 Core Pipeline Components

Required by:

- 015 Folder Selection
- 016 Scan Progress
- 017 Results View