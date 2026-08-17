namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied hidden or public item objective for campaign setup.
/// </summary>
public sealed class ItemObjectiveTypeInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the item name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the item stays hidden until found or staff-revealed. Defaults to true.</summary>
    public bool? IsHiddenUntilFound { get; init; }

    /// <summary>Gets Random or Placed. Defaults to Random.</summary>
    public string? Placement { get; init; }

    /// <summary>Gets whether the item may spawn on a faction spawn territory. Defaults to false.</summary>
    public bool? AllowOnSpawn { get; init; }
}
