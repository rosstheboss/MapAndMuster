using System.Text.RegularExpressions;

namespace Campaign.Domain.Play;

/// <summary>
/// Reads army points and supply-costing unit counts from Warhammer: The Old World list text.
/// </summary>
public static class OldWorldArmyListParser
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

    private static readonly Regex PointsToken = new(
        @"\[(?:(?<grouped>\d{1,3}(?:,\d{3})+)|\d+)\s*(?:pts|points)?\]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        RegexTimeout);

    private static readonly Regex OldWorldBuilderCategory = new(
        @"^\+\+\s*(?<name>Characters|Lords|Heroes|Core|Special|Rare|Mercenaries|Allies)\s*(?:\[[^\]]*\])?\s*\+\+\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        RegexTimeout);

    private static readonly Regex NewRecruitCategory = new(
        @"^\+\s*(?<name>Characters|Lords|Heroes|Core|Special|Rare|Mercenaries|Allies)\s*\+(?:\s*\[[^\]]*\])?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        RegexTimeout);

    private static readonly Regex NewRecruitArmyHeader = new(
        @"^\+\+\s*(?!Total\b).+\[(?:\d{1,3}(?:,\d{3})+|\d+)\s*(?:pts|points)?\]\s*\+\+\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        RegexTimeout);

    /// <summary>
    /// Attempts to parse list text for the selected builder. Other never parses.
    /// </summary>
    public static ArmyListParseResult Parse(string? text, ArmyListBuilder builder)
    {
        if (builder is ArmyListBuilder.Other || string.IsNullOrWhiteSpace(text))
        {
            return ArmyListParseResult.Failed;
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        return builder switch
        {
            ArmyListBuilder.NewRecruit => ParseNewRecruit(normalized),
            ArmyListBuilder.OldWorldBuilder => ParseOldWorldBuilder(normalized),
            _ => ArmyListParseResult.Failed,
        };
    }

    private static ArmyListParseResult ParseOldWorldBuilder(string text)
    {
        if (!LooksLikeOldWorldBuilder(text))
        {
            return ArmyListParseResult.Failed;
        }

        var lines = SplitLines(text);
        var categories = ReadCategories(lines, OldWorldBuilderCategory);
        var armyPoints = ReadArmyPoints(lines, preferTotal: false);
        return Finish(armyPoints, categories);
    }

    private static ArmyListParseResult ParseNewRecruit(string text)
    {
        if (!LooksLikeNewRecruit(text))
        {
            return ArmyListParseResult.Failed;
        }

        var lines = SplitLines(text);
        var categories = ReadCategories(lines, NewRecruitCategory);
        var armyPoints = ReadArmyPoints(lines, preferTotal: true);
        return Finish(armyPoints, categories);
    }

    private static bool LooksLikeOldWorldBuilder(string text)
    {
        if (ContainsIgnoreCase(text, "newrecruit.eu") || ContainsIgnoreCase(text, "Created with New Recruit"))
        {
            return false;
        }

        if (!ContainsIgnoreCase(text, "old-world-builder.com")
            && !ContainsIgnoreCase(text, "Created with \"Old World Builder\"")
            && !ContainsIgnoreCase(text, "Created with Old World Builder"))
        {
            return false;
        }

        return HasCategoryHeader(SplitLines(text), OldWorldBuilderCategory);
    }

    private static bool LooksLikeNewRecruit(string text)
    {
        if (ContainsIgnoreCase(text, "old-world-builder.com")
            || ContainsIgnoreCase(text, "Created with \"Old World Builder\"")
            || ContainsIgnoreCase(text, "Created with Old World Builder"))
        {
            return false;
        }

        var lines = SplitLines(text);
        if (!HasCategoryHeader(lines, NewRecruitCategory))
        {
            return false;
        }

        var hasMarker = ContainsIgnoreCase(text, "newrecruit.eu")
            || ContainsIgnoreCase(text, "Created with New Recruit");
        var hasArmyHeader = lines.Any(static line => TryMatch(NewRecruitArmyHeader, line, out _));
        return hasMarker || hasArmyHeader;
    }

    private static ArmyListParseResult Finish(int armyPoints, List<ArmyListSupplyCategory> categories)
    {
        if (armyPoints <= 0 && categories.Count == 0)
        {
            return ArmyListParseResult.Failed;
        }

        return ArmyListParseResult.Success(armyPoints, categories);
    }

    private static List<ArmyListSupplyCategory> ReadCategories(string[] lines, Regex header)
    {
        var categories = new List<ArmyListSupplyCategory>();
        string? currentName = null;
        var unitCount = 0;
        for (var index = 0; index <= lines.Length; index++)
        {
            var line = index < lines.Length ? lines[index] : null;
            if (line is not null && TryMatch(header, line, out var match))
            {
                AppendCategory(categories, currentName, unitCount);
                currentName = DisplayName(match.Groups["name"].Value);
                unitCount = 0;
                continue;
            }

            if (currentName is null || line is null)
            {
                continue;
            }

            if (IsUnitLine(line))
            {
                unitCount++;
            }
        }

        AppendCategory(categories, currentName, unitCount);
        return categories;
    }

    private static void AppendCategory(List<ArmyListSupplyCategory> categories, string? name, int unitCount)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var costsSupply = CostsSupply(name);
        categories.Add(new ArmyListSupplyCategory(name, unitCount, costsSupply ? unitCount : 0, costsSupply));
    }

    private static int ReadArmyPoints(string[] lines, bool preferTotal)
    {
        if (preferTotal)
        {
            foreach (var line in lines)
            {
                if (line.Contains("Total", StringComparison.OrdinalIgnoreCase)
                    && TryReadPoints(line, out var total))
                {
                    return total;
                }
            }
        }

        foreach (var line in lines)
        {
            if (TryMatch(OldWorldBuilderCategory, line, out _) || TryMatch(NewRecruitCategory, line, out _))
            {
                continue;
            }

            if (line.StartsWith("++", StringComparison.Ordinal)
                || line.Contains('[', StringComparison.Ordinal))
            {
                if (TryReadPoints(line, out var points) && points > 0)
                {
                    return points;
                }
            }
        }

        return 0;
    }

    private static bool IsUnitLine(string line)
    {
        if (line.Length == 0
            || line.StartsWith('-')
            || line.StartsWith('.')
            || line.StartsWith('*')
            || line.StartsWith('#')
            || line.StartsWith('=')
            || line.StartsWith('+')
            || line.StartsWith('[')
            || ContainsIgnoreCase(line, "Created with")
            || ContainsIgnoreCase(line, "http://")
            || ContainsIgnoreCase(line, "https://"))
        {
            return false;
        }

        return TryReadPoints(line, out _);
    }

    private static bool TryReadPoints(string line, out int points)
    {
        points = 0;
        try
        {
            var matches = PointsToken.Matches(line);
            if (matches.Count == 0)
            {
                return false;
            }

            var token = matches[^1];
            var raw = token.Groups["grouped"].Success
                ? token.Groups["grouped"].Value
                : token.Value.Trim('[', ']');
            raw = raw.Replace("pts", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("points", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(",", string.Empty, StringComparison.Ordinal)
                .Trim();
            return int.TryParse(raw, out points) && points >= 0;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static bool HasCategoryHeader(string[] lines, Regex header)
    {
        return lines.Any(line => TryMatch(header, line, out _));
    }

    private static bool TryMatch(Regex regex, string line, out Match match)
    {
        try
        {
            match = regex.Match(line);
            return match.Success;
        }
        catch (RegexMatchTimeoutException)
        {
            match = Match.Empty;
            return false;
        }
    }

    private static string[] SplitLines(string text)
    {
        return text.Split('\n', StringSplitOptions.TrimEntries);
    }

    private static bool ContainsIgnoreCase(string text, string value)
    {
        return text.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static string DisplayName(string raw)
    {
        if (raw.Length == 0)
        {
            return raw;
        }

        return char.ToUpperInvariant(raw[0]) + raw[1..].ToLowerInvariant();
    }

    private static bool CostsSupply(string name)
    {
        return name.Equals("Special", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Rare", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Mercenaries", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Allies", StringComparison.OrdinalIgnoreCase);
    }
}
