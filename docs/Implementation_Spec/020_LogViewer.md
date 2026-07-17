# Implementation Specification

| Property | Value |
|----------|-------|
| Spec ID | 020 |
| Component | Log Viewer |
| Project | TidyMind.UI |
| Version | 1.0 |
| Target Release | v0.1 |
| Status | Draft |

---

# Purpose

The Log Viewer displays application logs, warnings, errors, and completed operations in a user-friendly interface.

It provides visibility into application activity for troubleshooting and auditing.

---

# Why

Users and developers need an easy way to understand what TidyMind has done, especially when diagnosing problems or reviewing completed operations.

---

# Responsibilities

The Log Viewer shall:

- Display log entries.
- Display warnings and errors.
- Display operation history.
- Allow users to filter log entries.
- Allow users to clear the displayed logs.
- Allow users to export logs.

---

# Does NOT

The Log Viewer must NOT:

- Write log entries.
- Execute business logic.
- Scan folders.
- Move files.
- Modify the filesystem.
- Execute rules.

---

# Inputs

- Log entries.
- User interaction.

---

# Outputs

The component provides:

- Filter requests.
- Export requests.
- Clear log requests.

---

# Workflow

1. Load log entries.
2. Display logs.
3. Apply filters when requested.
4. Export logs if requested.
5. Refresh the view.

---

# Assumptions

- Logging Service is available.
- Log storage is accessible.

---

# Acceptance Criteria

The implementation is complete when:

- Logs are displayed correctly.
- Errors and warnings are distinguishable.
- Filtering works.
- Logs can be exported.
- UI tests pass.

---

# Layout

+------------------------------------------------------------+
| Log Viewer                                                 |
+------------------------------------------------------------+

Filter:

[ All ▼ ]

------------------------------------------------------------

Time        Level      Message

09:15:01    INFO       Scan started

09:15:12    INFO       2,154 files discovered

09:15:15    WARNING    Folder inaccessible

09:15:18    ERROR      File locked

------------------------------------------------------------

[ Refresh ]

[ Export ]

[ Clear ]

------------------------------------------------------------

Status

154 Log Entries

---

# Future

Not part of v0.1:

- Live log streaming.
- Advanced filtering.
- Search.
- Structured JSON logs.
- Remote logs.

---

# Dependencies

Depends on:

- 011 - Logging Service

Required by:

- None

---

# Autonomous v0.1 Decisions

Specification 011 intentionally does not retain or expose individual log entry payloads, and the draft does not define a safe entry-query, export, or deletion contract. v0.1 therefore provides a privacy-safe aggregate logging-health view over `ILoggingService.GetStatistics()`. An optional severity filter selects an aggregate counter only. Refresh reads counters; clear clears the presented snapshot only.

The viewer cannot read log files, display log text, export logs, delete logs, access operation history, or mutate logging configuration. Those operations are deferred until a reviewed privacy, retention, access-control, and export contract exists.
