namespace Campaign.Domain.Campaigns;

/// <summary>
/// How an item objective is placed on the map when the campaign launches.
/// </summary>
public enum ItemObjectivePlacementKind
{
    /// <summary>The application chooses an eligible territory at launch.</summary>
    Random = 0,

    /// <summary>The manager assigned a territory on the overlay graph.</summary>
    Placed = 1,
}
