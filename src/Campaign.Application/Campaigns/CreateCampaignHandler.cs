using Campaign.Application.Common;
using Campaign.Application.Maps;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;

namespace Campaign.Application.Campaigns;

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
                command.SpecialRules,
                command.PrivateObjectiveTypes,
                command.ForceStatuses))
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

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="secrets">The secret hasher.</param>
    /// <param name="assets">The catalog file storage.</param>
    public UpdateCampaignHandler(ICampaignStore campaigns, IClock clock, ISecretHasher secrets, ICampaignAssetStorage assets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(secrets);
        ArgumentNullException.ThrowIfNull(assets);
        _campaigns = campaigns;
        _clock = clock;
        _secrets = secrets;
        _assets = assets;
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
                command.SpecialRules,
                command.PrivateObjectiveTypes,
                command.ForceStatuses))
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
            existing.ItemObjectiveTypes);

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
                cancellationToken).ConfigureAwait(false);
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
        IReadOnlyList<StoredItemObjectiveType>? previousItemObjectiveTypes = null)
    {
        var allyGroups = setup.AllyGroups
            .Select(group => new StoredAllyGroup
            {
                Id = Guid.NewGuid(),
                Name = group.Name,
                Color = group.Color,
            })
            .ToArray();

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
            Factions = CatalogFileBinder.BindFactions(setup.Factions, previousFactions),
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
                }),
            ],
            MapGraph = mapGraph,
            TerrainTypes = CatalogFileBinder.BindTerrains(setup.TerrainTypes, previousTerrainTypes),
            StructureTypes = CatalogFileBinder.BindStructures(setup.StructureTypes, previousStructureTypes),
            ItemObjectiveTypes = CatalogFileBinder.BindItemObjectives(setup.ItemObjectiveTypes, previousItemObjectiveTypes),
            PublicObjectiveTypes = CatalogFileBinder.BindPublicObjectives(setup.PublicObjectiveTypes),
            SpecialRules = CatalogFileBinder.BindSpecialRules(setup.SpecialRules),
            ForceStatuses = CatalogFileBinder.BindForceStatuses(setup.ForceStatuses),
            PrivateObjectiveTypes = CatalogFileBinder.BindPrivateObjectives(setup.PrivateObjectiveTypes),
            BattleScoring = setup.BattleScoring,
            RankingObjectivePoints = setup.RankingObjectivePoints,
        };
    }
}
