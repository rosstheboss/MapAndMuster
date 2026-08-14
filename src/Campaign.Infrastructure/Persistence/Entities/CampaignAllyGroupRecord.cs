namespace Campaign.Infrastructure.Persistence.Entities;

/// <summary>
/// Persisted ally group that factions may join.
/// </summary>
public sealed class CampaignAllyGroupRecord
{
    /// <summary>Gets or sets the ally-group identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the campaign identifier.</summary>
    public Guid CampaignId { get; set; }

    /// <summary>Gets or sets the group name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the campaign.</summary>
    public CampaignRecord? Campaign { get; set; }

    /// <summary>Gets the member factions.</summary>
    public ICollection<CampaignFactionRecord> Factions { get; } = [];
}
