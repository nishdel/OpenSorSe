using System.Globalization;

namespace OpenSorSe.Application.Semantic;

/// <summary>Creates deterministic 256-dimensional local feature-hashing vectors.</summary>
public sealed class FeatureHashingEmbeddingProvider : IEmbeddingProvider
{
    /// <inheritdoc />
    public int Dimensions => 256;

    /// <inheritdoc />
    public IReadOnlyList<float> Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (var token in SemanticTokenizer.Tokenize(text, 512))
        {
            var hash = StableHash(token);
            var index = (int)(hash % (uint)Dimensions);
            var sign = (hash & 0x80000000) == 0 ? 1f : -1f;
            vector[index] += sign;
        }

        var magnitude = Math.Sqrt(vector.Sum(value => value * value));
        if (magnitude > 0)
        {
            for (var index = 0; index < vector.Length; index++)
            {
                vector[index] = (float)(vector[index] / magnitude);
            }
        }

        return Array.AsReadOnly(vector);
    }

    private static uint StableHash(string value)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var hash = offset;
        foreach (var character in value.Normalize(System.Text.NormalizationForm.FormKC))
        {
            hash ^= char.ToLower(character, CultureInfo.InvariantCulture);
            hash *= prime;
        }

        return hash;
    }
}

internal static class SemanticTokenizer
{
    public static IReadOnlyList<string> Tokenize(string? text, int maximumTokens) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : Array.AsReadOnly(text
                .Split(
                    [' ', '\t', '\r', '\n', '/', '\\', '-', '_', '.', ',', ';', ':', '(', ')', '[', ']'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(token => token.Trim().ToLowerInvariant())
                .Where(token => token.Length is > 1 and <= 64)
                .Take(maximumTokens)
                .ToArray());
}
