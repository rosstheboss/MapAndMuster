namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Resolves which missions apply to a territory: structure missions when present, otherwise terrain missions.
/// </summary>
public static class TerritoryMissionRules
{
    /// <summary>
    /// Returns structure missions when the structure has any; otherwise returns the terrain missions.
    /// </summary>
    /// <param name="terrain">The territory terrain type.</param>
    /// <param name="structure">The optional structure type.</param>
    /// <returns>The missions that apply.</returns>
    public static IReadOnlyList<MissionSetup> Resolve(TerrainTypeSetup terrain, StructureTypeSetup? structure)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        if (structure is not null && structure.Missions.Count > 0)
        {
            return structure.Missions;
        }

        return terrain.Missions;
    }
}
