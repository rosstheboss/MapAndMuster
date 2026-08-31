namespace MapAndMuster.Infrastructure.Persistence.Entities;

/// <summary>
/// Persisted campaign faction.
/// </summary>
public sealed class CampaignFactionRecord
{
    /// <summary>Gets or sets the faction identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the campaign identifier.</summary>
    public Guid CampaignId { get; set; }

    /// <summary>Gets or sets the faction name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique faction color as #RRGGBB.</summary>
    public string Color { get; set; } = "#2563EB";

    /// <summary>Gets or sets whether a player who chooses this faction must pick a subfaction.</summary>
    public bool RequiresSubfaction { get; set; }

    /// <summary>Gets or sets the stored custom flag image key.</summary>
    public string? FlagImageStorageKey { get; set; }

    /// <summary>Gets or sets whether an uploaded logo should be tinted with the faction color.</summary>
    public bool TintFlagImage { get; set; }

    /// <summary>Gets or sets the optional ally-group identifier.</summary>
    public Guid? AllyGroupId { get; set; }

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the campaign.</summary>
    public CampaignRecord? Campaign { get; set; }

    /// <summary>Gets or sets the ally group.</summary>
    public CampaignAllyGroupRecord? AllyGroup { get; set; }

    /// <summary>Gets the subfactions.</summary>
    public ICollection<CampaignSubfactionRecord> Subfactions { get; } = [];
}
