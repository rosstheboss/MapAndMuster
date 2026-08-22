using MapAndMuster.Application.Common;
using MapAndMuster.Application.Identity;
using MapAndMuster.Application.Ports;

namespace MapAndMuster.Backend.UnitTests.Identity;

public sealed class ChangePasswordHandlerTests
{
    [Fact]
    public async Task RejectsAWeakNewPasswordWithoutCallingTheStore()
    {
        var store = new FakePasswordStore();
        var handler = new ChangePasswordHandler(store);

        var result = await handler.HandleAsync(
            new ChangePasswordCommand
            {
                UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CurrentPassword = "Correct-Horse-1",
                NewPassword = "short",
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.PasswordInvalid, result.ErrorCode);
        Assert.Contains("too short", result.Message, StringComparison.Ordinal);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task MapsAnIncorrectCurrentPassword()
    {
        var store = new FakePasswordStore { CurrentInvalid = true };
        var handler = new ChangePasswordHandler(store);

        var result = await handler.HandleAsync(
            new ChangePasswordCommand
            {
                UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CurrentPassword = "Wrong-Password-1",
                NewPassword = "Correct-Horse-2!",
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CurrentPasswordInvalid, result.ErrorCode);
        Assert.Contains("Current password is incorrect", result.Message, StringComparison.Ordinal);
    }

    private sealed class FakePasswordStore : IUserAccountStore
    {
        public bool CurrentInvalid { get; init; }

        public int Calls { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> UsernameExistsAsync(string username, Guid? userIdToIgnore, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CreateLocalAccountOutcome> CreateLocalAccountAsync(
            CreateLocalAccountRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
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
            Calls++;
            return Task.FromResult(new ChangePasswordOutcome
            {
                IsSuccess = !CurrentInvalid,
                ErrorCode = CurrentInvalid ? ErrorCodes.CurrentPasswordInvalid : null,
                Message = CurrentInvalid ? "Current password is incorrect." : null,
            });
        }
    }
}
