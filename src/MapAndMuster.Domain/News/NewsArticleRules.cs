using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Common;
using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Domain.News;

/// <summary>
/// Validates administrator-authored news articles shown on the home page.
/// </summary>
public static class NewsArticleRules
{
    /// <summary>Minimum title length after trimming.</summary>
    public const int TitleMinLength = 3;

    /// <summary>Maximum title length after trimming.</summary>
    public const int TitleMaxLength = 120;

    /// <summary>Maximum markdown body length after trimming.</summary>
    public const int BodyMaxLength = 20_000;

    /// <summary>
    /// Validates title and markdown body.
    /// </summary>
    public static bool TryCreate(
        string? title,
        string? bodyMarkdown,
        [NotNullWhen(true)] out string? normalizedTitle,
        [NotNullWhen(true)] out string? normalizedBody,
        [NotNullWhen(false)] out DomainError? error)
    {
        normalizedTitle = null;
        normalizedBody = null;
        error = null;

        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length < TitleMinLength || trimmedTitle.Length > TitleMaxLength)
        {
            error = new DomainError(
                "news.title.length",
                $"News titles must be {TitleMinLength}-{TitleMaxLength} characters.",
                "title");
            return false;
        }

        if (ProhibitedLanguage.ContainsProhibitedTerm(trimmedTitle))
        {
            error = ProhibitedLanguage.ErrorFor("title", "News title");
            return false;
        }

        var trimmedBody = bodyMarkdown?.Trim() ?? string.Empty;
        if (trimmedBody.Length == 0)
        {
            error = new DomainError("news.body.required", "Enter the news article markdown.", "bodyMarkdown");
            return false;
        }

        if (trimmedBody.Length > BodyMaxLength)
        {
            error = new DomainError(
                "news.body.too_long",
                $"News articles are limited to {BodyMaxLength} characters.",
                "bodyMarkdown");
            return false;
        }

        if (ProhibitedLanguage.ContainsProhibitedTerm(trimmedBody))
        {
            error = ProhibitedLanguage.ErrorFor("bodyMarkdown", "News article");
            return false;
        }

        normalizedTitle = trimmedTitle;
        normalizedBody = trimmedBody;
        return true;
    }
}
