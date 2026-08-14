namespace Campaign.Domain.Maps;

/// <summary>
/// Whether an adjacency was suggested from shared borders or added by a user.
/// </summary>
public enum AdjacencyOrigin
{
    /// <summary>Created by Generate Connections from shared polygon borders.</summary>
    Generated = 0,

    /// <summary>Created by a user. Regeneration must keep it and skip that territory pair.</summary>
    Manual = 1,
}
