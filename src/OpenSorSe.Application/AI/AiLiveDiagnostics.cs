using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenSorSe.Application.AI;

/// <summary>Identifies the lifecycle state of one diagnostic stage or request.</summary>
public enum AiDiagnosticState
{
    /// <summary>Stage has not started.</summary>
    Pending,
    /// <summary>Stage is running.</summary>
    Active,
    /// <summary>Stage or request completed successfully.</summary>
    Succeeded,
    /// <summary>Response was safely rejected.</summary>
    Rejected,
    /// <summary>Request was cancelled.</summary>
    Cancelled,
    /// <summary>Stage or request failed.</summary>
    Failed,
}

/// <summary>Identifies captured text without coupling the collector to a UI.</summary>
public enum AiDiagnosticContentKind
{
    /// <summary>Final system prompt.</summary>
    SystemPrompt,
    /// <summary>Final user prompt.</summary>
    UserPrompt,
    /// <summary>Serialized HTTP request body.</summary>
    RequestJson,
    /// <summary>Complete HTTP response body.</summary>
    RawHttpResponse,
    /// <summary>Assistant text extracted from the provider envelope.</summary>
    ExtractedAssistantResponse,
    /// <summary>Pretty-printed structured response.</summary>
    ParsedStructuredResponse,
}

/// <summary>Describes one live processing stage.</summary>
public sealed record AiDiagnosticStage(
    string Name,
    AiDiagnosticState State,
    DateTimeOffset TimestampUtc,
    TimeSpan Elapsed,
    string? Error = null);

/// <summary>Describes one explicit structured-response validation check.</summary>
public sealed record AiDiagnosticValidation(
    string PropertyName,
    bool Required,
    string ExpectedType,
    string? AllowedValues,
    string ActualType,
    string ActualValue,
    bool Passed,
    string Message);

/// <summary>Immutable view of one live request retained only for this process session.</summary>
public sealed record AiDiagnosticSession(
    string RequestId,
    AiSuggestionKind OperationType,
    string Model,
    string Endpoint,
    DateTimeOffset StartedAtUtc,
    TimeSpan Elapsed,
    AiDiagnosticState Status,
    int? HttpStatusCode,
    bool WasCancelled,
    int RetryAttempt,
    string ContentType,
    IReadOnlyDictionary<string, string> SafeResponseHeaders,
    int ResponseSizeBytes,
    bool ResponseComplete,
    bool WasStreaming,
    IReadOnlyList<AiDiagnosticStage> Stages,
    string SystemPrompt,
    string UserPrompt,
    string RequestJson,
    string RawHttpResponse,
    string ExtractedAssistantResponse,
    string ParsedStructuredResponse,
    IReadOnlyList<AiDiagnosticValidation> Validation,
    IReadOnlyList<string> Errors);

/// <summary>Raised whenever a live diagnostic session is created or updated.</summary>
public sealed class AiDiagnosticSessionChangedEventArgs(AiDiagnosticSession session, bool isNew) : EventArgs
{
    /// <summary>Gets the latest immutable session snapshot.</summary>
    public AiDiagnosticSession Session { get; } = session;
    /// <summary>Gets whether the session was just created.</summary>
    public bool IsNew { get; } = isNew;
}

/// <summary>Collects observable, bounded, process-memory-only Ollama diagnostics.</summary>
public interface IAiDiagnosticsCollector
{
    /// <summary>Gets whether collection is enabled.</summary>
    bool IsEnabled { get; }
    /// <summary>Gets whether retained display content is exact rather than redacted.</summary>
    bool ShowUnredactedContent { get; }
    /// <summary>Occurs after a session is created or updated.</summary>
    event EventHandler<AiDiagnosticSessionChangedEventArgs>? SessionChanged;
    /// <summary>Applies privacy settings and clears history when disabled.</summary>
    void Configure(bool enabled, bool showUnredactedContent);
    /// <summary>Begins one session and returns its identity, or null when disabled.</summary>
    string? Begin(AiSuggestionKind operationType, string model, string endpoint, int retryAttempt = 1);
    /// <summary>Publishes one stage transition.</summary>
    void ReportStage(string? requestId, string name, AiDiagnosticState state, TimeSpan elapsed, string? error = null);
    /// <summary>Captures one diagnostic artifact.</summary>
    void Capture(string? requestId, AiDiagnosticContentKind kind, string? value);
    /// <summary>Captures safe HTTP transport facts.</summary>
    void SetTransport(string? requestId, int? statusCode, string? contentType, IReadOnlyDictionary<string, string>? safeHeaders, int responseSizeBytes, bool complete, bool streaming);
    /// <summary>Captures parsed content and validation checks.</summary>
    void SetValidation(string? requestId, string? parsedJson, IReadOnlyList<AiDiagnosticValidation> validation, IReadOnlyList<string> errors);
    /// <summary>Completes a session.</summary>
    void Complete(string? requestId, AiDiagnosticState state, bool cancelled, TimeSpan elapsed, string? error = null);
    /// <summary>Gets newest-first session snapshots.</summary>
    IReadOnlyList<AiDiagnosticSession> GetRecent();
    /// <summary>Clears one retained request.</summary>
    void Clear(string requestId);
    /// <summary>Clears all retained requests.</summary>
    void Clear();
}

/// <summary>Creates a failure-isolating publisher facade for optional diagnostics.</summary>
public static class AiDiagnosticsIsolation
{
    /// <summary>Returns a collector facade whose failures cannot escape into an AI operation.</summary>
    public static IAiDiagnosticsCollector? Protect(IAiDiagnosticsCollector? collector) =>
        collector is null ? null : new ProtectedCollector(collector);

    private sealed class ProtectedCollector(IAiDiagnosticsCollector inner) : IAiDiagnosticsCollector
    {
        public bool IsEnabled { get { try { return inner.IsEnabled; } catch { return false; } } }
        public bool ShowUnredactedContent { get { try { return inner.ShowUnredactedContent; } catch { return false; } } }
        public event EventHandler<AiDiagnosticSessionChangedEventArgs>? SessionChanged { add { } remove { } }
        public void Configure(bool enabled, bool showUnredactedContent) => Try(() => inner.Configure(enabled, showUnredactedContent));
        public string? Begin(AiSuggestionKind operationType, string model, string endpoint, int retryAttempt = 1)
        {
            try { return inner.Begin(operationType, model, endpoint, retryAttempt); } catch { return null; }
        }
        public void ReportStage(string? requestId, string name, AiDiagnosticState state, TimeSpan elapsed, string? error = null) =>
            Try(() => inner.ReportStage(requestId, name, state, elapsed, error));
        public void Capture(string? requestId, AiDiagnosticContentKind kind, string? value) => Try(() => inner.Capture(requestId, kind, value));
        public void SetTransport(string? requestId, int? statusCode, string? contentType, IReadOnlyDictionary<string, string>? safeHeaders, int responseSizeBytes, bool complete, bool streaming) =>
            Try(() => inner.SetTransport(requestId, statusCode, contentType, safeHeaders, responseSizeBytes, complete, streaming));
        public void SetValidation(string? requestId, string? parsedJson, IReadOnlyList<AiDiagnosticValidation> validation, IReadOnlyList<string> errors) =>
            Try(() => inner.SetValidation(requestId, parsedJson, validation, errors));
        public void Complete(string? requestId, AiDiagnosticState state, bool cancelled, TimeSpan elapsed, string? error = null) =>
            Try(() => inner.Complete(requestId, state, cancelled, elapsed, error));
        public IReadOnlyList<AiDiagnosticSession> GetRecent() { try { return inner.GetRecent(); } catch { return []; } }
        public void Clear(string requestId) => Try(() => inner.Clear(requestId));
        public void Clear() => Try(inner.Clear);
        private static void Try(Action action) { try { action(); } catch { } }
    }
}

/// <summary>Thread-safe observable implementation; observer failures never escape.</summary>
public sealed partial class AiDiagnosticsCollector : IAiDiagnosticsCollector
{
    private readonly object _sync = new();
    private readonly LinkedList<AiDiagnosticSession> _sessions = [];
    private bool _enabled;
    private bool _unredacted;
    private static readonly string[] ExpectedStages =
    [
        "Preparing file context",
        "Building system prompt",
        "Building user prompt",
        "Serializing Ollama request",
        "Connecting to Ollama",
        "Request sent",
        "Waiting for model",
        "Response headers received",
        "Response body received",
        "Extracting assistant content",
        "Removing permitted formatting wrappers",
        "Parsing structured JSON",
        "Validating response",
        "Completed",
    ];

    /// <inheritdoc />
    public bool IsEnabled { get { lock (_sync) return _enabled; } }
    /// <inheritdoc />
    public bool ShowUnredactedContent { get { lock (_sync) return _unredacted; } }
    /// <inheritdoc />
    public event EventHandler<AiDiagnosticSessionChangedEventArgs>? SessionChanged;

    /// <inheritdoc />
    public void Configure(bool enabled, bool showUnredactedContent)
    {
        lock (_sync)
        {
            _enabled = enabled;
            _unredacted = enabled && showUnredactedContent;
            if (!enabled)
            {
                _sessions.Clear();
            }
        }
    }

    /// <inheritdoc />
    public string? Begin(AiSuggestionKind operationType, string model, string endpoint, int retryAttempt = 1)
    {
        AiDiagnosticSession? session;
        lock (_sync)
        {
            if (!_enabled) return null;
            var started = DateTimeOffset.UtcNow;
            session = new AiDiagnosticSession(
                $"ai-request:{Guid.NewGuid():N}", operationType, model, Redact(endpoint), DateTimeOffset.UtcNow,
                TimeSpan.Zero, AiDiagnosticState.Active, null, false, Math.Max(1, retryAttempt), string.Empty,
                new Dictionary<string, string>(), 0, false, false,
                ExpectedStages.Select(name => new AiDiagnosticStage(name, AiDiagnosticState.Pending, started, TimeSpan.Zero)).ToArray(),
                "", "", "", "", "", "", [], []);
            _sessions.AddFirst(session);
            while (_sessions.Count > AiRequestDiagnosticLimits.MaximumRetainedRequests) _sessions.RemoveLast();
        }
        Publish(session, true);
        return session.RequestId;
    }

    /// <inheritdoc />
    public void ReportStage(string? requestId, string name, AiDiagnosticState state, TimeSpan elapsed, string? error = null) =>
        Update(requestId, current =>
        {
            var stages = current.Stages.ToArray();
            var index = Array.FindIndex(stages, stage => string.Equals(stage.Name, name, StringComparison.Ordinal));
            var update = new AiDiagnosticStage(name, state, DateTimeOffset.UtcNow, elapsed, Safe(error));
            if (index >= 0) stages[index] = update;
            else stages = stages.Concat([update]).TakeLast(100).ToArray();
            return current with { Elapsed = elapsed, Stages = stages };
        });

    /// <inheritdoc />
    public void Capture(string? requestId, AiDiagnosticContentKind kind, string? value)
    {
        var safe = Safe(value);
        Update(requestId, current => kind switch
        {
            AiDiagnosticContentKind.SystemPrompt => current with { SystemPrompt = safe },
            AiDiagnosticContentKind.UserPrompt => current with { UserPrompt = safe },
            AiDiagnosticContentKind.RequestJson => current with { RequestJson = safe },
            AiDiagnosticContentKind.RawHttpResponse => current with { RawHttpResponse = safe },
            AiDiagnosticContentKind.ExtractedAssistantResponse => current with { ExtractedAssistantResponse = safe },
            AiDiagnosticContentKind.ParsedStructuredResponse => current with { ParsedStructuredResponse = safe },
            _ => current,
        });
    }

    /// <inheritdoc />
    public void SetTransport(string? requestId, int? statusCode, string? contentType, IReadOnlyDictionary<string, string>? safeHeaders, int responseSizeBytes, bool complete, bool streaming) =>
        Update(requestId, current => current with
        {
            HttpStatusCode = statusCode,
            ContentType = Bound(contentType, 200),
            SafeResponseHeaders = safeHeaders is null
                ? new Dictionary<string, string>()
                : safeHeaders.ToDictionary(pair => Bound(pair.Key, 100), pair => Safe(Bound(pair.Value, 500))),
            ResponseSizeBytes = Math.Max(0, responseSizeBytes),
            ResponseComplete = complete,
            WasStreaming = streaming,
        });

    /// <inheritdoc />
    public void SetValidation(string? requestId, string? parsedJson, IReadOnlyList<AiDiagnosticValidation> validation, IReadOnlyList<string> errors)
    {
        Capture(requestId, AiDiagnosticContentKind.ParsedStructuredResponse, parsedJson);
        Update(requestId, current => current with
        {
            Validation = validation.Take(100).Select(item => item with
            {
                ActualValue = Safe(Bound(item.ActualValue, 1000)),
                Message = Safe(Bound(item.Message, 1000)),
            }).ToArray(),
            Errors = errors.Select(error => Safe(Bound(error, 1000))).Take(100).ToArray(),
        });
    }

    /// <inheritdoc />
    public void Complete(string? requestId, AiDiagnosticState state, bool cancelled, TimeSpan elapsed, string? error = null) =>
        Update(requestId, current => current with
        {
            Status = state,
            WasCancelled = cancelled,
            Elapsed = elapsed,
            Errors = string.IsNullOrWhiteSpace(error) ? current.Errors : current.Errors.Concat([Safe(error)]).Take(100).ToArray(),
        });

    /// <inheritdoc />
    public IReadOnlyList<AiDiagnosticSession> GetRecent()
    {
        lock (_sync) return _sessions.ToArray();
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_sync) _sessions.Clear();
    }

    /// <inheritdoc />
    public void Clear(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId)) return;
        lock (_sync)
        {
            var node = _sessions.First;
            while (node is not null)
            {
                if (node.Value.RequestId == requestId)
                {
                    _sessions.Remove(node);
                    return;
                }
                node = node.Next;
            }
        }
    }

    private void Update(string? requestId, Func<AiDiagnosticSession, AiDiagnosticSession> update)
    {
        if (requestId is null) return;
        AiDiagnosticSession? changed = null;
        lock (_sync)
        {
            if (!_enabled) return;
            var node = _sessions.First;
            while (node is not null && node.Value.RequestId != requestId) node = node.Next;
            if (node is null) return;
            changed = update(node.Value);
            node.Value = changed;
        }
        Publish(changed, false);
    }

    private void Publish(AiDiagnosticSession session, bool isNew)
    {
        foreach (EventHandler<AiDiagnosticSessionChangedEventArgs> handler in SessionChanged?.GetInvocationList() ?? [])
        {
            try { handler(this, new AiDiagnosticSessionChangedEventArgs(session, isNew)); }
            catch { /* Diagnostics observers must never affect the AI operation. */ }
        }
    }

    private string Safe(string? value) => _unredacted ? value ?? string.Empty : Redact(value);
    private static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var redacted = SensitiveJsonStringRegex().Replace(value, "$1[REDACTED]$3");
        return PathLikeRegex().Replace(redacted, "[REDACTED_PATH]");
    }

    private static string Bound(string? value, int maximum) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= maximum ? value : value[..maximum];

    [GeneratedRegex("(\\\"(?:fileName|currentFileName|displayFileName|sourceFile|relativePath|path|text|metadata)\\\"\\s*:\\s*\\\")([^\\\"]*)(\\\")", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveJsonStringRegex();

    [GeneratedRegex(@"(?:[A-Za-z]:\\|(?<!:)\/(?!\/))[^\s""}]+", RegexOptions.IgnoreCase)]
    private static partial Regex PathLikeRegex();
}

/// <summary>Creates validation diagnostics without changing the authoritative parser result.</summary>
public static class AiDiagnosticValidationInspector
{
    /// <summary>Inspects common contract fields and preserves the parsed JSON for diagnostics.</summary>
    public static (string ParsedJson, IReadOnlyList<AiDiagnosticValidation> Checks) Inspect(string? json, string taskId)
    {
        if (string.IsNullOrWhiteSpace(json)) return ("", [Check("$", true, "object", null, "empty", "", false, "Expected a JSON object, but received an empty response.")]);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var pretty = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            if (root.ValueKind != JsonValueKind.Object)
                return (pretty, [Check("$", true, "object", null, Type(root), root.ToString(), false, $"Expected the root to be an object, but received {Type(root)}.")]);

            var checks = new List<AiDiagnosticValidation>
            {
                StringCheck(root, "taskId", true, taskId),
                StringCheck(root, "status", true, "suggestion, no_suggestion"),
                StringCheck(root, "reason", true, null),
            };
            if (root.TryGetProperty("confidence", out var confidence))
            {
                var valid = confidence.ValueKind is JsonValueKind.Null ||
                    confidence.ValueKind == JsonValueKind.Number && confidence.TryGetDouble(out var number) && number is >= 0 and <= 1;
                checks.Add(Check("confidence", false, "number or null", "0..1", Type(confidence), confidence.ToString(), valid,
                    valid ? "Confidence is valid." : $"Expected `confidence` to be a number from 0 through 1, but received {Type(confidence)} `{confidence}`."));
            }
            return (pretty, checks);
        }
        catch (JsonException exception)
        {
            return ("Parsing failed; original response remains available.", [Check("$", true, "valid JSON object", null, "malformed JSON", json, false, $"JSON parsing failed: {exception.Message}")]);
        }
    }

    private static AiDiagnosticValidation StringCheck(JsonElement root, string name, bool required, string? allowed)
    {
        if (!root.TryGetProperty(name, out var value))
            return Check(name, required, "non-empty string", allowed, "missing", "", false, $"Expected `{name}` to be a non-empty string, but the property was missing.");
        var valid = value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString());
        if (valid && allowed is not null)
            valid = allowed.Split(',', StringSplitOptions.TrimEntries).Contains(value.GetString(), StringComparer.Ordinal);
        var message = valid ? $"{name} is valid." :
            $"Expected `{name}` to be {(allowed is null ? "a non-empty string" : $"one of [{allowed}]")}, but received {Type(value)} `{Bound(value.ToString(), 200)}`.";
        return Check(name, required, "non-empty string", allowed, Type(value), value.ToString(), valid, message);
    }

    private static AiDiagnosticValidation Check(string name, bool required, string expected, string? allowed, string actualType, string actualValue, bool passed, string message) =>
        new(name, required, expected, allowed, actualType, actualValue, passed, message);

    private static string Type(JsonElement element) => element.ValueKind.ToString().ToLowerInvariant();
    private static string Bound(string value, int maximum) => value.Length <= maximum ? value : value[..maximum];
}
