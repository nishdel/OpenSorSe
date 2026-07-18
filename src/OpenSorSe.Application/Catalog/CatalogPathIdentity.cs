namespace OpenSorSe.Application.Catalog;

/// <summary>
/// Produces platform-neutral identities for historical catalog paths without accessing the filesystem.
/// </summary>
public static class CatalogPathIdentity
{
    /// <summary>Determines whether a historical path is absolute without consulting the current host filesystem.</summary>
    /// <param name="path">The historical path to validate.</param>
    /// <returns><see langword="true"/> for Unix-rooted, drive-rooted, or UNC-style values.</returns>
    public static bool IsAbsolute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Trim().Replace('\\', '/');
        return normalized.StartsWith("/", StringComparison.Ordinal) ||
               (normalized.Length >= 3 && char.IsAsciiLetter(normalized[0]) && normalized[1] == ':' && normalized[2] == '/');
    }

    /// <summary>
    /// Normalizes separators and trailing delimiters, preserving Unix case while folding Windows drive and UNC paths.
    /// </summary>
    /// <param name="path">The non-empty historical path string.</param>
    /// <returns>A deterministic identity suitable for catalog comparisons and duplicate checks.</returns>
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalized = path.Trim().Replace('\\', '/');
        if (normalized.Length > 1)
        {
            normalized = normalized.TrimEnd('/');
        }

        var isWindowsStyle = normalized.StartsWith("//", StringComparison.Ordinal) ||
                             (normalized.Length >= 2 && char.IsAsciiLetter(normalized[0]) && normalized[1] == ':');
        return isWindowsStyle ? normalized.ToLowerInvariant() : normalized;
    }
}
