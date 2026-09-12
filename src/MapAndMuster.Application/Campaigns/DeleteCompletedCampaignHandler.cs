using MapAndMuster.Application.Common;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Permanently deletes a completed campaign and unreferenced uploaded files.
/// </summary>
public sealed class DeleteCompletedCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly ICampaignMapStorage _maps;
    private readonly ICampaignAssetStorage _assets;
    private readonly ICampaignPresetStore _presets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public DeleteCompletedCampaignHandler(
        ICampaignStore campaigns,
        IClock clock,
        ICampaignMapStorage maps,
        ICampaignAssetStorage assets,
        ICampaignPresetStore presets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(presets);
        _campaigns = campaigns;
        _clock = clock;
        _maps = maps;
        _assets = assets;
        _presets = presets;
    }

    /// <summary>
    /// Deletes the campaign when the caller is a manager or administrator and play is finished.
    /// </summary>
    public async Task<OperationResult> HandleAsync(
        DeleteCompletedCampaignCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResult.Failure(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!CampaignAccess.CanStaffMembers(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResult.Failure(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager or administrator can delete this campaign.");
        }

        if (CampaignLifecycle.Progress(campaign, _clock.UtcNow).Status != CampaignStatus.Completed)
        {
            return OperationResult.Failure(
                ErrorCodes.CampaignNotCompleted,
                "Only a completed campaign can be deleted.");
        }

        var storageKeys = CatalogFileBinder.CollectCampaignStorageKeys(campaign)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (!await _campaigns.DeleteAsync(command.CampaignId, cancellationToken).ConfigureAwait(false))
        {
            return OperationResult.Failure(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        foreach (var key in storageKeys)
        {
            if (key.StartsWith("maps/", StringComparison.Ordinal))
            {
                await CampaignAssetRetention.DeleteIfUnreferencedAsync(
                        _campaigns,
                        _maps.DeleteAsync,
                        key,
                        command.CampaignId,
                        cancellationToken,
                        _presets)
                    .ConfigureAwait(false);
            }
            else
            {
                await CampaignAssetRetention.DeleteIfUnreferencedAsync(
                        _campaigns,
                        _assets.DeleteAsync,
                        key,
                        command.CampaignId,
                        cancellationToken,
                        _presets)
                    .ConfigureAwait(false);
            }
        }

        return OperationResult.Success();
    }
}
