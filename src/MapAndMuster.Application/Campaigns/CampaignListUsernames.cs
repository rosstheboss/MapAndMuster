using MapAndMuster.Application.Ports;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Loads usernames for campaign list filters.
/// </summary>
internal static class CampaignListUsernames
{
    public static async Task<IReadOnlyDictionary<Guid, string>> LoadAsync(
        IReadOnlyList<StoredCampaign> campaigns,
        IUserAccountStore? accounts,
        CancellationToken cancellationToken)
    {
        if (accounts is null || campaigns.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var ids = campaigns
            .SelectMany(static campaign => campaign.Memberships.Select(static member => member.UserId))
            .Distinct()
            .ToArray();
        var found = await accounts.FindManyByIdAsync(ids, cancellationToken).ConfigureAwait(false);
        return found.ToDictionary(static pair => pair.Key, static pair => pair.Value.Username);
    }
}
