using System.ComponentModel.DataAnnotations;
using MapAndMuster.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace MapAndMuster.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user with campaign profile fields.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional middle initial.
    /// </summary>
    public string? MiddleInitial { get; set; }

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional name suffix.
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional state, province, or region.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional IANA time-zone identifier used to display UTC timestamps.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Gets or sets whether other users see the full name.
    /// </summary>
    public DisplayNameMode DisplayNameMode { get; set; }

    /// <summary>
    /// Gets or sets the generated avatar storage key.
    /// </summary>
    public string? AvatarStorageKey { get; set; }

    /// <summary>
    /// Gets or sets when the account was created, in UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets when the profile was last edited, in UTC.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the optimistic concurrency revision for profile updates.
    /// </summary>
    [ConcurrencyCheck]
    public int ProfileRevision { get; set; }

    /// <summary>
    /// Gets or sets whether unread notices appear on the home board.
    /// </summary>
    public bool InAppNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether notices are also queued for email.
    /// </summary>
    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default site-chat compose language.
    /// </summary>
    public string PreferredChatLanguage { get; set; } = "English";

    /// <summary>Gets or sets whether this account is a seeded administrator test user.</summary>
    public bool IsTestAccount { get; set; }

    /// <summary>Gets or sets the test-account number when <see cref="IsTestAccount"/> is true.</summary>
    public int? TestAccountNumber { get; set; }
}
