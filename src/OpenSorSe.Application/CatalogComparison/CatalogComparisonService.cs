using OpenSorSe.Application.Catalog;
using OpenSorSe.Application.Models;
using OpenSorSe.Application.Tags;

namespace OpenSorSe.Application.CatalogComparison;

/// <summary>
/// Performs deterministic comparison over bounded historical catalog metadata without accessing stored paths.
/// </summary>
public sealed class CatalogComparisonService : ICatalogComparisonService
{
    private static readonly IReadOnlyList<string> EmptyValues = Array.Empty<string>();

    /// <inheritdoc />
    public CatalogComparisonResult Compare(
        CatalogEntry baseline,
        CatalogEntry current,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEntry(baseline, nameof(baseline));
        ValidateEntry(current, nameof(current));

        var baselineMap = BuildFileMap(baseline, cancellationToken, out var baselineDuplicates);
        var currentMap = BuildFileMap(current, cancellationToken, out var currentDuplicates);
        var changes = new List<CatalogFileChange>(Math.Min(
            CatalogComparisonLimits.MaximumChangeCount,
            baselineMap.Count + currentMap.Count));
        var identities = baselineMap.Keys
            .Concat(currentMap.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(identity => identity, StringComparer.Ordinal);

        foreach (var identity in identities)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hasBaseline = baselineMap.TryGetValue(identity, out var baselineState);
            var hasCurrent = currentMap.TryGetValue(identity, out var currentState);
            if (!hasBaseline)
            {
                changes.Add(new CatalogFileChange(
                    identity,
                    CatalogComparisonChangeKind.Added,
                    null,
                    currentState!.File,
                    EmptyValues,
                    currentState.Tags,
                    EmptyValues));
                continue;
            }

            if (!hasCurrent)
            {
                changes.Add(new CatalogFileChange(
                    identity,
                    CatalogComparisonChangeKind.Removed,
                    baselineState!.File,
                    null,
                    baselineState.Tags,
                    EmptyValues,
                    EmptyValues));
                continue;
            }

            var changedFields = CompareFields(baselineState!, currentState!);
            changes.Add(new CatalogFileChange(
                identity,
                changedFields.Count == 0 ? CatalogComparisonChangeKind.Unchanged : CatalogComparisonChangeKind.Modified,
                baselineState!.File,
                currentState!.File,
                baselineState.Tags,
                currentState.Tags,
                changedFields));
        }

        var orderedChanges = changes
            .OrderBy(change => GetKindOrder(change.Kind))
            .ThenBy(change => change.PathIdentity, StringComparer.Ordinal)
            .ThenBy(change => change.CurrentFile?.FullPath ?? change.BaselineFile?.FullPath, StringComparer.Ordinal)
            .ThenBy(change => change.CurrentFile?.Id ?? change.BaselineFile?.Id, StringComparer.Ordinal)
            .ToArray();
        var statistics = new CatalogComparisonStatistics(
            baseline.Snapshot.Files.Count,
            current.Snapshot.Files.Count,
            orderedChanges.Count(change => change.Kind == CatalogComparisonChangeKind.Added),
            orderedChanges.Count(change => change.Kind == CatalogComparisonChangeKind.Removed),
            orderedChanges.Count(change => change.Kind == CatalogComparisonChangeKind.Modified),
            orderedChanges.Count(change => change.Kind == CatalogComparisonChangeKind.Unchanged),
            baselineDuplicates + currentDuplicates);

        return new CatalogComparisonResult(
            baseline.Id,
            baseline.DisplayName,
            baseline.SavedAtUtc,
            current.Id,
            current.DisplayName,
            current.SavedAtUtc,
            CompareScope(baseline.SourceRoots, current.SourceRoots),
            statistics,
            Array.AsReadOnly(orderedChanges));
    }

    private static void ValidateEntry(CatalogEntry entry, string parameterName)
    {
        if (entry.Snapshot?.Files is null || entry.AcceptedTags is null || entry.SourceRoots is null)
        {
            throw new ArgumentException("A complete catalog entry is required.", parameterName);
        }

        if (entry.Snapshot.Files.Count > CatalogComparisonLimits.MaximumFilesPerSnapshot)
        {
            throw new CatalogCapacityExceededException(
                $"A snapshot comparison accepts at most {CatalogComparisonLimits.MaximumFilesPerSnapshot} files per side.");
        }

        if (entry.SourceRoots.Count > CatalogLimits.MaximumSourceRootCount ||
            entry.AcceptedTags.Count > CatalogComparisonLimits.MaximumFilesPerSnapshot * UserTagLimits.MaximumAcceptedTagsPerFile)
        {
            throw new CatalogCapacityExceededException("Catalog comparison metadata exceeds its supported capacity.");
        }

        if (entry.Snapshot.Files.Any(file => file is null || string.IsNullOrWhiteSpace(file.Id) || string.IsNullOrWhiteSpace(file.FullPath)))
        {
            throw new InvalidDataException("A catalog comparison file record is invalid.");
        }


        if (entry.SourceRoots.Any(root => string.IsNullOrWhiteSpace(root) ||
                                          root.Length > CatalogLimits.MaximumSourceRootLength ||
                                          root.Any(char.IsControl) ||
                                          !CatalogPathIdentity.IsAbsolute(root)) ||
            entry.AcceptedTags
                .Where(tag => tag is not null &&
                              tag.AcceptanceState == TagAcceptanceState.Accepted &&
                              tag.Source != TagSource.Deterministic)
                .Any(tag => string.IsNullOrWhiteSpace(tag.FileId) ||
                            string.IsNullOrWhiteSpace(tag.NormalizedValue) ||
                            tag.NormalizedValue.Length > UserTagLimits.MaximumTagLength ||
                            tag.NormalizedValue.Any(char.IsControl)) ||
            entry.AcceptedTags
                .Where(tag => tag is not null &&
                              tag.AcceptanceState == TagAcceptanceState.Accepted &&
                              tag.Source != TagSource.Deterministic)
                .GroupBy(tag => tag.FileId, StringComparer.Ordinal)
                .Any(group => group.Count() > UserTagLimits.MaximumAcceptedTagsPerFile))
        {
            throw new InvalidDataException("Catalog comparison metadata is invalid.");
        }
    }

    private static Dictionary<string, FileState> BuildFileMap(
        CatalogEntry entry,
        CancellationToken cancellationToken,
        out int duplicateCount)
    {
        var tagsByFile = entry.AcceptedTags
            .Where(tag => tag is not null &&
                          tag.AcceptanceState == TagAcceptanceState.Accepted &&
                          tag.Source != TagSource.Deterministic &&
                          !string.IsNullOrWhiteSpace(tag.FileId) &&
                          !string.IsNullOrWhiteSpace(tag.NormalizedValue))
            .GroupBy(tag => tag.FileId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)Array.AsReadOnly(group
                    .Select(tag => tag.NormalizedValue.Trim().ToLowerInvariant())
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray()),
                StringComparer.Ordinal);
        var map = new Dictionary<string, FileState>(StringComparer.Ordinal);
        duplicateCount = 0;
        foreach (var file in entry.Snapshot.Files
                     .OrderBy(file => file.Id, StringComparer.Ordinal)
                     .ThenBy(file => file.FullPath, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var identity = CatalogPathIdentity.Normalize(file.FullPath);
            var tags = tagsByFile.TryGetValue(file.Id, out var fileTags) ? fileTags : EmptyValues;
            if (!map.TryAdd(identity, new FileState(file, tags)))
            {
                duplicateCount++;
            }
        }

        return map;
    }

    private static IReadOnlyList<string> CompareFields(FileState baseline, FileState current)
    {
        var changed = new List<string>(8);
        if (baseline.File.SizeInBytes != current.File.SizeInBytes)
        {
            changed.Add("size");
        }

        if (baseline.File.LastWriteTimeUtc != current.File.LastWriteTimeUtc)
        {
            changed.Add("last modified");
        }

        if (!string.Equals(baseline.File.NormalizedExtension, current.File.NormalizedExtension, StringComparison.OrdinalIgnoreCase))
        {
            changed.Add("extension");
        }

        if (baseline.File.Category != current.File.Category)
        {
            changed.Add("category");
        }

        if (!string.Equals(baseline.File.ClassificationDisplay, current.File.ClassificationDisplay, StringComparison.Ordinal))
        {
            changed.Add("classification");
        }

        if (baseline.File.DuplicateStatus != current.File.DuplicateStatus)
        {
            changed.Add("duplicate status");
        }

        if (baseline.File.HasPlannedOperation != current.File.HasPlannedOperation)
        {
            changed.Add("planned-operation preview");
        }

        if (!baseline.Tags.SequenceEqual(current.Tags, StringComparer.Ordinal))
        {
            changed.Add("tags");
        }

        return Array.AsReadOnly(changed.ToArray());
    }

    private static CatalogScopeMatch CompareScope(IReadOnlyList<string> baselineRoots, IReadOnlyList<string> currentRoots)
    {
        if (baselineRoots.Count == 0 || currentRoots.Count == 0)
        {
            return CatalogScopeMatch.Unknown;
        }

        var baselineIdentities = baselineRoots.Select(CatalogPathIdentity.Normalize).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal);
        var currentIdentities = currentRoots.Select(CatalogPathIdentity.Normalize).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal);
        return baselineIdentities.SequenceEqual(currentIdentities, StringComparer.Ordinal)
            ? CatalogScopeMatch.Same
            : CatalogScopeMatch.Different;
    }

    private static int GetKindOrder(CatalogComparisonChangeKind kind) => kind switch
    {
        CatalogComparisonChangeKind.Added => 0,
        CatalogComparisonChangeKind.Removed => 1,
        CatalogComparisonChangeKind.Modified => 2,
        CatalogComparisonChangeKind.Unchanged => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private sealed record FileState(ResultFile File, IReadOnlyList<string> Tags);
}
