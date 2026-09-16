using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Random and specified item-objective teleport actions, interruption, and recharge.
/// </summary>
public static class TeleportActionRules
{
    /// <summary>Phases before a specified teleport can be used again, before Hold reductions.</summary>
    public const int ChosenTeleportRechargePhases = 3;

    /// <summary>Returns whether the kind is a random teleport, including the legacy Teleport kind without a target.</summary>
    public static bool IsRandomTeleport(ActionKind kind, Guid? targetTerritoryId = null)
    {
        return kind == ActionKind.TeleportRandomly
            || (kind == ActionKind.Teleport && targetTerritoryId is null);
    }

    /// <summary>Returns whether the kind is a player-chosen teleport destination.</summary>
    public static bool IsChosenTeleport(ActionKind kind, Guid? targetTerritoryId = null)
    {
        return kind == ActionKind.TeleportToSpecificTerritory
            || (kind == ActionKind.Teleport && targetTerritoryId is not null);
    }

    /// <summary>Returns whether the kind is either teleport action.</summary>
    public static bool IsTeleport(ActionKind kind)
    {
        return kind is ActionKind.Teleport
            or ActionKind.TeleportRandomly
            or ActionKind.TeleportToSpecificTerritory;
    }

    /// <summary>Returns whether a held item still may be dropped during Move.</summary>
    public static bool CanDropOnMove(CampaignItemObjective item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return !item.IsDestroyed
            && item.PossessorForceId is not null
            && item.ResolvedChoiceId is null
            && string.IsNullOrWhiteSpace(item.StateKey);
    }

    /// <summary>Neutral or allied non-spawn territories with no battle and no enemy occupants.</summary>
    public static IReadOnlyList<Guid> RandomDestinations(
        PlayMap map,
        CampaignForce force,
        IReadOnlyList<CampaignForce> occupyingForces,
        IReadOnlyList<CampaignBattle> battles,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(occupyingForces);
        ArgumentNullException.ThrowIfNull(battles);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(broken);
        ArgumentNullException.ThrowIfNull(betrayals);
        var openBattles = OpenBattleTerritories(battles);
        return
        [
            .. map.Territories
                .Where(territory =>
                    territory.Id != force.TerritoryId
                    && !territory.IsSpawn
                    && !openBattles.Contains(territory.Id)
                    && IsNeutralOrAllied(territory, force, factionAllyGroups, broken)
                    && !HasEnemyOccupant(
                        territory.Id,
                        force,
                        occupyingForces,
                        factionAllyGroups,
                        broken,
                        betrayals))
                .Select(static territory => territory.Id),
        ];
    }

    /// <summary>Non-spawn territories that currently have no enemy occupants.</summary>
    public static IReadOnlyList<Guid> ChosenDestinations(
        PlayMap map,
        CampaignForce force,
        IReadOnlyList<CampaignForce> occupyingForces,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(occupyingForces);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(broken);
        ArgumentNullException.ThrowIfNull(betrayals);
        return
        [
            .. map.Territories
                .Where(territory =>
                    territory.Id != force.TerritoryId
                    && !territory.IsSpawn
                    && !HasEnemyOccupant(
                        territory.Id,
                        force,
                        occupyingForces,
                        factionAllyGroups,
                        broken,
                        betrayals))
                .Select(static territory => territory.Id),
        ];
    }

    /// <summary>Picks a random teleport destination, or null when none exist.</summary>
    public static Guid? PickRandomDestination(
        PlayMap map,
        CampaignForce force,
        IReadOnlyList<CampaignForce> occupyingForces,
        IReadOnlyList<CampaignBattle> battles,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal> betrayals,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(pickIndex);
        var dests = RandomDestinations(
                map,
                force,
                occupyingForces,
                battles,
                factionAllyGroups,
                broken,
                betrayals)
            .OrderBy(static id => id)
            .ToArray();
        if (dests.Length == 0)
        {
            return null;
        }

        var index = pickIndex(dests.Length);
        if (index < 0 || index >= dests.Length)
        {
            index = 0;
        }

        return dests[index];
    }

    /// <summary>Returns whether an enemy occupies or is arriving at the territory.</summary>
    public static bool HasEnemyOccupant(
        Guid territoryId,
        CampaignForce force,
        IEnumerable<CampaignForce> others,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(others);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(broken);
        ArgumentNullException.ThrowIfNull(betrayals);
        return FirstEnemyOccupant(territoryId, force, others, factionAllyGroups, broken, betrayals) is not null;
    }

    /// <summary>Returns whether an allied force is submitting Backstab in the teleporter's source territory.</summary>
    public static bool HasSourceBackstab(
        Guid sourceTerritoryId,
        CampaignForce force,
        IEnumerable<CampaignForce> occupyingForces,
        IEnumerable<ActionKind> sourceActionKinds,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(occupyingForces);
        ArgumentNullException.ThrowIfNull(sourceActionKinds);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(broken);
        ArgumentNullException.ThrowIfNull(betrayals);
        var occupants = occupyingForces
            .Where(item => item.Id != force.Id && item.TerritoryId == sourceTerritoryId)
            .ToArray();
        if (occupants.Length == 0)
        {
            return false;
        }

        var backstabs = sourceActionKinds.Any(static kind => kind == ActionKind.Backstab);
        if (!backstabs)
        {
            return false;
        }

        return occupants.Any(item =>
            !IsEnemyOf(force, item, factionAllyGroups, broken, betrayals)
            && item.FactionId != force.FactionId);
    }

    /// <summary>True when an enemy is at the source or destination, or an ally backstabs at the source.</summary>
    public static bool IsInterrupted(
        Guid sourceTerritoryId,
        Guid? destinationTerritoryId,
        CampaignForce force,
        IReadOnlyList<CampaignForce> occupyingForces,
        IReadOnlyDictionary<Guid, ActionKind> actionByForceId,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        return DescribeInterrupt(
            sourceTerritoryId,
            destinationTerritoryId,
            force,
            occupyingForces,
            actionByForceId,
            factionAllyGroups,
            broken,
            betrayals) is not null;
    }

    /// <summary>
    /// Names the first interruption that cancels a teleport: an enemy at the source, then an enemy
    /// at the destination, then an allied Backstab at the source.
    /// </summary>
    public static TeleportInterrupt? DescribeInterrupt(
        Guid sourceTerritoryId,
        Guid? destinationTerritoryId,
        CampaignForce force,
        IReadOnlyList<CampaignForce> occupyingForces,
        IReadOnlyDictionary<Guid, ActionKind> actionByForceId,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(occupyingForces);
        ArgumentNullException.ThrowIfNull(actionByForceId);
        ArgumentNullException.ThrowIfNull(factionAllyGroups);
        ArgumentNullException.ThrowIfNull(broken);
        ArgumentNullException.ThrowIfNull(betrayals);
        var sourceEnemy = FirstEnemyOccupant(
            sourceTerritoryId,
            force,
            occupyingForces,
            factionAllyGroups,
            broken,
            betrayals);
        if (sourceEnemy is not null)
        {
            return new TeleportInterrupt(PlayLogFacts.InterruptEnemy, sourceEnemy.ControllerUserId, sourceTerritoryId);
        }

        if (destinationTerritoryId is { } dest)
        {
            var destEnemy = FirstEnemyOccupant(
                dest,
                force,
                occupyingForces,
                factionAllyGroups,
                broken,
                betrayals);
            if (destEnemy is not null)
            {
                return new TeleportInterrupt(PlayLogFacts.InterruptEnemy, destEnemy.ControllerUserId, dest);
            }
        }

        var backstabber = occupyingForces
            .Where(item => item.Id != force.Id && item.TerritoryId == sourceTerritoryId)
            .Where(item => actionByForceId.GetValueOrDefault(item.Id, ActionKind.Hold) == ActionKind.Backstab)
            .Where(item =>
                item.FactionId != force.FactionId
                && !IsEnemyOf(force, item, factionAllyGroups, broken, betrayals))
            .OrderBy(static item => item.Id)
            .FirstOrDefault();
        return backstabber is null
            ? null
            : new TeleportInterrupt(PlayLogFacts.InterruptBackstab, backstabber.ControllerUserId, sourceTerritoryId);
    }

    /// <summary>Reduces specified-teleport recharge; Hold subtracts one extra phase.</summary>
    public static int NextChosenTeleportCooldown(int remaining, ActionKind resolvedKind)
    {
        if (remaining <= 0)
        {
            return 0;
        }

        var next = remaining - 1;
        if (resolvedKind == ActionKind.Hold)
        {
            next--;
        }

        return Math.Max(0, next);
    }

    /// <summary>Configured status granted after a teleport succeeds or fails.</summary>
    public static string? ConfiguredOutcomeStatusName(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules,
        bool succeeded)
    {
        foreach (var effect in ItemObjectiveEffectRules.ActiveEffects(force, map, items, rules))
        {
            if (effect.Kind is not ItemObjectiveEffectKind.TeleportToRandomEmptyNonSpawn
                and not ItemObjectiveEffectKind.TeleportToChosenNonSpawnOncePerRound)
            {
                continue;
            }

            var statusId = succeeded ? effect.SuccessStatusTypeId : effect.FailureStatusTypeId;
            if (statusId is not { } id || id == Guid.Empty)
            {
                continue;
            }

            var name = rules.ForceStatusName(id);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return null;
    }

    private static CampaignForce? FirstEnemyOccupant(
        Guid territoryId,
        CampaignForce force,
        IEnumerable<CampaignForce> others,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        return others
            .Where(item =>
                item.Id != force.Id
                && item.TerritoryId == territoryId
                && IsEnemyOf(force, item, factionAllyGroups, broken, betrayals))
            .OrderBy(static item => item.Id)
            .FirstOrDefault();
    }

    private static bool IsNeutralOrAllied(
        PlayTerritory territory,
        CampaignForce force,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken)
    {
        if (territory.OwnerFactionId is not { } owner)
        {
            return true;
        }

        if (owner == force.FactionId)
        {
            return true;
        }

        return ActionResolution.AreAllies(force.FactionId, owner, factionAllyGroups, broken);
    }

    private static bool IsEnemyOf(
        CampaignForce force,
        CampaignForce other,
        IReadOnlyDictionary<Guid, string?> factionAllyGroups,
        IReadOnlyCollection<Guid> broken,
        IReadOnlyList<AllyBetrayal> betrayals)
    {
        if (other.FactionId == force.FactionId)
        {
            return false;
        }

        return AllyBetrayalRules.AreHostile(force, other, betrayals)
            || !ActionResolution.AreAllies(force.FactionId, other.FactionId, factionAllyGroups, broken);
    }

    private static HashSet<Guid> OpenBattleTerritories(IReadOnlyList<CampaignBattle> battles)
    {
        return battles
            .Where(static battle =>
                battle.Status is BattleStatus.Pending or BattleStatus.AwaitingResults or BattleStatus.Disputed)
            .Select(static battle => battle.TerritoryId)
            .ToHashSet();
    }
}

/// <summary>Why a teleport did not complete, including who interrupted it and where.</summary>
public sealed record TeleportInterrupt(string Reason, Guid InterrupterUserId, Guid PlaceTerritoryId);
