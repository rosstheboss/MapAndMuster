namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// User-supplied reusable special rule for campaign setup.
/// </summary>
public sealed class SpecialRuleInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the rule name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the rule text shown to players.</summary>
    public string? Text { get; init; }

    /// <summary>Gets the mechanical policy key, when this rule is enforced or calculated.</summary>
    public string? EffectKey { get; init; }
}
