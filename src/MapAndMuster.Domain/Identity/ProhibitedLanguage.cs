using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace MapAndMuster.Domain.Identity;

/// <summary>
/// Detects English profanity, racial slurs, and similar abusive terms in usernames and legal names.
/// Matching uses whole-word tokens plus a smaller embedded-term list to limit false positives.
/// </summary>
public static class ProhibitedLanguage
{
    private static readonly FrozenSet<string> TokenTerms = CreateTokenTerms();

    private static readonly FrozenSet<string> EmbeddedTerms = FrozenSet.ToFrozenSet(
        [
            "asshole",
            "blowjob",
            "faggot",
            "fuck",
            "handjob",
            "motherfuck",
            "nigger",
            "nigga",
            "retard",
            "shit",
            "cunt",
            "kike",
            "chink",
            "wetback",
            "beaner",
            "spastic",
            "tranny",
            "raghead",
            "towelhead",
            "darkie",
        ],
        StringComparer.Ordinal);

    private static readonly Regex LetterRuns = new(
        @"[\p{L}]+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Returns whether the text contains a prohibited English term.
    /// </summary>
    /// <param name="raw">The user-supplied text.</param>
    /// <returns><see langword="true"/> when a prohibited term is present.</returns>
    public static bool ContainsProhibitedTerm(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        MatchCollection runs;
        try
        {
            runs = LetterRuns.Matches(raw);
        }
        catch (RegexMatchTimeoutException)
        {
            return true;
        }

        foreach (Match match in runs)
        {
            if (TokenTerms.Contains(match.Value.ToLowerInvariant()))
            {
                return true;
            }
        }

        var compact = string.Concat(runs.Select(static match => match.Value)).ToLowerInvariant();
        foreach (var term in EmbeddedTerms)
        {
            if (compact.Contains(term, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds the field-scoped error used when a name or username contains prohibited language.
    /// </summary>
    /// <param name="field">The request field.</param>
    /// <param name="fieldLabel">The user-facing field label.</param>
    /// <returns>The domain error.</returns>
    public static Common.DomainError ErrorFor(string field, string fieldLabel)
    {
        return new Common.DomainError(
            $"{field}.prohibited",
            $"{fieldLabel} contains prohibited language.",
            field);
    }

    private static FrozenSet<string> CreateTokenTerms()
    {
        return FrozenSet.ToFrozenSet(
            [
                "anal",
                "anus",
                "arse",
                "ass",
                "asshole",
                "bastard",
                "beaner",
                "bitch",
                "blowjob",
                "bollock",
                "bollocks",
                "bugger",
                "chink",
                "cock",
                "coon",
                "crap",
                "cunt",
                "damn",
                "darkie",
                "dick",
                "dildo",
                "dyke",
                "fag",
                "faggot",
                "fuck",
                "fucker",
                "fucking",
                "goddamn",
                "gook",
                "handjob",
                "hell",
                "homo",
                "jizz",
                "kike",
                "motherfucker",
                "nigger",
                "nigga",
                "paki",
                "piss",
                "prick",
                "pussy",
                "raghead",
                "retard",
                "shit",
                "slut",
                "spastic",
                "spic",
                "twat",
                "towelhead",
                "tranny",
                "wank",
                "wanker",
                "wetback",
                "whore",
            ],
            StringComparer.Ordinal);
    }
}
