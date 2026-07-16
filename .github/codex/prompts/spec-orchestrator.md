# v0.1 Specification Orchestrator

Implement the remaining numbered v0.1 implementation specifications one at a time in numeric order. Begin each specification by verifying that its filename, internal Spec ID, named component, and content agree with each other and with the specification sequence.

Stop and report the inconsistency if any specification is missing, duplicated, contradictory, or materially incomplete. Before implementation, inspect the relevant architecture documentation, coding standards, existing projects, public contracts, and current implementation.

Implement only the active specification. Preserve documented project boundaries and existing behavior outside its scope. Add deterministic tests for the active specification. Do not add unrelated refactoring, feature work, dependencies without documented justification, warning suppression, analyzer changes, or weakened tests.

Perform a static review before verification: check the active specification against its implementation, public XML documentation, nullable contracts, project references, architecture boundaries, deterministic test coverage, and prohibited placeholders or TODOs.

Run this verification sequence after each specification:

```text
dotnet restore TidyMind.sln
dotnet build TidyMind.sln --configuration Release --no-restore
dotnet test TidyMind.sln --configuration Release --no-build
```

For a verification failure, read the complete failure, identify the root cause, and make the smallest compliant correction. Retry no more than three repair passes. Stop and report a blocker if the solution cannot be made green without a missing product decision or specification clarification.

After a green solution, create a per-specification completion summary containing the specification identity, implementation scope, tests added or updated, static-review and verification results, and any assumptions. Continue automatically to the next specification only after the full solution is green. Never merge directly to `main`, force-push, or rewrite history. Maintain one final consolidated implementation report across completed specifications, and stop at the final documented v0.1 implementation specification. Do not create an autonomous push or merge workflow.
