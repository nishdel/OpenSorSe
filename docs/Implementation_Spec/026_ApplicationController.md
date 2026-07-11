# Implementation Specification

| Property | Value |
|----------|-------|
| Spec ID | 026 |
| Component | Application Controller |
| Project | TidyMind.Application |
| Version | 1.0 |
| Target Release | v0.1 |
| Status | Draft |

---

# Purpose

The Application Controller coordinates communication between the user interface and the application services.

It manages application state and routes user actions to the appropriate components.

---

# Why

UI components should never communicate directly with backend services.

The Application Controller provides a single entry point for application workflows.

---

# Responsibilities

- Receive UI commands.
- Route commands.
- Manage application state.
- Coordinate navigation.
- Coordinate long-running operations.

---

# Does NOT

- Execute business logic.
- Scan files.
- Move files.
- Calculate hashes.
- Execute rules.

---

# Inputs

- User commands.
- Application events.

---

# Outputs

- Service requests.
- UI state updates.

---

# Workflow

1. Receive command.
2. Validate request.
3. Forward to appropriate service.
4. Update application state.
5. Notify UI.

---

# Acceptance Criteria

- UI communicates only through the controller.
- State updates correctly.
- Navigation functions correctly.
- Unit tests pass.

---

# Dependencies

Depends on:

- 024 Scan Orchestrator

Required by:

- All UI Views