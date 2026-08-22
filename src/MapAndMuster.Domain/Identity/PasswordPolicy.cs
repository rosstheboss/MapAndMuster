using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Domain.Identity;

/// <summary>
/// Password complexity required for local accounts. The server remains authoritative.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>
    /// Minimum password length.
    /// </summary>
    public const int MinimumLength = 12;

    /// <summary>
    /// Validates a proposed password.
    /// </summary>
    /// <param name="password">The proposed password.</param>
    /// <param name="error">The validation error when the password is rejected.</param>
    /// <param name="field">The field name reported to clients.</param>
    /// <returns><see langword="true"/> when the password meets the policy.</returns>
    public static bool TryValidate(
        string? password,
        [NotNullWhen(false)] out DomainError? error,
        string field = "password")
    {
        if (string.IsNullOrEmpty(password))
        {
            error = new DomainError("password.invalid", "Password is not filled in.", field);
            return false;
        }

        var parts = new List<string>();
        if (password.Length < MinimumLength)
        {
            parts.Add($"Password is too short (minimum {MinimumLength} characters).");
        }

        var missingClasses = new List<string>();
        if (!password.Any(char.IsUpper))
        {
            missingClasses.Add("an uppercase letter");
        }

        if (!password.Any(char.IsLower))
        {
            missingClasses.Add("a lowercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            missingClasses.Add("a number");
        }

        if (password.All(char.IsLetterOrDigit))
        {
            missingClasses.Add("a special character");
        }

        if (missingClasses.Count > 0)
        {
            parts.Add("Password must contain " + JoinRequirements(missingClasses) + ".");
        }

        if (parts.Count > 0)
        {
            error = new DomainError("password.invalid", string.Join(" ", parts), field);
            return false;
        }

        error = null;
        return true;
    }

    private static string JoinRequirements(List<string> parts)
    {
        if (parts.Count == 1)
        {
            return parts[0];
        }

        if (parts.Count == 2)
        {
            return $"{parts[0]} and {parts[1]}";
        }

        return string.Join(", ", parts.Take(parts.Count - 1)) + ", and " + parts[^1];
    }
}
