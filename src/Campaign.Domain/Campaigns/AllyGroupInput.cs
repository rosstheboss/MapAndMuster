namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied ally group configuration for campaign setup.
/// </summary>
public sealed class AllyGroupInput
{
    /// <summary>Gets the ally-group display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color as #RRGGBB. A palette color is assigned when omitted.</summary>
    public string? Color { get; init; }
}
