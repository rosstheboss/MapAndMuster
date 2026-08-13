namespace Campaign.Domain.Identity;

/// <summary>
/// Controls whether other users see this account's full name or only its username.
/// </summary>
public enum DisplayNameMode
{
    /// <summary>
    /// Other users see the unique username.
    /// </summary>
    Username = 0,

    /// <summary>
    /// Other users see the account's full name in addition to the username.
    /// </summary>
    FullName = 1,
}
