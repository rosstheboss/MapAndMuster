using Campaign.Application.Common;
using Campaign.Application.Identity;
using Campaign.Application.Ports;
using Campaign.Domain.Identity;

namespace Campaign.Backend.UnitTests.Identity;

public sealed class RegisterAccountHandlerTests
{
    [Fact]
    public async Task RejectsDuplicateUsernameBeforeCreatingAccount()
    {
        var store = new FakeUserAccountStore { UsernameTaken = true };
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.UsernameTaken, result.ErrorCode);
        Assert.Equal(0, store.CreateCalls);
    }

    [Fact]
    public async Task RejectsUnknownTimeZoneBeforeCreatingAccount()
    {
        var store = new FakeUserAccountStore();
        var handler = CreateHandler(store);
        var command = ValidCommand("Not/AZone");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.TimeZoneInvalid, result.ErrorCode);
        Assert.Equal(0, store.CreateCalls);
    }

    [Fact]
    public async Task ListsEveryInvalidField()
    {
        var store = new FakeUserAccountStore();
        var handler = CreateHandler(store);
        var command = new RegisterAccountCommand
        {
            Email = "not-an-email",
            Username = "ab",
            Password = "short",
            FirstName = "A",
            LastName = "",
            City = "",
            Region = null,
            Country = "Canada",
            DisplayNameMode = DisplayNameMode.Username,
        };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.ValidationFailed, result.ErrorCode);
        Assert.Contains("Username is too short", result.Message, StringComparison.Ordinal);
        Assert.Contains("Password is too short", result.Message, StringComparison.Ordinal);
        Assert.Contains("First name is too short", result.Message, StringComparison.Ordinal);
        Assert.Contains("Last name is not filled in", result.Message, StringComparison.Ordinal);
        Assert.Contains("City is not filled in", result.Message, StringComparison.Ordinal);
        Assert.Contains("State or province is not filled in", result.Message, StringComparison.Ordinal);
        Assert.Contains("Time zone is not filled in", result.Message, StringComparison.Ordinal);
        Assert.Equal(0, store.CreateCalls);
    }

    [Fact]
    public async Task QueuesConfirmationEmailWhenRegistrationSucceeds()
    {
        var store = new FakeUserAccountStore();
        var outbox = new FakeEmailOutbox();
        var handler = CreateHandler(store, outbox);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ada", result.Value!.Username);
        Assert.Equal(1, outbox.ConfirmationCount);
        Assert.Equal("ada@example.test", outbox.LastEmail);
    }

    private static RegisterAccountHandler CreateHandler(FakeUserAccountStore store, FakeEmailOutbox? outbox = null)
    {
        return new RegisterAccountHandler(
            store,
            outbox ?? new FakeEmailOutbox(),
            new FakeAvatarProcessor(),
            new FakeAvatarStorage());
    }

    private static RegisterAccountCommand ValidCommand(string? timeZoneId = null)
    {
        return new RegisterAccountCommand
        {
            Email = "ada@example.test",
            Username = "ada",
            Password = "Correct-Horse-Battery-1",
            FirstName = "Ada",
            LastName = "Lovelace",
            City = "Halifax",
            Region = "Nova Scotia",
            Country = "Canada",
            TimeZoneId = timeZoneId ?? "America/Halifax",
            DisplayNameMode = DisplayNameMode.Username,
        };
    }

    private sealed class FakeUserAccountStore : IUserAccountStore
    {
        public bool UsernameTaken { get; init; }

        public int CreateCalls { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> UsernameExistsAsync(string username, Guid? userIdToIgnore, CancellationToken cancellationToken)
        {
            return Task.FromResult(UsernameTaken);
        }

        public Task<CreateLocalAccountOutcome> CreateLocalAccountAsync(
            CreateLocalAccountRequest request,
            CancellationToken cancellationToken)
        {
            CreateCalls++;
            var account = new UserAccount
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Email = request.Email,
                Username = request.Username.Value,
                FirstName = request.Name.FirstName,
                MiddleInitial = request.Name.MiddleInitial,
                LastName = request.Name.LastName,
                City = request.Location.City,
                Region = request.Location.Region,
                Country = request.Location.Country,
                DisplayNameMode = request.DisplayNameMode,
                CreatedUtc = new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero),
                UpdatedUtc = new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero),
                ProfileRevision = 1,
                EmailConfirmed = false,
            };

            return Task.FromResult(new CreateLocalAccountOutcome
            {
                IsSuccess = true,
                Account = account,
                EmailConfirmationToken = "token",
            });
        }

        public Task<CreateLocalAccountOutcome> CreateExternalAccountAsync(
            CreateExternalAccountRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpdateProfileOutcome> UpdateProfileAsync(
            UpdateStoredProfileRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string?> ReplaceAvatarKeyAsync(Guid userId, string? avatarStorageKey, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ChangePasswordOutcome> ChangePasswordAsync(
            Guid userId,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeEmailOutbox : IEmailOutbox
    {
        public int ConfirmationCount { get; private set; }

        public string? LastEmail { get; private set; }

        public Task QueueEmailConfirmationAsync(string email, Guid userId, string token, CancellationToken cancellationToken)
        {
            ConfirmationCount++;
            LastEmail = email;
            return Task.CompletedTask;
        }

        public Task QueuePasswordResetAsync(string email, Guid userId, string token, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAvatarProcessor : IAvatarImageProcessor
    {
        public Task<ProcessedAvatarResult> ProcessAsync(
            Stream content,
            string contentType,
            long? length,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ProcessedAvatarResult
            {
                IsSuccess = false,
                ErrorCode = ErrorCodes.UploadInvalidType,
                Message = "unexpected",
            });
        }
    }

    private sealed class FakeAvatarStorage : IAvatarStorage
    {
        public Task<string> SaveAsync(ReadOnlyMemory<byte> content, string fileExtension, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<StoredAvatar?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredAvatar?>(null);
        }
    }
}
