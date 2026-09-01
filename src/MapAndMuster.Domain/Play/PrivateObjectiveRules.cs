using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Catalog facts used to assign and score private objectives.
/// </summary>
public sealed class PrivateObjectiveTypePlayRules
{
    /// <summary>
    /// Initializes private-objective play rules.
    /// </summary>
    public PrivateObjectiveTypePlayRules(
        Guid id,
        string name,
        int campaignPoints,
        IReadOnlyList<PrivateObjectiveHolderKind> allowedHolderKinds,
        PrivateObjectiveScoringKind scoringKind,
        PrivateObjectiveAutomaticKind automaticKind,
        int requiredCount,
        Guid? structureTypeId,
        IReadOnlyList<Guid> territoryIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(allowedHolderKinds);
        ArgumentNullException.ThrowIfNull(territoryIds);
        Id = id;
        Name = name;
        CampaignPoints = campaignPoints;
        AllowedHolderKinds = allowedHolderKinds;
        ScoringKind = scoringKind;
        AutomaticKind = automaticKind;
        RequiredCount = requiredCount;
        StructureTypeId = structureTypeId;
        TerritoryIds = territoryIds;
    }

    /// <summary>Gets the catalog identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the objective name.</summary>
    public string Name { get; }

    /// <summary>Gets campaign points awarded when revealed or completed.</summary>
    public int CampaignPoints { get; }

    /// <summary>Gets holder kinds this entry may be assigned to.</summary>
    public IReadOnlyList<PrivateObjectiveHolderKind> AllowedHolderKinds { get; }

    /// <summary>Gets whether scoring is manual or automatic.</summary>
    public PrivateObjectiveScoringKind ScoringKind { get; }

    /// <summary>Gets the automatic criterion kind.</summary>
    public PrivateObjectiveAutomaticKind AutomaticKind { get; }

    /// <summary>Gets how many matching facts complete an automatic objective.</summary>
    public int RequiredCount { get; }

    /// <summary>Gets the structure type for structure-based automatic criteria.</summary>
    public Guid? StructureTypeId { get; }

    /// <summary>Gets named territories for ControlNamedTerritories.</summary>
    public IReadOnlyList<Guid> TerritoryIds { get; }

    /// <summary>Gets whether this catalog entry may be assigned to <paramref name="kind"/>.</summary>
    public bool Allows(PrivateObjectiveHolderKind kind)
    {
        return AllowedHolderKinds.Contains(kind);
    }
}

/// <summary>
/// One currently controlled territory used to evaluate automatic private objectives.
/// </summary>
public readonly record struct PrivateObjectiveTerritory(
    Guid TerritoryId,
    Guid? OwnerFactionId,
    Guid? StructureTypeId,
    StructureCondition StructureCondition);

/// <summary>
/// Assigns, claims, approves, and automatically completes private objectives.
/// </summary>
public static class PrivateObjectiveRules
{
    /// <summary>
    /// Assigns one secret catalog objective to each occupying player, faction, and ally group at launch.
    /// Empty holder-kind pools are skipped. Unique draws are used first; the pool is reshuffled for
    /// remaining holders so everyone in a non-empty pool still receives an independent assignment.
    /// </summary>
    public static IReadOnlyList<PrivateObjectiveAssignment> SeedInitial(
        IReadOnlyList<PrivateObjectiveTypePlayRules> types,
        IReadOnlyList<Guid> playerUserIds,
        IReadOnlyList<Guid> factionIds,
        IReadOnlyList<Guid> allyGroupIds,
        DateTimeOffset utcNow,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(playerUserIds);
        ArgumentNullException.ThrowIfNull(factionIds);
        ArgumentNullException.ThrowIfNull(allyGroupIds);
        ArgumentNullException.ThrowIfNull(pickIndex);

        var assigned = new List<PrivateObjectiveAssignment>();
        AssignGroup(assigned, types, PrivateObjectiveHolderKind.Player, playerUserIds, utcNow, pickIndex);
        AssignGroup(assigned, types, PrivateObjectiveHolderKind.Faction, factionIds, utcNow, pickIndex);
        AssignGroup(assigned, types, PrivateObjectiveHolderKind.AllyGroup, allyGroupIds, utcNow, pickIndex);
        return assigned;
    }

    /// <summary>
    /// Grants one player-pool catalog objective to a late-joining player when they have none.
    /// Prefers a still-unique type in that pool, then a reshuffled duplicate when the pool is exhausted.
    /// </summary>
    public static CampaignPlayState EnsurePlayerAssignment(
        CampaignPlayState state,
        IReadOnlyList<PrivateObjectiveTypePlayRules> types,
        Guid playerUserId,
        DateTimeOffset utcNow,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(pickIndex);
        if (state.PrivateObjectives.Any(item =>
                item.HolderKind == PrivateObjectiveHolderKind.Player && item.HolderId == playerUserId))
        {
            return state;
        }

        if (!TryGrant(
                state,
                types,
                PrivateObjectiveHolderKind.Player,
                playerUserId,
                typeId: null,
                utcNow,
                pickIndex,
                out var next,
                out _))
        {
            return state;
        }

        return next;
    }

    /// <summary>
    /// Assigns a specific or random catalog objective from the holder's pool.
    /// Random grants prefer a still-unique type for that holder kind, then a duplicate from a rebuilt pool.
    /// </summary>
    public static bool TryGrant(
        CampaignPlayState state,
        IReadOnlyList<PrivateObjectiveTypePlayRules> types,
        PrivateObjectiveHolderKind holderKind,
        Guid holderId,
        Guid? typeId,
        DateTimeOffset utcNow,
        Func<int, int> pickIndex,
        out CampaignPlayState next,
        out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(pickIndex);
        next = state;
        error = null;
        PrivateObjectiveTypePlayRules? type;
        if (typeId is { } requested)
        {
            type = types.FirstOrDefault(item => item.Id == requested);
            if (type is null)
            {
                error = new DomainError("privateObjective.unknown", "That private objective was not found.", "typeId");
                return false;
            }

            if (!type.Allows(holderKind))
            {
                error = new DomainError(
                    "privateObjective.holder.invalid",
                    "That private objective cannot be given to this holder.",
                    "holderKind");
                return false;
            }

            if (HolderAlreadyHas(state.PrivateObjectives, holderKind, holderId, type.Id))
            {
                error = new DomainError(
                    "privateObjective.unavailable",
                    "This holder already has that private objective.",
                    "typeId");
                return false;
            }
        }
        else
        {
            type = PickFromPool(types, state.PrivateObjectives, holderKind, holderId, pickIndex);
            if (type is null)
            {
                error = new DomainError("privateObjective.none_available", "No available private objective remains for that holder.");
                return false;
            }
        }

        var assignment = new PrivateObjectiveAssignment(
            Guid.NewGuid(),
            type.Id,
            holderKind,
            holderId,
            type.ScoringKind,
            PrivateObjectiveAssignmentStatus.Assigned,
            utcNow);
        next = state.With(privateObjectives: [.. state.PrivateObjectives, assignment]);
        return true;
    }

    /// <summary>
    /// Submits a manual claim for manager approval.
    /// </summary>
    public static bool TryClaim(
        CampaignPlayState state,
        Guid assignmentId,
        Guid actorUserId,
        DateTimeOffset utcNow,
        out CampaignPlayState next,
        out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        next = state;
        error = null;
        var assignment = state.PrivateObjectives.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
        {
            error = new DomainError("privateObjective.unknown", "That private objective was not found.", "assignmentId");
            return false;
        }

        if (assignment.ScoringKind != PrivateObjectiveScoringKind.Manual)
        {
            error = new DomainError("privateObjective.automatic", "Automatic private objectives cannot be claimed.");
            return false;
        }

        if (assignment.Status != PrivateObjectiveAssignmentStatus.Assigned)
        {
            error = new DomainError("privateObjective.claimed", "That private objective is already claimed or revealed.");
            return false;
        }

        var updated = assignment.With(
            status: PrivateObjectiveAssignmentStatus.Claimed,
            claimedUtc: utcNow,
            claimedByUserId: actorUserId);
        next = Replace(state, updated);
        return true;
    }

    /// <summary>
    /// Approves a claimed manual objective, revealing it and adding its points.
    /// </summary>
    public static bool TryApprove(
        CampaignPlayState state,
        Guid assignmentId,
        Guid actorUserId,
        DateTimeOffset utcNow,
        IReadOnlyDictionary<Guid, string> names,
        out CampaignPlayState next,
        out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(names);
        next = state;
        error = null;
        var assignment = state.PrivateObjectives.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
        {
            error = new DomainError("privateObjective.unknown", "That private objective was not found.", "assignmentId");
            return false;
        }

        if (assignment.Status is not PrivateObjectiveAssignmentStatus.Claimed
            and not PrivateObjectiveAssignmentStatus.Assigned)
        {
            error = new DomainError("privateObjective.revealed", "That private objective is already revealed.");
            return false;
        }

        if (assignment.ScoringKind != PrivateObjectiveScoringKind.Manual)
        {
            error = new DomainError("privateObjective.automatic", "Automatic private objectives complete themselves.");
            return false;
        }

        var updated = assignment.With(
            status: PrivateObjectiveAssignmentStatus.Revealed,
            revealedUtc: utcNow,
            approvedByUserId: actorUserId);
        next = Replace(state, updated).AppendLog(RevealLog(updated, utcNow, actorUserId, names.GetValueOrDefault(updated.TypeId)));
        return true;
    }

    /// <summary>
    /// Returns a claimed manual objective to assigned so it can be claimed again.
    /// </summary>
    public static bool TryDeny(
        CampaignPlayState state,
        Guid assignmentId,
        out CampaignPlayState next,
        out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        next = state;
        error = null;
        var assignment = state.PrivateObjectives.FirstOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
        {
            error = new DomainError("privateObjective.unknown", "That private objective was not found.", "assignmentId");
            return false;
        }

        if (assignment.Status != PrivateObjectiveAssignmentStatus.Claimed)
        {
            error = new DomainError("privateObjective.not_claimed", "That private objective is not waiting for approval.");
            return false;
        }

        next = Replace(state, assignment.With(status: PrivateObjectiveAssignmentStatus.Assigned, clearClaim: true));
        return true;
    }

    /// <summary>
    /// Completes automatic private objectives whose map criteria are currently met.
    /// </summary>
    public static CampaignPlayState EvaluateAutomatic(
        CampaignPlayState state,
        IReadOnlyList<PrivateObjectiveTypePlayRules> types,
        IReadOnlyList<PrivateObjectiveTerritory> territories,
        IReadOnlyDictionary<Guid, Guid> factionByPlayer,
        IReadOnlyDictionary<Guid, Guid?> allyGroupByFaction,
        IReadOnlySet<Guid> brokenAllyFactionIds,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(territories);
        ArgumentNullException.ThrowIfNull(factionByPlayer);
        ArgumentNullException.ThrowIfNull(allyGroupByFaction);
        ArgumentNullException.ThrowIfNull(brokenAllyFactionIds);

        var byId = types.ToDictionary(static type => type.Id);
        var next = state.PrivateObjectives.ToList();
        var log = new List<PlayLogEntry>();
        var changed = false;
        for (var index = 0; index < next.Count; index++)
        {
            var assignment = next[index];
            if (assignment.ScoringKind != PrivateObjectiveScoringKind.Automatic
                || assignment.Status == PrivateObjectiveAssignmentStatus.Revealed
                || !byId.TryGetValue(assignment.TypeId, out var type)
                || type.AutomaticKind == PrivateObjectiveAutomaticKind.None)
            {
                continue;
            }

            if (!IsAutomaticComplete(
                    assignment,
                    type,
                    territories,
                    state.StructureDestructions,
                    factionByPlayer,
                    allyGroupByFaction,
                    brokenAllyFactionIds))
            {
                continue;
            }

            var revealed = assignment.With(
                status: PrivateObjectiveAssignmentStatus.Revealed,
                revealedUtc: utcNow);
            next[index] = revealed;
            log.Add(RevealLog(revealed, utcNow, actorUserId: null, type.Name));
            changed = true;
        }

        return changed ? state.With(privateObjectives: next).AppendLog([.. log]) : state;
    }

    /// <summary>
    /// Makes remaining assignments public when the campaign completes. Points then count without a mid-campaign claim.
    /// </summary>
    public static CampaignPlayState RevealRemainingAtCompletion(
        CampaignPlayState state,
        IReadOnlyDictionary<Guid, string> names,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(names);
        var next = state.PrivateObjectives.ToList();
        var log = new List<PlayLogEntry>();
        var changed = false;
        for (var index = 0; index < next.Count; index++)
        {
            var assignment = next[index];
            if (assignment.Status == PrivateObjectiveAssignmentStatus.Revealed)
            {
                continue;
            }

            var revealed = assignment.With(
                status: PrivateObjectiveAssignmentStatus.Revealed,
                revealedUtc: utcNow);
            next[index] = revealed;
            log.Add(RevealLog(revealed, utcNow, actorUserId: null, names.GetValueOrDefault(assignment.TypeId)));
            changed = true;
        }

        return changed ? state.With(privateObjectives: next).AppendLog([.. log]) : state;
    }

    /// <summary>
    /// Public unclaimed counts grouped by holder.
    /// </summary>
    public static IReadOnlyList<(PrivateObjectiveHolderKind HolderKind, Guid HolderId, int Count)> UnclaimedCounts(
        IReadOnlyList<PrivateObjectiveAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        return
        [
            .. assignments
                .Where(static item => item.IsUnclaimed)
                .GroupBy(static item => (item.HolderKind, item.HolderId))
                .Select(static group => (group.Key.HolderKind, group.Key.HolderId, group.Count()))
                .OrderBy(static item => item.HolderKind)
                .ThenBy(static item => item.HolderId),
        ];
    }

    /// <summary>
    /// Campaign points from revealed private objectives that apply to a player.
    /// When <paramref name="campaignCompleted"/> is true, remaining held assignments also count.
    /// </summary>
    public static int PointsForPlayer(
        IReadOnlyList<PrivateObjectiveAssignment> assignments,
        IReadOnlyDictionary<Guid, int> pointsByType,
        Guid playerUserId,
        Guid? factionId,
        Guid? allyGroupId,
        bool campaignCompleted)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(pointsByType);
        var total = 0;
        foreach (var assignment in assignments)
        {
            var counts = assignment.CountsDuringPlay
                || (campaignCompleted && assignment.Status != PrivateObjectiveAssignmentStatus.Revealed);
            if (!counts)
            {
                continue;
            }

            var applies = assignment.HolderKind switch
            {
                PrivateObjectiveHolderKind.Player => assignment.HolderId == playerUserId,
                PrivateObjectiveHolderKind.Faction => factionId is { } faction && assignment.HolderId == faction,
                PrivateObjectiveHolderKind.AllyGroup => allyGroupId is { } group && assignment.HolderId == group,
                _ => false,
            };
            if (!applies)
            {
                continue;
            }

            total += pointsByType.GetValueOrDefault(assignment.TypeId);
        }

        return total;
    }

    /// <summary>
    /// Whether the viewer may see the secret text of an assignment.
    /// </summary>
    public static bool CanViewDetails(
        PrivateObjectiveAssignment assignment,
        Guid viewerUserId,
        Guid? viewerFactionId,
        Guid? viewerAllyGroupId,
        bool staffView,
        bool campaignCompleted)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        if (staffView
            || campaignCompleted
            || assignment.Status == PrivateObjectiveAssignmentStatus.Revealed)
        {
            return true;
        }

        return assignment.HolderKind switch
        {
            PrivateObjectiveHolderKind.Player => assignment.HolderId == viewerUserId,
            PrivateObjectiveHolderKind.Faction => viewerFactionId is { } faction && assignment.HolderId == faction,
            PrivateObjectiveHolderKind.AllyGroup => viewerAllyGroupId is { } group && assignment.HolderId == group,
            _ => false,
        };
    }

    private static void AssignGroup(
        List<PrivateObjectiveAssignment> assigned,
        IReadOnlyList<PrivateObjectiveTypePlayRules> types,
        PrivateObjectiveHolderKind holderKind,
        IReadOnlyList<Guid> holderIds,
        DateTimeOffset utcNow,
        Func<int, int> pickIndex)
    {
        var pool = PoolFor(types, holderKind);
        if (pool.Length == 0)
        {
            return;
        }

        var remaining = new List<PrivateObjectiveTypePlayRules>();
        foreach (var holderId in holderIds.OrderBy(static id => id))
        {
            if (remaining.Count == 0)
            {
                remaining.AddRange(pool);
            }

            var pick = pickIndex(remaining.Count);
            var type = remaining[pick];
            remaining.RemoveAt(pick);
            assigned.Add(new PrivateObjectiveAssignment(
                Guid.NewGuid(),
                type.Id,
                holderKind,
                holderId,
                type.ScoringKind,
                PrivateObjectiveAssignmentStatus.Assigned,
                utcNow));
        }
    }

    private static PrivateObjectiveTypePlayRules? PickFromPool(
        IReadOnlyList<PrivateObjectiveTypePlayRules> types,
        IReadOnlyList<PrivateObjectiveAssignment> existing,
        PrivateObjectiveHolderKind holderKind,
        Guid holderId,
        Func<int, int> pickIndex)
    {
        var pool = PoolFor(types, holderKind);
        if (pool.Length == 0)
        {
            return null;
        }

        var holderHas = existing
            .Where(item => item.HolderKind == holderKind && item.HolderId == holderId)
            .Select(static item => item.TypeId)
            .ToHashSet();
        var usedInKind = existing
            .Where(item => item.HolderKind == holderKind)
            .Select(static item => item.TypeId)
            .ToHashSet();
        var unused = pool.Where(item => !usedInKind.Contains(item.Id) && !holderHas.Contains(item.Id)).ToArray();
        var candidates = unused.Length > 0
            ? unused
            : pool.Where(item => !holderHas.Contains(item.Id)).ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        return candidates[pickIndex(candidates.Length)];
    }

    private static PrivateObjectiveTypePlayRules[] PoolFor(
        IReadOnlyList<PrivateObjectiveTypePlayRules> types,
        PrivateObjectiveHolderKind holderKind)
    {
        return [.. types.Where(item => item.Allows(holderKind)).OrderBy(static item => item.Id)];
    }

    private static bool HolderAlreadyHas(
        IReadOnlyList<PrivateObjectiveAssignment> existing,
        PrivateObjectiveHolderKind holderKind,
        Guid holderId,
        Guid typeId)
    {
        return existing.Any(item =>
            item.HolderKind == holderKind && item.HolderId == holderId && item.TypeId == typeId);
    }

    private static bool IsAutomaticComplete(
        PrivateObjectiveAssignment assignment,
        PrivateObjectiveTypePlayRules type,
        IReadOnlyList<PrivateObjectiveTerritory> territories,
        IReadOnlyList<StructureDestructionFact> destructions,
        IReadOnlyDictionary<Guid, Guid> factionByPlayer,
        IReadOnlyDictionary<Guid, Guid?> allyGroupByFaction,
        IReadOnlySet<Guid> brokenAllyFactionIds)
    {
        var factionIds = HolderFactions(assignment, factionByPlayer, allyGroupByFaction, brokenAllyFactionIds);
        if (factionIds.Count == 0 && assignment.HolderKind != PrivateObjectiveHolderKind.Player)
        {
            return false;
        }

        var owned = territories.Where(territory =>
            territory.OwnerFactionId is { } owner && factionIds.Contains(owner)).ToArray();
        return type.AutomaticKind switch
        {
            PrivateObjectiveAutomaticKind.ControlTerritoryCount => owned.Length >= type.RequiredCount,
            PrivateObjectiveAutomaticKind.ControlNamedTerritories =>
                type.TerritoryIds.Count(id => owned.Any(territory => territory.TerritoryId == id)) >= type.RequiredCount,
            PrivateObjectiveAutomaticKind.ControlStructureType =>
                owned.Count(territory =>
                    territory.StructureTypeId == type.StructureTypeId
                    && territory.StructureCondition != StructureCondition.Destroyed) >= type.RequiredCount,
            PrivateObjectiveAutomaticKind.PillageStructureType =>
                owned.Count(territory =>
                    territory.StructureTypeId == type.StructureTypeId
                    && territory.StructureCondition == StructureCondition.Pillaged) >= type.RequiredCount,
            PrivateObjectiveAutomaticKind.DestroyStructureType =>
                destructions.Count(fact =>
                    fact.StructureTypeId == type.StructureTypeId
                    && AttributionMatches(assignment, fact, factionByPlayer, allyGroupByFaction, brokenAllyFactionIds))
                    >= type.RequiredCount,
            _ => false,
        };
    }

    private static HashSet<Guid> HolderFactions(
        PrivateObjectiveAssignment assignment,
        IReadOnlyDictionary<Guid, Guid> factionByPlayer,
        IReadOnlyDictionary<Guid, Guid?> allyGroupByFaction,
        IReadOnlySet<Guid> brokenAllyFactionIds)
    {
        return assignment.HolderKind switch
        {
            PrivateObjectiveHolderKind.Player => factionByPlayer.TryGetValue(assignment.HolderId, out var faction)
                ? [faction]
                : [],
            PrivateObjectiveHolderKind.Faction => [assignment.HolderId],
            PrivateObjectiveHolderKind.AllyGroup =>
            [
                .. allyGroupByFaction
                    .Where(pair => pair.Value == assignment.HolderId && !brokenAllyFactionIds.Contains(pair.Key))
                    .Select(static pair => pair.Key),
            ],
            _ => [],
        };
    }

    private static bool AttributionMatches(
        PrivateObjectiveAssignment assignment,
        StructureDestructionFact fact,
        IReadOnlyDictionary<Guid, Guid> factionByPlayer,
        IReadOnlyDictionary<Guid, Guid?> allyGroupByFaction,
        IReadOnlySet<Guid> brokenAllyFactionIds)
    {
        return assignment.HolderKind switch
        {
            PrivateObjectiveHolderKind.Player => fact.ActorUserId == assignment.HolderId,
            PrivateObjectiveHolderKind.Faction => fact.ActorFactionId == assignment.HolderId,
            PrivateObjectiveHolderKind.AllyGroup =>
                allyGroupByFaction.GetValueOrDefault(fact.ActorFactionId) == assignment.HolderId
                && !brokenAllyFactionIds.Contains(fact.ActorFactionId),
            _ => false,
        };
    }

    private static CampaignPlayState Replace(CampaignPlayState state, PrivateObjectiveAssignment updated)
    {
        return state.With(privateObjectives:
        [
            .. state.PrivateObjectives.Select(item => item.Id == updated.Id ? updated : item),
        ]);
    }

    private static PlayLogEntry RevealLog(
        PrivateObjectiveAssignment assignment,
        DateTimeOffset utcNow,
        Guid? actorUserId,
        string? name)
    {
        return new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            PlayLogKind.PrivateObjectiveRevealed,
            windowId: null,
            forceId: null,
            actorUserId,
            territoryId: null,
            targetTerritoryId: null,
            battleId: null,
            actionKind: null,
            relatedForceIds: [],
            message: name ?? assignment.TypeId.ToString());
    }
}
