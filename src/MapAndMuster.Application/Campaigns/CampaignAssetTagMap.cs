using System.Globalization;
using MapAndMuster.Application.Common;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Builds the per-campaign table of asset cache tags sent to clients.
/// </summary>
/// <remarks>
/// The keys are a contract shared with the web client, which uses them to build asset URLs of
/// the form <c>?t={tag}</c>. Because the tag changes only when the underlying file is replaced,
/// those URLs are stable across campaign revisions and the responses can be cached immutably.
/// </remarks>
public static class CampaignAssetTagMap
{
    /// <summary>Key for the campaign map image.</summary>
    public const string MapKey = "map";

    /// <summary>Builds the key for a structure logo.</summary>
    /// <param name="structureTypeId">The structure type identifier.</param>
    /// <param name="pillaged">Whether this is the pillaged variant.</param>
    /// <returns>The lookup key.</returns>
    public static string StructureKey(Guid structureTypeId, bool pillaged)
        => string.Create(CultureInfo.InvariantCulture, $"{(pillaged ? "structure-pillaged" : "structure")}:{structureTypeId}");

    /// <summary>Builds the key for an item-objective logo.</summary>
    /// <param name="itemObjectiveTypeId">The item objective type identifier.</param>
    /// <returns>The lookup key.</returns>
    public static string ItemKey(Guid itemObjectiveTypeId)
        => string.Create(CultureInfo.InvariantCulture, $"item:{itemObjectiveTypeId}");

    /// <summary>Builds the key for a faction or subfaction flag.</summary>
    /// <param name="factionId">The faction identifier.</param>
    /// <param name="subfactionName">The subfaction name, or <see langword="null"/> for the faction itself.</param>
    /// <returns>The lookup key.</returns>
    public static string FactionKey(Guid factionId, string? subfactionName)
    {
        var trimmed = subfactionName?.Trim();
        return string.IsNullOrEmpty(trimmed)
            ? string.Create(CultureInfo.InvariantCulture, $"faction:{factionId}")
            : string.Create(CultureInfo.InvariantCulture, $"faction:{factionId}:{trimmed.ToLowerInvariant()}");
    }

    /// <summary>
    /// Collects a tag for every stored file the campaign references.
    /// </summary>
    /// <param name="campaign">The stored campaign or preset.</param>
    /// <returns>Asset keys mapped to opaque cache tags. Entries without a stored file are omitted.</returns>
    public static IReadOnlyDictionary<string, string> Build(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var tags = new Dictionary<string, string>(StringComparer.Ordinal);

        Add(tags, MapKey, campaign.MapStorageKey);

        foreach (var structure in campaign.StructureTypes)
        {
            Add(tags, StructureKey(structure.Id, pillaged: false), structure.ImageStorageKey);
            Add(tags, StructureKey(structure.Id, pillaged: true), structure.PillagedImageStorageKey);
        }

        foreach (var item in campaign.ItemObjectiveTypes)
        {
            Add(tags, ItemKey(item.Id), item.ImageStorageKey);
        }

        foreach (var faction in campaign.Factions)
        {
            Add(tags, FactionKey(faction.Id, null), faction.FlagImageStorageKey);
            foreach (var subfaction in faction.Subfactions)
            {
                // Resolve so an inheriting subfaction gets the parent's tag and therefore the
                // parent's cached image, instead of appearing to have no flag.
                Add(tags, FactionKey(faction.Id, subfaction), FactionAppearance.Resolve(faction, subfaction).FlagImageStorageKey);
            }
        }

        return tags;
    }

    private static void Add(Dictionary<string, string> tags, string key, string? storageKey)
    {
        if (AssetTags.For(storageKey) is { } tag)
        {
            tags[key] = tag;
        }
    }
}
