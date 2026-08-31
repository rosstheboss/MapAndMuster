namespace MapAndMuster.Infrastructure.Persistence.Entities;

/// <summary>
/// Persisted optional subfaction belonging to a faction.
/// </summary>
public sealed class CampaignSubfactionRecord
{
    /// <summary>Gets or sets the subfaction identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the faction identifier.</summary>
    public Guid FactionId { get; set; }

    /// <summary>Gets or sets the subfaction name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the unique color when chosen, otherwise inherit the parent.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets whether the subfaction inherits, uses a color flag, or uses an uploaded logo.</summary>
    public string FlagSource { get; set; } = "inherit";

    /// <summary>Gets or sets the stored logo key, when a custom logo was uploaded.</summary>
    public string? FlagImageStorageKey { get; set; }

    /// <summary>Gets or sets whether an uploaded logo should be tinted with the resolved color.</summary>
    public bool TintFlagImage { get; set; }

    /// <summary>Gets or sets the faction.</summary>
    public CampaignFactionRecord? Faction { get; set; }
}
