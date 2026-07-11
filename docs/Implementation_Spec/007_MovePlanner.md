# Implementation Specification

| Property | Value |
|----------|-------|
| Spec ID | 007 |
| Component | Move Planner |
| Version | 1.0 |
| Target Release | v0.1 |
| Status | Draft |

---

# Purpose

The Move Planner converts the decisions produced by the Rule Engine into a safe execution plan.

It prepares every required file operation but does not perform any filesystem changes.

---

# Why

Separating planning from execution allows TidyMind to:

- Preview planned changes.
- Validate operations before execution.
- Detect problems early.
- Support Undo.
- Prevent accidental file modifications.

---

# Responsibilities

The Move Planner shall:

- Create a move plan for every file with an action.
- Determine the source and destination paths.
- Verify that destination paths are valid.
- Skip files that require no action.
- Produce a complete execution plan.
- Return planning statistics.

---

# Does NOT

The Move Planner must NOT:

- Move files.
- Rename files.
- Delete files.
- Create directories.
- Resolve filename conflicts.
- Modify the filesystem.
- Execute operations.

---

# Inputs

- Collection of `FileEntry` objects.
- Rule Engine decisions.
- Application configuration.

---

# Outputs

The component returns:

- MovePlan
- Planned move operations.
- Planning statistics.
- Planning errors.

---

# Workflow

1. Receive processed files.
2. Read the Rule Engine decision.
3. Calculate the destination path.
4. Validate the planned operation.
5. Add the operation to the MovePlan.
6. Repeat for all files.
7. Return the completed MovePlan.

---

# Acceptance Criteria

The implementation is complete when:

- Every actionable file receives a planned operation.
- Destination paths are generated correctly.
- Invalid plans are reported.
- Files requiring no action are ignored.
- No filesystem modifications occur.
- Unit tests pass.

---

# Future

Not part of v0.1:

- Move optimization.
- Batch operations.
- Copy operations.
- Delete planning.
- Cloud storage planning.

---

# Dependencies

Depends on:

- 006 - Rule Engine

Required by:

- 008 - Conflict Resolver