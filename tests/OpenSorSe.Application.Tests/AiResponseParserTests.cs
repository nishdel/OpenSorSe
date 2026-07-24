using System.Text.Json;
using OpenSorSe.Application.AI;
using OpenSorSe.Application.Content;
using OpenSorSe.Application.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.Tests;

/// <summary>Verifies strict whole-response validation of untrusted AI JSON.</summary>
public sealed class AiResponseParserTests
{
    private readonly AiResponseParser _parser = new();

    /// <summary>Verifies a complete rename response is accepted and unknown properties are ignored.</summary>
    [Fact]
    public void ParseFileRename_ValidResponseWithUnknownProperty_IsAccepted()
    {
        var result = _parser.ParseFileRename(
            RenameJson("file:1", "April Invoice.pdf", confidence: "0.51", extra: ",\"futureField\":{\"value\":1}"),
            RenameRequest());

        var value = Assert.IsType<AiParsedFileRename>(result.Value);
        Assert.True(result.IsValid);
        Assert.False(result.IsNoSuggestion);
        Assert.Equal("April Invoice.pdf", value.SuggestedFileName);
        Assert.Equal(0.51, value.Confidence);
    }

    /// <summary>Verifies explicit no-suggestion output is a valid non-actionable result.</summary>
    [Fact]
    public void ParseFileRename_NoSuggestion_IsAcceptedWithoutValue()
    {
        var result = _parser.ParseFileRename(
            """{"taskId":"file-rename-v1","status":"no_suggestion","reason":"The current name is already clear."}""",
            RenameRequest());

        Assert.True(result.IsValid);
        Assert.True(result.IsNoSuggestion);
        Assert.Null(result.Value);
    }

    /// <summary>Verifies malformed, fenced, empty, wrong-shape, and missing-field responses are rejected.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("{invalid")]
    [InlineData("```json\n{}\n```")]
    [InlineData("[]")]
    [InlineData("{\"taskId\":\"file-rename-v1\",\"status\":\"suggestion\",\"reason\":\"why\"}")]
    [InlineData("{\"taskId\":12,\"status\":\"suggestion\",\"reason\":\"why\"}")]
    public void ParseFileRename_InvalidEnvelope_IsRejected(string json)
    {
        var result = _parser.ParseFileRename(json, RenameRequest());

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
    }

    /// <summary>Verifies source identity, filename, extension, collision, no-change, and confidence constraints.</summary>
    [Theory]
    [InlineData("other", "safe.pdf", "0.5")]
    [InlineData("file:1", "../safe.pdf", "0.5")]
    [InlineData("file:1", "C:\\safe.pdf", "0.5")]
    [InlineData("file:1", "bad:name.pdf", "0.5")]
    [InlineData("file:1", "NUL.pdf", "0.5")]
    [InlineData("file:1", "safe.txt", "0.5")]
    [InlineData("file:1", "invoice.pdf", "0.5")]
    [InlineData("file:1", "existing.pdf", "0.5")]
    [InlineData("file:1", "safe.pdf", "-0.1")]
    [InlineData("file:1", "safe.pdf", "1.1")]
    [InlineData("file:1", "safe.pdf", "\"high\"")]
    public void ParseFileRename_UnsafeOrInconsistentValue_IsRejected(string sourceId, string name, string confidence)
    {
        var result = _parser.ParseFileRename(
            RenameJson(sourceId, name, confidence),
            RenameRequest(["existing.pdf"]));

        Assert.False(result.IsValid);
    }

    /// <summary>Verifies a parent-child folder graph and known assignments produce deterministic logical paths.</summary>
    [Fact]
    public void ParseFolderStructure_ValidHierarchy_IsAccepted()
    {
        var result = _parser.ParseFolderStructure(ValidFolderJson(), [CreateFile("file:1", "invoice.pdf")]);

        var value = Assert.IsType<AiParsedFolderStructure>(result.Value);
        Assert.Equal(["Finance", "Finance/Invoices"], value.Folders.Select(folder => folder.LogicalPath));
        Assert.Equal("Finance/Invoices", Assert.Single(value.Items).DestinationFolder);
    }

    /// <summary>Verifies request-local identities map back only after every included item is assigned exactly once.</summary>
    [Fact]
    public void ParseFolderStructure_RequestLocalMappings_RequireCompleteExactOnceAssignment()
    {
        var files = new[]
        {
            CreateFile("known:a", "a.pdf"),
            CreateFile("known:b", "b.pdf"),
        };
        var mappings = new[]
        {
            new AiPromptSourceMapping("item-001", "known:a", "a.pdf"),
            new AiPromptSourceMapping("item-002", "known:b", "b.pdf"),
        };
        const string complete = """{"taskId":"folder-structure-v1","status":"suggestion","folders":[{"folderId":"f1","name":"Documents","parentFolderId":null,"reason":"Type","confidence":0.8}],"assignments":[{"sourceFileId":"item-001","folderId":"f1"},{"sourceFileId":"item-002","folderId":"f1"}],"reason":"Group documents."}""";
        const string missing = """{"taskId":"folder-structure-v1","status":"suggestion","folders":[{"folderId":"f1","name":"Documents","parentFolderId":null,"reason":"Type","confidence":0.8}],"assignments":[{"sourceFileId":"item-001","folderId":"f1"}],"reason":"Group documents."}""";

        var accepted = _parser.ParseFolderStructure(complete, files, mappings);
        var rejected = _parser.ParseFolderStructure(missing, files, mappings);

        Assert.True(accepted.IsValid);
        Assert.Equal(["known:a", "known:b"], accepted.Value!.Items.Select(item => item.FileId));
        Assert.False(rejected.IsValid);
        Assert.Null(rejected.Value);
    }

    /// <summary>Verifies a valid explicit folder no-suggestion response remains non-actionable.</summary>
    [Fact]
    public void ParseFolderStructure_NoSuggestion_IsAcceptedWithoutPlan()
    {
        var result = _parser.ParseFolderStructure(
            """{"taskId":"folder-structure-v1","status":"no_suggestion","reason":"Not enough metadata."}""",
            [CreateFile("file:1", "invoice.pdf")]);

        Assert.True(result.IsValid);
        Assert.True(result.IsNoSuggestion);
        Assert.Null(result.Value);
    }

    /// <summary>Verifies no-suggestion envelopes cannot smuggle actionable values past validation.</summary>
    [Theory]
    [InlineData("{\"taskId\":\"file-rename-v1\",\"status\":\"no_suggestion\",\"reason\":\"No change.\",\"suggestedFileName\":\"other.pdf\"}", true)]
    [InlineData("{\"taskId\":\"folder-structure-v1\",\"status\":\"no_suggestion\",\"reason\":\"No plan.\",\"folders\":[]}", false)]
    public void ParseNoSuggestion_WithActionableProperties_IsRejected(string json, bool rename)
    {
        var isValid = rename
            ? _parser.ParseFileRename(json, RenameRequest()).IsValid
            : _parser.ParseFolderStructure(json, [CreateFile("file:1", "invoice.pdf")]).IsValid;

        Assert.False(isValid);
    }

    /// <summary>Verifies invented sources, unsafe names, unknown parents, and duplicate identities reject the whole response.</summary>
    [Theory]
    [MemberData(nameof(InvalidFolderResponses))]
    public void ParseFolderStructure_InvalidGraphOrAssignment_IsRejected(string json)
    {
        var result = _parser.ParseFolderStructure(json, [CreateFile("file:1", "invoice.pdf")]);

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
    }

    /// <summary>Verifies result-count limits are enforced before a proposal can be published.</summary>
    [Fact]
    public void ParseFolderStructure_ExcessiveFolderCount_IsRejected()
    {
        var folders = Enumerable.Range(0, AiResponseLimits.MaximumFolders + 1)
            .Select(index => new { folderId = $"f{index}", name = $"Folder {index}", parentFolderId = (string?)null, reason = "why", confidence = 0.5 })
            .ToArray();
        var json = JsonSerializer.Serialize(new
        {
            taskId = AiPromptBuilder.FolderStructureTaskId,
            status = "suggestion",
            folders,
            assignments = new[] { new { sourceFileId = "file:1", folderId = "f0" } },
            reason = "why",
        });

        var result = _parser.ParseFolderStructure(json, [CreateFile("file:1", "invoice.pdf")]);

        Assert.False(result.IsValid);
    }

    /// <summary>Verifies structured responses above the parser bound are rejected without deserialization.</summary>
    [Fact]
    public void ParseFileRename_OversizedResponse_IsRejected()
    {
        var json = "{\"padding\":\"" + new string('x', AiResponseLimits.MaximumStructuredResponseBytes) + "\"}";

        var result = _parser.ParseFileRename(json, RenameRequest());

        Assert.False(result.IsValid);
        Assert.Contains("large", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies a complete document interpretation is validated and mapped to the known source.</summary>
    [Fact]
    public void ParseDocumentInterpretation_ValidResponse_IsAccepted()
    {
        const string json = """
            {"taskId":"document-text-interpretation-v1","status":"suggestion","sourceFileId":"item-001",
             "documentType":"Invoice","title":"Consulting invoice","tags":["Finance","Consulting"],
             "dates":["2026-07-24"],"issuer":"Local Studio","suggestedFolder":"Invoices",
             "reason":"Explicit invoice fields are present.","confidence":0.71}
            """;
        var request = DocumentRequest();

        var result = _parser.ParseDocumentInterpretation(
            json,
            request,
            [new AiPromptSourceMapping("item-001", "known:1", "invoice.pdf")]);

        var value = Assert.IsType<AiParsedDocumentInterpretation>(result.Value);
        Assert.Equal("known:1", value.SourceFileId);
        Assert.Equal("Invoices", value.SuggestedFolder);
        Assert.Equal(["finance", "consulting"], value.Tags.Select(tag => tag.NormalizedValue));
    }

    /// <summary>Verifies unsafe folder, invalid date, unknown identity, and smuggled no-suggestion values fail closed.</summary>
    [Theory]
    [InlineData("""{"taskId":"document-text-interpretation-v1","status":"suggestion","sourceFileId":"other","documentType":"Invoice","title":null,"tags":[],"dates":[],"issuer":null,"suggestedFolder":null,"reason":"why","confidence":0.5}""")]
    [InlineData("""{"taskId":"document-text-interpretation-v1","status":"suggestion","sourceFileId":"item-001","documentType":"Invoice","title":null,"tags":[],"dates":["24/07/2026"],"issuer":null,"suggestedFolder":null,"reason":"why","confidence":0.5}""")]
    [InlineData("""{"taskId":"document-text-interpretation-v1","status":"suggestion","sourceFileId":"item-001","documentType":"Invoice","title":null,"tags":[],"dates":[],"issuer":null,"suggestedFolder":"../outside","reason":"why","confidence":0.5}""")]
    [InlineData("""{"taskId":"document-text-interpretation-v1","status":"no_suggestion","reason":"why","tags":[]}""")]
    public void ParseDocumentInterpretation_UnsafeResponse_IsRejected(string json)
    {
        var result = _parser.ParseDocumentInterpretation(
            json,
            DocumentRequest(),
            [new AiPromptSourceMapping("item-001", "known:1", "invoice.pdf")]);

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
    }

    /// <summary>Provides invalid folder graphs, paths, identities, types, and confidence values.</summary>
    public static IEnumerable<object[]> InvalidFolderResponses()
    {
        yield return [FolderJson("../outside", null, "file:1", "f1")];
        yield return [FolderJson("C:\\System", null, "file:1", "f1")];
        yield return [FolderJson("NUL", null, "file:1", "f1")];
        yield return [FolderJson("Windows", null, "file:1", "f1")];
        yield return [FolderJson("Finance", "missing", "file:1", "f1")];
        yield return [FolderJson("Finance", "f1", "file:1", "f1")];
        yield return [FolderJson("Finance", null, "invented", "f1")];
        yield return ["""{"taskId":"folder-structure-v1","status":"suggestion","folders":[],"assignments":[],"reason":"why"}"""];
        yield return ["""{"taskId":"folder-structure-v1","status":"suggestion","folders":[{"folderId":"f1","name":"Finance","parentFolderId":null,"reason":"why","confidence":0.5},{"folderId":"f1","name":"Other","parentFolderId":null,"reason":"why","confidence":0.5}],"assignments":[{"sourceFileId":"file:1","folderId":"f1"}],"reason":"why"}"""];
        yield return ["""{"taskId":"folder-structure-v1","status":"suggestion","folders":[{"folderId":"f1","name":"Finance","parentFolderId":null,"reason":"why","confidence":0.5},{"folderId":"f2","name":"finance","parentFolderId":null,"reason":"why","confidence":0.5}],"assignments":[{"sourceFileId":"file:1","folderId":"f1"}],"reason":"why"}"""];
        yield return ["""{"taskId":"folder-structure-v1","status":"suggestion","folders":[{"folderId":"f1","name":"Finance","parentFolderId":null,"reason":"why","confidence":0.5}],"assignments":[{"sourceFileId":"file:1","folderId":"f1"},{"sourceFileId":"file:1","folderId":"f1"}],"reason":"why"}"""];
        yield return ["""{"taskId":"folder-structure-v1","status":"suggestion","folders":[{"folderId":"f1","name":"Finance","parentFolderId":null,"reason":"why","confidence":2}],"assignments":[{"sourceFileId":"file:1","folderId":"f1"}],"reason":"why"}"""];
        yield return ["""{"taskId":"folder-structure-v1","status":"suggestion","folders":"wrong","assignments":[],"reason":"why"}"""];
    }

    private static string RenameJson(string sourceId, string name, string confidence, string extra = "")
    {
        var escaped = name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"{{\"taskId\":\"file-rename-v1\",\"status\":\"suggestion\",\"sourceFileId\":\"{sourceId}\",\"suggestedFileName\":\"{escaped}\",\"reason\":\"A bounded reason.\",\"confidence\":{confidence}{extra}}}";
    }

    private static string ValidFolderJson() => """
        {
          "taskId":"folder-structure-v1",
          "status":"suggestion",
          "folders":[
            {"folderId":"f2","name":"Invoices","parentFolderId":"f1","reason":"Filename","confidence":0.7},
            {"folderId":"f1","name":"Finance","parentFolderId":null,"reason":"Category","confidence":0.8}
          ],
          "assignments":[{"sourceFileId":"file:1","folderId":"f2"}],
          "reason":"A bounded plan reason."
        }
        """;

    private static string FolderJson(string name, string? parentId, string sourceId, string assignmentFolderId) => JsonSerializer.Serialize(new
    {
        taskId = AiPromptBuilder.FolderStructureTaskId,
        status = "suggestion",
        folders = new[] { new { folderId = "f1", name, parentFolderId = parentId, reason = "why", confidence = 0.5 } },
        assignments = new[] { new { sourceFileId = sourceId, folderId = assignmentFolderId } },
        reason = "why",
    });

    private static AiFileRenameRequest RenameRequest(IReadOnlyList<string>? siblings = null) =>
        new(CreateFile("file:1", "invoice.pdf"), siblings ?? []);

    private static AiDocumentTextRequest DocumentRequest() =>
        new("known:1", "invoice.pdf", "Invoice date 2026-07-24 and issuer Local Studio.", null, []);

    private static ResultFile CreateFile(string id, string name) => new(
        id,
        $"C:\\Private\\{name}",
        name,
        Path.GetExtension(name),
        10,
        DateTimeOffset.UnixEpoch,
        FileCategory.Document,
        "Document",
        DuplicateStatus.Unique,
        null,
        false);
}
