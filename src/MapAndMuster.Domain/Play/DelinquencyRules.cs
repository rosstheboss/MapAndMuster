namespace MapAndMuster.Domain.Play;

/// <summary>
/// Tracks missed-order offences per force for the whole campaign. Managers are notified from
/// the third offence onward; removal is never automatic.
/// </summary>
public static class DelinquencyRules
{
    /// <summary>Offence count at which managers are notified, and again after each later offence.</summary>
    public const int NotifyFromOffence = 3;

    /// <summary>
    /// Adds one offence for each listed force and appends a public log fact when the count
    /// reaches or passes the notify threshold.
    /// </summary>
    public static CampaignPlayState Record(
        CampaignPlayState state,
        IEnumerable<Guid> forceIds,
        PhaseWindow window,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(forceIds);
        ArgumentNullException.ThrowIfNull(window);
        var ids = forceIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return state;
        }

        var counts = state.Delinquencies.ToDictionary(static item => item.ForceId, static item => item.OffenceCount);
        var log = new List<PlayLogEntry>();
        foreach (var forceId in ids.OrderBy(static id => id))
        {
            var next = counts.GetValueOrDefault(forceId) + 1;
            counts[forceId] = next;
            if (next < NotifyFromOffence)
            {
                continue;
            }

            var force = state.Forces.FirstOrDefault(item => item.Id == forceId);
            log.Add(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.DelinquencyThreshold,
                window.Id,
                forceId,
                force?.ControllerUserId,
                force?.TerritoryId,
                targetTerritoryId: null,
                battleId: null,
                actionKind: null,
                force is null ? [] : [force.Id]));
        }

        var delinquencies = counts
            .OrderBy(static pair => pair.Key)
            .Select(static pair => new ForceDelinquency(pair.Key, pair.Value))
            .ToArray();
        return state.With(delinquencies: delinquencies).AppendLog([.. log]);
    }

    /// <summary>
    /// Whether new threshold facts were appended after <paramref name="previousLogCount"/>.
    /// </summary>
    public static bool ShouldNotifyManagers(CampaignPlayState state, int previousLogCount)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Log.Skip(previousLogCount).Any(static item => item.Kind == PlayLogKind.DelinquencyThreshold);
    }
}
