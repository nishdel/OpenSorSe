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
}

/// <summary>
/// Contains one deterministic provider prompt and the exact known identities included in it.
/// </summary>
public sealed record AiPromptPackage(
    string TaskId,
    string Prompt,
    IReadOnlyList<string> IncludedSourceIds,
    bool WasInputBounded);

/// <summary>Builds capability-specific, bounded, metadata-only prompts.</summary>
public interface IAiPromptBuilder
{
    /// <summary>Builds the file-rename prompt.</summary>
    AiPromptPackage BuildFileRenamePrompt(AiFileRenameRequest request, AiPreferenceSummary preferences);

    /// <summary>Builds the folder-structure prompt.</summary>
    AiPromptPackage BuildFolderStructurePrompt(AiFolderStructureRequest request, AiPreferenceSummary preferences);
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
        var prompt = new
        {
            taskIdentifier = FileRenameTaskId,
            objective = "Suggest one clearer filename for the supplied known file using only supplied metadata.",
            inputData = new
            {
                sourceFile = new
                {
                    sourceFileId = Bound(request.File.Id, 128),
                    currentFileName = Bound(request.File.DisplayFileName, 255),
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
                "Return the supplied sourceFileId exactly.",
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
            siblings.WasBounded || rejectedValues.WasBounded);
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
        var prompt = new
        {
            taskIdentifier = FolderStructureTaskId,
            objective = "Suggest one logical preview-only folder hierarchy and assignments for supplied known file metadata.",
            inputData = new
            {
                files = orderedFiles.Select(file => new
                {
                    sourceFileId = Bound(file.Id, 128),
                    fileName = Bound(file.DisplayFileName, 255),
                    extension = Bound(file.NormalizedExtension, 32),
                    deterministicCategory = Bound(file.ClassificationDisplay, 128),
                }).ToArray(),
                existingLogicalFolderNames = existingFolders.Values,
                preferredLogicalFolderNames = preferredFolders.Values,
                rejectedValues = rejectedValues.Values,
            },
            allowedReasoningScope = new[]
            {
                "Use only supplied filenames, extensions, categories, logical folder names, and bounded preferences.",
                "Suggest logical parent-child relationships and assign only supplied sourceFileIds.",
            },
            mandatoryRules = new[]
            {
                "Use unique short folderId values and reference only those values from parentFolderId and assignments.",
                "Use one safe folder-name component per folder; never return a path as a folder name.",
                "Reference only supplied sourceFileIds and assign each source at most once.",
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
                        sourceFileId = "exact supplied opaque id",
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
            orderedFiles.Length < request.Files.Count || existingFolders.WasBounded || preferredFolders.WasBounded || rejectedValues.WasBounded);
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
}
