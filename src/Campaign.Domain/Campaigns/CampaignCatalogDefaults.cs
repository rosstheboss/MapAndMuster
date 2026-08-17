using Campaign.Domain.Maps;

namespace Campaign.Domain.Campaigns;

/// <summary>
/// Default terrain types, structure types, and mission names used when setup omits a catalog.
/// </summary>
public static class CampaignCatalogDefaults
{
    /// <summary>
    /// Creates the initial terrain catalog with unique colors and one mission per type.
    /// </summary>
    /// <returns>Terrain type inputs in alphabetical order.</returns>
    public static IReadOnlyList<TerrainTypeInput> TerrainTypes()
    {
        return
        [
            .. TerrainCatalog.All.Select(static entry => new TerrainTypeInput
            {
                Name = entry.Label,
                Color = entry.OverlayColor,
                Missions = [new MissionInput { Name = DefaultMissionName(entry.Label) }],
            }),
        ];
    }

    /// <summary>
    /// Creates the initial structure catalog with built-in logos and no missions.
    /// </summary>
    /// <returns>Structure type inputs in alphabetical order.</returns>
    public static IReadOnlyList<StructureTypeInput> StructureTypes()
    {
        return
        [
            .. StructureCatalog.All.Select(static entry =>
            {
                var flags = StructureCatalog.DefaultFlags(entry.Type);
                return new StructureTypeInput
                {
                    Name = entry.Label,
                    BuiltinSymbol = entry.Type.ToString(),
                    IsBuildable = flags.IsBuildable,
                    IsPillageable = flags.IsPillageable,
                    IsDestructible = flags.IsDestructible,
                };
            }),
        ];
    }

    /// <summary>
    /// Returns the default mission name for a terrain type.
    /// </summary>
    /// <param name="terrainLabel">The terrain type label.</param>
    /// <returns>The mission name.</returns>
    public static string DefaultMissionName(string terrainLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(terrainLabel);
        return $"{terrainLabel.Trim()} control";
    }

    /// <summary>
    /// Returns whether <paramref name="symbol"/> is a built-in structure logo key.
    /// </summary>
    /// <param name="symbol">The logo key.</param>
    /// <returns><see langword="true"/> when the key is a default structure symbol.</returns>
    public static bool IsBuiltinStructureSymbol(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return false;
        }

        foreach (var entry in StructureCatalog.All)
        {
            if (string.Equals(entry.Type.ToString(), symbol, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the canonical built-in structure logo key.
    /// </summary>
    /// <param name="symbol">The supplied key.</param>
    /// <returns>The canonical key, or <see langword="null"/>.</returns>
    public static string? CanonicalBuiltinSymbol(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        foreach (var entry in StructureCatalog.All)
        {
            var key = entry.Type.ToString();
            if (string.Equals(key, symbol, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.Label, symbol, StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return null;
    }
}
