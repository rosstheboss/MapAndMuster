using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Campaign.Domain.Common;

namespace Campaign.Domain.Identity;

/// <summary>
/// A person's legal-style name used on their private profile. Other users see this only when
/// <see cref="DisplayNameMode.FullName"/> is selected.
/// </summary>
public sealed class PersonName
{
    /// <summary>
    /// Minimum length of a first or last name.
    /// </summary>
    public const int MinNameLength = 2;

    /// <summary>
    /// Maximum length of a first or last name.
    /// </summary>
    public const int MaxNameLength = 50;

    private static readonly Regex NamePattern = new(
        @"^[\p{L}][\p{L}\s'.-]{1,49}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private PersonName(string firstName, char? middleInitial, string lastName, string? suffix)
    {
        FirstName = firstName;
        MiddleInitial = middleInitial;
        LastName = lastName;
        Suffix = suffix;
    }

    /// <summary>
    /// Gets the first name.
    /// </summary>
    public string FirstName { get; }

    /// <summary>
    /// Gets the optional middle initial.
    /// </summary>
    public char? MiddleInitial { get; }

    /// <summary>
    /// Gets the last name.
    /// </summary>
    public string LastName { get; }

    /// <summary>
    /// Gets the optional name suffix.
    /// </summary>
    public string? Suffix { get; }

    /// <summary>
    /// Attempts to create a person name from user input.
    /// </summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleInitial">The optional middle initial.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="name">The created name when validation succeeds.</param>
    /// <param name="error">The validation error when creation fails.</param>
    /// <returns><see langword="true"/> when the name is valid.</returns>
    public static bool TryCreate(
        string? firstName,
        string? middleInitial,
        string? lastName,
        [NotNullWhen(true)] out PersonName? name,
        [NotNullWhen(false)] out DomainError? error)
    {
        return TryCreate(firstName, middleInitial, lastName, suffix: null, out name, out error);
    }

    /// <summary>
    /// Attempts to create a person name from user input, including an optional suffix.
    /// </summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleInitial">The optional middle initial.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="suffix">The optional name suffix.</param>
    /// <param name="name">The created name when validation succeeds.</param>
    /// <param name="error">The first validation error when creation fails.</param>
    /// <returns><see langword="true"/> when the name is valid.</returns>
    public static bool TryCreate(
        string? firstName,
        string? middleInitial,
        string? lastName,
        string? suffix,
        [NotNullWhen(true)] out PersonName? name,
        [NotNullWhen(false)] out DomainError? error)
    {
        var errors = CollectErrors(firstName, middleInitial, lastName, suffix);
        if (errors.Count > 0)
        {
            name = null;
            error = errors[0];
            return false;
        }

        _ = TryParseMiddleInitial(middleInitial, out var initial, out _);
        _ = NameSuffix.TryCreateOptional(suffix, out var parsedSuffix, out _);
        name = new PersonName(
            CollapseWhitespace(firstName!.Trim()),
            initial,
            CollapseWhitespace(lastName!.Trim()),
            parsedSuffix);
        error = null;
        return true;
    }

    /// <summary>
    /// Validates every name field and returns all failures.
    /// </summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleInitial">The optional middle initial.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="suffix">The optional name suffix.</param>
    /// <returns>The field errors, or an empty list.</returns>
    public static IReadOnlyList<DomainError> CollectErrors(
        string? firstName,
        string? middleInitial,
        string? lastName,
        string? suffix)
    {
        var errors = new List<DomainError>(4);
        if (!TryValidateNamePart(firstName, "firstName", "First name", out _, out var firstError))
        {
            errors.Add(firstError);
        }

        if (!TryParseMiddleInitial(middleInitial, out _, out var middleError) && middleError is not null)
        {
            errors.Add(middleError);
        }

        if (!TryValidateNamePart(lastName, "lastName", "Last name", out _, out var lastError))
        {
            errors.Add(lastError);
        }

        if (!NameSuffix.TryCreateOptional(suffix, out _, out var suffixError) && suffixError is not null)
        {
            errors.Add(suffixError);
        }

        return errors;
    }

    /// <summary>
    /// Formats the full name for display.
    /// </summary>
    /// <returns>The formatted full name.</returns>
    public string FormatFullName()
    {
        var core = MiddleInitial is { } initial
            ? $"{FirstName} {initial}. {LastName}"
            : $"{FirstName} {LastName}";

        return string.IsNullOrWhiteSpace(Suffix) ? core : $"{core} {Suffix}";
    }

    private static bool TryValidateNamePart(
        string? raw,
        string field,
        string fieldLabel,
        [NotNullWhen(true)] out string? value,
        [NotNullWhen(false)] out DomainError? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = new DomainError($"{field}.invalid", $"{fieldLabel} is not filled in.", field);
            return false;
        }

        var trimmed = CollapseWhitespace(raw.Trim());
        if (trimmed.Length < MinNameLength)
        {
            error = new DomainError(
                $"{field}.invalid",
                $"{fieldLabel} is too short (minimum {MinNameLength} characters).",
                field);
            return false;
        }

        if (trimmed.Length > MaxNameLength)
        {
            error = new DomainError(
                $"{field}.invalid",
                $"{fieldLabel} is too long (maximum {MaxNameLength} characters).",
                field);
            return false;
        }

        if (!NamePattern.IsMatch(trimmed))
        {
            error = new DomainError(
                $"{field}.invalid",
                $"{fieldLabel} may include letters, spaces, apostrophes, periods, or hyphens.",
                field);
            return false;
        }

        if (ProhibitedLanguage.ContainsProhibitedTerm(trimmed))
        {
            error = ProhibitedLanguage.ErrorFor(field, fieldLabel);
            return false;
        }

        value = trimmed;
        error = null;
        return true;
    }

    private static bool TryParseMiddleInitial(
        string? middleInitial,
        out char? initial,
        out DomainError? error)
    {
        initial = null;
        error = null;
        if (string.IsNullOrWhiteSpace(middleInitial))
        {
            return true;
        }

        var trimmedInitial = middleInitial.Trim().TrimEnd('.');
        if (trimmedInitial.Length != 1 || !char.IsLetter(trimmedInitial[0]))
        {
            error = new DomainError(
                "middleInitial.invalid",
                "Middle initial must be a single alphabetical character.",
                "middleInitial");
            return false;
        }

        initial = char.ToUpperInvariant(trimmedInitial[0]);
        return true;
    }

    private static string CollapseWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }
}
