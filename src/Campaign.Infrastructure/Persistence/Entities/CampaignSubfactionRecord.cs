namespace Campaign.Infrastructure.Persistence.Entities;

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

    /// <summary>Gets or sets the faction.</summary>
    public CampaignFactionRecord? Faction { get; set; }
}
