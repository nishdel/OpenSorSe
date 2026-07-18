using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenSorSe.Application.AI;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;

namespace OpenSorSe.AI;

/// <summary>
/// Implements the provider-neutral suggestion boundary against a user-configured Ollama-compatible endpoint.
/// </summary>
public sealed class OllamaSuggestionProvider : IAiSuggestionProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes the Ollama transport with a caller-owned reusable HTTP client.
    /// </summary>
    /// <param name="httpClient">The reusable HTTP client used only for configured Ollama requests.</param>
    /// <param name="loggingService">The central redacted diagnostics service.</param>
    public OllamaSuggestionProvider(HttpClient httpClient, ILoggingService loggingService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = (loggingService ?? throw new ArgumentNullException(nameof(loggingService))).CreateLogger(nameof(OllamaSuggestionProvider));
    }

    /// <inheritdoc />
    public async Task<AiConnectionResult> GetConnectionAsync(AiSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.Enabled)
        {
            return new AiConnectionResult(AiAvailabilityState.Disabled, "AI assistance is disabled in Settings.", Array.Empty<AiModel>());
        }

        if (!TryCreateEndpoint(settings.Endpoint, out var endpoint))
        {
            return new AiConnectionResult(AiAvailabilityState.Unavailable, "The Ollama endpoint is invalid.", Array.Empty<AiModel>());
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            using var requestCancellation = CreateTimeoutCancellation(settings.RequestTimeoutSeconds, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, "api/tags"));
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCancellation.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama health check returned HTTP {StatusCode} after {ElapsedMilliseconds} ms.", (int)response.StatusCode, ElapsedMilliseconds(started));
                return new AiConnectionResult(AiAvailabilityState.Unavailable, "Ollama did not accept the connection test.", Array.Empty<AiModel>());
            }

            var responseBytes = await ReadBoundedContentAsync(response.Content, requestCancellation.Token).ConfigureAwait(false);
            var payload = JsonSerializer.Deserialize<ModelsResponse>(responseBytes, JsonOptions);
            if (payload?.Models is null)
            {
                _logger.LogWarning("Ollama health check returned an invalid model list after {ElapsedMilliseconds} ms.", ElapsedMilliseconds(started));
                return new AiConnectionResult(AiAvailabilityState.Unavailable, "Ollama returned an invalid model list.", Array.Empty<AiModel>());
            }

            var models = payload.Models
                .Where(model => !string.IsNullOrWhiteSpace(model.Name) &&
                                model.Name.Length <= AiSettings.MaximumModelIdentifierLength &&
                                !model.Name.Any(char.IsControl))
                .Select(model => new AiModel(model.Name!.Trim(), model.Name!.Trim()))
                .DistinctBy(model => model.Id, StringComparer.Ordinal)
                .OrderBy(model => model.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(OllamaTransportLimits.MaximumPublishedModelCount)
                .ToArray();
            _logger.LogInformation("Ollama health check succeeded with {ModelCount} model(s) after {ElapsedMilliseconds} ms.", models.Length, ElapsedMilliseconds(started));
            return new AiConnectionResult(
                models.Length == 0 ? AiAvailabilityState.NoModelsAvailable : AiAvailabilityState.Connected,
                models.Length == 0 ? "Ollama is reachable, but no installed models were found." : "Ollama is connected.",
                Array.AsReadOnly(models));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new AiConnectionResult(AiAvailabilityState.RequestCancelled, "The Ollama connection test was cancelled.", Array.Empty<AiModel>());
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Ollama health check timed out after {ElapsedMilliseconds} ms.", ElapsedMilliseconds(started));
            return new AiConnectionResult(AiAvailabilityState.Unavailable, "The Ollama connection test timed out.", Array.Empty<AiModel>());
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Ollama health check could not reach the configured endpoint after {ElapsedMilliseconds} ms.", ElapsedMilliseconds(started));
            return new AiConnectionResult(AiAvailabilityState.Unavailable, "Ollama is unavailable at the configured endpoint.", Array.Empty<AiModel>());
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Ollama health check returned malformed JSON after {ElapsedMilliseconds} ms.", ElapsedMilliseconds(started));
            return new AiConnectionResult(AiAvailabilityState.Unavailable, "Ollama returned an invalid model list.", Array.Empty<AiModel>());
        }
        catch (InvalidDataException exception)
        {
            _logger.LogWarning(exception, "Ollama health check exceeded the supported response size after {ElapsedMilliseconds} ms.", ElapsedMilliseconds(started));
            return new AiConnectionResult(AiAvailabilityState.Unavailable, "Ollama returned an oversized model list.", Array.Empty<AiModel>());
        }
    }

    /// <inheritdoc />
    public async Task<AiProviderGenerationResult> GenerateAsync(AiProviderGenerationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryCreateEndpoint(request.Endpoint, out var endpoint) ||
            string.IsNullOrWhiteSpace(request.Model) ||
            request.Model.Length > AiSettings.MaximumModelIdentifierLength ||
            request.Model.Any(char.IsControl) ||
            string.IsNullOrWhiteSpace(request.Prompt) ||
            Encoding.UTF8.GetByteCount(request.Prompt) > OllamaTransportLimits.MaximumPromptBytes)
        {
            return new AiProviderGenerationResult(null, AiProviderFailureKind.Configuration, "The Ollama suggestion settings are incomplete.");
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            using var requestCancellation = CreateTimeoutCancellation(request.Timeout, cancellationToken);
            var payload = new GenerateRequest(request.Model, request.Prompt, false, "json", new GenerateOptions(0.2));
            using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, "api/generate"))
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            };
            using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestCancellation.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama {SuggestionKind} request for model {Model} returned HTTP {StatusCode} after {ElapsedMilliseconds} ms.", request.Kind, request.Model, (int)response.StatusCode, ElapsedMilliseconds(started));
                return new AiProviderGenerationResult(null, AiProviderFailureKind.HttpFailure, "Ollama could not complete the suggestion request.");
            }

            var responseBytes = await ReadBoundedContentAsync(response.Content, requestCancellation.Token).ConfigureAwait(false);
            var payloadResponse = JsonSerializer.Deserialize<GenerateResponse>(responseBytes, JsonOptions);
            if (string.IsNullOrWhiteSpace(payloadResponse?.Response))
            {
                _logger.LogWarning("Ollama {SuggestionKind} request for model {Model} returned no structured response after {ElapsedMilliseconds} ms.", request.Kind, request.Model, ElapsedMilliseconds(started));
                return new AiProviderGenerationResult(null, AiProviderFailureKind.InvalidResponse, "Ollama returned no structured suggestion data.");
            }

            _logger.LogInformation("Ollama {SuggestionKind} request for model {Model} succeeded after {ElapsedMilliseconds} ms.", request.Kind, request.Model, ElapsedMilliseconds(started));
            return new AiProviderGenerationResult(payloadResponse.Response, AiProviderFailureKind.None, "Ollama returned a response for validation.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new AiProviderGenerationResult(null, AiProviderFailureKind.Cancelled, "The suggestion request was cancelled.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Ollama {SuggestionKind} request for model {Model} timed out after {ElapsedMilliseconds} ms.", request.Kind, request.Model, ElapsedMilliseconds(started));
            return new AiProviderGenerationResult(null, AiProviderFailureKind.Timeout, "The Ollama suggestion request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Ollama {SuggestionKind} request for model {Model} could not reach the configured endpoint after {ElapsedMilliseconds} ms.", request.Kind, request.Model, ElapsedMilliseconds(started));
            return new AiProviderGenerationResult(null, AiProviderFailureKind.Unavailable, "Ollama is unavailable at the configured endpoint.");
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Ollama {SuggestionKind} response for model {Model} was malformed after {ElapsedMilliseconds} ms.", request.Kind, request.Model, ElapsedMilliseconds(started));
            return new AiProviderGenerationResult(null, AiProviderFailureKind.InvalidResponse, "Ollama returned malformed response data.");
        }
        catch (InvalidDataException exception)
        {
            _logger.LogWarning(exception, "Ollama {SuggestionKind} response for model {Model} exceeded the supported size after {ElapsedMilliseconds} ms.", request.Kind, request.Model, ElapsedMilliseconds(started));
            return new AiProviderGenerationResult(null, AiProviderFailureKind.InvalidResponse, "Ollama returned an oversized response.");
        }
    }

    private static async Task<byte[]> ReadBoundedContentAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > OllamaTransportLimits.MaximumResponseBytes)
        {
            throw new InvalidDataException("The provider response exceeds the supported size.");
        }

        await using var source = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var destination = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return destination.ToArray();
            }

            if (destination.Length + read > OllamaTransportLimits.MaximumResponseBytes)
            {
                throw new InvalidDataException("The provider response exceeds the supported size.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private static CancellationTokenSource CreateTimeoutCancellation(int seconds, CancellationToken cancellationToken) =>
        CreateTimeoutCancellation(TimeSpan.FromSeconds(seconds), cancellationToken);

    private static CancellationTokenSource CreateTimeoutCancellation(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout);
        return linked;
    }

    private static bool TryCreateEndpoint(string endpoint, out Uri baseEndpoint)
    {
        baseEndpoint = default!;
        if (!Uri.TryCreate(endpoint?.Trim(), UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(parsed.Host))
        {
            return false;
        }

        baseEndpoint = new Uri($"{parsed.Scheme}://{parsed.Authority}{parsed.AbsolutePath.TrimEnd('/')}/", UriKind.Absolute);
        return true;
    }

    private static long ElapsedMilliseconds(long timestamp) => (long)Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;

    private sealed record ModelsResponse([property: JsonPropertyName("models")] List<ModelResponse>? Models);

    private sealed record ModelResponse([property: JsonPropertyName("name")] string? Name);

    private sealed record GenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("options")] GenerateOptions Options);

    private sealed record GenerateOptions([property: JsonPropertyName("temperature")] double Temperature);

    private sealed record GenerateResponse([property: JsonPropertyName("response")] string? Response);
}
