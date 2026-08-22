namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Unvalidated round schedule collected during campaign setup.
/// </summary>
public sealed record CampaignScheduleInput
{
    /// <summary>Gets the IANA time zone used to interpret the start wall-clock time.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets the start date and time in the campaign time zone, without an offset.</summary>
    public string? StartsAtLocal { get; init; }

    /// <summary>Gets the number of rounds.</summary>
    public int RoundCount { get; init; }

    /// <summary>Gets the round-length amount.</summary>
    public int RoundLengthAmount { get; init; }

    /// <summary>Gets the round-length unit name.</summary>
    public string? RoundLengthUnit { get; init; }

    /// <summary>Gets the ordered action and battle steps that make up one round.</summary>
    public IReadOnlyList<RoundPhaseInput>? Phases { get; init; }

    /// <summary>Gets per-round army size, free supply, and free characters. Omitted values use Hunt in Estalia defaults.</summary>
    public IReadOnlyList<RoundArmyEscalationInput>? RoundEscalations { get; init; }
}
