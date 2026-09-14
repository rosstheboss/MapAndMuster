using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Seeds, scores, and replenishes secret rival objectives.
/// </summary>
public static class RivalObjectiveRules
{
    /// <summary>Default campaign points awarded for defeating a rival.</summary>
    public const int DefaultCampaignPoints = 5;

    /// <summary>
    /// Assigns each occupying player an enemy rival, preferring a unique target for every holder.
    /// </summary>
    public static IReadOnlyList<RivalObjectiveAssignment> SeedInitial(
        IReadOnlyList<Guid> occupyingPlayers,
        IReadOnlyDictionary<Guid, Guid> factionByPlayer,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenAllyFactionIds,
        DateTimeOffset utcNow,
        Func<int, int> pickIndex,
        int campaignPoints = DefaultCampaignPoints)
    {
        ArgumentNullException.ThrowIfNull(occupyingPlayers);
        ArgumentNullException.ThrowIfNull(factionByPlayer);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(brokenAllyFactionIds);
        ArgumentNullException.ThrowIfNull(pickIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(campaignPoints);
        if (occupyingPlayers.Count < 2)
        {
            return [];
        }

        var occupying = occupyingPlayers.Distinct().OrderBy(static id => id).ToArray();
        var blocked = new HashSet<Guid>();
        var eligible = occupying.ToDictionary(
            static player => player,
            player => EligibleRivals(
                player,
                occupying,
                blocked,
                factionByPlayer,
                factionAllyGroups,
                brokenAllyFactionIds));
        var remaining = occupying
            .Where(player => eligible[player].Length > 0)
            .OrderBy(player => eligible[player].Length)
            .ThenBy(static player => player)
            .ToArray();
        var unique = UniqueMatching(remaining, eligible, pickIndex);
        var assigned = new List<RivalObjectiveAssignment>();
        foreach (var player in remaining)
        {
            Guid rival;
            if (unique.TryGetValue(player, out var matched))
            {
                rival = matched;
            }
            else
            {
                var pool = eligible[player];
                rival = pool[pickIndex(pool.Length) % pool.Length];
            }

            assigned.Add(new RivalObjectiveAssignment(
                Guid.NewGuid(),
                player,
                rival,
                campaignPoints,
                PrivateObjectiveAssignmentStatus.Assigned,
                utcNow));
        }

        return assigned;
    }

    /// <summary>
    /// Reveals a holder's active rival after they win a battle or the rival surrenders.
    /// Delinquency, draws, no-contest, and ringer battles do not count.
    /// </summary>
    public static CampaignPlayState ApplyVictories(
        CampaignPlayState state,
        CampaignBattle resolved,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(resolved);
        if (resolved.IsNoContest || resolved.IsDraw || resolved.IsRinger)
        {
            return state;
        }

        var byForce = state.Forces.ToDictionary(static force => force.Id);
        Guid? ControllerOf(Guid forceId) =>
            byForce.TryGetValue(forceId, out var force) ? force.ControllerUserId : null;
        var winner = resolved.WinnerForceId is { } winnerId ? ControllerOf(winnerId) : null;
        var surrendered = resolved.SurrenderedForceIds
            .Select(ControllerOf)
            .OfType<Guid>()
            .ToHashSet();
        var participants = resolved.ParticipantForceIds
            .Select(ControllerOf)
            .OfType<Guid>()
            .ToHashSet();
        if (winner is null && surrendered.Count == 0)
        {
            return state;
        }

        var next = state.RivalObjectives.ToList();
        var changed = false;
        for (var index = 0; index < next.Count; index++)
        {
            var assignment = next[index];
            if (!assignment.IsActive)
            {
                continue;
            }

            var holderWon = winner == assignment.HolderUserId
                && (participants.Contains(assignment.RivalUserId) || surrendered.Contains(assignment.RivalUserId));
            var rivalSurrendered = surrendered.Contains(assignment.RivalUserId)
                && participants.Contains(assignment.HolderUserId)
                && !surrendered.Contains(assignment.HolderUserId);
            if (!holderWon && !rivalSurrendered)
            {
                continue;
            }

            next[index] = assignment.Reveal(utcNow);
            changed = true;
            state = state.AppendLog(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.RivalObjectiveRevealed,
                state.CurrentWindow()?.Id,
                resolved.WinnerForceId,
                assignment.HolderUserId,
                resolved.TerritoryId,
                null,
                resolved.Id,
                null,
                [],
                assignment.RivalUserId.ToString("N")));
        }

        return changed ? state.With(rivalObjectives: next) : state;
    }

    /// <summary>
    /// At the start of a non-final Action round, assigns a new closest enemy rival to players
    /// who already beat their previous rivals and currently have none active.
    /// </summary>
    public static CampaignPlayState Replenish(
        CampaignPlayState state,
        PlayMap map,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        DateTimeOffset utcNow,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(pickIndex);
        var window = state.CurrentWindow();
        if (window is not { Kind: RoundPhaseKind.Action, Status: PhaseWindowStatus.Open }
            || window.RoundNumber <= 1)
        {
            return state;
        }

        var lastRound = state.Windows.Count == 0 ? 1 : state.Windows.Max(static item => item.RoundNumber);
        if (window.RoundNumber >= lastRound)
        {
            return state;
        }

        var occupying = OccupyingPlayers(state);
        if (occupying.Length < 2)
        {
            return state;
        }

        var factionByPlayer = state.Forces
            .GroupBy(static force => force.ControllerUserId)
            .ToDictionary(static group => group.Key, static group => group.First().FactionId);
        var engaged = PlayersInOpenBattle(state);
        var next = state.RivalObjectives.ToList();
        var changed = false;
        foreach (var player in occupying)
        {
            if (next.Any(item => item.HolderUserId == player && item.IsActive))
            {
                continue;
            }

            var prior = next
                .Where(item => item.HolderUserId == player)
                .ToArray();
            if (prior.Length == 0)
            {
                continue;
            }

            var blocked = engaged.GetValueOrDefault(player) ?? [];
            var candidates = EligibleRivals(
                    player,
                    occupying,
                    prior.Select(static item => item.RivalUserId).ToHashSet(),
                    factionByPlayer,
                    factionAllyGroups,
                    state.BrokenAllyFactionIds)
                .Where(candidate => !blocked.Contains(candidate) && HasForce(state, candidate))
                .ToArray();
            if (candidates.Length == 0 || !HasForce(state, player))
            {
                continue;
            }

            var closest = ClosestRival(state, map, player, candidates, pickIndex);
            if (closest is not { } rival)
            {
                continue;
            }

            next.Add(new RivalObjectiveAssignment(
                Guid.NewGuid(),
                player,
                rival,
                prior[^1].CampaignPoints,
                PrivateObjectiveAssignmentStatus.Assigned,
                utcNow));
            changed = true;
        }

        return changed ? state.With(rivalObjectives: next) : state;
    }

    /// <summary>
    /// Campaign points from revealed rival assignments for a player.
    /// </summary>
    public static int PointsForPlayer(CampaignPlayState state, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(state);
        return PointsForPlayer(state.RivalObjectives, userId);
    }

    /// <summary>
    /// Campaign points from revealed rival assignments for a player.
    /// </summary>
    public static int PointsForPlayer(IReadOnlyList<RivalObjectiveAssignment> assignments, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        return assignments
            .Where(item => item.HolderUserId == userId && item.Status == PrivateObjectiveAssignmentStatus.Revealed)
            .Sum(static item => item.CampaignPoints);
    }

    /// <summary>
    /// Whether the viewer may see the rival's identity for an assignment.
    /// </summary>
    public static bool CanViewDetails(RivalObjectiveAssignment assignment, Guid viewerUserId, bool isStaff, bool campaignCompleted)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        return isStaff
            || campaignCompleted
            || assignment.HolderUserId == viewerUserId
            || assignment.Status == PrivateObjectiveAssignmentStatus.Revealed;
    }

    private static Dictionary<Guid, Guid> UniqueMatching(
        IReadOnlyList<Guid> holders,
        IReadOnlyDictionary<Guid, Guid[]> eligible,
        Func<int, int> pickIndex)
    {
        var rivalToHolder = new Dictionary<Guid, Guid>();
        foreach (var holder in holders)
        {
            TryAugment(holder, eligible, pickIndex, rivalToHolder, []);
        }

        return rivalToHolder.ToDictionary(static item => item.Value, static item => item.Key);
    }

    private static bool TryAugment(
        Guid holder,
        IReadOnlyDictionary<Guid, Guid[]> eligible,
        Func<int, int> pickIndex,
        Dictionary<Guid, Guid> rivalToHolder,
        HashSet<Guid> seenRivals)
    {
        var pool = eligible[holder];
        if (pool.Length == 0)
        {
            return false;
        }

        var start = pickIndex(pool.Length) % pool.Length;
        for (var index = 0; index < pool.Length; index++)
        {
            var rival = pool[(start + index) % pool.Length];
            if (!seenRivals.Add(rival))
            {
                continue;
            }

            if (!rivalToHolder.TryGetValue(rival, out var current)
                || TryAugment(current, eligible, pickIndex, rivalToHolder, seenRivals))
            {
                rivalToHolder[rival] = holder;
                return true;
            }
        }

        return false;
    }

    private static Guid[] OccupyingPlayers(CampaignPlayState state)
    {
        return
        [
            .. state.Forces
                .Select(static force => force.ControllerUserId)
                .Distinct()
                .OrderBy(static id => id),
        ];
    }

    private static bool HasForce(CampaignPlayState state, Guid userId)
    {
        return state.Forces.Any(force => force.ControllerUserId == userId);
    }

    private static Guid[] EligibleRivals(
        Guid player,
        IReadOnlyList<Guid> occupying,
        HashSet<Guid> blockedRivals,
        IReadOnlyDictionary<Guid, Guid> factionByPlayer,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenAllyFactionIds)
    {
        if (!factionByPlayer.TryGetValue(player, out var playerFaction))
        {
            return [];
        }

        return
        [
            .. occupying
                .Where(candidate =>
                    candidate != player
                    && !blockedRivals.Contains(candidate)
                    && factionByPlayer.TryGetValue(candidate, out var rivalFaction)
                    && ActionResolution.AreEnemies(playerFaction, rivalFaction, factionAllyGroups, brokenAllyFactionIds))
                .OrderBy(static id => id),
        ];
    }

    private static Dictionary<Guid, HashSet<Guid>> PlayersInOpenBattle(CampaignPlayState state)
    {
        var engaged = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var battle in state.Battles)
        {
            if (battle.Status is BattleStatus.Finalized or BattleStatus.GMResolved)
            {
                continue;
            }

            var players = battle.ParticipantForceIds
                .Select(id => state.Forces.FirstOrDefault(force => force.Id == id)?.ControllerUserId)
                .OfType<Guid>()
                .Distinct()
                .ToArray();
            foreach (var player in players)
            {
                if (!engaged.TryGetValue(player, out var others))
                {
                    others = [];
                    engaged[player] = others;
                }

                foreach (var other in players)
                {
                    if (other != player)
                    {
                        others.Add(other);
                    }
                }
            }
        }

        return engaged;
    }

    private static Guid? ClosestRival(
        CampaignPlayState state,
        PlayMap map,
        Guid player,
        IReadOnlyList<Guid> candidates,
        Func<int, int> pickIndex)
    {
        var holderTerritories = state.Forces
            .Where(force => force.ControllerUserId == player)
            .Select(static force => force.TerritoryId)
            .ToHashSet();
        if (holderTerritories.Count == 0)
        {
            return null;
        }

        var occupying = new Dictionary<Guid, List<Guid>>();
        foreach (var force in state.Forces)
        {
            if (!candidates.Contains(force.ControllerUserId))
            {
                continue;
            }

            if (!occupying.TryGetValue(force.TerritoryId, out var holders))
            {
                holders = [];
                occupying[force.TerritoryId] = holders;
            }

            holders.Add(force.ControllerUserId);
        }

        var best = new Dictionary<Guid, int>();
        var seen = new HashSet<Guid>();
        var queue = new Queue<(Guid TerritoryId, int Distance)>();
        foreach (var start in holderTerritories.OrderBy(static id => id))
        {
            queue.Enqueue((start, 0));
            seen.Add(start);
        }

        while (queue.Count > 0)
        {
            var (territoryId, distance) = queue.Dequeue();
            if (occupying.TryGetValue(territoryId, out var here))
            {
                foreach (var candidate in here)
                {
                    if (!best.TryGetValue(candidate, out var previous) || distance < previous)
                    {
                        best[candidate] = distance;
                    }
                }
            }

            foreach (var neighbor in map.Neighbors(territoryId))
            {
                if (seen.Add(neighbor))
                {
                    queue.Enqueue((neighbor, distance + 1));
                }
            }
        }

        if (best.Count == 0)
        {
            return null;
        }

        var closest = best.Values.Min();
        var tied = best
            .Where(item => item.Value == closest)
            .Select(static item => item.Key)
            .OrderBy(static id => id)
            .ToArray();
        return tied[pickIndex(tied.Length) % tied.Length];
    }
}
