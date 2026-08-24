using MapAndMuster.Domain.Chat;
using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Application.Identity;

/// <summary>
/// Private account state visible only to the owning user.
/// </summary>
public sealed class UserAccount
{
    /// <summary>
    /// Gets the account identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the unique email address.
    /// </summary>
    public required string Email { get; init; }

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
    public char? MiddleInitial { get; init; }

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
    /// Gets the optional state, province, or region.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Gets the country.
    /// </summary>
    public required string Country { get; init; }

    /// <summary>
    /// Gets the optional IANA time-zone identifier used to display UTC timestamps.
    /// </summary>
    public string? TimeZoneId { get; init; }

    /// <summary>
    /// Gets whether other users see the full name.
    /// </summary>
    public required DisplayNameMode DisplayNameMode { get; init; }

    /// <summary>
    /// Gets the stored avatar key, if any.
    /// </summary>
    public string? AvatarStorageKey { get; init; }

    /// <summary>
    /// Gets when the account was created, in UTC.
    /// </summary>
    public required DateTimeOffset CreatedUtc { get; init; }

    /// <summary>
    /// Gets when the profile was last edited, in UTC.
    /// </summary>
    public required DateTimeOffset UpdatedUtc { get; init; }

    /// <summary>
    /// Gets the optimistic concurrency revision for profile updates.
    /// </summary>
    public required int ProfileRevision { get; init; }

    /// <summary>
    /// Gets a value indicating whether the email address is confirmed.
    /// </summary>
    public required bool EmailConfirmed { get; init; }

    /// <summary>
    /// Gets whether unread notices appear on the home board.
    /// </summary>
    public bool InAppNotificationsEnabled { get; init; } = true;

    /// <summary>
    /// Gets whether notices are also queued for email.
    /// </summary>
    public bool EmailNotificationsEnabled { get; init; } = true;

    /// <summary>
    /// Gets the default site-chat compose language. English when unset.
    /// </summary>
    public string PreferredChatLanguage { get; init; } = nameof(ChatLanguage.English);

    /// <summary>
    /// Gets how UTC timestamps are formatted for this user.
    /// </summary>
    public DateTimeDisplayFormat DateTimeDisplayFormat { get; init; } = DateTimeDisplayFormats.Default;

    /// <summary>Gets whether this is a seeded administrator test account.</summary>
    public bool IsTestAccount { get; init; }

    /// <summary>Gets the test-account number when this is a seeded test user.</summary>
    public int? TestAccountNumber { get; init; }
}
