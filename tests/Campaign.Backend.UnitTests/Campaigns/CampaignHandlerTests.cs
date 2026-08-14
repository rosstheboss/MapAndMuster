using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;

namespace Campaign.Backend.UnitTests.Campaigns;

public sealed class CampaignHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePersistsManagerMembershipAndHashesJoinPassword()
    {
        var store = new FakeCampaignStore();
        var secrets = new FakeSecretHasher();
        var handler = new CreateCampaignHandler(store, new FakeClock(), secrets);

        var result = await handler.HandleAsync(ValidCreateCommand(isPrivate: true, joinPassword: "join-secret"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanManage);
        Assert.True(result.Value.IsParticipant);
        Assert.Equal(1, result.Value.OccupiedPlayerSlots);
        Assert.Null(GetHashProperty(result.Value));
        Assert.Equal("hash:join-secret", store.Added!.JoinPasswordHash);
        Assert.DoesNotContain("join-secret", result.Value.Name, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRejectsInvalidSetupBeforeSaving()
    {
        var store = new FakeCampaignStore();
        var handler = new CreateCampaignHandler(store, new FakeClock(), new FakeSecretHasher());
        var command = ValidCreateCommand();
        command = new CreateCampaignCommand
        {
            UserId = command.UserId,
            Name = "x",
            PlayerCount = 8,
            IsPrivate = false,
            CreatorIsParticipant = true,
            Factions = command.Factions,
        };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(store.Added);
    }

    [Fact]
    public async Task GetReturnsNotFoundForNonMembers()
    {
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var handler = new GetCampaignHandler(store);

        var result = await handler.HandleAsync(store.Existing.Id, OtherUserId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task UpdateRejectsParticipantsWhoAreNotManagers()
    {
        var campaign = StoredCampaignFor(UserId);
        campaign = WithMemberships(campaign,
        [
            new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = false },
            new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
        ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new UpdateCampaignHandler(store, new FakeClock(), new FakeSecretHasher());

        var result = await handler.HandleAsync(
            new UpdateCampaignCommand
            {
                UserId = OtherUserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Name = "Renamed",
                PlayerCount = 8,
                IsPrivate = false,
                CreatorIsParticipant = false,
                Factions =
                [
                    new FactionInput { Name = "North" },
                    new FactionInput { Name = "South" },
                ],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
        Assert.Null(store.Updated);
    }

    [Fact]
    public async Task DeleteRemovesCampaignForManagersOnly()
    {
        var campaign = StoredCampaignFor(UserId);
        campaign = WithMemberships(campaign,
        [
            new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = true },
            new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
        ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var maps = new FakeMapStorage();
        var handler = new DeleteCampaignHandler(store, maps);

        var forbidden = await handler.HandleAsync(campaign.Id, OtherUserId, CancellationToken.None);
        Assert.False(forbidden.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, forbidden.ErrorCode);
        Assert.False(store.Deleted);

        var deleted = await handler.HandleAsync(campaign.Id, UserId, CancellationToken.None);
        Assert.True(deleted.IsSuccess);
        Assert.True(store.Deleted);
        Assert.Contains("maps/old.png", maps.DeletedKeys);
    }

    [Fact]
    public async Task ListReturnsOnlyMappedViewerFields()
    {
        var store = new FakeCampaignStore();
        store.ForUser.Add(StoredCampaignFor(UserId));
        var handler = new ListCampaignsHandler(store);

        var result = await handler.HandleAsync(UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!);
        Assert.Equal("Border War", item.Name);
        Assert.True(item.CanManage);
        Assert.True(item.IsParticipant);
    }

    [Fact]
    public void DetailMappingOmitsJoinPasswordHash()
    {
        var campaign = StoredCampaignFor(UserId);
        var json = System.Text.Json.JsonSerializer.Serialize(CampaignMapper.ToDetail(campaign, UserId));
        Assert.DoesNotContain("JoinPassword", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash:join-secret", json, StringComparison.Ordinal);
    }

    private static CreateCampaignCommand ValidCreateCommand(bool isPrivate = false, string? joinPassword = null)
    {
        return new CreateCampaignCommand
        {
            UserId = UserId,
            Name = "Border War",
            Description = "A contested frontier.",
            PlayerCount = 8,
            IsPrivate = isPrivate,
            JoinPassword = joinPassword,
            CreatorIsParticipant = true,
            Factions =
            [
                new FactionInput { Name = "North", Subfactions = ["Riders"] },
                new FactionInput { Name = "South" },
            ],
            AllyGroups = null,
            Links = [new CampaignLinkInput { Label = "Notes", Url = "https://example.test/notes" }],
        };
    }

    private static StoredCampaign StoredCampaignFor(Guid userId)
    {
        return new StoredCampaign
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Border War",
            Description = "A contested frontier.",
            PlayerSlotCount = 8,
            IsPrivate = true,
            JoinPasswordHash = "hash:join-secret",
            CreatorIsParticipant = true,
            MapStorageKey = "maps/old.png",
            Revision = 1,
            CreatedUtc = Now,
            UpdatedUtc = Now,
            CreatedByUserId = userId,
            Memberships =
            [
                new StoredCampaignMembership { UserId = userId, IsGameMaster = true, IsPlayer = true },
            ],
            Factions =
            [
                new StoredFaction { Id = Guid.NewGuid(), Name = "North", Subfactions = ["Riders"] },
                new StoredFaction { Id = Guid.NewGuid(), Name = "South", Subfactions = [] },
            ],
            AllyGroups = [],
            Links =
            [
                new StoredCampaignLink { Id = Guid.NewGuid(), Label = "Notes", Url = "https://example.test/notes" },
            ],
        };
    }

    private static StoredCampaign WithMemberships(StoredCampaign campaign, IReadOnlyList<StoredCampaignMembership> memberships)
    {
        return new StoredCampaign
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            IsPrivate = campaign.IsPrivate,
            JoinPasswordHash = campaign.JoinPasswordHash,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            MapStorageKey = campaign.MapStorageKey,
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            CreatedByUserId = campaign.CreatedByUserId,
            Memberships = memberships,
            Factions = campaign.Factions,
            AllyGroups = campaign.AllyGroups,
            Links = campaign.Links,
        };
    }

    private static object? GetHashProperty(CampaignDetail detail)
    {
        return detail.GetType().GetProperty("JoinPasswordHash")?.GetValue(detail);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeSecretHasher : ISecretHasher
    {
        public string Hash(string secret)
        {
            return $"hash:{secret}";
        }

        public bool Verify(string hash, string secret)
        {
            return hash == $"hash:{secret}";
        }
    }

    private sealed class FakeCampaignStore : ICampaignStore
    {
        public StoredCampaign? Added { get; private set; }

        public StoredCampaign? Existing { get; set; }

        public StoredCampaign? Updated { get; private set; }

        public bool Deleted { get; private set; }

        public List<StoredCampaign> ForUser { get; } = [];

        public Task<StoredCampaign> AddAsync(StoredCampaign campaign, CancellationToken cancellationToken)
        {
            Added = campaign;
            return Task.FromResult(campaign);
        }

        public Task<StoredCampaign?> FindByIdAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Existing is not null && Existing.Id == campaignId ? Existing : null);
        }

        public Task<IReadOnlyList<StoredCampaign>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<StoredCampaign>>(ForUser);
        }

        public Task<UpdateStoredCampaignOutcome> UpdateAsync(
            StoredCampaign campaign,
            int expectedRevision,
            CancellationToken cancellationToken)
        {
            Updated = campaign;
            return Task.FromResult(new UpdateStoredCampaignOutcome { IsSuccess = true, Campaign = campaign });
        }

        public Task<bool> DeleteAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            Deleted = Existing is not null && Existing.Id == campaignId;
            return Task.FromResult(Deleted);
        }
    }

    private sealed class FakeMapStorage : ICampaignMapStorage
    {
        public List<string> DeletedKeys { get; } = [];

        public Task<string> SaveAsync(ReadOnlyMemory<byte> content, string fileExtension, CancellationToken cancellationToken)
        {
            return Task.FromResult("maps/new.png");
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(storageKey);
            return Task.CompletedTask;
        }

        public Task<StoredCampaignMap?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredCampaignMap?>(null);
        }
    }
}
