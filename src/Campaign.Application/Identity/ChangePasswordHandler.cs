using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.Common;
using Campaign.Domain.Identity;

namespace Campaign.Application.Identity;

/// <summary>
/// Input required to change the authenticated user's password.
/// </summary>
public sealed class ChangePasswordCommand
{
    /// <summary>
    /// Gets the account identifier of the authenticated user.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the current password.
    /// </summary>
    public required string CurrentPassword { get; init; }

    /// <summary>
    /// Gets the proposed password.
    /// </summary>
    public required string NewPassword { get; init; }
}

/// <summary>
/// Changes the authenticated user's password after verifying the current password.
/// </summary>
public sealed class ChangePasswordHandler
{
    private readonly IUserAccountStore _accounts;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="accounts">The account store.</param>
    public ChangePasswordHandler(IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        _accounts = accounts;
    }

    /// <summary>
    /// Changes the password for the owning user.
    /// </summary>
    /// <param name="command">The change command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The outcome.</returns>
    public async Task<OperationResult> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<DomainError>();
        if (string.IsNullOrEmpty(command.CurrentPassword))
        {
            errors.Add(new DomainError(
                ErrorCodes.CurrentPasswordInvalid,
                "Current password is not filled in.",
                "currentPassword"));
        }

        if (!PasswordPolicy.TryValidate(command.NewPassword, out var passwordError, "newPassword"))
        {
            errors.Add(passwordError);
        }

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        var changed = await _accounts
            .ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken)
            .ConfigureAwait(false);

        if (!changed.IsSuccess)
        {
            if (string.Equals(changed.ErrorCode, ErrorCodes.CurrentPasswordInvalid, StringComparison.Ordinal))
            {
                return OperationResult.Failure(
                [
                    new DomainError(
                        ErrorCodes.CurrentPasswordInvalid,
                        changed.Message ?? "Current password is incorrect.",
                        "currentPassword"),
                ]);
            }

            return OperationResult.Failure(
                changed.ErrorCode ?? ErrorCodes.PasswordInvalid,
                changed.Message ?? "The password could not be updated.");
        }

        return OperationResult.Success();
    }
}
