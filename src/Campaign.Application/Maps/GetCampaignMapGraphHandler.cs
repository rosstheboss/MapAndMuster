using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.Maps;

namespace Campaign.Application.Maps;

/// <summary>
/// Reads the overlay territory graph for a campaign member.
/// </summary>
public sealed class GetCampaignMapGraphHandler
{
    private readonly ICampaignStore _campaigns;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    public GetCampaignMapGraphHandler(ICampaignStore campaigns)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        _campaigns = campaigns;
    }

    /// <summary>
    /// Returns the stored overlay graph. Non-members receive not-found.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The map graph.</returns>
    public async Task<OperationResult<CampaignMapGraphDetail>> HandleAsync(
        Guid campaignId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        var membership = campaign is null ? null : CampaignMapper.MembershipFor(campaign, userId);
        if (campaign is null || membership is null)
        {
            return OperationResults.Failure<CampaignMapGraphDetail>(
                ErrorCodes.CampaignNotFound,
                "The campaign was not found.");
        }

        var stored = campaign.MapGraph ?? MapGraphMapper.Empty();
        var factionIds = campaign.Factions.Select(static faction => faction.Id).ToHashSet();
        var terrainIds = campaign.TerrainTypes.Select(static type => type.Id).ToHashSet();
        var structureIds = campaign.StructureTypes.Select(static type => type.Id).ToHashSet();
        if (!CampaignMapGraphRules.TryCreate(
                MapGraphMapper.ToTerritoryInputs(stored),
                MapGraphMapper.ToAdjacencyInputs(stored),
                factionIds,
                terrainIds,
                structureIds,
                out var graph,
                out _))
        {
            graph = new CampaignMapGraph([], []);
        }

        return OperationResults.Success(
            MapGraphMapper.ToDetail(campaign.Id, campaign.Revision, membership.IsGameMaster, graph));
    }
}
