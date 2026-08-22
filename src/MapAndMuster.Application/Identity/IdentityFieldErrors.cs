using MapAndMuster.Domain.Common;

namespace MapAndMuster.Application.Identity;

/// <summary>
/// Shared identity field checks used by registration and profile use cases.
/// </summary>
internal static class IdentityFieldErrors
{
    /// <summary>
    /// Validates an email address shape. Uniqueness is checked separately.
    /// </summary>
    /// <param name="raw">The email.</param>
    /// <returns>The error when invalid; otherwise <see langword="null"/>.</returns>
    public static DomainError? Email(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new DomainError("email.invalid", "Email is not filled in.", "email");
        }

        var email = raw.Trim();
        if (!email.Contains('@', StringComparison.Ordinal))
        {
            return new DomainError("email.invalid", "Email address is invalid.", "email");
        }

        return null;
    }
}
