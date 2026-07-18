using OpenSorSe.Scanner.Models;
namespace OpenSorSe.Scanner;
/// <summary>Classifies entries using ordered metadata rules.</summary>
public interface IFileClassifier
{
    /// <summary>Classifies supplied entries.</summary>
    Task<FileClassificationResult> ClassifyAsync(IReadOnlyCollection<FileEntry> files, FileClassificationOptions? options = null, CancellationToken cancellationToken = default);
}
