using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Identity;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Identity;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Identity;

public sealed class GetPublicProfileHandlerTests
{
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ViewerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListsPublicAndSharedCampaignsAndOmitsHiddenPrivateOnes()
    {
        var publicCampaign = CampaignNamed("Open War", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), publiclyViewable: true, ownerOnly: true);
        var hiddenCampaign = CampaignNamed("Secret War", Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), publiclyViewable: false, ownerOnly: true);
        var sharedCampaign = CampaignNamed("Shared War", Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), publiclyViewable: false, ownerOnly: false);
        var handler = CreateHandler([publicCampaign, hiddenCampaign, sharedCampaign]);

        var stranger = await handler.HandleAsync("ada", ViewerId, isAdministrator: false, CancellationToken.None);
        Assert.True(stranger.IsSuccess);
        Assert.NotNull(stranger.Value);
        Assert.Equal(["Open War", "Shared War"], [.. stranger.Value.Campaigns.Select(campaign => campaign.Name)]);
        Assert.DoesNotContain(stranger.Value.Campaigns, campaign => campaign.Name == "Secret War");
        Assert.DoesNotContain("join-secret", System.Text.Json.JsonSerializer.Serialize(stranger.Value), StringComparison.Ordinal);

        var anonymous = await handler.HandleAsync("ada", viewerUserId: null, isAdministrator: false, CancellationToken.None);
        Assert.Equal(["Open War"], [.. anonymous.Value!.Campaigns.Select(campaign => campaign.Name)]);

        var admin = await handler.HandleAsync("ada", ViewerId, isAdministrator: true, CancellationToken.None);
        Assert.Equal(3, admin.Value!.Campaigns.Count);

        var owner = await handler.HandleAsync("ada", OwnerId, isAdministrator: false, CancellationToken.None);
        Assert.Equal(3, owner.Value!.Campaigns.Count);
    }

    [Fact]
    public void TestAccountPublicDisplayNameUsesTheAccountNumber()
    {
        var profile = ProfileMapper.ToPublic(new UserAccount
        {
            Id = OwnerId,
            Email = "test1@users.invalid",
            Username = "test1",
            FirstName = "Test",
            LastName = "Account",
            City = "Testville",
            Country = "Testland",
            DisplayNameMode = DisplayNameMode.Username,
            CreatedUtc = Now,
            UpdatedUtc = Now,
            ProfileRevision = 1,
            EmailConfirmed = true,
            IsTestAccount = true,
            TestAccountNumber = 1,
        });

        Assert.Equal("Test 1", profile.DisplayName);
        Assert.Equal("test1", profile.Username);
    }

    [Fact]
    public async Task ReturnsNotFoundForUnknownUsername()
    {
        var handler = CreateHandler([]);

        var result = await handler.HandleAsync("missing", ViewerId, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.ProfileNotFound, result.ErrorCode);
    }

    private static GetPublicProfileHandler CreateHandler(IReadOnlyList<StoredCampaign> campaigns)
    {
        return new GetPublicProfileHandler(new FakeAccounts(), new FakeCampaigns { ForUser = [.. campaigns] }, new FakeClock());
    }

    private static StoredCampaign CampaignNamed(string name, Guid id, bool publiclyViewable, bool ownerOnly)
    {
        var memberships = new List<StoredCampaignMembership>
        {
            new() { UserId = OwnerId, IsGameMaster = true, IsPlayer = true },
        };
        if (!ownerOnly)
        {
            memberships.Add(new StoredCampaignMembership { UserId = ViewerId, IsGameMaster = false, IsPlayer = true });
        }

        return new StoredCampaign
        {
            Id = id,
            Name = name,
            PlayerSlotCount = 8,
            IsPrivate = !publiclyViewable,
            IsPubliclyViewable = publiclyViewable,
            JoinPasswordHash = publiclyViewable ? null : "hash:join-secret",
            CreatorIsParticipant = true,
            Revision = 1,
            CreatedUtc = Now,
            UpdatedUtc = Now,
            CreatedByUserId = OwnerId,
            Memberships = memberships,
            Factions =
            [
                new StoredFaction
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Name = "North",
                    Color = "#2563EB",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
                new StoredFaction
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Name = "South",
                    Color = "#DC2626",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
            ],
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = Now.AddDays(14),
            EndsUtc = Now.AddDays(70),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases = [new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" }],
            TerrainTypes = [],
            StructureTypes = [],
        };
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeAccounts : IUserAccountStore
    {
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> UsernameExistsAsync(string username, Guid? userIdToIgnore, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<CreateLocalAccountOutcome> CreateLocalAccountAsync(CreateLocalAccountRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CreateLocalAccountOutcome> CreateExternalAccountAsync(CreateExternalAccountRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<UserAccount?>(Account(userId == OwnerId ? "ada" : "northplayer", userId));
        }

        public Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            return Task.FromResult(username == "ada" ? Account("ada", OwnerId) : null);
        }

        public Task<UpdateProfileOutcome> UpdateProfileAsync(UpdateStoredProfileRequest request, CancellationToken cancellationToken)
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

        public Task<string?> ReplaceAvatarKeyAsync(Guid userId, string? avatarStorageKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(null);
        }

        private static UserAccount Account(string username, Guid id)
        {
            return new UserAccount
            {
                Id = id,
                Email = $"{username}@example.test",
                Username = username,
                FirstName = "Ada",
                LastName = "Lovelace",
                City = "Halifax",
                Country = "Canada",
                DisplayNameMode = DisplayNameMode.Username,
                CreatedUtc = Now,
                UpdatedUtc = Now,
                ProfileRevision = 1,
                EmailConfirmed = true,
            };
        }
    }

    private sealed class FakeCampaigns : ICampaignStore
    {
        public List<StoredCampaign> ForUser { get; init; } = [];

        public Task<StoredCampaign> AddAsync(StoredCampaign campaign, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StoredCampaign?> FindByIdAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ForUser.FirstOrDefault(campaign => campaign.Id == campaignId));
        }

        public Task<IReadOnlyList<StoredCampaign>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<StoredCampaign>>(
                [.. ForUser.Where(campaign => campaign.Memberships.Any(member => member.UserId == userId))]);
        }

        public Task<IReadOnlyList<StoredCampaign>> ListDiscoverableAsync(
            Guid userId,
            bool isAdministrator,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpdateStoredCampaignOutcome> UpdateAsync(
            StoredCampaign campaign,
            int expectedRevision,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> IsStorageKeyInUseAsync(
            string storageKey,
            Guid? excludingCampaignId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpdateStoredCampaignOutcome> UpdateMapGraphAsync(
            Guid campaignId,
            StoredMapGraph graph,
            int expectedRevision,
            DateTimeOffset updatedUtc,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpdateStoredCampaignOutcome> UpdatePlayStateAsync(
            Guid campaignId,
            CampaignPlayState playState,
            StoredMapGraph? mapGraph,
            DateTimeOffset endsUtc,
            int roundCount,
            int expectedRevision,
            DateTimeOffset updatedUtc,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
