using Campaign.Application.Identity;
using Campaign.Application.Ports;
using Campaign.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace Campaign.Infrastructure.Identity;

/// <summary>
/// Promotes the privileged administrator account and seeds Test 1–Test 30 outside automated tests.
/// </summary>
public sealed class IdentityMaintenance
{
    /// <summary>Email that always receives the Administrator role when the account exists.</summary>
    public const string PrivilegedEmail = "ross.gustafson@gmail.com";

    /// <summary>Username that always receives the Administrator role when the account exists.</summary>
    public const string PrivilegedUsername = "rosstheboss";

    /// <summary>ASP.NET Identity role name for system administrators.</summary>
    public const string AdministratorRole = "Administrator";

    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole<Guid>> _roles;
    private readonly IClock _clock;
    private readonly IHostEnvironment _environment;

    /// <summary>Initializes maintenance.</summary>
    public IdentityMaintenance(
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole<Guid>> roles,
        IClock clock,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(environment);
        _users = users;
        _roles = roles;
        _clock = clock;
        _environment = environment;
    }

    /// <summary>Ensures the Administrator role, promotes the privileged user, and seeds test accounts.</summary>
    public async Task EnsureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureAdministratorRoleAsync().ConfigureAwait(false);
        await PromotePrivilegedUserAsync(cancellationToken).ConfigureAwait(false);
        if (!_environment.IsEnvironment("Testing"))
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
            await _users.AddToRoleAsync(user, AdministratorRole).ConfigureAwait(false);
        }
    }

    private async Task PromotePrivilegedUserAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var byEmail = await _users.FindByEmailAsync(PrivilegedEmail).ConfigureAwait(false);
        if (byEmail is not null)
        {
            await PromoteIfPrivilegedAsync(byEmail).ConfigureAwait(false);
        }

        var byName = await _users.FindByNameAsync(PrivilegedUsername).ConfigureAwait(false);
        if (byName is not null && byName.Id != byEmail?.Id)
        {
            await PromoteIfPrivilegedAsync(byName).ConfigureAwait(false);
        }
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
                    await _users.UpdateAsync(existing).ConfigureAwait(false);
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
                IsTestAccount = true,
                TestAccountNumber = number,
                CreatedUtc = now,
                UpdatedUtc = now,
                ProfileRevision = 1,
            };
            var password = $"Test-{number:00}-{Guid.NewGuid():N}Aa!";
            await _users.CreateAsync(user, password).ConfigureAwait(false);
        }
    }

    private async Task EnsureAdministratorRoleAsync()
    {
        if (!await _roles.RoleExistsAsync(AdministratorRole).ConfigureAwait(false))
        {
            await _roles.CreateAsync(new IdentityRole<Guid>(AdministratorRole)).ConfigureAwait(false);
        }
    }

    private static bool IsPrivileged(ApplicationUser user)
    {
        return string.Equals(user.Email, PrivilegedEmail, StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.UserName, PrivilegedUsername, StringComparison.OrdinalIgnoreCase);
    }
}
