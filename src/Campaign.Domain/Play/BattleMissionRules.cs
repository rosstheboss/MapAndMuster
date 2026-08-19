using Campaign.Domain.Campaigns;

namespace Campaign.Domain.Play;

/// <summary>
/// Chooses a battle mission from structure missions when any exist, otherwise terrain missions,
/// and assigns attacker/defender roles only when the situation or leftover attacker/defender
/// missions require it.
/// </summary>
public static class BattleMissionRules
{
    /// <summary>
    /// Picks one mission for a newly created battle and optional attacker/defender force ids.
    /// </summary>
    public static BattleMissionAssignment? Choose(
        PlayTerritory? territory,
        IReadOnlyList<CampaignForce> present,
        IReadOnlyDictionary<Guid, ActionKind> arrivalKinds,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenAllyFactionIds,
        IReadOnlyList<TerrainTypeSetup> terrainTypes,
        IReadOnlyList<StructureTypeSetup> structureTypes,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(present);
        ArgumentNullException.ThrowIfNull(arrivalKinds);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(brokenAllyFactionIds);
        ArgumentNullException.ThrowIfNull(terrainTypes);
        ArgumentNullException.ThrowIfNull(structureTypes);
        ArgumentNullException.ThrowIfNull(pickIndex);

        var pool = PoolFor(territory, terrainTypes, structureTypes);
        if (pool.Count == 0)
        {
            return null;
        }

        var roles = TryDetermineRoles(territory, present, arrivalKinds, factionAllyGroups, brokenAllyFactionIds);
        var attackerDefender = pool.Where(static mission => mission.IsAttackerDefender).ToArray();
        var normal = pool.Where(static mission => !mission.IsAttackerDefender).ToArray();
        MissionSetup[] candidates;
        Guid? attackerForceId = null;
        Guid? defenderForceId = null;
        if (roles is { } determined)
        {
            candidates = attackerDefender.Length > 0 ? attackerDefender : normal;
            attackerForceId = determined.AttackerForceId;
            defenderForceId = determined.DefenderForceId;
        }
        else if (normal.Length > 0)
        {
            candidates = normal;
        }
        else
        {
            candidates = attackerDefender;
            var assigned = RandomRoles(present, factionAllyGroups, brokenAllyFactionIds, pickIndex);
            attackerForceId = assigned?.AttackerForceId;
            defenderForceId = assigned?.DefenderForceId;
        }

        if (candidates.Length == 0)
        {
            return null;
        }

        var index = pickIndex(candidates.Length);
        if (index < 0 || index >= candidates.Length)
        {
            index = 0;
        }

        var mission = candidates[index];
        if (!mission.IsAttackerDefender)
        {
            attackerForceId = null;
            defenderForceId = null;
        }

        return new BattleMissionAssignment(mission.Id, attackerForceId, defenderForceId);
    }

    /// <summary>
    /// Structure missions when the structure has any; otherwise terrain missions.
    /// </summary>
    public static IReadOnlyList<MissionSetup> PoolFor(
        PlayTerritory? territory,
        IReadOnlyList<TerrainTypeSetup> terrainTypes,
        IReadOnlyList<StructureTypeSetup> structureTypes)
    {
        ArgumentNullException.ThrowIfNull(terrainTypes);
        ArgumentNullException.ThrowIfNull(structureTypes);
        if (territory is null)
        {
            return [];
        }

        var terrain = territory.TerrainTypeId is { } terrainId
            ? terrainTypes.FirstOrDefault(type => type.Id == terrainId)
            : null;
        var structure = territory.StructureTypeId is { } structureId
            ? structureTypes.FirstOrDefault(type => type.Id == structureId)
            : null;
        if (terrain is null)
        {
            return structure?.Missions ?? [];
        }

        return TerritoryMissionRules.Resolve(terrain, structure);
    }

    private static (Guid AttackerForceId, Guid DefenderForceId)? TryDetermineRoles(
        PlayTerritory? territory,
        IReadOnlyList<CampaignForce> present,
        IReadOnlyDictionary<Guid, ActionKind> arrivalKinds,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenAllyFactionIds)
    {
        var ordered = present.OrderBy(static force => force.Id).ToArray();
        var backstabber = ordered.FirstOrDefault(force =>
            arrivalKinds.GetValueOrDefault(force.Id) == ActionKind.Backstab);
        if (backstabber is not null)
        {
            var target = FirstEnemy(backstabber, ordered, factionAllyGroups, brokenAllyFactionIds);
            if (target is not null)
            {
                return (backstabber.Id, target.Id);
            }
        }

        if (territory is { StructureTypeId: not null, OwnerFactionId: { } owner })
        {
            var defender = ordered.FirstOrDefault(force =>
                force.FactionId == owner
                || ActionResolution.AreAllies(force.FactionId, owner, factionAllyGroups, brokenAllyFactionIds));
            var attacker = ordered.FirstOrDefault(force =>
                ActionResolution.AreEnemies(force.FactionId, owner, factionAllyGroups, brokenAllyFactionIds));
            if (defender is not null && attacker is not null)
            {
                return (attacker.Id, defender.Id);
            }
        }

        var holders = ordered.Where(force => IsDefendingArrival(arrivalKinds.GetValueOrDefault(force.Id))).ToArray();
        var movers = ordered.Where(force => IsAttackingArrival(arrivalKinds.GetValueOrDefault(force.Id))).ToArray();
        foreach (var holder in holders)
        {
            foreach (var mover in movers)
            {
                if (ActionResolution.AreEnemies(
                        holder.FactionId,
                        mover.FactionId,
                        factionAllyGroups,
                        brokenAllyFactionIds))
                {
                    return (mover.Id, holder.Id);
                }
            }
        }

        return null;
    }

    private static (Guid AttackerForceId, Guid DefenderForceId)? RandomRoles(
        IReadOnlyList<CampaignForce> present,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenAllyFactionIds,
        Func<int, int> pickIndex)
    {
        var ordered = present.OrderBy(static force => force.Id).ToArray();
        if (ordered.Length < 2)
        {
            return null;
        }

        var attacker = ordered[ClampIndex(pickIndex(ordered.Length), ordered.Length)];
        var enemies = ordered
            .Where(force => ActionResolution.AreEnemies(
                attacker.FactionId,
                force.FactionId,
                factionAllyGroups,
                brokenAllyFactionIds))
            .ToArray();
        if (enemies.Length == 0)
        {
            return null;
        }

        var defender = enemies[ClampIndex(pickIndex(enemies.Length), enemies.Length)];
        return (attacker.Id, defender.Id);
    }

    private static CampaignForce? FirstEnemy(
        CampaignForce force,
        IReadOnlyList<CampaignForce> present,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> brokenAllyFactionIds)
    {
        return present.FirstOrDefault(other =>
            other.Id != force.Id
            && ActionResolution.AreEnemies(
                force.FactionId,
                other.FactionId,
                factionAllyGroups,
                brokenAllyFactionIds));
    }

    private static bool IsAttackingArrival(ActionKind kind)
    {
        return kind is ActionKind.Move or ActionKind.Split;
    }

    private static bool IsDefendingArrival(ActionKind kind)
    {
        return kind is ActionKind.Hold or ActionKind.Retreat;
    }

    private static int ClampIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return index < 0 || index >= count ? 0 : index;
    }
}
