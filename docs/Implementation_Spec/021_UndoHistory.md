# Implementation Specification

| Property | Value |
|----------|-------|
| Spec ID | 021 |
| Component | Undo History |
| Project | TidyMind.UI |
| Version | 1.0 |
| Target Release | v0.1 |
| Status | Draft |

---

# Purpose

The Undo History view displays previous organization operations and allows users to review and restore eligible operations.

It provides a safe and transparent way to reverse changes made by TidyMind.

---

# Why

Users should always be able to see what TidyMind has changed and restore previous organization operations if necessary.

This builds trust in the application and reduces the risk of accidental file organization.

---

# Responsibilities

The Undo History view shall:

- Display previous organization sessions.
- Display the operations contained within a session.
- Display execution timestamps.
- Display undo availability.
- Allow users to start an undo operation.
- Display undo results.

---

# Does NOT

The Undo History view must NOT:

- Execute filesystem operations directly.
- Move files.
- Delete files.
- Scan folders.
- Calculate hashes.
- Execute organization rules.
- Modify undo history.

---

# Inputs

- Undo history.
- Execution history.
- User interaction.

---

# Outputs

The component provides:

- Undo requests.
- Navigation events.

---

# Workflow

1. Load undo history.
2. Display previous organization sessions.
3. User selects a session.
4. Display session details.
5. User confirms undo.
6. Submit undo request.
7. Display undo results.

---

# Assumptions

- Undo history exists.
- Undo Engine is available.

---

# Acceptance Criteria

The implementation is complete when:

- Previous sessions are displayed.
- Session details are available.
- Eligible sessions can be restored.
- Confirmation is required before undo.
- UI tests pass.

---

# Layout

+------------------------------------------------------------+
| Undo History                                               |
+------------------------------------------------------------+

Previous Sessions

------------------------------------------------------------

Today 14:25

✔ 214 files organized

------------------------------------------------------------

Yesterday 19:42

✔ 83 files organized

------------------------------------------------------------

Selected Session

Files Moved: 214

Folders Created: 8

Duration: 00:00:47

------------------------------------------------------------

[ View Details ]

[ Undo ]

[ Close ]

------------------------------------------------------------

Status

2 Undo Sessions Available

---

# Future

Not part of v0.1:

- Selective undo.
- Multi-level undo.
- Redo support.
- Search.
- Undo previews.
- Session comparison.

---

# Dependencies

Depends on:

- 010 - Undo Engine
- 009 - Move Executor

Required by:

- None

---

# Autonomous v0.1 Decisions

No history repository, session model, or database contract exists. v0.1 accepts explicit ordered `UndoHistorySession` values from a later controller. A session wraps caller-supplied ordered `UndoRecord` values and a UTC completion time; this page does not discover, persist, or alter them.

Undo requires a two-step UI confirmation. Confirmation emits a read-only ordered snapshot of the selected records for a later controller to send to `IUndoEngine`; it never calls the engine itself. Undo results may be presented after external execution. Persistent history, selective undo, redo, sessions inferred from files, and automatic ordering reversal remain deferred.
