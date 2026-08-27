namespace MapAndMuster.Infrastructure.Identity;

/// <summary>
/// Identity seed settings. The bootstrap password is used only when the privileged administrator
/// does not exist yet. It never overwrites an existing account password.
/// </summary>
public sealed class IdentityBootstrapOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Identity";

    /// <summary>
    /// Hierarchical configuration key for the bootstrap administrator password.
    /// </summary>
    public const string BootstrapAdminPasswordKey = SectionName + ":BootstrapAdminPassword";

    /// <summary>
    /// Hierarchical configuration key for the bootstrap administrator email.
    /// </summary>
    public const string BootstrapAdminEmailKey = SectionName + ":BootstrapAdminEmail";

    /// <summary>
    /// Gets or sets the password assigned when creating the privileged administrator.
    /// Store this in user secrets or the host secret store, never in source control.
    /// </summary>
    public string? BootstrapAdminPassword { get; set; }

    /// <summary>
    /// Gets or sets the email assigned when creating the privileged administrator.
    /// Production and Staging require this. Keep personal mailboxes in host configuration, not Git.
    /// </summary>
    public string? BootstrapAdminEmail { get; set; }

    /// <summary>
    /// Gets or sets whether to seed the privileged administrator and Test 1–Test 45 in the Testing
    /// environment. Ignored outside Testing; those hosts always seed.
    /// </summary>
    public bool SeedTestAccounts { get; set; }
}
