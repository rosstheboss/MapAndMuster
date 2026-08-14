using Campaign.Application.Campaigns;
using Campaign.Application.Maps;

namespace Campaign.Application.Ports;

/// <summary>
/// Persistence for campaign setup, membership, and map metadata.
/// </summary>
public interface ICampaignStore
{
    /// <summary>
    /// Creates a campaign and its initial memberships.
    /// </summary>
    /// <param name="campaign">The campaign to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored campaign.</returns>
    Task<StoredCampaign> AddAsync(StoredCampaign campaign, CancellationToken cancellationToken);

    /// <summary>
    /// Finds a campaign by identifier, including members who are not the caller.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The campaign, or <see langword="null"/>.</returns>
    Task<StoredCampaign?> FindByIdAsync(Guid campaignId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists campaigns the user manages or participates in.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The campaigns, newest first.</returns>
    Task<IReadOnlyList<StoredCampaign>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces campaign setup when the revision matches.
    /// </summary>
    /// <param name="campaign">The campaign to persist.</param>
    /// <param name="expectedRevision">The last observed revision.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign, or a concurrency/not-found failure.</returns>
    Task<UpdateStoredCampaignOutcome> UpdateAsync(
        StoredCampaign campaign,
        int expectedRevision,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a campaign when it exists.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when a row was deleted.</returns>
    Task<bool> DeleteAsync(Guid campaignId, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the overlay territory graph when the campaign revision matches.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="graph">The validated graph to persist.</param>
    /// <param name="expectedRevision">The last observed revision.</param>
    /// <param name="updatedUtc">The edit instant.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign, or a concurrency/not-found failure.</returns>
    Task<UpdateStoredCampaignOutcome> UpdateMapGraphAsync(
        Guid campaignId,
        StoredMapGraph graph,
        int expectedRevision,
        DateTimeOffset updatedUtc,
        CancellationToken cancellationToken);
}
