# Implementation Specification

| Property | Value |
|----------|-------|
| Spec ID | 017 |
| Component | Results View |
| Project | OpenSorSe.UI |
| Version | 1.0 |
| Target Release | v0.1 |
| Status | Draft |

---

# Purpose

The Results View presents the outcome of the analysis phase and allows the user to review all proposed actions before any filesystem changes occur.

No file operations may be executed from this view without explicit user confirmation.

---

# Why

Users should always understand what OpenSorSe intends to do before their files are modified.

The Results View provides transparency and allows users to review, adjust, or cancel the planned operations.

---

# Responsibilities

The Results View shall:

- Display scan results.
- Display proposed file operations.
- Display detected duplicates.
- Display file classifications.
- Display warnings and conflicts.
- Allow users to review the planned actions.
- Allow users to proceed to execution.
- Allow users to cancel the operation.

---

# Does NOT

The Results View must NOT:

- Scan files.
- Execute file operations.
- Modify the filesystem.
- Calculate hashes.
- Evaluate organization rules.
- Resolve conflicts.

---

# Inputs

- ScanResult
- MovePlan
- Duplicate information
- Rule Engine results

---

# Outputs

The component provides:

- User approval.
- User cancellation.
- Navigation events.

---

# Workflow

1. Load analysis results.
2. Display planned operations.
3. Display warnings.
4. Allow user review.
5. User confirms or cancels.
6. Continue to execution.

---

# Assumptions

- The analysis pipeline has completed successfully.
- A valid MovePlan exists.

---

# Acceptance Criteria

The implementation is complete when:

- Results are displayed correctly.
- Planned operations are visible.
- Warnings are displayed.
- User can approve execution.
- User can cancel execution.
- UI tests pass.

---

# Layout

+------------------------------------------------------------+
| Results                                                    |
+------------------------------------------------------------+

Summary

Files Scanned:        5,243

Files To Move:          814

Duplicates Found:        52

Warnings:                 3

------------------------------------------------------------

Planned Operations

------------------------------------------------------------

📄 Invoice.pdf

Documents → Documents/Invoices

------------------------------------------------------------

🎵 Song.mp3

Downloads → Music

------------------------------------------------------------

⚠ Duplicate

photo.jpg

------------------------------------------------------------

[ Back ]

[ Cancel ]

[ Execute ]

------------------------------------------------------------

Status

Waiting for user confirmation

---

# Future

Not part of v0.1:

- Search.
- Filters.
- Sorting.
- Grouping.
- Side-by-side comparison.
- AI explanations.
- Manual editing of individual operations.

---

# Dependencies

Depends on:

- 007 - Move Planner
- 008 - Conflict Resolver

Required by:

- 009 - Move Executor

---

# Autonomous v0.1 Decisions

The draft references an undefined `MovePlan` and does not define a result aggregate. v0.1 consumes existing `ConflictResolutionResult` plus an explicitly supplied ordered `FileEntry` collection. It presents accepted `PlannedOperation` values, conflict messages, deterministic summary totals, duplicate group identifiers already attached to files, and immutable classifications already attached to files.

The view model exposes approval, cancellation, and back events only. Approval emits a read-only snapshot of the accepted operations for a later controller; it does not invoke the executor. Loading copies collection membership but never modifies `FileEntry` or `PlannedOperation` records. Search, sorting, filtering, previews, manual plan editing, AI explanations, execution orchestration, and navigation policy are deferred.
