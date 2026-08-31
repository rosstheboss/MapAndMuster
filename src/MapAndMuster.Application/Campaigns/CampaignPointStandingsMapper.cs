using MapAndMuster.Application.Play;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Maps stored campaign state onto viewer-specific campaign-point standings.
/// Hidden item objectives are omitted from unauthorized totals and logos.
/// </summary>
internal static class CampaignPointStandingsMapper
{
    public static IReadOnlyList<CampaignPointStandingDetail> ToStandings(
        StoredCampaign campaign,
        IReadOnlyList<CampaignParticipantDetail> participants,
        Guid viewerUserId,
        bool staffView,
        DateTimeOffset utcNow)
    {
        return ToScoring(campaign, participants, viewerUserId, staffView, utcNow).Standings;
    }

    public static CampaignScoringView ToScoring(
        StoredCampaign campaign,
        IReadOnlyList<CampaignParticipantDetail> participants,
        Guid viewerUserId,
        bool staffView,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(participants);
        var play = campaign.PlayState ?? CampaignPlayState.Empty;
        var forcesById = play.Forces.ToDictionary(static force => force.Id);
        var visibleItems = play.ItemObjectives
            .Where(item => item.IsRevealed
                || staffView
                || (item.PossessorForceId is { } forceId
                    && forcesById.TryGetValue(forceId, out var possessor)
                    && possessor.ControllerUserId == viewerUserId))
            .ToArray();
        var graph = campaign.MapGraph;
        var conditions = play.Structures.ToDictionary(static item => item.TerritoryId);
        var territories = graph is null
            ? Array.Empty<CampaignPointTerritory>()
            : [.. graph.Territories.Select(territory =>
            {
                conditions.TryGetValue(territory.Id, out var structure);
                var structureTypeId = structure?.StructureTypeId ?? territory.StructureTypeId;
                var condition = structure?.Condition
                    ?? ParseCondition(territory.StructureCondition)
                    ?? StructureCondition.Operational;
                return new CampaignPointTerritory(
                    territory.Id,
                    territory.OwnerFactionId,
                    structureTypeId,
                    condition);
            })];
        var adjacencies = graph is null
            ? Array.Empty<CampaignPointAdjacency>()
            : [.. graph.Adjacencies.Select(static edge => new CampaignPointAdjacency(edge.TerritoryAId, edge.TerritoryBId))];

        var calculated = CampaignPointStandingsRules.Calculate(new CampaignPointScoringState
        {
            Players =
            [
                .. campaign.Memberships
                    .Where(static member => member.IsPlayer)
                    .Select(static member => new CampaignPointPlayer(member.UserId, member.FactionId)),
            ],
            Territories = territories,
            Adjacencies = adjacencies,
            StructurePoints = campaign.StructureTypes.ToDictionary(static type => type.Id, static type => type.CampaignPoints),
            ItemPoints = campaign.ItemObjectiveTypes.ToDictionary(static type => type.Id, static type => type.CampaignPoints),
            PublicObjectivePoints = campaign.PublicObjectiveTypes.ToDictionary(static type => type.Id, static type => type.CampaignPoints),
            BattleScoring = campaign.BattleScoring,
            RankingObjectivePoints = campaign.RankingObjectivePoints,
            Battles = play.Battles,
            Forces = play.Forces,
            VisibleItems = visibleItems,
            Awards = play.PublicObjectiveAwards,
            PrivateObjectives = play.PrivateObjectives,
            PrivateObjectivePoints = campaign.PrivateObjectiveTypes.ToDictionary(static type => type.Id, static type => type.CampaignPoints),
            AllyGroupByFaction = CampaignPlayCatalog.AllyGroupByFaction(campaign),
            BrokenAllyFactionIds = play.BrokenAllyFactionIds.ToHashSet(),
            CampaignCompleted = CampaignLifecycle.Progress(campaign, utcNow).Status == MapAndMuster.Domain.Campaigns.CampaignStatus.Completed,
            ExtraBattleReportPoints = CampaignPlayCatalog.ExtraBattleReportPoints(campaign),
        });

        var byUser = participants.ToDictionary(static participant => participant.UserId);
        var types = campaign.ItemObjectiveTypes.ToDictionary(static type => type.Id);
        var rows = new List<CampaignPointStandingDetail>();
        foreach (var standing in calculated.Standings)
        {
            if (!byUser.TryGetValue(standing.UserId, out var participant) || !participant.IsPlayer)
            {
                continue;
            }

            rows.Add(new CampaignPointStandingDetail
            {
                UserId = standing.UserId,
                Username = participant.Username,
                DisplayName = participant.DisplayName,
                FactionId = participant.FactionId,
                FactionName = participant.FactionName,
                FactionColor = participant.FactionColor,
                HasFlagImage = participant.HasFlagImage,
                TintFlagImage = participant.TintFlagImage,
                AllyGroupName = participant.AllyGroupName,
                TerritoryAndStructurePoints = standing.TerritoryAndStructurePoints,
                BattlesWonPoints = standing.BattlesWonPoints,
                PublicObjectivePoints = standing.PublicObjectivePoints,
                PrivateObjectivePoints = standing.PrivateObjectivePoints,
                OtherPoints = standing.OtherPoints,
                Total = standing.Total,
                HeldItems =
                [
                    .. standing.HeldItemTypeIds.Select(typeId =>
                    {
                        types.TryGetValue(typeId, out var type);
                        return new HeldItemObjectiveDetail
                        {
                            TypeId = typeId,
                            Name = type?.Name ?? "Item objective",
                            BuiltinSymbol = type is { ImageStorageKey: null } ? type.BuiltinSymbol : null,
                            Color = type?.Color ?? "#C45C26",
                            HasImage = !string.IsNullOrWhiteSpace(type?.ImageStorageKey),
                        };
                    }),
                ],
            });
        }

        return new CampaignScoringView
        {
            Standings =
            [
                .. rows
                    .OrderByDescending(static row => row.Total)
                    .ThenBy(static row => row.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static row => row.Username, StringComparer.OrdinalIgnoreCase),
            ],
            Leaderboards =
            [
                .. calculated.Leaderboards.Select(board => new PublicObjectiveLeaderboardDetail
                {
                    Kind = board.Kind,
                    AwardPoints = board.AwardPoints,
                    Leaders =
                    [
                        .. board.Leaders.Select(leader =>
                        {
                            byUser.TryGetValue(leader.UserId, out var participant);
                            return new PublicObjectiveLeaderDetail
                            {
                                UserId = leader.UserId,
                                Username = participant?.Username ?? string.Empty,
                                DisplayName = participant?.DisplayName ?? participant?.Username ?? "Player",
                                Rank = leader.Rank,
                                Metric = leader.Metric,
                                TieBreakMetric = leader.TieBreakMetric,
                                AwardsPoints = leader.AwardsPoints,
                            };
                        }),
                    ],
                }),
            ],
        };
    }

    private static StructureCondition? ParseCondition(string? value)
    {
        return Enum.TryParse<StructureCondition>(value, true, out var condition) ? condition : null;
    }
}

/// <summary>
/// Viewer-specific standings and ranking leaderboards.
/// </summary>
internal sealed class CampaignScoringView
{
    public required IReadOnlyList<CampaignPointStandingDetail> Standings { get; init; }

    public required IReadOnlyList<PublicObjectiveLeaderboardDetail> Leaderboards { get; init; }
}
