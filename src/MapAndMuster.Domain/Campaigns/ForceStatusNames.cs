namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Canonical force-status names the engine matches. Catalog rows still supply display text.
/// </summary>
public static class ForceStatusNames
{
    /// <summary>The named disease status. Normal is the absence of a status and is not listed.</summary>
    public const string Diseased = "Diseased";

    /// <summary>Returns whether <paramref name="statusName"/> is the named disease status.</summary>
    public static bool IsDiseased(string? statusName)
    {
        return string.Equals(statusName, Diseased, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns whether <paramref name="statusName"/> is Normal (stored as no status).</summary>
    public static bool IsNormal(string? statusName)
    {
        return string.IsNullOrWhiteSpace(statusName)
            || string.Equals(statusName.Trim(), "Normal", StringComparison.OrdinalIgnoreCase);
    }
}
