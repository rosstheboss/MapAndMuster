using Campaign.Domain.Campaigns;

namespace Campaign.Domain.Play;

/// <summary>
/// Calculates current campaign-point standings from configured values and live map, battle, and award state.
/// Structure points are the current holdings. Battle points are cumulative from resolved results.
/// Ranking public objectives award their configured points to every player currently tied for first.
/// Named public objectives with 0 campaign points are ignored. Hidden item-objective points are included
/// only when those items are supplied in <see cref="CampaignPointScoringState.VisibleItems"/>.
/// </summary>
public static class CampaignPointStandingsRules
{
    /// <summary>Maximum rank shown on general public-objective leaderboards.</summary>
    public const int LeaderboardSize = 5;

    /// <summary>
    /// Returns standings and general public-objective leaderboards.
    /// </summary>
    /// <param name="state">The scoring snapshot.</param>
    /// <returns>Standings for every player plus ranking leaderboards.</returns>
    public static CampaignPointStandingsResult Calculate(CampaignPointScoringState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var structurePoints = state.StructurePoints;
        var itemPoints = state.ItemPoints;
        var publicPoints = state.PublicObjectivePoints;
        var scoring = state.BattleScoring;
        var ranking = state.RankingObjectivePoints;
        var forcesById = state.Forces.ToDictionary(static force => force.Id);
        var winsByPlayer = new Dictionary<Guid, int>();
        var drawsByPlayer = new Dictionary<Guid, int>();
        var battlePointsByPlayer = new Dictionary<Guid, int>();
        foreach (var battle in state.Battles)
        {
            if (battle.Status is not BattleStatus.Finalized and not BattleStatus.GMResolved)
            {
                continue;
            }

            var participants = battle.ParticipantForceIds
                .Select(id => forcesById.GetValueOrDefault(id))
                .OfType<CampaignForce>()
                .ToArray();
            if (battle.IsNoContest)
            {
                continue;
            }

            if (battle.IsDraw)
            {
                var drawPoints = BattleCampaignPointRules.DrawPoints(scoring, isDraw: true);
                foreach (var force in participants)
                {
                    drawsByPlayer[force.ControllerUserId] = drawsByPlayer.GetValueOrDefault(force.ControllerUserId) + 1;
                    battlePointsByPlayer[force.ControllerUserId] =
                        battlePointsByPlayer.GetValueOrDefault(force.ControllerUserId) + drawPoints;
                }

                continue;
            }

            if (battle.WinnerForceId is not { } winnerId || !forcesById.TryGetValue(winnerId, out var winner))
            {
                continue;
            }

            winsByPlayer[winner.ControllerUserId] = winsByPlayer.GetValueOrDefault(winner.ControllerUserId) + 1;
            battlePointsByPlayer[winner.ControllerUserId] =
                battlePointsByPlayer.GetValueOrDefault(winner.ControllerUserId)
                + BattleCampaignPointRules.WinnerPoints(scoring, isDraw: false, battle.WinnerScore, battle.LoserScore);
            var loserPoints = BattleCampaignPointRules.LoserPoints(
                scoring,
                isDraw: false,
                battle.WinnerScore,
                battle.LoserScore);
            foreach (var force in participants)
            {
                if (force.Id == winnerId)
                {
                    continue;
                }

                battlePointsByPlayer[force.ControllerUserId] =
                    battlePointsByPlayer.GetValueOrDefault(force.ControllerUserId) + loserPoints;
            }
        }

        foreach (var (userId, extra) in state.ExtraBattleReportPoints)
        {
            battlePointsByPlayer[userId] = battlePointsByPlayer.GetValueOrDefault(userId) + extra;
        }

        var activeAwards = new HashSet<(Guid PlayerId, Guid ObjectiveId)>();
        foreach (var award in state.Awards.OrderBy(static item => item.AwardedUtc).ThenBy(static item => item.Id))
        {
            var key = (award.PlayerUserId, award.ObjectiveId);
            if (award.IsActive)
            {
                activeAwards.Add(key);
            }
            else
            {
                activeAwards.Remove(key);
            }
        }

        var heldItemsByPlayer = new Dictionary<Guid, List<Guid>>();
        var otherByPlayer = new Dictionary<Guid, int>();
        foreach (var item in state.VisibleItems)
        {
            if (item.IsDestroyed
                || item.PossessorForceId is not { } forceId
                || !forcesById.TryGetValue(forceId, out var possessor))
            {
                continue;
            }

            var points = itemPoints.GetValueOrDefault(item.TypeId);
            otherByPlayer[possessor.ControllerUserId] = otherByPlayer.GetValueOrDefault(possessor.ControllerUserId) + points;
            if (!heldItemsByPlayer.TryGetValue(possessor.ControllerUserId, out var held))
            {
                held = [];
                heldItemsByPlayer[possessor.ControllerUserId] = held;
            }

            if (!held.Contains(item.TypeId))
            {
                held.Add(item.TypeId);
            }
        }

        var captureByFaction = new Dictionary<Guid, int>();
        var territoriesByFaction = new Dictionary<Guid, List<Guid>>();
        foreach (var territory in state.Territories)
        {
            if (territory.OwnerFactionId is not { } factionId)
            {
                continue;
            }

            if (!territoriesByFaction.TryGetValue(factionId, out var owned))
            {
                owned = [];
                territoriesByFaction[factionId] = owned;
            }

            owned.Add(territory.TerritoryId);
            if (territory.StructureTypeId is { } structureId
                && territory.StructureCondition != StructureCondition.Destroyed)
            {
                captureByFaction[factionId] =
                    captureByFaction.GetValueOrDefault(factionId) + structurePoints.GetValueOrDefault(structureId);
            }
        }

        var territoryCountByPlayer = new Dictionary<Guid, int>();
        var chainByPlayer = new Dictionary<Guid, int>();
        foreach (var player in state.Players)
        {
            var owned = player.FactionId is { } factionId
                ? territoriesByFaction.GetValueOrDefault(factionId) ?? []
                : [];
            territoryCountByPlayer[player.UserId] = owned.Count;
            chainByPlayer[player.UserId] = TerritoryChainRules.LongestOwnedChain(owned, state.Adjacencies);
        }

        var mostTerritoryLeaders = FirstPlace(state.Players, territoryCountByPlayer, _ => 0);
        var longestChainLeaders = FirstPlace(state.Players, chainByPlayer, _ => 0);
        var mostBattleLeaders = FirstPlace(state.Players, winsByPlayer, playerId => drawsByPlayer.GetValueOrDefault(playerId));

        var standings = new List<CampaignPointStanding>(state.Players.Count);
        foreach (var player in state.Players)
        {
            var capture = player.FactionId is { } factionId
                ? captureByFaction.GetValueOrDefault(factionId)
                : 0;
            var publicTotal = 0;
            foreach (var (playerId, objectiveId) in activeAwards)
            {
                if (playerId != player.UserId)
                {
                    continue;
                }

                var awarded = publicPoints.GetValueOrDefault(objectiveId);
                if (awarded > 0)
                {
                    publicTotal += awarded;
                }
            }

            if (ranking.MostTerritories > 0
                && mostTerritoryLeaders.Contains(player.UserId)
                && territoryCountByPlayer.GetValueOrDefault(player.UserId) > 0)
            {
                publicTotal += ranking.MostTerritories;
            }

            if (ranking.LongestTerritoryChain > 0
                && longestChainLeaders.Contains(player.UserId)
                && chainByPlayer.GetValueOrDefault(player.UserId) > 0)
            {
                publicTotal += ranking.LongestTerritoryChain;
            }

            if (ranking.MostBattlesWon > 0
                && mostBattleLeaders.Contains(player.UserId)
                && winsByPlayer.GetValueOrDefault(player.UserId) > 0)
            {
                publicTotal += ranking.MostBattlesWon;
            }

            var privateTotal = PrivateObjectiveRules.PointsForPlayer(
                state.PrivateObjectives,
                state.PrivateObjectivePoints,
                player.UserId,
                player.FactionId,
                player.FactionId is { } playerFaction
                    ? state.AllyGroupByFaction.GetValueOrDefault(playerFaction)
                    : null,
                state.CampaignCompleted);

            standings.Add(new CampaignPointStanding(
                player.UserId,
                capture,
                battlePointsByPlayer.GetValueOrDefault(player.UserId),
                publicTotal,
                privateTotal,
                otherByPlayer.GetValueOrDefault(player.UserId),
                heldItemsByPlayer.GetValueOrDefault(player.UserId) ?? []));
        }

        return new CampaignPointStandingsResult
        {
            Standings = standings,
            Leaderboards =
            [
                .. Leaderboard(
                    GeneralPublicObjectiveKinds.MostTerritories,
                    ranking.MostTerritories,
                    state.Players,
                    territoryCountByPlayer,
                    _ => 0),
                .. Leaderboard(
                    GeneralPublicObjectiveKinds.LongestTerritoryChain,
                    ranking.LongestTerritoryChain,
                    state.Players,
                    chainByPlayer,
                    _ => 0),
                .. Leaderboard(
                    GeneralPublicObjectiveKinds.MostBattlesWon,
                    ranking.MostBattlesWon,
                    state.Players,
                    winsByPlayer,
                    playerId => drawsByPlayer.GetValueOrDefault(playerId)),
            ],
        };
    }

    private static HashSet<Guid> FirstPlace(
        IReadOnlyList<CampaignPointPlayer> players,
        IReadOnlyDictionary<Guid, int> primary,
        Func<Guid, int> secondary)
    {
        var bestPrimary = int.MinValue;
        var bestSecondary = int.MinValue;
        foreach (var player in players)
        {
            var metric = primary.GetValueOrDefault(player.UserId);
            var tieBreak = secondary(player.UserId);
            if (metric > bestPrimary || (metric == bestPrimary && tieBreak > bestSecondary))
            {
                bestPrimary = metric;
                bestSecondary = tieBreak;
            }
        }

        var leaders = new HashSet<Guid>();
        if (bestPrimary <= 0)
        {
            return leaders;
        }

        foreach (var player in players)
        {
            if (primary.GetValueOrDefault(player.UserId) == bestPrimary && secondary(player.UserId) == bestSecondary)
            {
                leaders.Add(player.UserId);
            }
        }

        return leaders;
    }

    private static IEnumerable<PublicObjectiveLeaderboard> Leaderboard(
        string kind,
        int awardPoints,
        IReadOnlyList<CampaignPointPlayer> players,
        IReadOnlyDictionary<Guid, int> primary,
        Func<Guid, int> secondary)
    {
        if (awardPoints <= 0)
        {
            yield break;
        }

        var ordered = players
            .Select(player => (
                player.UserId,
                Primary: primary.GetValueOrDefault(player.UserId),
                Secondary: secondary(player.UserId)))
            .Where(row => row.Primary > 0 || row.Secondary > 0)
            .OrderByDescending(row => row.Primary)
            .ThenByDescending(row => row.Secondary)
            .ThenBy(row => row.UserId)
            .ToArray();
        if (ordered.Length == 0)
        {
            yield break;
        }

        var entries = new List<PublicObjectiveLeader>();
        var previous = (Primary: int.MinValue, Secondary: int.MinValue);
        var rank = 0;
        var index = 0;
        foreach (var row in ordered)
        {
            index++;
            if (row.Primary != previous.Primary || row.Secondary != previous.Secondary)
            {
                rank = index;
                previous = (row.Primary, row.Secondary);
            }

            if (rank > LeaderboardSize)
            {
                break;
            }

            entries.Add(new PublicObjectiveLeader(
                row.UserId,
                rank,
                row.Primary,
                row.Secondary,
                rank == 1));
        }

        yield return new PublicObjectiveLeaderboard(kind, awardPoints, entries);
    }
}

/// <summary>
/// Kind names for built-in ranking public objectives.
/// </summary>
public static class GeneralPublicObjectiveKinds
{
    /// <summary>Most territories currently controlled by the player's faction.</summary>
    public const string MostTerritories = "MostTerritories";

    /// <summary>Longest unbroken chain of the player's own territories.</summary>
    public const string LongestTerritoryChain = "LongestTerritoryChain";

    /// <summary>Most finalized battle wins, with draws as the tie-break.</summary>
    public const string MostBattlesWon = "MostBattlesWon";
}

/// <summary>
/// Standings plus ranking public-objective leaderboards.
/// </summary>
public sealed class CampaignPointStandingsResult
{
    /// <summary>Gets one standing per player, unsorted.</summary>
    public required IReadOnlyList<CampaignPointStanding> Standings { get; init; }

    /// <summary>Gets enabled ranking objectives with a current top five.</summary>
    public required IReadOnlyList<PublicObjectiveLeaderboard> Leaderboards { get; init; }
}

/// <summary>
/// Current leaders for one ranking public objective.
/// </summary>
/// <param name="Kind">The ranking objective kind.</param>
/// <param name="AwardPoints">Campaign points awarded to each current first-place player.</param>
/// <param name="Leaders">Players currently in the top five.</param>
public sealed record PublicObjectiveLeaderboard(
    string Kind,
    int AwardPoints,
    IReadOnlyList<PublicObjectiveLeader> Leaders);

/// <summary>
/// One player on a ranking public-objective leaderboard.
/// </summary>
/// <param name="UserId">The player.</param>
/// <param name="Rank">1-based rank after friendly ties.</param>
/// <param name="Metric">Primary metric (territories, chain length, or wins).</param>
/// <param name="TieBreakMetric">Secondary metric used only for most battles won (draws).</param>
/// <param name="AwardsPoints">Whether this player currently receives the objective's campaign points.</param>
public sealed record PublicObjectiveLeader(
    Guid UserId,
    int Rank,
    int Metric,
    int TieBreakMetric,
    bool AwardsPoints);

/// <summary>
/// Inputs needed to calculate current campaign-point standings.
/// </summary>
public sealed class CampaignPointScoringState
{
    /// <summary>Gets players who occupy a player slot.</summary>
    public required IReadOnlyList<CampaignPointPlayer> Players { get; init; }

    /// <summary>Gets current territory capture facts.</summary>
    public required IReadOnlyList<CampaignPointTerritory> Territories { get; init; }

    /// <summary>Gets undirected adjacencies used for territory-chain scoring.</summary>
    public IReadOnlyList<CampaignPointAdjacency> Adjacencies { get; init; } = [];

    /// <summary>Gets campaign points for controlling each structure type that is not destroyed.</summary>
    public required IReadOnlyDictionary<Guid, int> StructurePoints { get; init; }

    /// <summary>Gets campaign points for currently holding each item-objective type.</summary>
    public required IReadOnlyDictionary<Guid, int> ItemPoints { get; init; }

    /// <summary>Gets campaign points for each named public objective. Zero means the objective is ignored.</summary>
    public required IReadOnlyDictionary<Guid, int> PublicObjectivePoints { get; init; }

    /// <summary>Gets conversion from resolved battles into campaign points.</summary>
    public required BattleScoringSetup BattleScoring { get; init; }

    /// <summary>Gets campaign points for the built-in ranking public objectives.</summary>
    public required GeneralPublicObjectivePoints RankingObjectivePoints { get; init; }

    /// <summary>Gets battles.</summary>
    public required IReadOnlyList<CampaignBattle> Battles { get; init; }

    /// <summary>Gets forces.</summary>
    public required IReadOnlyList<CampaignForce> Forces { get; init; }

    /// <summary>Gets item objectives the viewer is allowed to score and display.</summary>
    public required IReadOnlyList<CampaignItemObjective> VisibleItems { get; init; }

    /// <summary>Gets public-objective award facts, oldest first when ordered by time.</summary>
    public required IReadOnlyList<PublicObjectiveAward> Awards { get; init; }

    /// <summary>Gets assigned private objectives.</summary>
    public IReadOnlyList<PrivateObjectiveAssignment> PrivateObjectives { get; init; } = [];

    /// <summary>Gets campaign points for each private-objective catalog type.</summary>
    public IReadOnlyDictionary<Guid, int> PrivateObjectivePoints { get; init; } =
        new Dictionary<Guid, int>();

    /// <summary>Gets ally-group identifiers by faction.</summary>
    public IReadOnlyDictionary<Guid, Guid?> AllyGroupByFaction { get; init; } =
        new Dictionary<Guid, Guid?>();

    /// <summary>Gets whether the campaign is completed, so remaining private objectives count.</summary>
    public bool CampaignCompleted { get; init; }

    /// <summary>
    /// Gets extra campaign points from slain generals, destroyed supply lines, and scored mission questions.
    /// </summary>
    public IReadOnlyDictionary<Guid, int> ExtraBattleReportPoints { get; init; } =
        new Dictionary<Guid, int>();
}

/// <summary>
/// A player included in campaign-point standings.
/// </summary>
/// <param name="UserId">The player's user identifier.</param>
/// <param name="FactionId">The chosen faction, when one is selected.</param>
public readonly record struct CampaignPointPlayer(Guid UserId, Guid? FactionId);

/// <summary>
/// Current capture facts for one territory.
/// </summary>
/// <param name="TerritoryId">The territory.</param>
/// <param name="OwnerFactionId">The controlling faction, or null when neutral.</param>
/// <param name="StructureTypeId">The structure type when one is present.</param>
/// <param name="StructureCondition">The structure condition.</param>
public readonly record struct CampaignPointTerritory(
    Guid TerritoryId,
    Guid? OwnerFactionId,
    Guid? StructureTypeId,
    StructureCondition StructureCondition);

/// <summary>
/// One player's current campaign-point breakdown. The five component totals add up to <see cref="Total"/>.
/// </summary>
/// <param name="UserId">The player.</param>
/// <param name="TerritoryAndStructurePoints">Points from currently owned non-destroyed structures.</param>
/// <param name="BattlesWonPoints">Points from resolved battles, including draws and differentials.</param>
/// <param name="PublicObjectivePoints">Points from ranking objectives and currently active named awards.</param>
/// <param name="PrivateObjectivePoints">Points from revealed or completed private objectives that apply to this player.</param>
/// <param name="OtherPoints">Points from currently held visible item objectives.</param>
/// <param name="HeldItemTypeIds">Distinct item-objective types the player currently holds, when visible to the viewer.</param>
public sealed record CampaignPointStanding(
    Guid UserId,
    int TerritoryAndStructurePoints,
    int BattlesWonPoints,
    int PublicObjectivePoints,
    int PrivateObjectivePoints,
    int OtherPoints,
    IReadOnlyList<Guid> HeldItemTypeIds)
{
    /// <summary>Gets the sum of the five component columns.</summary>
    public int Total =>
        TerritoryAndStructurePoints + BattlesWonPoints + PublicObjectivePoints + PrivateObjectivePoints + OtherPoints;
}
