using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Play;

/// <summary>
/// Appends public campaign-log snapshots of final scores and remaining item objectives.
/// </summary>
internal static class CampaignCompletionLog
{
    public static async Task<CampaignPlayState> SyncAsync(
        StoredCampaign campaign,
        CampaignPlayState play,
        DateTimeOffset utcNow,
        IUserAccountStore? accounts,
        bool revised,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(play);
        var withPlay = CampaignPlayPipeline.Clone(
            campaign,
            play,
            campaign.MapGraph,
            campaign.EndsUtc,
            campaign.RoundCount,
            campaign.UpdatedUtc);
        if (CampaignLifecycle.Progress(withPlay, utcNow).Status != CampaignStatus.Completed)
        {
            return play;
        }

        var participants = accounts is null
            ? (IReadOnlyList<CampaignParticipantDetail>)[]
            : await CampaignPlayMapper.ParticipantsAsync(campaign, accounts, cancellationToken).ConfigureAwait(false);
        var scoring = CampaignPointStandingsMapper.ToScoring(
            withPlay,
            participants,
            viewerUserId: Guid.Empty,
            staffView: false,
            utcNow);
        var names = participants.ToDictionary(static item => item.UserId, static item => item.DisplayName);
        var scores = scoring.Standings
            .Select(row => (
                Player: names.GetValueOrDefault(row.UserId) ?? row.DisplayName,
                row.Total))
            .ToArray();
        var items = ItemLines(play, campaign, names);
        var firstMessage = CampaignCompletionLogRules.Format(scores, items, revised: false);
        var last = play.Log.LastOrDefault(static entry => entry.Kind == PlayLogKind.CampaignEnded);
        if (last is null)
        {
            return play.AppendLog(Entry(utcNow, firstMessage));
        }

        var revisedMessage = CampaignCompletionLogRules.Format(scores, items, revised: true);
        if (!revised || string.Equals(last.Message, firstMessage, StringComparison.Ordinal)
            || string.Equals(last.Message, revisedMessage, StringComparison.Ordinal))
        {
            return play;
        }

        return play.AppendLog(Entry(utcNow, revisedMessage));
    }

    private static PlayLogEntry Entry(DateTimeOffset utcNow, string message)
    {
        return new PlayLogEntry(
            Guid.NewGuid(),
            utcNow,
            PlayLogKind.CampaignEnded,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [],
            message);
    }

    private static List<string> ItemLines(
        CampaignPlayState play,
        StoredCampaign campaign,
        IReadOnlyDictionary<Guid, string> names)
    {
        var forces = play.Forces.ToDictionary(static force => force.Id);
        var territories = campaign.MapGraph?.Territories.ToDictionary(static item => item.Id) ?? [];
        var lines = new List<string>();
        foreach (var item in play.ItemObjectives
                     .Where(static objective => !objective.IsDestroyed && objective.IsRevealed)
                     .OrderBy(static objective => objective.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (item.PossessorForceId is { } forceId && forces.TryGetValue(forceId, out var force))
            {
                var holder = names.GetValueOrDefault(force.ControllerUserId) ?? "a player";
                lines.Add($"{item.Name} held by {holder}");
                continue;
            }

            if (item.TerritoryId is { } territoryId
                && territories.TryGetValue(territoryId, out var territory)
                && !string.IsNullOrWhiteSpace(territory.Name))
            {
                lines.Add($"{item.Name} in {territory.Name}");
                continue;
            }

            lines.Add(item.Name);
        }

        return lines;
    }
}
