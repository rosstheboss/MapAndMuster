namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// How a subfaction chooses its map flag or logo.
/// </summary>
public static class SubfactionFlagSource
{
    /// <summary>Use the parent faction's flag or logo.</summary>
    public const string Inherit = "inherit";

    /// <summary>Use the default color flag in the resolved subfaction or parent color.</summary>
    public const string Color = "color";

    /// <summary>Use an uploaded logo, falling back to the parent logo when none is stored.</summary>
    public const string Image = "image";

    /// <summary>
    /// Returns whether <paramref name="value"/> is a recognized flag source.
    /// </summary>
    /// <param name="value">The stored or requested source.</param>
    /// <returns>True when the value is inherit, color, or image.</returns>
    public static bool IsDefined(string? value)
    {
        return string.Equals(value, Inherit, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, Color, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, Image, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the canonical source, or inherit when the value is empty.
    /// </summary>
    /// <param name="value">The stored or requested source.</param>
    /// <returns>The canonical source, or null when the value is unrecognized.</returns>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Inherit;
        }

        if (string.Equals(value, Inherit, StringComparison.OrdinalIgnoreCase))
        {
            return Inherit;
        }

        if (string.Equals(value, Color, StringComparison.OrdinalIgnoreCase))
        {
            return Color;
        }

        if (string.Equals(value, Image, StringComparison.OrdinalIgnoreCase))
        {
            return Image;
        }

        return null;
    }
}
