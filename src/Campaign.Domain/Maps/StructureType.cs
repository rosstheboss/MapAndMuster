namespace Campaign.Domain.Maps;

/// <summary>
/// Optional structure occupying a territory. At most one structure is allowed. Names are alphabetical.
/// </summary>
public enum StructureType
{
    /// <summary>A faction capital.</summary>
    CapitalCity = 0,

    /// <summary>A fortified keep.</summary>
    Castle = 1,

    /// <summary>A large settlement.</summary>
    City = 2,

    /// <summary>A defensive work that is not a castle.</summary>
    Fortification = 3,

    /// <summary>A supply cache.</summary>
    SupplyDepot = 4,

    /// <summary>A small settlement.</summary>
    Town = 5,
}
