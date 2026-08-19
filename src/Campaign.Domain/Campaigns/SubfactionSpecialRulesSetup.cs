namespace Campaign.Domain.Campaigns;

/// <summary>
/// Special rules assigned to one named subfaction.
/// </summary>
public sealed class SubfactionSpecialRulesSetup
{
    /// <summary>
    /// Initializes a subfaction rule assignment.
    /// </summary>
    /// <param name="name">The subfaction name.</param>
    /// <param name="specialRuleIds">Special rules assigned to this subfaction.</param>
    public SubfactionSpecialRulesSetup(string name, IReadOnlyList<Guid> specialRuleIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(specialRuleIds);
        Name = name;
        SpecialRuleIds = specialRuleIds;
    }

    /// <summary>Gets the subfaction name.</summary>
    public string Name { get; }

    /// <summary>Gets special rules assigned to this subfaction.</summary>
    public IReadOnlyList<Guid> SpecialRuleIds { get; }
}

/// <summary>
/// User-supplied special-rule assignment for one subfaction.
/// </summary>
public sealed class SubfactionSpecialRulesInput
{
    /// <summary>Gets the subfaction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets special-rule identifiers assigned to this subfaction.</summary>
    public IReadOnlyList<Guid>? SpecialRuleIds { get; init; }
}
