using System.Security.Claims;
using Campaign.Api.Contracts;
using Campaign.Application.Common;
using Campaign.Domain.Identity;

namespace Campaign.Api;

/// <summary>
/// HTTP helpers for identity endpoints.
/// </summary>
public static class IdentityHttp
{
    /// <summary>
    /// Authentication scheme used while an external login waits for profile completion.
    /// </summary>
    public const string ExternalRegistrationScheme = "ExternalRegistration";

    /// <summary>
    /// Rate-limit policy for authentication endpoints.
    /// </summary>
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>
    /// Rate-limit policy for uploads.
    /// </summary>
    public const string UploadRateLimitPolicy = "upload";

    /// <summary>
    /// Rate-limit policy for chat posts.
    /// </summary>
    public const string ChatRateLimitPolicy = "chat";

    /// <summary>
    /// Reads the authenticated user's identifier.
    /// </summary>
    /// <param name="user">The user principal.</param>
    /// <returns>The user identifier, or <see langword="null"/>.</returns>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    /// <summary>
    /// Whether the caller has the system Administrator role.
    /// </summary>
    /// <param name="user">The user principal.</param>
    /// <returns><see langword="true"/> when the caller is an administrator.</returns>
    public static bool IsAdministrator(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.IsInRole("Administrator");
    }

    /// <summary>
    /// Parses the public display-name preference.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="mode">The parsed mode.</param>
    /// <returns><see langword="true"/> when the value is valid.</returns>
    public static bool TryParseDisplayNameMode(string? value, out DisplayNameMode mode)
    {
        return Enum.TryParse(value, ignoreCase: true, out mode)
            && Enum.IsDefined(mode);
    }

    /// <summary>
    /// Maps a failed operation to an HTTP result.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The message.</param>
    /// <returns>The HTTP result.</returns>
    public static IResult Problem(string errorCode, string message)
    {
        return Problem(errorCode, message, []);
    }

    /// <summary>
    /// Maps a failed operation result to an HTTP result.
    /// </summary>
    /// <param name="result">The failed result.</param>
    /// <returns>The HTTP result.</returns>
    public static IResult Problem(OperationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return Problem(
            result.ErrorCode ?? "request.invalid",
            result.Message ?? "The request was invalid.",
            result.Errors);
    }

    /// <summary>
    /// Maps a failed operation result to an HTTP result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The failed result.</param>
    /// <returns>The HTTP result.</returns>
    public static IResult Problem<T>(OperationResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return Problem(
            result.ErrorCode ?? "request.invalid",
            result.Message ?? "The request was invalid.",
            result.Errors);
    }

    private static IResult Problem(string errorCode, string message, IReadOnlyList<Campaign.Domain.Common.DomainError> errors)
    {
        var status = StatusFor(errorCode);
        return Results.Json(
            new ErrorResponse(errorCode, message, ToFieldErrors(errors)),
            statusCode: status);
    }

    private static int StatusFor(string errorCode)
    {
        return errorCode switch
        {
            ErrorCodes.Unauthorized or ErrorCodes.InvalidCredentials => StatusCodes.Status401Unauthorized,
            ErrorCodes.LockedOut or ErrorCodes.EmailNotConfirmed => StatusCodes.Status403Forbidden,
            ErrorCodes.ProfileNotFound or ErrorCodes.CampaignNotFound => StatusCodes.Status404NotFound,
            ErrorCodes.CampaignForbidden or ErrorCodes.CampaignLocked => StatusCodes.Status403Forbidden,
            ErrorCodes.EmailTaken or ErrorCodes.UsernameTaken or ErrorCodes.ConcurrencyConflict
                or ErrorCodes.ExternalLinkRequired
                or ErrorCodes.CampaignAlreadyMember
                or ErrorCodes.CampaignJoinFull => StatusCodes.Status409Conflict,
            ErrorCodes.UploadTooLarge => StatusCodes.Status413PayloadTooLarge,
            ErrorCodes.ExternalProviderUnavailable => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };
    }

    private static FieldErrorResponse[]? ToFieldErrors(IReadOnlyList<Campaign.Domain.Common.DomainError> errors)
    {
        if (errors.Count == 0)
        {
            return null;
        }

        var mapped = errors
            .Where(static error => !string.IsNullOrWhiteSpace(error.Field))
            .Select(static error => new FieldErrorResponse(error.Field!, error.Code, error.Message))
            .ToArray();
        return mapped.Length == 0 ? null : mapped;
    }
}
