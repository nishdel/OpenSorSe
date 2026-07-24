using System.Text.Json;

namespace OpenSorSe.Application.AI;

/// <summary>Keeps Ollama JSON Schemas beside the matching prompt and validation contracts.</summary>
public static class AiStructuredOutputContracts
{
    /// <summary>Gets the exact provider system instruction shared by bounded operations.</summary>
    public const string SystemPrompt =
        "You are OpenSorSe's constrained local suggestion engine. Use only supplied context. Return exactly one JSON object matching the supplied schema. Never return Markdown, prose, commands, paths, or filesystem actions.";

    /// <summary>Gets the JSON Schema sent to Ollama for the requested generation capability.</summary>
    public static JsonElement GetSchema(AiSuggestionKind kind)
    {
        var schema = kind switch
        {
            AiSuggestionKind.FileRename => RenameSchema,
            AiSuggestionKind.FolderStructure => FolderSchema,
            AiSuggestionKind.DocumentTextInterpretation => DocumentSchema,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        using var document = JsonDocument.Parse(schema);
        return document.RootElement.Clone();
    }

    private const string RenameSchema = """
    {"type":"object","additionalProperties":true,"required":["taskId","status","reason"],"properties":{"taskId":{"const":"file-rename-v1"},"status":{"enum":["suggestion","no_suggestion"]},"sourceFileId":{"type":"string"},"suggestedFileName":{"type":"string"},"reason":{"type":"string","minLength":1,"maxLength":240},"confidence":{"type":["number","null"],"minimum":0,"maximum":1}}}
    """;

    private const string FolderSchema = """
    {"type":"object","additionalProperties":true,"required":["taskId","status","reason"],"properties":{"taskId":{"const":"folder-structure-v1"},"status":{"enum":["suggestion","no_suggestion"]},"folders":{"type":"array","maxItems":25,"items":{"type":"object","required":["folderId","name","reason"],"properties":{"folderId":{"type":"string"},"name":{"type":"string"},"parentFolderId":{"type":["string","null"]},"reason":{"type":"string","minLength":1,"maxLength":240},"confidence":{"type":["number","null"],"minimum":0,"maximum":1}}},"assignments":{"type":"array","maxItems":25,"items":{"type":"object","required":["sourceFileId","folderId"],"properties":{"sourceFileId":{"type":"string"},"folderId":{"type":"string"}}}},"reason":{"type":"string","minLength":1,"maxLength":240}}}
    """;

    private const string DocumentSchema = """
    {"type":"object","additionalProperties":true,"required":["taskId","status","reason"],"properties":{"taskId":{"const":"document-text-interpretation-v1"},"status":{"enum":["suggestion","no_suggestion"]},"sourceFileId":{"type":"string"},"documentType":{"type":["string","null"]},"title":{"type":["string","null"]},"tags":{"type":"array","maxItems":12,"items":{"type":"string"}},"dates":{"type":"array","items":{"type":"string"}},"issuer":{"type":["string","null"]},"suggestedFolder":{"type":["string","null"]},"reason":{"type":"string","minLength":1,"maxLength":240},"confidence":{"type":["number","null"],"minimum":0,"maximum":1}}}
    """;
}
