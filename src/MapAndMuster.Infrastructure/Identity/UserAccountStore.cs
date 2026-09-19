using MapAndMuster.Application.Common;
using MapAndMuster.Application.Identity;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Identity;
using MapAndMuster.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MapAndMuster.Infrastructure.Identity;

/// <summary>
/// Identity-backed account store.
/// </summary>
public sealed class UserAccountStore : IUserAccountStore
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CampaignDbContext _dbContext;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new store.
    /// </summary>
    /// <param name="userManager">The Identity user manager.</param>
    /// <param name="dbContext">The database context, used for set-based role and search queries.</param>
    /// <param name="clock">The clock.</param>
    public UserAccountStore(UserManager<ApplicationUser> userManager, CampaignDbContext dbContext, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(clock);
        _userManager = userManager;
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc />
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return _userManager.Users.AnyAsync(
            user => user.NormalizedEmail == _userManager.NormalizeEmail(email),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> UsernameExistsAsync(string username, Guid? userIdToIgnore, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        var normalized = _userManager.NormalizeName(username);
        return _userManager.Users.AnyAsync(
            user => user.NormalizedUserName == normalized && (!userIdToIgnore.HasValue || user.Id != userIdToIgnore.Value),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CreateLocalAccountOutcome> CreateLocalAccountAsync(
        CreateLocalAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var user = CreateUser(
            request.Email,
            request.Username,
            request.Name,
            request.Location,
            request.TimeZoneId,
            request.DisplayNameMode,
            request.AvatarStorageKey);
        var created = await _userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!created.Succeeded)
        {
            return ToCreateFailure(created);
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
        return new CreateLocalAccountOutcome
        {
            IsSuccess = true,
            Account = Map(user),
            EmailConfirmationToken = token,
        };
    }

    /// <inheritdoc />
    public async Task<CreateLocalAccountOutcome> CreateExternalAccountAsync(
        CreateExternalAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var user = CreateUser(
            request.Email,
            request.Username,
            request.Name,
            request.Location,
            request.TimeZoneId,
            request.DisplayNameMode,
            request.AvatarStorageKey);
        user.EmailConfirmed = request.EmailConfirmed;

        var created = await _userManager.CreateAsync(user).ConfigureAwait(false);
        if (!created.Succeeded)
        {
            return ToCreateFailure(created);
        }

        var login = await _userManager.AddLoginAsync(
                user,
                new UserLoginInfo(request.Provider, request.ProviderKey, request.Provider))
            .ConfigureAwait(false);
        if (!login.Succeeded)
        {
            await _userManager.DeleteAsync(user).ConfigureAwait(false);
            return ToCreateFailure(login);
        }

        return new CreateLocalAccountOutcome
        {
            IsSuccess = true,
            Account = Map(user),
        };
    }

    /// <inheritdoc />
    public async Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        return user is null ? null : Map(user);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, UserAccount>> FindManyByIdAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        cancellationToken.ThrowIfCancellationRequested();
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, UserAccount>();
        }

        var wanted = userIds.Distinct().ToArray();
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user => wanted.Contains(user.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return users.ToDictionary(static user => user.Id, Map);
    }

    /// <inheritdoc />
    public async Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByNameAsync(username).ConfigureAwait(false);
        return user is null ? null : Map(user);
    }

    /// <inheritdoc />
    public async Task<UpdateProfileOutcome> UpdateProfileAsync(
        UpdateStoredProfileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return new UpdateProfileOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ProfileNotFound,
                Message = "The profile was not found.",
            };
        }

        if (user.ProfileRevision != request.ExpectedRevision)
        {
            return new UpdateProfileOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ConcurrencyConflict,
                Message = "The profile was updated by another request. Reload and try again.",
            };
        }

        if (user.IsTestAccount)
        {
            return new UpdateProfileOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.CampaignForbidden,
                Message = "Test accounts cannot change their profile.",
            };
        }

        if (user.IsGuestAccount)
        {
            return new UpdateProfileOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.GuestForbidden,
                Message = GuestRestrictions.Message,
            };
        }

        user.UserName = request.Username.Value;
        user.FirstName = request.Name.FirstName;
        user.MiddleInitial = request.Name.MiddleInitial?.ToString();
        user.LastName = request.Name.LastName;
        user.Suffix = request.Name.Suffix;
        user.City = request.Location.City;
        user.Region = request.Location.Region;
        user.Country = request.Location.Country;
        user.TimeZoneId = request.TimeZoneId;
        user.DisplayNameMode = request.DisplayNameMode;
        user.InAppNotificationsEnabled = request.InAppNotificationsEnabled;
        user.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        user.PreferredChatLanguage = request.PreferredChatLanguage;
        user.DateTimeDisplayFormat = request.DateTimeDisplayFormat;
        user.UpdatedUtc = _clock.UtcNow;
        user.ProfileRevision++;

        try
        {
            var updated = await _userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!updated.Succeeded)
            {
                return new UpdateProfileOutcome
                {
                    IsSuccess = false,
                    ErrorCode = MapIdentityError(updated),
                    Message = CombineErrors(updated),
                };
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateProfileOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ConcurrencyConflict,
                Message = "The profile was updated by another request. Reload and try again.",
            };
        }

        return new UpdateProfileOutcome
        {
            IsSuccess = true,
            Account = Map(user),
        };
    }

    /// <inheritdoc />
    public async Task<string?> ReplaceAvatarKeyAsync(Guid userId, string? avatarStorageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var previous = user.AvatarStorageKey;
        user.AvatarStorageKey = avatarStorageKey;
        user.UpdatedUtc = _clock.UtcNow;
        user.ProfileRevision++;
        await _userManager.UpdateAsync(user).ConfigureAwait(false);
        return previous;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> FindAdministratorIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        cancellationToken.ThrowIfCancellationRequested();
        if (userIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        // GetUsersInRoleAsync would materialize every administrator account. Join on the role
        // link table filtered to the requested users instead; callers only need membership flags.
        var wanted = userIds.Distinct().ToArray();
        var normalizedRole = _userManager.KeyNormalizer.NormalizeName(IdentityMaintenance.AdministratorRole);
        var administrators = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(link => wanted.Contains(link.UserId)
                && _dbContext.Roles.Any(role => role.Id == link.RoleId && role.NormalizedName == normalizedRole))
            .Select(static link => link.UserId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return administrators.ToHashSet();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MentionableAccount>> ListMentionableAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user => !user.IsTestAccount && !user.IsGuestAccount)
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. users.Select(user => new MentionableAccount
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            DisplayName = ProfileMapper.ToPublic(Map(user)).DisplayName,
        })];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserAccount>> ListAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user => !user.IsGuestAccount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. users.Select(Map)];
    }

    /// <inheritdoc />
    public async Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return new ChangePasswordOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ProfileNotFound,
                Message = "The profile was not found.",
            };
        }

        if (user.IsGuestAccount)
        {
            return new ChangePasswordOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.GuestForbidden,
                Message = GuestRestrictions.Message,
            };
        }

        var changed = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword).ConfigureAwait(false);
        if (!changed.Succeeded)
        {
            if (changed.Errors.Any(error =>
                    error.Code.Contains("PasswordMismatch", StringComparison.OrdinalIgnoreCase)
                    || error.Code.Contains("PasswordIncorrect", StringComparison.OrdinalIgnoreCase)))
            {
                return new ChangePasswordOutcome
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.CurrentPasswordInvalid,
                    Message = "Current password is incorrect.",
                };
            }

            return new ChangePasswordOutcome
            {
                IsSuccess = false,
                ErrorCode = MapIdentityError(changed),
                Message = CombineErrors(changed),
            };
        }

        return new ChangePasswordOutcome { IsSuccess = true };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MentionableAccount>> SearchAsync(
        string query,
        int take,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        cancellationToken.ThrowIfCancellationRequested();
        var needle = query.Trim();
        if (needle.Length < 2 || take <= 0)
        {
            return [];
        }

        // The database narrows to candidates and the display-name rule below decides the actual
        // matches, so this predicate is deliberately broader than the final one: it covers the
        // username, either name part, the two joined as they appear in a full display name, and
        // every test account, whose display name is derived rather than stored. Without it every
        // account in the table was loaded and mapped on each keystroke.
        var pattern = $"%{needle.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)}%";
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user =>
                !user.IsGuestAccount
                && (EF.Functions.ILike(user.UserName!, pattern, "\\")
                || EF.Functions.ILike(user.FirstName, pattern, "\\")
                || EF.Functions.ILike(user.LastName, pattern, "\\")
                || EF.Functions.ILike(user.FirstName + " " + user.LastName, pattern, "\\")
                || user.IsTestAccount))
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return
        [
            .. users
                .Select(user =>
                {
                    var account = Map(user);
                    return new MentionableAccount
                    {
                        UserId = account.Id,
                        Username = account.Username,
                        DisplayName = ProfileMapper.ToPublic(account).DisplayName,
                    };
                })
                .Where(hit =>
                    hit.Username.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || hit.DisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase))
                .Take(take),
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserAccount>> ListTestAccountsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user => user.IsTestAccount)
            .OrderBy(user => user.TestAccountNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. users.Select(Map)];
    }

    /// <inheritdoc />
    public async Task<AllocateGuestAccountOutcome> AllocateGuestAccountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await RecycleExpiredGuestAccountsAsync(cancellationToken).ConfigureAwait(false);

        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var outcome = await TryAllocateGuestAccountAsync(cancellationToken).ConfigureAwait(false);
            if (outcome.IsSuccess || outcome.ErrorCode != ErrorCodes.ConcurrencyConflict)
            {
                return outcome;
            }
        }

        return new AllocateGuestAccountOutcome
        {
            IsSuccess = false,
            ErrorCode = ErrorCodes.GuestUnavailable,
            Message = "Guest preview is busy. Try again in a moment.",
        };
    }

    /// <inheritdoc />
    public async Task RecycleGuestAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null || !user.IsGuestAccount)
        {
            return;
        }

        await _userManager.DeleteAsync(user).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecycleExpiredGuestAccountsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _clock.UtcNow;
        var expired = await _userManager.Users
            .Where(user => user.IsGuestAccount && user.GuestExpiresUtc != null && user.GuestExpiresUtc <= now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var user in expired)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _userManager.DeleteAsync(user).ConfigureAwait(false);
        }
    }

    private async Task<AllocateGuestAccountOutcome> TryAllocateGuestAccountAsync(CancellationToken cancellationToken)
    {
        var used = await _userManager.Users
            .Where(user => user.IsGuestAccount && user.GuestAccountNumber != null)
            .Select(user => user.GuestAccountNumber!.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (used.Count >= GuestAccountCatalog.MaxConcurrent)
        {
            return new AllocateGuestAccountOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.GuestUnavailable,
                Message = "Guest preview is at capacity. Try again later.",
            };
        }

        var taken = used.ToHashSet();
        var number = 1;
        while (taken.Contains(number))
        {
            number++;
        }

        if (!Username.TryCreate(GuestAccountCatalog.Username(number), out var username, out _)
            || !PersonName.TryCreate("Guest", null, "Account", null, out var name, out _)
            || !GeographicLocation.TryCreate(
                GuestAccountCatalog.Location,
                GuestAccountCatalog.Location,
                GuestAccountCatalog.Location,
                out var location,
                out _))
        {
            return new AllocateGuestAccountOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.GuestUnavailable,
                Message = "Guest preview is unavailable.",
            };
        }

        var now = _clock.UtcNow;
        var expires = now + GuestAccountCatalog.Lifetime;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = GuestAccountCatalog.Email(number),
            UserName = username.Value,
            FirstName = name.FirstName,
            LastName = name.LastName,
            City = location.City,
            Region = location.Region,
            Country = location.Country,
            DisplayNameMode = DisplayNameMode.Username,
            InAppNotificationsEnabled = false,
            EmailNotificationsEnabled = false,
            EmailConfirmed = true,
            PreferredChatLanguage = "English",
            DateTimeDisplayFormat = DateTimeDisplayFormats.Default,
            IsGuestAccount = true,
            GuestAccountNumber = number,
            GuestExpiresUtc = expires,
            CreatedUtc = now,
            UpdatedUtc = now,
            ProfileRevision = 1,
        };

        IdentityResult created;
        try
        {
            created = await _userManager.CreateAsync(user).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            return new AllocateGuestAccountOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.ConcurrencyConflict,
                Message = "Guest preview is busy. Try again in a moment.",
            };
        }

        if (!created.Succeeded)
        {
            if (created.Errors.Any(error =>
                    error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
            {
                return new AllocateGuestAccountOutcome
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ConcurrencyConflict,
                    Message = "Guest preview is busy. Try again in a moment.",
                };
            }

            return new AllocateGuestAccountOutcome
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.GuestUnavailable,
                Message = "Guest preview is unavailable.",
            };
        }

        return new AllocateGuestAccountOutcome
        {
            IsSuccess = true,
            Account = Map(user),
            ExpiresUtc = expires,
        };
    }

    private ApplicationUser CreateUser(
        string email,
        Username username,
        PersonName name,
        GeographicLocation location,
        string? timeZoneId,
        DisplayNameMode displayNameMode,
        string? avatarStorageKey)
    {
        var now = _clock.UtcNow;
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = username.Value,
            FirstName = name.FirstName,
            MiddleInitial = name.MiddleInitial?.ToString(),
            LastName = name.LastName,
            Suffix = name.Suffix,
            City = location.City,
            Region = location.Region,
            Country = location.Country,
            TimeZoneId = timeZoneId,
            DisplayNameMode = displayNameMode,
            AvatarStorageKey = avatarStorageKey,
            InAppNotificationsEnabled = true,
            EmailNotificationsEnabled = true,
            PreferredChatLanguage = "English",
            DateTimeDisplayFormat = DateTimeDisplayFormats.Default,
            CreatedUtc = now,
            UpdatedUtc = now,
            ProfileRevision = 1,
        };
    }

    private static UserAccount Map(ApplicationUser user)
    {
        char? initial = null;
        if (!string.IsNullOrWhiteSpace(user.MiddleInitial))
        {
            initial = user.MiddleInitial[0];
        }

        return new UserAccount
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Username = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            MiddleInitial = initial,
            LastName = user.LastName,
            Suffix = user.Suffix,
            City = user.City,
            Region = user.Region,
            Country = user.Country,
            TimeZoneId = user.TimeZoneId,
            DisplayNameMode = user.DisplayNameMode,
            AvatarStorageKey = user.AvatarStorageKey,
            CreatedUtc = user.CreatedUtc,
            UpdatedUtc = user.UpdatedUtc,
            ProfileRevision = user.ProfileRevision,
            EmailConfirmed = user.EmailConfirmed,
            InAppNotificationsEnabled = user.InAppNotificationsEnabled,
            EmailNotificationsEnabled = user.EmailNotificationsEnabled,
            PreferredChatLanguage = string.IsNullOrWhiteSpace(user.PreferredChatLanguage)
                ? "English"
                : user.PreferredChatLanguage,
            DateTimeDisplayFormat = user.DateTimeDisplayFormat,
            IsTestAccount = user.IsTestAccount,
            TestAccountNumber = user.TestAccountNumber,
            IsGuestAccount = user.IsGuestAccount,
            GuestAccountNumber = user.GuestAccountNumber,
            GuestExpiresUtc = user.GuestExpiresUtc,
        };
    }

    private static CreateLocalAccountOutcome ToCreateFailure(IdentityResult result)
    {
        return new CreateLocalAccountOutcome
        {
            IsSuccess = false,
            ErrorCode = MapIdentityError(result),
            Message = CombineErrors(result),
        };
    }

    private static string MapIdentityError(IdentityResult result)
    {
        if (result.Errors.Any(error => error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)))
        {
            return ErrorCodes.PasswordInvalid;
        }

        if (result.Errors.Any(error => error.Code.Contains("DuplicateEmail", StringComparison.OrdinalIgnoreCase)))
        {
            return ErrorCodes.EmailTaken;
        }

        if (result.Errors.Any(error => error.Code.Contains("DuplicateUserName", StringComparison.OrdinalIgnoreCase)))
        {
            return ErrorCodes.UsernameTaken;
        }

        if (result.Errors.Any(error => error.Code.Contains("InvalidEmail", StringComparison.OrdinalIgnoreCase)))
        {
            return ErrorCodes.EmailInvalid;
        }

        return ErrorCodes.PasswordInvalid;
    }

    private static string CombineErrors(IdentityResult result)
    {
        return string.Join(" ", result.Errors.Select(error => error.Description));
    }
}
