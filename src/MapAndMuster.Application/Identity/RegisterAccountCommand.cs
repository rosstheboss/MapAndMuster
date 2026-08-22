using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Application.Identity;

/// <summary>
/// Input required to register a local email-and-password account.
/// </summary>
public sealed class RegisterAccountCommand
{
    /// <summary>
    /// Gets the email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the unique username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the password.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Gets the first name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Gets the optional middle initial.
    /// </summary>
    public string? MiddleInitial { get; init; }

    /// <summary>
    /// Gets the last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Gets the optional name suffix.
    /// </summary>
    public string? Suffix { get; init; }

    /// <summary>
    /// Gets the city.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// Gets the state, province, or region.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Gets the country.
    /// </summary>
    public required string Country { get; init; }

    /// <summary>
    /// Gets the IANA time-zone identifier used to display UTC timestamps.
    /// </summary>
    public string? TimeZoneId { get; init; }

    /// <summary>
    /// Gets the public display-name preference.
    /// </summary>
    public required DisplayNameMode DisplayNameMode { get; init; }

    /// <summary>
    /// Gets the optional avatar upload stream. The caller owns disposal.
    /// </summary>
    public Stream? AvatarContent { get; init; }

    /// <summary>
    /// Gets the avatar content type when <see cref="AvatarContent"/> is provided.
    /// </summary>
    public string? AvatarContentType { get; init; }

    /// <summary>
    /// Gets the avatar file length in bytes when known.
    /// </summary>
    public long? AvatarLength { get; init; }
}

/// <summary>
/// Result of a successful local registration. The user is not signed in until email is confirmed.
/// </summary>
public sealed class RegisterAccountResult
{
    /// <summary>
    /// Gets the new account identifier.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the registered username.
    /// </summary>
    public required string Username { get; init; }
}
