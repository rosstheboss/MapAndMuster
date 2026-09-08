using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Applies configured force-status enable and clear triggers, then the named Diseased engine rules.
/// A force has at most one status; Normal is stored as no status name. Diseased overrides other
/// catalog statuses. Mission, item, special-rule, and staff changes may replace Diseased.
/// </summary>
public static class ForceStatusRules
{
    /// <summary>Consecutive water-feature actions that inflict Diseased.</summary>
    public const int ConsecutiveWaterActionsToInfect = 3;

    /// <summary>Consecutive water-feature actions that inflict Diseased on a pre-battle surrender.</summary>
    public const int ConsecutiveWaterActionsToInfectOnSurrender = 2;

    /// <summary>
    /// Resolution facts used to enable or clear a force status.
    /// </summary>
    public readonly struct Facts
    {
        /// <summary>Initializes facts for one force after a window resolves.</summary>
        public Facts(
            bool held,
            bool moved,
            bool foughtBattle,
            bool won,
            bool lost,
            bool retreated,
            bool occupiesWater,
            bool occupiesCureSettlement = false,
            bool surrendered = false,
            bool lostOnWater = false,
            bool skipActionResolution = false,
            bool updateWaterStreak = true)
        {
            Held = held;
            Moved = moved;
            FoughtBattle = foughtBattle;
            Won = won;
            Lost = lost;
            Retreated = retreated;
            OccupiesWater = occupiesWater;
            OccupiesCureSettlement = occupiesCureSettlement;
            Surrendered = surrendered;
            LostOnWater = lostOnWater;
            SkipActionResolution = skipActionResolution;
            UpdateWaterStreak = updateWaterStreak;
        }

        /// <summary>Gets whether the force Held.</summary>
        public bool Held { get; }

        /// <summary>Gets whether the force Moved or Splits.</summary>
        public bool Moved { get; }

        /// <summary>Gets whether the force fought a resolved battle.</summary>
        public bool FoughtBattle { get; }

        /// <summary>Gets whether the force won a resolved battle.</summary>
        public bool Won { get; }

        /// <summary>Gets whether the force lost a resolved battle.</summary>
        public bool Lost { get; }

        /// <summary>Gets whether the force was forced to retreat.</summary>
        public bool Retreated { get; }

        /// <summary>Gets whether the force occupies a water-feature territory.</summary>
        public bool OccupiesWater { get; }

        /// <summary>Gets whether the force Holds on a Capital City, City, Supply Depot, or Town.</summary>
        public bool OccupiesCureSettlement { get; }

        /// <summary>Gets whether the force surrendered before fighting.</summary>
        public bool Surrendered { get; }

        /// <summary>Gets whether the force lost a fought battle on a water-feature territory.</summary>
        public bool LostOnWater { get; }

        /// <summary>Gets whether action-window catalog and water-streak updates should be skipped.</summary>
        public bool SkipActionResolution { get; }

        /// <summary>Gets whether this pass counts as a consecutive water-feature action.</summary>
        public bool UpdateWaterStreak { get; }
    }

    /// <summary>
    /// Attribution for one force whose status was set by the engine this pass.
    /// </summary>
    public readonly struct Attribution
    {
        /// <summary>Initializes attribution for a status change.</summary>
        public Attribution(
            ForceStatusChangeSource source,
            string? detail = null,
            Guid? actorForceId = null,
            Guid? actorFactionId = null,
            Guid? actorUserId = null)
        {
            Source = source;
            Detail = detail;
            ActorForceId = actorForceId;
            ActorFactionId = actorFactionId;
            ActorUserId = actorUserId;
        }

        /// <summary>Gets why the status changed.</summary>
        public ForceStatusChangeSource Source { get; }

        /// <summary>Gets extra source text, such as a mission name.</summary>
        public string? Detail { get; }

        /// <summary>Gets the force attributed as causing the change, when known.</summary>
        public Guid? ActorForceId { get; }

        /// <summary>Gets the faction attributed as causing the change, when known.</summary>
        public Guid? ActorFactionId { get; }

        /// <summary>Gets the player attributed as causing the change, when known.</summary>
        public Guid? ActorUserId { get; }
    }

    /// <summary>
    /// Result of applying catalog and disease rules, including per-force attributions.
    /// </summary>
    public sealed class Application
    {
        /// <summary>Initializes an application result.</summary>
        public Application(
            IReadOnlyList<CampaignForce> forces,
            IReadOnlyDictionary<Guid, Attribution> attributions)
        {
            Forces = forces;
            Attributions = attributions;
        }

        /// <summary>Gets the forces after status application.</summary>
        public IReadOnlyList<CampaignForce> Forces { get; }

        /// <summary>Gets attributions for forces whose status was decided this pass.</summary>
        public IReadOnlyDictionary<Guid, Attribution> Attributions { get; }
    }

    /// <summary>
    /// Applies catalog statuses and named Diseased rules. Current statuses that have not met
    /// their clear trigger are kept; otherwise the first matching enable in catalog order wins.
    /// Named Diseased overrides other catalog statuses.
    /// </summary>
    public static IReadOnlyList<CampaignForce> Apply(
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyList<ForceStatusSetup> statuses,
        IReadOnlyDictionary<Guid, Facts> factsByForceId,
        SpecialRuleContext? specialRules = null)
    {
        return ApplyDetailed(forces, statuses, factsByForceId, specialRules).Forces;
    }

    /// <summary>
    /// Applies catalog statuses and named Diseased rules, returning per-force attributions.
    /// </summary>
    public static Application ApplyDetailed(
        IReadOnlyList<CampaignForce> forces,
        IReadOnlyList<ForceStatusSetup> statuses,
        IReadOnlyDictionary<Guid, Facts> factsByForceId,
        SpecialRuleContext? specialRules = null)
    {
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(statuses);
        ArgumentNullException.ThrowIfNull(factsByForceId);
        var rules = specialRules ?? SpecialRuleContext.None;
        var attributions = new Dictionary<Guid, Attribution>();
        var catalogByName = statuses.ToDictionary(static status => status.Name, StringComparer.OrdinalIgnoreCase);
        var hasDisease = statuses.Any(static status => ForceStatusNames.IsDiseased(status.Name));
        var next = new List<CampaignForce>(forces.Count);

        foreach (var force in forces)
        {
            if (!factsByForceId.TryGetValue(force.Id, out var facts) || facts.SkipActionResolution)
            {
                next.Add(force);
                continue;
            }

            var streak = facts.UpdateWaterStreak
                ? facts.OccupiesWater
                    ? force.ConsecutiveWaterActions + 1
                    : 0
                : force.ConsecutiveWaterActions;
            var withStreak = force.With(consecutiveWaterActions: streak);
            if (statuses.Count == 0 && !hasDisease)
            {
                next.Add(withStreak);
                continue;
            }

            var (status, attribution) = ResolveForce(
                withStreak,
                statuses,
                catalogByName,
                facts,
                hasDisease,
                rules);
            var updated = withStreak.WithStatus(status);
            if (attribution is { } assigned
                && !string.Equals(updated.StatusName, force.StatusName, StringComparison.Ordinal))
            {
                attributions[updated.Id] = assigned;
            }

            next.Add(updated);
        }

        if (hasDisease)
        {
            SpreadContagion(next, rules, attributions);
        }

        return new Application(next, attributions);
    }

    /// <summary>
    /// Inflicts Diseased on other factions sharing a territory with a Diseased force.
    /// </summary>
    public static void SpreadContagion(
        List<CampaignForce> forces,
        SpecialRuleContext rules,
        IDictionary<Guid, Attribution> attributions)
    {
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(attributions);
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var group in forces.GroupBy(static force => force.TerritoryId).ToArray())
            {
                var members = group.ToArray();
                var carriers = members
                    .Where(static force => ForceStatusNames.IsDiseased(force.StatusName))
                    .OrderBy(static force => force.Id)
                    .ToArray();
                if (carriers.Length == 0)
                {
                    continue;
                }

                for (var index = 0; index < forces.Count; index++)
                {
                    var force = forces[index];
                    if (force.TerritoryId != group.Key
                        || ForceStatusNames.IsDiseased(force.StatusName)
                        || carriers.All(carrier => carrier.FactionId == force.FactionId)
                        || !FactionSpecialRulePolicies.AllowsStatus(force, ForceStatusNames.Diseased, rules))
                    {
                        continue;
                    }

                    var carrier = carriers.First(item => item.FactionId != force.FactionId);
                    forces[index] = force.WithStatus(ForceStatusNames.Diseased);
                    attributions[force.Id] = new Attribution(
                        ForceStatusChangeSource.Contagion,
                        "sharing a territory with a Diseased force of another faction",
                        carrier.Id,
                        carrier.FactionId,
                        carrier.ControllerUserId);
                    changed = true;
                }
            }
        }
    }

    /// <summary>
    /// Inflicts Diseased on other factions that fought in the same battle this phase, even after a retreat.
    /// </summary>
    public static void SpreadBattleContagion(
        List<CampaignForce> forces,
        IEnumerable<IReadOnlyList<Guid>> participantGroups,
        SpecialRuleContext rules,
        IDictionary<Guid, Attribution> attributions)
    {
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(participantGroups);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(attributions);
        var groups = participantGroups.Select(static group => group.ToArray()).ToArray();
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var ids in groups)
            {
                var members = ids
                    .Select(id => forces.FirstOrDefault(force => force.Id == id))
                    .OfType<CampaignForce>()
                    .ToArray();
                var carriers = members
                    .Where(static force => ForceStatusNames.IsDiseased(force.StatusName))
                    .OrderBy(static force => force.Id)
                    .ToArray();
                if (carriers.Length == 0)
                {
                    continue;
                }

                for (var index = 0; index < forces.Count; index++)
                {
                    var force = forces[index];
                    if (!ids.Contains(force.Id)
                        || ForceStatusNames.IsDiseased(force.StatusName)
                        || carriers.All(carrier => carrier.FactionId == force.FactionId)
                        || !FactionSpecialRulePolicies.AllowsStatus(force, ForceStatusNames.Diseased, rules))
                    {
                        continue;
                    }

                    var carrier = carriers.First(item => item.FactionId != force.FactionId);
                    forces[index] = force.WithStatus(ForceStatusNames.Diseased);
                    attributions[force.Id] = new Attribution(
                        ForceStatusChangeSource.Contagion,
                        "sharing a battle with a Diseased force of another faction",
                        carrier.Id,
                        carrier.FactionId,
                        carrier.ControllerUserId);
                    changed = true;
                }
            }
        }
    }

    /// <summary>
    /// Applies the first matching mission status-change condition for a won or lost fought battle.
    /// Explicit conditions may replace Diseased; a catch-all still matches Diseased unless a
    /// leave-unchanged row matched first.
    /// </summary>
    public static (CampaignForce Force, Attribution? Attribution) ApplyMission(
        CampaignForce force,
        MissionSetup mission,
        bool won,
        SpecialRuleContext rules)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(mission);
        ArgumentNullException.ThrowIfNull(rules);
        var outcome = won ? MissionBattleOutcome.Win : MissionBattleOutcome.Lose;
        foreach (var change in mission.StatusChanges.Where(item => item.Outcome == outcome))
        {
            if (change.WhenCurrentStatus is { } required
                && !CurrentStatusMatches(force.StatusName, required))
            {
                continue;
            }

            if (change.LeaveUnchanged)
            {
                return (force, null);
            }

            if (change.SetStatus is { } next
                && !FactionSpecialRulePolicies.AllowsStatus(force, next, rules))
            {
                return (force, null);
            }

            var updated = force.WithStatus(change.SetStatus);
            if (string.Equals(updated.StatusName, force.StatusName, StringComparison.Ordinal))
            {
                return (force, null);
            }

            return (updated, new Attribution(
                ForceStatusChangeSource.Mission,
                mission.Name,
                force.Id,
                force.FactionId,
                force.ControllerUserId));
        }

        return (force, null);
    }

    /// <summary>
    /// Assigns a catalog status or Normal. Staff assignment ignores faction immunity.
    /// </summary>
    public static CampaignForce Assign(
        CampaignForce force,
        string? statusName,
        IReadOnlyList<ForceStatusSetup> catalog)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(catalog);
        if (ForceStatusNames.IsNormal(statusName))
        {
            return force.WithStatus(null);
        }

        var match = catalog.FirstOrDefault(status =>
            string.Equals(status.Name, statusName, StringComparison.OrdinalIgnoreCase));
        return match is null ? force : force.WithStatus(match.Name);
    }

    /// <summary>
    /// Applies a configured status name from a mission, item, or similar source. Empty names do
    /// nothing. Normal clears the status. Immune factions refuse named statuses other than Normal.
    /// </summary>
    public static (CampaignForce Force, Attribution? Attribution) ApplyConfiguredStatus(
        CampaignForce force,
        string? statusName,
        IReadOnlyList<ForceStatusSetup> catalog,
        SpecialRuleContext rules,
        ForceStatusChangeSource source,
        string? detail)
    {
        ArgumentNullException.ThrowIfNull(force);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(rules);
        if (string.IsNullOrWhiteSpace(statusName))
        {
            return (force, null);
        }

        var assigned = Assign(force, statusName, catalog);
        if (assigned.StatusName is { } next
            && !FactionSpecialRulePolicies.AllowsStatus(assigned, next, rules))
        {
            return (force, null);
        }

        if (string.Equals(assigned.StatusName, force.StatusName, StringComparison.Ordinal))
        {
            return (force, null);
        }

        return (assigned, new Attribution(source, detail, force.Id, force.FactionId, force.ControllerUserId));
    }

    /// <summary>
    /// Records public log lines and private-objective facts for forces whose status name changed.
    /// </summary>
    public static (List<ForceStatusChangeFact> Facts, List<PlayLogEntry> Log) RecordChanges(
        IReadOnlyList<CampaignForce> before,
        IReadOnlyList<CampaignForce> after,
        IReadOnlyList<ForceStatusSetup> catalog,
        DateTimeOffset utcNow,
        IReadOnlyDictionary<Guid, Attribution> attributions,
        Guid? windowId)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(attributions);
        var byName = catalog.ToDictionary(static status => status.Name, static status => status.Id, StringComparer.OrdinalIgnoreCase);
        var previousById = before.ToDictionary(static force => force.Id);
        var facts = new List<ForceStatusChangeFact>();
        var log = new List<PlayLogEntry>();
        foreach (var next in after.OrderBy(static force => force.Id))
        {
            if (!previousById.TryGetValue(next.Id, out var previous)
                || string.Equals(previous.StatusName, next.StatusName, StringComparison.Ordinal))
            {
                continue;
            }

            attributions.TryGetValue(next.Id, out var attribution);
            var actorForceId = attribution.ActorForceId ?? next.Id;
            var actorFactionId = attribution.ActorFactionId ?? next.FactionId;
            var actorUserId = attribution.ActorUserId ?? next.ControllerUserId;
            var source = attributions.ContainsKey(next.Id) ? attribution.Source : ForceStatusChangeSource.Catalog;
            facts.Add(new ForceStatusChangeFact(
                Guid.NewGuid(),
                next.Id,
                next.FactionId,
                next.ControllerUserId,
                next.StatusName is { } name && byName.TryGetValue(name, out var statusId) ? statusId : null,
                previous.StatusName,
                next.StatusName,
                actorForceId,
                actorFactionId,
                actorUserId,
                utcNow,
                previous.StatusName is { } previousName && byName.TryGetValue(previousName, out var previousId)
                    ? previousId
                    : null,
                source,
                attribution.Detail));
            log.Add(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.ForceStatusChanged,
                windowId,
                next.Id,
                actorUserId,
                next.TerritoryId,
                targetTerritoryId: null,
                battleId: null,
                actionKind: null,
                relatedForceIds: actorForceId == next.Id ? [next.Id] : [next.Id, actorForceId],
                message: DescribeChange(
                    previous.StatusName,
                    next.StatusName,
                    source,
                    attribution.Detail)));
        }

        return (facts, log);
    }

    /// <summary>
    /// Builds a public log sentence for a status change.
    /// </summary>
    public static string DescribeChange(
        string? previousStatusName,
        string? nextStatusName,
        ForceStatusChangeSource source,
        string? detail)
    {
        var from = previousStatusName ?? "Normal";
        var to = nextStatusName ?? "Normal";
        var reason = source switch
        {
            ForceStatusChangeSource.ConsecutiveWater =>
                "three consecutive actions in water-feature territories",
            ForceStatusChangeSource.WaterBattleDefeat =>
                "defeat in a battle on a water-feature territory",
            ForceStatusChangeSource.WaterSurrender =>
                "surrender after two consecutive water-feature actions",
            ForceStatusChangeSource.Contagion =>
                detail ?? "sharing a territory with a Diseased force of another faction",
            ForceStatusChangeSource.Rejoin =>
                "rejoining a split force that was Diseased",
            ForceStatusChangeSource.SpecialRule =>
                detail ?? "a special rule",
            ForceStatusChangeSource.Mission =>
                string.IsNullOrWhiteSpace(detail) ? "a mission result" : $"mission {detail}",
            ForceStatusChangeSource.ItemObjective =>
                string.IsNullOrWhiteSpace(detail) ? "an item-objective choice" : $"item objective {detail}",
            ForceStatusChangeSource.Staff =>
                "a manager or administrator assignment",
            ForceStatusChangeSource.SettlementHold =>
                "Hold at a Capital City, City, Supply Depot, or Town",
            _ => detail ?? "a catalog status trigger",
        };
        return $"{from} became {to} ({reason}).";
    }

    /// <summary>
    /// Builds action-window facts from the latest submission and the force's territory.
    /// </summary>
    public static Facts FromAction(
        ActionKind? kind,
        bool occupiesWater,
        bool occupiesCureSettlement = false,
        bool skipActionResolution = false)
    {
        var held = kind is null or ActionKind.Hold;
        var moved = kind is ActionKind.Move or ActionKind.Split or ActionKind.Retreat;
        return new Facts(
            held,
            moved,
            false,
            false,
            false,
            false,
            occupiesWater,
            occupiesCureSettlement,
            surrendered: false,
            lostOnWater: false,
            skipActionResolution);
    }

    /// <summary>
    /// Builds battle-window facts from finalized engagements and retreats.
    /// </summary>
    public static Facts FromBattle(
        bool fought,
        bool won,
        bool lost,
        bool retreated,
        bool occupiesWater,
        bool surrendered = false,
        bool lostOnWater = false)
    {
        return new Facts(
            false,
            false,
            fought,
            won,
            lost,
            retreated,
            occupiesWater,
            occupiesCureSettlement: false,
            surrendered,
            lostOnWater,
            skipActionResolution: false,
            updateWaterStreak: false);
    }

    /// <summary>
    /// Returns whether a Hold on this territory can clear Diseased.
    /// </summary>
    public static bool CanCureDisease(PlayTerritory? territory)
    {
        if (territory is null
            || territory.StructureTypeId is null
            || territory.StructureCondition == StructureCondition.Destroyed)
        {
            return false;
        }

        return StructureKinds.CanCureDisease(territory.StructureName);
    }

    private static (string? Status, Attribution? Attribution) ResolveForce(
        CampaignForce force,
        IReadOnlyList<ForceStatusSetup> statuses,
        Dictionary<string, ForceStatusSetup> catalogByName,
        Facts facts,
        bool hasDisease,
        SpecialRuleContext rules)
    {
        var diseased = ForceStatusNames.IsDiseased(force.StatusName);
        if (hasDisease && diseased && facts.Held && facts.OccupiesCureSettlement)
        {
            return (null, new Attribution(ForceStatusChangeSource.SettlementHold));
        }

        if (hasDisease && diseased)
        {
            return (ForceStatusNames.Diseased, null);
        }

        string? next = force.StatusName;
        Attribution? attribution = null;
        var current = force.StatusName is { } name && catalogByName.TryGetValue(name, out var status)
            && !ForceStatusNames.IsDiseased(status.Name)
            ? status
            : null;
        if (current is not null && MatchesClear(current.ClearTrigger, facts))
        {
            next = null;
            attribution = new Attribution(ForceStatusChangeSource.Catalog, current.Name);
        }

        if (next is not null && !FactionSpecialRulePolicies.AllowsStatus(force, next, rules))
        {
            next = null;
        }

        if (next is null)
        {
            foreach (var candidate in statuses)
            {
                if (ForceStatusNames.IsDiseased(candidate.Name)
                    || !MatchesEnable(candidate.EnableTrigger, facts)
                    || !FactionSpecialRulePolicies.AllowsStatus(force, candidate.Name, rules))
                {
                    continue;
                }

                next = candidate.Name;
                attribution = new Attribution(ForceStatusChangeSource.Catalog, candidate.Name);
                break;
            }
        }

        if (hasDisease
            && FactionSpecialRulePolicies.AllowsStatus(force, ForceStatusNames.Diseased, rules)
            && TryInfect(force, facts, out var diseaseSource))
        {
            return (ForceStatusNames.Diseased, diseaseSource);
        }

        return (next, attribution);
    }

    private static bool TryInfect(CampaignForce force, Facts facts, out Attribution attribution)
    {
        if (facts.LostOnWater)
        {
            attribution = new Attribution(ForceStatusChangeSource.WaterBattleDefeat);
            return true;
        }

        if (facts.Surrendered && force.ConsecutiveWaterActions >= ConsecutiveWaterActionsToInfectOnSurrender)
        {
            attribution = new Attribution(ForceStatusChangeSource.WaterSurrender);
            return true;
        }

        if (facts.UpdateWaterStreak && force.ConsecutiveWaterActions >= ConsecutiveWaterActionsToInfect)
        {
            attribution = new Attribution(ForceStatusChangeSource.ConsecutiveWater);
            return true;
        }

        attribution = default;
        return false;
    }

    private static bool CurrentStatusMatches(string? current, string required)
    {
        if (ForceStatusNames.IsNormal(required))
        {
            return ForceStatusNames.IsNormal(current);
        }

        return string.Equals(current, required, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesEnable(ForceStatusEnableTrigger trigger, Facts facts)
    {
        return trigger switch
        {
            ForceStatusEnableTrigger.Hold => facts.Held,
            ForceStatusEnableTrigger.AfterBattle => facts.FoughtBattle,
            ForceStatusEnableTrigger.BattleWon => facts.Won,
            ForceStatusEnableTrigger.BattleLostOrRetreat => facts.Lost || facts.Retreated,
            ForceStatusEnableTrigger.OccupyingWater => facts.OccupiesWater,
            ForceStatusEnableTrigger.Disease => false,
            _ => false,
        };
    }

    private static bool MatchesClear(ForceStatusClearTrigger trigger, Facts facts)
    {
        return trigger switch
        {
            ForceStatusClearTrigger.Hold => facts.Held,
            ForceStatusClearTrigger.AfterMove => facts.Moved,
            ForceStatusClearTrigger.AfterBattle => facts.FoughtBattle,
            ForceStatusClearTrigger.AfterMoveOrBattle => facts.Moved || facts.FoughtBattle,
            ForceStatusClearTrigger.BattleWon => facts.Won,
            ForceStatusClearTrigger.BattleLostOrRetreat => facts.Lost || facts.Retreated,
            ForceStatusClearTrigger.HoldWhileNotWater => facts.Held && !facts.OccupiesWater,
            ForceStatusClearTrigger.HoldAtSettlement => facts.Held && facts.OccupiesCureSettlement,
            _ => false,
        };
    }
}
