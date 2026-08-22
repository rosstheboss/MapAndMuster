namespace MapAndMuster.Infrastructure.Persistence.Entities;

/// <summary>
/// Persisted campaign membership for one user.
/// </summary>
public sealed class CampaignMembershipRecord
{
    /// <summary>Gets or sets the membership identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the campaign identifier.</summary>
    public Guid CampaignId { get; set; }

    /// <summary>Gets or sets the member's user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets whether the member is a campaign manager.</summary>
    public bool IsGameMaster { get; set; }

    /// <summary>Gets or sets whether the member occupies a player slot.</summary>
    public bool IsPlayer { get; set; }

    /// <summary>Gets or sets the chosen faction identifier.</summary>
    public Guid? FactionId { get; set; }

    /// <summary>Gets or sets the chosen subfaction name.</summary>
    public string? Subfaction { get; set; }

    /// <summary>Gets or sets the campaign.</summary>
    public CampaignRecord? Campaign { get; set; }
}
