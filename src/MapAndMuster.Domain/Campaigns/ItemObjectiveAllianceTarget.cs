namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A faction, optionally scoped to a subfaction, treated as allied while an item is held.
/// </summary>
public sealed class ItemObjectiveAllianceTarget
{
    /// <summary>
    /// Initializes an alliance target.
    /// </summary>
    /// <param name="factionId">The faction treated as allied.</param>
    /// <param name="subfaction">The subfaction scope, or null for the whole faction.</param>
    public ItemObjectiveAllianceTarget(Guid factionId, string? subfaction = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(factionId, Guid.Empty);
        FactionId = factionId;
        Subfaction = string.IsNullOrWhiteSpace(subfaction) ? null : subfaction.Trim();
    }

    /// <summary>Gets the faction treated as allied.</summary>
    public Guid FactionId { get; }

    /// <summary>Gets the subfaction scope, when set.</summary>
    public string? Subfaction { get; }
}
