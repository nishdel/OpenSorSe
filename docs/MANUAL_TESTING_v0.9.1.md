# OpenSorSe v0.9.1 Manual Verification

Use a small disposable folder and record a before/after manifest. Ollama is optional for the non-AI checks and is externally managed; never use valuable files to test a pre-release build.

## Preparation

1. Build and launch the `v0.9.1` branch.
2. If an older `settings.json` is present, preserve a copy and verify it loads without being deleted or manually migrated.
3. Prepare a disposable folder containing a few documents, images, duplicate files, and safely named subfolders.
4. Record filenames, paths, sizes, and timestamps before testing.

## Settings and visibility

1. Start with default settings. Verify Dashboard, Scan, Results, Duplicate review, Rules, Saved catalog, Catalog search, Settings, and About are available.
2. Verify **Enable AI features** and **Show advanced features** are off.
3. Verify AI capability/provider controls and the Results AI panel are hidden. Verify Compare snapshots, Diagnostics, and Operation history are hidden.
4. Turn on **Enable AI features**. Verify the two capability switches appear in Settings immediately; save and verify the Results AI panel remains absent until a capability is enabled.
5. Enable only **Enable file rename suggestions**, save, and verify only the rename proposal controls appear for an eligible selected result.
6. Disable rename, enable only **Enable folder structure suggestions**, save, and verify only folder-structure controls appear when results exist.
7. Enable both capabilities and verify neither disables or resets the other.
8. Turn on **Show advanced features**, save, and verify Compare snapshots, Diagnostics, Operation history, detailed logging, and advanced Ollama settings appear.
9. Verify the four combinations independently: neither flag; advanced only; AI only; both AI and advanced.
10. While an advanced page is selected, turn advanced mode off and save. Verify navigation safely returns to Dashboard and the hidden page cannot be reached by keyboard navigation.
11. Restart and verify all saved flags and provider values persist. Disable AI/advanced again and confirm hidden dependent values are preserved when re-enabled.

## Provider failures

1. With AI and advanced mode enabled, stop Ollama or use an unreachable test endpoint. Test the connection and generate a suggestion. Verify concise unavailable messages and continued scanning/search use.
2. Start Ollama with no installed model, or clear the selected model. Verify a suggestion is blocked before generation and the message directs the user to advanced Settings.
3. Select a nonexistent model and verify a missing-model failure is controlled.
4. Cancel an in-flight connection or generation request and verify a cancellation message with no stale suggestion.
5. If practical with a controlled stub endpoint, return HTTP errors, an empty response, oversized response, malformed JSON, Markdown-fenced JSON, and an unsafe but well-formed response. Verify each is rejected without raw exception text.

## Suggestion workflow

1. Select one known result and request a rename. Verify the proposal repeats that source, preserves its extension, has no path component, and is labelled AI-generated, unverified, and review-only.
2. Edit the proposed base filename to another safe value, accept it as a review decision, and verify the actual file remains unchanged.
3. Generate again and reject the proposal. Verify dismissal does not change the file.
4. Select result metadata and request a folder structure. Verify it contains only logical relative folders and assignments to known selected files.
5. Accept or reject the folder proposal and verify no folder is created and no file is moved.
6. Disable the active capability while a proposal is visible, save Settings, and verify the proposal and action controls are cleared/hidden.
7. Disable global AI, save, and verify all AI UI disappears and no provider request occurs.

## Non-AI regression smoke test

1. Scan the disposable folder; verify progress, cancellation, warnings, and result totals.
2. Filter, sort, page, use ranked search, and inspect result details.
3. Review exact duplicate groups and return to the corresponding result rows.
4. Add/remove a manual tag and verify deterministic and accepted tags remain searchable without AI.
5. Enable the catalog, save a bounded snapshot, reopen it, name it, and inspect captured source scope.
6. Search the catalog, save/rerun/remove a named search, and verify hits are not persisted.
7. With advanced mode enabled, compare two snapshots and verify filters, scope status, cancellation, and historical opening.
8. Recheck the disposable-folder manifest. No filename, path, content, size, or timestamp should have changed because of an OpenSorSe suggestion or review action.

## Expected result

All UI visibility combinations are coherent, settings survive restart, disabled commands cannot communicate with Ollama, failures remain isolated, and no suggestion or review action modifies the selected filesystem.
