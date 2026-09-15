using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Play;

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
                type.IsHiddenUntilFound,
                type.FlavorText));
        }

        return spawned;
    }

    /// <summary>
    /// Drops selected carried items onto the territory a moving force left.
    /// Opened or already-interacted items cannot be dropped this way.
    /// </summary>
    public static IReadOnlyList<CampaignItemObjective> DropCarriedByMovers(
        IReadOnlyList<CampaignItemObjective> items,
        IReadOnlyDictionary<Guid, Guid> originByForceId,
        DateTimeOffset utcNow,
        ICollection<PlayLogEntry> log,
        IReadOnlySet<Guid>? itemIds = null,
        bool requireUnopened = true)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(originByForceId);
        ArgumentNullException.ThrowIfNull(log);
        var next = new List<CampaignItemObjective>(items.Count);
        foreach (var item in items.OrderBy(static entry => entry.Id))
        {
            if (item.IsDestroyed)
            {
                next.Add(item);
                continue;
            }

            if (item.PossessorForceId is { } forceId
                && originByForceId.TryGetValue(forceId, out var origin)
                && (itemIds is null || itemIds.Contains(item.Id))
                && (!requireUnopened || TeleportActionRules.CanDropOnMove(item)))
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
    /// Two or more occupying forces reveal a hidden item without a possessor.
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
            .GroupBy(static force => force.TerritoryId)
            .ToDictionary(static group => group.Key, static group => group.ToArray());
        var next = new List<CampaignItemObjective>(items.Count);
        foreach (var item in items.OrderBy(static entry => entry.Id))
        {
            if (item.IsDestroyed
                || item.PossessorForceId is not null
                || item.TerritoryId is not { } territoryId
                || !occupants.TryGetValue(territoryId, out var present)
                || present.Length == 0)
            {
                next.Add(item);
                continue;
            }

            var idle = present.Where(static force => !force.InBattle).ToArray();
            if (idle.Length == 1 && present.Length == 1)
            {
                var force = idle[0];
                var found = !item.IsRevealed;
                var taken = item.With(possessorForceId: force.Id, isRevealed: true, clearTerritory: true);
                next.Add(taken);
                log.Add(ItemLog(
                    found ? PlayLogKind.ItemObjectiveFound : PlayLogKind.ItemObjectivePickedUp,
                    taken,
                    utcNow,
                    territoryId,
                    force.Id));
                continue;
            }

            if (!item.IsRevealed)
            {
                var revealed = item.With(isRevealed: true);
                next.Add(revealed);
                log.Add(ItemLog(
                    PlayLogKind.ItemObjectiveFound,
                    revealed,
                    utcNow,
                    territoryId,
                    forceId: null,
                    relatedForceId: null,
                    relatedForceIds: present.Select(static force => force.Id).ToArray()));
                continue;
            }

            next.Add(item);
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
        if (battle.IsDraw || battle.IsNoContest || battle.WinnerForceId is not { } winnerId)
        {
            return items;
        }

        var participants = battle.ParticipantForceIds.ToHashSet();
        var next = new List<CampaignItemObjective>(items.Count);
        foreach (var item in items.OrderBy(static entry => entry.Id))
        {
            if (item.IsDestroyed)
            {
                next.Add(item);
                continue;
            }

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
                previousHolder,
                battleId: battle.Id));
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

    /// <summary>
    /// Returns items transferred as this battle's spoils to their previous holder or the battlefield.
    /// </summary>
    public static IReadOnlyList<CampaignItemObjective> RevertBattleSpoils(
        IReadOnlyList<CampaignItemObjective> items,
        CampaignBattle battle,
        IReadOnlyList<PlayLogEntry> log)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(log);

        var spoils = log
            .Where(entry =>
                entry.BattleId == battle.Id
                && entry.Kind is PlayLogKind.ItemObjectiveFound or PlayLogKind.ItemObjectivePickedUp)
            .ToArray();
        if (spoils.Length == 0)
        {
            return items;
        }

        var next = new List<CampaignItemObjective>(items.Count);
        foreach (var item in items.OrderBy(static entry => entry.Id))
        {
            var taken = spoils.LastOrDefault(entry =>
                string.Equals(entry.Message, item.Name, StringComparison.Ordinal)
                && entry.ForceId == item.PossessorForceId);
            if (taken is null)
            {
                next.Add(item);
                continue;
            }

            var previousHolder = taken.RelatedForceIds.FirstOrDefault(id => id != taken.ForceId);
            next.Add(
                previousHolder != default
                    ? item.With(possessorForceId: previousHolder, clearTerritory: true)
                    : item.With(territoryId: battle.TerritoryId, clearPossessor: true));
        }

        return next;
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
        Guid? relatedForceId = null,
        Guid? battleId = null,
        IReadOnlyList<Guid>? relatedForceIds = null)
    {
        IReadOnlyList<Guid> related = relatedForceIds
            ?? (relatedForceId is { } extra && extra != forceId
                ? forceId is { } id ? [id, extra] : [extra]
                : forceId is { } only ? [only] : []);
        return new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            kind,
            windowId: null,
            forceId,
            actorUserId: null,
            territoryId,
            targetTerritoryId: null,
            battleId,
            actionKind: null,
            related,
            item.Name);
    }
}
