using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Calculates current campaign-point standings from configured values and live map, battle, and award state.
/// Structure points are the current holdings. Battle points are cumulative from resolved results.
/// Ranking public objectives award their configured points to every player currently tied for first.
/// Campaign points per owned territory are scored with current structure holdings. Running public
/// objectives also add points per revealed relic held by an ally or faction-mate other than the
/// scoring player. Named public objectives with 0 campaign points are ignored.
/// Map holdings are scored per player: a shared faction total is never copied onto every co-faction
/// player. A territory counts for a player when that player's force occupies it, when it is stamped
/// with that player's subfaction, or when they are the only player of the owning faction.
/// Leaderboards cover each enabled public objective that is not an item objective;
/// allied relic control is omitted. At most five rows are listed; a tied group that would
/// exceed five becomes a summary of how many players share that value.
/// Hidden item-objective points are included only when those items are supplied in
/// <see cref="CampaignPointScoringState.VisibleItems"/>.
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
        var otherSourcesByPlayer = new Dictionary<Guid, List<CampaignPointSource>>();
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
            if (!otherSourcesByPlayer.TryGetValue(possessor.ControllerUserId, out var otherSources))
            {
                otherSources = [];
                otherSourcesByPlayer[possessor.ControllerUserId] = otherSources;
            }

            AddSource(
                otherSources,
                NameOrDefault(state.ItemNames, item.TypeId, "Item objective"),
                points);
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

        var territoriesByPlayer = TerritoriesByPlayer(state);
        var territoryCountByPlayer = new Dictionary<Guid, int>();
        var chainByPlayer = new Dictionary<Guid, int>();
        var structurePointsByPlayer = new Dictionary<Guid, int>();
        var unfilteredStructurePointsByPlayer = new Dictionary<Guid, int>();
        var pointsPerTerritoryCountByPlayer = new Dictionary<Guid, int>();
        foreach (var player in state.Players)
        {
            var owned = territoriesByPlayer.GetValueOrDefault(player.UserId) ?? [];
            var mostTerritoryOwned = FilterByTerrainTag(owned, ranking.MostTerritoriesTerrainTagId);
            var chainOwned = FilterByTerrainTag(owned, ranking.LongestTerritoryChainTerrainTagId);
            var perTerritoryOwned = FilterByTerrainTag(owned, ranking.PointsPerTerritoryTerrainTagId);
            territoryCountByPlayer[player.UserId] = mostTerritoryOwned.Count;
            chainByPlayer[player.UserId] = TerritoryChainRules.LongestOwnedChain(
                [.. chainOwned.Select(static item => item.TerritoryId)],
                state.Adjacencies);
            var structureTotal = 0;
            foreach (var territory in owned)
            {
                if (territory.StructureTypeId is { } structureId
                    && territory.StructureCondition != StructureCondition.Destroyed)
                {
                    structureTotal += structurePoints.GetValueOrDefault(structureId);
                }
            }

            unfilteredStructurePointsByPlayer[player.UserId] = structureTotal;
            structurePointsByPlayer[player.UserId] = StructurePointsFor(
                owned,
                structurePoints,
                ranking.MostStructurePointsStructureTagId,
                structureTotal);
            pointsPerTerritoryCountByPlayer[player.UserId] = perTerritoryOwned.Count;
        }

        var mostTerritoryLeaders = FirstPlace(state.Players, territoryCountByPlayer, _ => 0);
        var longestChainLeaders = FirstPlace(state.Players, chainByPlayer, _ => 0);
        var mostBattleLeaders = FirstPlace(state.Players, winsByPlayer, playerId => drawsByPlayer.GetValueOrDefault(playerId));
        var mostStructureLeaders = FirstPlace(state.Players, structurePointsByPlayer, _ => 0);

        var standings = new List<CampaignPointStanding>(state.Players.Count);
        foreach (var player in state.Players)
        {
            var capture = unfilteredStructurePointsByPlayer.GetValueOrDefault(player.UserId);
            var publicTotal = 0;
            var publicSources = new List<CampaignPointSource>();
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
                    AddSource(publicSources, NamedPublicLabel(state, objectiveId), awarded);
                }
            }

            if (ranking.MostTerritories > 0
                && mostTerritoryLeaders.Contains(player.UserId)
                && territoryCountByPlayer.GetValueOrDefault(player.UserId) > 0)
            {
                publicTotal += ranking.MostTerritories;
                AddSource(publicSources, "Most territories", ranking.MostTerritories);
            }

            if (ranking.LongestTerritoryChain > 0
                && longestChainLeaders.Contains(player.UserId)
                && chainByPlayer.GetValueOrDefault(player.UserId) > 0)
            {
                publicTotal += ranking.LongestTerritoryChain;
                AddSource(publicSources, "Longest territory chain", ranking.LongestTerritoryChain);
            }

            if (ranking.MostBattlesWon > 0
                && mostBattleLeaders.Contains(player.UserId)
                && winsByPlayer.GetValueOrDefault(player.UserId) > 0)
            {
                publicTotal += ranking.MostBattlesWon;
                AddSource(publicSources, "Most battles won", ranking.MostBattlesWon);
            }

            if (ranking.MostStructurePoints > 0
                && mostStructureLeaders.Contains(player.UserId)
                && structurePointsByPlayer.GetValueOrDefault(player.UserId) > 0)
            {
                publicTotal += ranking.MostStructurePoints;
                AddSource(publicSources, "Most structure points", ranking.MostStructurePoints);
            }

            if (ranking.AlliedRelicControlPoints > 0)
            {
                var alliedRelicPoints = ranking.AlliedRelicControlPoints
                    * AlliedRelicCount(player, state, forcesById);
                publicTotal += alliedRelicPoints;
                AddSource(publicSources, "Allied relic control", alliedRelicPoints);
            }

            var perTerritoryPoints = ranking.PointsPerTerritory
                * pointsPerTerritoryCountByPlayer.GetValueOrDefault(player.UserId);
            var territoryTotal = capture + perTerritoryPoints;
            var territorySources = new List<CampaignPointSource>();
            foreach (var territory in territoriesByPlayer.GetValueOrDefault(player.UserId) ?? [])
            {
                if (territory.StructureTypeId is not { } structureId
                    || territory.StructureCondition == StructureCondition.Destroyed)
                {
                    continue;
                }

                AddSource(
                    territorySources,
                    NameOrDefault(state.StructureNames, structureId, "Structures"),
                    structurePoints.GetValueOrDefault(structureId));
            }

            AddSource(territorySources, "Campaign points per territory", perTerritoryPoints);

            var extraBattlePoints = state.ExtraBattleReportPoints.GetValueOrDefault(player.UserId);
            var battleTotal = battlePointsByPlayer.GetValueOrDefault(player.UserId);
            var battleSources = new List<CampaignPointSource>();
            AddSource(battleSources, "Resolved battles", battleTotal - extraBattlePoints);
            AddSource(battleSources, "Battle reports", extraBattlePoints);

            var allyGroupId = player.FactionId is { } playerFaction
                ? state.AllyGroupByFaction.GetValueOrDefault(playerFaction)
                : null;
            var privateTotal = PrivateObjectiveRules.PointsForPlayer(
                state.PrivateObjectives,
                state.PrivateObjectivePoints,
                player.UserId,
                player.FactionId,
                allyGroupId,
                player.Subfaction);
            var privateSources = new List<CampaignPointSource>();
            foreach (var assignment in state.PrivateObjectives)
            {
                if (!PrivateObjectiveRules.CountsForPlayer(
                    assignment,
                    player.UserId,
                    player.FactionId,
                    allyGroupId,
                    player.Subfaction))
                {
                    continue;
                }

                AddSource(
                    privateSources,
                    NameOrDefault(state.PrivateObjectiveNames, assignment.TypeId, "Private objective"),
                    state.PrivateObjectivePoints.GetValueOrDefault(assignment.TypeId));
            }

            var rivalPoints = RivalObjectiveRules.PointsForPlayer(state.RivalObjectives, player.UserId);
            AddSource(privateSources, "Secret rival", rivalPoints);

            standings.Add(new CampaignPointStanding(
                player.UserId,
                territoryTotal,
                battleTotal,
                publicTotal,
                privateTotal + rivalPoints,
                otherByPlayer.GetValueOrDefault(player.UserId),
                heldItemsByPlayer.GetValueOrDefault(player.UserId) ?? [])
            {
                TerritoryAndStructureSources = territorySources,
                BattleSources = battleSources,
                PublicObjectiveSources = publicSources,
                PrivateObjectiveSources = privateSources,
                OtherSources = otherSourcesByPlayer.GetValueOrDefault(player.UserId) ?? [],
            });
        }

        return new CampaignPointStandingsResult
        {
            Standings = standings,
            Leaderboards =
            [
                .. Leaderboard(
                    GeneralPublicObjectiveKinds.MostTerritories,
                    ranking.MostTerritories,
                    "Most territories",
                    state.Players,
                    territoryCountByPlayer,
                    _ => 0),
                .. Leaderboard(
                    GeneralPublicObjectiveKinds.LongestTerritoryChain,
                    ranking.LongestTerritoryChain,
                    "Longest territory chain",
                    state.Players,
                    chainByPlayer,
                    _ => 0),
                .. Leaderboard(
                    GeneralPublicObjectiveKinds.MostBattlesWon,
                    ranking.MostBattlesWon,
                    "Most battles won",
                    state.Players,
                    winsByPlayer,
                    playerId => drawsByPlayer.GetValueOrDefault(playerId)),
                .. Leaderboard(
                    GeneralPublicObjectiveKinds.MostStructurePoints,
                    ranking.MostStructurePoints,
                    "Most structure points",
                    state.Players,
                    structurePointsByPlayer,
                    _ => 0),
                .. Leaderboard(
                    GeneralPublicObjectiveKinds.PointsPerTerritory,
                    ranking.PointsPerTerritory,
                    "Campaign points per territory",
                    state.Players,
                    pointsPerTerritoryCountByPlayer,
                    _ => 0),
                .. NamedPublicObjectiveLeaderboards(state, activeAwards),
            ],
        };
    }

    private static Dictionary<Guid, List<CampaignPointTerritory>> TerritoriesByPlayer(CampaignPointScoringState state)
    {
        var credited = new Dictionary<Guid, List<CampaignPointTerritory>>();
        foreach (var player in state.Players)
        {
            credited[player.UserId] = [];
        }

        var playersByFaction = state.Players
            .Where(static player => player.FactionId is not null)
            .GroupBy(static player => player.FactionId!.Value)
            .ToDictionary(static group => group.Key, static group => group.ToArray());
        var occupiersByTerritory = state.Forces
            .GroupBy(static force => force.TerritoryId)
            .ToDictionary(static group => group.Key, static group => group.ToArray());

        foreach (var territory in state.Territories)
        {
            if (territory.OwnerFactionId is not { } factionId
                || !playersByFaction.TryGetValue(factionId, out var factionPlayers))
            {
                continue;
            }

            var owner = CreditOwner(territory, factionPlayers, occupiersByTerritory);
            if (owner is { } playerId && credited.TryGetValue(playerId, out var owned))
            {
                owned.Add(territory);
            }
        }

        return credited;
    }

    private static Guid? CreditOwner(
        CampaignPointTerritory territory,
        CampaignPointPlayer[] factionPlayers,
        Dictionary<Guid, CampaignForce[]> occupiersByTerritory)
    {
        if (factionPlayers.Length == 1)
        {
            return factionPlayers[0].UserId;
        }

        occupiersByTerritory.TryGetValue(territory.TerritoryId, out var occupants);
        var occupyingOwners = (occupants ?? [])
            .Where(force => force.FactionId == territory.OwnerFactionId
                && factionPlayers.Any(player => player.UserId == force.ControllerUserId))
            .Select(static force => force.ControllerUserId)
            .Distinct()
            .ToArray();
        if (occupyingOwners.Length == 1)
        {
            return occupyingOwners[0];
        }

        if (string.IsNullOrWhiteSpace(territory.OwnerSubfaction))
        {
            return null;
        }

        var matching = factionPlayers
            .Where(player => string.Equals(player.Subfaction, territory.OwnerSubfaction, StringComparison.OrdinalIgnoreCase))
            .Select(static player => player.UserId)
            .Distinct()
            .ToArray();
        return matching.Length == 1 ? matching[0] : null;
    }

    private static List<CampaignPointTerritory> FilterByTerrainTag(
        IReadOnlyList<CampaignPointTerritory> owned,
        Guid? terrainTagId)
    {
        if (terrainTagId is not { } tag)
        {
            return [.. owned];
        }

        return [.. owned.Where(territory => (territory.TerrainTagIds ?? []).Contains(tag))];
    }

    private static int StructurePointsFor(
        IReadOnlyList<CampaignPointTerritory> owned,
        IReadOnlyDictionary<Guid, int> structurePoints,
        Guid? structureTagId,
        int unfilteredTotal)
    {
        if (structureTagId is not { } tag)
        {
            return unfilteredTotal;
        }

        var total = 0;
        foreach (var territory in owned)
        {
            if (territory.StructureTypeId is not { } structureId
                || territory.StructureCondition == StructureCondition.Destroyed
                || !(territory.StructureTagIds ?? []).Contains(tag))
            {
                continue;
            }

            total += structurePoints.GetValueOrDefault(structureId);
        }

        return total;
    }

    private static void AddSource(List<CampaignPointSource> sources, string label, int points)
    {
        if (points == 0)
        {
            return;
        }

        for (var index = 0; index < sources.Count; index++)
        {
            if (string.Equals(sources[index].Label, label, StringComparison.Ordinal))
            {
                sources[index] = sources[index] with { Points = sources[index].Points + points };
                return;
            }
        }

        sources.Add(new CampaignPointSource(label, points));
    }

    private static string NameOrDefault(IReadOnlyDictionary<Guid, string> names, Guid id, string fallback)
    {
        return names.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : fallback;
    }

    private static string NamedPublicLabel(CampaignPointScoringState state, Guid objectiveId)
    {
        foreach (var named in state.NamedPublicObjectives)
        {
            if (named.Id == objectiveId && !string.IsNullOrWhiteSpace(named.Name))
            {
                return named.Name;
            }
        }

        return "Public objective";
    }

    private static int AlliedRelicCount(
        CampaignPointPlayer player,
        CampaignPointScoringState state,
        Dictionary<Guid, CampaignForce> forcesById)
    {
        if (player.FactionId is not { } factionId)
        {
            return 0;
        }

        var selfGroup = state.AllyGroupByFaction.GetValueOrDefault(factionId);
        var broken = state.BrokenAllyFactionIds;
        var count = 0;
        foreach (var item in state.VisibleItems)
        {
            if (item.IsDestroyed
                || !item.IsRevealed
                || item.PossessorForceId is not { } forceId
                || !forcesById.TryGetValue(forceId, out var possessor)
                || possessor.ControllerUserId == player.UserId)
            {
                continue;
            }

            if (possessor.FactionId == factionId)
            {
                count++;
                continue;
            }

            var playerSubfaction = state.Forces.FirstOrDefault(item => item.ControllerUserId == player.UserId)?.Subfaction;
            if (AllyBetrayalRules.PlayerBetrayedFaction(player.UserId, possessor.FactionId, possessor.Subfaction, state.AllyBetrayals)
                || AllyBetrayalRules.PlayerBetrayedFaction(possessor.ControllerUserId, factionId, playerSubfaction, state.AllyBetrayals))
            {
                continue;
            }

            if (broken.Contains(factionId) || broken.Contains(possessor.FactionId))
            {
                continue;
            }

            var otherGroup = state.AllyGroupByFaction.GetValueOrDefault(possessor.FactionId);
            if (selfGroup is { } group && otherGroup == group)
            {
                count++;
            }
        }

        return count;
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

    private static IEnumerable<PublicObjectiveLeaderboard> NamedPublicObjectiveLeaderboards(
        CampaignPointScoringState state,
        HashSet<(Guid PlayerId, Guid ObjectiveId)> activeAwards)
    {
        foreach (var named in state.NamedPublicObjectives)
        {
            if (named.CampaignPoints <= 0)
            {
                continue;
            }

            var held = new Dictionary<Guid, int>();
            foreach (var player in state.Players)
            {
                if (activeAwards.Contains((player.UserId, named.Id)))
                {
                    held[player.UserId] = 1;
                }
            }

            foreach (var board in Leaderboard(
                GeneralPublicObjectiveKinds.Named,
                named.CampaignPoints,
                named.Name,
                state.Players,
                held,
                _ => 0))
            {
                yield return board;
            }
        }
    }

    private static IEnumerable<PublicObjectiveLeaderboard> Leaderboard(
        string kind,
        int awardPoints,
        string title,
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
        var entries = new List<PublicObjectiveLeader>();
        var remaining = LeaderboardSize;
        var seen = 0;
        var index = 0;
        while (index < ordered.Length && remaining > 0)
        {
            var current = ordered[index];
            var groupEnd = index + 1;
            while (groupEnd < ordered.Length
                && ordered[groupEnd].Primary == current.Primary
                && ordered[groupEnd].Secondary == current.Secondary)
            {
                groupEnd++;
            }

            var groupSize = groupEnd - index;
            var rank = seen + 1;
            if (groupSize <= remaining)
            {
                for (var member = index; member < groupEnd; member++)
                {
                    var row = ordered[member];
                    entries.Add(new PublicObjectiveLeader(
                        row.UserId,
                        rank,
                        row.Primary,
                        row.Secondary,
                        rank == 1));
                }

                remaining -= groupSize;
            }
            else
            {
                entries.Add(new PublicObjectiveLeader(
                    Guid.Empty,
                    rank,
                    current.Primary,
                    current.Secondary,
                    rank == 1,
                    groupSize));
                remaining = 0;
            }

            seen += groupSize;
            index = groupEnd;
        }

        yield return new PublicObjectiveLeaderboard(kind, awardPoints, entries, title);
    }
}

/// <summary>
/// Kind names for built-in ranking public objectives.
/// </summary>
public static class GeneralPublicObjectiveKinds
{
    /// <summary>Most territories currently credited to the player.</summary>
    public const string MostTerritories = "MostTerritories";

    /// <summary>Longest unbroken chain of the player's own territories.</summary>
    public const string LongestTerritoryChain = "LongestTerritoryChain";

    /// <summary>Most finalized battle wins, with draws as the tie-break.</summary>
    public const string MostBattlesWon = "MostBattlesWon";

    /// <summary>Most campaign points from currently owned non-destroyed structures.</summary>
    public const string MostStructurePoints = "MostStructurePoints";

    /// <summary>Configured campaign points for each currently owned territory.</summary>
    public const string PointsPerTerritory = "PointsPerTerritory";

    /// <summary>A named catalog public objective currently awarded to one or more players.</summary>
    public const string Named = "NamedPublicObjective";
}

/// <summary>
/// Standings plus ranking public-objective leaderboards.
/// </summary>
public sealed class CampaignPointStandingsResult
{
    /// <summary>Gets one standing per player, unsorted.</summary>
    public required IReadOnlyList<CampaignPointStanding> Standings { get; init; }

    /// <summary>
    /// Gets enabled public objectives that are not item objectives, each with a current top five.
    /// Allied relic control is scored but never listed here. A tied group that would push a board
    /// past five rows is summarized instead of listing every tied player.
    /// </summary>
    public required IReadOnlyList<PublicObjectiveLeaderboard> Leaderboards { get; init; }
}

/// <summary>
/// Current leaders for one ranking public objective.
/// </summary>
/// <param name="Kind">The ranking objective kind, or <see cref="GeneralPublicObjectiveKinds.Named"/>.</param>
/// <param name="AwardPoints">Campaign points awarded to each current first-place player.</param>
/// <param name="Leaders">Players currently in the top five, or a trailing tie summary.</param>
/// <param name="Title">Display name for the leaderboard heading.</param>
public sealed record PublicObjectiveLeaderboard(
    string Kind,
    int AwardPoints,
    IReadOnlyList<PublicObjectiveLeader> Leaders,
    string Title = "");

/// <summary>
/// One player on a ranking public-objective leaderboard.
/// </summary>
/// <param name="UserId">The player.</param>
/// <param name="Rank">1-based rank after friendly ties.</param>
/// <param name="Metric">Primary metric (territories, chain length, or wins).</param>
/// <param name="TieBreakMetric">Secondary metric used only for most battles won (draws).</param>
/// <param name="AwardsPoints">Whether this player currently receives the objective's campaign points.</param>
/// <param name="TiedPlayerCount">When greater than zero, this row summarizes that many tied players and <see cref="UserId"/> is empty.</param>
public sealed record PublicObjectiveLeader(
    Guid UserId,
    int Rank,
    int Metric,
    int TieBreakMetric,
    bool AwardsPoints,
    int TiedPlayerCount = 0);

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

    /// <summary>Gets named catalog public objectives in setup order, used for per-objective leaderboards.</summary>
    public IReadOnlyList<CampaignNamedPublicObjective> NamedPublicObjectives { get; init; } = [];

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

    /// <summary>Gets assigned rival objectives.</summary>
    public IReadOnlyList<RivalObjectiveAssignment> RivalObjectives { get; init; } = [];

    /// <summary>Gets campaign points for each private-objective catalog type.</summary>
    public IReadOnlyDictionary<Guid, int> PrivateObjectivePoints { get; init; } =
        new Dictionary<Guid, int>();

    /// <summary>Gets ally-group identifiers by faction.</summary>
    public IReadOnlyDictionary<Guid, Guid?> AllyGroupByFaction { get; init; } =
        new Dictionary<Guid, Guid?>();

    /// <summary>Gets factions that left their ally group through Backstab.</summary>
    public IReadOnlySet<Guid> BrokenAllyFactionIds { get; init; } = new HashSet<Guid>();

    /// <summary>Gets player-scoped Backstab betrayals.</summary>
    public IReadOnlyList<AllyBetrayal> AllyBetrayals { get; init; } = [];

    /// <summary>
    /// Gets extra campaign points from slain generals, destroyed supply lines, and scored mission questions.
    /// </summary>
    public IReadOnlyDictionary<Guid, int> ExtraBattleReportPoints { get; init; } =
        new Dictionary<Guid, int>();

    /// <summary>Gets display names for structure types used in standings source lists.</summary>
    public IReadOnlyDictionary<Guid, string> StructureNames { get; init; } = new Dictionary<Guid, string>();

    /// <summary>Gets display names for item-objective types used in standings source lists.</summary>
    public IReadOnlyDictionary<Guid, string> ItemNames { get; init; } = new Dictionary<Guid, string>();

    /// <summary>Gets display names for private-objective types used in standings source lists.</summary>
    public IReadOnlyDictionary<Guid, string> PrivateObjectiveNames { get; init; } =
        new Dictionary<Guid, string>();
}

/// <summary>
/// A player included in campaign-point standings.
/// </summary>
/// <param name="UserId">The player's user identifier.</param>
/// <param name="FactionId">The chosen faction, when one is selected.</param>
/// <param name="Subfaction">The chosen subfaction, when one is selected.</param>
public readonly record struct CampaignPointPlayer(Guid UserId, Guid? FactionId, string? Subfaction = null);

/// <summary>
/// A named catalog public objective included on standings leaderboards.
/// </summary>
/// <param name="Id">The catalog identifier.</param>
/// <param name="Name">The display name.</param>
/// <param name="CampaignPoints">Configured campaign points. Zero ignores the objective.</param>
public readonly record struct CampaignNamedPublicObjective(Guid Id, string Name, int CampaignPoints);

/// <summary>
/// Current capture facts for one territory.
/// </summary>
/// <param name="TerritoryId">The territory.</param>
/// <param name="OwnerFactionId">The controlling faction, or null when neutral.</param>
/// <param name="StructureTypeId">The structure type when one is present.</param>
/// <param name="StructureCondition">The structure condition.</param>
/// <param name="TerrainTagIds">Terrain-catalog tags on the occupying terrain type.</param>
/// <param name="StructureTagIds">Structure-catalog tags on the occupying structure type.</param>
/// <param name="OwnerSubfaction">The owning required subfaction, when ownership is subfaction-specific.</param>
public readonly record struct CampaignPointTerritory(
    Guid TerritoryId,
    Guid? OwnerFactionId,
    Guid? StructureTypeId,
    StructureCondition StructureCondition,
    IReadOnlyList<Guid>? TerrainTagIds = null,
    IReadOnlyList<Guid>? StructureTagIds = null,
    string? OwnerSubfaction = null);

/// <summary>
/// One player's current campaign-point breakdown. The five component totals add up to <see cref="Total"/>.
/// </summary>
/// <param name="UserId">The player.</param>
/// <param name="TerritoryAndStructurePoints">
/// Points from currently owned non-destroyed structures plus configured campaign points per owned territory.
/// </param>
/// <param name="BattlesWonPoints">Points from resolved battles, including draws and differentials.</param>
/// <param name="PublicObjectivePoints">
/// Points from ranking objectives, allied relic control, and currently active named awards.
/// </param>
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

    /// <summary>Gets labeled sources that add up to <see cref="TerritoryAndStructurePoints"/>.</summary>
    public IReadOnlyList<CampaignPointSource> TerritoryAndStructureSources { get; init; } = [];

    /// <summary>Gets labeled sources that add up to <see cref="BattlesWonPoints"/>.</summary>
    public IReadOnlyList<CampaignPointSource> BattleSources { get; init; } = [];

    /// <summary>Gets labeled sources that add up to <see cref="PublicObjectivePoints"/>.</summary>
    public IReadOnlyList<CampaignPointSource> PublicObjectiveSources { get; init; } = [];

    /// <summary>Gets labeled sources that add up to <see cref="PrivateObjectivePoints"/>.</summary>
    public IReadOnlyList<CampaignPointSource> PrivateObjectiveSources { get; init; } = [];

    /// <summary>Gets labeled sources that add up to <see cref="OtherPoints"/>.</summary>
    public IReadOnlyList<CampaignPointSource> OtherSources { get; init; } = [];
}

/// <summary>
/// One labeled contribution to a campaign-point column.
/// </summary>
/// <param name="Label">The source name.</param>
/// <param name="Points">Points from this source.</param>
public readonly record struct CampaignPointSource(string Label, int Points);
