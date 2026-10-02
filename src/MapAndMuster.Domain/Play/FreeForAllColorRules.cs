namespace MapAndMuster.Domain.Play;

/// <summary>
/// Assigns a unique color to each player in a free-for-all.
/// A lone player of a faction or subfaction keeps that color. Later duplicates receive a different color.
/// </summary>
public static class FreeForAllColorRules
{
    private static readonly string[] Palette =
    [
        "#2563EB", "#DC2626", "#16A34A", "#CA8A04", "#7C3AED", "#EA580C", "#0891B2", "#BE185D",
        "#4B5563", "#65A30D", "#C026D3", "#0F766E", "#1D4ED8", "#B45309", "#15803D", "#6D28D9",
        "#9F1239", "#0369A1", "#A16207", "#334155", "#DB2777", "#047857", "#7C2D12", "#4338CA",
    ];

    /// <summary>A player who needs a color.</summary>
    public sealed record Player(Guid UserId, Guid FactionId, string? Subfaction, string FactionColor, string? SubfactionColor);

    /// <summary>Returns a color for each user.</summary>
    public static IReadOnlyDictionary<Guid, string> Assign(IReadOnlyList<Player> players)
    {
        ArgumentNullException.ThrowIfNull(players);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<Guid, string>();
        var groups = players.GroupBy(static player => (player.FactionId, Subfaction: player.Subfaction ?? string.Empty));
        var unique = new List<Player>();
        var duplicates = new List<Player>();
        foreach (var group in groups)
        {
            var ordered = group.OrderBy(static player => player.UserId).ToList();
            unique.Add(ordered[0]);
            duplicates.AddRange(ordered.Skip(1));
        }

        foreach (var player in unique.OrderBy(static player => player.UserId))
        {
            var preferred = player.SubfactionColor ?? player.FactionColor;
            result[player.UserId] = Take(preferred, used);
        }

        foreach (var player in duplicates.OrderBy(static player => player.UserId))
        {
            var preferred = player.SubfactionColor ?? player.FactionColor;
            result[player.UserId] = Take(preferred, used);
        }

        return result;
    }

    private static string Take(string preferred, HashSet<string> used)
    {
        if (used.Add(preferred))
        {
            return preferred;
        }

        foreach (var color in Palette)
        {
            if (used.Add(color))
            {
                return color;
            }
        }

        var generated = $"#{used.Count:X6}";
        used.Add(generated);
        return generated;
    }
}
