namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Checks that a campaign can open. A failed check delays the start until a manager fixes it.
/// </summary>
public static class CampaignConfigurationRules
{
    /// <summary>
    /// Free-for-all campaigns need at least one quarter of the player slots, rounded up, as spawn locations.
    /// </summary>
    public static int MinimumFreeForAllSpawnCount(int playerSlotCount)
    {
        if (playerSlotCount <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(playerSlotCount / 4d);
    }

    /// <summary>
    /// Returns the reason the campaign cannot start, or null when configuration is ready.
    /// </summary>
    public static string? StartBlockReason(bool isFreeForAll, int playerSlotCount, int spawnLocationCount)
    {
        if (!isFreeForAll)
        {
            return null;
        }

        var required = MinimumFreeForAllSpawnCount(playerSlotCount);
        if (spawnLocationCount >= required)
        {
            return null;
        }

        return $"Free-for-all needs at least {required} spawn locations (one quarter of {playerSlotCount} players, rounded up) and currently has {spawnLocationCount}.";
    }
}
