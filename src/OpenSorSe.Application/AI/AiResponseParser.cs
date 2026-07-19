using System.Text;
using System.Text.Json;
using OpenSorSe.Application.Models;

namespace OpenSorSe.Application.AI;

/// <summary>Defines fixed bounds for structured AI response validation.</summary>
public static class AiResponseLimits
{
    /// <summary>Maximum UTF-8 bytes accepted by the application parser.</summary>
    public const int MaximumStructuredResponseBytes = 256 * 1024;

    /// <summary>Maximum folders in one logical hierarchy.</summary>
    public const int MaximumFolders = 25;

    /// <summary>Maximum assignments in one logical hierarchy.</summary>
    public const int MaximumAssignments = AiPromptLimits.MaximumFolderStructureFiles;

    /// <summary>Maximum provider reason length.</summary>
    public const int MaximumReasonLength = 240;
}

/// <summary>Contains validated rename response values before provider attribution.</summary>
public sealed record AiParsedFileRename(
    string SourceFileId,
    string SuggestedFileName,
    string Reason,
    double? Confidence);

/// <summary>Contains a validated logical hierarchy before provider attribution.</summary>
public sealed record AiParsedFolderStructure(
    IReadOnlyList<AiSuggestedFolder> Folders,
    IReadOnlyList<AiFolderStructurePlanItem> Items,
    string Reason);

/// <summary>Contains either one fully valid response, a valid no-suggestion response, or one safe error.</summary>
public sealed record AiResponseParseResult<T>(T? Value, bool IsNoSuggestion, string Message)
    where T : class
{
    /// <summary>Gets whether the complete structured response passed validation.</summary>
    public bool IsValid => Value is not null || IsNoSuggestion;
}

/// <summary>Parses and validates capability-specific untrusted JSON.</summary>
public interface IAiResponseParser
{
    /// <summary>Parses one file-rename response against the exact known file context.</summary>
    AiResponseParseResult<AiParsedFileRename> ParseFileRename(string response, AiFileRenameRequest request);

    /// <summary>Parses one folder-structure response against only the file records included in the prompt.</summary>
    AiResponseParseResult<AiParsedFolderStructure> ParseFolderStructure(string response, IReadOnlyList<ResultFile> includedFiles);
}

/// <summary>
/// Implements strict required-field validation while ignoring unknown JSON properties for forward compatibility.
/// </summary>
public sealed class AiResponseParser : IAiResponseParser
{
    private const string SuggestionStatus = "suggestion";
    private const string NoSuggestionStatus = "no_suggestion";

    /// <inheritdoc />
    public AiResponseParseResult<AiParsedFileRename> ParseFileRename(string response, AiFileRenameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.File);
        ArgumentNullException.ThrowIfNull(request.SiblingFileNames);

        if (!TryOpen(response, out var document, out var error))
        {
            return Failure<AiParsedFileRename>(error);
        }

        using (document)
        {
            var root = document.RootElement;
            if (!TryReadCommon(root, AiPromptBuilder.FileRenameTaskId, out var status, out var reason, out error))
            {
                return Failure<AiParsedFileRename>(error);
            }

            if (status == NoSuggestionStatus)
            {
                if (root.TryGetProperty("sourceFileId", out _) || root.TryGetProperty("suggestedFileName", out _) || root.TryGetProperty("confidence", out _))
                {
                    return Failure<AiParsedFileRename>("The AI no-suggestion response contained actionable rename values. No suggestion was used.");
                }

                return new AiResponseParseResult<AiParsedFileRename>(null, true, reason);
            }

            if (!TryReadRequiredString(root, "sourceFileId", 128, out var sourceFileId, out error) ||
                !string.Equals(sourceFileId, request.File.Id, StringComparison.Ordinal))
            {
                return Failure<AiParsedFileRename>("The AI rename response referenced an unknown source file. No suggestion was used.");
            }

            if (!TryReadRequiredString(root, "suggestedFileName", 255, out var suggestedFileName, out error) ||
                !AiSuggestionValidator.TryNormalizeFileName(
                    suggestedFileName,
                    request.File.NormalizedExtension,
                    request.SiblingFileNames,
                    out var normalizedFileName,
                    out error))
            {
                return Failure<AiParsedFileRename>(error);
            }

            if (string.Equals(normalizedFileName, request.File.DisplayFileName, StringComparison.OrdinalIgnoreCase))
            {
                return Failure<AiParsedFileRename>("The AI rename response did not propose a filename change. No suggestion was used.");
            }

            if (!TryReadConfidence(root, out var confidence, out error))
            {
                return Failure<AiParsedFileRename>(error);
            }

            return new AiResponseParseResult<AiParsedFileRename>(
                new AiParsedFileRename(sourceFileId, normalizedFileName, reason, confidence),
                false,
                "A validated AI-generated rename suggestion is available for review.");
        }
    }

    /// <inheritdoc />
    public AiResponseParseResult<AiParsedFolderStructure> ParseFolderStructure(string response, IReadOnlyList<ResultFile> includedFiles)
    {
        ArgumentNullException.ThrowIfNull(includedFiles);
        if (includedFiles.Count == 0 || includedFiles.Any(file => file is null || string.IsNullOrWhiteSpace(file.Id)) ||
            includedFiles.Select(file => file.Id).Distinct(StringComparer.Ordinal).Count() != includedFiles.Count)
        {
            return Failure<AiParsedFolderStructure>("The known folder-structure context is invalid. No suggestion was used.");
        }

        if (!TryOpen(response, out var document, out var error))
        {
            return Failure<AiParsedFolderStructure>(error);
        }

        using (document)
        {
            var root = document.RootElement;
            if (!TryReadCommon(root, AiPromptBuilder.FolderStructureTaskId, out var status, out var reason, out error))
            {
                return Failure<AiParsedFolderStructure>(error);
            }

            if (status == NoSuggestionStatus)
            {
                if (root.TryGetProperty("folders", out _) || root.TryGetProperty("assignments", out _))
                {
                    return Failure<AiParsedFolderStructure>("The AI no-suggestion response contained actionable folder values. No suggestion was used.");
                }

                return new AiResponseParseResult<AiParsedFolderStructure>(null, true, reason);
            }

            if (!root.TryGetProperty("folders", out var foldersElement) || foldersElement.ValueKind != JsonValueKind.Array ||
                foldersElement.GetArrayLength() is 0 or > AiResponseLimits.MaximumFolders)
            {
                return Failure<AiParsedFolderStructure>("The AI folder response contains an invalid folder list. No suggestion was used.");
            }

            var folderInputs = new Dictionary<string, FolderInput>(StringComparer.Ordinal);
            foreach (var folderElement in foldersElement.EnumerateArray())
            {
                if (folderElement.ValueKind != JsonValueKind.Object ||
                    !TryReadRequiredString(folderElement, "folderId", 64, out var folderId, out error) ||
                    !TryReadRequiredString(folderElement, "name", 100, out var name, out error) ||
                    !AiSuggestionValidator.TryNormalizeFolderName(name, out var normalizedName, out error) ||
                    !TryReadNullableRequiredString(folderElement, "parentFolderId", 64, out var parentFolderId, out error) ||
                    !TryReadRequiredString(folderElement, "reason", AiResponseLimits.MaximumReasonLength, out var folderReason, out error) ||
                    !TryReadConfidence(folderElement, out var confidence, out error))
                {
                    return Failure<AiParsedFolderStructure>(error);
                }

                if (!folderInputs.TryAdd(folderId, new FolderInput(folderId, normalizedName, parentFolderId, folderReason, confidence)))
                {
                    return Failure<AiParsedFolderStructure>("The AI folder response contains duplicate folder identities. No suggestion was used.");
                }
            }

            foreach (var folder in folderInputs.Values)
            {
                if (folder.ParentFolderId is not null &&
                    (!folderInputs.ContainsKey(folder.ParentFolderId) || string.Equals(folder.FolderId, folder.ParentFolderId, StringComparison.Ordinal)))
                {
                    return Failure<AiParsedFolderStructure>("The AI folder response contains an unknown or circular parent folder. No suggestion was used.");
                }
            }

            var paths = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var folderId in folderInputs.Keys.Order(StringComparer.Ordinal))
            {
                if (!TryBuildLogicalPath(folderId, folderInputs, paths, new HashSet<string>(StringComparer.Ordinal), out _, out error))
                {
                    return Failure<AiParsedFolderStructure>(error);
                }
            }

            if (paths.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != paths.Count)
            {
                return Failure<AiParsedFolderStructure>("The AI folder response contains duplicate logical folder paths. No suggestion was used.");
            }

            if (!root.TryGetProperty("assignments", out var assignmentsElement) || assignmentsElement.ValueKind != JsonValueKind.Array ||
                assignmentsElement.GetArrayLength() is 0 or > AiResponseLimits.MaximumAssignments ||
                assignmentsElement.GetArrayLength() > includedFiles.Count)
            {
                return Failure<AiParsedFolderStructure>("The AI folder response contains an invalid assignment list. No suggestion was used.");
            }

            var knownFiles = includedFiles.ToDictionary(file => file.Id, StringComparer.Ordinal);
            var assignedFiles = new HashSet<string>(StringComparer.Ordinal);
            var items = new List<AiFolderStructurePlanItem>();
            foreach (var assignmentElement in assignmentsElement.EnumerateArray())
            {
                if (assignmentElement.ValueKind != JsonValueKind.Object ||
                    !TryReadRequiredString(assignmentElement, "sourceFileId", 128, out var sourceFileId, out error) ||
                    !TryReadRequiredString(assignmentElement, "folderId", 64, out var folderId, out error) ||
                    !knownFiles.TryGetValue(sourceFileId, out var file) ||
                    !paths.TryGetValue(folderId, out var logicalPath))
                {
                    return Failure<AiParsedFolderStructure>("The AI folder response referenced an unknown source file or folder. No suggestion was used.");
                }

                if (!assignedFiles.Add(sourceFileId))
                {
                    return Failure<AiParsedFolderStructure>("The AI folder response assigned the same source file more than once. No suggestion was used.");
                }

                items.Add(new AiFolderStructurePlanItem(file.Id, file.DisplayFileName, logicalPath));
            }

            var folders = folderInputs.Values
                .OrderBy(folder => paths[folder.FolderId], StringComparer.Ordinal)
                .Select(folder => new AiSuggestedFolder(
                    folder.FolderId,
                    folder.Name,
                    folder.ParentFolderId,
                    paths[folder.FolderId],
                    folder.Reason,
                    folder.Confidence))
                .ToArray();
            var orderedItems = items
                .OrderBy(item => item.FileId, StringComparer.Ordinal)
                .ThenBy(item => item.DestinationFolder, StringComparer.Ordinal)
                .ToArray();

            return new AiResponseParseResult<AiParsedFolderStructure>(
                new AiParsedFolderStructure(Array.AsReadOnly(folders), Array.AsReadOnly(orderedItems), reason),
                false,
                "A validated AI-generated folder-structure suggestion is available for review.");
        }
    }

    private static bool TryOpen(string response, out JsonDocument document, out string error)
    {
        document = default!;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(response))
        {
            error = "The AI returned an empty structured response. No suggestion was used.";
            return false;
        }

        var trimmed = response.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            error = "The AI wrapped its response in Markdown instead of returning the required JSON. No suggestion was used.";
            return false;
        }

        if (Encoding.UTF8.GetByteCount(trimmed) > AiResponseLimits.MaximumStructuredResponseBytes)
        {
            error = "The AI returned an excessively large structured response. No suggestion was used.";
            return false;
        }

        try
        {
            document = JsonDocument.Parse(trimmed, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32,
            });
        }
        catch (JsonException)
        {
            error = "The AI returned malformed JSON. No suggestion was used.";
            return false;
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            document = default!;
            error = "The AI response did not have the required JSON object shape. No suggestion was used.";
            return false;
        }

        return true;
    }

    private static bool TryReadCommon(
        JsonElement root,
        string expectedTaskId,
        out string status,
        out string reason,
        out string error)
    {
        status = string.Empty;
        reason = string.Empty;
        if (!TryReadRequiredString(root, "taskId", 64, out var taskId, out error) ||
            !string.Equals(taskId, expectedTaskId, StringComparison.Ordinal))
        {
            error = "The AI response used an unexpected task identifier. No suggestion was used.";
            return false;
        }

        if (!TryReadRequiredString(root, "status", 32, out status, out error) ||
            status is not (SuggestionStatus or NoSuggestionStatus))
        {
            error = "The AI response used an unsupported status. No suggestion was used.";
            return false;
        }

        return TryReadRequiredString(root, "reason", AiResponseLimits.MaximumReasonLength, out reason, out error);
    }

    private static bool TryReadRequiredString(
        JsonElement element,
        string propertyName,
        int maximumLength,
        out string value,
        out string error)
    {
        value = string.Empty;
        error = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            error = $"The AI response is missing the required '{propertyName}' string. No suggestion was used.";
            return false;
        }

        var candidate = property.GetString();
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > maximumLength || candidate.Any(char.IsControl))
        {
            error = $"The AI response contains an invalid '{propertyName}' value. No suggestion was used.";
            return false;
        }

        value = candidate.Trim();
        return true;
    }

    private static bool TryReadNullableRequiredString(
        JsonElement element,
        string propertyName,
        int maximumLength,
        out string? value,
        out string error)
    {
        value = null;
        error = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            error = $"The AI response is missing the required '{propertyName}' property. No suggestion was used.";
            return false;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            error = $"The AI response contains an invalid '{propertyName}' value. No suggestion was used.";
            return false;
        }

        var candidate = property.GetString();
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > maximumLength || candidate.Any(char.IsControl))
        {
            error = $"The AI response contains an invalid '{propertyName}' value. No suggestion was used.";
            return false;
        }

        value = candidate.Trim();
        return true;
    }

    private static bool TryReadConfidence(JsonElement element, out double? confidence, out string error)
    {
        confidence = null;
        error = string.Empty;
        if (!element.TryGetProperty("confidence", out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out var value) ||
            double.IsNaN(value) || double.IsInfinity(value) || value is < 0 or > 1)
        {
            error = "The AI response contains an invalid confidence value. No suggestion was used.";
            return false;
        }

        confidence = value;
        return true;
    }

    private static bool TryBuildLogicalPath(
        string folderId,
        IReadOnlyDictionary<string, FolderInput> folders,
        IDictionary<string, string> completedPaths,
        ISet<string> visiting,
        out string logicalPath,
        out string error)
    {
        if (completedPaths.TryGetValue(folderId, out logicalPath!))
        {
            error = string.Empty;
            return true;
        }

        if (!visiting.Add(folderId))
        {
            logicalPath = string.Empty;
            error = "The AI folder response contains a circular folder hierarchy. No suggestion was used.";
            return false;
        }

        var folder = folders[folderId];
        if (folder.ParentFolderId is null)
        {
            logicalPath = folder.Name;
        }
        else if (!TryBuildLogicalPath(folder.ParentFolderId, folders, completedPaths, visiting, out var parentPath, out error))
        {
            logicalPath = string.Empty;
            return false;
        }
        else
        {
            logicalPath = $"{parentPath}/{folder.Name}";
        }

        if (logicalPath.Length > 512)
        {
            visiting.Remove(folderId);
            logicalPath = string.Empty;
            error = "The AI folder response contains an excessively deep or long logical path. No suggestion was used.";
            return false;
        }

        visiting.Remove(folderId);
        completedPaths[folderId] = logicalPath;
        error = string.Empty;
        return true;
    }

    private static AiResponseParseResult<T> Failure<T>(string error)
        where T : class => new(null, false, string.IsNullOrWhiteSpace(error)
            ? "The AI response was invalid. No suggestion was used."
            : error);

    private sealed record FolderInput(
        string FolderId,
        string Name,
        string? ParentFolderId,
        string Reason,
        double? Confidence);
}
