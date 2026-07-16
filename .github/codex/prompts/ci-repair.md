# CI Repair

Inspect the current repository, its governing documentation, and the supplied CI failure output before making any change.

Repair only a confirmed compiler, analyzer, test, project-reference, dependency-injection, or specification-compliance defect. Identify the root cause and make the smallest safe correction that resolves it while preserving the governing specifications. Do not suppress warnings, lower warning severity, weaken analyzers, weaken tests or assertions, introduce unrelated refactoring, or add feature work.

When the workflow environment permits, run the following verification sequence after every repair pass:

```text
dotnet restore TidyMind.sln
dotnet build TidyMind.sln --configuration Release --no-restore
dotnet test TidyMind.sln --configuration Release --no-build
```

Attempt no more than three repair passes. Produce a concise repair report stating the observed failure, root cause, changed files, verification results, and any remaining blocker. Stop and report the issue if the failure requires a missing product decision, public contract, or specification clarification.
