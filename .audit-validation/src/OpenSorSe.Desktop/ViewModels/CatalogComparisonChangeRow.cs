using OpenSorSe.Application.CatalogComparison;
using OpenSorSe.Application.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>Provides bounded text presentation for one historical stored-metadata change.</summary>
public sealed record CatalogComparisonChangeRow(
    string PathIdentity,
    CatalogComparisonChangeKind Kind,
    string FileName,
    string FullPath,
    string ChangedFieldsText,
    string BaselineDetails,
    string CurrentDetails)
{
    /// <summary>Gets a user-facing change-kind label that does not rely on color.</summary>
    public string KindText => Kind.ToString();

    /// <summary>Creates a display row from one immutable application comparison value.</summary>
    public static CatalogComparisonChangeRow FromModel(CatalogFileChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        var displayFile = change.CurrentFile ?? change.BaselineFile
            ?? throw new ArgumentException("A comparison change must contain at least one file record.", nameof(change));
        var changedFields = change.Kind switch
        {
            CatalogComparisonChangeKind.Added => "Stored path is present only in the current snapshot.",
            CatalogComparisonChangeKind.Removed => "Stored path is present only in the baseline snapshot.",
            CatalogComparisonChangeKind.Modified => $"Changed stored fields: {string.Join(", ", change.ChangedFields)}.",
            CatalogComparisonChangeKind.Unchanged => "Compared stored metadata is unchanged.",
            _ => throw new ArgumentOutOfRangeException(nameof(change)),
        };
        return new CatalogComparisonChangeRow(
            change.PathIdentity,
            change.Kind,
            displayFile.DisplayFileName,
            displayFile.FullPath,
            changedFields,
            Describe(change.BaselineFile, change.BaselineTags),
            Describe(change.CurrentFile, change.CurrentTags));
    }

    private static string Describe(ResultFile? file, IReadOnlyList<string> tags)
    {
        if (file is null)
        {
            return "Not present.";
        }

        var size = file.SizeInBytes is null ? "unknown size" : $"{file.SizeInBytes:N0} bytes";
        var modified = file.LastWriteTimeUtc?.ToString("u") ?? "unknown modified time";
        var tagText = tags.Count == 0 ? "none" : string.Join(", ", tags);
        return $"{size}; modified {modified}; {file.ClassificationDisplay}; duplicate status {file.DuplicateStatus}; tags {tagText}.";
    }
}
