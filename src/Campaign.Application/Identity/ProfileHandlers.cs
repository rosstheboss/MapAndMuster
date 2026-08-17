using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.Common;
using Campaign.Domain.Identity;

namespace Campaign.Application.Identity;

/// <summary>
/// Updates the authenticated user's own profile fields.
/// </summary>
public sealed class UpdateProfileHandler
{
    private readonly IUserAccountStore _accounts;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="accounts">The account store.</param>
    public UpdateProfileHandler(IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        _accounts = accounts;
    }

    /// <summary>
    /// Updates profile fields for the owning user.
    /// </summary>
    /// <param name="command">The update command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated account.</returns>
    public async Task<OperationResult<UserAccount>> HandleAsync(
        UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _ = AccountProfileRules.TryCreate(
            command.Username,
            command.FirstName,
            command.MiddleInitial,
            command.LastName,
            command.Suffix,
            command.City,
            command.Region,
            command.Country,
            command.TimeZoneId,
            out var username,
            out var name,
            out var location,
            out var timeZone,
            out var errors);

        if (username is not null
            && await _accounts.UsernameExistsAsync(username.Value, command.UserId, cancellationToken).ConfigureAwait(false))
        {
            errors = [.. errors, new DomainError(ErrorCodes.UsernameTaken, "That username is already taken.", "username")];
        }

        if (errors.Count > 0)
        {
            return OperationResults.Failure<UserAccount>(errors);
        }

        var updated = await _accounts.UpdateProfileAsync(
                new UpdateStoredProfileRequest
                {
                    UserId = command.UserId,
                    Username = username!,
                    Name = name!,
                    Location = location!,
                    TimeZoneId = timeZone!.Id,
                    DisplayNameMode = command.DisplayNameMode,
                    InAppNotificationsEnabled = command.InAppNotificationsEnabled,
                    EmailNotificationsEnabled = command.EmailNotificationsEnabled,
                    ExpectedRevision = command.ProfileRevision,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!updated.IsSuccess || updated.Account is null)
        {
            return OperationResults.Failure<UserAccount>(
                updated.ErrorCode ?? ErrorCodes.ProfileNotFound,
                updated.Message ?? "The profile could not be updated.");
        }

        return OperationResults.Success(updated.Account);
    }
}

/// <summary>
/// Replaces the authenticated user's avatar.
/// </summary>
public sealed class UploadAvatarHandler
{
    private readonly IUserAccountStore _accounts;
    private readonly IAvatarImageProcessor _avatarProcessor;
    private readonly IAvatarStorage _avatarStorage;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="accounts">The account store.</param>
    /// <param name="avatarProcessor">The avatar processor.</param>
    /// <param name="avatarStorage">The avatar storage.</param>
    public UploadAvatarHandler(
        IUserAccountStore accounts,
        IAvatarImageProcessor avatarProcessor,
        IAvatarStorage avatarStorage)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(avatarProcessor);
        ArgumentNullException.ThrowIfNull(avatarStorage);

        _accounts = accounts;
        _avatarProcessor = avatarProcessor;
        _avatarStorage = avatarStorage;
    }

    /// <summary>
    /// Replaces the user's avatar after validating and re-encoding the upload.
    /// </summary>
    /// <param name="command">The upload command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated account.</returns>
    public async Task<OperationResult<UserAccount>> HandleAsync(
        UploadAvatarCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var account = await _accounts.FindByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return OperationResults.Failure<UserAccount>(ErrorCodes.ProfileNotFound, "The profile was not found.");
        }

        var processed = await _avatarProcessor
            .ProcessAsync(command.Content, command.ContentType, command.Length, cancellationToken)
            .ConfigureAwait(false);
        if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null)
        {
            return OperationResults.Failure<UserAccount>(
                processed.ErrorCode ?? ErrorCodes.UploadInvalidImage,
                processed.Message ?? "The profile picture could not be processed.");
        }

        var newKey = await _avatarStorage
            .SaveAsync(processed.Content, processed.FileExtension, cancellationToken)
            .ConfigureAwait(false);

        var previousKey = await _accounts
            .ReplaceAvatarKeyAsync(command.UserId, newKey, cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            await _avatarStorage.DeleteAsync(previousKey, cancellationToken).ConfigureAwait(false);
        }

        var updated = await _accounts.FindByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (updated is null)
        {
            return OperationResults.Failure<UserAccount>(ErrorCodes.ProfileNotFound, "The profile was not found.");
        }

        return OperationResults.Success(updated);
    }
}

/// <summary>
/// Reads the authenticated user's private profile.
/// </summary>
public sealed class GetOwnProfileHandler
{
    private readonly IUserAccountStore _accounts;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="accounts">The account store.</param>
    public GetOwnProfileHandler(IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        _accounts = accounts;
    }

    /// <summary>
    /// Returns the caller's private profile.
    /// </summary>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The private profile.</returns>
    public async Task<OperationResult<UserAccount>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return OperationResults.Failure<UserAccount>(ErrorCodes.ProfileNotFound, "The profile was not found.");
        }

        return OperationResults.Success(account);
    }
}

/// <summary>
/// Reads another user's public profile. Private fields are omitted by mapping, not by the client.
/// </summary>
public sealed class GetPublicProfileHandler
{
    private readonly IUserAccountStore _accounts;
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="accounts">The account store.</param>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    public GetPublicProfileHandler(IUserAccountStore accounts, ICampaignStore campaigns, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        _accounts = accounts;
        _campaigns = campaigns;
        _clock = clock;
    }

    /// <summary>
    /// Returns the public profile for a username, including campaigns the viewer may see.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="viewerUserId">The viewing user's identifier, when signed in.</param>
    /// <param name="isAdministrator">Whether the viewer is a system administrator.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The public profile.</returns>
    public async Task<OperationResult<PublicProfile>> HandleAsync(
        string username,
        Guid? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var account = await _accounts.FindByUsernameAsync(username, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return OperationResults.Failure<PublicProfile>(ErrorCodes.ProfileNotFound, "The profile was not found.");
        }

        var owned = await _campaigns.ListForUserAsync(account.Id, cancellationToken).ConfigureAwait(false);
        var viewerId = viewerUserId ?? Guid.Empty;
        var utcNow = _clock.UtcNow;
        var campaigns = owned
            .Where(campaign => CampaignAccess.CanView(campaign, viewerId, isAdministrator))
            .Select(campaign => new PublicProfileCampaign
            {
                Id = campaign.Id,
                Name = campaign.Name,
                Status = CampaignLifecycle.Progress(campaign, utcNow).Status.ToString(),
                IsPrivate = campaign.IsPrivate,
            })
            .ToArray();

        return OperationResults.Success(ProfileMapper.ToPublic(account, campaigns));
    }
}
