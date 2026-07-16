namespace TidyMind.Scanner.Models;
/// <summary>Contains ordered classification rules.</summary>
public sealed record FileClassificationOptions(IReadOnlyList<FileClassificationRule> Rules)
{
    /// <summary>Gets approved built-in extension rules.</summary>
    public static FileClassificationOptions Default { get; } = new(CreateDefaultRules());

    private static IReadOnlyList<FileClassificationRule> CreateDefaultRules()
    {
        var mappings = new (FileCategory Category, string Extensions)[]
        {
            (FileCategory.Document, ".txt .md .rtf .pdf .doc .docx .odt"), (FileCategory.Spreadsheet, ".csv .xls .xlsx .ods"),
            (FileCategory.Presentation, ".ppt .pptx .odp"), (FileCategory.Image, ".jpg .jpeg .png .gif .bmp .tif .tiff .webp .svg .heic"),
            (FileCategory.Audio, ".mp3 .wav .flac .aac .m4a .ogg .wma"), (FileCategory.Video, ".mp4 .mkv .mov .avi .wmv .webm .m4v"),
            (FileCategory.Archive, ".zip .7z .rar .tar .gz .bz2 .xz"), (FileCategory.Code, ".cs .fs .vb .java .py .js .ts .tsx .jsx .c .cpp .h .hpp .rs .go .php .rb .swift .kt .kts .html .css .scss .xml .json .yaml .yml .toml .sql .sh .ps1"),
            (FileCategory.Data, ".db .sqlite .sqlite3 .parquet .avro"), (FileCategory.Executable, ".exe .msi .dll .bat .cmd .com .appx .msix"), (FileCategory.Font, ".ttf .otf .woff .woff2"),
        };
        return mappings.SelectMany(mapping => mapping.Extensions.Split(' ').Select(extension => new FileClassificationRule($"{mapping.Category}-{extension}", FileClassificationMatchKind.Extension, extension, mapping.Category))).ToArray();
    }
}
