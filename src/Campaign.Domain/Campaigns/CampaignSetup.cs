namespace Campaign.Domain.Campaigns;

/// <summary>
/// Validated campaign configuration collected during setup or a later manager edit.
/// </summary>
public sealed class CampaignSetup
{
    /// <summary>
    /// Initializes a validated campaign setup.
    /// </summary>
    /// <param name="name">The campaign name.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="playerSlotCount">The configured player-slot count.</param>
    /// <param name="isPrivate">Whether a join password is required.</param>
    /// <param name="isPubliclyViewable">Whether non-members may view the campaign.</param>
    /// <param name="creatorIsParticipant">Whether the creating manager also occupies a player slot.</param>
    /// <param name="city">The optional city.</param>
    /// <param name="region">The optional state, province, or region.</param>
    /// <param name="country">The optional country.</param>
    /// <param name="factions">The factions.</param>
    /// <param name="allyGroups">The ally groups.</param>
    /// <param name="links">The external links.</param>
    /// <param name="terrainTypes">The terrain types.</param>
    /// <param name="structureTypes">The structure types.</param>
    /// <param name="itemObjectiveTypes">The item objective types. Empty means none.</param>
    /// <param name="schedule">The validated round schedule.</param>
    /// <param name="publicObjectiveTypes">The public campaign objectives. Empty means none.</param>
    /// <param name="battleScoring">Conversion from resolved battles into campaign points.</param>
    /// <param name="rankingObjectivePoints">Campaign points for built-in ranking public objectives.</param>
    public CampaignSetup(
        string name,
        string? description,
        int playerSlotCount,
        bool isPrivate,
        bool isPubliclyViewable,
        bool creatorIsParticipant,
        string? city,
        string? region,
        string? country,
        IReadOnlyList<FactionSetup> factions,
        IReadOnlyList<AllyGroupSetup> allyGroups,
        IReadOnlyList<CampaignExternalLink> links,
        IReadOnlyList<TerrainTypeSetup> terrainTypes,
        IReadOnlyList<StructureTypeSetup> structureTypes,
        IReadOnlyList<ItemObjectiveTypeSetup> itemObjectiveTypes,
        CampaignSchedule schedule,
        IReadOnlyList<PublicObjectiveTypeSetup>? publicObjectiveTypes = null,
        BattleScoringSetup? battleScoring = null,
        GeneralPublicObjectivePoints? rankingObjectivePoints = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factions);
        ArgumentNullException.ThrowIfNull(allyGroups);
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(terrainTypes);
        ArgumentNullException.ThrowIfNull(structureTypes);
        ArgumentNullException.ThrowIfNull(itemObjectiveTypes);
        ArgumentNullException.ThrowIfNull(schedule);
        Name = name;
        Description = description;
        PlayerSlotCount = playerSlotCount;
        IsPrivate = isPrivate;
        IsPubliclyViewable = isPubliclyViewable;
        CreatorIsParticipant = creatorIsParticipant;
        City = city;
        Region = region;
        Country = country;
        Factions = factions;
        AllyGroups = allyGroups;
        Links = links;
        TerrainTypes = terrainTypes;
        StructureTypes = structureTypes;
        ItemObjectiveTypes = itemObjectiveTypes;
        Schedule = schedule;
        PublicObjectiveTypes = publicObjectiveTypes ?? [];
        BattleScoring = battleScoring ?? BattleScoringSetup.Default;
        RankingObjectivePoints = rankingObjectivePoints ?? GeneralPublicObjectivePoints.None;
    }

    /// <summary>Gets the campaign name.</summary>
    public string Name { get; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; }

    /// <summary>Gets the configured player-slot count.</summary>
    public int PlayerSlotCount { get; }

    /// <summary>Gets a value indicating whether a join password is required.</summary>
    public bool IsPrivate { get; }

    /// <summary>Gets a value indicating whether non-members may view the campaign.</summary>
    public bool IsPubliclyViewable { get; }

    /// <summary>Gets a value indicating whether the creating manager also occupies a player slot.</summary>
    public bool CreatorIsParticipant { get; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; }

    /// <summary>Gets the factions.</summary>
    public IReadOnlyList<FactionSetup> Factions { get; }

    /// <summary>Gets the ally groups.</summary>
    public IReadOnlyList<AllyGroupSetup> AllyGroups { get; }

    /// <summary>Gets the external links.</summary>
    public IReadOnlyList<CampaignExternalLink> Links { get; }

    /// <summary>Gets the terrain types.</summary>
    public IReadOnlyList<TerrainTypeSetup> TerrainTypes { get; }

    /// <summary>Gets the structure types.</summary>
    public IReadOnlyList<StructureTypeSetup> StructureTypes { get; }

    /// <summary>Gets the item objective types. Empty means the campaign has none.</summary>
    public IReadOnlyList<ItemObjectiveTypeSetup> ItemObjectiveTypes { get; }

    /// <summary>Gets the public campaign objectives. Empty means none.</summary>
    public IReadOnlyList<PublicObjectiveTypeSetup> PublicObjectiveTypes { get; }

    /// <summary>Gets conversion from resolved battles into campaign points.</summary>
    public BattleScoringSetup BattleScoring { get; }

    /// <summary>Gets campaign points for the built-in ranking public objectives.</summary>
    public GeneralPublicObjectivePoints RankingObjectivePoints { get; }

    /// <summary>Gets campaign points awarded to the winner when differential scoring is off.</summary>
    public int PointsPerBattleWon => BattleScoring.PointsPerWin;

    /// <summary>Gets the validated round schedule.</summary>
    public CampaignSchedule Schedule { get; }
}
