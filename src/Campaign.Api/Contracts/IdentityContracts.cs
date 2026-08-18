using System.Text.Json.Serialization;
using Campaign.Application.Identity;
using Campaign.Domain.Identity;

namespace Campaign.Api.Contracts;

/// <summary>
/// A field-scoped validation failure returned with an <see cref="ErrorResponse"/>.
/// </summary>
/// <param name="Field">The request field name.</param>
/// <param name="Code">The stable error code.</param>
/// <param name="Message">A safe explanation.</param>
public sealed record FieldErrorResponse(string Field, string Code, string Message);

/// <summary>
/// Machine-readable API error payload.
/// </summary>
/// <param name="Code">The stable error code.</param>
/// <param name="Message">A safe explanation listing every failed field.</param>
/// <param name="Errors">Optional field-scoped errors used to highlight inputs.</param>
public sealed record ErrorResponse(string Code, string Message, IReadOnlyList<FieldErrorResponse>? Errors = null);

/// <summary>
/// Local account registration request.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>Gets the email address.</summary>
    public required string Email { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the password.</summary>
    public required string Password { get; init; }

    /// <summary>Gets the first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>Gets the optional middle initial.</summary>
    public string? MiddleInitial { get; init; }

    /// <summary>Gets the last name.</summary>
    public required string LastName { get; init; }

    /// <summary>Gets the optional name suffix.</summary>
    public string? Suffix { get; init; }

    /// <summary>Gets the city.</summary>
    public required string City { get; init; }

    /// <summary>Gets the state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the country.</summary>
    public required string Country { get; init; }

    /// <summary>Gets the IANA time-zone identifier used to display UTC timestamps.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets the public display-name preference.</summary>
    public required string DisplayNameMode { get; init; }
}

/// <summary>
/// Successful registration acknowledgement. The user must confirm email before signing in.
/// </summary>
/// <param name="UserId">The new account identifier.</param>
/// <param name="Username">The registered username.</param>
public sealed record RegisterResponse(Guid UserId, string Username);

/// <summary>
/// Email-and-password sign-in request.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>Gets the email address.</summary>
    public required string Email { get; init; }

    /// <summary>Gets the password.</summary>
    public required string Password { get; init; }
}

/// <summary>
/// Email confirmation request.
/// </summary>
public sealed class ConfirmEmailRequest
{
    /// <summary>Gets the account identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the confirmation token.</summary>
    public required string Token { get; init; }
}

/// <summary>
/// Password reset request. The response does not reveal whether the email exists.
/// </summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>Gets the email address.</summary>
    public required string Email { get; init; }
}

/// <summary>
/// Password reset completion request.
/// </summary>
public sealed class ResetPasswordRequest
{
    /// <summary>Gets the account identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the reset token.</summary>
    public required string Token { get; init; }

    /// <summary>Gets the new password.</summary>
    public required string Password { get; init; }
}

/// <summary>
/// Authenticated password change request.
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>Gets the current password.</summary>
    public required string CurrentPassword { get; init; }

    /// <summary>Gets the new password.</summary>
    public required string NewPassword { get; init; }
}

/// <summary>
/// Resend confirmation request. The response does not reveal whether the email exists.
/// </summary>
public sealed class ResendConfirmationRequest
{
    /// <summary>Gets the email address.</summary>
    public required string Email { get; init; }
}

/// <summary>
/// An available external login provider.
/// </summary>
/// <param name="Name">The provider scheme name.</param>
/// <param name="DisplayName">The label shown to users.</param>
public sealed record ExternalProviderResponse(string Name, string DisplayName);

/// <summary>
/// Completes registration after an external-provider challenge.
/// </summary>
public sealed class CompleteExternalRegistrationRequest
{
    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>Gets the optional middle initial.</summary>
    public string? MiddleInitial { get; init; }

    /// <summary>Gets the last name.</summary>
    public required string LastName { get; init; }

    /// <summary>Gets the optional name suffix.</summary>
    public string? Suffix { get; init; }

    /// <summary>Gets the city.</summary>
    public required string City { get; init; }

    /// <summary>Gets the state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the country.</summary>
    public required string Country { get; init; }

    /// <summary>Gets the IANA time-zone identifier used to display UTC timestamps.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets the public display-name preference.</summary>
    public required string DisplayNameMode { get; init; }
}

/// <summary>
/// Private profile returned to the owning user.
/// </summary>
public sealed class OwnProfileResponse
{
    /// <summary>Gets the account identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the email address.</summary>
    public required string Email { get; init; }

    /// <summary>Gets the username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>Gets the optional middle initial.</summary>
    public string? MiddleInitial { get; init; }

    /// <summary>Gets the last name.</summary>
    public required string LastName { get; init; }

    /// <summary>Gets the optional name suffix.</summary>
    public string? Suffix { get; init; }

    /// <summary>Gets the city.</summary>
    public required string City { get; init; }

    /// <summary>Gets the state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the country.</summary>
    public required string Country { get; init; }

    /// <summary>Gets the display-name preference.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required DisplayNameMode DisplayNameMode { get; init; }

    /// <summary>Gets the optional IANA time-zone identifier used to display UTC timestamps.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets a value indicating whether an avatar is stored.</summary>
    public required bool HasAvatar { get; init; }

    /// <summary>Gets when the account was created, in UTC.</summary>
    public required DateTimeOffset CreatedUtc { get; init; }

    /// <summary>Gets when the profile was last edited, in UTC.</summary>
    public required DateTimeOffset UpdatedUtc { get; init; }

    /// <summary>Gets the profile concurrency revision.</summary>
    public required int ProfileRevision { get; init; }

    /// <summary>Gets a value indicating whether the email is confirmed.</summary>
    public required bool EmailConfirmed { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public bool IsAdministrator { get; init; }

    /// <summary>Gets whether unread notices appear on the home board.</summary>
    public bool InAppNotificationsEnabled { get; init; } = true;

    /// <summary>Gets whether notices are also queued for email.</summary>
    public bool EmailNotificationsEnabled { get; init; } = true;

    /// <summary>Gets the default site-chat compose language.</summary>
    public string PreferredChatLanguage { get; init; } = "English";

    /// <summary>Gets whether this is a seeded administrator test account.</summary>
    public bool IsTestAccount { get; init; }

    /// <summary>Gets the test-account number when this is a seeded test user.</summary>
    public int? TestAccountNumber { get; init; }

    /// <summary>Gets whether the caller is signed in as a test account on behalf of an administrator.</summary>
    public bool IsImpersonating { get; init; }
}

/// <summary>
/// Public profile returned to other users.
/// </summary>
public sealed class PublicProfileResponse
{
    /// <summary>Gets the username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets whether the display name is the full name.</summary>
    public required bool ShowsFullName { get; init; }

    /// <summary>Gets the city.</summary>
    public required string City { get; init; }

    /// <summary>Gets the state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the country.</summary>
    public required string Country { get; init; }

    /// <summary>Gets whether an avatar is stored.</summary>
    public required bool HasAvatar { get; init; }

    /// <summary>
    /// Gets campaigns the viewer may see for this player. Scores and rankings are omitted until implemented.
    /// </summary>
    public IReadOnlyList<PublicProfileCampaignResponse> Campaigns { get; init; } = [];
}

/// <summary>
/// A campaign listed on a public profile.
/// </summary>
public sealed class PublicProfileCampaignResponse
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
/// Profile update request for the owning user.
/// </summary>
public sealed class UpdateProfileRequest
{
    /// <summary>Gets the username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>Gets the optional middle initial.</summary>
    public string? MiddleInitial { get; init; }

    /// <summary>Gets the last name.</summary>
    public required string LastName { get; init; }

    /// <summary>Gets the optional name suffix.</summary>
    public string? Suffix { get; init; }

    /// <summary>Gets the city.</summary>
    public required string City { get; init; }

    /// <summary>Gets the state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the country.</summary>
    public required string Country { get; init; }

    /// <summary>Gets the display-name preference.</summary>
    public required string DisplayNameMode { get; init; }

    /// <summary>Gets the IANA time-zone identifier used to display UTC timestamps.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets the last observed profile revision.</summary>
    public required int ProfileRevision { get; init; }

    /// <summary>Gets whether unread notices appear on the home board.</summary>
    public bool InAppNotificationsEnabled { get; init; } = true;

    /// <summary>Gets whether notices are also queued for email.</summary>
    public bool EmailNotificationsEnabled { get; init; } = true;

    /// <summary>Gets the default site-chat compose language.</summary>
    public string PreferredChatLanguage { get; init; } = "English";
}

/// <summary>
/// Maps application models onto HTTP contracts.
/// </summary>
public static class ProfileResponses
{
    /// <summary>
    /// Maps a private account to the owner response.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <param name="isImpersonating">Whether the caller is impersonating a test account.</param>
    /// <returns>The owner response.</returns>
    public static OwnProfileResponse FromAccount(
        UserAccount account,
        bool isAdministrator = false,
        bool isImpersonating = false)
    {
        ArgumentNullException.ThrowIfNull(account);
        return new OwnProfileResponse
        {
            Id = account.Id,
            Email = account.Email,
            Username = account.Username,
            FirstName = account.FirstName,
            MiddleInitial = account.MiddleInitial?.ToString(),
            LastName = account.LastName,
            Suffix = account.Suffix,
            City = account.City,
            Region = account.Region,
            Country = account.Country,
            DisplayNameMode = account.DisplayNameMode,
            TimeZoneId = account.TimeZoneId,
            HasAvatar = !string.IsNullOrWhiteSpace(account.AvatarStorageKey),
            CreatedUtc = account.CreatedUtc,
            UpdatedUtc = account.UpdatedUtc,
            ProfileRevision = account.ProfileRevision,
            EmailConfirmed = account.EmailConfirmed,
            IsAdministrator = isAdministrator,
            InAppNotificationsEnabled = account.InAppNotificationsEnabled,
            EmailNotificationsEnabled = account.EmailNotificationsEnabled,
            PreferredChatLanguage = account.PreferredChatLanguage,
            IsTestAccount = account.IsTestAccount,
            TestAccountNumber = account.TestAccountNumber,
            IsImpersonating = isImpersonating,
        };
    }

    /// <summary>
    /// Maps a public profile to the public response.
    /// </summary>
    /// <param name="profile">The public profile.</param>
    /// <returns>The public response.</returns>
    public static PublicProfileResponse FromPublic(PublicProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new PublicProfileResponse
        {
            Username = profile.Username,
            DisplayName = profile.DisplayName,
            ShowsFullName = profile.ShowsFullName,
            City = profile.City,
            Region = profile.Region,
            Country = profile.Country,
            HasAvatar = profile.HasAvatar,
            Campaigns =
            [
                .. profile.Campaigns.Select(static campaign => new PublicProfileCampaignResponse
                {
                    Id = campaign.Id,
                    Name = campaign.Name,
                    Status = campaign.Status,
                    IsPrivate = campaign.IsPrivate,
                }),
            ],
        };
    }
}

/// <summary>
/// A seeded administrator test account.
/// </summary>
public sealed class TestAccountResponse
{
    /// <summary>Gets the account identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the test number from 1 to 30.</summary>
    public required int Number { get; init; }

    /// <summary>Gets the username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the public display name.</summary>
    public required string DisplayName { get; init; }
}
