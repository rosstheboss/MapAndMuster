namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated mission in the campaign catalog, optionally attached to terrain or structures.
/// </summary>
public sealed class MissionSetup
{
    /// <summary>
    /// Initializes a validated mission.
    /// </summary>
    /// <param name="id">The mission identifier.</param>
    /// <param name="name">The mission name.</param>
    /// <param name="url">The optional http or https link.</param>
    /// <param name="clearFile">Whether an existing uploaded file should be removed.</param>
    /// <param name="resultQuestions">Questions asked when reporting this mission's battle result.</param>
    /// <param name="isAttackerDefender">Whether this mission is used for attacker/defender engagements.</param>
    /// <param name="hasArmyPointsAdvantage">Whether attacker or defender army points are adjusted.</param>
    /// <param name="armyPointsAdvantageSide">Which role receives the army-point adjustment.</param>
    /// <param name="armyPointsAdvantageIsPercent">Whether the army-point amount is a percent of the cap.</param>
    /// <param name="armyPointsAdvantageAmount">Signed army-point number or percent change.</param>
    /// <param name="hasSupplyPointsAdvantage">Whether attacker or defender supply points are adjusted.</param>
    /// <param name="supplyPointsAdvantageSide">Which role receives the supply-point adjustment.</param>
    /// <param name="supplyPointsAdvantageAmount">Signed raw supply-point change.</param>
    public MissionSetup(
        Guid id,
        string name,
        string? url,
        bool clearFile,
        IReadOnlyList<MissionResultQuestionSetup>? resultQuestions = null,
        bool isAttackerDefender = false,
        bool hasArmyPointsAdvantage = false,
        MissionAdvantageSide armyPointsAdvantageSide = MissionAdvantageSide.Defender,
        bool armyPointsAdvantageIsPercent = false,
        int armyPointsAdvantageAmount = 0,
        bool hasSupplyPointsAdvantage = false,
        MissionAdvantageSide supplyPointsAdvantageSide = MissionAdvantageSide.Defender,
        int supplyPointsAdvantageAmount = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
        Url = url;
        ClearFile = clearFile;
        ResultQuestions = resultQuestions ?? [];
        IsAttackerDefender = isAttackerDefender;
        HasArmyPointsAdvantage = hasArmyPointsAdvantage;
        ArmyPointsAdvantageSide = armyPointsAdvantageSide;
        ArmyPointsAdvantageIsPercent = armyPointsAdvantageIsPercent;
        ArmyPointsAdvantageAmount = armyPointsAdvantageAmount;
        HasSupplyPointsAdvantage = hasSupplyPointsAdvantage;
        SupplyPointsAdvantageSide = supplyPointsAdvantageSide;
        SupplyPointsAdvantageAmount = supplyPointsAdvantageAmount;
    }

    /// <summary>Gets the mission identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the mission name.</summary>
    public string Name { get; }

    /// <summary>Gets the optional http or https link.</summary>
    public string? Url { get; }

    /// <summary>Gets whether an existing uploaded file should be removed.</summary>
    public bool ClearFile { get; }

    /// <summary>Gets questions asked when reporting this mission's battle result.</summary>
    public IReadOnlyList<MissionResultQuestionSetup> ResultQuestions { get; }

    /// <summary>Gets whether this mission is used for attacker/defender engagements.</summary>
    public bool IsAttackerDefender { get; }

    /// <summary>Gets whether attacker or defender army points are adjusted.</summary>
    public bool HasArmyPointsAdvantage { get; }

    /// <summary>Gets which role receives the army-point adjustment.</summary>
    public MissionAdvantageSide ArmyPointsAdvantageSide { get; }

    /// <summary>Gets whether the army-point amount is a percent of the cap.</summary>
    public bool ArmyPointsAdvantageIsPercent { get; }

    /// <summary>Gets the signed army-point number or percent change.</summary>
    public int ArmyPointsAdvantageAmount { get; }

    /// <summary>Gets whether attacker or defender supply points are adjusted.</summary>
    public bool HasSupplyPointsAdvantage { get; }

    /// <summary>Gets which role receives the supply-point adjustment.</summary>
    public MissionAdvantageSide SupplyPointsAdvantageSide { get; }

    /// <summary>Gets the signed raw supply-point change.</summary>
    public int SupplyPointsAdvantageAmount { get; }
}
