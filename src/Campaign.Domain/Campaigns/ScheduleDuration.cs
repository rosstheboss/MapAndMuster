namespace Campaign.Domain.Campaigns;

/// <summary>
/// A positive length in one allowed unit.
/// </summary>
public sealed class ScheduleDuration
{
    /// <summary>
    /// Initializes a duration.
    /// </summary>
    /// <param name="amount">The amount in <paramref name="unit"/>.</param>
    /// <param name="unit">The unit.</param>
    public ScheduleDuration(int amount, DurationUnit unit)
    {
        Amount = amount;
        Unit = unit;
    }

    /// <summary>Gets the amount.</summary>
    public int Amount { get; }

    /// <summary>Gets the unit.</summary>
    public DurationUnit Unit { get; }
}
