using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.Maps;

namespace Campaign.Application.Maps;

/// <summary>
/// Replaces the overlay territory graph for a campaign manager.
/// </summary>
public sealed class SaveCampaignMapGraphHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    public SaveCampaignMapGraphHandler(ICampaignStore campaigns, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _clock = clock;
    }

    /// <summary>
    /// Validates and stores the overlay graph when the caller is a manager and the revision matches.
    /// </summary>
    /// <param name="command">The save command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored graph.</returns>
    public async Task<OperationResult<CampaignMapGraphDetail>> HandleAsync(
        SaveCampaignMapGraphCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        var membership = existing is null ? null : CampaignMapper.MembershipFor(existing, command.UserId);
        if (existing is null || membership is null)
        {
            return OperationResults.Failure<CampaignMapGraphDetail>(
                ErrorCodes.CampaignNotFound,
                "The campaign was not found.");
        }

        if (!membership.IsGameMaster)
        {
            return OperationResults.Failure<CampaignMapGraphDetail>(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager can edit the campaign map.");
        }

        if (CampaignLifecycle.HasLaunched(existing, _clock.UtcNow))
        {
            return OperationResults.Failure<CampaignMapGraphDetail>(ErrorCodes.CampaignLocked, CampaignLifecycle.LockedMessage);
        }

        var factionIds = existing.Factions.Select(static faction => faction.Id).ToHashSet();
        var terrainIds = existing.TerrainTypes.Select(static type => type.Id).ToHashSet();
        var structureIds = existing.StructureTypes.Select(static type => type.Id).ToHashSet();
        if (!CampaignMapGraphRules.TryCreate(
                command.Territories,
                command.Adjacencies,
                factionIds,
                terrainIds,
                structureIds,
                out var graph,
                out var errors))
        {
            return OperationResults.Failure<CampaignMapGraphDetail>(errors);
        }

        var stored = MapGraphMapper.ToStored(graph, BindPlacements(command, graph, existing));
        var outcome = await _campaigns
            .UpdateMapGraphAsync(command.CampaignId, stored, command.ExpectedRevision, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignMapGraphDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The campaign map could not be saved.");
        }

        return OperationResults.Success(
            MapGraphMapper.ToDetail(outcome.Campaign.Id, outcome.Campaign.Revision, canManage: true, graph, stored.ItemObjectivePlacements));
    }

    private static IReadOnlyList<ItemObjectivePlacementDetail> BindPlacements(
        SaveCampaignMapGraphCommand command,
        CampaignMapGraph graph,
        StoredCampaign existing)
    {
        var territoryIds = graph.Territories.Select(static territory => territory.Id).ToHashSet();
        var placedTypeIds = existing.ItemObjectiveTypes
            .Where(static type => type.Placement.Equals("Placed", StringComparison.OrdinalIgnoreCase))
            .Select(static type => type.Id)
            .ToHashSet();
        return
        [
            .. (command.ItemObjectivePlacements ?? [])
                .Where(item => placedTypeIds.Contains(item.TypeId) && territoryIds.Contains(item.TerritoryId))
                .GroupBy(static item => item.TypeId)
                .Select(static group => new ItemObjectivePlacementDetail
                {
                    TypeId = group.Key,
                    TerritoryId = group.First().TerritoryId,
                }),
        ];
    }
}
