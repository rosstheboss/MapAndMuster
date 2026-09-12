using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Applies parameterized item-objective effects to the possessing force.
/// Nullify-adjacent items suppress other items whose location is next to the nullifier.
/// </summary>
public static class ItemObjectiveEffectRules
{
    /// <summary>Minimum map supply after a ModifySupply effect.</summary>
    public const int MinimumSupply = 1;

    /// <summary>
    /// Active (non-nullified) effects on items the force currently holds.
    /// </summary>
    public static IReadOnlyList<ItemObjectiveEffectSetup> ActiveEffects(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        var held = items
            .Where(item => !item.IsDestroyed && item.PossessorForceId == force.Id)
            .ToArray();
        var effects = new List<ItemObjectiveEffectSetup>();
        foreach (var item in held)
        {
            if (IsNullified(item, map, items, rules, occupyingForces: null))
            {
                continue;
            }

            effects.AddRange(rules.EffectsForItemType(item.TypeId));
        }

        return effects;
    }

    /// <summary>
    /// Active effects using occupying forces so carried items nullify by the holder's location.
    /// </summary>
    public static IReadOnlyList<ItemObjectiveEffectSetup> ActiveEffects(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules,
        IReadOnlyList<CampaignForce> occupyingForces)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(occupyingForces);
        var held = items
            .Where(item => !item.IsDestroyed && item.PossessorForceId == force.Id)
            .ToArray();
        var effects = new List<ItemObjectiveEffectSetup>();
        foreach (var item in held)
        {
            if (IsNullified(item, map, items, rules, occupyingForces))
            {
                continue;
            }

            effects.AddRange(rules.EffectsForItemType(item.TypeId));
        }

        return effects;
    }

    /// <summary>Returns whether another adjacent item is currently nullifying this instance.</summary>
    public static bool IsNullified(
        CampaignItemObjective item,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules,
        IReadOnlyList<CampaignForce>? occupyingForces = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        var location = LocationOf(item, items, occupyingForces);
        if (location is null)
        {
            return false;
        }

        foreach (var other in items)
        {
            if (other.Id == item.Id || other.IsDestroyed)
            {
                continue;
            }

            if (!rules.EffectsForItemType(other.TypeId).Any(static effect =>
                effect.Kind == ItemObjectiveEffectKind.NullifyAdjacentItemObjectives))
            {
                continue;
            }

            var otherLocation = LocationOf(other, items, occupyingForces);
            if (otherLocation is { } otherId && map.AreAdjacent(location.Value, otherId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Movement-speed bonus from held, non-nullified items.</summary>
    public static int MovementSpeedBonus(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        return ActiveEffects(force, map, items, rules)
            .Where(static effect => effect.Kind == ItemObjectiveEffectKind.AddMovementSpeed)
            .Sum(static effect => effect.Amount);
    }

    /// <summary>Signed supply adjustment from held items. Callers clamp to <see cref="MinimumSupply"/>.</summary>
    public static int SupplyAdjustment(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        return ActiveEffects(force, map, items, rules)
            .Where(static effect => effect.Kind == ItemObjectiveEffectKind.ModifySupply)
            .Sum(static effect => effect.Amount);
    }

    /// <summary>Applies supply adjustment and clamps to a minimum of 1 when an adjustment exists.</summary>
    public static int AdjustSupply(
        int mapSupply,
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        var delta = SupplyAdjustment(force, map, items, rules);
        if (delta == 0)
        {
            return mapSupply;
        }

        return Math.Max(MinimumSupply, mapSupply + delta);
    }

    /// <summary>Adjusts a force's army-point cap by held item effects.</summary>
    public static int AdjustArmyPoints(
        int roundMaxArmyPoints,
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        var cap = roundMaxArmyPoints;
        foreach (var effect in ActiveEffects(force, map, items, rules))
        {
            if (effect.Kind != ItemObjectiveEffectKind.ModifyArmyPoints)
            {
                continue;
            }

            cap += effect.AmountIsPercent
                ? (int)decimal.Floor(roundMaxArmyPoints * effect.Amount / 100m)
                : effect.Amount;
        }

        return Math.Max(0, cap);
    }

    /// <summary>Returns whether the holder may teleport this action window.</summary>
    public static bool CanTeleport(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        return ActiveEffects(force, map, items, rules)
            .Any(static effect => effect.Kind == ItemObjectiveEffectKind.TeleportToRandomEmptyNonSpawn);
    }

    /// <summary>Empty non-spawn territories with no occupying force.</summary>
    public static IReadOnlyList<Guid> TeleportDestinations(
        PlayMap map,
        IReadOnlyList<CampaignForce> forces)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(forces);
        var occupied = forces.Select(static force => force.TerritoryId).ToHashSet();
        return
        [
            .. map.Territories
                .Where(territory => !territory.IsSpawn && !occupied.Contains(territory.Id))
                .Select(static territory => territory.Id),
        ];
    }

    /// <summary>Picks a teleport destination, or null when none exist.</summary>
    public static Guid? PickTeleportDestination(
        PlayMap map,
        IReadOnlyList<CampaignForce> forces,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(pickIndex);
        var dests = TeleportDestinations(map, forces).OrderBy(static id => id).ToArray();
        if (dests.Length == 0)
        {
            return null;
        }

        return dests[pickIndex(dests.Length)];
    }

    /// <summary>Returns whether a winner's item forces losers to spawn.</summary>
    public static bool PushesDefeatedToSpawn(
        CampaignForce winner,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        return ActiveEffects(winner, map, items, rules)
            .Any(static effect => effect.Kind == ItemObjectiveEffectKind.PushDefeatedOpponentToSpawn);
    }

    /// <summary>Status names the holder should have while carrying the item.</summary>
    public static IReadOnlyList<string> HeldStatusNames(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        var names = new List<string>();
        foreach (var effect in ActiveEffects(force, map, items, rules))
        {
            if (effect.Kind != ItemObjectiveEffectKind.InflictStatusWhileHeld)
            {
                continue;
            }

            foreach (var statusId in effect.StatusTypeIds)
            {
                var name = rules.ForceStatusName(statusId);
                if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    /// <summary>Status names inflicted on other forces sharing the holder's territory.</summary>
    public static IReadOnlyList<string> SharedTerritoryStatusNames(
        CampaignForce holder,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        var names = new List<string>();
        foreach (var effect in ActiveEffects(holder, map, items, rules))
        {
            if (effect.Kind != ItemObjectiveEffectKind.InflictStatusOnSharedTerritory)
            {
                continue;
            }

            foreach (var statusId in effect.StatusTypeIds)
            {
                var name = rules.ForceStatusName(statusId);
                if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    /// <summary>Returns whether the force is immune to the named status from item effects.</summary>
    public static bool IsImmuneToStatus(
        CampaignForce force,
        string statusName,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        foreach (var effect in ActiveEffects(force, map, items, rules))
        {
            if (effect.Kind != ItemObjectiveEffectKind.ImmuneToStatuses)
            {
                continue;
            }

            if (effect.ImmuneToAllStatuses)
            {
                return true;
            }

            foreach (var statusId in effect.StatusTypeIds)
            {
                var name = rules.ForceStatusName(statusId);
                if (string.Equals(name, statusName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Custom battle-reminder text from held items.</summary>
    public static IReadOnlyList<string> CustomReminders(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        return
        [
            .. ActiveEffects(force, map, items, rules)
                .Where(static effect => effect.Kind == ItemObjectiveEffectKind.Custom
                    && !string.IsNullOrWhiteSpace(effect.CustomText))
                .Select(static effect => effect.CustomText!),
        ];
    }

    /// <summary>
    /// Returns whether two forces are treated as allied because of a held item, ignoring campaign groups.
    /// </summary>
    public static bool ForcesAlliedByItem(
        CampaignForce left,
        CampaignForce right,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        return AlliedBy(left, right, map, items, rules) || AlliedBy(right, left, map, items, rules);
    }

    /// <summary>Returns whether the force's campaign ally group is suspended by a held item.</summary>
    public static bool SuspendsAllyGroup(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        return ActiveEffects(force, map, items, rules)
            .Any(static effect => effect.Kind == ItemObjectiveEffectKind.OverrideAlliances
                && (effect.SuspendCurrentAllyGroup || !string.IsNullOrWhiteSpace(effect.ForcedAllyGroupName)));
    }

    /// <summary>Forced ally-group name from a held item, when present.</summary>
    public static string? ForcedAllyGroupName(
        CampaignForce force,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        return ActiveEffects(force, map, items, rules)
            .Where(static effect => effect.Kind == ItemObjectiveEffectKind.OverrideAlliances
                && !string.IsNullOrWhiteSpace(effect.ForcedAllyGroupName))
            .Select(static effect => effect.ForcedAllyGroupName)
            .FirstOrDefault();
    }

    /// <summary>
    /// Assigns held-item and shared-territory statuses after pickup or resolution.
    /// </summary>
    public static IReadOnlyList<CampaignForce> ApplyStatuses(
        IReadOnlyList<CampaignForce> forces,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rules);
        var next = forces.ToList();
        for (var index = 0; index < next.Count; index++)
        {
            var names = HeldStatusNames(next[index], map, items, rules);
            if (names.Count == 0)
            {
                continue;
            }

            var status = names[0];
            if (!string.Equals(next[index].StatusName, status, StringComparison.OrdinalIgnoreCase)
                && FactionSpecialRulePolicies.AllowsStatus(next[index], status, rules, map, items))
            {
                next[index] = next[index].WithStatus(status);
            }
        }

        var holders = next.ToArray();
        foreach (var holder in holders)
        {
            var shared = SharedTerritoryStatusNames(holder, map, items, rules);
            if (shared.Count == 0)
            {
                continue;
            }

            var status = shared[0];
            for (var index = 0; index < next.Count; index++)
            {
                if (next[index].Id == holder.Id || next[index].TerritoryId != holder.TerritoryId)
                {
                    continue;
                }

                if (!string.Equals(next[index].StatusName, status, StringComparison.OrdinalIgnoreCase)
                    && FactionSpecialRulePolicies.AllowsStatus(next[index], status, rules, map, items))
                {
                    next[index] = next[index].WithStatus(status);
                }
            }
        }

        return next;
    }

    /// <summary>
    /// Territory of a ground or carried item. Carried items use the possessor's current territory
    /// when occupying forces are supplied.
    /// </summary>
    public static Guid? LocationOf(
        CampaignItemObjective item,
        IReadOnlyList<CampaignItemObjective> items,
        IReadOnlyList<CampaignForce>? occupyingForces = null)
    {
        _ = items;
        return occupyingForces is null ? item.TerritoryId : LocationOf(item, occupyingForces);
    }

    /// <summary>
    /// Territory of an item, using the possessor's current location when carried.
    /// </summary>
    public static Guid? LocationOf(
        CampaignItemObjective item,
        IReadOnlyList<CampaignForce> forces)
    {
        if (item.PossessorForceId is { } forceId)
        {
            return forces.FirstOrDefault(force => force.Id == forceId)?.TerritoryId;
        }

        return item.TerritoryId;
    }

    private static bool AlliedBy(
        CampaignForce holder,
        CampaignForce other,
        PlayMap map,
        IReadOnlyList<CampaignItemObjective> items,
        SpecialRuleContext rules)
    {
        foreach (var effect in ActiveEffects(holder, map, items, rules))
        {
            if (effect.Kind != ItemObjectiveEffectKind.OverrideAlliances)
            {
                continue;
            }

            foreach (var target in effect.AlliedFactions)
            {
                if (target.FactionId != other.FactionId)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(target.Subfaction)
                    || string.Equals(target.Subfaction, other.Subfaction, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
