using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Calculates per-force map supply from the connected owned or allied chain that force can reach,
/// plus round bonuses, the split-force penalty, and a player-owned temporary pool that any of that
/// player's forces may spend (each point applies to exactly one force).
/// </summary>
public static class SupplyRules
{
    /// <summary>
    /// Returns the current spendable supply snapshot for one player: the best-connected force's
    /// chain plus the shared temporary pool.
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
        var forces = state.Forces.Where(force => force.ControllerUserId == userId).ToArray();
        var temporary = state.PlayerSupplies.FirstOrDefault(item => item.UserId == userId)?.TemporarySupplyPoints ?? 0;
        if (forces.Length == 0)
        {
            return BuildSnapshot(state, map, catalog, userId, roundNumber, originTerritoryId: null, temporary);
        }

        var best = forces
            .Select(force => ForForce(state, map, catalog, force, roundNumber))
            .OrderByDescending(static item => item.ForceAllowancePoints)
            .ThenByDescending(static item => item.MapSupplyPoints)
            .First();
        return WithTemporary(best, temporary);
    }

    /// <summary>
    /// Returns map, round, and split-penalty supply for one force from the owned or allied chain
    /// that force can reach. Temporary points are omitted; they belong to the shared player pool.
    /// </summary>
    public static PlayerSupplySnapshot ForForce(
        CampaignPlayState state,
        PlayMap map,
        SupplyCatalog catalog,
        CampaignForce force,
        int roundNumber)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(force);
        return BuildSnapshot(state, map, catalog, force.ControllerUserId, roundNumber, force.TerritoryId, temporary: 0);
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
        IReadOnlyDictionary<Guid, StructureSupplyRules> structures,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(structures);
        var catalog = new SupplyCatalog(
            new Dictionary<Guid, int>(),
            structures,
            splitForceSupplyPenaltyPercent: 0,
            armyEscalations: [],
            factionByPlayer: new Dictionary<Guid, Guid>(),
            allyGroupByFaction: new Dictionary<Guid, string?>(),
            brokenAllyFactionIds: new HashSet<Guid>(),
            specialRules);
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
            var actor = forces.FirstOrDefault(force => force.TerritoryId == previous.Id && !force.InBattle);
            if (previous.StructureCondition == StructureCondition.Operational
                && next.StructureCondition == StructureCondition.Pillaged
                && next.StructureTypeId == structureTypeId)
            {
                awarded = rules.PillageSupplyPoints;
                if (actor is not null && catalog.SpecialRules.Has(actor, SpecialRuleEffectKeys.NorthernRaiders))
                {
                    awarded = Math.Max(2, awarded);
                }
            }
            else if (previous.StructureTypeId is not null && next.StructureTypeId is null)
            {
                awarded = rules.DestroySupplyPoints;
            }

            if (awarded <= 0)
            {
                continue;
            }

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
        var connected = ConnectedTerritoryIds(map, catalog, force.FactionId, originTerritoryId: null);
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

    internal static int SplitPenalty(int baseSupply, int value, bool isPercent)
    {
        if (baseSupply <= 0 || value <= 0)
        {
            return 0;
        }

        if (!isPercent)
        {
            return Math.Min(baseSupply, value);
        }

        return (int)Math.Floor(baseSupply * (value / 100m));
    }

    private static PlayerSupplySnapshot BuildSnapshot(
        CampaignPlayState state,
        PlayMap map,
        SupplyCatalog catalog,
        Guid userId,
        int roundNumber,
        Guid? originTerritoryId,
        int temporary)
    {
        var forceCount = state.Forces.Count(force => force.ControllerUserId == userId);
        var isSplit = forceCount > 1;
        var contributions = new List<SupplyContribution>();
        var mapSupply = MapSupply(state, map, catalog, userId, originTerritoryId, contributions);
        var escalation = EscalationFor(catalog.ArmyEscalations, roundNumber);
        var mapAfterPenalty = isSplit
            ? Math.Max(
                HuntInEstaliaDefaults.SplitForceMinimumMapSupply,
                mapSupply - SplitPenalty(
                    mapSupply,
                    catalog.SplitForceSupplyPenaltyPercent,
                    catalog.SplitForceSupplyPenaltyIsPercent))
            : mapSupply;
        if (isSplit && mapSupply <= 0)
        {
            mapAfterPenalty = 0;
        }

        var splitPenalty = mapSupply - mapAfterPenalty;
        var allowance = mapAfterPenalty + escalation.FreeSupplyPoints;
        if (escalation.FreeSupplyPoints != 0)
        {
            contributions.Add(
                new SupplyContribution(
                    SupplyContributionKind.RoundFree,
                    null,
                    escalation.FreeSupplyPoints,
                    "Round free supply",
                    IsAllied: false));
        }

        if (splitPenalty != 0)
        {
            contributions.Add(
                new SupplyContribution(
                    SupplyContributionKind.SplitPenalty,
                    null,
                    -splitPenalty,
                    "Split-force penalty",
                    IsAllied: false));
        }

        return WithTemporary(
            new PlayerSupplySnapshot(
                userId,
                mapSupply,
                escalation.FreeSupplyPoints,
                splitPenalty,
                0,
                allowance,
                escalation.MaxArmyPoints,
                escalation.FreeCharacterCount,
                isSplit,
                contributions),
            temporary);
    }

    private static PlayerSupplySnapshot WithTemporary(PlayerSupplySnapshot snapshot, int temporary)
    {
        if (temporary == 0)
        {
            return snapshot with
            {
                TemporarySupplyPoints = 0,
                CurrentSupplyPoints = snapshot.ForceAllowancePoints,
            };
        }

        return snapshot with
        {
            TemporarySupplyPoints = temporary,
            CurrentSupplyPoints = snapshot.ForceAllowancePoints + temporary,
            Contributions =
            [
                .. snapshot.Contributions,
                new SupplyContribution(
                    SupplyContributionKind.Temporary,
                    null,
                    temporary,
                    "Temporary supply",
                    IsAllied: false),
            ],
        };
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

    private static int MapSupply(
        CampaignPlayState state,
        PlayMap map,
        SupplyCatalog catalog,
        Guid userId,
        Guid? originTerritoryId,
        List<SupplyContribution> contributions)
    {
        if (!catalog.FactionByPlayer.TryGetValue(userId, out var factionId))
        {
            return 0;
        }

        var connected = ConnectedTerritoryIds(map, catalog, factionId, originTerritoryId);
        var total = 0;
        foreach (var territoryId in connected)
        {
            var territory = map.Territory(territoryId);
            if (territory is null)
            {
                continue;
            }

            var countsForSupply = territory.OwnerFactionId == factionId
                || (territory.OwnerFactionId is { } owner && InSupplyNetwork(territory, factionId, catalog));
            if (!countsForSupply)
            {
                continue;
            }

            var isAllied = territory.OwnerFactionId is { } holding && holding != factionId;
            if (territory.TerrainTypeId is { } terrainId
                && catalog.TerrainSupplyByType.TryGetValue(terrainId, out var terrainSupply)
                && terrainSupply != 0)
            {
                total += terrainSupply;
                contributions.Add(
                    new SupplyContribution(
                        SupplyContributionKind.TerritoryTerrain,
                        territory.Id,
                        terrainSupply,
                        "Terrain",
                        isAllied));
            }

            if (territory.StructureTypeId is { } structureId
                && territory.StructureCondition == StructureCondition.Operational
                && catalog.Structures.TryGetValue(structureId, out var structure)
                && structure.SupplyPoints != 0)
            {
                total += structure.SupplyPoints;
                contributions.Add(
                    new SupplyContribution(
                        SupplyContributionKind.TerritoryStructure,
                        territory.Id,
                        structure.SupplyPoints,
                        territory.StructureName ?? "Structure",
                        isAllied));
            }

            total += ExtraMapSupply(territory, factionId, catalog, userId, contributions);
        }

        total += PathIndependentSupply(map, catalog, factionId, userId, connected, contributions);
        _ = state;
        return total;
    }

    private static int ExtraMapSupply(
        PlayTerritory territory,
        Guid factionId,
        SupplyCatalog catalog,
        Guid userId,
        List<SupplyContribution> contributions)
    {
        var subfaction = catalog.SubfactionByPlayer.GetValueOrDefault(userId);
        var rules = catalog.SpecialRules;
        var extra = 0;
        var name = territory.StructureName;
        if (rules.Has(factionId, subfaction, SpecialRuleEffectKeys.Slavers)
            && territory.OwnerFactionId == factionId
            && territory.StructureCondition == StructureCondition.Operational
            && StructureKinds.IsTownOrCity(name))
        {
            extra += AddSpecial(contributions, territory.Id, 1, SpecialRuleEffectKeys.Slavers);
        }

        if (rules.Has(factionId, subfaction, SpecialRuleEffectKeys.SpawningPools)
            && territory.OwnerFactionId == factionId)
        {
            if (territory.IsWaterFeature && !StructureKinds.IsSettlement(name)
                && (territory.StructureTypeId is null || territory.StructureCondition != StructureCondition.Operational))
            {
                extra += AddSpecial(
                    contributions,
                    territory.Id,
                    HuntInEstaliaDefaults.SupplyPoints,
                    SpecialRuleEffectKeys.SpawningPools);
            }

            if (territory.StructureCondition == StructureCondition.Operational
                && (StructureKinds.IsSupplyDepot(name) || StructureKinds.IsFortification(name)))
            {
                extra += AddSpecial(contributions, territory.Id, 1, SpecialRuleEffectKeys.SpawningPools);
            }
        }

        if (rules.Has(factionId, subfaction, SpecialRuleEffectKeys.GreenTide)
            && territory.OwnerFactionId == factionId
            && (territory.StructureTypeId is null || territory.StructureCondition == StructureCondition.Pillaged))
        {
            extra += AddSpecial(
                contributions,
                territory.Id,
                HuntInEstaliaDefaults.SupplyPoints,
                SpecialRuleEffectKeys.GreenTide);
        }

        return extra;
    }

    private static int PathIndependentSupply(
        PlayMap map,
        SupplyCatalog catalog,
        Guid factionId,
        Guid userId,
        HashSet<Guid> connected,
        List<SupplyContribution> contributions)
    {
        var subfaction = catalog.SubfactionByPlayer.GetValueOrDefault(userId);
        var rules = catalog.SpecialRules;
        var extra = 0;
        foreach (var territory in map.Territories)
        {
            if (connected.Contains(territory.Id))
            {
                continue;
            }

            if (rules.Has(factionId, subfaction, SpecialRuleEffectKeys.SpawningPools)
                && territory.OwnerFactionId == factionId
                && territory.IsWaterFeature
                && !StructureKinds.IsSettlement(territory.StructureName)
                && (territory.StructureTypeId is null || territory.StructureCondition != StructureCondition.Operational))
            {
                extra += AddSpecial(
                    contributions,
                    territory.Id,
                    HuntInEstaliaDefaults.SupplyPoints,
                    SpecialRuleEffectKeys.SpawningPools);
            }

            if (rules.Has(factionId, subfaction, SpecialRuleEffectKeys.DefendersOfTheHomeland)
                && territory.OwnerFactionId is null
                && territory.StructureCondition == StructureCondition.Operational
                && StructureKinds.IsTownOrCity(territory.StructureName))
            {
                extra += AddSpecial(
                    contributions,
                    territory.Id,
                    HuntInEstaliaDefaults.SupplyPoints,
                    SpecialRuleEffectKeys.DefendersOfTheHomeland);
            }
        }

        return extra;
    }

    private static int AddSpecial(
        List<SupplyContribution> contributions,
        Guid territoryId,
        int points,
        string sourceName)
    {
        if (points == 0)
        {
            return 0;
        }

        contributions.Add(
            new SupplyContribution(
                SupplyContributionKind.SpecialRule,
                territoryId,
                points,
                sourceName,
                IsAllied: false));
        return points;
    }

    private static HashSet<Guid> ConnectedTerritoryIds(
        PlayMap map,
        SupplyCatalog catalog,
        Guid factionId,
        Guid? originTerritoryId)
    {
        var connected = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        foreach (var start in SupplyOrigins(map, catalog, factionId, originTerritoryId))
        {
            if (connected.Add(start))
            {
                queue.Enqueue(start);
            }
        }

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

    private static IEnumerable<Guid> SupplyOrigins(
        PlayMap map,
        SupplyCatalog catalog,
        Guid factionId,
        Guid? originTerritoryId)
    {
        if (originTerritoryId is not { } origin)
        {
            var spawn = map.SpawnFor(factionId);
            if (spawn is not null)
            {
                yield return spawn.Id;
            }

            yield break;
        }

        var territory = map.Territory(origin);
        if (territory is null)
        {
            yield break;
        }

        if (IsSupplyOrigin(map, catalog, factionId, territory))
        {
            yield return origin;
            yield break;
        }

        foreach (var neighborId in map.Neighbors(origin))
        {
            var neighbor = map.Territory(neighborId);
            if (neighbor is not null && IsSupplyOrigin(map, catalog, factionId, neighbor))
            {
                yield return neighborId;
            }
        }
    }

    private static bool IsSupplyOrigin(
        PlayMap map,
        SupplyCatalog catalog,
        Guid factionId,
        PlayTerritory territory)
    {
        var spawn = map.SpawnFor(factionId);
        return (spawn is not null && territory.Id == spawn.Id) || InSupplyNetwork(territory, factionId, catalog);
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
