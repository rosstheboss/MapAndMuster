namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied force status for campaign setup. Normal is omitted.
/// </summary>
public sealed class ForceStatusInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the status name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets tabletop effect text shown to players.</summary>
    public string? Effects { get; init; }

    /// <summary>Gets the enable-trigger name.</summary>
    public string? EnableTrigger { get; init; }

    /// <summary>Gets the clear-trigger name.</summary>
    public string? ClearTrigger { get; init; }
}
