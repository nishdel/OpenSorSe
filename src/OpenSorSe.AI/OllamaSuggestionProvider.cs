using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenSorSe.Application.AI;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;

namespace OpenSorSe.AI;

/// <summary>Implements the provider-neutral suggestion boundary against Ollama.</summary>
public sealed class OllamaSuggestionProvider : IAiSuggestionProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>Initializes the transport with one caller-owned reusable HTTP client.</summary>
    public OllamaSuggestionProvider(HttpClient httpClient, ILoggingService loggingService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = (loggingService ?? throw new ArgumentNullException(nameof(loggingService))).CreateLogger(nameof(OllamaSuggestionProvider));
    }

    /// <inheritdoc />
    public async Task<AiConnectionResult> CheckConnectionAsync(AiSettings settings, CancellationToken cancellationToken)
    {
        var validation = ValidateConnectionSettings(settings);
        if (validation is not null)
        {
            return validation;
        }

        OllamaEndpointNormalizer.TryNormalize(settings.Endpoint, out var endpoint);
        var started = Stopwatch.GetTimestamp();
        try
        {
            using var requestCancellation = CreateTimeoutCancellation(settings.RequestTimeoutSeconds, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, "api/version"));
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCancellation.Token).ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(started);
            if (!response.IsSuccessStatusCode)
            {
                await ReadBoundedContentAsync(response.Content, requestCancellation.Token).ConfigureAwait(false);
                _logger.LogWarning("Ollama connection check returned HTTP {StatusCode} after {ElapsedMilliseconds} ms.", (int)response.StatusCode, (long)elapsed.TotalMilliseconds);
                return ConnectionFailure("Ollama is reachable but did not accept the connection check.", endpoint, elapsed, response.StatusCode);
            }

            var responseBytes = await ReadBoundedContentAsync(response.Content, requestCancellation.Token).ConfigureAwait(false);
            var payload = JsonSerializer.Deserialize<VersionResponse>(responseBytes, JsonOptions);
            var version = string.IsNullOrWhiteSpace(payload?.Version) ? null : Bound(payload.Version, 128);
            _logger.LogInformation("Ollama connection check succeeded after {ElapsedMilliseconds} ms.", (long)elapsed.TotalMilliseconds);
            return new AiConnectionResult(
                AiAvailabilityState.Connected,
                version is null ? "Ollama is reachable." : $"Ollama is reachable (version {version}).",
                Array.Empty<AiModel>())
            {
                NormalizedEndpoint = endpoint.AbsoluteUri.TrimEnd('/'),
                ProviderVersion = version,
                HttpStatusCode = (int)response.StatusCode,
                Elapsed = elapsed,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ConnectionFailure("The Ollama connection check was cancelled.", endpoint, Stopwatch.GetElapsedTime(started), null, AiAvailabilityState.RequestCancelled);
        }
        catch (OperationCanceledException)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            _logger.LogWarning("Ollama connection check timed out after {ElapsedMilliseconds} ms.", (long)elapsed.TotalMilliseconds);
            return ConnectionFailure($"The Ollama connection check timed out after {settings.RequestTimeoutSeconds} seconds.", endpoint, elapsed);
        }
        catch (HttpRequestException exception)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            _logger.LogWarning(exception, "Ollama connection check could not reach the configured endpoint after {ElapsedMilliseconds} ms.", (long)elapsed.TotalMilliseconds);
            return ConnectionFailure("Ollama is unavailable at the configured endpoint. Confirm that Ollama is installed and running.", endpoint, elapsed);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            _logger.LogWarning(exception, "Ollama connection check returned invalid bounded data after {ElapsedMilliseconds} ms.", (long)elapsed.TotalMilliseconds);
            return ConnectionFailure("Ollama returned invalid connection information.", endpoint, elapsed);
        }
    }

    /// <inheritdoc />
    public async Task<AiConnectionResult> GetConnectionAsync(AiSettings settings, CancellationToken cancellationToken)
    {
        var validation = ValidateConnectionSettings(settings);
        if (validation is not null)
        {
            return validation;
        }

        OllamaEndpointNormalizer.TryNormalize(settings.Endpoint, out var endpoint);
        var started = Stopwatch.GetTimestamp();
        try
        {
            using var requestCancellation = CreateTimeoutCancellation(settings.RequestTimeoutSeconds, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, "api/tags"));
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCancellation.Token).ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(started);
            if (!response.IsSuccessStatusCode)
            {
                await ReadBoundedContentAsync(response.Content, requestCancellation.Token).ConfigureAwait(false);
                _logger.LogWarning("Ollama model discovery returned HTTP {StatusCode} after {ElapsedMilliseconds} ms.", (int)response.StatusCode, (long)elapsed.TotalMilliseconds);
                return ConnectionFailure("Ollama did not accept model discovery.", endpoint, elapsed, response.StatusCode);
            }

            var responseBytes = await ReadBoundedContentAsync(response.Content, requestCancellation.Token).ConfigureAwait(false);
            var payload = JsonSerializer.Deserialize<ModelsResponse>(responseBytes, JsonOptions);
            if (payload?.Models is null)
            {
                return ConnectionFailure("Ollama returned an invalid model list.", endpoint, elapsed, response.StatusCode);
            }

            var models = payload.Models
                .Where(model => IsValidModelIdentifier(model.Name))
                .Select(model => new AiModel(model.Name!, model.Name!))
                .DistinctBy(model => model.Id, StringComparer.Ordinal)
                .OrderBy(model => model.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(OllamaTransportLimits.MaximumPublishedModelCount)
                .ToArray();
            _logger.LogInformation("Ollama model discovery found {ModelCount} model(s) after {ElapsedMilliseconds} ms.", models.Length, (long)elapsed.TotalMilliseconds);
            return new AiConnectionResult(
                models.Length == 0 ? AiAvailabilityState.NoModelsAvailable : AiAvailabilityState.Connected,
                models.Length == 0 ? "Ollama is reachable, but no installed models were found." : $"Discovered {models.Length} installed Ollama model(s).",
                Array.AsReadOnly(models))
            {
                NormalizedEndpoint = endpoint.AbsoluteUri.TrimEnd('/'),
                HttpStatusCode = (int)response.StatusCode,
                Elapsed = elapsed,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ConnectionFailure("Ollama model discovery was cancelled.", endpoint, Stopwatch.GetElapsedTime(started), null, AiAvailabilityState.RequestCancelled);
        }
        catch (OperationCanceledException)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            return ConnectionFailure($"Ollama model discovery timed out after {settings.RequestTimeoutSeconds} seconds.", endpoint, elapsed);
        }
        catch (HttpRequestException exception)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            _logger.LogWarning(exception, "Ollama model discovery could not reach the configured endpoint after {ElapsedMilliseconds} ms.", (long)elapsed.TotalMilliseconds);
            return ConnectionFailure("Ollama is unavailable at the configured endpoint. Confirm that Ollama is installed and running.", endpoint, elapsed);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            _logger.LogWarning(exception, "Ollama model discovery returned invalid bounded data after {ElapsedMilliseconds} ms.", (long)elapsed.TotalMilliseconds);
            return ConnectionFailure(exception is InvalidDataException ? "Ollama returned an oversized model list." : "Ollama returned an invalid model list.", endpoint, elapsed);
        }
    }

    /// <inheritdoc />
    public async Task<AiProviderGenerationResult> GenerateAsync(AiProviderGenerationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Kind) ||
            !OllamaEndpointNormalizer.TryNormalize(request.Endpoint, out var endpoint) ||
            !IsValidModelIdentifier(request.Model) ||
            string.IsNullOrWhiteSpace(request.Prompt) ||
            Encoding.UTF8.GetByteCount(request.Prompt) > OllamaTransportLimits.MaximumPromptBytes ||
            request.Timeout < TimeSpan.FromSeconds(AiSettings.MinimumRequestTimeoutSeconds) ||
            request.Timeout > TimeSpan.FromSeconds(AiSettings.MaximumRequestTimeoutSeconds))
        {
            return new AiProviderGenerationResult(null, AiProviderFailureKind.Configuration, "The Ollama suggestion settings are incomplete or outside supported limits.");
        }

        var started = Stopwatch.GetTimestamp();
        var normalizedEndpoint = endpoint.AbsoluteUri.TrimEnd('/');
        try
        {
            using var requestCancellation = CreateTimeoutCancellation(request.Timeout, cancellationToken);
            var payload = new GenerateRequest(request.Model, request.Prompt, false, "json", "5m", new GenerateOptions(0.1));
            using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, "api/generate"))
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            };
            Report(request, AiRequestStage.SendingRequest, "Sending the bounded request to Ollama.", started);
            var sendTask = _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestCancellation.Token);
            Report(request, AiRequestStage.WaitingForModel, "Waiting for the selected model to respond.", started);
            using var response = await sendTask.ConfigureAwait(false);
            Report(request, AiRequestStage.ReceivingResponse, "Receiving the bounded Ollama response.", started);
            var responseBytes = await ReadBoundedContentAsync(response.Content, requestCancellation.Token).ConfigureAwait(false);
            var rawEnvelope = Encoding.UTF8.GetString(responseBytes);
            var elapsed = Stopwatch.GetElapsedTime(started);
            if (!response.IsSuccessStatusCode)
            {
                var failureKind = MapHttpFailure(response.StatusCode, rawEnvelope);
                _logger.LogWarning("Ollama {SuggestionKind} request for model {Model} returned HTTP {StatusCode} after {ElapsedMilliseconds} ms.", request.Kind, request.Model, (int)response.StatusCode, (long)elapsed.TotalMilliseconds);
                return WithDiagnostics(
                    new AiProviderGenerationResult(null, failureKind, FailureMessage(failureKind, response.StatusCode)),
                    normalizedEndpoint,
                    response.StatusCode,
                    elapsed,
                    rawEnvelope);
            }

            var payloadResponse = JsonSerializer.Deserialize<GenerateResponse>(responseBytes, JsonOptions);
            if (string.IsNullOrWhiteSpace(payloadResponse?.Response))
            {
                return WithDiagnostics(
                    new AiProviderGenerationResult(null, AiProviderFailureKind.InvalidResponse, "Ollama returned no structured suggestion data."),
                    normalizedEndpoint,
                    response.StatusCode,
                    elapsed,
                    payloadResponse?.Response ?? string.Empty);
            }

            _logger.LogInformation("Ollama {SuggestionKind} request for model {Model} succeeded after {ElapsedMilliseconds} ms.", request.Kind, request.Model, (long)elapsed.TotalMilliseconds);
            return WithDiagnostics(
                new AiProviderGenerationResult(payloadResponse.Response, AiProviderFailureKind.None, "Ollama returned a response for validation."),
                normalizedEndpoint,
                response.StatusCode,
                elapsed,
                payloadResponse.Response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            return WithDiagnostics(new AiProviderGenerationResult(null, AiProviderFailureKind.Cancelled, "The suggestion request was cancelled."), normalizedEndpoint, null, elapsed, string.Empty);
        }
        catch (OperationCanceledException)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            _logger.LogWarning("Ollama {SuggestionKind} request for model {Model} timed out after {ElapsedMilliseconds} ms.", request.Kind, request.Model, (long)elapsed.TotalMilliseconds);
            return WithDiagnostics(new AiProviderGenerationResult(null, AiProviderFailureKind.Timeout, $"The Ollama suggestion request timed out after {(int)request.Timeout.TotalSeconds} seconds."), normalizedEndpoint, null, elapsed, string.Empty);
        }
        catch (HttpRequestException exception)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            _logger.LogWarning(exception, "Ollama {SuggestionKind} request for model {Model} could not reach the configured endpoint after {ElapsedMilliseconds} ms.", request.Kind, request.Model, (long)elapsed.TotalMilliseconds);
            return WithDiagnostics(new AiProviderGenerationResult(null, AiProviderFailureKind.Unavailable, "Ollama is unavailable at the configured endpoint. Confirm that Ollama is running."), normalizedEndpoint, null, elapsed, string.Empty);
        }
        catch (JsonException exception)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            _logger.LogWarning(exception, "Ollama {SuggestionKind} response for model {Model} was malformed after {ElapsedMilliseconds} ms.", request.Kind, request.Model, (long)elapsed.TotalMilliseconds);
            return WithDiagnostics(new AiProviderGenerationResult(null, AiProviderFailureKind.InvalidResponse, "Ollama returned malformed response data."), normalizedEndpoint, null, elapsed, string.Empty);
        }
        catch (InvalidDataException exception)
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            _logger.LogWarning(exception, "Ollama {SuggestionKind} response for model {Model} exceeded the supported size after {ElapsedMilliseconds} ms.", request.Kind, request.Model, (long)elapsed.TotalMilliseconds);
            return WithDiagnostics(new AiProviderGenerationResult(null, AiProviderFailureKind.InvalidResponse, "Ollama returned an oversized response."), normalizedEndpoint, null, elapsed, string.Empty);
        }
    }

    private static AiConnectionResult? ValidateConnectionSettings(AiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.Enabled)
        {
            return new AiConnectionResult(AiAvailabilityState.Disabled, "AI assistance is disabled in Settings.", Array.Empty<AiModel>());
        }

        return !OllamaEndpointNormalizer.TryNormalize(settings.Endpoint, out _) ||
               settings.RequestTimeoutSeconds is < AiSettings.MinimumRequestTimeoutSeconds or > AiSettings.MaximumRequestTimeoutSeconds
            ? new AiConnectionResult(AiAvailabilityState.Unavailable, "The Ollama endpoint or request timeout is invalid.", Array.Empty<AiModel>())
            : null;
    }

    private static AiConnectionResult ConnectionFailure(
        string message,
        Uri endpoint,
        TimeSpan elapsed,
        HttpStatusCode? statusCode = null,
        AiAvailabilityState state = AiAvailabilityState.Unavailable) =>
        new(state, message, Array.Empty<AiModel>())
        {
            NormalizedEndpoint = endpoint.AbsoluteUri.TrimEnd('/'),
            HttpStatusCode = statusCode is null ? null : (int)statusCode,
            Elapsed = elapsed,
        };

    private static AiProviderGenerationResult WithDiagnostics(
        AiProviderGenerationResult result,
        string endpoint,
        HttpStatusCode? statusCode,
        TimeSpan elapsed,
        string rawResponse) => result with
        {
            Diagnostics = new AiProviderRequestDiagnostics(
                endpoint,
                statusCode is null ? null : (int)statusCode,
                elapsed,
                rawResponse.Length,
                Encoding.UTF8.GetByteCount(rawResponse),
                rawResponse),
        };

    private static AiProviderFailureKind MapHttpFailure(HttpStatusCode statusCode, string responseBody)
    {
        if (statusCode == HttpStatusCode.NotFound || responseBody.Contains("model", StringComparison.OrdinalIgnoreCase) && responseBody.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return AiProviderFailureKind.ModelUnavailable;
        }

        return statusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity
            ? AiProviderFailureKind.UnsupportedResponse
            : AiProviderFailureKind.HttpFailure;
    }

    private static string FailureMessage(AiProviderFailureKind failureKind, HttpStatusCode statusCode) => failureKind switch
    {
        AiProviderFailureKind.ModelUnavailable => "The exact configured Ollama model is unavailable. Refresh installed models and select one.",
        AiProviderFailureKind.UnsupportedResponse => "Ollama or the selected model rejected the required JSON response format. Try another installed model.",
        _ => $"Ollama could not complete the request (HTTP {(int)statusCode}).",
    };

    private static bool IsValidModelIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= AiSettings.MaximumModelIdentifierLength &&
        !value.Any(char.IsControl);

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

    private static void Report(AiProviderGenerationRequest request, AiRequestStage stage, string message, long started) =>
        request.Progress?.Report(new AiRequestProgress(stage, message, Stopwatch.GetElapsedTime(started)));

    private static string Bound(string value, int maximumLength) => value.Length <= maximumLength ? value : value[..maximumLength];

    private sealed record ModelsResponse([property: JsonPropertyName("models")] List<ModelResponse>? Models);
    private sealed record ModelResponse([property: JsonPropertyName("name")] string? Name);
    private sealed record VersionResponse([property: JsonPropertyName("version")] string? Version);

    private sealed record GenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("keep_alive")] string KeepAlive,
        [property: JsonPropertyName("options")] GenerateOptions Options);

    private sealed record GenerateOptions([property: JsonPropertyName("temperature")] double Temperature);
    private sealed record GenerateResponse([property: JsonPropertyName("response")] string? Response);
}
