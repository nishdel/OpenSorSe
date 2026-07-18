using System.Globalization;
using OpenSorSe.Application.Models;
using OpenSorSe.Scanner.Models;
using ScannerDuplicateStatus = OpenSorSe.Scanner.Models.DuplicateStatus;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Provides user-friendly, immutable file details for one row in the read-only results list.
/// </summary>
public sealed record ResultsFileRow(
    string FileId,
    string FileName,
    string ContainingFolder,
    string FullPath,
    string Extension,
    string Size,
    string DuplicateStatus,
    string ModifiedTime,
    string Classification,
    string PlannedOperation,
    string Tags,
    string MatchExplanation)
{
    /// <summary>
    /// Creates a display row without changing the supplied scanner model.
    /// </summary>
    /// <param name="file">The file discovered by the completed scan.</param>
    /// <returns>A presentation-only projection of the file details.</returns>
    public static ResultsFileRow FromFileEntry(FileEntry file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var metadata = file.Metadata;
        var fileName = metadata?.FileName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = file.FullPath;
        }

        var containingFolder = Path.GetDirectoryName(file.FullPath);
        var extension = metadata?.Extension;
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = Path.GetExtension(file.FullPath);
        }

        return new ResultsFileRow(
            string.Empty,
            fileName,
            string.IsNullOrWhiteSpace(containingFolder) ? "Not available" : containingFolder,
            file.FullPath,
            string.IsNullOrWhiteSpace(extension) ? "No extension" : extension,
            FormatSize(metadata?.SizeInBytes),
            FormatDuplicateStatus(file.Duplicate?.Status),
            metadata?.LastWriteTimeUtc?.ToString("u", CultureInfo.InvariantCulture) ?? "Modified time unavailable",
            file.Classification?.Category.ToString() ?? "Unclassified",
            "No",
            "",
            "");
    }

    /// <summary>Creates a bounded-page display row from a projected result file.</summary>
    /// <param name="file">The projected file to present.</param>
    /// <param name="tags">The accepted application-owned tags for the current in-memory result session.</param>
    /// <param name="match">The optional deterministic text-match explanation for the active query.</param>
    /// <returns>A presentation-only row.</returns>
    public static ResultsFileRow FromResultFile(ResultFile file, IReadOnlyList<TagAssociation>? tags = null, ResultSearchMatch? match = null)
    {
        ArgumentNullException.ThrowIfNull(file);
        var containingFolder = Path.GetDirectoryName(file.FullPath);
        return new ResultsFileRow(
            file.Id,
            file.DisplayFileName,
            string.IsNullOrWhiteSpace(containingFolder) ? "Not available" : containingFolder,
            file.FullPath,
            string.IsNullOrEmpty(file.NormalizedExtension) ? "No extension" : file.NormalizedExtension,
            FormatSize(file.SizeInBytes),
            FormatDuplicateStatus(file.DuplicateStatus),
            file.LastWriteTimeUtc?.ToString("u", CultureInfo.InvariantCulture) ?? "Modified time unavailable",
            file.ClassificationDisplay,
            file.HasPlannedOperation ? "Yes" : "No",
            FormatTags(tags),
            match?.Explanation ?? "No text query applied.");
    }

    internal static string FormatSize(long? sizeInBytes)
    {
        if (sizeInBytes is null)
        {
            return "Size unavailable";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)sizeInBytes.Value;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{sizeInBytes.Value.ToString(CultureInfo.InvariantCulture)} {units[unitIndex]}"
            : $"{size.ToString("0.##", CultureInfo.InvariantCulture)} {units[unitIndex]}";
    }

    internal static string FormatDuplicateStatus(ScannerDuplicateStatus? status) => status switch
    {
        ScannerDuplicateStatus.Duplicate => "Exact duplicate",
        ScannerDuplicateStatus.Unique => "No exact duplicate",
        ScannerDuplicateStatus.Unknown => "Not checked",
        null => "Not checked",
        _ => "Not checked",
    };

    private static string FormatTags(IReadOnlyList<TagAssociation>? tags) => tags is null || tags.Count == 0
        ? "—"
        : string.Join(", ", tags
            .Where(tag => tag.AcceptanceState == TagAcceptanceState.Accepted)
            .Select(tag => tag.DisplayName)
            .Distinct(StringComparer.OrdinalIgnoreCase));
}
