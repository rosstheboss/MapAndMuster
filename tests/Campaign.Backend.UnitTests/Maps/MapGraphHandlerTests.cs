using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Maps;
using Campaign.Application.Ports;
using Campaign.Domain.Maps;

namespace Campaign.Backend.UnitTests.Maps;

public sealed class MapGraphHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PlainsId = Guid.Parse("cccccc01-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveRejectsOverlappingTerritories()
    {
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var handler = new SaveCampaignMapGraphHandler(store, new FakeClock());

        var result = await handler.HandleAsync(
            new SaveCampaignMapGraphCommand
            {
                UserId = UserId,
                CampaignId = store.Existing.Id,
                ExpectedRevision = 1,
                Territories =
                [
                    Territory(Guid.NewGuid(), 1, Square(0.1, 0.1, 0.4)),
                    Territory(Guid.NewGuid(), 2, Square(0.3, 0.1, 0.4)),
                ],
                Adjacencies = [],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "territories.overlap");
    }

    [Fact]
    public async Task SavePersistsNonOverlappingGraphAndGetReturnsIt()
    {
        var leftId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var rightId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var save = new SaveCampaignMapGraphHandler(store, new FakeClock());

        var saved = await save.HandleAsync(
            new SaveCampaignMapGraphCommand
            {
                UserId = UserId,
                CampaignId = store.Existing.Id,
                ExpectedRevision = 1,
                Territories =
                [
                    Territory(leftId, 1, Square(0.1, 0.1, 0.3), "North"),
                    Territory(rightId, 2, Square(0.4, 0.1, 0.3)),
                ],
                Adjacencies =
                [
                    new AdjacencyInput
                    {
                        TerritoryAId = leftId,
                        TerritoryBId = rightId,
                        Origin = "Manual",
                        MarkerX = 0.4,
                        MarkerY = 0.25,
                    },
                ],
            },
            CancellationToken.None);

        Assert.True(saved.IsSuccess);
        Assert.NotNull(saved.Value);
        Assert.Equal(2, saved.Value.Territories.Count);
        Assert.Equal("North", saved.Value.Territories[0].Name);
        Assert.Equal("Manual", saved.Value.Adjacencies[0].Origin);
        Assert.Equal(2, store.Existing.Revision);

        var loaded = await new GetCampaignMapGraphHandler(store).HandleAsync(store.Existing.Id, UserId, CancellationToken.None);
        Assert.True(loaded.IsSuccess);
        Assert.Equal("North", loaded.Value!.Territories[0].Name);
        Assert.True(loaded.Value.CanManage);
    }

    [Fact]
    public async Task SaveRejectsParticipantsWhoAreNotManagers()
    {
        var campaign = StoredCampaignFor(UserId);
        campaign = new StoredCampaign
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
            Memberships =
            [
                new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = false },
                new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
            ],
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
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new SaveCampaignMapGraphHandler(store, new FakeClock());

        var result = await handler.HandleAsync(
            new SaveCampaignMapGraphCommand
            {
                UserId = OtherUserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Territories = [],
                Adjacencies = [],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
    }

    private static TerritoryInput Territory(Guid id, int number, IReadOnlyList<MapPointInput> polygon, string? name = null)
    {
        return new TerritoryInput
        {
            Id = id,
            DisplayNumber = number,
            Name = name,
            Polygon = polygon,
            TerrainTypeId = PlainsId,
        };
    }

    private static IReadOnlyList<MapPointInput> Square(double x, double y, double size)
    {
        return
        [
            new MapPointInput { X = x, Y = y },
            new MapPointInput { X = x + size, Y = y },
            new MapPointInput { X = x + size, Y = y + size },
            new MapPointInput { X = x, Y = y + size },
        ];
    }

    private static StoredCampaign StoredCampaignFor(Guid userId)
    {
        return new StoredCampaign
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Name = "Border War",
            PlayerSlotCount = 8,
            IsPrivate = false,
            CreatorIsParticipant = true,
            Revision = 1,
            CreatedUtc = Now,
            UpdatedUtc = Now,
            CreatedByUserId = userId,
            Memberships = [new StoredCampaignMembership { UserId = userId, IsGameMaster = true, IsPlayer = true }],
            Factions =
            [
                new StoredFaction { Id = Guid.NewGuid(), Name = "North", Color = "#2563EB", Subfactions = [], RequiresSubfaction = false },
                new StoredFaction { Id = Guid.NewGuid(), Name = "South", Color = "#DC2626", Subfactions = [], RequiresSubfaction = false },
            ],
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = Now,
            EndsUtc = Now.AddDays(56),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases = [new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" }],
            TerrainTypes =
            [
                new StoredTerrainType
                {
                    Id = PlainsId,
                    Name = "Plains",
                    Color = "#7CB342",
                    Missions = [new StoredMission { Id = Guid.Parse("eeeeee01-eeee-eeee-eeee-eeeeeeeeeeee"), Name = "Plains control" }],
                },
            ],
            StructureTypes = [],
        };
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeCampaignStore : ICampaignStore
    {
        public StoredCampaign? Existing { get; set; }

        public Task<StoredCampaign> AddAsync(StoredCampaign campaign, CancellationToken cancellationToken)
        {
            return Task.FromResult(campaign);
        }

        public Task<StoredCampaign?> FindByIdAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Existing is not null && Existing.Id == campaignId ? Existing : null);
        }

        public Task<IReadOnlyList<StoredCampaign>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<StoredCampaign>>([]);
        }

        public Task<UpdateStoredCampaignOutcome> UpdateAsync(
            StoredCampaign campaign,
            int expectedRevision,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new UpdateStoredCampaignOutcome { IsSuccess = true, Campaign = campaign });
        }

        public Task<bool> DeleteAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<UpdateStoredCampaignOutcome> UpdateMapGraphAsync(
            Guid campaignId,
            StoredMapGraph graph,
            int expectedRevision,
            DateTimeOffset updatedUtc,
            CancellationToken cancellationToken)
        {
            if (Existing is null || Existing.Revision != expectedRevision)
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
}
