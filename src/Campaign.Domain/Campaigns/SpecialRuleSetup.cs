namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated reusable special rule. User-created rules are display-only.
/// </summary>
public sealed class SpecialRuleSetup
{
    /// <summary>
    /// Initializes a validated special rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="name">The unique rule name.</param>
    /// <param name="text">The player-facing rule text.</param>
    public SpecialRuleSetup(Guid id, string name, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(text);
        Id = id;
        Name = name;
        Text = text;
    }

    /// <summary>Gets the rule identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the unique rule name.</summary>
    public string Name { get; }

    /// <summary>Gets the player-facing rule text.</summary>
    public string Text { get; }
}
