# Implementation Specification Index

This index distinguishes historical foundation specifications from release-specific packages. Implemented code and the latest release proposal are authoritative when an older planning document describes a then-future boundary.

| Release | Status | Specification package |
| --- | --- | --- |
| v0.1 Foundation | Historical / complete | [Specifications 001–028](../Implementation_Spec/) and archived coding prompts |
| v0.2 Results Exploration | Complete | [Release proposal](v0.2/00_v0.2_Release_Proposal.md), specifications 029–031, and decisions |
| v0.3 Suggestions and Ranked Search | Complete | [Release proposal](v0.3/00_v0.3_Release_Proposal.md), specifications 032–034 |
| v0.4 Local Catalog | Complete | [Release proposal](v0.4/00_v0.4_Release_Proposal.md), specifications 035–036, and decisions |
| v0.5 Catalog Search and Maintenance | Complete | [Release proposal](v0.5/00_v0.5_Release_Proposal.md), specifications 037–038, and decisions |
| v0.6 User-Managed Tags | Complete | [Release proposal](v0.6/00_v0.6_Release_Proposal.md), specification 039, and decisions |
| v0.7 Saved Catalog Searches | Complete | [Release proposal](v0.7/00_v0.7_Release_Proposal.md), specifications 040–041, and decisions |
| v0.8 Snapshot Identity and Scope | Complete | [Release proposal](v0.8/00_v0.8_Release_Proposal.md), specifications 042-043, and decisions |
| v0.9 Historical Snapshot Comparison | Complete / audited | [Release proposal](v0.9/00_v0.9_Release_Proposal.md), specifications 044-045, [audit corrections](v0.9/AUDIT_CORRECTIONS.md), and decisions |
| v0.9.1 Optional AI and Feature Controls | Complete; manual UI verification pending | [Release proposal](v0.9.1/00_v0.9.1_Release_Proposal.md), [specification 046](v0.9.1/046_Optional_AI_and_Advanced_Feature_Controls.md), and [decisions](v0.9.1/IMPLEMENTATION_DECISIONS.md) |

## Current boundary

v0.9.1 remains a local-first, read-only selected-file workflow. Its persistent stores are small, versioned, bounded JSON files in OpenSorSe application data. AI and advanced mode default off; AI is limited to two independent, strictly validated, metadata-only proposal capabilities and cannot communicate while disabled. Catalog snapshots/tags/names/source roots are opt-in; saved searches retain names and query text only; comparison results remain in memory. No current specification authorizes live verification, selected-user-file mutation, content extraction, semantic indexing, report export, monitoring, plugins, or automatic execution.
