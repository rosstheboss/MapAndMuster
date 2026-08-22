namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A validated reusable special rule. User-created rules without an effect key are display-only.
/// </summary>
public sealed class SpecialRuleSetup
{
    /// <summary>
    /// Initializes a validated special rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="name">The unique rule name.</param>
    /// <param name="text">The player-facing rule text.</param>
    /// <param name="effectKey">The mechanical policy key, when this rule is enforced or calculated.</param>
    public SpecialRuleSetup(Guid id, string name, string text, string? effectKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(text);
        Id = id;
        Name = name;
        Text = text;
        EffectKey = string.IsNullOrWhiteSpace(effectKey) ? null : effectKey.Trim();
    }

    /// <summary>Gets the rule identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the unique rule name.</summary>
    public string Name { get; }

    /// <summary>Gets the player-facing rule text.</summary>
    public string Text { get; }

    /// <summary>Gets the mechanical policy key, or null when the rule is display-only.</summary>
    public string? EffectKey { get; }
}
