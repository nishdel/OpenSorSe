# OpenSorSe v1.0 Manual Testing Guide

This checklist validates the OpenSorSe 1.0 release candidate using disposable test data. Unless a step explicitly tests an apply operation, OpenSorSe must not rename, move, delete, overwrite, or modify source files. Record the application version, operating system, .NET runtime, test-data root, OCR capability state, and result for every section.

## Results

1. Scan a folder with more files than fit vertically.
2. Open Results.
3. Scroll the file list.
4. Confirm the search/filter toolbar remains fixed.
5. Confirm result rows scroll independently.
6. Resize the window.
7. Confirm controls reflow without clipping.
8. Filter by type.
9. Filter by duplicates.
10. Filter by tag.
11. Change sorting.
12. Change page size.
13. Clear filters.
14. Confirm the result count remains correct.

## Duplicate drawer

15. Open Duplicate View.
16. Select a duplicate group.
17. Confirm the right-side drawer opens.
18. Select another group.
19. Confirm the drawer updates.
20. Close the drawer.
21. Reopen it.
22. Press Escape and confirm the drawer closes.
23. Test long filenames.
24. Test long paths.
25. Test **Open both files** for a two-file group.
26. Test **Open selected files** for a larger group.
27. Open containing folders.
28. Test a missing file.
29. Confirm partial failure is reported.
30. Confirm no file is deleted, moved, renamed, or modified.

## Global toggles

31. Navigate through every page.
32. Confirm **Enable AI features** remains visible in the shell.
33. Confirm **Show advanced features** remains visible in the shell.
34. Toggle AI.
35. Verify Settings updates.
36. Toggle Advanced.
37. Verify Settings updates.
38. Disable Advanced while on an advanced page.
39. Confirm safe navigation fallback.
40. Restart and confirm persistence.
41. Confirm enabling AI alone does not contact Ollama.

## OCR

42. Scan an image containing text.
43. Scan a scanned PDF.
44. Scan a PDF with native text.
45. Confirm native text prevents unnecessary OCR.
46. Disable OCR.
47. Confirm OCR is skipped.
48. Cancel OCR.
49. Test an oversized file.
50. Inspect OCR status.
51. Confirm source documents remain unchanged.

If no compatible local Tesseract engine is detected, confirm the interface reports **Unavailable** rather than claiming OCR succeeded. PDF rendering is built in; recognition still requires the externally installed Tesseract executable and all configured language data.

## Metadata

52. Inspect PDF metadata.
53. Inspect DOCX metadata.
54. Inspect XLSX metadata.
55. Inspect image metadata.
56. Confirm provenance.
57. Test malformed files.
58. Confirm the scan continues.

## Tags

59. View user tags.
60. View generated tag suggestions.
61. Accept a tag.
62. Reject a tag.
63. Restart and verify persistence.
64. Confirm rejected tags do not immediately reappear.
65. Filter Results by tag.

## Semantic Search

66. Build the semantic index.
67. Observe progress.
68. Cancel indexing.
69. Resume or rebuild.
70. Search by exact filename.
71. Search by user tag.
72. Search by metadata.
73. Search by OCR text.
74. Search with a natural-language phrase.
75. Inspect result explanations.
76. Delete or move a test file externally.
77. Refresh the index.
78. Confirm stale results are removed.
79. Clear the index.
80. Confirm files remain untouched.
81. Rebuild the index.

## Restructuring history

Use only disposable files for the apply test.

82. Preview a folder restructuring.
83. Confirm preview alone does not mark the folder organized.
84. Apply a restructuring using disposable files.
85. Rescan the same folder.
86. Confirm full restructuring is not proposed again.
87. Add new files.
88. Confirm incremental organization is offered.
89. Change the structure manually.
90. Confirm change detection.
91. Use **Propose restructuring again**.
92. Confirm explicit override.

## Structure History

93. Open Structure History.
94. Inspect previous runs.
95. Filter by root.
96. Filter by status.
97. Open the source structure.
98. Open the proposed structure.
99. Open the applied structure.
100. Compare previous and newer structures.
101. Inspect added, removed, moved, renamed, and unchanged nodes.
102. Test a large structure.
103. Confirm accessible textual summaries.
104. Confirm diagrams are read-only.

## Migration and regression

105. Start with existing v0.9.1 settings.
106. Confirm settings load.
107. Open an existing catalog.
108. Open existing saved searches.
109. Open existing tags.
110. Confirm no data is lost.
111. Test scanning.
112. Test cancellation.
113. Test Rules.
114. Test snapshots.
115. Test Catalog Search.
116. Test Help.
117. Test Diagnostics.
118. Test Operation History.
119. Test AI rename suggestions.
120. Test AI folder suggestions.
121. Confirm all suggestion workflows remain preview-only.

## Final OCR and AI-text hardening

122. On a system without Tesseract, use **Recheck OCR capability** and confirm the UI names Tesseract as unavailable while reporting the built-in PDF renderer separately.
123. Install or configure a supported Tesseract 5 executable with `eng`, recheck, and confirm its version and detected languages appear.
124. Select `deu` without German language data and confirm recognition is blocked with an actionable language message.
125. Install `deu`, select `deu` or `deu+eng`, recheck, and recognize a representative German image.
126. OCR a scanned multi-page PDF and confirm page boundaries/provenance are retained.
127. OCR a mixed PDF containing reliable native-text pages and scanned pages; confirm only insufficient pages require Tesseract.
128. Cancel mixed-PDF OCR and confirm the operation stops, normal scanning remains usable, and no `OpenSorSe/ocr/job-*` temporary workspace remains.
129. Exercise the page, file, time, raster-edge, extracted-text, and temporary-storage bounds and confirm each returns a controlled result.
130. Restart after changing OCR language/DPI/bounds and confirm settings persist and affected cached records are reprocessed.
131. Confirm accepted/user tags survive reprocessing and a rejected generated tag remains suppressed for an unchanged source fingerprint.
132. Enable AI but leave **Allow local AI to analyze extracted document text** off; confirm no document-text action appears or reaches the provider.
133. Enable document-text interpretation and inspect the non-local endpoint warning before using a custom remote endpoint.
134. Select one indexed document and explicitly generate a document interpretation proposal.
135. Confirm the preview is labelled AI-generated/unverified and contains only bounded type/title/tag/date/issuer/folder suggestions, confidence, and explanation.
136. Reject or dismiss the proposal and confirm no source file, folder, embedded metadata, or tag is changed automatically.
137. Return malformed, fenced, unknown-source, unsafe-folder, overlong, excessive-count, and out-of-range-confidence provider fixtures and confirm whole-response rejection.
138. Disable the global AI switch while a request is active and confirm cancellation, preview clearing, and no later provider-driven UI update.
139. Point Ollama at an unavailable endpoint/model and confirm OCR, scanning, Results, Duplicate View, catalog, tags, saved searches, semantic search, and Structure history continue to work.
140. Compare source-file hashes/timestamps before and after all OCR and AI interpretation checks and confirm no automatic filesystem modification occurred.

## Completion record

Manual testing is complete only when all failures are recorded with reproduction steps and either fixed or explicitly accepted as release limitations. Do not merge the v1.0 branch into `main` until both the inherited v0.9.1 checklist and this checklist have been completed.
