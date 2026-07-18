using OpenSorSe.Application.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Provides display-only summary values for one existing exact-duplicate group.
/// </summary>
public sealed record DuplicateReviewGroupRow(
    ResultDuplicateGroup Group,
    string MemberSummary,
    string CommonFileSize,
    string PotentialReclaimableSpace);
