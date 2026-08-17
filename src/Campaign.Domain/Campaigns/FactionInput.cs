namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied faction configuration for campaign setup.
/// </summary>
public sealed class FactionInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the faction display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique faction color as #RRGGBB.</summary>
    public string? Color { get; init; }

    /// <summary>Gets optional subfaction names.</summary>
    public IReadOnlyList<string>? Subfactions { get; init; }

    /// <summary>Gets the optional ally-group name this faction joins.</summary>
    public string? AllyGroupName { get; init; }

    /// <summary>Gets whether a player who chooses this faction must pick a subfaction.</summary>
    public bool RequiresSubfaction { get; init; }

    /// <summary>Gets whether an existing uploaded flag image should be removed.</summary>
    public bool ClearFlagImage { get; init; }

    /// <summary>Gets special-rule identifiers assigned to this faction.</summary>
    public IReadOnlyList<Guid>? SpecialRuleIds { get; init; }
}
