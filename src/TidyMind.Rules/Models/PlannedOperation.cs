using TidyMind.Scanner.Models;

namespace TidyMind.Rules.Models;

/// <summary>
/// Represents one immutable, non-executed filesystem operation.
/// </summary>
/// <param name="OperationId">The deterministic identifier within one plan.</param>
/// <param name="Kind">The planned operation kind.</param>
/// <param name="File">The original immutable file entry.</param>
/// <param name="SourcePath">The source path copied from <paramref name="File"/>.</param>
/// <param name="DestinationPath">The planned destination path, or null for delete.</param>
/// <param name="SelectedRuleId">The originating selected rule identifier.</param>
/// <param name="SelectedRuleName">The originating selected rule name.</param>
/// <param name="SelectedRulePriority">The originating selected rule priority.</param>
public sealed record PlannedOperation(string OperationId, PlannedOperationKind Kind, FileEntry File, string SourcePath, string? DestinationPath, string? SelectedRuleId, string? SelectedRuleName, int? SelectedRulePriority);
