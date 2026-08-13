using Campaign.Domain.Identity;

namespace Campaign.Application.Identity;

/// <summary>
/// Public fields that any authenticated or anonymous caller may see for another user.
/// </summary>
public sealed class PublicProfile
{
    /// <summary>
    /// Gets the unique username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the name shown to other users: the username, or the full name when that preference is selected.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the display name is the user's full name.
    /// </summary>
    public required bool ShowsFullName { get; init; }

    /// <summary>
    /// Gets the city.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// Gets the optional state, province, or region.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Gets the country.
    /// </summary>
    public required string Country { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user has an avatar.
    /// </summary>
    public required bool HasAvatar { get; init; }
}

/// <summary>
/// Maps account records onto public and private profile shapes.
/// </summary>
public static class ProfileMapper
{
    /// <summary>
    /// Builds the public profile, omitting email, timestamps, and the legal name unless the owner opted in.
    /// </summary>
    /// <param name="account">The source account.</param>
    /// <returns>The public profile.</returns>
    public static PublicProfile ToPublic(UserAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        var showsFullName = account.DisplayNameMode == DisplayNameMode.FullName;
        var displayName = showsFullName
            ? FormatFullName(account.FirstName, account.MiddleInitial, account.LastName)
            : account.Username;

        return new PublicProfile
        {
            Username = account.Username,
            DisplayName = displayName,
            ShowsFullName = showsFullName,
            City = account.City,
            Region = account.Region,
            Country = account.Country,
            HasAvatar = !string.IsNullOrWhiteSpace(account.AvatarStorageKey),
        };
    }

    /// <summary>
    /// Formats a full name using the same rules as <c>PersonName</c>.
    /// </summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleInitial">The optional middle initial.</param>
    /// <param name="lastName">The last name.</param>
    /// <returns>The formatted full name.</returns>
    public static string FormatFullName(string firstName, char? middleInitial, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        return middleInitial is { } initial
            ? $"{firstName} {initial}. {lastName}"
            : $"{firstName} {lastName}";
    }
}
