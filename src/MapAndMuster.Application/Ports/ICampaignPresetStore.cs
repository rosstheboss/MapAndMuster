using MapAndMuster.Application.Campaigns;

namespace MapAndMuster.Application.Ports;

/// <summary>
/// Persistence for named campaign setup presets, including map graph and image keys.
/// </summary>
public interface ICampaignPresetStore
{
    /// <summary>
    /// Lists saved presets by name.
    /// </summary>
    Task<IReadOnlyList<CampaignPresetListItem>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Finds a preset and reconstructs it as a stored campaign snapshot.
    /// </summary>
    Task<StoredCampaign?> FindByIdAsync(Guid presetId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or replaces a preset with the given name using the campaign's current setup and map.
    /// </summary>
    Task<CampaignPresetListItem> UpsertFromCampaignAsync(
        string name,
        StoredCampaign campaign,
        Guid createdByUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    /// <summary>
    /// Whether any preset other than <paramref name="excludingPresetId"/> still references the storage key.
    /// </summary>
    Task<bool> IsStorageKeyInUseAsync(
        string storageKey,
        Guid? excludingPresetId,
        CancellationToken cancellationToken);
}
