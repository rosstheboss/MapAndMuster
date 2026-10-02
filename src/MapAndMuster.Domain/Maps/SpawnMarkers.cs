namespace MapAndMuster.Domain.Maps;

/// <summary>
/// Spawn-faction identifiers that are not a real faction.
/// </summary>
public static class SpawnMarkers
{
    /// <summary>
    /// A general spawn used by random spawn placement. It is not owned by a faction.
    /// </summary>
    public static readonly Guid General = Guid.Parse("00000000-0000-4000-8000-000000000001");

    /// <summary>Returns whether <paramref name="spawnFactionId"/> marks a general spawn.</summary>
    public static bool IsGeneral(Guid? spawnFactionId)
    {
        return spawnFactionId == General;
    }
}
