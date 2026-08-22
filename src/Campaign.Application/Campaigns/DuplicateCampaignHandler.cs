using Campaign.Application.Common;
using Campaign.Application.Maps;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Identity;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Copies campaign setup, overlay graph, factions, missions, and settings into a new campaign
/// managed by the caller. Raster and catalog files are reused until the copy changes them.
/// </summary>
public sealed class DuplicateCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    public DuplicateCampaignHandler(ICampaignStore campaigns, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _clock = clock;
    }

    /// <summary>
    /// Duplicates a campaign the caller manages or plays in.
    /// </summary>
    /// <param name="command">The duplicate command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        DuplicateCampaignCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var source = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        var membership = source is null ? null : CampaignMapper.MembershipFor(source, command.UserId);
        if (source is null || membership is null)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var now = _clock.UtcNow;
        if (!TryShiftSchedule(source, now, out var startsUtc, out var endsUtc, out var error))
        {
            return OperationResults.Failure<CampaignDetail>(
                error?.Code ?? ErrorCodes.TimeZoneInvalid,
                error?.Message ?? "The campaign schedule could not be copied.");
        }

        var creatorIsPlayer = membership.IsPlayer;
        var factionIds = source.Factions.ToDictionary(static faction => faction.Id, static _ => Guid.NewGuid());
        var copy = new StoredCampaign
        {
            Id = Guid.NewGuid(),
            Name = source.Name,
            Description = source.Description,
            PlayerSlotCount = source.PlayerSlotCount,
            IsPrivate = source.IsPrivate,
            IsPubliclyViewable = source.IsPubliclyViewable,
            JoinPasswordHash = source.JoinPasswordHash,
            CreatorIsParticipant = creatorIsPlayer,
            City = source.City,
            Region = source.Region,
            Country = source.Country,
            MapStorageKey = source.MapStorageKey,
            Revision = 1,
            CreatedUtc = now,
            UpdatedUtc = now,
            CreatedByUserId = command.UserId,
            Memberships =
            [
                new StoredCampaignMembership
                {
                    UserId = command.UserId,
                    IsGameMaster = true,
                    IsPlayer = creatorIsPlayer,
                },
            ],
            Factions =
            [
                .. source.Factions.Select(faction => new StoredFaction
                {
                    Id = factionIds[faction.Id],
                    Name = faction.Name,
                    Color = faction.Color,
                    Subfactions = faction.Subfactions,
                    AllyGroupName = faction.AllyGroupName,
                    RequiresSubfaction = faction.RequiresSubfaction,
                    FlagImageStorageKey = faction.FlagImageStorageKey,
                    SpecialRuleIds = faction.SpecialRuleIds,
                    SubfactionSpecialRules = faction.SubfactionSpecialRules,
                }),
            ],
            AllyGroups =
            [
                .. source.AllyGroups.Select(static group => new StoredAllyGroup
                {
                    Id = Guid.NewGuid(),
                    Name = group.Name,
                    Color = group.Color,
                }),
            ],
            Links =
            [
                .. source.Links.Select(static link => new StoredCampaignLink
                {
                    Id = Guid.NewGuid(),
                    Label = link.Label,
                    Url = link.Url,
                }),
            ],
            TimeZoneId = source.TimeZoneId,
            StartsUtc = startsUtc,
            EndsUtc = endsUtc,
            RoundCount = source.RoundCount,
            RoundLengthAmount = source.RoundLengthAmount,
            RoundLengthUnit = source.RoundLengthUnit,
            Phases = [.. source.Phases],
            MapGraph = CloneGraph(source.MapGraph, factionIds),
            PlayState = null,
            TerrainTypes = [.. source.TerrainTypes],
            StructureTypes = [.. source.StructureTypes],
            ItemObjectiveTypes = [.. source.ItemObjectiveTypes],
            PublicObjectiveTypes = [.. source.PublicObjectiveTypes],
            SpecialRules = [.. source.SpecialRules],
            Missions = [.. source.Missions],
            ForceStatuses = [.. source.ForceStatuses],
            PrivateObjectiveTypes = [.. source.PrivateObjectiveTypes],
            BattleScoring = source.BattleScoring,
            RankingObjectivePoints = source.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = source.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = source.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = source.BattleReportRules,
            ArmyEscalations = source.ArmyEscalations,
        };

        var created = await _campaigns.AddAsync(copy, cancellationToken).ConfigureAwait(false);
        return OperationResults.Success(CampaignMapper.ToDetail(created, command.UserId, now));
    }

    private static bool TryShiftSchedule(
        StoredCampaign source,
        DateTimeOffset duplicatedUtc,
        out DateTimeOffset startsUtc,
        out DateTimeOffset endsUtc,
        out Campaign.Domain.Common.DomainError? error)
    {
        startsUtc = default;
        endsUtc = default;
        error = null;
        if (!IanaTimeZone.TryCreate(source.TimeZoneId, out var timeZone, out error) || timeZone is null)
        {
            if (!IanaTimeZone.TryCreate(IanaTimeZone.UtcId, out timeZone, out error) || timeZone is null)
            {
                return false;
            }
        }

        if (timeZone is null)
        {
            error = new Campaign.Domain.Common.DomainError(
                "timeZone.invalid",
                "The copied time zone is not valid.",
                "timeZoneId");
            return false;
        }

        if (!Enum.TryParse<DurationUnit>(source.RoundLengthUnit, ignoreCase: true, out var roundUnit))
        {
            error = new Campaign.Domain.Common.DomainError(
                "roundLength.invalid",
                "The copied round length is not valid.",
                "roundLength");
            return false;
        }

        var week = new ScheduleDuration(1, DurationUnit.Weeks);
        var roundLength = new ScheduleDuration(source.RoundLengthAmount, roundUnit);
        startsUtc = CampaignCalendar.Add(duplicatedUtc, timeZone, week);
        endsUtc = startsUtc;
        for (var round = 0; round < source.RoundCount; round++)
        {
            endsUtc = CampaignCalendar.Add(endsUtc, timeZone, roundLength);
        }

        return true;
    }

    private static StoredMapGraph? CloneGraph(StoredMapGraph? graph, IReadOnlyDictionary<Guid, Guid> factionIds)
    {
        if (graph is null)
        {
            return null;
        }

        return new StoredMapGraph
        {
            Territories =
            [
                .. graph.Territories.Select(territory => new TerritoryDetail
                {
                    Id = territory.Id,
                    DisplayNumber = territory.DisplayNumber,
                    Name = territory.Name,
                    Description = territory.Description,
                    Polygon =
                    [
                        .. territory.Polygon.Select(static point => new MapPointDetail { X = point.X, Y = point.Y }),
                    ],
                    TerrainTypeId = territory.TerrainTypeId,
                    StructureTypeId = territory.StructureTypeId,
                    OverlayColor = territory.OverlayColor,
                    OwnerFactionId = Remap(territory.OwnerFactionId, factionIds),
                    OwnerSubfaction = territory.OwnerSubfaction,
                    SpawnFactionId = Remap(territory.SpawnFactionId, factionIds),
                    SpawnSubfaction = territory.SpawnSubfaction,
                    StructureCondition = territory.StructureCondition,
                }),
            ],
            Adjacencies =
            [
                .. graph.Adjacencies.Select(static edge => new AdjacencyDetail
                {
                    Id = edge.Id,
                    TerritoryAId = edge.TerritoryAId,
                    TerritoryBId = edge.TerritoryBId,
                    Origin = edge.Origin,
                    MarkerX = edge.MarkerX,
                    MarkerY = edge.MarkerY,
                }),
            ],
        };
    }

    private static Guid? Remap(Guid? factionId, IReadOnlyDictionary<Guid, Guid> factionIds)
    {
        if (factionId is { } id && factionIds.TryGetValue(id, out var mapped))
        {
            return mapped;
        }

        return factionId;
    }
}
