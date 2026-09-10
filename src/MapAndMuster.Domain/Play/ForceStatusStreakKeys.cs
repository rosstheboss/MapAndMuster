using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Domain.Play;

/// <summary>
/// Play-state keys for consecutive enable and clear trigger streaks.
/// </summary>
public static class ForceStatusStreakKeys
{
    /// <summary>
    /// Returns the enable-streak key for one catalog status and trigger.
    /// </summary>
    public static string Enable(Guid statusId, ForceStatusEnableTrigger trigger)
    {
        return $"{statusId:D}:{trigger}";
    }

    /// <summary>
    /// Returns the clear-streak key for one trigger on the force's current status.
    /// </summary>
    public static string Clear(ForceStatusClearTrigger trigger)
    {
        return trigger.ToString();
    }

    /// <summary>
    /// Parses an enable-streak key written by <see cref="Enable"/>.
    /// </summary>
    public static bool TryParseEnable(string key, out Guid statusId, out ForceStatusEnableTrigger trigger)
    {
        statusId = Guid.Empty;
        trigger = default;
        var separator = key.LastIndexOf(':');
        if (separator <= 0 || separator == key.Length - 1)
        {
            return false;
        }

        return Guid.TryParse(key[..separator], out statusId)
            && Enum.TryParse(key[(separator + 1)..], ignoreCase: true, out trigger)
            && Enum.IsDefined(trigger);
    }
}
