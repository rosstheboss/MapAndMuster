namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// One test-player assignment covering a faction, or a named subfaction of that faction.
/// </summary>
/// <param name="FactionName">The campaign faction name.</param>
/// <param name="Subfaction">The subfaction when this slot represents one, otherwise null.</param>
public sealed record TestCampaignPlayerSlot(string FactionName, string? Subfaction);

/// <summary>
/// Builds one player slot per faction and per named subfaction so local test campaigns can run every army.
/// </summary>
public static class TestCampaignRoster
{
    /// <summary>
    /// Returns slots ordered by faction name, then an unsubfacted parent when allowed, then each subfaction.
    /// </summary>
    /// <param name="factions">The campaign factions.</param>
    /// <returns>One slot per faction and named subfaction.</returns>
    public static IReadOnlyList<TestCampaignPlayerSlot> Slots(IReadOnlyList<StoredFaction> factions)
    {
        ArgumentNullException.ThrowIfNull(factions);
        var slots = new List<TestCampaignPlayerSlot>();
        foreach (var faction in factions.OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var subfactions = faction.Subfactions
                .Select(static name => name.Trim())
                .Where(static name => name.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (faction.RequiresSubfaction)
            {
                foreach (var subfaction in subfactions)
                {
                    slots.Add(new TestCampaignPlayerSlot(faction.Name, subfaction));
                }

                continue;
            }

            slots.Add(new TestCampaignPlayerSlot(faction.Name, null));
            foreach (var subfaction in subfactions)
            {
                slots.Add(new TestCampaignPlayerSlot(faction.Name, subfaction));
            }
        }

        return slots;
    }
}
