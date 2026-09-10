namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// How many times in a row an enable or clear trigger must match before it applies.
/// </summary>
public static class ForceStatusOccurrences
{
    /// <summary>A single matching trigger is enough.</summary>
    public const int Min = 1;

    /// <summary>Longest consecutive streak a catalog status may require.</summary>
    public const int Max = 10;

    /// <summary>Default for new and existing statuses that omit a count.</summary>
    public const int Default = Min;

    /// <summary>
    /// Returns whether <paramref name="value"/> is an allowed consecutive-occurrence count.
    /// </summary>
    public static bool IsValid(int value)
    {
        return value is >= Min and <= Max;
    }

    /// <summary>
    /// Returns <paramref name="value"/> when it is in range; otherwise <see cref="Default"/>.
    /// </summary>
    public static int Normalize(int? value)
    {
        return value is { } supplied && IsValid(supplied) ? supplied : Default;
    }
}
