using System.IO.Compression;
using System.Text;
using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Infrastructure.Campaigns;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class CampaignPresetPackageCodecTests
{
    [Fact]
    public void ReadRejectsAnEmptyArchive()
    {
        var codec = new CampaignPresetPackageCodec();
        var result = codec.Read(Array.Empty<byte>());
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
        var result = codec.Read(output.ToArray());
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
        var result = codec.Read(output.ToArray());
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
        var packed = codec.Write(campaign, files);
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
    }
}
