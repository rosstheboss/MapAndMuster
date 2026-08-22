using MapAndMuster.Application.Ports;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Deletes stored campaign files only when no remaining campaign references them.
/// </summary>
internal static class CampaignAssetRetention
{
    public static async Task DeleteIfUnreferencedAsync(
        ICampaignStore campaigns,
        Func<string, CancellationToken, Task> deleteAsync,
        string? storageKey,
        Guid excludingCampaignId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(deleteAsync);
        if (!CatalogFileBinder.IsUserUploadedFileKey(storageKey))
        {
            return;
        }

        if (await campaigns.IsStorageKeyInUseAsync(storageKey, excludingCampaignId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await deleteAsync(storageKey, cancellationToken).ConfigureAwait(false);
    }
}
