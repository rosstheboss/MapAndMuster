using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Common;
using Campaign.Domain.Identity;

namespace Campaign.Domain.Play;

/// <summary>
/// Normalizes optional battle-report army-list text and builder metadata.
/// </summary>
public static class ArmyListRules
{
    /// <summary>Maximum length of pasted army-list text after trimming.</summary>
    public const int TextMaxLength = 20_000;

    /// <summary>Maximum composition categories stored with a report.</summary>
    public const int CategoryMaxCount = 20;

    /// <summary>Maximum length of a category name.</summary>
    public const int CategoryNameMaxLength = 40;

    /// <summary>Shown when New Recruit or Old World Builder text cannot be read.</summary>
    public const string ParseFailedMessage =
        "The list could not be parsed. Enter the supply points manually.";

    /// <summary>
    /// Trims optional army-list text and rejects oversized or prohibited content.
    /// </summary>
    public static bool TryNormalizeText(
        string? text,
        [NotNullWhen(false)] out DomainError? error,
        out string? normalized)
    {
        error = null;
        normalized = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var trimmed = text.Trim();
        if (trimmed.Length > TextMaxLength)
        {
            error = new DomainError(
                "armyListText.length",
                $"Army list text must be at most {TextMaxLength} characters.",
                "armyListText");
            return false;
        }

        if (ProhibitedLanguage.ContainsProhibitedTerm(trimmed))
        {
            error = ProhibitedLanguage.ErrorFor("armyListText", "Army list");
            return false;
        }

        normalized = trimmed;
        return true;
    }

    /// <summary>
    /// Reads a builder choice from stored or submitted text. Unknown values become Other.
    /// </summary>
    public static ArmyListBuilder ParseBuilder(string? raw)
    {
        if (string.Equals(raw, nameof(ArmyListBuilder.NewRecruit), StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "New Recruit", StringComparison.OrdinalIgnoreCase))
        {
            return ArmyListBuilder.NewRecruit;
        }

        if (string.Equals(raw, nameof(ArmyListBuilder.OldWorldBuilder), StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "Old World Builder", StringComparison.OrdinalIgnoreCase))
        {
            return ArmyListBuilder.OldWorldBuilder;
        }

        return ArmyListBuilder.Other;
    }

    /// <summary>
    /// Returns a stored game-system identifier, or null when omitted.
    /// </summary>
    public static string? NormalizeGameSystem(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (string.Equals(trimmed, ArmyListGameSystems.WarhammerTheOldWorld, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, ArmyListGameSystems.WarhammerTheOldWorldDisplayName, StringComparison.OrdinalIgnoreCase))
        {
            return ArmyListGameSystems.WarhammerTheOldWorld;
        }

        return trimmed.Length <= 80 ? trimmed : trimmed[..80];
    }

    /// <summary>
    /// Validates optional category rows submitted with a battle report.
    /// </summary>
    public static bool TryNormalizeCategories(
        IReadOnlyList<ArmyListSupplyCategory>? categories,
        [NotNullWhen(false)] out DomainError? error,
        out IReadOnlyList<ArmyListSupplyCategory> normalized)
    {
        error = null;
        if (categories is null || categories.Count == 0)
        {
            normalized = [];
            return true;
        }

        if (categories.Count > CategoryMaxCount)
        {
            error = new DomainError(
                "armyListCategories.count",
                $"A battle report may list at most {CategoryMaxCount} army-list categories.",
                "supplyCategories");
            normalized = [];
            return false;
        }

        var next = new List<ArmyListSupplyCategory>(categories.Count);
        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category.Name) || category.Name.Trim().Length > CategoryNameMaxLength)
            {
                error = new DomainError(
                    "armyListCategories.name",
                    $"Category names must be 1 to {CategoryNameMaxLength} characters.",
                    "supplyCategories");
                normalized = [];
                return false;
            }

            if (category.UnitCount < 0 || category.SupplyPoints < 0)
            {
                error = new DomainError(
                    "armyListCategories.value",
                    "Category unit counts and supply points cannot be negative.",
                    "supplyCategories");
                normalized = [];
                return false;
            }

            next.Add(
                new ArmyListSupplyCategory(
                    category.Name.Trim(),
                    category.UnitCount,
                    category.SupplyPoints,
                    category.CostsSupply));
        }

        normalized = next;
        return true;
    }
}
