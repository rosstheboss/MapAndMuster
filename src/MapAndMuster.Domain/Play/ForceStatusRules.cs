using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Applies configured force-status enable and clear triggers, then the named Diseased engine rules.
/// A force has at most one status; Normal is stored as no status name. Enable and clear counts
/// require that many matching triggers in a row. When more than one status would apply, the lowest
/// unique priority number wins. A status may cancel another to Normal.
/// Staff assignment may set any catalog status; mission and item gains use priority and cancel-out.
/// </summary>
public static class ForceStatusRules
{
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
            bool updateWaterStreak = true,
            Guid? terrainTypeId = null,
            IReadOnlyList<Guid>? terrainTagIds = null,
            Guid? structureTypeId = null,
            IReadOnlyList<Guid>? structureTagIds = null,
            bool built = false,
            bool pillaged = false,
            bool repaired = false,
            bool destroyed = false)
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
            TerrainTypeId = terrainTypeId;
            TerrainTagIds = terrainTagIds ?? [];
            StructureTypeId = structureTypeId;
            StructureTagIds = structureTagIds ?? [];
            Built = built;
            Pillaged = pillaged;
            Repaired = repaired;
            Destroyed = destroyed;
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

        /// <summary>Gets whether this pass counts as a consecutive occupying action phase.</summary>
        public bool UpdateWaterStreak { get; }

        /// <summary>Gets the current or battle terrain type.</summary>
        public Guid? TerrainTypeId { get; }

        /// <summary>Gets terrain-catalog tags for the current or battle territory.</summary>
        public IReadOnlyList<Guid> TerrainTagIds { get; }

        /// <summary>Gets the current structure type when it is not destroyed.</summary>
        public Guid? StructureTypeId { get; }

        /// <summary>Gets structure-catalog tags for the current structure when it is not destroyed.</summary>
        public IReadOnlyList<Guid> StructureTagIds { get; }

        /// <summary>Gets whether the force successfully Built this action phase.</summary>
        public bool Built { get; }

        /// <summary>Gets whether the force successfully Pillaged without destroying this action phase.</summary>
        public bool Pillaged { get; }

        /// <summary>Gets whether the force successfully Repaired this action phase.</summary>
        public bool Repaired { get; }

        /// <summary>Gets whether the force successfully destroyed a structure this action phase.</summary>
        public bool Destroyed { get; }

        /// <summary>Gets whether this resolution is an action phase rather than a battle window.</summary>
        public bool IsActionPhase => UpdateWaterStreak;
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
    /// their clear trigger remain candidates. Newly enabled statuses are candidates too. Cancel-out
    /// pairs become Normal; otherwise the lowest priority number wins.
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
            var enableStreaks = NextEnableStreaks(force, statuses, facts);
            var clearStreaks = NextClearStreaks(force, statuses, facts);
            var withStreak = force.With(
                consecutiveWaterActions: streak,
                enableStreaks: enableStreaks,
                clearStreaks: clearStreaks,
                clearStreak: 0);
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
                rules);
            var updated = withStreak.WithStatus(status);
            if (!string.Equals(updated.StatusName, withStreak.StatusName, StringComparison.Ordinal))
            {
                var remainingStreaks = enableStreaks
                    .Where(pair => KeepEnableStreak(pair.Key, pair.Value, status, statuses))
                    .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
                updated = updated.With(enableStreaks: remainingStreaks, clearStreaks: new Dictionary<string, int>(), clearStreak: 0);
            }

            if (attribution is { } assigned
                && !string.Equals(updated.StatusName, force.StatusName, StringComparison.Ordinal))
            {
                attributions[updated.Id] = assigned;
            }

            next.Add(updated);
        }

        if (hasDisease)
        {
            SpreadContagion(next, statuses, rules, attributions);
        }

        return new Application(next, attributions);
    }

    /// <summary>
    /// Inflicts Diseased on other factions sharing a territory with a Diseased force, subject to
    /// priority and cancel-out.
    /// </summary>
    public static void SpreadContagion(
        List<CampaignForce> forces,
        IReadOnlyList<ForceStatusSetup> statuses,
        SpecialRuleContext rules,
        IDictionary<Guid, Attribution> attributions)
    {
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(statuses);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(attributions);
        var considered = new HashSet<Guid>();
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
                        || considered.Contains(force.Id)
                        || ForceStatusNames.IsDiseased(force.StatusName)
                        || carriers.All(carrier => carrier.FactionId == force.FactionId))
                    {
                        continue;
                    }

                    var carrier = carriers.First(item => item.FactionId != force.FactionId);
                    considered.Add(force.Id);
                    if (TryApplyDiseasedGain(
                        forces,
                        index,
                        carrier,
                        statuses,
                        rules,
                        attributions,
                        "sharing a territory with a Diseased force of another faction"))
                    {
                        changed = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Inflicts Diseased on other factions that fought in the same battle this phase, even after a retreat,
    /// subject to priority and cancel-out.
    /// </summary>
    public static void SpreadBattleContagion(
        List<CampaignForce> forces,
        IEnumerable<IReadOnlyList<Guid>> participantGroups,
        IReadOnlyList<ForceStatusSetup> statuses,
        SpecialRuleContext rules,
        IDictionary<Guid, Attribution> attributions)
    {
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(participantGroups);
        ArgumentNullException.ThrowIfNull(statuses);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(attributions);
        var groups = participantGroups.Select(static group => group.ToArray()).ToArray();
        var considered = new HashSet<Guid>();
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
                        || considered.Contains(force.Id)
                        || ForceStatusNames.IsDiseased(force.StatusName)
                        || carriers.All(carrier => carrier.FactionId == force.FactionId))
                    {
                        continue;
                    }

                    var carrier = carriers.First(item => item.FactionId != force.FactionId);
                    considered.Add(force.Id);
                    if (TryApplyDiseasedGain(
                        forces,
                        index,
                        carrier,
                        statuses,
                        rules,
                        attributions,
                        "sharing a battle with a Diseased force of another faction"))
                    {
                        changed = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies the first matching mission status-change condition for a won or lost fought battle.
    /// Explicit Normal clears the status. Named gains use priority and cancel-out when a catalog is
    /// supplied. A leave-unchanged row keeps the current status, including Diseased.
    /// </summary>
    public static (CampaignForce Force, Attribution? Attribution) ApplyMission(
        CampaignForce force,
        MissionSetup mission,
        bool won,
        SpecialRuleContext rules,
        IReadOnlyList<ForceStatusSetup>? catalog = null)
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

            if (catalog is { Count: > 0 })
            {
                return ApplyConfiguredStatus(
                    force,
                    change.SetStatus ?? "Normal",
                    catalog,
                    rules,
                    ForceStatusChangeSource.Mission,
                    mission.Name);
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
    /// nothing. Normal clears the status. Named gains use priority and cancel-out. Immune factions
    /// refuse named statuses other than Normal.
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

        if (ForceStatusNames.IsNormal(statusName))
        {
            var cleared = force.WithStatus(null);
            if (string.Equals(cleared.StatusName, force.StatusName, StringComparison.Ordinal))
            {
                return (force, null);
            }

            return (cleared, new Attribution(source, detail, force.Id, force.FactionId, force.ControllerUserId));
        }

        var match = catalog.FirstOrDefault(status =>
            string.Equals(status.Name, statusName, StringComparison.OrdinalIgnoreCase));
        if (match is null || !FactionSpecialRulePolicies.AllowsStatus(force, match.Name, rules))
        {
            return (force, null);
        }

        var nextName = ResolveGain(force.StatusName, match, catalog);
        if (string.Equals(nextName, force.StatusName, StringComparison.Ordinal))
        {
            return (force, null);
        }

        return (force.WithStatus(nextName), new Attribution(source, detail, force.Id, force.FactionId, force.ControllerUserId));
    }

    /// <summary>
    /// Resolves gaining <paramref name="incoming"/> while the force has <paramref name="currentName"/>.
    /// Cancel-out yields Normal. Otherwise the lower priority number remains.
    /// </summary>
    public static string? ResolveGain(
        string? currentName,
        ForceStatusSetup incoming,
        IReadOnlyList<ForceStatusSetup> catalog)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(catalog);
        var current = currentName is { } name
            ? catalog.FirstOrDefault(status => string.Equals(status.Name, name, StringComparison.OrdinalIgnoreCase))
            : null;
        if (current is not null && incoming.CancelsStatusIds.Contains(current.Id))
        {
            return null;
        }

        if (current is not null && current.Priority <= incoming.Priority)
        {
            return current.Name;
        }

        return incoming.Name;
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
        bool skipActionResolution = false,
        PlayTerritory? territory = null,
        bool built = false,
        bool pillaged = false,
        bool repaired = false,
        bool destroyed = false)
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
            skipActionResolution,
            updateWaterStreak: true,
            territory?.TerrainTypeId,
            territory?.TerrainTagIds,
            destroyed ? null : territory?.StructureTypeId,
            destroyed ? [] : territory?.StructureTagIds,
            built,
            pillaged,
            repaired,
            destroyed);
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
        bool lostOnWater = false,
        PlayTerritory? territory = null)
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
            updateWaterStreak: false,
            territory?.TerrainTypeId,
            territory?.TerrainTagIds,
            territory?.StructureTypeId,
            territory?.StructureTagIds);
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

    private static bool TryApplyDiseasedGain(
        List<CampaignForce> forces,
        int index,
        CampaignForce carrier,
        IReadOnlyList<ForceStatusSetup> statuses,
        SpecialRuleContext rules,
        IDictionary<Guid, Attribution> attributions,
        string detail)
    {
        var force = forces[index];
        var diseased = statuses.FirstOrDefault(static status => ForceStatusNames.IsDiseased(status.Name));
        if (diseased is null || !FactionSpecialRulePolicies.AllowsStatus(force, ForceStatusNames.Diseased, rules))
        {
            return false;
        }

        var nextName = ResolveGain(force.StatusName, diseased, statuses);
        if (string.Equals(nextName, force.StatusName, StringComparison.Ordinal))
        {
            return false;
        }

        forces[index] = force.WithStatus(nextName);
        attributions[force.Id] = new Attribution(
            ForceStatusChangeSource.Contagion,
            detail,
            carrier.Id,
            carrier.FactionId,
            carrier.ControllerUserId);
        return true;
    }

    private static (string? Status, Attribution? Attribution) ResolveForce(
        CampaignForce force,
        IReadOnlyList<ForceStatusSetup> statuses,
        Dictionary<string, ForceStatusSetup> catalogByName,
        Facts facts,
        SpecialRuleContext rules)
    {
        var starting = force.StatusName is { } name && catalogByName.TryGetValue(name, out var startingSetup)
            ? startingSetup
            : null;
        var startingCleared = starting is not null && starting.ClearConditions.Any(condition =>
            ShouldEvaluateClear(condition.Trigger, facts)
            && MatchesClear(condition, facts)
            && EnableStreakValue(
                force.ClearStreaks,
                ForceStatusStreakKeys.Clear(condition.Id),
                force.ClearStreak) >= condition.Occurrences);
        var incoming = new List<ForceStatusSetup>();
        foreach (var candidate in statuses)
        {
            if (!candidate.EnableConditions.Any(condition => IsEnableReady(force, candidate, condition, facts))
                || !FactionSpecialRulePolicies.AllowsStatus(force, candidate.Name, rules))
            {
                continue;
            }

            incoming.Add(candidate);
        }

        var cancelled = new HashSet<Guid>();
        if (starting is not null)
        {
            foreach (var candidate in incoming)
            {
                if (!candidate.CancelsStatusIds.Contains(starting.Id))
                {
                    continue;
                }

                cancelled.Add(candidate.Id);
                cancelled.Add(starting.Id);
            }
        }

        for (var left = 0; left < incoming.Count; left++)
        {
            for (var right = 0; right < incoming.Count; right++)
            {
                if (left == right || !incoming[left].CancelsStatusIds.Contains(incoming[right].Id))
                {
                    continue;
                }

                cancelled.Add(incoming[left].Id);
                cancelled.Add(incoming[right].Id);
            }
        }

        var remaining = new List<ForceStatusSetup>();
        if (starting is not null
            && !startingCleared
            && !cancelled.Contains(starting.Id)
            && FactionSpecialRulePolicies.AllowsStatus(force, starting.Name, rules))
        {
            remaining.Add(starting);
        }

        foreach (var candidate in incoming)
        {
            if (!cancelled.Contains(candidate.Id))
            {
                remaining.Add(candidate);
            }
        }

        remaining = [.. remaining.DistinctBy(static status => status.Id)];
        if (remaining.Count == 0)
        {
            return starting is null
                ? (null, null)
                : (null, new Attribution(ForceStatusChangeSource.Catalog, starting.Name));
        }

        var winner = remaining
            .OrderBy(static status => status.Priority)
            .ThenBy(static status => status.Id)
            .First();
        if (string.Equals(winner.Name, force.StatusName, StringComparison.Ordinal))
        {
            return (winner.Name, null);
        }

        return (winner.Name, new Attribution(ForceStatusChangeSource.Catalog, winner.Name));
    }

    private static bool IsEnableReady(
        CampaignForce force,
        ForceStatusSetup status,
        ForceStatusEnableCondition condition,
        Facts facts)
    {
        if (condition.Trigger == ForceStatusEnableTrigger.Surrender)
        {
            return facts.Surrendered
                && MatchesLocation(condition.Location, facts)
                && EnableStreakValue(
                    force.EnableStreaks,
                    ForceStatusStreakKeys.Enable(status.Id, condition.Id),
                    LegacyEnableStreak(force, status.Id)) >= condition.Occurrences;
        }

        return ShouldEvaluateEnable(condition.Trigger, facts)
            && MatchesEnable(condition, facts)
            && EnableStreakValue(
                force.EnableStreaks,
                ForceStatusStreakKeys.Enable(status.Id, condition.Id),
                LegacyEnableStreak(force, status.Id)) >= condition.Occurrences;
    }

    private static int LegacyEnableStreak(CampaignForce force, Guid statusId)
    {
        return force.EnableStreaks.GetValueOrDefault(statusId.ToString("D"));
    }

    private static int EnableStreakValue(IReadOnlyDictionary<string, int> streaks, string key, int legacy)
    {
        if (streaks.TryGetValue(key, out var value))
        {
            return value;
        }

        return legacy;
    }

    private static bool KeepEnableStreak(
        string key,
        int streak,
        string? gained,
        IReadOnlyList<ForceStatusSetup> statuses)
    {
        if (ForceStatusStreakKeys.TryParseEnable(key, out var statusId, out var conditionId))
        {
            var setup = statuses.FirstOrDefault(candidate => candidate.Id == statusId);
            if (setup is null)
            {
                return false;
            }

            if (gained is not null && string.Equals(setup.Name, gained, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var condition = setup.EnableConditions.FirstOrDefault(item => item.Id == conditionId);
            return condition is not null && streak < condition.Occurrences;
        }

        if (!Guid.TryParse(key, out statusId))
        {
            return false;
        }

        var legacy = statuses.FirstOrDefault(candidate => candidate.Id == statusId);
        if (legacy is null)
        {
            return false;
        }

        if (gained is not null && string.Equals(legacy.Name, gained, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return streak < legacy.EnableConditions.Min(static condition => condition.Occurrences);
    }

    private static Dictionary<string, int> NextEnableStreaks(
        CampaignForce force,
        IReadOnlyList<ForceStatusSetup> statuses,
        Facts facts)
    {
        var next = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var status in statuses)
        {
            var legacy = LegacyEnableStreak(force, status.Id);
            foreach (var condition in status.EnableConditions)
            {
                var key = ForceStatusStreakKeys.Enable(status.Id, condition.Id);
                var previous = EnableStreakValue(force.EnableStreaks, key, legacy);
                if (TracksOccupying(condition.Trigger))
                {
                    if (!facts.IsActionPhase)
                    {
                        if (previous > 0)
                        {
                            next[key] = previous;
                        }

                        continue;
                    }

                    var occupying = OccupyingMatchesEnable(condition, facts);
                    var streak = occupying ? Math.Min(previous + 1, ForceStatusOccurrences.Max) : 0;
                    if (streak > 0)
                    {
                        next[key] = streak;
                    }

                    continue;
                }

                if (!ShouldEvaluateEnable(condition.Trigger, facts))
                {
                    if (previous > 0)
                    {
                        next[key] = previous;
                    }

                    continue;
                }

                var battleStreak = MatchesEnable(condition, facts)
                    ? Math.Min(previous + 1, ForceStatusOccurrences.Max)
                    : 0;
                if (battleStreak > 0)
                {
                    next[key] = battleStreak;
                }
            }
        }

        return next;
    }

    private static Dictionary<string, int> NextClearStreaks(
        CampaignForce force,
        IReadOnlyList<ForceStatusSetup> statuses,
        Facts facts)
    {
        var next = new Dictionary<string, int>(StringComparer.Ordinal);
        if (force.StatusName is not { } name)
        {
            return next;
        }

        var starting = statuses.FirstOrDefault(status =>
            string.Equals(status.Name, name, StringComparison.OrdinalIgnoreCase));
        if (starting is null)
        {
            return next;
        }

        var legacy = force.ClearStreaks.Count == 0 ? force.ClearStreak : 0;
        foreach (var condition in starting.ClearConditions)
        {
            var key = ForceStatusStreakKeys.Clear(condition.Id);
            var previous = EnableStreakValue(force.ClearStreaks, key, legacy);
            if (TracksOccupying(condition.Trigger))
            {
                if (!facts.IsActionPhase)
                {
                    if (previous > 0)
                    {
                        next[key] = previous;
                    }

                    continue;
                }

                var occupying = MatchesLocation(condition.Location, facts)
                    && MatchesClearTrigger(condition.Trigger, facts);
                var occupyingStreak = occupying ? Math.Min(previous + 1, ForceStatusOccurrences.Max) : 0;
                if (occupyingStreak > 0)
                {
                    next[key] = occupyingStreak;
                }

                continue;
            }

            if (!ShouldEvaluateClear(condition.Trigger, facts))
            {
                if (previous > 0)
                {
                    next[key] = previous;
                }

                continue;
            }

            var streak = MatchesClear(condition, facts)
                ? Math.Min(previous + 1, ForceStatusOccurrences.Max)
                : 0;
            if (streak > 0)
            {
                next[key] = streak;
            }
        }

        return next;
    }

    private static bool ShouldEvaluateEnable(ForceStatusEnableTrigger trigger, Facts facts)
    {
        if (facts.SkipActionResolution)
        {
            return false;
        }

        return trigger switch
        {
            ForceStatusEnableTrigger.Hold
                or ForceStatusEnableTrigger.OccupyingWater
                or ForceStatusEnableTrigger.ConsecutiveActions
                or ForceStatusEnableTrigger.Build
                or ForceStatusEnableTrigger.Pillage
                or ForceStatusEnableTrigger.Repair
                or ForceStatusEnableTrigger.Destroy
                or ForceStatusEnableTrigger.Surrender => facts.UpdateWaterStreak,
            ForceStatusEnableTrigger.AfterBattle
                or ForceStatusEnableTrigger.BattleWon
                or ForceStatusEnableTrigger.BattleLostOrRetreat => !facts.UpdateWaterStreak,
            _ => false,
        };
    }

    private static bool ShouldEvaluateClear(ForceStatusClearTrigger trigger, Facts facts)
    {
        if (facts.SkipActionResolution)
        {
            return false;
        }

        return trigger switch
        {
            ForceStatusClearTrigger.Hold
                or ForceStatusClearTrigger.AfterMove
                or ForceStatusClearTrigger.HoldWhileNotWater
                or ForceStatusClearTrigger.HoldAtSettlement
                or ForceStatusClearTrigger.ConsecutiveActions
                or ForceStatusClearTrigger.Build
                or ForceStatusClearTrigger.Pillage
                or ForceStatusClearTrigger.Repair
                or ForceStatusClearTrigger.Destroy
                or ForceStatusClearTrigger.Surrender => facts.UpdateWaterStreak,
            ForceStatusClearTrigger.AfterBattle
                or ForceStatusClearTrigger.BattleWon
                or ForceStatusClearTrigger.BattleLostOrRetreat => !facts.UpdateWaterStreak,
            ForceStatusClearTrigger.AfterMoveOrBattle => true,
            _ => false,
        };
    }

    private static bool TracksOccupying(ForceStatusEnableTrigger trigger)
    {
        return trigger is ForceStatusEnableTrigger.ConsecutiveActions
            or ForceStatusEnableTrigger.OccupyingWater
            or ForceStatusEnableTrigger.Surrender;
    }

    private static bool TracksOccupying(ForceStatusClearTrigger trigger)
    {
        return trigger is ForceStatusClearTrigger.ConsecutiveActions
            or ForceStatusClearTrigger.Surrender;
    }

    private static bool CurrentStatusMatches(string? current, string required)
    {
        if (ForceStatusNames.IsNormal(required))
        {
            return ForceStatusNames.IsNormal(current);
        }

        return string.Equals(current, required, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesEnable(ForceStatusEnableCondition condition, Facts facts)
    {
        return MatchesEnableTrigger(condition.Trigger, facts) && MatchesLocation(condition.Location, facts);
    }

    private static bool MatchesClear(ForceStatusClearCondition condition, Facts facts)
    {
        return MatchesClearTrigger(condition.Trigger, facts) && MatchesLocation(condition.Location, facts);
    }

    private static bool OccupyingMatchesEnable(ForceStatusEnableCondition condition, Facts facts)
    {
        if (!MatchesLocation(condition.Location, facts))
        {
            return false;
        }

        return condition.Trigger != ForceStatusEnableTrigger.OccupyingWater || facts.OccupiesWater;
    }

    private static bool MatchesLocation(ConditionLocation location, Facts facts)
    {
        if (location.Kind == ConditionLocationKind.TerrainTag
            && location.TagId is not null
            && facts.TerrainTagIds.Count == 0
            && (facts.OccupiesWater || facts.LostOnWater))
        {
            return true;
        }

        return location.Matches(facts.TerrainTypeId, facts.TerrainTagIds, facts.StructureTypeId, facts.StructureTagIds);
    }

    private static bool MatchesEnableTrigger(ForceStatusEnableTrigger trigger, Facts facts)
    {
        return trigger switch
        {
            ForceStatusEnableTrigger.Hold => facts.Held,
            ForceStatusEnableTrigger.AfterBattle => facts.FoughtBattle,
            ForceStatusEnableTrigger.BattleWon => facts.Won,
            ForceStatusEnableTrigger.BattleLostOrRetreat => facts.Lost || facts.Retreated || facts.LostOnWater,
            ForceStatusEnableTrigger.OccupyingWater => facts.OccupiesWater,
            ForceStatusEnableTrigger.ConsecutiveActions => facts.IsActionPhase,
            ForceStatusEnableTrigger.Surrender => facts.Surrendered,
            ForceStatusEnableTrigger.Build => facts.Built,
            ForceStatusEnableTrigger.Pillage => facts.Pillaged,
            ForceStatusEnableTrigger.Repair => facts.Repaired,
            ForceStatusEnableTrigger.Destroy => facts.Destroyed,
            ForceStatusEnableTrigger.Disease => false,
            _ => false,
        };
    }

    private static bool MatchesClearTrigger(ForceStatusClearTrigger trigger, Facts facts)
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
            ForceStatusClearTrigger.ConsecutiveActions => facts.IsActionPhase,
            ForceStatusClearTrigger.Surrender => facts.Surrendered,
            ForceStatusClearTrigger.Build => facts.Built,
            ForceStatusClearTrigger.Pillage => facts.Pillaged,
            ForceStatusClearTrigger.Repair => facts.Repaired,
            ForceStatusClearTrigger.Destroy => facts.Destroyed,
            _ => false,
        };
    }
}
