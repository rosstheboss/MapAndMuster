using System.ComponentModel.DataAnnotations;

namespace Campaign.Infrastructure.Persistence.Entities;

/// <summary>
/// Persisted campaign aggregate root.
/// </summary>
public sealed class CampaignRecord
{
    /// <summary>Gets or sets the campaign identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the campaign name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the configured player-slot count.</summary>
    public int PlayerSlotCount { get; set; }

    /// <summary>Gets or sets whether a join password is required.</summary>
    public bool IsPrivate { get; set; }

    /// <summary>Gets or sets the hashed join password.</summary>
    public string? JoinPasswordHash { get; set; }

    /// <summary>Gets or sets whether the creating manager occupies a player slot.</summary>
    public bool CreatorIsParticipant { get; set; }

    /// <summary>Gets or sets the generated map storage key.</summary>
    public string? MapStorageKey { get; set; }

    /// <summary>Gets or sets the optimistic concurrency revision.</summary>
    [ConcurrencyCheck]
    public int Revision { get; set; }

    /// <summary>Gets or sets when the campaign was created, in UTC.</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>Gets or sets when the campaign was last edited, in UTC.</summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>Gets or sets the creating user's identifier.</summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>Gets or sets the IANA time zone used when the schedule was configured.</summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>Gets or sets the campaign start instant, in UTC.</summary>
    public DateTimeOffset StartsUtc { get; set; }

    /// <summary>Gets or sets the campaign end instant, in UTC.</summary>
    public DateTimeOffset EndsUtc { get; set; }

    /// <summary>Gets or sets the number of rounds.</summary>
    public int RoundCount { get; set; }

    /// <summary>Gets or sets the round-length amount.</summary>
    public int RoundLengthAmount { get; set; }

    /// <summary>Gets or sets the round-length unit name.</summary>
    public string RoundLengthUnit { get; set; } = string.Empty;

    /// <summary>Gets the memberships.</summary>
    public ICollection<CampaignMembershipRecord> Memberships { get; } = [];

    /// <summary>Gets the factions.</summary>
    public ICollection<CampaignFactionRecord> Factions { get; } = [];

    /// <summary>Gets the ally groups.</summary>
    public ICollection<CampaignAllyGroupRecord> AllyGroups { get; } = [];

    /// <summary>Gets the external links.</summary>
    public ICollection<CampaignLinkRecord> Links { get; } = [];

    /// <summary>Gets the ordered round phases.</summary>
    public ICollection<CampaignRoundPhaseRecord> Phases { get; } = [];
}
