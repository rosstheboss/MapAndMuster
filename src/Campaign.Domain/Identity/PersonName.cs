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
    /// Maximum length of a first or last name.
    /// </summary>
    public const int MaxNameLength = 50;

    private static readonly Regex NamePattern = new(
        @"^[\p{L}][\p{L}\s'.-]{0,49}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private PersonName(string firstName, char? middleInitial, string lastName)
    {
        FirstName = firstName;
        MiddleInitial = middleInitial;
        LastName = lastName;
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
        name = null;

        if (!TryValidateNamePart(firstName, "firstName.invalid", "First name", out var first, out error))
        {
            return false;
        }

        if (!TryValidateNamePart(lastName, "lastName.invalid", "Last name", out var last, out error))
        {
            return false;
        }

        char? initial = null;
        if (!string.IsNullOrWhiteSpace(middleInitial))
        {
            var trimmedInitial = middleInitial.Trim().TrimEnd('.');
            if (trimmedInitial.Length != 1 || !char.IsLetter(trimmedInitial[0]))
            {
                error = new DomainError("middleInitial.invalid", "Middle initial must be a single letter.");
                return false;
            }

            initial = char.ToUpperInvariant(trimmedInitial[0]);
        }

        name = new PersonName(first, initial, last);
        error = null;
        return true;
    }

    /// <summary>
    /// Formats the full name for display.
    /// </summary>
    /// <returns>The formatted full name.</returns>
    public string FormatFullName()
    {
        return MiddleInitial is { } initial
            ? $"{FirstName} {initial}. {LastName}"
            : $"{FirstName} {LastName}";
    }

    private static bool TryValidateNamePart(
        string? raw,
        string errorCode,
        string fieldLabel,
        [NotNullWhen(true)] out string? value,
        [NotNullWhen(false)] out DomainError? error)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = new DomainError(errorCode, $"{fieldLabel} is required.");
            return false;
        }

        var trimmed = CollapseWhitespace(raw.Trim());
        if (trimmed.Length > MaxNameLength || !NamePattern.IsMatch(trimmed))
        {
            error = new DomainError(
                errorCode,
                $"{fieldLabel} must be 1-{MaxNameLength} letters and may include spaces, apostrophes, periods, or hyphens.");
            return false;
        }

        value = trimmed;
        error = null;
        return true;
    }

    private static string CollapseWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }
}
