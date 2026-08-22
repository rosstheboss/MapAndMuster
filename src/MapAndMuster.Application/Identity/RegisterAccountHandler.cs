using MapAndMuster.Application.Common;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Common;
using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Application.Identity;

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

        var errors = new List<DomainError>();
        var emailError = IdentityFieldErrors.Email(command.Email);
        if (emailError is not null)
        {
            errors.Add(emailError);
        }

        if (!PasswordPolicy.TryValidate(command.Password, out var passwordError))
        {
            errors.Add(passwordError);
        }

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
            out var profileErrors);
        errors.AddRange(profileErrors);

        if (emailError is null)
        {
            var email = command.Email.Trim();
            if (await _accounts.EmailExistsAsync(email, cancellationToken).ConfigureAwait(false))
            {
                errors.Add(new DomainError(ErrorCodes.EmailTaken, "That email address is already registered.", "email"));
            }
        }

        if (username is not null
            && await _accounts.UsernameExistsAsync(username.Value, null, cancellationToken).ConfigureAwait(false))
        {
            errors.Add(new DomainError(ErrorCodes.UsernameTaken, "That username is already taken.", "username"));
        }

        if (errors.Count > 0)
        {
            return OperationResults.Failure<RegisterAccountResult>(errors);
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
                    Email = command.Email.Trim(),
                    Username = username!,
                    Password = command.Password,
                    Name = name!,
                    Location = location!,
                    TimeZoneId = timeZone!.Id,
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
