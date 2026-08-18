using Campaign.Domain.Campaigns;

namespace Campaign.Domain.Play;

/// <summary>
/// Calculates per-player supply from the connected map graph, round bonuses, split penalty, and a
/// player-owned temporary pool that can be spent on any of that player's forces (one point per force).
/// </summary>
public static class SupplyRules
{
    /// <summary>
    /// Returns the current spendable supply snapshot for one player.
    /// </summary>
    public static PlayerSupplySnapshot ForPlayer(
        CampaignPlayState state,
        PlayMap map,
        SupplyCatalog catalog,
        Guid userId,
        int roundNumber)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(catalog);
        var forceCount = state.Forces.Count(force => force.ControllerUserId == userId);
        var isSplit = forceCount > 1;
        var mapSupply = MapSupply(state, map, catalog, userId);
        var escalation = EscalationFor(catalog.ArmyEscalations, roundNumber);
        var mapAfterPenalty = isSplit
            ? Math.Max(
                HuntInEstaliaDefaults.SplitForceMinimumMapSupply,
                mapSupply - SplitPenalty(mapSupply, catalog.SplitForceSupplyPenaltyPercent))
            : mapSupply;
        if (isSplit && mapSupply <= 0)
        {
            mapAfterPenalty = 0;
        }

        var splitPenalty = mapSupply - mapAfterPenalty;
        var allowance = mapAfterPenalty + escalation.FreeSupplyPoints;
        var temporary = state.PlayerSupplies.FirstOrDefault(item => item.UserId == userId)?.TemporarySupplyPoints ?? 0;
        var current = allowance + temporary;
        return new PlayerSupplySnapshot(
            userId,
            mapSupply,
            escalation.FreeSupplyPoints,
            splitPenalty,
            temporary,
            current,
            escalation.MaxArmyPoints,
            escalation.FreeCharacterCount,
            isSplit);
    }

    /// <summary>
    /// Returns snapshots for every player who currently has a force.
    /// </summary>
    public static IReadOnlyList<PlayerSupplySnapshot> ForPlayers(
        CampaignPlayState state,
        PlayMap map,
        SupplyCatalog catalog,
        int roundNumber)
    {
        ArgumentNullException.ThrowIfNull(state);
        return
        [
            .. state.Forces
                .Select(static force => force.ControllerUserId)
                .Distinct()
                .OrderBy(static id => id)
                .Select(userId => ForPlayer(state, map, catalog, userId, roundNumber)),
        ];
    }

    /// <summary>
    /// Awards temporary supply when structures are pillaged or destroyed during action resolution.
    /// </summary>
    public static IReadOnlyList<PlayerSupplyBalance> AwardTemporary(
        IReadOnlyList<PlayerSupplyBalance> current,
        PlayMap before,
        PlayMap after,
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyDictionary<Guid, StructureSupplyRules> structures)
    {
        ArgumentNullException.ThrowIfNull(structures);
        var catalog = new SupplyCatalog(
            new Dictionary<Guid, int>(),
            structures,
            splitForceSupplyPenaltyPercent: 0,
            armyEscalations: [],
            factionByPlayer: new Dictionary<Guid, Guid>(),
            allyGroupByFaction: new Dictionary<Guid, string?>(),
            brokenAllyFactionIds: new HashSet<Guid>());
        return AwardTemporary(current, before, after, forces, catalog);
    }

    /// <summary>
    /// Awards temporary supply when structures are pillaged or destroyed during action resolution.
    /// </summary>
    public static IReadOnlyList<PlayerSupplyBalance> AwardTemporary(
        IReadOnlyList<PlayerSupplyBalance> current,
        PlayMap before,
        PlayMap after,
        IReadOnlyList<CampaignForce> forces,
        SupplyCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(catalog);
        var awards = new Dictionary<Guid, int>();
        foreach (var previous in before.Territories)
        {
            var next = after.Territory(previous.Id);
            if (next is null || previous.StructureTypeId is not { } structureTypeId)
            {
                continue;
            }

            if (!catalog.Structures.TryGetValue(structureTypeId, out var rules))
            {
                continue;
            }

            var awarded = 0;
            if (previous.StructureCondition == StructureCondition.Operational
                && next.StructureCondition == StructureCondition.Pillaged
                && next.StructureTypeId == structureTypeId)
            {
                awarded = rules.PillageSupplyPoints;
            }
            else if (previous.StructureTypeId is not null && next.StructureTypeId is null)
            {
                awarded = rules.DestroySupplyPoints;
            }

            if (awarded <= 0)
            {
                continue;
            }

            var actor = forces.FirstOrDefault(force => force.TerritoryId == previous.Id && !force.InBattle);
            if (actor is null)
            {
                continue;
            }

            awards[actor.ControllerUserId] = awards.GetValueOrDefault(actor.ControllerUserId) + awarded;
        }

        if (awards.Count == 0)
        {
            return current;
        }

        var nextBalances = current.ToDictionary(static item => item.UserId, static item => item.TemporarySupplyPoints);
        foreach (var pair in awards)
        {
            nextBalances[pair.Key] = nextBalances.GetValueOrDefault(pair.Key) + pair.Value;
        }

        return
        [
            .. nextBalances
                .OrderBy(static pair => pair.Key)
                .Select(static pair => new PlayerSupplyBalance(pair.Key, pair.Value)),
        ];
    }

    /// <summary>
    /// Splits a force's supply-costing unit count into recurring (map/round) spend then temporary spend.
    /// </summary>
    public static (int RecurringSpent, int TemporarySpent) AllocateSpend(int supplyCostingUnitCount, int forceAllowancePoints)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(supplyCostingUnitCount);
        ArgumentOutOfRangeException.ThrowIfNegative(forceAllowancePoints);
        var recurring = Math.Min(supplyCostingUnitCount, forceAllowancePoints);
        return (recurring, supplyCostingUnitCount - recurring);
    }

    /// <summary>
    /// Returns how many player-pool temporary points are required to cover independent per-force spends.
    /// Each force's requested points are added; one point cannot satisfy two forces.
    /// </summary>
    public static int TemporaryPointsRequired(IReadOnlyList<int> pointsPerForce)
    {
        ArgumentNullException.ThrowIfNull(pointsPerForce);
        var required = 0;
        foreach (var points in pointsPerForce)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(points);
            required += points;
        }

        return required;
    }

    /// <summary>
    /// Spends requested temporary points from the player's pool onto one or more forces.
    /// Each force's requested points are added independently so a single point cannot cover two forces.
    /// </summary>
    public static IReadOnlyList<PlayerSupplyBalance> SpendTemporary(
        IReadOnlyList<PlayerSupplyBalance> current,
        Guid userId,
        IReadOnlyList<int> pointsPerForce)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(pointsPerForce);
        var needed = TemporaryPointsRequired(pointsPerForce);
        if (needed == 0)
        {
            return current;
        }

        var found = false;
        var next = new List<PlayerSupplyBalance>(current.Count);
        foreach (var item in current)
        {
            if (item.UserId == userId)
            {
                found = true;
                next.Add(new PlayerSupplyBalance(userId, Math.Max(0, item.TemporarySupplyPoints - needed)));
                continue;
            }

            next.Add(item);
        }

        if (!found)
        {
            return current;
        }

        return [.. next.Where(static item => item.TemporarySupplyPoints > 0).OrderBy(static item => item.UserId)];
    }

    /// <summary>
    /// Eligible retreat destinations ranked safest-first: owned connected, then allied connected, then other
    /// eligible adjacent territories, then the spawn fallback.
    /// </summary>
    public static Guid SafestRetreat(
        PlayMap map,
        CampaignForce force,
        SupplyCatalog catalog,
        IReadOnlySet<Guid> occupiedAfterSubmittedRetreats)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(occupiedAfterSubmittedRetreats);
        var eligible = CampaignPlayRules.EligibleRetreats(map, force);
        var spawn = map.SpawnFor(force.FactionId);
        var connected = ConnectedTerritoryIds(map, catalog, force.FactionId);
        PlayTerritory? best = null;
        var bestRank = int.MaxValue;
        foreach (var id in eligible)
        {
            var territory = map.Territory(id);
            if (territory is null)
            {
                continue;
            }

            var occupied = occupiedAfterSubmittedRetreats.Contains(id);
            var rank = RetreatRank(territory, force.FactionId, connected, catalog, occupied, spawn?.Id);
            if (rank < bestRank || (rank == bestRank && (best is null || territory.DisplayNumber < best.DisplayNumber)))
            {
                best = territory;
                bestRank = rank;
            }
        }

        return best?.Id ?? spawn?.Id ?? force.TerritoryId;
    }

    internal static int SplitPenalty(int baseSupply, int percent)
    {
        if (baseSupply <= 0 || percent <= 0)
        {
            return 0;
        }

        return (int)Math.Floor(baseSupply * (percent / 100m));
    }

    private static RoundArmyEscalationSetup EscalationFor(
        IReadOnlyList<RoundArmyEscalationSetup> rows,
        int roundNumber)
    {
        if (rows.Count == 0)
        {
            return new RoundArmyEscalationSetup(Math.Max(1, roundNumber), 0, 0, 0);
        }

        return rows.FirstOrDefault(row => row.RoundNumber == roundNumber) ?? rows[^1];
    }

    private static int MapSupply(CampaignPlayState state, PlayMap map, SupplyCatalog catalog, Guid userId)
    {
        if (!catalog.FactionByPlayer.TryGetValue(userId, out var factionId))
        {
            return 0;
        }

        var connected = ConnectedTerritoryIds(map, catalog, factionId);
        var total = 0;
        foreach (var territoryId in connected)
        {
            var territory = map.Territory(territoryId);
            if (territory is null || territory.OwnerFactionId != factionId)
            {
                continue;
            }

            if (territory.TerrainTypeId is { } terrainId
                && catalog.TerrainSupplyByType.TryGetValue(terrainId, out var terrainSupply))
            {
                total += terrainSupply;
            }

            if (territory.StructureTypeId is { } structureId
                && territory.StructureCondition == StructureCondition.Operational
                && catalog.Structures.TryGetValue(structureId, out var structure))
            {
                total += structure.SupplyPoints;
            }
        }

        _ = state;
        return total;
    }

    private static HashSet<Guid> ConnectedTerritoryIds(PlayMap map, SupplyCatalog catalog, Guid factionId)
    {
        var spawn = map.SpawnFor(factionId);
        var connected = new HashSet<Guid>();
        if (spawn is null)
        {
            return connected;
        }

        var queue = new Queue<Guid>();
        queue.Enqueue(spawn.Id);
        connected.Add(spawn.Id);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighborId in map.Neighbors(current))
            {
                if (!connected.Add(neighborId))
                {
                    continue;
                }

                var neighbor = map.Territory(neighborId);
                if (neighbor is null || !InSupplyNetwork(neighbor, factionId, catalog))
                {
                    connected.Remove(neighborId);
                    continue;
                }

                queue.Enqueue(neighborId);
            }
        }

        return connected;
    }

    private static bool InSupplyNetwork(PlayTerritory territory, Guid factionId, SupplyCatalog catalog)
    {
        if (territory.OwnerFactionId is not { } owner)
        {
            return false;
        }

        if (owner == factionId)
        {
            return true;
        }

        if (catalog.BrokenAllyFactionIds.Contains(factionId) || catalog.BrokenAllyFactionIds.Contains(owner))
        {
            return false;
        }

        var selfGroup = catalog.AllyGroupByFaction.GetValueOrDefault(factionId);
        var ownerGroup = catalog.AllyGroupByFaction.GetValueOrDefault(owner);
        return !string.IsNullOrWhiteSpace(selfGroup)
            && string.Equals(selfGroup, ownerGroup, StringComparison.Ordinal);
    }

    private static int RetreatRank(
        PlayTerritory territory,
        Guid factionId,
        HashSet<Guid> connected,
        SupplyCatalog catalog,
        bool occupied,
        Guid? spawnId)
    {
        if (spawnId is { } spawn && territory.Id == spawn)
        {
            return 40;
        }

        var occupiedPenalty = occupied ? 10 : 0;
        if (territory.OwnerFactionId == factionId && connected.Contains(territory.Id))
        {
            return 0 + occupiedPenalty;
        }

        if (InSupplyNetwork(territory, factionId, catalog) && connected.Contains(territory.Id))
        {
            return 1 + occupiedPenalty;
        }

        if (territory.OwnerFactionId == factionId)
        {
            return 2 + occupiedPenalty;
        }

        return 3 + occupiedPenalty;
    }
}
