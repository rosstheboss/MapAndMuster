using MapAndMuster.Application.Campaigns;
using MapAndMuster.Domain.Campaigns;

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
    /// Finds a preset by its unique-name key.
    /// </summary>
    /// <param name="name">The display name to normalize and match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The preset, or <see langword="null"/> when no preset uses that name.</returns>
    /// <remarks>
    /// The default implementation scans the list. Real storage should match the indexed
    /// normalized name instead of loading every preset.
    /// </remarks>
    async Task<StoredCampaign?> FindByNameAsync(string name, CancellationToken cancellationToken)
    {
        var key = CampaignSetupRules.UniqueNameKey(name);
        var listed = await ListAsync(cancellationToken).ConfigureAwait(false);
        var match = listed.FirstOrDefault(item => CampaignSetupRules.UniqueNameKey(item.Name) == key);
        return match is null ? null : await FindByIdAsync(match.Id, cancellationToken).ConfigureAwait(false);
    }

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
