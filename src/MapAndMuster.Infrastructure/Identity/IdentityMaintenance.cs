using MapAndMuster.Application.Identity;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MapAndMuster.Infrastructure.Identity;

/// <summary>
/// Creates the privileged administrator when missing, promotes that account, and seeds Test 1–Test 45
/// outside the default Testing environment.
/// </summary>
public sealed partial class IdentityMaintenance
{
    /// <summary>Username that always receives the Administrator role when the account exists.</summary>
    public const string PrivilegedUsername = "rosstheboss";

    /// <summary>
    /// Non-deliverable mailbox used when creating the privileged administrator in Development or Testing
    /// if <see cref="IdentityBootstrapOptions.BootstrapAdminEmail"/> is empty.
    /// </summary>
    public const string DevelopmentBootstrapAdminEmail = "admin@users.invalid";

    /// <summary>ASP.NET Identity role name for system administrators.</summary>
    public const string AdministratorRole = "Administrator";

    private const string PrivilegedFirstName = "Admin";
    private const string PrivilegedLastName = "Operator";
    private const string PrivilegedCity = "Testville";
    private const string PrivilegedRegion = "Testshire";
    private const string PrivilegedCountry = "Testland";
    private const string PrivilegedTimeZoneId = "UTC";

    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole<Guid>> _roles;
    private readonly IClock _clock;
    private readonly IHostEnvironment _environment;
    private readonly IOptions<IdentityBootstrapOptions> _bootstrap;
    private readonly ILogger<IdentityMaintenance> _logger;

    /// <summary>Initializes maintenance.</summary>
    /// <param name="users">The Identity user manager.</param>
    /// <param name="roles">The Identity role manager.</param>
    /// <param name="clock">The authoritative clock.</param>
    /// <param name="environment">The host environment.</param>
    /// <param name="bootstrap">Bootstrap administrator settings.</param>
    /// <param name="logger">The logger.</param>
    public IdentityMaintenance(
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole<Guid>> roles,
        IClock clock,
        IHostEnvironment environment,
        IOptions<IdentityBootstrapOptions> bootstrap,
        ILogger<IdentityMaintenance> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(bootstrap);
        ArgumentNullException.ThrowIfNull(logger);
        _users = users;
        _roles = roles;
        _clock = clock;
        _environment = environment;
        _bootstrap = bootstrap;
        _logger = logger;
    }

    /// <summary>Ensures the Administrator role, the privileged user, and seeded test accounts.</summary>
    public async Task EnsureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureAdministratorRoleAsync().ConfigureAwait(false);
        await EnsurePrivilegedAdministratorAsync(cancellationToken).ConfigureAwait(false);
        if (ShouldSeedTestAccounts())
        {
            await SeedTestAccountsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Adds the Administrator role when the user matches the privileged email or username.</summary>
    public async Task PromoteIfPrivilegedAsync(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!IsPrivileged(user))
        {
            return;
        }

        await EnsureAdministratorRoleAsync().ConfigureAwait(false);
        if (!await _users.IsInRoleAsync(user, AdministratorRole).ConfigureAwait(false))
        {
            ThrowIfFailed(await _users.AddToRoleAsync(user, AdministratorRole).ConfigureAwait(false), "promoting the privileged administrator");
        }
    }

    private async Task EnsurePrivilegedAdministratorAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var privilegedEmail = ResolveBootstrapAdminEmail();
        var byEmail = await _users.FindByEmailAsync(privilegedEmail).ConfigureAwait(false);
        var byName = await _users.FindByNameAsync(PrivilegedUsername).ConfigureAwait(false);
        if (byEmail is not null)
        {
            await PromoteIfPrivilegedAsync(byEmail).ConfigureAwait(false);
        }

        if (byName is not null && byName.Id != byEmail?.Id)
        {
            await PromoteIfPrivilegedAsync(byName).ConfigureAwait(false);
        }

        if (byEmail is not null || byName is not null || !ShouldSeedTestAccounts())
        {
            return;
        }

        var password = _bootstrap.Value.BootstrapAdminPassword;
        if (string.IsNullOrWhiteSpace(password))
        {
            if (IsProductionLike())
            {
                throw new InvalidOperationException(
                    $"Missing required production configuration keys: {IdentityBootstrapOptions.BootstrapAdminPasswordKey}.");
            }

            LogMissingBootstrapPassword(_logger);
            return;
        }

        if (!PasswordPolicy.TryValidate(password, out _))
        {
            throw new InvalidOperationException(
                $"{IdentityBootstrapOptions.BootstrapAdminPasswordKey} does not meet the password policy.");
        }

        if (!Username.TryCreate(PrivilegedUsername, out var username, out _)
            || !PersonName.TryCreate(PrivilegedFirstName, null, PrivilegedLastName, null, out var name, out _)
            || !GeographicLocation.TryCreate(PrivilegedCity, PrivilegedRegion, PrivilegedCountry, out var location, out _))
        {
            throw new InvalidOperationException("Privileged administrator profile constants are invalid.");
        }

        var now = _clock.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = privilegedEmail,
            UserName = username.Value,
            FirstName = name.FirstName,
            MiddleInitial = name.MiddleInitial?.ToString(),
            LastName = name.LastName,
            City = location.City,
            Region = location.Region,
            Country = location.Country,
            TimeZoneId = PrivilegedTimeZoneId,
            DisplayNameMode = DisplayNameMode.Username,
            InAppNotificationsEnabled = true,
            EmailNotificationsEnabled = true,
            EmailConfirmed = true,
            PreferredChatLanguage = "English",
            DateTimeDisplayFormat = DateTimeDisplayFormats.Default,
            CreatedUtc = now,
            UpdatedUtc = now,
            ProfileRevision = 1,
        };

        ThrowIfFailed(await _users.CreateAsync(user, password).ConfigureAwait(false), "creating the privileged administrator");
        await PromoteIfPrivilegedAsync(user).ConfigureAwait(false);
        LogCreatedPrivilegedAdministrator(_logger);
    }

    private async Task SeedTestAccountsAsync(CancellationToken cancellationToken)
    {
        if (!GeographicLocation.TryCreate("Testville", "Testshire", "Testland", out var location, out _)
            || !PersonName.TryCreate("Test", null, "Account", null, out var name, out _))
        {
            return;
        }

        for (var number = 1; number <= TestAccountCatalog.Count; number++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var username = TestAccountCatalog.Username(number);
            var existing = await _users.FindByNameAsync(username).ConfigureAwait(false);
            if (existing is not null)
            {
                if (!existing.IsTestAccount || existing.TestAccountNumber != number)
                {
                    existing.IsTestAccount = true;
                    existing.TestAccountNumber = number;
                    existing.EmailNotificationsEnabled = false;
                    existing.EmailConfirmed = true;
                    ThrowIfFailed(await _users.UpdateAsync(existing).ConfigureAwait(false), "updating a seeded test account");
                }

                continue;
            }

            if (!Username.TryCreate(username, out var parsedUsername, out _))
            {
                continue;
            }

            var now = _clock.UtcNow;
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = TestAccountCatalog.Email(number),
                UserName = parsedUsername.Value,
                FirstName = name.FirstName,
                LastName = name.LastName,
                City = location.City,
                Region = location.Region,
                Country = location.Country,
                DisplayNameMode = DisplayNameMode.Username,
                InAppNotificationsEnabled = true,
                EmailNotificationsEnabled = false,
                EmailConfirmed = true,
                PreferredChatLanguage = "English",
                DateTimeDisplayFormat = DateTimeDisplayFormats.Default,
                IsTestAccount = true,
                TestAccountNumber = number,
                CreatedUtc = now,
                UpdatedUtc = now,
                ProfileRevision = 1,
            };
            var password = $"Test-{number:00}-{Guid.NewGuid():N}Aa!";
            ThrowIfFailed(await _users.CreateAsync(user, password).ConfigureAwait(false), "creating a seeded test account");
        }
    }

    private async Task EnsureAdministratorRoleAsync()
    {
        if (!await _roles.RoleExistsAsync(AdministratorRole).ConfigureAwait(false))
        {
            ThrowIfFailed(await _roles.CreateAsync(new IdentityRole<Guid>(AdministratorRole)).ConfigureAwait(false), "creating the Administrator role");
        }
    }

    private bool ShouldSeedTestAccounts()
    {
        return !_environment.IsEnvironment("Testing") || _bootstrap.Value.SeedTestAccounts;
    }

    private bool IsProductionLike()
    {
        return _environment.IsProduction() || _environment.IsEnvironment("Staging");
    }

    private bool IsPrivileged(ApplicationUser user)
    {
        if (string.Equals(user.UserName, PrivilegedUsername, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var email = _bootstrap.Value.BootstrapAdminEmail;
        return !string.IsNullOrWhiteSpace(email)
            && string.Equals(user.Email, email.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveBootstrapAdminEmail()
    {
        var configured = _bootstrap.Value.BootstrapAdminEmail;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        if (IsProductionLike())
        {
            throw new InvalidOperationException(
                $"Missing required production configuration keys: {IdentityBootstrapOptions.BootstrapAdminEmailKey}.");
        }

        return DevelopmentBootstrapAdminEmail;
    }

    private static void ThrowIfFailed(IdentityResult result, string action)
    {
        if (result.Succeeded)
        {
            return;
        }

        var details = string.Join("; ", result.Errors.Select(static error => error.Description));
        throw new InvalidOperationException($"Identity maintenance failed while {action}. {details}");
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Privileged administrator was not created because Identity:BootstrapAdminPassword is empty.")]
    private static partial void LogMissingBootstrapPassword(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Created the privileged administrator account.")]
    private static partial void LogCreatedPrivilegedAdministrator(ILogger logger);
}
