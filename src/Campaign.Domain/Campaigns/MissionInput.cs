namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied mission configuration nested under a terrain type or structure.
/// </summary>
public sealed class MissionInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the mission name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets an optional http or https link for the mission.</summary>
    public string? Url { get; init; }

    /// <summary>Gets whether an existing uploaded file should be removed.</summary>
    public bool ClearFile { get; init; }

    /// <summary>Gets questions asked when reporting this mission's battle result.</summary>
    public IReadOnlyList<MissionResultQuestionInput>? ResultQuestions { get; init; }

    /// <summary>Gets whether this mission is used for attacker/defender engagements.</summary>
    public bool IsAttackerDefender { get; init; }

    /// <summary>Gets whether attacker or defender army points are adjusted.</summary>
    public bool HasArmyPointsAdvantage { get; init; }

    /// <summary>Gets Attacker or Defender for the army-point adjustment.</summary>
    public string? ArmyPointsAdvantageSide { get; init; }

    /// <summary>Gets whether the army-point amount is a percent of the cap.</summary>
    public bool ArmyPointsAdvantageIsPercent { get; init; }

    /// <summary>Gets the signed army-point number or percent change.</summary>
    public int ArmyPointsAdvantageAmount { get; init; }

    /// <summary>Gets whether attacker or defender supply points are adjusted.</summary>
    public bool HasSupplyPointsAdvantage { get; init; }

    /// <summary>Gets Attacker or Defender for the supply-point adjustment.</summary>
    public string? SupplyPointsAdvantageSide { get; init; }

    /// <summary>Gets the signed raw supply-point change.</summary>
    public int SupplyPointsAdvantageAmount { get; init; }
}
