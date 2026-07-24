using System.Text.Json;

namespace OpenSorSe.Application.AI;

/// <summary>Defines deterministic limits for metadata-only AI prompts.</summary>
public static class AiPromptLimits
{
    /// <summary>Maximum file records included in a folder-structure prompt.</summary>
    public const int MaximumFolderStructureFiles = 25;

    /// <summary>Maximum sibling filenames included in a rename prompt.</summary>
    public const int MaximumSiblingFileNames = 20;

    /// <summary>Maximum existing folder names included in a structure prompt.</summary>
    public const int MaximumExistingFolderNames = 30;

    /// <summary>Maximum preference values included per supported preference category.</summary>
    public const int MaximumPreferenceValues = 10;

    /// <summary>Maximum PDF pages included in one explicit document-text request.</summary>
    public const int MaximumDocumentTextPages = 12;

    /// <summary>Maximum extracted characters included in one explicit document-text request.</summary>
    public const int MaximumDocumentTextCharacters = 16_384;
}

/// <summary>
/// Contains one deterministic provider prompt and the exact known identities included in it.
/// </summary>
public sealed record AiPromptPackage(
    string TaskId,
    string Prompt,
    IReadOnlyList<string> IncludedSourceIds,
    bool WasInputBounded)
{
    /// <summary>Gets request-local identities mapped to known application source identities.</summary>
    public IReadOnlyList<AiPromptSourceMapping> SourceMappings { get; init; } = Array.Empty<AiPromptSourceMapping>();

    /// <summary>Gets the total eligible source count before deterministic bounding.</summary>
    public int TotalInputCount { get; init; } = IncludedSourceIds.Count;

    /// <summary>Gets the source count serialized into the request.</summary>
    public int IncludedInputCount => SourceMappings.Count == 0 ? IncludedSourceIds.Count : SourceMappings.Count;

    /// <summary>Gets the count omitted by deterministic bounding.</summary>
    public int OmittedInputCount => Math.Max(0, TotalInputCount - IncludedInputCount);
}

/// <summary>Maps one short request-local identity back to a known result without exposing its path.</summary>
public sealed record AiPromptSourceMapping(string RequestSourceId, string KnownSourceId, string ExactFileName);

/// <summary>Builds capability-specific, bounded, metadata-only prompts.</summary>
public interface IAiPromptBuilder
{
    /// <summary>Builds the file-rename prompt.</summary>
    AiPromptPackage BuildFileRenamePrompt(AiFileRenameRequest request, AiPreferenceSummary preferences);

    /// <summary>Builds the folder-structure prompt.</summary>
    AiPromptPackage BuildFolderStructurePrompt(AiFolderStructureRequest request, AiPreferenceSummary preferences);

    /// <summary>Builds a bounded extracted-document-text interpretation prompt.</summary>
    AiPromptPackage BuildDocumentInterpretationPrompt(AiDocumentTextRequest request);
}

/// <summary>
/// Builds deterministic English v1 prompts without including file content or absolute paths.
/// </summary>
public sealed class AiPromptBuilder : IAiPromptBuilder
{
    /// <summary>Gets the versioned file-rename task identifier.</summary>
    public const string FileRenameTaskId = "file-rename-v1";

    /// <summary>Gets the versioned folder-structure task identifier.</summary>
    public const string FolderStructureTaskId = "folder-structure-v1";

    /// <summary>Gets the versioned extracted-text interpretation task identifier.</summary>
    public const string DocumentInterpretationTaskId = "document-text-interpretation-v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <inheritdoc />
    public AiPromptPackage BuildFileRenamePrompt(AiFileRenameRequest request, AiPreferenceSummary preferences)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.File);
        ArgumentNullException.ThrowIfNull(request.SiblingFileNames);
        ArgumentNullException.ThrowIfNull(preferences);

        var siblings = BoundStrings(request.SiblingFileNames, AiPromptLimits.MaximumSiblingFileNames);
        var rejectedValues = BoundStrings(preferences.RejectedValues, AiPromptLimits.MaximumPreferenceValues);
        const string requestSourceId = "item-001";
        var prompt = new
        {
            taskIdentifier = FileRenameTaskId,
            objective = "Suggest one clearer filename for the supplied known file using only supplied metadata.",
            inputData = new
            {
                sourceFile = new
                {
                    sourceFileId = requestSourceId,
                    currentFileName = request.File.DisplayFileName,
                    extension = Bound(request.File.NormalizedExtension, 32),
                    deterministicCategory = Bound(request.File.ClassificationDisplay, 128),
                },
                siblingFileNames = siblings.Values,
                rejectedNamingValues = rejectedValues.Values,
            },
            allowedReasoningScope = new[]
            {
                "Use filename, extension, deterministic category, nearby filenames, and bounded prior rejected naming values only.",
                "Prefer a concise descriptive name consistent with nearby filenames.",
            },
            mandatoryRules = new[]
            {
                "Copy the supplied request-local sourceFileId item-001 exactly without modification.",
                "Preserve the original extension exactly.",
                "Return one filename only, never a path.",
                "Use status no_suggestion when no safe improvement is justified.",
                "Keep reason at or below 240 characters and confidence between 0 and 1 when supplied.",
            },
            forbiddenBehaviors = new[]
            {
                "Do not invent a file, content, metadata, directory, or source identity.",
                "Do not return path separators, absolute paths, traversal, commands, Markdown, or filesystem actions.",
                "Do not propose deleting, moving, overwriting, or directly renaming anything.",
            },
            requiredResponseSchema = new
            {
                taskId = FileRenameTaskId,
                status = "suggestion | no_suggestion",
                sourceFileId = "required for suggestion; exact supplied opaque id",
                suggestedFileName = "required for suggestion; safe filename with original extension",
                reason = "required bounded string",
                confidence = "optional number from 0 through 1",
            },
            noSuggestionBehavior = "Return an object with taskId, status no_suggestion, and a bounded reason. Omit sourceFileId, suggestedFileName, and confidence.",
            outputInstruction = "Return only one JSON object matching the schema. Do not wrap it in Markdown fences and do not add prose.",
        };

        return new AiPromptPackage(
            FileRenameTaskId,
            JsonSerializer.Serialize(prompt, JsonOptions),
            Array.AsReadOnly([request.File.Id]),
            siblings.WasBounded || rejectedValues.WasBounded)
        {
            SourceMappings = Array.AsReadOnly([new AiPromptSourceMapping(requestSourceId, request.File.Id, request.File.DisplayFileName)]),
            TotalInputCount = 1,
        };
    }

    /// <inheritdoc />
    public AiPromptPackage BuildFolderStructurePrompt(AiFolderStructureRequest request, AiPreferenceSummary preferences)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Files);
        ArgumentNullException.ThrowIfNull(request.ExistingFolderNames);
        ArgumentNullException.ThrowIfNull(preferences);

        var orderedFiles = request.Files
            .OrderBy(file => file.Id, StringComparer.Ordinal)
            .ThenBy(file => file.DisplayFileName, StringComparer.Ordinal)
            .Take(AiPromptLimits.MaximumFolderStructureFiles)
            .ToArray();
        var existingFolders = BoundStrings(request.ExistingFolderNames, AiPromptLimits.MaximumExistingFolderNames);
        var preferredFolders = BoundStrings(preferences.PreferredFolders, AiPromptLimits.MaximumPreferenceValues);
        var rejectedValues = BoundStrings(preferences.RejectedValues, AiPromptLimits.MaximumPreferenceValues);
        var sourceMappings = orderedFiles
            .Select((file, index) => new AiPromptSourceMapping($"item-{index + 1:D3}", file.Id, file.DisplayFileName))
            .ToArray();
        var prompt = new
        {
            taskIdentifier = FolderStructureTaskId,
            objective = "Suggest one logical preview-only folder hierarchy and assignments for supplied known file metadata.",
            inputData = new
            {
                totalAvailableItemCount = request.Files.Count,
                includedItemCount = orderedFiles.Length,
                omittedItemCount = Math.Max(0, request.Files.Count - orderedFiles.Length),
                files = orderedFiles.Select((file, index) => new
                {
                    sourceFileId = sourceMappings[index].RequestSourceId,
                    fileName = file.DisplayFileName,
                    extension = Bound(file.NormalizedExtension, 32),
                    deterministicCategory = Bound(file.ClassificationDisplay, 128),
                    sizeInBytes = file.SizeInBytes,
                }).ToArray(),
                existingLogicalFolderNames = existingFolders.Values,
                preferredLogicalFolderNames = preferredFolders.Values,
                rejectedValues = rejectedValues.Values,
            },
            allowedReasoningScope = new[]
            {
                "Use only supplied filenames, extensions, categories, logical folder names, and bounded preferences.",
                "Given only the supplied exact filenames and bounded metadata, propose a logical folder hierarchy and assign every supplied request-local sourceFileId to one proposed folder.",
            },
            mandatoryRules = new[]
            {
                "Use unique short folderId values and reference only those values from parentFolderId and assignments.",
                "Use one safe folder-name component per folder; never return a path as a folder name.",
                "Copy request-local sourceFileIds exactly, reference only supplied IDs, and assign every supplied item exactly once.",
                "Keep the folder count bounded by the supplied response schema and use concise portable folder names.",
                "Use status no_suggestion when no safe useful hierarchy is justified.",
                "Keep every reason at or below 240 characters and confidence between 0 and 1 when supplied.",
            },
            forbiddenBehaviors = new[]
            {
                "Do not invent files, source identities, contents, metadata, or existing folders.",
                "Do not return absolute paths, system directories, traversal, commands, Markdown, or executable steps.",
                "Do not claim folders exist and do not propose directly creating or moving anything.",
            },
            requiredResponseSchema = new
            {
                taskId = FolderStructureTaskId,
                status = "suggestion | no_suggestion",
                folders = new[]
                {
                    new
                    {
                        folderId = "unique bounded id",
                        name = "one safe folder-name component",
                        parentFolderId = "another returned folderId or null",
                        reason = "bounded string",
                        confidence = "optional number from 0 through 1",
                    },
                },
                assignments = new[]
                {
                    new
                    {
                        sourceFileId = "exact supplied request-local item-NNN id",
                        folderId = "returned folderId",
                    },
                },
                reason = "required bounded plan explanation",
            },
            noSuggestionBehavior = "Return an object with taskId, status no_suggestion, and a bounded reason. Omit folders and assignments.",
            outputInstruction = "Return only one JSON object matching the schema. Do not wrap it in Markdown fences and do not add prose.",
        };

        return new AiPromptPackage(
            FolderStructureTaskId,
            JsonSerializer.Serialize(prompt, JsonOptions),
            Array.AsReadOnly(orderedFiles.Select(file => file.Id).ToArray()),
            orderedFiles.Length < request.Files.Count || existingFolders.WasBounded || preferredFolders.WasBounded || rejectedValues.WasBounded)
        {
            SourceMappings = Array.AsReadOnly(sourceMappings),
            TotalInputCount = request.Files.Count,
        };
    }

    /// <inheritdoc />
    public AiPromptPackage BuildDocumentInterpretationPrompt(AiDocumentTextRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Pages);
        var pageInputs = request.Pages
            .Where(page => page is not null && !string.IsNullOrWhiteSpace(page.Text))
            .OrderBy(page => page.PageNumber)
            .Take(AiPromptLimits.MaximumDocumentTextPages)
            .Select(page => new DocumentTextInput(
                page.PageNumber,
                page.TextSource.ToString(),
                page.Text!))
            .ToArray();
        var fallbackInputs = pageInputs.Length == 0
            ? new[]
            {
                new DocumentTextInput(0, "NativeText", request.NativeText ?? string.Empty),
                new DocumentTextInput(0, "Ocr", request.OcrText ?? string.Empty),
            }.Where(item => !string.IsNullOrWhiteSpace(item.Text)).ToArray()
            : [];
        var inputs = pageInputs.Concat(fallbackInputs).ToArray();
        var remaining = AiPromptLimits.MaximumDocumentTextCharacters;
        var boundedInputs = new List<object>();
        var wasBounded = request.Pages.Count > AiPromptLimits.MaximumDocumentTextPages;
        foreach (var input in inputs)
        {
            if (remaining <= 0)
            {
                wasBounded = true;
                break;
            }

            var boundedText = Bound(input.Text, remaining);
            wasBounded |= boundedText.Length < input.Text.Length;
            boundedInputs.Add(new
            {
                pageNumber = input.PageNumber,
                provenance = input.Provenance,
                text = boundedText,
            });
            remaining -= boundedText.Length;
        }

        var prompt = new
        {
            taskIdentifier = DocumentInterpretationTaskId,
            objective = "Suggest bounded descriptive metadata from explicitly supplied extracted document text.",
            inputData = new
            {
                sourceFileId = "item-001",
                fileName = Bound(request.DisplayFileName, 255),
                extractedTextPages = boundedInputs,
                wasInputBounded = wasBounded,
            },
            allowedReasoningScope = new[]
            {
                "Use only the supplied normalized native/OCR text and filename.",
                "Return descriptive review suggestions, not authoritative transcription or advice.",
            },
            mandatoryRules = new[]
            {
                "Copy sourceFileId item-001 exactly.",
                "Use null or empty arrays when a value is not supported by supplied text.",
                "Use ISO yyyy-MM-dd dates only when explicit in the text.",
                "Keep tags at 12 or fewer, reason at 240 characters or fewer, and confidence between 0 and 1.",
            },
            forbiddenBehaviors = new[]
            {
                "Do not invent facts, identifiers, dates, issuers, content, or source identities.",
                "Do not provide legal, financial, medical, or identity conclusions.",
                "Do not return commands, paths, Markdown, or filesystem actions.",
                "Do not claim OCR text is exact or verified.",
            },
            requiredResponseSchema = new
            {
                taskId = DocumentInterpretationTaskId,
                status = "suggestion | no_suggestion",
                sourceFileId = "item-001 for suggestion",
                documentType = "string or null",
                title = "string or null",
                tags = new[] { "bounded tag" },
                dates = new[] { "yyyy-MM-dd" },
                issuer = "string or null",
                suggestedFolder = "one safe folder-name component or null",
                reason = "required bounded string",
                confidence = "optional number from 0 through 1",
            },
            noSuggestionBehavior = "Return taskId, status no_suggestion, and a bounded reason; omit actionable values.",
            outputInstruction = "Return only one JSON object. Do not wrap it in Markdown fences or add prose.",
        };
        return new AiPromptPackage(
            DocumentInterpretationTaskId,
            JsonSerializer.Serialize(prompt, JsonOptions),
            Array.AsReadOnly([request.SourceFileId]),
            wasBounded)
        {
            SourceMappings = Array.AsReadOnly([
                new AiPromptSourceMapping("item-001", request.SourceFileId, request.DisplayFileName),
            ]),
            TotalInputCount = 1,
        };
    }

    private static (IReadOnlyList<string> Values, bool WasBounded) BoundStrings(IReadOnlyList<string> values, int limit)
    {
        var candidates = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();
        var all = candidates
            .Select(value => Bound(value, 255))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return (Array.AsReadOnly(all.Take(limit).ToArray()), all.Length > limit || candidates.Any(value => value.Length > 255));
    }

    private static string Bound(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private sealed record DocumentTextInput(int PageNumber, string Provenance, string Text);
}
