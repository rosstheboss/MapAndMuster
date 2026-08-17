using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Campaign.Domain.Common;

namespace Campaign.Domain.Identity;

/// <summary>
/// A unique public handle for an account. Comparison is case-insensitive; the original casing is preserved.
/// </summary>
public sealed class Username : IEquatable<Username>
{
    /// <summary>
    /// Minimum length of a username.
    /// </summary>
    public const int MinLength = 3;

    /// <summary>
    /// Maximum length of a username.
    /// </summary>
    public const int MaxLength = 32;

    private static readonly Regex Pattern = new(
        "^[A-Za-z][A-Za-z0-9_]{2,31}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private Username(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the username with the casing supplied by the user.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Attempts to create a username from user input.
    /// </summary>
    /// <param name="raw">The raw username.</param>
    /// <param name="username">The created username when validation succeeds.</param>
    /// <param name="error">The validation error when creation fails.</param>
    /// <returns><see langword="true"/> when the username is valid.</returns>
    public static bool TryCreate(
        string? raw,
        [NotNullWhen(true)] out Username? username,
        [NotNullWhen(false)] out DomainError? error)
    {
        username = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = new DomainError("username.invalid", "Username is not filled in.", "username");
            return false;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length < MinLength)
        {
            error = new DomainError(
                "username.invalid",
                $"Username is too short (minimum {MinLength} characters).",
                "username");
            return false;
        }

        if (trimmed.Length > MaxLength)
        {
            error = new DomainError(
                "username.invalid",
                $"Username is too long (maximum {MaxLength} characters).",
                "username");
            return false;
        }

        if (!Pattern.IsMatch(trimmed))
        {
            error = new DomainError(
                "username.invalid",
                "Username must start with a letter and contain only letters, digits, or underscores.",
                "username");
            return false;
        }

        if (ReservedUsernames.Contains(trimmed))
        {
            error = ReservedUsernames.Error();
            return false;
        }

        if (ProhibitedLanguage.ContainsProhibitedTerm(trimmed))
        {
            error = ProhibitedLanguage.ErrorFor("username", "Username");
            return false;
        }

        username = new Username(trimmed);
        error = null;
        return true;
    }

    /// <inheritdoc />
    public bool Equals(Username? other)
    {
        return other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as Username);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }
}
