namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A named subfaction that refuses a catalog force status.
/// </summary>
public sealed class ForceStatusImmuneSubfaction
{
    /// <summary>
    /// Initializes an immune subfaction.
    /// </summary>
    /// <param name="factionId">The parent faction.</param>
    /// <param name="subfaction">The subfaction name.</param>
    public ForceStatusImmuneSubfaction(Guid factionId, string subfaction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subfaction);
        if (factionId == Guid.Empty)
        {
            throw new ArgumentException("A faction identifier is required.", nameof(factionId));
        }

        FactionId = factionId;
        Subfaction = subfaction.Trim();
    }

    /// <summary>Gets the parent faction.</summary>
    public Guid FactionId { get; }

    /// <summary>Gets the subfaction name.</summary>
    public string Subfaction { get; }
}
