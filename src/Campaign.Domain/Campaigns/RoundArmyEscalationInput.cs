namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied per-round army escalation.
/// </summary>
public sealed class RoundArmyEscalationInput
{
    /// <summary>Gets the 1-based round. Omitted values are filled in order.</summary>
    public int? RoundNumber { get; init; }

    /// <summary>Gets the maximum army points size for the round.</summary>
    public int? MaxArmyPoints { get; init; }

    /// <summary>Gets free supply points granted this round.</summary>
    public int? FreeSupplyPoints { get; init; }

    /// <summary>Gets how many characters have a free base cost against supply.</summary>
    public int? FreeCharacterCount { get; init; }
}
