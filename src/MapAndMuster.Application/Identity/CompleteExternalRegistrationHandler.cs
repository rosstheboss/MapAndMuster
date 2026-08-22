using MapAndMuster.Application.Common;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Application.Identity;

/// <summary>
/// Completes registration after a successful external-provider challenge.
/// </summary>
public sealed class CompleteExternalRegistrationCommand
{
    /// <summary>Gets the email imported from the provider or supplied by the user.</summary>
    public required string Email { get; init; }

    /// <summary>Gets a value indicating whether the provider confirmed the email.</summary>
    public required bool EmailConfirmed { get; init; }

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
    public required DisplayNameMode DisplayNameMode { get; init; }

    /// <summary>Gets the external provider name.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the provider's stable user key.</summary>
    public required string ProviderKey { get; init; }

    /// <summary>Gets the optional imported avatar stream. The caller owns disposal.</summary>
    public Stream? AvatarContent { get; init; }

    /// <summary>Gets the avatar content type when an avatar is supplied.</summary>
    public string? AvatarContentType { get; init; }
}

/// <summary>
/// Creates an account from a completed external login and imported demographics.
/// </summary>
public sealed class CompleteExternalRegistrationHandler
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
    public CompleteExternalRegistrationHandler(
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
    /// Completes external registration after uniqueness and profile validation.
    /// </summary>
    /// <param name="command">The completion command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created account.</returns>
    public async Task<OperationResult<UserAccount>> HandleAsync(
        CompleteExternalRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<Domain.Common.DomainError>();
        var emailError = IdentityFieldErrors.Email(command.Email);
        if (emailError is not null)
        {
            errors.Add(emailError);
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

        var email = (command.Email ?? string.Empty).Trim();
        if (emailError is null && await _accounts.EmailExistsAsync(email, cancellationToken).ConfigureAwait(false))
        {
            errors.Add(new Domain.Common.DomainError(
                ErrorCodes.ExternalLinkRequired,
                "An account with that email already exists. Sign in and link the provider from your profile.",
                "email"));
        }

        if (username is not null
            && await _accounts.UsernameExistsAsync(username.Value, null, cancellationToken).ConfigureAwait(false))
        {
            errors.Add(new Domain.Common.DomainError(ErrorCodes.UsernameTaken, "That username is already taken.", "username"));
        }

        if (errors.Count > 0)
        {
            return OperationResults.Failure<UserAccount>(errors);
        }

        string? avatarKey = null;
        if (command.AvatarContent is not null)
        {
            var processed = await _avatarProcessor
                .ProcessAsync(command.AvatarContent, command.AvatarContentType ?? "image/jpeg", null, cancellationToken)
                .ConfigureAwait(false);
            if (processed.IsSuccess && processed.Content is not null && processed.FileExtension is not null)
            {
                avatarKey = await _avatarStorage
                    .SaveAsync(processed.Content, processed.FileExtension, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var created = await _accounts.CreateExternalAccountAsync(
                new CreateExternalAccountRequest
                {
                    Email = email,
                    EmailConfirmed = command.EmailConfirmed,
                    Username = username!,
                    Name = name!,
                    Location = location!,
                    TimeZoneId = timeZone!.Id,
                    DisplayNameMode = command.DisplayNameMode,
                    AvatarStorageKey = avatarKey,
                    Provider = command.Provider,
                    ProviderKey = command.ProviderKey,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!created.IsSuccess || created.Account is null)
        {
            if (avatarKey is not null)
            {
                await _avatarStorage.DeleteAsync(avatarKey, cancellationToken).ConfigureAwait(false);
            }

            return OperationResults.Failure<UserAccount>(
                created.ErrorCode ?? ErrorCodes.ExternalProviderUnavailable,
                created.Message ?? "The external account could not be created.");
        }

        return OperationResults.Success(created.Account);
    }
}
