using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Play;

/// <summary>
/// Display order for close-of-phase game-log facts. Chat and headings stay in time order;
/// action-window force resolutions group by owning player, then battle locks; battle-window
/// results precede retreats and surrenders.
/// </summary>
internal static class PlayLogDisplayOrder
{
    internal static IReadOnlyList<(PlayLogEntry Entry, int Index)> Sort(
        IEnumerable<(PlayLogEntry Entry, int Index)> items,
        CampaignPlayState play,
        IReadOnlyDictionary<Guid, string> names)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(play);
        ArgumentNullException.ThrowIfNull(names);
        var windows = play.Windows.ToDictionary(static window => window.Id);
        return
        [
            .. items
                .OrderBy(static item => item.Entry.OccurredUtc)
                .ThenBy(static item => PhaseHeadingSort(item.Entry.Kind))
                .ThenBy(item => item.Entry.WindowId)
                .ThenBy(item => ResolutionBucket(item.Entry, windows))
                .ThenBy(item => ResolutionSortName(item.Entry, play, names, windows), StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => ResolutionOwnerId(item.Entry, play))
                .ThenBy(static item => item.Index),
        ];
    }

    internal static int PhaseHeadingSort(PlayLogKind kind)
    {
        return kind is PlayLogKind.PhaseChanged or PlayLogKind.CampaignEnded or PlayLogKind.CampaignClosed
            ? 1
            : 0;
    }

    private static int ResolutionBucket(PlayLogEntry entry, IReadOnlyDictionary<Guid, PhaseWindow> windows)
    {
        var kind = WindowKind(entry, windows);
        if (kind == RoundPhaseKind.Action)
        {
            if (entry.Kind == PlayLogKind.BattleCreated)
            {
                return 2;
            }

            return IsActionPlayerResolution(entry.Kind) ? 1 : 0;
        }

        if (kind == RoundPhaseKind.Battle)
        {
            if (IsBattleResult(entry.Kind))
            {
                return 1;
            }

            return IsBattleRetreat(entry.Kind) ? 2 : 0;
        }

        return 0;
    }

    private static string ResolutionSortName(
        PlayLogEntry entry,
        CampaignPlayState play,
        IReadOnlyDictionary<Guid, string> names,
        IReadOnlyDictionary<Guid, PhaseWindow> windows)
    {
        var kind = WindowKind(entry, windows);
        if (kind == RoundPhaseKind.Action)
        {
            if (entry.Kind == PlayLogKind.BattleCreated)
            {
                return MinParticipantName(entry, play, names);
            }

            return IsActionPlayerResolution(entry.Kind) ? OwnerName(entry, play, names) : string.Empty;
        }

        if (kind == RoundPhaseKind.Battle)
        {
            if (IsBattleResult(entry.Kind))
            {
                return MinParticipantName(entry, play, names);
            }

            return IsBattleRetreat(entry.Kind) ? OwnerName(entry, play, names) : string.Empty;
        }

        return string.Empty;
    }

    private static Guid ResolutionOwnerId(PlayLogEntry entry, CampaignPlayState play)
    {
        return OwnerUserId(entry, play) ?? Guid.Empty;
    }

    private static RoundPhaseKind? WindowKind(PlayLogEntry entry, IReadOnlyDictionary<Guid, PhaseWindow> windows)
    {
        return entry.WindowId is { } id && windows.TryGetValue(id, out var window) ? window.Kind : null;
    }

    private static bool IsActionPlayerResolution(PlayLogKind kind)
    {
        return kind is PlayLogKind.ResolvedAction
            or PlayLogKind.ActionCancelled
            or PlayLogKind.RandomTeleportPreparing
            or PlayLogKind.MissingOrderHold
            or PlayLogKind.DeadlineDraftSubmitted
            or PlayLogKind.InvalidOrderHold
            or PlayLogKind.ConflictingBuildHold
            or PlayLogKind.AllianceBetrayed
            or PlayLogKind.ForcesRejoined
            or PlayLogKind.ItemObjectiveDropped
            or PlayLogKind.ItemObjectivePickedUp
            or PlayLogKind.ItemObjectiveFound
            or PlayLogKind.ItemObjectiveDestroyed
            or PlayLogKind.ForceStatusChanged;
    }

    private static bool IsBattleResult(PlayLogKind kind)
    {
        return kind is PlayLogKind.BattleFinalized
            or PlayLogKind.BattleGmResolved
            or PlayLogKind.BattleDisputed
            or PlayLogKind.UnresolvedBattleHeldOpen
            or PlayLogKind.NoResultForcedRetreat
            or PlayLogKind.RingerBattleVoided
            or PlayLogKind.BattleMatchAdvanced;
    }

    private static bool IsBattleRetreat(PlayLogKind kind)
    {
        return kind is PlayLogKind.PlayerRetreat
            or PlayLogKind.PlayerSurrendered
            or PlayLogKind.DefaultRetreat
            or PlayLogKind.RetreatCollisionResolved;
    }

    private static string OwnerName(PlayLogEntry entry, CampaignPlayState play, IReadOnlyDictionary<Guid, string> names)
    {
        return Username(OwnerUserId(entry, play), names);
    }

    private static Guid? OwnerUserId(PlayLogEntry entry, CampaignPlayState play)
    {
        if (entry.ForceId is { } forceId)
        {
            var force = play.Forces.FirstOrDefault(item => item.Id == forceId);
            if (force is not null)
            {
                return force.ControllerUserId;
            }
        }

        return entry.ActorUserId;
    }

    private static string MinParticipantName(
        PlayLogEntry entry,
        CampaignPlayState play,
        IReadOnlyDictionary<Guid, string> names)
    {
        var forceIds = entry.RelatedForceIds.Count > 0
            ? entry.RelatedForceIds
            : play.Battles.FirstOrDefault(item => item.Id == entry.BattleId)?.ParticipantForceIds
                ?? [];
        var labels = forceIds
            .Select(id => play.Forces.FirstOrDefault(force => force.Id == id))
            .Select(force => Username(force?.ControllerUserId ?? entry.ActorUserId, names))
            .Where(static name => name.Length > 0)
            .ToArray();
        return labels.Length == 0
            ? Username(entry.ActorUserId, names)
            : labels.Min(StringComparer.OrdinalIgnoreCase) ?? string.Empty;
    }

    private static string Username(Guid? userId, IReadOnlyDictionary<Guid, string> names)
    {
        if (userId is { } id && names.TryGetValue(id, out var username) && !string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        return string.Empty;
    }
}
