namespace Campaign.Domain.Campaigns;

/// <summary>
/// Allowed units for round, action, and battle-phase lengths.
/// </summary>
public enum DurationUnit
{
    /// <summary>Whole minutes, from 1 to 60.</summary>
    Minutes = 0,

    /// <summary>Whole hours, from 1 to 24.</summary>
    Hours = 1,

    /// <summary>Whole days, from 1 to 7.</summary>
    Days = 2,

    /// <summary>Whole weeks, from 1 to 52.</summary>
    Weeks = 3,

    /// <summary>Calendar months, from 1 to 12.</summary>
    Months = 4,
}
