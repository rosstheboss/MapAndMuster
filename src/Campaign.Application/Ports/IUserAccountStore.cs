using Campaign.Application.Identity;
using Campaign.Domain.Identity;

namespace Campaign.Application.Ports;

/// <summary>
/// Persistence for local and externally linked user accounts.
/// </summary>
public interface IUserAccountStore
{
    /// <summary>
    /// Returns whether the email is already registered.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the email exists.</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether the username is already registered.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="userIdToIgnore">An optional user to exclude, used when renaming.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the username exists.</returns>
    Task<bool> UsernameExistsAsync(string username, Guid? userIdToIgnore, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a local password account and returns the confirmation token.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created account and email confirmation token, or a failure.</returns>
    Task<CreateLocalAccountOutcome> CreateLocalAccountAsync(
        CreateLocalAccountRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates an account from a completed external-login registration.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created account, or a failure.</returns>
    Task<CreateLocalAccountOutcome> CreateExternalAccountAsync(
        CreateExternalAccountRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds an account by identifier.
    /// </summary>
    /// <param name="userId">The account identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account, or <see langword="null"/>.</returns>
    Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds an account by username.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account, or <see langword="null"/>.</returns>
    Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    /// <summary>
    /// Updates mutable profile fields when the revision matches.
    /// </summary>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated account, or a failure.</returns>
    Task<UpdateProfileOutcome> UpdateProfileAsync(UpdateStoredProfileRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the password for a local account after verifying the current password.
    /// </summary>
    /// <param name="userId">The account identifier.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The proposed password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The outcome.</returns>
    Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the stored avatar key for an account.
    /// </summary>
    /// <param name="userId">The account identifier.</param>
    /// <param name="avatarStorageKey">The new storage key, or <see langword="null"/> to clear.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The previous storage key, if any.</returns>
    Task<string?> ReplaceAvatarKeyAsync(Guid userId, string? avatarStorageKey, CancellationToken cancellationToken);

    /// <summary>
    /// Returns which of the supplied users currently have the system Administrator role.
    /// </summary>
    /// <param name="userIds">The user identifiers to inspect.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The administrator identifiers that appear in <paramref name="userIds"/>.</returns>
    Task<IReadOnlySet<Guid>> FindAdministratorIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        return Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
    }

    /// <summary>
    /// Returns public mention tokens for every account. Email and other private fields are omitted.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Mention identities for every account.</returns>
    Task<IReadOnlyList<MentionableAccount>> ListMentionableAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<MentionableAccount>>([]);
    }

    /// <summary>
    /// Returns every account. Used to notify all users of an administrator site-chat announcement.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Every stored account.</returns>
    Task<IReadOnlyList<UserAccount>> ListAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<UserAccount>>([]);
    }

    /// <summary>
    /// Searches accounts by username or public display name. Email is omitted.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <param name="take">The maximum number of hits.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Matching public identities.</returns>
    Task<IReadOnlyList<MentionableAccount>> SearchAsync(
        string query,
        int take,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        return Task.FromResult<IReadOnlyList<MentionableAccount>>([]);
    }

    /// <summary>
    /// Returns seeded administrator test accounts ordered by number.
    /// </summary>
    Task<IReadOnlyList<UserAccount>> ListTestAccountsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<UserAccount>>([]);
    }
}

/// <summary>
/// Public mention identity for site chat. Email is omitted.
/// </summary>
public sealed class MentionableAccount
{
    /// <summary>Gets the account identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the name shown to other users.</summary>
    public required string DisplayName { get; init; }
}

/// <summary>
/// Request to create a local password account.
/// </summary>
public sealed class CreateLocalAccountRequest
{
    /// <summary>Gets the email address.</summary>
    public required string Email { get; init; }

    /// <summary>Gets the username.</summary>
    public required Username Username { get; init; }

    /// <summary>Gets the password.</summary>
    public required string Password { get; init; }

    /// <summary>Gets the person name.</summary>
    public required PersonName Name { get; init; }

    /// <summary>Gets the location.</summary>
    public required GeographicLocation Location { get; init; }

    /// <summary>Gets the optional IANA time-zone identifier used to display UTC timestamps.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets the display-name preference.</summary>
    public required DisplayNameMode DisplayNameMode { get; init; }

    /// <summary>Gets the optional avatar storage key.</summary>
    public string? AvatarStorageKey { get; init; }
}

/// <summary>
/// Request to create an account from an external login.
/// </summary>
public sealed class CreateExternalAccountRequest
{
    /// <summary>Gets the email address.</summary>
    public required string Email { get; init; }

    /// <summary>Gets a value indicating whether the provider confirmed the email.</summary>
    public required bool EmailConfirmed { get; init; }

    /// <summary>Gets the username.</summary>
    public required Username Username { get; init; }

    /// <summary>Gets the person name.</summary>
    public required PersonName Name { get; init; }

    /// <summary>Gets the location.</summary>
    public required GeographicLocation Location { get; init; }

    /// <summary>Gets the optional IANA time-zone identifier used to display UTC timestamps.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets the display-name preference.</summary>
    public required DisplayNameMode DisplayNameMode { get; init; }

    /// <summary>Gets the optional avatar storage key.</summary>
    public string? AvatarStorageKey { get; init; }

    /// <summary>Gets the external provider name.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the provider's stable user key.</summary>
    public required string ProviderKey { get; init; }
}

/// <summary>
/// Request to update stored profile fields.
/// </summary>
public sealed class UpdateStoredProfileRequest
{
    /// <summary>Gets the account identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the username.</summary>
    public required Username Username { get; init; }

    /// <summary>Gets the person name.</summary>
    public required PersonName Name { get; init; }

    /// <summary>Gets the location.</summary>
    public required GeographicLocation Location { get; init; }

    /// <summary>Gets the optional IANA time-zone identifier used to display UTC timestamps.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets the display-name preference.</summary>
    public required DisplayNameMode DisplayNameMode { get; init; }

    /// <summary>Gets whether unread notices appear on the home board.</summary>
    public bool InAppNotificationsEnabled { get; init; } = true;

    /// <summary>Gets whether notices are also queued for email.</summary>
    public bool EmailNotificationsEnabled { get; init; } = true;

    /// <summary>Gets the default site-chat compose language.</summary>
    public string PreferredChatLanguage { get; init; } = "English";

    /// <summary>Gets the expected profile revision.</summary>
    public required int ExpectedRevision { get; init; }
}

/// <summary>
/// Outcome of creating an account.
/// </summary>
public sealed class CreateLocalAccountOutcome
{
    /// <summary>Gets a value indicating whether creation succeeded.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Gets the created account when successful.</summary>
    public UserAccount? Account { get; init; }

    /// <summary>Gets the email confirmation token for local accounts.</summary>
    public string? EmailConfirmationToken { get; init; }

    /// <summary>Gets the error code when creation failed.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Gets the error message when creation failed.</summary>
    public string? Message { get; init; }
}

/// <summary>
/// Outcome of updating a stored profile.
/// </summary>
public sealed class UpdateProfileOutcome
{
    /// <summary>Gets a value indicating whether the update succeeded.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Gets the updated account when successful.</summary>
    public UserAccount? Account { get; init; }

    /// <summary>Gets the error code when the update failed.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Gets the error message when the update failed.</summary>
    public string? Message { get; init; }
}

/// <summary>
/// Outcome of changing a stored password.
/// </summary>
public sealed class ChangePasswordOutcome
{
    /// <summary>Gets a value indicating whether the change succeeded.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Gets the error code when the change failed.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Gets the error message when the change failed.</summary>
    public string? Message { get; init; }
}
