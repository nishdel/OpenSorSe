using OpenSorSe.Application.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Provides read-only detail values for one selected result row.
/// </summary>
public sealed record SelectedResultDetails(
    string FileName,
    string FullPath,
    string Extension,
    string Size,
    string ModifiedTime,
    string Classification,
    string DuplicateStatus,
    IReadOnlyList<ResultPlannedOperation> PlannedOperations)
{
    /// <summary>
    /// Maps one immutable snapshot row and its related display-only operations to detail values.
    /// </summary>
    /// <param name="file">The selected result row.</param>
    /// <param name="operations">The display-only operations related to the row.</param>
    /// <returns>Immutable safe detail values.</returns>
    public static SelectedResultDetails From(ResultFile file, IReadOnlyList<ResultPlannedOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(operations);
        return new SelectedResultDetails(
            file.DisplayFileName,
            file.FullPath,
            string.IsNullOrEmpty(file.NormalizedExtension) ? "No extension" : file.NormalizedExtension,
            ResultsFileRow.FormatSize(file.SizeInBytes),
            file.LastWriteTimeUtc?.ToString("u", System.Globalization.CultureInfo.InvariantCulture) ?? "Modified time unavailable",
            file.ClassificationDisplay,
            ResultsFileRow.FormatDuplicateStatus(file.DuplicateStatus),
            Array.AsReadOnly(operations.ToArray()));
    }
}
