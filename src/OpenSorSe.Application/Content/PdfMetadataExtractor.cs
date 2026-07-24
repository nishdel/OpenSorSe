using System.Text;
using System.Text.RegularExpressions;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.Content;

/// <summary>Reads bounded PDF information-dictionary values and simple native text without executing content.</summary>
public sealed class PdfMetadataExtractor : IMetadataExtractor
{
    private static readonly Regex PageRegex = new(@"/Type\s*/Page\b", RegexOptions.CultureInvariant);
    private static readonly Regex TextRegex = new(@"\((?<value>(?:\\.|[^\\)])*)\)\s*T[Jj]", RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public bool Supports(string normalizedExtension) => normalizedExtension == ".pdf";

    /// <inheritdoc />
    public async Task<MetadataExtractionResult> ExtractAsync(
        FileEntry file,
        long maximumInputBytes,
        int maximumPages,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(file.FullPath);
        if (!info.Exists)
        {
            return Empty("PDF content was unavailable.");
        }

        if (info.Length > maximumInputBytes || info.Length > int.MaxValue)
        {
            return Empty("PDF metadata was skipped because the file exceeds the configured content bound.");
        }

        var bytes = new byte[(int)info.Length];
        await using (var stream = new FileStream(
            file.FullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
        }

        var source = Encoding.Latin1.GetString(bytes);
        var pageCount = PageRegex.Matches(source).Count;
        var warnings = new List<string>();
        if (pageCount > maximumPages)
        {
            warnings.Add("PDF page count exceeds the configured extraction bound.");
        }

        var fields = new List<ExtractedMetadataField>();
        AddDictionaryValue(fields, source, "Title", "Document title");
        AddDictionaryValue(fields, source, "Author", "Author");
        AddDictionaryValue(fields, source, "Subject", "Subject");
        AddDictionaryValue(fields, source, "Keywords", "Keywords");
        AddDictionaryValue(fields, source, "Creator", "Application name");
        fields.Add(new ExtractedMetadataField(
            "Page count",
            pageCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ContentProvenance.EmbeddedMetadata));
        var nativeText = pageCount > maximumPages
            ? null
            : string.Join(' ', TextRegex.Matches(source)
                .Take(2048)
                .Select(match => UnescapePdfText(match.Groups["value"].Value)));
        return new MetadataExtractionResult(
            Array.AsReadOnly(fields.ToArray()),
            ContentText.Normalize(nativeText),
            ContentText.Normalize(nativeText).Length >= ContentText.ReliableTextMinimumLength,
            pageCount,
            Array.AsReadOnly(warnings.ToArray()));
    }

    private static void AddDictionaryValue(
        List<ExtractedMetadataField> fields,
        string source,
        string key,
        string displayName)
    {
        var match = Regex.Match(
            source,
            $@"/{Regex.Escape(key)}\s*\((?<value>(?:\\.|[^\\)])*)\)",
            RegexOptions.CultureInvariant);
        if (match.Success)
        {
            fields.Add(new ExtractedMetadataField(
                displayName,
                UnescapePdfText(match.Groups["value"].Value),
                ContentProvenance.EmbeddedMetadata));
        }
    }

    private static string UnescapePdfText(string value) => value
        .Replace("\\(", "(", StringComparison.Ordinal)
        .Replace("\\)", ")", StringComparison.Ordinal)
        .Replace("\\n", " ", StringComparison.Ordinal)
        .Replace("\\r", " ", StringComparison.Ordinal)
        .Replace("\\\\", "\\", StringComparison.Ordinal);

    private static MetadataExtractionResult Empty(string warning) =>
        new([], null, false, null, [warning]);
}
