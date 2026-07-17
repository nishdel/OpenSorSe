namespace OpenSorSe.Application.Models;

/// <summary>
/// Represents one existing exact-duplicate group without exposing its content hash.
/// </summary>
public sealed record ResultDuplicateGroup(
    string GroupId,
    int Ordinal,
    IReadOnlyList<string> MemberFileIds,
    int MemberCount,
    long? CommonFileSizeInBytes,
    long? PotentialReclaimableBytes);
