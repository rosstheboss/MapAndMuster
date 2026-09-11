namespace MapAndMuster.Domain.Play;

/// <summary>
/// Play-state keys for consecutive enable and clear condition streaks.
/// </summary>
public static class ForceStatusStreakKeys
{
    /// <summary>
    /// Returns the enable-streak key for one catalog status and condition.
    /// </summary>
    public static string Enable(Guid statusId, Guid conditionId)
    {
        return $"{statusId:D}:{conditionId:D}";
    }

    /// <summary>
    /// Returns the clear-streak key for one condition on the force's current status.
    /// </summary>
    public static string Clear(Guid conditionId)
    {
        return conditionId.ToString("D");
    }

    /// <summary>
    /// Parses an enable-streak key written by <see cref="Enable"/>.
    /// </summary>
    public static bool TryParseEnable(string key, out Guid statusId, out Guid conditionId)
    {
        statusId = Guid.Empty;
        conditionId = Guid.Empty;
        var separator = key.LastIndexOf(':');
        if (separator <= 0 || separator == key.Length - 1)
        {
            return false;
        }

        return Guid.TryParse(key[..separator], out statusId)
            && Guid.TryParse(key[(separator + 1)..], out conditionId)
            && statusId != Guid.Empty
            && conditionId != Guid.Empty;
    }
}
