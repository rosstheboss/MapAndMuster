namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied faction configuration for campaign setup.
/// </summary>
public sealed class FactionInput
{
    /// <summary>Gets the faction display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets optional subfaction names.</summary>
    public IReadOnlyList<string>? Subfactions { get; init; }

    /// <summary>Gets the optional ally-group name this faction joins.</summary>
    public string? AllyGroupName { get; init; }
}
