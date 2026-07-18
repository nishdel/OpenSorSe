using System.Net;
using System.Text;
using OpenSorSe.AI;
using OpenSorSe.Application.AI;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;

namespace OpenSorSe.Application.Tests;

/// <summary>
/// Verifies the Ollama HTTP implementation with mocked transport and no installed Ollama dependency.
/// </summary>
public sealed class OllamaSuggestionProviderTests
{
    /// <summary>
    /// Verifies a reachable provider discovers installed models through its typed transport contract.
    /// </summary>
    [Fact]
    public async Task GetConnectionAsync_ReachableProvider_ReturnsDiscoveredModels()
    {
        using var client = new HttpClient(new StubHandler(_ => Json(HttpStatusCode.OK, """{"models":[{"name":"llama3:latest"},{"name":"mistral"}]}""")));
        var provider = new OllamaSuggestionProvider(client, new LoggingService());

        var result = await provider.GetConnectionAsync(EnabledSettings(), CancellationToken.None);

        Assert.Equal(AiAvailabilityState.Connected, result.State);
        Assert.Equal(["llama3:latest", "mistral"], result.Models.Select(model => model.Id));
    }

    /// <summary>
    /// Verifies no installed model is a clear nonfatal provider state.
    /// </summary>
    [Fact]
    public async Task GetConnectionAsync_NoModels_ReturnsNoModelsState()
    {
        using var client = new HttpClient(new StubHandler(_ => Json(HttpStatusCode.OK, """{"models":[]}""")));
        var provider = new OllamaSuggestionProvider(client, new LoggingService());

        var result = await provider.GetConnectionAsync(EnabledSettings(), CancellationToken.None);

        Assert.Equal(AiAvailabilityState.NoModelsAvailable, result.State);
        Assert.Empty(result.Models);
    }

    /// <summary>
    /// Verifies malformed model output and HTTP errors remain isolated from core application behavior.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    [InlineData(HttpStatusCode.OK, "{invalid")]
    public async Task GetConnectionAsync_InvalidOrFailedResponse_ReturnsUnavailable(HttpStatusCode statusCode, string body)
    {
        using var client = new HttpClient(new StubHandler(_ => Json(statusCode, body)));
        var provider = new OllamaSuggestionProvider(client, new LoggingService());

        var result = await provider.GetConnectionAsync(EnabledSettings(), CancellationToken.None);

        Assert.Equal(AiAvailabilityState.Unavailable, result.State);
    }

    /// <summary>
    /// Verifies valid structured generation is returned as raw JSON for application-owned parsing and never exposes transport DTOs.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ValidResponse_ReturnsStructuredJson()
    {
        using var client = new HttpClient(new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/api/generate", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            return Json(HttpStatusCode.OK, """{"response":"{\"fileName\":null}"}""");
        }));
        var provider = new OllamaSuggestionProvider(client, new LoggingService());

        var result = await provider.GenerateAsync(new AiProviderGenerationRequest(
            AiSuggestionKind.FileOrganization,
            "http://127.0.0.1:11434",
            "llama3:latest",
            "redacted test prompt",
            TimeSpan.FromSeconds(1)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("{\"fileName\":null}", result.StructuredJson);
    }

    /// <summary>
    /// Verifies request timeout and cancellation are distinct safe outcomes.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_TimeoutAndCancellation_ReturnDistinctFailures()
    {
        using var client = new HttpClient(new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Json(HttpStatusCode.OK, "{}");
        }));
        var provider = new OllamaSuggestionProvider(client, new LoggingService());
        var request = new AiProviderGenerationRequest(AiSuggestionKind.FileOrganization, "http://127.0.0.1:11434", "model", "prompt", TimeSpan.FromMilliseconds(25));

        var timeout = await provider.GenerateAsync(request, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = await provider.GenerateAsync(request, cancellation.Token);

        Assert.Equal(AiProviderFailureKind.Timeout, timeout.FailureKind);
        Assert.Equal(AiProviderFailureKind.Cancelled, cancelled.FailureKind);
    }

    private static AiSettings EnabledSettings() => new() { Enabled = true };

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) => new(statusCode)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : this((request, _) => Task.FromResult(handler(request)))
        {
        }

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _handler(request, cancellationToken);
    }
}
