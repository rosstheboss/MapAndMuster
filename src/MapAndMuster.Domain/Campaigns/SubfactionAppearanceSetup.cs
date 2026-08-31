namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Validated color, flag, and logo choices for one named subfaction.
/// </summary>
public sealed class SubfactionAppearanceSetup
{
    /// <summary>
    /// Initializes a validated subfaction appearance.
    /// </summary>
    /// <param name="name">The subfaction name.</param>
    /// <param name="color">The unique color when chosen, otherwise inherit the parent.</param>
    /// <param name="flagSource">Whether the subfaction inherits, uses a color flag, or uses an uploaded logo.</param>
    /// <param name="clearFlagImage">Whether an existing uploaded logo should be removed.</param>
    /// <param name="tintFlagImage">Whether an uploaded logo should be tinted with the resolved color.</param>
    public SubfactionAppearanceSetup(
        string name,
        string? color,
        string flagSource,
        bool clearFlagImage,
        bool tintFlagImage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagSource);
        Name = name;
        Color = color;
        FlagSource = flagSource;
        ClearFlagImage = clearFlagImage;
        TintFlagImage = tintFlagImage;
    }

    /// <summary>Gets the subfaction name.</summary>
    public string Name { get; }

    /// <summary>Gets the unique color when chosen, otherwise inherit the parent.</summary>
    public string? Color { get; }

    /// <summary>Gets whether the subfaction inherits, uses a color flag, or uses an uploaded logo.</summary>
    public string FlagSource { get; }

    /// <summary>Gets whether an existing uploaded logo should be removed.</summary>
    public bool ClearFlagImage { get; }

    /// <summary>Gets whether an uploaded logo should be tinted with the resolved color.</summary>
    public bool TintFlagImage { get; }
}

/// <summary>
/// User-supplied color, flag, and logo choices for one named subfaction.
/// </summary>
public sealed class SubfactionAppearanceInput
{
    /// <summary>Gets the subfaction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique color when chosen, otherwise inherit the parent.</summary>
    public string? Color { get; init; }

    /// <summary>Gets whether the subfaction inherits, uses a color flag, or uses an uploaded logo.</summary>
    public string? FlagSource { get; init; }

    /// <summary>Gets whether an existing uploaded logo should be removed.</summary>
    public bool ClearFlagImage { get; init; }

    /// <summary>Gets whether an uploaded logo should be tinted with the resolved color.</summary>
    public bool TintFlagImage { get; init; }
}
