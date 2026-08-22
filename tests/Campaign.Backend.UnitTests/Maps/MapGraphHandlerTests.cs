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
    private static readonly Guid NorthFactionId = Guid.Parse("aaaa1111-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SouthFactionId = Guid.Parse("bbbb2222-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid DaemonsFactionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10");
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
    public async Task SaveRejectsAfterLaunch()
    {
        var upcoming = StoredCampaignFor(UserId);
        var launched = new StoredCampaign
        {
            Id = upcoming.Id,
            Name = upcoming.Name,
            Description = upcoming.Description,
            PlayerSlotCount = upcoming.PlayerSlotCount,
            IsPrivate = upcoming.IsPrivate,
            IsPubliclyViewable = upcoming.IsPubliclyViewable,
            JoinPasswordHash = upcoming.JoinPasswordHash,
            CreatorIsParticipant = upcoming.CreatorIsParticipant,
            City = upcoming.City,
            Region = upcoming.Region,
            Country = upcoming.Country,
            MapStorageKey = upcoming.MapStorageKey,
            Revision = upcoming.Revision,
            CreatedUtc = upcoming.CreatedUtc,
            UpdatedUtc = upcoming.UpdatedUtc,
            CreatedByUserId = upcoming.CreatedByUserId,
            Memberships = upcoming.Memberships,
            Factions = upcoming.Factions,
            AllyGroups = upcoming.AllyGroups,
            Links = upcoming.Links,
            TimeZoneId = upcoming.TimeZoneId,
            StartsUtc = Now.AddHours(-1),
            EndsUtc = Now.AddDays(40),
            RoundCount = upcoming.RoundCount,
            RoundLengthAmount = upcoming.RoundLengthAmount,
            RoundLengthUnit = upcoming.RoundLengthUnit,
            Phases = upcoming.Phases,
            MapGraph = upcoming.MapGraph,
            TerrainTypes = upcoming.TerrainTypes,
            StructureTypes = upcoming.StructureTypes,
        };
        var store = new FakeCampaignStore { Existing = launched };
        var handler = new SaveCampaignMapGraphHandler(store, new FakeClock());

        var result = await handler.HandleAsync(
            new SaveCampaignMapGraphCommand
            {
                UserId = UserId,
                CampaignId = launched.Id,
                ExpectedRevision = 1,
                Territories = [],
                Adjacencies = [],
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignLocked, result.ErrorCode);
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
    public async Task SaveAcceptsDistinctRequiredSubfactionSpawns()
    {
        var khorneId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var nurgleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId, extraFactions: [DaemonsFaction()]) };
        var handler = new SaveCampaignMapGraphHandler(store, new FakeClock());

        var saved = await handler.HandleAsync(
            new SaveCampaignMapGraphCommand
            {
                UserId = UserId,
                CampaignId = store.Existing.Id,
                ExpectedRevision = 1,
                Territories =
                [
                    Territory(khorneId, 1, Square(0.1, 0.1, 0.3), "Khornehold", DaemonsFactionId, "Khorne"),
                    Territory(nurgleId, 2, Square(0.4, 0.1, 0.3), "Nurglefen", DaemonsFactionId, "Nurgle"),
                ],
                Adjacencies = [],
            },
            CancellationToken.None);

        Assert.True(saved.IsSuccess, string.Join("; ", saved.Errors.Select(error => error.Message)));
        Assert.Equal("Khorne", saved.Value!.Territories.Single(territory => territory.Id == khorneId).SpawnSubfaction);
        Assert.Equal("Nurgle", saved.Value.Territories.Single(territory => territory.Id == nurgleId).SpawnSubfaction);
    }

    [Fact]
    public async Task GetReturnsRequiredSubfactionSpawnsAndDoesNotDropDuplicateParentSpawns()
    {
        var khorneId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var nurgleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var leftId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");
        var rightId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4");
        var gods = StoredCampaignFor(
            UserId,
            extraFactions: [DaemonsFaction()],
            mapGraph: new StoredMapGraph
            {
                Territories =
                [
                    StoredTerritory(khorneId, 1, 0.1, DaemonsFactionId, "Khorne"),
                    StoredTerritory(nurgleId, 2, 0.4, DaemonsFactionId, "Nurgle"),
                ],
                Adjacencies = [],
            });
        var loadedGods = await new GetCampaignMapGraphHandler(new FakeCampaignStore { Existing = gods })
            .HandleAsync(gods.Id, UserId, CancellationToken.None);
        Assert.True(loadedGods.IsSuccess);
        Assert.Equal(2, loadedGods.Value!.Territories.Count);
        Assert.Contains(loadedGods.Value.Territories, territory => territory.SpawnSubfaction == "Khorne");
        Assert.Contains(loadedGods.Value.Territories, territory => territory.SpawnSubfaction == "Nurgle");

        var unlabeled = StoredCampaignFor(
            UserId,
            extraFactions: [DaemonsFaction()],
            mapGraph: new StoredMapGraph
            {
                Territories =
                [
                    StoredTerritory(leftId, 1, 0.1, DaemonsFactionId),
                    StoredTerritory(rightId, 2, 0.4, DaemonsFactionId),
                ],
                Adjacencies = [],
            });
        var loadedUnlabeled = await new GetCampaignMapGraphHandler(new FakeCampaignStore { Existing = unlabeled })
            .HandleAsync(unlabeled.Id, UserId, CancellationToken.None);
        Assert.True(loadedUnlabeled.IsSuccess);
        Assert.Equal(2, loadedUnlabeled.Value!.Territories.Count);
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
            IsPubliclyViewable = campaign.IsPubliclyViewable,
            JoinPasswordHash = campaign.JoinPasswordHash,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            City = campaign.City,
            Region = campaign.Region,
            Country = campaign.Country,
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

    private static TerritoryInput Territory(
        Guid id,
        int number,
        IReadOnlyList<MapPointInput> polygon,
        string? name = null,
        Guid? spawnFactionId = null,
        string? spawnSubfaction = null)
    {
        return new TerritoryInput
        {
            Id = id,
            DisplayNumber = number,
            Name = name,
            Polygon = polygon,
            TerrainTypeId = PlainsId,
            SpawnFactionId = spawnFactionId,
            SpawnSubfaction = spawnSubfaction,
        };
    }

    private static TerritoryDetail StoredTerritory(
        Guid id,
        int number,
        double x,
        Guid spawnFactionId,
        string? spawnSubfaction = null)
    {
        return new TerritoryDetail
        {
            Id = id,
            DisplayNumber = number,
            Name = null,
            Polygon =
            [
                new MapPointDetail { X = x, Y = 0.1 },
                new MapPointDetail { X = x + 0.3, Y = 0.1 },
                new MapPointDetail { X = x + 0.3, Y = 0.4 },
                new MapPointDetail { X = x, Y = 0.4 },
            ],
            TerrainTypeId = PlainsId,
            OwnerFactionId = spawnFactionId,
            OwnerSubfaction = spawnSubfaction,
            SpawnFactionId = spawnFactionId,
            SpawnSubfaction = spawnSubfaction,
        };
    }

    private static StoredFaction DaemonsFaction()
    {
        return new StoredFaction
        {
            Id = DaemonsFactionId,
            Name = "Daemons of Chaos",
            Color = "#AD1457",
            Subfactions = ["Khorne", "Nurgle", "Slaanesh", "Tzeentch"],
            RequiresSubfaction = true,
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

    private static StoredCampaign StoredCampaignFor(
        Guid userId,
        StoredMapGraph? mapGraph = null,
        IReadOnlyList<StoredFaction>? extraFactions = null)
    {
        return new StoredCampaign
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Name = "Border War",
            PlayerSlotCount = 8,
            IsPrivate = false,
            IsPubliclyViewable = true,
            CreatorIsParticipant = true,
            Revision = 1,
            CreatedUtc = Now,
            UpdatedUtc = Now,
            CreatedByUserId = userId,
            Memberships = [new StoredCampaignMembership { UserId = userId, IsGameMaster = true, IsPlayer = true }],
            Factions =
            [
                new StoredFaction { Id = NorthFactionId, Name = "North", Color = "#2563EB", Subfactions = [], RequiresSubfaction = false },
                new StoredFaction { Id = SouthFactionId, Name = "South", Color = "#DC2626", Subfactions = [], RequiresSubfaction = false },
                .. extraFactions ?? [],
            ],
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = Now.AddDays(7),
            EndsUtc = Now.AddDays(63),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases = [new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" }],
            MapGraph = mapGraph,
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

        public Task<IReadOnlyList<StoredCampaign>> ListDiscoverableAsync(
            Guid userId,
            bool isAdministrator,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken)
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

        public Task<bool> IsStorageKeyInUseAsync(
            string storageKey,
            Guid? excludingCampaignId,
            CancellationToken cancellationToken)
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
                IsPubliclyViewable = Existing.IsPubliclyViewable,
                JoinPasswordHash = Existing.JoinPasswordHash,
                CreatorIsParticipant = Existing.CreatorIsParticipant,
                City = Existing.City,
                Region = Existing.Region,
                Country = Existing.Country,
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
                ItemObjectiveTypes = Existing.ItemObjectiveTypes,
                PublicObjectiveTypes = Existing.PublicObjectiveTypes,
                BattleScoring = Existing.BattleScoring,
                RankingObjectivePoints = Existing.RankingObjectivePoints,
                PlayState = Existing.PlayState,
            };
            return Task.FromResult(new UpdateStoredCampaignOutcome { IsSuccess = true, Campaign = Existing });
        }

        public Task<UpdateStoredCampaignOutcome> UpdatePlayStateAsync(
            Guid campaignId,
            Campaign.Domain.Play.CampaignPlayState playState,
            StoredMapGraph? mapGraph,
            DateTimeOffset endsUtc,
            int roundCount,
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
                IsPubliclyViewable = Existing.IsPubliclyViewable,
                JoinPasswordHash = Existing.JoinPasswordHash,
                CreatorIsParticipant = Existing.CreatorIsParticipant,
                City = Existing.City,
                Region = Existing.Region,
                Country = Existing.Country,
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
                EndsUtc = endsUtc,
                RoundCount = roundCount,
                RoundLengthAmount = Existing.RoundLengthAmount,
                RoundLengthUnit = Existing.RoundLengthUnit,
                Phases = Existing.Phases,
                MapGraph = mapGraph ?? Existing.MapGraph,
                TerrainTypes = Existing.TerrainTypes,
                StructureTypes = Existing.StructureTypes,
                ItemObjectiveTypes = Existing.ItemObjectiveTypes,
                PublicObjectiveTypes = Existing.PublicObjectiveTypes,
                BattleScoring = Existing.BattleScoring,
                RankingObjectivePoints = Existing.RankingObjectivePoints,
                PlayState = playState,
            };
            return Task.FromResult(new UpdateStoredCampaignOutcome { IsSuccess = true, Campaign = Existing });
        }
    }
}
