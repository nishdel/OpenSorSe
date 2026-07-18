namespace OpenSorSe.AI;

/// <summary>
/// Defines fixed memory and presentation bounds for the optional Ollama-compatible transport.
/// </summary>
public static class OllamaTransportLimits
{
    /// <summary>Gets the maximum UTF-8 request prompt size accepted by the provider boundary.</summary>
    public const int MaximumPromptBytes = 128 * 1024;

    /// <summary>Gets the maximum response body read from a configured provider endpoint.</summary>
    public const int MaximumResponseBytes = 1024 * 1024;

    /// <summary>Gets the maximum number of validated model identifiers published to the UI.</summary>
    public const int MaximumPublishedModelCount = 100;
}
