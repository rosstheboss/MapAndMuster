using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Detects structures removed from the map by a successful Pillage.
/// </summary>
public static class StructureDestructionRules
{
    /// <summary>
    /// Records each structure that existed on <paramref name="before"/> and is gone on <paramref name="after"/>.
    /// Attribution uses the force that occupied the territory after resolution.
    /// </summary>
    public static IReadOnlyList<StructureDestructionFact> Detect(
        PlayMap before,
        PlayMap after,
        IReadOnlyList<CampaignForce> forces,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(forces);
        var facts = new List<StructureDestructionFact>();
        foreach (var previous in before.Territories.OrderBy(static territory => territory.Id))
        {
            if (previous.StructureTypeId is not { } structureTypeId
                || previous.StructureCondition == StructureCondition.Destroyed)
            {
                continue;
            }

            var current = after.Territory(previous.Id);
            if (current is not null && current.StructureTypeId is not null)
            {
                continue;
            }

            var actor = forces
                .Where(force => force.TerritoryId == previous.Id)
                .OrderBy(static force => force.Id)
                .FirstOrDefault();
            if (actor is null)
            {
                continue;
            }

            facts.Add(new StructureDestructionFact(
                Guid.NewGuid(),
                previous.Id,
                structureTypeId,
                actor.FactionId,
                actor.ControllerUserId,
                utcNow));
        }

        return facts;
    }
}

/// <summary>
/// Resolves a configured holder choice on a possessed item objective.
/// </summary>
public static class ItemObjectiveChoiceRules
{
    /// <summary>
    /// Applies one configured choice to a held item. Several results pick one at random.
    /// </summary>
    public static bool TryResolve(
        CampaignPlayState state,
        Guid itemId,
        Guid choiceId,
        Guid actorUserId,
        IReadOnlyList<ItemObjectiveTypeSetup> types,
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
        var item = state.ItemObjectives.FirstOrDefault(entry => entry.Id == itemId);
        if (item is null || item.IsDestroyed)
        {
            error = new DomainError("itemObjective.unknown", "That item objective was not found.", "itemId");
            return false;
        }

        if (item.ResolvedChoiceId is not null)
        {
            error = new DomainError("itemObjective.choice.resolved", "That item already had a choice resolved.");
            return false;
        }

        if (item.PossessorForceId is not { } forceId)
        {
            error = new DomainError("itemObjective.not_held", "Only the force holding the item can resolve a choice.");
            return false;
        }

        var force = state.Forces.FirstOrDefault(entry => entry.Id == forceId);
        if (force is null || force.ControllerUserId != actorUserId)
        {
            error = new DomainError("itemObjective.forbidden", "Only the holding player can resolve that choice.");
            return false;
        }

        var type = types.FirstOrDefault(entry => entry.Id == item.TypeId);
        var choice = type?.Choices.FirstOrDefault(entry => entry.Id == choiceId);
        if (type is null || choice is null)
        {
            error = new DomainError("itemObjective.choice.unknown", "That item choice was not found.", "choiceId");
            return false;
        }

        if (choice.Results.Count == 0)
        {
            error = new DomainError("itemObjective.choice.empty", "That item choice has no configured result.");
            return false;
        }

        var result = choice.Results.Count == 1
            ? choice.Results[0]
            : choice.Results[pickIndex(choice.Results.Count)];
        var items = state.ItemObjectives.ToList();
        var itemIndex = items.FindIndex(entry => entry.Id == item.Id);
        var nextItem = item.With(
            flavorText: result.FlavorText ?? item.FlavorText,
            stateKey: result.NewStateKey ?? item.StateKey,
            resolvedChoiceId: choice.Id,
            isDestroyed: result.DestroyItem);
        if (result.DestroyItem)
        {
            nextItem = nextItem.With(clearTerritory: true, clearPossessor: true, isDestroyed: true);
        }

        items[itemIndex] = nextItem;
        if (result.ReplacementItemTypeId is { } replacementId)
        {
            var replacementType = types.FirstOrDefault(entry => entry.Id == replacementId);
            if (replacementType is not null)
            {
                var spawnTerritory = item.TerritoryId ?? force.TerritoryId;
                items.Add(new CampaignItemObjective(
                    Guid.NewGuid(),
                    replacementType.Id,
                    replacementType.Name,
                    result.DestroyItem ? spawnTerritory : item.TerritoryId,
                    result.DestroyItem ? (item.PossessorForceId ?? force.Id) : item.PossessorForceId,
                    isRevealed: !replacementType.IsHiddenUntilFound || item.IsRevealed,
                    spawnTerritory,
                    replacementType.IsHiddenUntilFound,
                    replacementType.FlavorText,
                    stateKey: null,
                    isDestroyed: false,
                    resolvedChoiceId: null));
            }
        }

        var nextState = state.With(itemObjectives: items);
        if (result.DestroyItem)
        {
            nextState = nextState.AppendLog(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.ItemObjectiveDestroyed,
                windowId: null,
                forceId,
                actorUserId,
                item.TerritoryId ?? force.TerritoryId,
                targetTerritoryId: null,
                battleId: null,
                actionKind: null,
                relatedForceIds: [],
                message: item.Name));
        }

        if (result.GrantedPrivateObjectiveTypeId is { } grantedId)
        {
            nextState = GrantSecretFromRelic(nextState, grantedId, actorUserId, utcNow);
        }

        next = nextState;
        return true;
    }

    private static CampaignPlayState GrantSecretFromRelic(
        CampaignPlayState state,
        Guid typeId,
        Guid playerUserId,
        DateTimeOffset utcNow)
    {
        if (state.PrivateObjectives.Any(item => item.TypeId == typeId))
        {
            return state;
        }

        var assignment = new PrivateObjectiveAssignment(
            Guid.NewGuid(),
            typeId,
            PrivateObjectiveHolderKind.Player,
            playerUserId,
            PrivateObjectiveScoringKind.Manual,
            PrivateObjectiveAssignmentStatus.Assigned,
            utcNow);
        return state.With(privateObjectives: [.. state.PrivateObjectives, assignment]);
    }
}
