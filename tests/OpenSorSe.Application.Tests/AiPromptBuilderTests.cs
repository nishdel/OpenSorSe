using System.Text.Json;
using OpenSorSe.Application.AI;
using OpenSorSe.Application.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.Tests;

/// <summary>Verifies deterministic, bounded, metadata-only capability prompts.</summary>
public sealed class AiPromptBuilderTests
{
    private readonly AiPromptBuilder _builder = new();

    /// <summary>Verifies rename prompts identify the task, schema, and mandatory safety boundary.</summary>
    [Fact]
    public void BuildFileRenamePrompt_IncludesStructuredSectionsAndEscapesInput()
    {
        var file = CreateFile("file:1", "invoice \"draft\".pdf", "C:\\Private\\content-secret\\invoice.pdf");

        var result = _builder.BuildFileRenamePrompt(
            new AiFileRenameRequest(file, ["sibling\\name.pdf", "z.pdf"]),
            EmptyPreferences());

        using var document = JsonDocument.Parse(result.Prompt);
        var root = document.RootElement;
        Assert.Equal(AiPromptBuilder.FileRenameTaskId, result.TaskId);
        Assert.Equal(AiPromptBuilder.FileRenameTaskId, root.GetProperty("taskIdentifier").GetString());
        Assert.True(root.TryGetProperty("allowedReasoningScope", out _));
        Assert.True(root.TryGetProperty("mandatoryRules", out _));
        Assert.True(root.TryGetProperty("forbiddenBehaviors", out _));
        Assert.True(root.TryGetProperty("requiredResponseSchema", out _));
        Assert.True(root.TryGetProperty("noSuggestionBehavior", out _));
        Assert.Contains("Do not wrap it in Markdown fences", root.GetProperty("outputInstruction").GetString(), StringComparison.Ordinal);
        Assert.Equal("invoice \"draft\".pdf",
            root.GetProperty("inputData").GetProperty("sourceFile").GetProperty("currentFileName").GetString());
        Assert.Equal("item-001",
            root.GetProperty("inputData").GetProperty("sourceFile").GetProperty("sourceFileId").GetString());
        Assert.Equal("file:1", Assert.Single(result.SourceMappings).KnownSourceId);
        Assert.DoesNotContain("content-secret", result.Prompt, StringComparison.Ordinal);
        Assert.DoesNotContain(file.FullPath, result.Prompt, StringComparison.Ordinal);
    }

    /// <summary>Verifies deterministically equivalent folder metadata produces byte-identical prompts.</summary>
    [Fact]
    public void BuildFolderStructurePrompt_ReorderedInputs_IsDeterministic()
    {
        var first = _builder.BuildFolderStructurePrompt(
            new AiFolderStructureRequest(
                [CreateFile("b", "b.pdf"), CreateFile("a", "a.pdf")],
                ["Zeta", "Alpha", "Alpha"]),
            EmptyPreferences());
        var second = _builder.BuildFolderStructurePrompt(
            new AiFolderStructureRequest(
                [CreateFile("a", "a.pdf"), CreateFile("b", "b.pdf")],
                ["Alpha", "Zeta"]),
            EmptyPreferences());

        Assert.Equal(first.Prompt, second.Prompt);
        Assert.Equal(["a", "b"], first.IncludedSourceIds);
        Assert.Equal(["item-001", "item-002"], first.SourceMappings.Select(mapping => mapping.RequestSourceId));
        Assert.Equal(["a", "b"], first.SourceMappings.Select(mapping => mapping.KnownSourceId));
        Assert.Contains("assign every supplied item exactly once", first.Prompt, StringComparison.Ordinal);
        Assert.Contains(AiPromptBuilder.FolderStructureTaskId, first.Prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\Private", first.Prompt, StringComparison.Ordinal);
    }

    /// <summary>Verifies large contexts are deterministically bounded and reported.</summary>
    [Fact]
    public void BuildFolderStructurePrompt_LargeContext_IsBoundedAndStable()
    {
        var files = Enumerable.Range(0, 40)
            .Reverse()
            .Select(index => CreateFile($"file:{index:D2}", $"file-{index:D2}.pdf"))
            .ToArray();
        var folders = Enumerable.Range(0, 45).Select(index => $"Folder {index:D2}").ToArray();

        var result = _builder.BuildFolderStructurePrompt(new AiFolderStructureRequest(files, folders), EmptyPreferences());

        Assert.True(result.WasInputBounded);
        Assert.Equal(40, result.TotalInputCount);
        Assert.Equal(25, result.IncludedInputCount);
        Assert.Equal(15, result.OmittedInputCount);
        Assert.Equal(AiPromptLimits.MaximumFolderStructureFiles, result.IncludedSourceIds.Count);
        Assert.Equal("file:00", result.IncludedSourceIds[0]);
        Assert.Equal("file:24", result.IncludedSourceIds[^1]);
        using var document = JsonDocument.Parse(result.Prompt);
        Assert.Equal(AiPromptLimits.MaximumFolderStructureFiles,
            document.RootElement.GetProperty("inputData").GetProperty("files").GetArrayLength());
        Assert.Equal(AiPromptLimits.MaximumExistingFolderNames,
            document.RootElement.GetProperty("inputData").GetProperty("existingLogicalFolderNames").GetArrayLength());
    }

    /// <summary>Verifies sibling-name bounds cannot leak an unlimited nearby directory listing.</summary>
    [Fact]
    public void BuildFileRenamePrompt_TooManySiblings_BoundsAndReportsInput()
    {
        var siblings = Enumerable.Range(0, 50).Select(index => $"nearby-{index:D2}.pdf").Reverse().ToArray();

        var result = _builder.BuildFileRenamePrompt(
            new AiFileRenameRequest(CreateFile("file:1", "invoice.pdf"), siblings),
            EmptyPreferences());

        Assert.True(result.WasInputBounded);
        using var document = JsonDocument.Parse(result.Prompt);
        var values = document.RootElement.GetProperty("inputData").GetProperty("siblingFileNames");
        Assert.Equal(AiPromptLimits.MaximumSiblingFileNames, values.GetArrayLength());
        Assert.Equal("nearby-00.pdf", values[0].GetString());
    }

    /// <summary>Verifies deterministic per-value truncation is also disclosed to the review workflow.</summary>
    [Fact]
    public void BuildFolderStructurePrompt_OverlongFolderContext_ReportsBoundedInput()
    {
        var result = _builder.BuildFolderStructurePrompt(
            new AiFolderStructureRequest([CreateFile("file:1", "invoice.pdf")], [new string('f', 300)]),
            EmptyPreferences());

        Assert.True(result.WasInputBounded);
        using var document = JsonDocument.Parse(result.Prompt);
        Assert.Equal(255,
            document.RootElement.GetProperty("inputData").GetProperty("existingLogicalFolderNames")[0].GetString()!.Length);
    }

    private static AiPreferenceSummary EmptyPreferences() =>
        new([], [], [], []);

    private static ResultFile CreateFile(string id, string name, string? path = null) => new(
        id,
        path ?? $"C:\\Private\\{name}",
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
