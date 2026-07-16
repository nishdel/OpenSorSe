# TidyMind Repository Instructions

## Governing sources

Repository documentation is the source of truth. Read it in this order:

1. The active detailed numbered specification in `docs/Implementation_Spec/` for v0.1 behavior.
2. The applicable architecture document in `docs/Architecture/`.
3. `docs/Architecture/99_Appendix/Coding_Standards.md` and `Naming_Conventions.md`.
4. Existing public contracts, tests, and implementation.

When sources conflict or a public contract, behavior, or product decision is materially missing, stop and report the gap rather than inventing it.

## Architecture and scope

- Preserve existing TidyMind project boundaries and the documented architecture.
- Use C# and the repository's existing .NET/Avalonia architecture.
- Keep dependencies directed through documented interfaces; do not create circular project references or cross component boundaries without specification support.
- Do not introduce unrelated refactoring or feature work.
- Do not introduce NuGet dependencies without documented justification.
- Preserve immutable `FileEntry` properties when adding later enrichment.
- Never perform destructive filesystem behavior unless a specification explicitly authorizes it and defines safeguards.

## Quality requirements

- Preserve nullable-reference analysis and warnings-as-errors.
- Add XML documentation to every public type, member, and enum value.
- Do not weaken analyzers, warnings, tests, or assertions.
- Do not add TODOs, placeholders, or `NotImplementedException`.
- Use deterministic tests.

## Review checklist

Before declaring work complete, confirm that the change:

- implements only the authorized specification or repair;
- preserves architecture, project boundaries, nullable analysis, warnings-as-errors, and immutable models;
- documents every public type, member, and enum value with XML documentation;
- adds or updates deterministic tests for observable behavior;
- introduces no undocumented NuGet dependency, placeholder, TODO, or destructive filesystem behavior; and
- passes the full verification sequence.

## Verification

Run this full sequence after a completed change:

```text
dotnet restore TidyMind.sln
dotnet build TidyMind.sln --configuration Release --no-restore
dotnet test TidyMind.sln --configuration Release --no-build
```

## Bounded repair policy

When verification fails:

1. Read the complete failure.
2. Identify the root cause.
3. Make the smallest correction.
4. Do not weaken warnings, analyzers, tests, or assertions.
5. Re-run the full verification sequence.
6. Stop after three unsuccessful repair attempts and report the blocker.
