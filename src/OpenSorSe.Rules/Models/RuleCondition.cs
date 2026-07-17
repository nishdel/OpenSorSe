using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Rules.Models;

/// <summary>
/// Defines one deterministic condition within a file rule.
/// </summary>
/// <param name="Kind">The condition kind.</param>
/// <param name="StringValue">The required string value for filename or extension conditions.</param>
/// <param name="LongValue">The required byte value for size conditions.</param>
/// <param name="CategoryValue">The required category for category conditions.</param>
/// <param name="DuplicateStatusValue">The required duplicate status for duplicate conditions.</param>
public sealed record RuleCondition(
    RuleConditionKind Kind,
    string? StringValue = null,
    long? LongValue = null,
    FileCategory? CategoryValue = null,
    DuplicateStatus? DuplicateStatusValue = null);
