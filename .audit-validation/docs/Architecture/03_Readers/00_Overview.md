# Readers Overview

> This document describes a possible future subsystem for extracting structured content from supported file types.

---

## Implementation Status

Readers are not implemented in the validated v0.2 release. The current application reads filesystem metadata and calculates SHA-256 hashes during scanning, but it does not read document contents or provide PDF, DOCX, spreadsheet, image, media, archive, or OCR processing.

The documents in this directory record future architectural direction only. They do not describe shipped components, supported file formats, or a committed release plan.

---

## Purpose

A future Readers subsystem could provide a consistent boundary for extracting content and format-specific metadata from supported files. It would keep format handling separate from scanning, rules, and presentation while allowing individual reader implementations to evolve independently.

---

## Prospective Responsibilities

If introduced in a future release, the subsystem would be responsible for:

* Selecting a reader for a supported file type.
* Extracting content and format-specific metadata without modifying the source file.
* Isolating failures to the affected file.
* Reporting extraction outcomes through the application's diagnostics model.

It would not own filesystem traversal, result presentation, persistence, AI inference, or file-changing operations.

---

## Relationship to the Current Release

The v0.2 Scanner remains the current boundary for recursive traversal, basic metadata collection, hashing, error reporting, progress, and cancellation. Reader architecture should be introduced only when a release explicitly scopes content extraction and its privacy, performance, and failure-handling requirements.

---

## Related Documents

* [Scanner Overview](../02_Scanner/00_Overview.md)
* [System Overview](../00_System/00_Overview.md)
* [Release Status](../../RELEASE_STATUS.md)
