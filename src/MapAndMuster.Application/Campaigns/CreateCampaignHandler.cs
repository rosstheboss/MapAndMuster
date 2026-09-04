using MapAndMuster.Application.Common;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Creates a campaign and records the caller as its manager, optionally occupying a player slot.
/// </summary>
public sealed class CreateCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly ISecretHasher _secrets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="secrets">The secret hasher.</param>
    public CreateCampaignHandler(ICampaignStore campaigns, IClock clock, ISecretHasher secrets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(secrets);
        _campaigns = campaigns;
        _clock = clock;
        _secrets = secrets;
    }

    /// <summary>
    /// Creates a campaign from validated setup input.
    /// </summary>
    /// <param name="command">The create command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        CreateCampaignCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!CampaignSetupRules.TryCreate(
                command.Name,
                command.Description,
                command.PlayerCount,
                command.IsPrivate,
                command.JoinPassword,
                joinPasswordRequired: command.IsPrivate,
                command.CreatorIsParticipant,
                occupiedPlayerSlotsExcludingCreator: 0,
                command.Factions,
                command.AllyGroups,
                command.Links,
                command.Schedule,
                command.TerrainTypes,
                command.StructureTypes,
                out var setup,
                out var joinPassword,
                out var errors,
                command.IsPubliclyViewable,
                command.City,
                command.Region,
                command.Country,
                command.ItemObjectiveTypes,
                command.PublicObjectiveTypes,
                command.PointsPerBattleWon,
                command.PointsPerBattleDraw,
                command.UseDifferentialBattleScoring,
                command.DifferentialMultiplier,
                command.DifferentialMinimum,
                command.DifferentialMaximum,
                command.AllowNegativeDifferential,
                command.MostTerritoriesCampaignPoints,
                command.LongestTerritoryChainCampaignPoints,
                command.MostBattlesWonCampaignPoints,
                command.MostStructurePointsCampaignPoints,
                command.PointsPerTerritoryCampaignPoints,
                command.AlliedRelicControlCampaignPoints,
                command.SpecialRules,
                command.PrivateObjectiveTypes,
                command.ForceStatuses,
                command.SplitForceSupplyPenaltyPercent,
                command.SplitForceSupplyPenaltyIsPercent,
                command.AlwaysAskGeneralKill,
                command.AlwaysAskSupplyLineDestroyed,
                command.GeneralKillCampaignPoints,
                command.SupplyLineDestroyedCampaignPoints,
                command.Missions))
        {
            return OperationResults.Failure<CampaignDetail>(errors);
        }

        var now = _clock.UtcNow;
        var stored = CampaignPersistenceFactory.FromSetup(
            Guid.NewGuid(),
            setup,
            joinPasswordHash: joinPassword is null ? null : _secrets.Hash(joinPassword),
            mapStorageKey: null,
            revision: 1,
            createdUtc: now,
            updatedUtc: now,
            createdByUserId: command.UserId);

        var created = await _campaigns.AddAsync(stored, cancellationToken).ConfigureAwait(false);
        return OperationResults.Success(CampaignMapper.ToDetail(created, command.UserId, now));
    }
}

/// <summary>
/// Replaces campaign setup for a manager. Original memberships other than the creator's player flag are preserved.
/// </summary>
public sealed class UpdateCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly ISecretHasher _secrets;
    private readonly ICampaignAssetStorage _assets;
    private readonly ICampaignPresetStore? _presets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="secrets">The secret hasher.</param>
    /// <param name="assets">The catalog file storage.</param>
    /// <param name="presets">The campaign-preset store used to keep shared logos.</param>
    public UpdateCampaignHandler(
        ICampaignStore campaigns,
        IClock clock,
        ISecretHasher secrets,
        ICampaignAssetStorage assets,
        ICampaignPresetStore? presets = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(secrets);
        ArgumentNullException.ThrowIfNull(assets);
        _campaigns = campaigns;
        _clock = clock;
        _secrets = secrets;
        _assets = assets;
        _presets = presets;
    }

    /// <summary>
    /// Updates campaign setup when the caller is a manager and the revision matches.
    /// </summary>
    /// <param name="command">The update command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        UpdateCampaignCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        var membership = existing is null ? null : CampaignMapper.MembershipFor(existing, command.UserId);
        if (existing is null || membership is null)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!membership.IsGameMaster)
        {
            return OperationResults.Failure<CampaignDetail>(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager can change this campaign.");
        }

        if (CampaignLifecycle.HasLaunched(existing, _clock.UtcNow))
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignLocked, CampaignLifecycle.LockedMessage);
        }

        var occupiedExcludingCreator = existing.Memberships.Count(member =>
            member.IsPlayer && member.UserId != existing.CreatedByUserId);

        if (!CampaignSetupRules.TryCreate(
                command.Name,
                command.Description,
                command.PlayerCount,
                command.IsPrivate,
                command.JoinPassword,
                joinPasswordRequired: command.IsPrivate && string.IsNullOrWhiteSpace(existing.JoinPasswordHash),
                command.CreatorIsParticipant,
                occupiedExcludingCreator,
                command.Factions,
                command.AllyGroups,
                command.Links,
                command.Schedule,
                command.TerrainTypes,
                command.StructureTypes,
                out var setup,
                out var joinPassword,
                out var errors,
                command.IsPubliclyViewable,
                command.City,
                command.Region,
                command.Country,
                command.ItemObjectiveTypes,
                command.PublicObjectiveTypes,
                command.PointsPerBattleWon,
                command.PointsPerBattleDraw,
                command.UseDifferentialBattleScoring,
                command.DifferentialMultiplier,
                command.DifferentialMinimum,
                command.DifferentialMaximum,
                command.AllowNegativeDifferential,
                command.MostTerritoriesCampaignPoints,
                command.LongestTerritoryChainCampaignPoints,
                command.MostBattlesWonCampaignPoints,
                command.MostStructurePointsCampaignPoints,
                command.PointsPerTerritoryCampaignPoints,
                command.AlliedRelicControlCampaignPoints,
                command.SpecialRules,
                command.PrivateObjectiveTypes,
                command.ForceStatuses,
                command.SplitForceSupplyPenaltyPercent,
                command.SplitForceSupplyPenaltyIsPercent,
                command.AlwaysAskGeneralKill,
                command.AlwaysAskSupplyLineDestroyed,
                command.GeneralKillCampaignPoints,
                command.SupplyLineDestroyedCampaignPoints,
                command.Missions))
        {
            return OperationResults.Failure<CampaignDetail>(errors);
        }

        var joinPasswordHash = existing.JoinPasswordHash;
        if (!setup.IsPrivate)
        {
            joinPasswordHash = null;
        }
        else if (joinPassword is not null)
        {
            joinPasswordHash = _secrets.Hash(joinPassword);
        }

        var memberships = existing.Memberships
            .Select(member => member.UserId == existing.CreatedByUserId
                ? new StoredCampaignMembership
                {
                    UserId = member.UserId,
                    IsGameMaster = true,
                    IsPlayer = setup.CreatorIsParticipant,
                    FactionId = member.FactionId,
                    Subfaction = member.Subfaction,
                }
                : member)
            .ToArray();

        if (memberships.All(member => member.UserId != existing.CreatedByUserId))
        {
            memberships =
            [
                .. memberships,
                new StoredCampaignMembership
                {
                    UserId = existing.CreatedByUserId,
                    IsGameMaster = true,
                    IsPlayer = setup.CreatorIsParticipant,
                },
            ];
        }

        var updated = CampaignPersistenceFactory.FromSetup(
            existing.Id,
            setup,
            joinPasswordHash,
            existing.MapStorageKey,
            existing.Revision,
            existing.CreatedUtc,
            _clock.UtcNow,
            existing.CreatedByUserId,
            memberships,
            existing.MapGraph,
            existing.TerrainTypes,
            existing.StructureTypes,
            existing.Factions,
            existing.ItemObjectiveTypes,
            existing.Missions);

        var outcome = await _campaigns
            .UpdateAsync(updated, command.ExpectedRevision, cancellationToken)
            .ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The campaign could not be updated.");
        }

        foreach (var key in CatalogFileBinder.UnusedStorageKeys(existing, outcome.Campaign))
        {
            await CampaignAssetRetention.DeleteIfUnreferencedAsync(
                _campaigns,
                _assets.DeleteAsync,
                key,
                existing.Id,
                cancellationToken,
                _presets).ConfigureAwait(false);
        }

        return OperationResults.Success(CampaignMapper.ToDetail(outcome.Campaign, command.UserId, _clock.UtcNow));
    }
}

/// <summary>
/// Builds a stored campaign from validated setup.
/// </summary>
internal static class CampaignPersistenceFactory
{
    public static StoredCampaign FromSetup(
        Guid campaignId,
        CampaignSetup setup,
        string? joinPasswordHash,
        string? mapStorageKey,
        int revision,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc,
        Guid createdByUserId,
        IReadOnlyList<StoredCampaignMembership>? memberships = null,
        StoredMapGraph? mapGraph = null,
        IReadOnlyList<StoredTerrainType>? previousTerrainTypes = null,
        IReadOnlyList<StoredStructureType>? previousStructureTypes = null,
        IReadOnlyList<StoredFaction>? previousFactions = null,
        IReadOnlyList<StoredItemObjectiveType>? previousItemObjectiveTypes = null,
        IReadOnlyList<StoredMission>? previousMissions = null)
    {
        var mintNewCatalogIds = previousFactions is null;
        var allyIdMap = new Dictionary<Guid, Guid>();
        var allyGroups = setup.AllyGroups
            .Select(group =>
            {
                var id = mintNewCatalogIds ? Guid.NewGuid() : group.Id;
                if (mintNewCatalogIds)
                {
                    allyIdMap[group.Id] = id;
                }

                return new StoredAllyGroup
                {
                    Id = id,
                    Name = group.Name,
                    Color = group.Color,
                };
            })
            .ToArray();

        var factions = CatalogFileBinder.BindFactions(setup.Factions, previousFactions);
        var factionIdMap = new Dictionary<Guid, Guid>();
        if (mintNewCatalogIds)
        {
            factions =
            [
                .. factions.Select(faction =>
                {
                    var id = Guid.NewGuid();
                    factionIdMap[faction.Id] = id;
                    return new StoredFaction
                    {
                        Id = id,
                        Name = faction.Name,
                        Color = faction.Color,
                        Subfactions = faction.Subfactions,
                        SubfactionAppearances = faction.SubfactionAppearances,
                        AllyGroupName = faction.AllyGroupName,
                        RequiresSubfaction = faction.RequiresSubfaction,
                        FlagImageStorageKey = faction.FlagImageStorageKey,
                        TintFlagImage = faction.TintFlagImage,
                        SpecialRuleIds = faction.SpecialRuleIds,
                        SubfactionSpecialRules = faction.SubfactionSpecialRules,
                    };
                }),
            ];
        }

        var privateObjectiveTypes = CatalogFileBinder.BindPrivateObjectives(setup.PrivateObjectiveTypes);
        if (mintNewCatalogIds)
        {
            privateObjectiveTypes =
            [
                .. privateObjectiveTypes.Select(type => new StoredPrivateObjectiveType
                {
                    Id = type.Id,
                    Name = type.Name,
                    Description = type.Description,
                    CampaignPoints = type.CampaignPoints,
                    AllowedHolderKinds = type.AllowedHolderKinds,
                    ScoringKind = type.ScoringKind,
                    AutomaticKind = type.AutomaticKind,
                    RequiredCount = type.RequiredCount,
                    StructureTypeId = type.StructureTypeId,
                    TerritoryIds = type.TerritoryIds,
                    MatchesAnyStructureType = type.MatchesAnyStructureType,
                    ItemObjectiveTypeId = type.ItemObjectiveTypeId,
                    MatchesAnyItemObjective = type.MatchesAnyItemObjective,
                    TargetKind = type.TargetKind,
                    TargetSelection = type.TargetSelection,
                    TargetId = RemapDefeatTarget(type, factionIdMap, allyIdMap),
                    ForceStatusTypeIds = type.ForceStatusTypeIds,
                    StatusMatchKind = type.StatusMatchKind,
                    PrerequisiteForceStatusTypeId = type.PrerequisiteForceStatusTypeId,
                    PrerequisiteWasLost = type.PrerequisiteWasLost,
                }),
            ];
        }

        return new StoredCampaign
        {
            Id = campaignId,
            Name = setup.Name,
            Description = setup.Description,
            PlayerSlotCount = setup.PlayerSlotCount,
            IsPrivate = setup.IsPrivate,
            IsPubliclyViewable = setup.IsPubliclyViewable,
            JoinPasswordHash = joinPasswordHash,
            CreatorIsParticipant = setup.CreatorIsParticipant,
            City = setup.City,
            Region = setup.Region,
            Country = setup.Country,
            MapStorageKey = mapStorageKey,
            Revision = revision,
            CreatedUtc = createdUtc,
            UpdatedUtc = updatedUtc,
            CreatedByUserId = createdByUserId,
            Memberships = memberships ??
            [
                new StoredCampaignMembership
                {
                    UserId = createdByUserId,
                    IsGameMaster = true,
                    IsPlayer = setup.CreatorIsParticipant,
                },
            ],
            Factions = factions,
            AllyGroups = allyGroups,
            Links =
            [
                .. setup.Links.Select(link => new StoredCampaignLink
                {
                    Id = Guid.NewGuid(),
                    Label = link.Label,
                    Url = link.Url,
                }),
            ],
            TimeZoneId = setup.Schedule.TimeZone.Id,
            StartsUtc = setup.Schedule.StartsUtc,
            EndsUtc = setup.Schedule.EndsUtc,
            RoundCount = setup.Schedule.RoundCount,
            RoundLengthAmount = setup.Schedule.RoundLength.Amount,
            RoundLengthUnit = setup.Schedule.RoundLength.Unit.ToString(),
            Phases =
            [
                .. setup.Schedule.Phases.Select(phase => new StoredRoundPhase
                {
                    Kind = phase.Kind.ToString(),
                    DurationAmount = phase.Duration.Amount,
                    DurationUnit = phase.Duration.Unit.ToString(),
                    EndPhaseEarlyIfAble = phase.EndPhaseEarlyIfAble,
                }),
            ],
            MapGraph = mapGraph,
            TerrainTypes = CatalogFileBinder.BindTerrains(setup.TerrainTypes, previousTerrainTypes, previousMissions),
            StructureTypes = CatalogFileBinder.BindStructures(setup.StructureTypes, previousStructureTypes, previousMissions),
            ItemObjectiveTypes = CatalogFileBinder.BindItemObjectives(setup.ItemObjectiveTypes, previousItemObjectiveTypes),
            PublicObjectiveTypes = CatalogFileBinder.BindPublicObjectives(setup.PublicObjectiveTypes),
            SpecialRules = CatalogFileBinder.BindSpecialRules(setup.SpecialRules),
            Missions = CatalogFileBinder.BindMissions(
                setup.Missions,
                CatalogFileBinder.IndexMissions(
                    (previousMissions ?? [])
                        .Concat(previousTerrainTypes?.SelectMany(static type => type.Missions) ?? [])
                        .Concat(previousStructureTypes?.SelectMany(static type => type.Missions) ?? []))),
            ForceStatuses = CatalogFileBinder.BindForceStatuses(setup.ForceStatuses),
            PrivateObjectiveTypes = privateObjectiveTypes,
            BattleScoring = setup.BattleScoring,
            RankingObjectivePoints = setup.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = setup.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = setup.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = setup.BattleReportRules,
            ArmyEscalations = setup.Schedule.ArmyEscalations,
        };
    }

    private static Guid? RemapDefeatTarget(
        StoredPrivateObjectiveType type,
        Dictionary<Guid, Guid> factionIdMap,
        Dictionary<Guid, Guid> allyIdMap)
    {
        if (type.TargetId is not { } targetId)
        {
            return null;
        }

        if (string.Equals(type.TargetKind, nameof(PrivateObjectiveTargetKind.Faction), StringComparison.Ordinal)
            && factionIdMap.TryGetValue(targetId, out var factionId))
        {
            return factionId;
        }

        if (string.Equals(type.TargetKind, nameof(PrivateObjectiveTargetKind.AllyGroup), StringComparison.Ordinal)
            && allyIdMap.TryGetValue(targetId, out var allyId))
        {
            return allyId;
        }

        return targetId;
    }
}
