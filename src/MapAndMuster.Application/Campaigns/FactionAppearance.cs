using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Resolves the displayed color, flag, and logo for a faction or named subfaction.
/// </summary>
internal static class FactionAppearance
{
    public sealed record Resolved(string Color, bool HasFlagImage, bool TintFlagImage, string? FlagImageStorageKey);

    public static Resolved Resolve(StoredFaction faction, string? subfactionName)
    {
        ArgumentNullException.ThrowIfNull(faction);
        var appearance = Find(faction, subfactionName);
        var color = string.IsNullOrWhiteSpace(appearance?.Color) ? faction.Color : appearance.Color;
        var source = SubfactionFlagSource.Normalize(appearance?.FlagSource) ?? SubfactionFlagSource.Inherit;
        if (source == SubfactionFlagSource.Color)
        {
            return new Resolved(color, false, false, null);
        }

        if (source == SubfactionFlagSource.Image)
        {
            if (!string.IsNullOrWhiteSpace(appearance?.FlagImageStorageKey))
            {
                return new Resolved(color, true, appearance.TintFlagImage, appearance.FlagImageStorageKey);
            }

            return new Resolved(
                color,
                !string.IsNullOrWhiteSpace(faction.FlagImageStorageKey),
                faction.TintFlagImage,
                faction.FlagImageStorageKey);
        }

        return new Resolved(
            color,
            !string.IsNullOrWhiteSpace(faction.FlagImageStorageKey),
            faction.TintFlagImage,
            faction.FlagImageStorageKey);
    }

    public static StoredSubfactionAppearance? Find(StoredFaction faction, string? subfactionName)
    {
        if (string.IsNullOrWhiteSpace(subfactionName))
        {
            return null;
        }

        return faction.SubfactionAppearances.FirstOrDefault(item =>
            string.Equals(item.Name, subfactionName, StringComparison.OrdinalIgnoreCase));
    }

    public static StoredFaction WithSubfactionFlag(
        StoredFaction faction,
        string subfactionName,
        string? flagImageStorageKey)
    {
        ArgumentNullException.ThrowIfNull(faction);
        ArgumentException.ThrowIfNullOrWhiteSpace(subfactionName);
        var appearances = faction.SubfactionAppearances.ToList();
        var index = appearances.FindIndex(item =>
            string.Equals(item.Name, subfactionName, StringComparison.OrdinalIgnoreCase));
        var previous = index >= 0
            ? appearances[index]
            : new StoredSubfactionAppearance
            {
                Name = subfactionName,
                Color = null,
                FlagSource = SubfactionFlagSource.Image,
                FlagImageStorageKey = null,
                TintFlagImage = false,
            };
        var updated = new StoredSubfactionAppearance
        {
            Name = previous.Name,
            Color = previous.Color,
            FlagSource = SubfactionFlagSource.Image,
            FlagImageStorageKey = flagImageStorageKey,
            TintFlagImage = previous.TintFlagImage,
        };
        if (index >= 0)
        {
            appearances[index] = updated;
        }
        else
        {
            appearances.Add(updated);
        }

        return new StoredFaction
        {
            Id = faction.Id,
            Name = faction.Name,
            Color = faction.Color,
            Subfactions = faction.Subfactions,
            SubfactionAppearances = appearances,
            AllyGroupName = faction.AllyGroupName,
            RequiresSubfaction = faction.RequiresSubfaction,
            FlagImageStorageKey = faction.FlagImageStorageKey,
            TintFlagImage = faction.TintFlagImage,
            SpecialRuleIds = faction.SpecialRuleIds,
            SubfactionSpecialRules = faction.SubfactionSpecialRules,
        };
    }
}
