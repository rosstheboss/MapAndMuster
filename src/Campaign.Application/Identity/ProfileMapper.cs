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

    /// <summary>
    /// Gets campaigns the viewer may see for this player: publicly viewable campaigns, plus
    /// private campaigns the viewer shares. Scores and rankings are not included yet.
    /// </summary>
    public IReadOnlyList<PublicProfileCampaign> Campaigns { get; init; } = [];
}

/// <summary>
/// A campaign listed on a public profile. Secrets and join passwords are omitted.
/// </summary>
public sealed class PublicProfileCampaign
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the lifecycle status name.</summary>
    public required string Status { get; init; }

    /// <summary>Gets whether the campaign requires a join password.</summary>
    public required bool IsPrivate { get; init; }
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
    /// <param name="campaigns">Campaigns the viewer may see for this player.</param>
    /// <returns>The public profile.</returns>
    public static PublicProfile ToPublic(
        UserAccount account,
        IReadOnlyList<PublicProfileCampaign>? campaigns = null)
    {
        ArgumentNullException.ThrowIfNull(account);

        var showsFullName = account.DisplayNameMode == DisplayNameMode.FullName;
        var displayName = showsFullName
            ? FormatFullName(account.FirstName, account.MiddleInitial, account.LastName, account.Suffix)
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
            Campaigns = campaigns ?? [],
        };
    }

    /// <summary>
    /// Formats a full name using the same rules as <c>PersonName</c>.
    /// </summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleInitial">The optional middle initial.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="suffix">The optional name suffix.</param>
    /// <returns>The formatted full name.</returns>
    public static string FormatFullName(string firstName, char? middleInitial, string lastName, string? suffix = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var core = middleInitial is { } initial
            ? $"{firstName} {initial}. {lastName}"
            : $"{firstName} {lastName}";

        return string.IsNullOrWhiteSpace(suffix) ? core : $"{core} {suffix}";
    }
}
