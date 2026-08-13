using Campaign.Domain.Identity;

namespace Campaign.Application.Identity;

/// <summary>
/// Input required to update the authenticated user's own profile.
/// </summary>
public sealed class UpdateProfileCommand
{
    /// <summary>
    /// Gets the account identifier of the authenticated user.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the unique username.
    /// </summary>
    public required string Username { get; init; }

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
    /// Gets the profile revision last observed by the client.
    /// </summary>
    public required int ProfileRevision { get; init; }
}

/// <summary>
/// Input required to replace the authenticated user's avatar.
/// </summary>
public sealed class UploadAvatarCommand
{
    /// <summary>
    /// Gets the account identifier of the authenticated user.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the uploaded image stream. The caller owns disposal.
    /// </summary>
    public required Stream Content { get; init; }

    /// <summary>
    /// Gets the declared content type.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the declared file length when known.
    /// </summary>
    public long? Length { get; init; }
}
