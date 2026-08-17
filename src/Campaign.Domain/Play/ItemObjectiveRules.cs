using Campaign.Domain.Campaigns;
using Campaign.Domain.Common;

namespace Campaign.Domain.Play;

/// <summary>
/// Seeds, drops, picks up, and reveals item objectives without exposing hidden locations.
/// </summary>
public static class ItemObjectiveRules
{
    /// <summary>
    /// Places catalog items on eligible territories at campaign launch.
    /// </summary>
    public static IReadOnlyList<CampaignItemObjective> Seed(
        IReadOnlyList<ItemObjectiveTypePlayRules> types,
        PlayMap map,
        IReadOnlyList<ItemObjectiveMapPlacement> placements,
        Func<int, int> pickIndex)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentNullException.ThrowIfNull(pickIndex);

        var placementByType = placements
            .GroupBy(static item => item.TypeId)
            .ToDictionary(static group => group.Key, static group => group.First().TerritoryId);
        var used = new HashSet<Guid>();
        var spawned = new List<CampaignItemObjective>();
        foreach (var type in types.OrderBy(static item => item.Id))
        {
            if (!TryChooseTerritory(type, map, placementByType, used, pickIndex, out var territoryId))
            {
                continue;
            }

            used.Add(territoryId);
            spawned.Add(new CampaignItemObjective(
                Guid.NewGuid(),
                type.Id,
                type.Name,
                territoryId,
                possessorForceId: null,
                isRevealed: !type.IsHiddenUntilFound,
                territoryId,
                type.IsHiddenUntilFound));
        }

        return spawned;
    }

    /// <summary>
    /// Drops carried items onto the territory a moving force left.
    /// </summary>
    public static IReadOnlyList<CampaignItemObjective> DropCarriedByMovers(
        IReadOnlyList<CampaignItemObjective> items,
        IReadOnlyDictionary<Guid, Guid> originByForceId,
        DateTimeOffset utcNow,
        ICollection<PlayLogEntry> log)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(originByForceId);
        ArgumentNullException.ThrowIfNull(log);
        var next = new List<CampaignItemObjective>(items.Count);
        foreach (var item in items.OrderBy(static entry => entry.Id))
        {
            if (item.PossessorForceId is { } forceId
                && originByForceId.TryGetValue(forceId, out var origin))
            {
                next.Add(item.With(territoryId: origin, clearPossessor: true));
                if (item.IsRevealed)
                {
                    log.Add(ItemLog(PlayLogKind.ItemObjectiveDropped, item, utcNow, origin, forceId));
                }
            }
            else
            {
                next.Add(item);
            }
        }

        return next;
    }

    /// <summary>
    /// A lone force not in battle takes an unpossessed item in its territory, revealing it if it was hidden.
    /// </summary>
    public static IReadOnlyList<CampaignItemObjective> PickUpUnpossessed(
        IReadOnlyList<CampaignItemObjective> items,
        IReadOnlyList<CampaignForce> forces,
        DateTimeOffset utcNow,
        ICollection<PlayLogEntry> log)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(log);

        var occupants = forces
            .Where(static force => !force.InBattle)
            .GroupBy(static force => force.TerritoryId)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.First());
        var next = new List<CampaignItemObjective>(items.Count);
        foreach (var item in items.OrderBy(static entry => entry.Id))
        {
            if (item.PossessorForceId is not null
                || item.TerritoryId is not { } territoryId
                || !occupants.TryGetValue(territoryId, out var force))
            {
                next.Add(item);
                continue;
            }

            var found = !item.IsRevealed;
            var taken = item.With(possessorForceId: force.Id, isRevealed: true, clearTerritory: true);
            next.Add(taken);
            log.Add(ItemLog(
                found ? PlayLogKind.ItemObjectiveFound : PlayLogKind.ItemObjectivePickedUp,
                taken,
                utcNow,
                territoryId,
                force.Id));
        }

        return next;
    }

    /// <summary>
    /// The battle winner takes items held by participants or lying in the battle territory.
    /// </summary>
    public static IReadOnlyList<CampaignItemObjective> AwardBattleSpoils(
        IReadOnlyList<CampaignItemObjective> items,
        CampaignBattle battle,
        IReadOnlyList<CampaignForce> forces,
        DateTimeOffset utcNow,
        ICollection<PlayLogEntry> log)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(forces);
        ArgumentNullException.ThrowIfNull(log);
        if (battle.IsDraw || battle.WinnerForceId is not { } winnerId)
        {
            return items;
        }

        var participants = battle.ParticipantForceIds.ToHashSet();
        var next = new List<CampaignItemObjective>(items.Count);
        foreach (var item in items.OrderBy(static entry => entry.Id))
        {
            var heldByParticipant = item.PossessorForceId is { } holder && participants.Contains(holder);
            var onBattlefield = item.PossessorForceId is null && item.TerritoryId == battle.TerritoryId;
            if (!heldByParticipant && !onBattlefield)
            {
                next.Add(item);
                continue;
            }

            if (item.PossessorForceId == winnerId)
            {
                next.Add(item.With(isRevealed: true));
                continue;
            }

            var previousHolder = item.PossessorForceId;
            var taken = item.With(possessorForceId: winnerId, isRevealed: true, clearTerritory: true);
            next.Add(taken);
            log.Add(ItemLog(
                item.IsRevealed ? PlayLogKind.ItemObjectivePickedUp : PlayLogKind.ItemObjectiveFound,
                taken,
                utcNow,
                battle.TerritoryId,
                winnerId,
                previousHolder));
        }

        _ = forces;
        return next;
    }

    /// <summary>
    /// Reveals every still-hidden item. Locations stay unchanged.
    /// </summary>
    public static bool TryRevealHidden(
        CampaignPlayState state,
        Guid actorUserId,
        DateTimeOffset utcNow,
        out CampaignPlayState? next,
        out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        next = null;
        if (state.DebugActorUserId != actorUserId)
        {
            error = new DomainError(
                "debug.required",
                "Enter debug mode before revealing hidden item objectives.");
            return false;
        }

        error = null;
        var revealed = state.ItemObjectives
            .Select(static item => item.IsRevealed ? item : item.With(isRevealed: true))
            .ToArray();
        var log = new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            PlayLogKind.ItemObjectivesStaffRevealed,
            windowId: null,
            forceId: null,
            actorUserId,
            territoryId: null,
            targetTerritoryId: null,
            battleId: null,
            actionKind: null,
            []);
        next = state.With(itemObjectives: revealed).AppendLog(log);
        return true;
    }

    private static bool TryChooseTerritory(
        ItemObjectiveTypePlayRules type,
        PlayMap map,
        Dictionary<Guid, Guid> placementByType,
        HashSet<Guid> used,
        Func<int, int> pickIndex,
        out Guid territoryId)
    {
        territoryId = default;
        if (type.Placement == ItemObjectivePlacementKind.Placed)
        {
            if (placementByType.TryGetValue(type.Id, out var placed)
                && map.Territory(placed) is { } placedTerritory
                && IsEligible(type, placedTerritory)
                && !used.Contains(placed))
            {
                territoryId = placed;
                return true;
            }

            return false;
        }

        var eligible = map.Territories
            .Where(territory => IsEligible(type, territory) && !used.Contains(territory.Id))
            .OrderBy(static territory => territory.Id)
            .Select(static territory => territory.Id)
            .ToArray();
        if (eligible.Length == 0)
        {
            return false;
        }

        var index = pickIndex(eligible.Length);
        if (index < 0 || index >= eligible.Length)
        {
            index = 0;
        }

        territoryId = eligible[index];
        return true;
    }

    private static bool IsEligible(ItemObjectiveTypePlayRules type, PlayTerritory territory)
    {
        return type.AllowOnSpawn || !territory.IsSpawn;
    }

    private static PlayLogEntry ItemLog(
        PlayLogKind kind,
        CampaignItemObjective item,
        DateTimeOffset utcNow,
        Guid? territoryId,
        Guid? forceId,
        Guid? relatedForceId = null)
    {
        IReadOnlyList<Guid> related = relatedForceId is { } extra && extra != forceId
            ? forceId is { } id ? [id, extra] : [extra]
            : forceId is { } only ? [only] : [];
        return new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            kind,
            windowId: null,
            forceId,
            actorUserId: null,
            territoryId,
            targetTerritoryId: null,
            battleId: null,
            actionKind: null,
            related,
            item.Name);
    }
}
