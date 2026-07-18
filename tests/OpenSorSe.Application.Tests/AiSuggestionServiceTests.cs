using Microsoft.Extensions.Logging;
using OpenSorSe.Application.AI;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.Tests;

/// <summary>
/// Verifies application-owned parsing, validation, and preference behavior for optional AI suggestions.
/// </summary>
public sealed class AiSuggestionServiceTests
{
    /// <summary>
    /// Verifies a valid structured provider response is normalized into a safe review-only suggestion.
    /// </summary>
    [Fact]
    public async Task GenerateFileSuggestionAsync_ValidStructuredResponse_NormalizesAndPreservesExtension()
    {
        var provider = new FakeProvider("""{"fileName":"April Invoice.pdf","tags":[" Finance ","finance","Invoices"],"category":"Document","destinationFolder":"Finance/Invoices","explanation":"Grouped by the supplied filename."}""");
        var store = new InMemoryDecisionHistoryStore();
        var service = CreateService(provider, store);

        var result = await service.GenerateFileSuggestionAsync(CreateRequest(), EnabledSettings(), CancellationToken.None);

        var suggestion = Assert.IsType<AiFileOrganizationSuggestion>(result.Suggestion);
        Assert.Equal(AiAvailabilityState.ModelSelected, result.State);
        Assert.Equal("April Invoice.pdf", suggestion.SuggestedFileName);
        Assert.Equal(["finance", "invoices"], suggestion.SuggestedTags.Select(tag => tag.NormalizedValue));
        Assert.Equal(FileCategory.Document, suggestion.SuggestedCategory);
        Assert.Equal("Finance/Invoices", suggestion.SuggestedDestinationFolder);
        Assert.DoesNotContain("C:\\", provider.LastPrompt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies unsafe paths, reserved names, extension changes, and sibling conflicts are never surfaced as a suggestion.
    /// </summary>
    [Theory]
    [InlineData("../invoice.pdf")]
    [InlineData("C:\\temp\\invoice.pdf")]
    [InlineData("NUL.pdf")]
    [InlineData("invoice.txt")]
    [InlineData("existing.pdf")]
    public async Task GenerateFileSuggestionAsync_UnsafeFileName_IsRejected(string fileName)
    {
        var escapedFileName = fileName.Replace("\\", "\\\\", StringComparison.Ordinal);
        var provider = new FakeProvider($"{{\"fileName\":\"{escapedFileName}\",\"tags\":[],\"category\":null,\"destinationFolder\":null,\"explanation\":\"test\"}}");
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());

        var result = await service.GenerateFileSuggestionAsync(CreateRequest(siblings: ["existing.pdf"]), EnabledSettings(), CancellationToken.None);

        Assert.Equal(AiAvailabilityState.ResponseInvalid, result.State);
        Assert.Null(result.Suggestion);
    }

    /// <summary>
    /// Verifies disabled AI never reaches the provider and leaves deterministic application features independent.
    /// </summary>
    [Fact]
    public async Task GenerateFileSuggestionAsync_Disabled_ReturnsDisabledWithoutCallingProvider()
    {
        var provider = new FakeProvider("{}");
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());

        var result = await service.GenerateFileSuggestionAsync(CreateRequest(), new AiSettings(), CancellationToken.None);

        Assert.Equal(AiAvailabilityState.Disabled, result.State);
        Assert.False(provider.GenerateCalled);
    }

    /// <summary>
    /// Verifies a folder structure containing traversal is rejected as untrusted response data.
    /// </summary>
    [Fact]
    public async Task GenerateFolderStructureAsync_Traversal_IsRejected()
    {
        var provider = new FakeProvider("""{"items":[{"fileId":"file:1","destinationFolder":"../outside"}],"explanation":"test"}""");
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());
        var file = CreateFile("file:1", "report.pdf");

        var result = await service.GenerateFolderStructureAsync(new AiFolderStructureRequest([file], ["Finance"]), EnabledSettings(), CancellationToken.None);

        Assert.Equal(AiAvailabilityState.ResponseInvalid, result.State);
        Assert.Null(result.Plan);
    }

    /// <summary>
    /// Verifies decision aggregation remains local, deterministic, bounded, and does not claim model training.
    /// </summary>
    [Fact]
    public void PreferenceAggregator_AcceptedAndRejectedDecisions_ProducesStableSignals()
    {
        var decisions = new AiSuggestionDecision[]
        {
            CreateDecision(AiSuggestionDecisionKind.Tags, AiSuggestionDecisionOutcome.Accepted, "finance", "finance"),
            CreateDecision(AiSuggestionDecisionKind.Tags, AiSuggestionDecisionOutcome.Accepted, "finance", "finance"),
            CreateDecision(AiSuggestionDecisionKind.DestinationFolder, AiSuggestionDecisionOutcome.Edited, "Work", "Work/Invoices"),
            CreateDecision(AiSuggestionDecisionKind.Category, AiSuggestionDecisionOutcome.Accepted, "Document", "Document"),
            CreateDecision(AiSuggestionDecisionKind.Rename, AiSuggestionDecisionOutcome.Rejected, "draft.pdf", null),
        };

        var preferences = AiPreferenceAggregator.Build(decisions);

        Assert.Equal(["finance"], preferences.PreferredTags);
        Assert.Equal(["Work/Invoices"], preferences.PreferredFolders);
        Assert.Equal(["Document"], preferences.PreferredCategories);
        Assert.Equal(["draft.pdf"], preferences.RejectedValues);
    }

    private static AiSuggestionService CreateService(FakeProvider provider, InMemoryDecisionHistoryStore store) =>
        new(provider, store, new LoggingService(), new FixedTimeProvider(DateTimeOffset.UnixEpoch));

    private static AiFileSuggestionRequest CreateRequest(IReadOnlyList<string>? siblings = null) =>
        new(CreateFile("file:1", "invoice.pdf"), ["Finance", "Personal"], siblings ?? Array.Empty<string>());

    private static ResultFile CreateFile(string id, string name) => new(
        id,
        $"C:\\Reports\\{name}",
        name,
        Path.GetExtension(name),
        10,
        DateTimeOffset.UnixEpoch,
        FileCategory.Document,
        "Document",
        DuplicateStatus.Unique,
        null,
        false);

    private static AiSettings EnabledSettings() => new()
    {
        Enabled = true,
        SelectedModel = "local-model",
    };

    private static AiSuggestionDecision CreateDecision(AiSuggestionDecisionKind kind, AiSuggestionDecisionOutcome outcome, string suggested, string? final) =>
        new(kind, outcome, ".pdf", suggested, final, "Ollama", "local-model", DateTimeOffset.UnixEpoch);

    private sealed class FakeProvider(string response) : IAiSuggestionProvider
    {
        public bool GenerateCalled { get; private set; }

        public string LastPrompt { get; private set; } = string.Empty;

        public Task<AiConnectionResult> GetConnectionAsync(AiSettings settings, CancellationToken cancellationToken) =>
            Task.FromResult(new AiConnectionResult(AiAvailabilityState.Connected, "Connected", [new AiModel("local-model", "local-model")]));

        public Task<AiProviderGenerationResult> GenerateAsync(AiProviderGenerationRequest request, CancellationToken cancellationToken)
        {
            GenerateCalled = true;
            LastPrompt = request.Prompt;
            return Task.FromResult(new AiProviderGenerationResult(response, AiProviderFailureKind.None, "OK"));
        }
    }

    private sealed class InMemoryDecisionHistoryStore : IDecisionHistoryStore
    {
        private readonly List<AiSuggestionDecision> _decisions = [];

        public Task<IReadOnlyList<AiSuggestionDecision>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AiSuggestionDecision>>(Array.AsReadOnly(_decisions.ToArray()));

        public Task AppendAsync(AiSuggestionDecision decision, CancellationToken cancellationToken)
        {
            _decisions.Add(decision);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            _decisions.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }
}
