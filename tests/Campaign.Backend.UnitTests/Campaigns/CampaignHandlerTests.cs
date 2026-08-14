using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Maps;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;

namespace Campaign.Backend.UnitTests.Campaigns;

public sealed class CampaignHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid NorthFactionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SouthFactionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid TownStructureId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
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
            Schedule = ValidSchedule(),
        };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(store.Added);
    }

    [Fact]
    public async Task GetReturnsNotFoundForNonMembers()
    {
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var handler = new GetCampaignHandler(store, new FakeClock());

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
        var handler = new UpdateCampaignHandler(store, new FakeClock(), new FakeSecretHasher(), new FakeAssetStorage());

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
                Schedule = ValidSchedule(),
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
        Assert.Contains("flags/north.png", maps.DeletedKeys);
        Assert.Contains("structures/town.png", maps.DeletedKeys);
        Assert.DoesNotContain("Town", maps.DeletedKeys);
        Assert.DoesNotContain("Castle", maps.DeletedKeys);
    }

    [Fact]
    public async Task UpdateDeletesCatalogFilesThatAreNoLongerReferenced()
    {
        var campaign = StoredCampaignFor(UserId);
        var store = new FakeCampaignStore { Existing = campaign };
        var assets = new FakeAssetStorage();
        var handler = new UpdateCampaignHandler(store, new FakeClock(), new FakeSecretHasher(), assets);

        var result = await handler.HandleAsync(
            new UpdateCampaignCommand
            {
                UserId = UserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Name = campaign.Name,
                Description = campaign.Description,
                PlayerCount = campaign.PlayerSlotCount,
                IsPrivate = false,
                CreatorIsParticipant = true,
                Factions =
                [
                    new FactionInput
                    {
                        Id = NorthFactionId,
                        Name = "North",
                        Color = "#2563EB",
                        ClearFlagImage = true,
                    },
                    new FactionInput
                    {
                        Id = SouthFactionId,
                        Name = "South",
                        Color = "#DC2626",
                    },
                ],
                Schedule = ValidSchedule(),
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("flags/north.png", assets.DeletedKeys);
        Assert.Contains("structures/town.png", assets.DeletedKeys);
        Assert.DoesNotContain("maps/old.png", assets.DeletedKeys);
        Assert.DoesNotContain("Town", assets.DeletedKeys);
        Assert.DoesNotContain("Castle", assets.DeletedKeys);
    }

    [Fact]
    public async Task UpdateDoesNotDeleteBuiltInStructureLogos()
    {
        var campaign = WithStructures(
            StoredCampaignFor(UserId),
            [
                new StoredStructureType
                {
                    Id = TownStructureId,
                    Name = "Town",
                    BuiltinSymbol = "Town",
                    ImageStorageKey = "Town",
                    Missions = [],
                },
                new StoredStructureType
                {
                    Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    Name = "Keep",
                    BuiltinSymbol = "Castle",
                    ImageStorageKey = "structures/keep.png",
                    Missions = [],
                },
            ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var assets = new FakeAssetStorage();
        var handler = new UpdateCampaignHandler(store, new FakeClock(), new FakeSecretHasher(), assets);

        var result = await handler.HandleAsync(
            new UpdateCampaignCommand
            {
                UserId = UserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Name = campaign.Name,
                Description = campaign.Description,
                PlayerCount = campaign.PlayerSlotCount,
                IsPrivate = false,
                CreatorIsParticipant = true,
                Factions =
                [
                    new FactionInput { Id = NorthFactionId, Name = "North", Color = "#2563EB" },
                    new FactionInput { Id = SouthFactionId, Name = "South", Color = "#DC2626" },
                ],
                Schedule = ValidSchedule(),
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("structures/keep.png", assets.DeletedKeys);
        Assert.DoesNotContain("Town", assets.DeletedKeys);
        Assert.DoesNotContain("Castle", assets.DeletedKeys);
    }

    [Fact]
    public async Task UploadMapDeletesThePreviousMapFile()
    {
        var campaign = StoredCampaignFor(UserId);
        var store = new FakeCampaignStore { Existing = campaign };
        var maps = new FakeMapStorage();
        var handler = new UploadCampaignMapHandler(store, new FakeMapProcessor(), maps, new FakeClock());

        var result = await handler.HandleAsync(
            new UploadCampaignMapCommand
            {
                UserId = UserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Content = Stream.Null,
                ContentType = "image/png",
                Length = 12,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("maps/new.png", store.Updated!.MapStorageKey);
        Assert.Contains("maps/old.png", maps.DeletedKeys);
        Assert.DoesNotContain("flags/north.png", maps.DeletedKeys);
    }

    [Fact]
    public async Task ListReturnsOnlyMappedViewerFields()
    {
        var store = new FakeCampaignStore();
        store.ForUser.Add(StoredCampaignFor(UserId));
        var handler = new ListCampaignsHandler(store, new FakeClock());

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
        var json = System.Text.Json.JsonSerializer.Serialize(CampaignMapper.ToDetail(campaign, UserId, Now));
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
            Schedule = ValidSchedule(),
        };
    }

    private static CampaignScheduleInput ValidSchedule()
    {
        return CampaignSetupRulesTests.WeekSchedule();
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
                new StoredFaction
                {
                    Id = NorthFactionId,
                    Name = "North",
                    Color = "#2563EB",
                    Subfactions = ["Riders"],
                    RequiresSubfaction = false,
                    FlagImageStorageKey = "flags/north.png",
                },
                new StoredFaction
                {
                    Id = SouthFactionId,
                    Name = "South",
                    Color = "#DC2626",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
            ],
            AllyGroups = [],
            Links =
            [
                new StoredCampaignLink { Id = Guid.NewGuid(), Label = "Notes", Url = "https://example.test/notes" },
            ],
            TimeZoneId = "UTC",
            StartsUtc = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero),
            EndsUtc = new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases =
            [
                new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new StoredRoundPhase { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
            TerrainTypes = [],
            StructureTypes =
            [
                new StoredStructureType
                {
                    Id = TownStructureId,
                    Name = "Town",
                    BuiltinSymbol = "Town",
                    ImageStorageKey = "structures/town.png",
                    Missions = [],
                },
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
            TimeZoneId = campaign.TimeZoneId,
            StartsUtc = campaign.StartsUtc,
            EndsUtc = campaign.EndsUtc,
            RoundCount = campaign.RoundCount,
            RoundLengthAmount = campaign.RoundLengthAmount,
            RoundLengthUnit = campaign.RoundLengthUnit,
            Phases = campaign.Phases,
            MapGraph = campaign.MapGraph,
            TerrainTypes = campaign.TerrainTypes,
            StructureTypes = campaign.StructureTypes,
        };
    }

    private static StoredCampaign WithStructures(StoredCampaign campaign, IReadOnlyList<StoredStructureType> structures)
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
            Memberships = campaign.Memberships,
            Factions = campaign.Factions,
            AllyGroups = campaign.AllyGroups,
            Links = campaign.Links,
            TimeZoneId = campaign.TimeZoneId,
            StartsUtc = campaign.StartsUtc,
            EndsUtc = campaign.EndsUtc,
            RoundCount = campaign.RoundCount,
            RoundLengthAmount = campaign.RoundLengthAmount,
            RoundLengthUnit = campaign.RoundLengthUnit,
            Phases = campaign.Phases,
            MapGraph = campaign.MapGraph,
            TerrainTypes = campaign.TerrainTypes,
            StructureTypes = structures,
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

        public Task<UpdateStoredCampaignOutcome> UpdateMapGraphAsync(
            Guid campaignId,
            StoredMapGraph graph,
            int expectedRevision,
            DateTimeOffset updatedUtc,
            CancellationToken cancellationToken)
        {
            if (Existing is null || Existing.Id != campaignId)
            {
                return Task.FromResult(new UpdateStoredCampaignOutcome
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.CampaignNotFound,
                    Message = "The campaign was not found.",
                });
            }

            if (Existing.Revision != expectedRevision)
            {
                return Task.FromResult(new UpdateStoredCampaignOutcome
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ConcurrencyConflict,
                    Message = "The campaign was changed by another request. Reload and try again.",
                });
            }

            Existing = new StoredCampaign
            {
                Id = Existing.Id,
                Name = Existing.Name,
                Description = Existing.Description,
                PlayerSlotCount = Existing.PlayerSlotCount,
                IsPrivate = Existing.IsPrivate,
                JoinPasswordHash = Existing.JoinPasswordHash,
                CreatorIsParticipant = Existing.CreatorIsParticipant,
                MapStorageKey = Existing.MapStorageKey,
                Revision = expectedRevision + 1,
                CreatedUtc = Existing.CreatedUtc,
                UpdatedUtc = updatedUtc,
                CreatedByUserId = Existing.CreatedByUserId,
                Memberships = Existing.Memberships,
                Factions = Existing.Factions,
                AllyGroups = Existing.AllyGroups,
                Links = Existing.Links,
                TimeZoneId = Existing.TimeZoneId,
                StartsUtc = Existing.StartsUtc,
                EndsUtc = Existing.EndsUtc,
                RoundCount = Existing.RoundCount,
                RoundLengthAmount = Existing.RoundLengthAmount,
                RoundLengthUnit = Existing.RoundLengthUnit,
                Phases = Existing.Phases,
                MapGraph = graph,
                TerrainTypes = Existing.TerrainTypes,
                StructureTypes = Existing.StructureTypes,
            };
            return Task.FromResult(new UpdateStoredCampaignOutcome { IsSuccess = true, Campaign = Existing });
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

    private sealed class FakeAssetStorage : ICampaignAssetStorage
    {
        public List<string> DeletedKeys { get; } = [];

        public Task<string> SaveAsync(
            string folder,
            ReadOnlyMemory<byte> content,
            string fileExtension,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult($"{folder}/new.png");
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(storageKey);
            return Task.CompletedTask;
        }

        public Task<StoredCampaignAsset?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredCampaignAsset?>(null);
        }
    }

    private sealed class FakeMapProcessor : ICampaignMapProcessor
    {
        public Task<ProcessedCampaignMapResult> ProcessAsync(
            Stream content,
            string contentType,
            long? length,
            CancellationToken cancellationToken,
            int maxDimension = ICampaignMapProcessor.MapMaxDimension)
        {
            return Task.FromResult(new ProcessedCampaignMapResult
            {
                IsSuccess = true,
                Content = [1, 2, 3],
                FileExtension = ".png",
            });
        }
    }
}
