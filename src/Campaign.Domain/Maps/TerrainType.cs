namespace Campaign.Domain.Maps;

/// <summary>
/// Catalog terrain for a territory. Names are alphabetical and used as the public list order.
/// </summary>
public enum TerrainType
{
    /// <summary>Coastal sand and surf.</summary>
    Beach = 0,

    /// <summary>Arid sand and rock.</summary>
    Desert = 1,

    /// <summary>Elevated hills and plateaus.</summary>
    Highlands = 2,

    /// <summary>Inland still water.</summary>
    Lake = 3,

    /// <summary>High rocky ground.</summary>
    Mountain = 4,

    /// <summary>Open grassland.</summary>
    Plains = 5,

    /// <summary>River corridors and floodplain.</summary>
    Riverlands = 6,

    /// <summary>Open salt water.</summary>
    Sea = 7,

    /// <summary>Wet, marshy ground.</summary>
    Swamp = 8,

    /// <summary>Underground chambers.</summary>
    Cave = 9,

    /// <summary>Dense woodland.</summary>
    Forest = 10,

    /// <summary>Thick tropical growth.</summary>
    Jungle = 11,
}
