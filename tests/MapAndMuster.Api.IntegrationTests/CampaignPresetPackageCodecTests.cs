using System.IO.Compression;
using System.Text;
using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Maps;
using MapAndMuster.Infrastructure.Campaigns;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class CampaignPresetPackageCodecTests
{
    /// <summary>Writes a package from an in-memory file table, mirroring what storage supplies.</summary>
    private static MemoryStream Pack(
        CampaignPresetPackageCodec codec,
        StoredCampaign campaign,
        IReadOnlyDictionary<string, byte[]> files)
    {
        var output = new MemoryStream();
        codec.WriteAsync(
                output,
                campaign,
                [.. files.Keys],
                (key, _) => Task.FromResult<Stream?>(new MemoryStream(files[key], writable: false)),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        output.Position = 0;
        return output;
    }

    [Fact]
    public void ReadRejectsAnEmptyArchive()
    {
        var codec = new CampaignPresetPackageCodec();
        var result = codec.Read(new MemoryStream());
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignPresetPackageInvalid, result.ErrorCode);
    }

    [Fact]
    public void ReadRejectsAnArchiveTooShortToHoldADirectory()
    {
        var codec = new CampaignPresetPackageCodec();
        var result = codec.Read(new MemoryStream([1, 2, 3], writable: false));
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignPresetPackageInvalid, result.ErrorCode);
    }

    [Fact]
    public void ReadRejectsAZipWithoutAManifest()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("catalog.json");
            using var stream = entry.Open();
            stream.Write(Encoding.UTF8.GetBytes("{}"));
        }

        var codec = new CampaignPresetPackageCodec();
        var result = codec.Read(output);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignPresetPackageInvalid, result.ErrorCode);
    }

    [Fact]
    public void ReadRejectsZipSlipPaths()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("assets/maps/../../secret.txt");
            using var stream = entry.Open();
            stream.Write(Encoding.UTF8.GetBytes("nope"));
        }

        var codec = new CampaignPresetPackageCodec();
        var result = codec.Read(output);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignPresetPackageInvalid, result.ErrorCode);
    }

    [Fact]
    public void RoundTripKeepsFactionFlagsAndStructureLogos()
    {
        var factionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var structureId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var campaign = new StoredCampaign
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Frontier War",
            PlayerSlotCount = 8,
            IsPrivate = false,
            IsPubliclyViewable = true,
            CreatorIsParticipant = true,
            Revision = 1,
            CreatedUtc = DateTimeOffset.UnixEpoch,
            UpdatedUtc = DateTimeOffset.UnixEpoch,
            CreatedByUserId = Guid.Empty,
            Memberships = [],
            Factions =
            [
                new StoredFaction
                {
                    Id = factionId,
                    Name = "North",
                    Color = "#2563EB",
                    Subfactions = [],
                    RequiresSubfaction = false,
                    FlagImageStorageKey = "flags/north.png",
                    TintFlagImage = true,
                },
                new StoredFaction
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "South",
                    Color = "#DC2626",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
            ],
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = DateTimeOffset.UnixEpoch,
            EndsUtc = DateTimeOffset.UnixEpoch,
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases =
            [
                new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new StoredRoundPhase { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
            TerrainTypes = [],
            StructureTypes =
            [
                new StoredStructureType
                {
                    Id = structureId,
                    Name = "Town",
                    BuiltinSymbol = "Town",
                    ImageStorageKey = "structures/town.png",
                    IsBuildable = true,
                    IsPillageable = true,
                    IsDestructible = true,
                    Missions = [],
                    CampaignPoints = 0,
                    SupplyPoints = 0,
                    PillageSupplyPoints = 0,
                    DestroySupplyPoints = 0,
                },
            ],
            BattleScoring = MapAndMuster.Domain.Campaigns.BattleScoringSetup.Default,
        };
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["flags/north.png"] = [1, 2, 3],
            ["structures/town.png"] = [4, 5, 6],
        };

        var codec = new CampaignPresetPackageCodec();
        var packed = Pack(codec, campaign, files);
        var unpacked = codec.Read(packed);

        Assert.True(unpacked.IsSuccess, unpacked.Message);
        Assert.NotNull(unpacked.Value);
        Assert.Equal([1, 2, 3], unpacked.Value.Files["flags/north.png"]);
        Assert.Equal([4, 5, 6], unpacked.Value.Files["structures/town.png"]);
        Assert.Equal(
            "flags/north.png",
            unpacked.Value.Campaign.Factions.Single(faction => faction.Name == "North").FlagImageStorageKey);
        Assert.Equal(
            "structures/town.png",
            unpacked.Value.Campaign.StructureTypes.Single(type => type.Name == "Town").ImageStorageKey);
        var town = unpacked.Value.Campaign.StructureTypes.Single(type => type.Name == "Town");
        Assert.Equal(0, town.SupplyPoints);
        Assert.Equal(0, town.PillageSupplyPoints);
        Assert.Equal(0, town.DestroySupplyPoints);
    }

    [Fact]
    public void RoundTripKeepsMapImageAndOverlayGraph()
    {
        var terrainId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var campaign = new StoredCampaign
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Frontier War",
            PlayerSlotCount = 8,
            IsPrivate = false,
            IsPubliclyViewable = true,
            IsFreeForAll = true,
            RandomSpawnLocations = true,
            GameSystem = "Warhammer: The Old World",
            CreatorIsParticipant = true,
            MapStorageKey = "maps/board.png",
            Revision = 1,
            CreatedUtc = DateTimeOffset.UnixEpoch,
            UpdatedUtc = DateTimeOffset.UnixEpoch,
            CreatedByUserId = Guid.Empty,
            Memberships = [],
            Factions = [],
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = DateTimeOffset.UnixEpoch,
            EndsUtc = DateTimeOffset.UnixEpoch,
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases =
            [
                new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new StoredRoundPhase { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
            MapGraph = new StoredMapGraph
            {
                Territories =
                [
                    new TerritoryDetail
                    {
                        Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        DisplayNumber = 1,
                        Name = "Northmarch",
                        Polygon =
                        [
                            new MapPointDetail { X = 0.1, Y = 0.1 },
                            new MapPointDetail { X = 0.3, Y = 0.1 },
                            new MapPointDetail { X = 0.3, Y = 0.3 },
                            new MapPointDetail { X = 0.1, Y = 0.3 },
                        ],
                        TerrainTypeId = terrainId,
                    },
                ],
                Adjacencies = [],
            },
            TerrainTypes =
            [
                new StoredTerrainType
                {
                    Id = terrainId,
                    Name = "Plains",
                    Color = "#7CB342",
                    Missions = [],
                },
            ],
            StructureTypes = [],
            BattleScoring = MapAndMuster.Domain.Campaigns.BattleScoringSetup.Default,
        };
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["maps/board.png"] = [9, 8, 7],
        };

        var codec = new CampaignPresetPackageCodec();
        var packed = Pack(codec, campaign, files);
        using (var zip = new ZipArchive(packed, ZipArchiveMode.Read, leaveOpen: true))
        {
            Assert.NotNull(zip.GetEntry("overlay.json"));
            Assert.NotNull(zip.GetEntry("overlay.svg"));
            Assert.NotNull(zip.GetEntry("map.png"));
        }

        var unpacked = codec.Read(packed);
        Assert.True(unpacked.IsSuccess, unpacked.Message);
        Assert.NotNull(unpacked.Value);
        Assert.Equal("Northmarch", unpacked.Value.Campaign.MapGraph?.Territories[0].Name);
        Assert.Equal([9, 8, 7], unpacked.Value.Files[unpacked.Value.Campaign.MapStorageKey!]);
        Assert.True(unpacked.Value.Campaign.IsFreeForAll);
        Assert.True(unpacked.Value.Campaign.RandomSpawnLocations);
        Assert.Equal("Warhammer: The Old World", unpacked.Value.Campaign.GameSystem);
    }

    [Fact]
    public void LegacyPresetPackageKeepsLogosWhenNewSettingsAreAbsent()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipText(
                zip,
                "manifest.json",
                """
                {"format":"mapandmuster.campaign-preset","version":1,"name":"The Hunt in Estalia"}
                """);
            WriteZipText(
                zip,
                "settings.json",
                """
                {
                  "playerSlotCount": 6,
                  "creatorIsParticipant": true,
                  "timeZoneId": "UTC",
                  "roundCount": 8,
                  "roundLengthAmount": 1,
                  "roundLengthUnit": "Weeks",
                  "factions": [
                    {
                      "id": "11111111-1111-1111-1111-111111111111",
                      "name": "Empire",
                      "color": "#FF0000",
                      "subfactions": ["Reikland"],
                      "requiresSubfaction": false,
                      "flagImageStorageKey": "flags/empire.png",
                      "tintFlagImage": true,
                      "subfactionAppearances": [
                        {
                          "name": "Reikland",
                          "flagSource": "image",
                          "flagImageStorageKey": "flags/reikland.png"
                        }
                      ]
                    }
                  ],
                  "phases": [
                    { "kind": "Action", "durationAmount": 1, "durationUnit": "Days" },
                    { "kind": "Battle", "durationAmount": 5, "durationUnit": "Days" }
                  ]
                }
                """);
            WriteZipText(
                zip,
                "catalog.json",
                """
                {
                  "structureTypes": [
                    {
                      "id": "22222222-2222-2222-2222-222222222222",
                      "name": "Town",
                      "imageStorageKey": "structures/town.png",
                      "pillagedImageStorageKey": "structures/town-pillaged.png"
                    }
                  ]
                }
                """);
            WriteZipText(zip, "assets/flags/empire.png", "empire-logo");
            WriteZipText(zip, "assets/flags/reikland.png", "reikland-logo");
            WriteZipText(zip, "assets/structures/town.png", "town-logo");
            WriteZipText(zip, "assets/structures/town-pillaged.png", "town-pillaged-logo");
        }

        output.Position = 0;
        var unpacked = new CampaignPresetPackageCodec().Read(output);

        Assert.True(unpacked.IsSuccess, unpacked.Message);
        Assert.NotNull(unpacked.Value);
        var faction = unpacked.Value.Campaign.Factions.Single();
        Assert.Equal("flags/empire.png", faction.FlagImageStorageKey);
        Assert.True(faction.TintFlagImage);
        Assert.Equal("flags/reikland.png", faction.SubfactionAppearances.Single().FlagImageStorageKey);
        Assert.Equal("image", faction.SubfactionAppearances.Single().FlagSource);
        var town = unpacked.Value.Campaign.StructureTypes.Single();
        Assert.Equal("structures/town.png", town.ImageStorageKey);
        Assert.Equal("structures/town-pillaged.png", town.PillagedImageStorageKey);
        Assert.False(unpacked.Value.Campaign.IsFreeForAll);
        Assert.False(unpacked.Value.Campaign.RandomSpawnLocations);
        Assert.Null(unpacked.Value.Campaign.GameSystem);
        Assert.Equal("empire-logo"u8.ToArray(), unpacked.Value.Files["flags/empire.png"]);
        Assert.Equal("reikland-logo"u8.ToArray(), unpacked.Value.Files["flags/reikland.png"]);
        Assert.Equal("town-logo"u8.ToArray(), unpacked.Value.Files["structures/town.png"]);
        Assert.Equal("town-pillaged-logo"u8.ToArray(), unpacked.Value.Files["structures/town-pillaged.png"]);
    }

    private static void WriteZipText(ZipArchive zip, string name, string text)
    {
        var entry = zip.CreateEntry(name);
        using var stream = entry.Open();
        stream.Write(Encoding.UTF8.GetBytes(text));
    }
}
