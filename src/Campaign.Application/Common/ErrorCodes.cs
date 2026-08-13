namespace Campaign.Application.Common;

/// <summary>
/// Stable machine-readable error codes returned by identity use cases.
/// </summary>
public static class ErrorCodes
{
    /// <summary>The supplied credentials were rejected.</summary>
    public const string InvalidCredentials = "auth.invalid_credentials";

    /// <summary>The account is locked after too many failed sign-in attempts.</summary>
    public const string LockedOut = "auth.locked_out";

    /// <summary>The account email address has not been confirmed.</summary>
    public const string EmailNotConfirmed = "auth.email_not_confirmed";

    /// <summary>The caller is not authenticated.</summary>
    public const string Unauthorized = "auth.unauthorized";

    /// <summary>The requested profile does not exist.</summary>
    public const string ProfileNotFound = "profile.not_found";

    /// <summary>The email address is already registered.</summary>
    public const string EmailTaken = "email.taken";

    /// <summary>The username is already registered.</summary>
    public const string UsernameTaken = "username.taken";

    /// <summary>The email address is not valid.</summary>
    public const string EmailInvalid = "email.invalid";

    /// <summary>The password does not meet complexity requirements.</summary>
    public const string PasswordInvalid = "password.invalid";

    /// <summary>The profile was modified by another request.</summary>
    public const string ConcurrencyConflict = "concurrency.conflict";

    /// <summary>The uploaded file type is not allowed.</summary>
    public const string UploadInvalidType = "upload.invalid_type";

    /// <summary>The uploaded file is too large.</summary>
    public const string UploadTooLarge = "upload.too_large";

    /// <summary>The uploaded image could not be processed.</summary>
    public const string UploadInvalidImage = "upload.invalid_image";

    /// <summary>An external login must be linked to an existing verified account.</summary>
    public const string ExternalLinkRequired = "auth.external_link_required";

    /// <summary>External login completion is required before the session is established.</summary>
    public const string ExternalProfileIncomplete = "auth.external_profile_incomplete";

    /// <summary>The requested external provider is not configured.</summary>
    public const string ExternalProviderUnavailable = "auth.external_provider_unavailable";
}
