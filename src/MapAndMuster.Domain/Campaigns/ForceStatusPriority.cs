namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Unique per-campaign ranking for configured force statuses. Lower numbers win.
/// </summary>
public static class ForceStatusPriority
{
    /// <summary>Highest-priority value (wins over every larger number).</summary>
    public const int Min = 0;

    /// <summary>Lowest-priority value allowed on a status.</summary>
    public const int Max = 999;

    /// <summary>
    /// Returns whether <paramref name="value"/> is an allowed priority.
    /// </summary>
    public static bool IsValid(int value)
    {
        return value is >= Min and <= Max;
    }

    /// <summary>
    /// Returns the lowest unused priority in <see cref="Min"/>..<see cref="Max"/>.
    /// </summary>
    public static int NextAvailable(IEnumerable<int> used)
    {
        ArgumentNullException.ThrowIfNull(used);
        var taken = used.ToHashSet();
        for (var value = Min; value <= Max; value++)
        {
            if (!taken.Contains(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException("No force-status priority remains.");
    }
}
