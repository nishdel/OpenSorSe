# Implementation Specification

| Property | Value |
|----------|-------|
| Spec ID | 014 |
| Component | Dashboard |
| Project | TidyMind.UI |
| Version | 1.0 |
| Target Release | v0.1 |
| Status | Draft |

---

# Purpose

The Dashboard is the application's home screen.

It provides users with an overview of TidyMind, quick access to common actions, and a summary of recent activity.

---

# Why

The Dashboard gives users a central place to begin using TidyMind without needing to navigate through multiple screens.

It should answer:

- What can I do?
- What happened recently?
- What should I do next?

---

# Responsibilities

The Dashboard shall:

- Display application status.
- Display recent scans.
- Display recent organization history.
- Display quick actions.
- Display basic statistics.
- Navigate to other parts of the application.

---

# Does NOT

The Dashboard must NOT:

- Scan folders.
- Execute business logic.
- Move files.
- Calculate hashes.
- Execute organization rules.
- Modify configuration.

---

# Inputs

- Application state.
- Recent scan history.
- Statistics.
- User interactions.

---

# Outputs

The Dashboard provides:

- Navigation requests.
- User actions.
- Dashboard updates.

---

# Workflow

1. Load dashboard.
2. Display application summary.
3. Display recent activity.
4. Display quick actions.
5. Handle user interaction.
6. Navigate to selected feature.

---

# Assumptions

- Application has started successfully.
- Configuration has been loaded.

---

# Acceptance Criteria

The implementation is complete when:

- Dashboard loads successfully.
- Recent activity is displayed.
- Quick actions function correctly.
- Statistics update correctly.
- Navigation works.
- UI tests pass.

---

# Layout

+------------------------------------------------------------+
|                     Dashboard                              |
+------------------------------------------------------------+

  Recent Activity

  • Last Scan
  • Files Organized
  • Duplicate Files Found

--------------------------------------------------------------

 Quick Actions

 [ Scan Folder ]

 [ View Results ]

 [ Settings ]

--------------------------------------------------------------

 Statistics

 Files Scanned
 Files Organized
 Duplicate Files
 Storage Saved

--------------------------------------------------------------

 Status

 Ready

---

# Future

Not part of v0.1:

- Widgets.
- Custom dashboard.
- AI recommendations.
- Charts.
- Productivity statistics.
- Themes.

---

# Dependencies

Depends on:

- 013 - Main Window

Required by:

- None

---

# Autonomous v0.1 Decisions

Dashboard state is read-only and starts with empty current-session statistics because persistent scan history and reports are later specifications. The three quick actions navigate only to Scan, Results, and Settings; they do not invoke scanners, executors, or configuration services. The view is hosted by the existing Main Window only when Dashboard is selected.
