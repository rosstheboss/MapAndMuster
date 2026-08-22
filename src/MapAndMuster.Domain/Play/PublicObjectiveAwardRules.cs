using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Awards or revokes public campaign objectives for a player. Original award facts are never overwritten.
/// </summary>
public static class PublicObjectiveAwardRules
{
    /// <summary>
    /// Records that a player completed a public objective.
    /// </summary>
    /// <param name="state">The current play state.</param>
    /// <param name="objectiveId">The public objective.</param>
    /// <param name="playerUserId">The player receiving the award.</param>
    /// <param name="actorUserId">The manager recording the award.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <param name="knownObjectiveIds">Configured public objective identifiers.</param>
    /// <param name="playerUserIds">Current player-slot user identifiers.</param>
    /// <param name="next">The updated play state.</param>
    /// <param name="error">The domain error when the command is invalid.</param>
    /// <returns><see langword="true"/> when the award was recorded.</returns>
    public static bool TryAward(
        CampaignPlayState state,
        Guid objectiveId,
        Guid playerUserId,
        Guid actorUserId,
        DateTimeOffset utcNow,
        IReadOnlySet<Guid> knownObjectiveIds,
        IReadOnlySet<Guid> playerUserIds,
        out CampaignPlayState? next,
        out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(knownObjectiveIds);
        ArgumentNullException.ThrowIfNull(playerUserIds);
        next = null;
        error = null;
        if (!knownObjectiveIds.Contains(objectiveId))
        {
            error = new DomainError("publicObjective.unknown", "That public objective was not found.", "objectiveId");
            return false;
        }

        if (!playerUserIds.Contains(playerUserId))
        {
            error = new DomainError("publicObjective.player.invalid", "Choose a player in this campaign.", "playerUserId");
            return false;
        }

        if (IsActive(state.PublicObjectiveAwards, objectiveId, playerUserId))
        {
            error = new DomainError("publicObjective.awarded", "That player already has this public objective.");
            return false;
        }

        next = state
            .With(publicObjectiveAwards:
            [
                .. state.PublicObjectiveAwards,
                new PublicObjectiveAward(Guid.NewGuid(), objectiveId, playerUserId, true, actorUserId, utcNow),
            ])
            .AppendLog(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.PublicObjectiveAwarded,
                windowId: null,
                forceId: null,
                actorUserId,
                territoryId: null,
                targetTerritoryId: null,
                battleId: null,
                actionKind: null,
                relatedForceIds: [],
                message: objectiveId.ToString()));
        return true;
    }

    /// <summary>
    /// Stops counting a previously awarded public objective without deleting the original award.
    /// </summary>
    /// <param name="state">The current play state.</param>
    /// <param name="objectiveId">The public objective.</param>
    /// <param name="playerUserId">The player whose award is revoked.</param>
    /// <param name="actorUserId">The manager recording the revocation.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <param name="next">The updated play state.</param>
    /// <param name="error">The domain error when the command is invalid.</param>
    /// <returns><see langword="true"/> when the revocation was recorded.</returns>
    public static bool TryRevoke(
        CampaignPlayState state,
        Guid objectiveId,
        Guid playerUserId,
        Guid actorUserId,
        DateTimeOffset utcNow,
        out CampaignPlayState? next,
        out DomainError? error)
    {
        ArgumentNullException.ThrowIfNull(state);
        next = null;
        error = null;
        if (!IsActive(state.PublicObjectiveAwards, objectiveId, playerUserId))
        {
            error = new DomainError("publicObjective.not_awarded", "That player does not currently have this public objective.");
            return false;
        }

        next = state
            .With(publicObjectiveAwards:
            [
                .. state.PublicObjectiveAwards,
                new PublicObjectiveAward(Guid.NewGuid(), objectiveId, playerUserId, false, actorUserId, utcNow),
            ])
            .AppendLog(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.PublicObjectiveRevoked,
                windowId: null,
                forceId: null,
                actorUserId,
                territoryId: null,
                targetTerritoryId: null,
                battleId: null,
                actionKind: null,
                relatedForceIds: [],
                message: objectiveId.ToString()));
        return true;
    }

    /// <summary>
    /// Whether a player currently has an active award for an objective.
    /// </summary>
    public static bool IsActive(IReadOnlyList<PublicObjectiveAward> awards, Guid objectiveId, Guid playerUserId)
    {
        ArgumentNullException.ThrowIfNull(awards);
        var active = false;
        foreach (var award in awards.OrderBy(static item => item.AwardedUtc).ThenBy(static item => item.Id))
        {
            if (award.ObjectiveId == objectiveId && award.PlayerUserId == playerUserId)
            {
                active = award.IsActive;
            }
        }

        return active;
    }
}
