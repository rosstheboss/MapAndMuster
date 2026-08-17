using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Common;

namespace Campaign.Domain.Chat;

/// <summary>
/// Language flag for a site-chat message. This is a filter label, not a translation.
/// </summary>
public enum ChatLanguage
{
    /// <summary>English. The default compose language.</summary>
    English = 0,

    /// <summary>Spanish.</summary>
    Spanish = 1,

    /// <summary>French.</summary>
    French = 2,

    /// <summary>German.</summary>
    German = 3,

    /// <summary>Dutch.</summary>
    Dutch = 4,

    /// <summary>Italian.</summary>
    Italian = 5,

    /// <summary>Russian.</summary>
    Russian = 6,

    /// <summary>Korean.</summary>
    Korean = 7,

    /// <summary>Chinese.</summary>
    Chinese = 8,

    /// <summary>Japanese.</summary>
    Japanese = 9,

    /// <summary>Danish.</summary>
    Danish = 10,

    /// <summary>Swedish.</summary>
    Swedish = 11,

    /// <summary>Norwegian.</summary>
    Norwegian = 12,

    /// <summary>Finnish.</summary>
    Finnish = 13,

    /// <summary>Hindi.</summary>
    Hindi = 14,

    /// <summary>Arabic.</summary>
    Arabic = 15,
}

/// <summary>
/// Parses and lists supported site-chat language flags.
/// </summary>
public static class ChatLanguages
{
    /// <summary>The compose and profile default.</summary>
    public const ChatLanguage Default = ChatLanguage.English;

    /// <summary>
    /// Every supported language flag, in display order.
    /// </summary>
    public static IReadOnlyList<ChatLanguage> All { get; } =
    [
        ChatLanguage.English,
        ChatLanguage.Spanish,
        ChatLanguage.French,
        ChatLanguage.German,
        ChatLanguage.Dutch,
        ChatLanguage.Italian,
        ChatLanguage.Russian,
        ChatLanguage.Korean,
        ChatLanguage.Chinese,
        ChatLanguage.Japanese,
        ChatLanguage.Danish,
        ChatLanguage.Swedish,
        ChatLanguage.Norwegian,
        ChatLanguage.Finnish,
        ChatLanguage.Hindi,
        ChatLanguage.Arabic,
    ];

    /// <summary>
    /// Parses a language flag. Blank values become English.
    /// </summary>
    public static bool TryParse(string? value, [NotNullWhen(false)] out DomainError? error, out ChatLanguage language)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            language = Default;
            return true;
        }

        if (Enum.TryParse(value.Trim(), ignoreCase: true, out language) && Enum.IsDefined(language))
        {
            return true;
        }

        language = Default;
        error = new DomainError(
            "sitechat.language.invalid",
            "Choose a supported chat language.",
            "language");
        return false;
    }
}
