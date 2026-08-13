using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.Identity;

namespace Campaign.Application.Identity;

/// <summary>
/// Registers a local email-and-password account and queues confirmation email.
/// </summary>
public sealed class RegisterAccountHandler
{
    private readonly IUserAccountStore _accounts;
    private readonly IEmailOutbox _emailOutbox;
    private readonly IAvatarImageProcessor _avatarProcessor;
    private readonly IAvatarStorage _avatarStorage;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="accounts">The account store.</param>
    /// <param name="emailOutbox">The email outbox.</param>
    /// <param name="avatarProcessor">The avatar processor.</param>
    /// <param name="avatarStorage">The avatar storage.</param>
    public RegisterAccountHandler(
        IUserAccountStore accounts,
        IEmailOutbox emailOutbox,
        IAvatarImageProcessor avatarProcessor,
        IAvatarStorage avatarStorage)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(emailOutbox);
        ArgumentNullException.ThrowIfNull(avatarProcessor);
        ArgumentNullException.ThrowIfNull(avatarStorage);

        _accounts = accounts;
        _emailOutbox = emailOutbox;
        _avatarProcessor = avatarProcessor;
        _avatarStorage = avatarStorage;
    }

    /// <summary>
    /// Registers a local account. The user is not signed in until the email is confirmed.
    /// </summary>
    /// <param name="command">The registration command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The registration result.</returns>
    public async Task<OperationResult<RegisterAccountResult>> HandleAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Username.TryCreate(command.Username, out var username, out var usernameError))
        {
            return OperationResults.Failure<RegisterAccountResult>(usernameError.Code, usernameError.Message);
        }

        if (!PersonName.TryCreate(command.FirstName, command.MiddleInitial, command.LastName, out var name, out var nameError))
        {
            return OperationResults.Failure<RegisterAccountResult>(nameError.Code, nameError.Message);
        }

        if (!GeographicLocation.TryCreate(command.City, command.Region, command.Country, out var location, out var locationError))
        {
            return OperationResults.Failure<RegisterAccountResult>(locationError.Code, locationError.Message);
        }

        if (!IanaTimeZone.TryCreateOptional(command.TimeZoneId, out var timeZone, out var timeZoneError))
        {
            return OperationResults.Failure<RegisterAccountResult>(timeZoneError.Code, timeZoneError.Message);
        }

        var email = command.Email.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            return OperationResults.Failure<RegisterAccountResult>(ErrorCodes.EmailInvalid, "Email address is invalid.");
        }

        if (await _accounts.EmailExistsAsync(email, cancellationToken).ConfigureAwait(false))
        {
            return OperationResults.Failure<RegisterAccountResult>(ErrorCodes.EmailTaken, "That email address is already registered.");
        }

        if (await _accounts.UsernameExistsAsync(username.Value, null, cancellationToken).ConfigureAwait(false))
        {
            return OperationResults.Failure<RegisterAccountResult>(ErrorCodes.UsernameTaken, "That username is already taken.");
        }

        string? avatarKey = null;
        if (command.AvatarContent is not null)
        {
            var processed = await _avatarProcessor
                .ProcessAsync(command.AvatarContent, command.AvatarContentType ?? string.Empty, command.AvatarLength, cancellationToken)
                .ConfigureAwait(false);
            if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null)
            {
                return OperationResults.Failure<RegisterAccountResult>(
                    processed.ErrorCode ?? ErrorCodes.UploadInvalidImage,
                    processed.Message ?? "The profile picture could not be processed.");
            }

            avatarKey = await _avatarStorage
                .SaveAsync(processed.Content, processed.FileExtension, cancellationToken)
                .ConfigureAwait(false);
        }

        var created = await _accounts.CreateLocalAccountAsync(
                new CreateLocalAccountRequest
                {
                    Email = email,
                    Username = username,
                    Password = command.Password,
                    Name = name,
                    Location = location,
                    TimeZoneId = timeZone?.Id,
                    DisplayNameMode = command.DisplayNameMode,
                    AvatarStorageKey = avatarKey,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!created.IsSuccess || created.Account is null)
        {
            if (avatarKey is not null)
            {
                await _avatarStorage.DeleteAsync(avatarKey, cancellationToken).ConfigureAwait(false);
            }

            return OperationResults.Failure<RegisterAccountResult>(
                created.ErrorCode ?? ErrorCodes.PasswordInvalid,
                created.Message ?? "The account could not be created.");
        }

        if (!string.IsNullOrWhiteSpace(created.EmailConfirmationToken))
        {
            await _emailOutbox
                .QueueEmailConfirmationAsync(created.Account.Email, created.Account.Id, created.EmailConfirmationToken, cancellationToken)
                .ConfigureAwait(false);
        }

        return OperationResults.Success(
            new RegisterAccountResult
            {
                UserId = created.Account.Id,
                Username = created.Account.Username,
            });
    }
}
