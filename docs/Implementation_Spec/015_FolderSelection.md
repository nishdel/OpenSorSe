# Implementation Specification

| Property | Value |
|----------|-------|
| Spec ID | 015 |
| Component | Folder Selection |
| Project | TidyMind.UI |
| Version | 1.0 |
| Target Release | v0.1 |
| Status | Draft |

---

# Purpose

The Folder Selection view allows users to choose one or more folders for TidyMind to analyze.

It serves as the starting point of every scan.

---

# Why

Before TidyMind can organize files, it needs to know which locations the user wants to process.

The Folder Selection view provides a simple, safe, and intuitive way to define scan targets.

---

# Responsibilities

The Folder Selection view shall:

- Allow users to add one or more folders.
- Allow users to remove selected folders.
- Display the selected folders.
- Validate selected paths.
- Start a scan.
- Remember recently used folders.

---

# Does NOT

The Folder Selection view must NOT:

- Scan folders.
- Move files.
- Read file contents.
- Execute organization rules.
- Calculate hashes.
- Classify files.
- Access AI services.

---

# Inputs

- User folder selections.
- Application configuration.
- Recent folder history.

---

# Outputs

The component provides:

- Selected folder list.
- Scan request.
- Navigation events.

---

# Workflow

1. Open the Folder Selection view.
2. Add or remove folders.
3. Validate folder paths.
4. Display the selected folders.
5. User starts the scan.
6. Pass the scan request to the application.

---

# Assumptions

- The application has started successfully.
- Folder picker is available.
- Selected folders are accessible.

---

# Acceptance Criteria

The implementation is complete when:

- Users can add folders.
- Users can remove folders.
- Invalid folders are detected.
- Recently used folders are available.
- The Scan button starts a scan request.
- UI tests pass.

---

# Layout

+------------------------------------------------------------+
| Folder Selection                                           |
+------------------------------------------------------------+

 Selected Folders

 ----------------------------------------------------------
 | C:\Users\John\Documents                                 |
 | D:\Photos                                               |
 | E:\Downloads                                            |
 ----------------------------------------------------------

 [ Add Folder ]

 [ Remove Folder ]

------------------------------------------------------------

 Recent Locations

 • Documents

 • Downloads

 • Pictures

------------------------------------------------------------

                     [ Start Scan ]

------------------------------------------------------------

 Status

 Ready

---

# Future

Not part of v0.1:

- Drag & Drop folders.
- Favorite folders.
- Network locations.
- Saved scan profiles.
- Folder tags.
- Folder exclusion rules.

---

# Dependencies

Depends on:

- 013 - Main Window
- 012 - Configuration Manager

Required by:

- 001 - File Scanner

flowchart LR

UI["UI Views"]
    --> APP["Application Layer"]

APP --> Scanner["001 File Scanner"]
APP --> Metadata["002 File Metadata"]
APP --> Hasher["003 File Hasher"]
APP --> Classifier["004 File Classifier"]
APP --> Rules["006 Rule Engine"]
APP --> Planner["007 Move Planner"]
APP --> Executor["009 Move Executor"]