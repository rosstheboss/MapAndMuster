namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Collapses duplicate and redundant force-status conditions before save.
/// </summary>
public static class ForceStatusConditionCollapse
{
    /// <summary>
    /// Drops exact duplicates, then strictly weaker conditions in the same list, then a clear
    /// condition that matches an enable fingerprint.
    /// </summary>
    public static (List<ForceStatusEnableCondition> Enables, List<ForceStatusClearCondition> Clears) Collapse(
        IReadOnlyList<ForceStatusEnableCondition> enables,
        IReadOnlyList<ForceStatusClearCondition> clears,
        Func<Guid, IReadOnlyList<Guid>> tagsForType)
    {
        ArgumentNullException.ThrowIfNull(enables);
        ArgumentNullException.ThrowIfNull(clears);
        ArgumentNullException.ThrowIfNull(tagsForType);
        var uniqueEnables = DistinctEnables(enables);
        var uniqueClears = DistinctClears(clears);
        uniqueEnables = DropWeakerEnables(uniqueEnables, tagsForType);
        uniqueClears = DropWeakerClears(uniqueClears, tagsForType);
        var enableFingerprints = uniqueEnables.Select(static condition => condition.Fingerprint()).ToHashSet(StringComparer.Ordinal);
        uniqueClears =
        [
            .. uniqueClears.Where(condition => !enableFingerprints.Contains(condition.Fingerprint())),
        ];
        return (uniqueEnables, uniqueClears);
    }

    private static List<ForceStatusEnableCondition> DistinctEnables(IReadOnlyList<ForceStatusEnableCondition> conditions)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<ForceStatusEnableCondition>();
        foreach (var condition in conditions)
        {
            if (seen.Add(condition.Fingerprint()))
            {
                kept.Add(condition);
            }
        }

        return kept;
    }

    private static List<ForceStatusClearCondition> DistinctClears(IReadOnlyList<ForceStatusClearCondition> conditions)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<ForceStatusClearCondition>();
        foreach (var condition in conditions)
        {
            if (seen.Add(condition.Fingerprint()))
            {
                kept.Add(condition);
            }
        }

        return kept;
    }

    private static List<ForceStatusEnableCondition> DropWeakerEnables(
        List<ForceStatusEnableCondition> conditions,
        Func<Guid, IReadOnlyList<Guid>> tagsForType)
    {
        return
        [
            .. conditions.Where((candidate, index) =>
                !conditions.Where((_, other) => other != index).Any(stronger =>
                    stronger.Trigger == candidate.Trigger
                    && candidate.Occurrences >= stronger.Occurrences
                    && stronger.Location.StrictlySubsumes(candidate.Location, tagsForType))),
        ];
    }

    private static List<ForceStatusClearCondition> DropWeakerClears(
        List<ForceStatusClearCondition> conditions,
        Func<Guid, IReadOnlyList<Guid>> tagsForType)
    {
        return
        [
            .. conditions.Where((candidate, index) =>
                !conditions.Where((_, other) => other != index).Any(stronger =>
                    stronger.Trigger == candidate.Trigger
                    && candidate.Occurrences >= stronger.Occurrences
                    && stronger.Location.StrictlySubsumes(candidate.Location, tagsForType))),
        ];
    }
}
