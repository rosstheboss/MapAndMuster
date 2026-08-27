using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Identity;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class LocalTestCampaignCopyTests
{
    private static readonly Guid ManagerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid DaemonsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EmpireId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SkavenId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void AssignsEveryFactionAndSubfactionOnATenMinuteSchedule()
    {
        var copy = LocalTestCampaignCopy.Configure(
            SourceCampaign(),
            LocalTestCampaignStage.Action1,
            ManagerId,
            TestUsers(8),
            Now);

        Assert.Equal("[Test] Estalia (Action 1)", copy.Name);
        Assert.False(copy.CreatorIsParticipant);
        Assert.Equal(LocalTestCampaignCopy.RoundMinutes, copy.RoundLengthAmount);
        Assert.Equal(nameof(DurationUnit.Minutes), copy.RoundLengthUnit);
        Assert.Equal(["Action", "Action", "Battle"], copy.Phases.Select(phase => phase.Kind).ToArray());
        Assert.Equal([10, 10, 40], copy.Phases.Select(phase => phase.DurationAmount).ToArray());
        Assert.False(copy.Phases[^1].EndPhaseEarlyIfAble);
        Assert.Equal(Now.AddSeconds(-15), copy.StartsUtc);
        Assert.Equal(TimeSpan.FromHours(8), copy.EndsUtc - copy.StartsUtc);
        Assert.Single(copy.Memberships, member => member.UserId == ManagerId && member.IsGameMaster && !member.IsPlayer);
        Assert.Equal(5, copy.Memberships.Count(member => member.IsPlayer));
        Assert.Equal(5, copy.PlayerSlotCount);
        Assert.Contains(
            copy.Memberships,
            member => member.FactionId == DaemonsId && member.Subfaction == "Khorne");
        Assert.Contains(
            copy.Memberships,
            member => member.FactionId == EmpireId && member.Subfaction is null);
        Assert.Contains(
            copy.Memberships,
            member => member.FactionId == EmpireId && member.Subfaction == "Knightly Orders");
        Assert.Contains(copy.Memberships, member => member.FactionId == SkavenId && member.Subfaction is null);
        Assert.DoesNotContain(copy.Memberships, member => member.FactionId == DaemonsId && member.Subfaction is null);
    }

    [Fact]
    public void LeavesNotStartedCopiesInTheFutureAndOpensBattleAfterBothActions()
    {
        Assert.Equal(Now.AddDays(7), LocalTestCampaignCopy.StartsUtc(LocalTestCampaignStage.NotStarted, Now));
        Assert.Equal(
            Now.AddMinutes(-10).AddSeconds(-30),
            LocalTestCampaignCopy.StartsUtc(LocalTestCampaignStage.Action2, Now));
        Assert.Equal(
            Now.AddMinutes(-20).AddSeconds(-30),
            LocalTestCampaignCopy.StartsUtc(LocalTestCampaignStage.Battle, Now));
        Assert.Equal("[Test] Estalia (not started)", LocalTestCampaignCopy.NameFor(LocalTestCampaignStage.NotStarted));
        Assert.Equal("[Test] Estalia (Battle)", LocalTestCampaignCopy.NameFor(LocalTestCampaignStage.Battle));
    }

    private static StoredCampaign SourceCampaign()
    {
        return new StoredCampaign
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Name = "The Hunt in Estalia",
            PlayerSlotCount = 8,
            IsPrivate = true,
            IsPubliclyViewable = false,
            JoinPasswordHash = "hash:secret",
            CreatorIsParticipant = true,
            MapStorageKey = "maps/estalia.png",
            Revision = 1,
            CreatedUtc = Now,
            UpdatedUtc = Now,
            CreatedByUserId = ManagerId,
            Memberships =
            [
                new StoredCampaignMembership { UserId = ManagerId, IsGameMaster = true, IsPlayer = true },
            ],
            Factions =
            [
                new StoredFaction
                {
                    Id = DaemonsId,
                    Name = "Daemons of Chaos",
                    Color = "#AD1457",
                    Subfactions = ["Khorne", "Nurgle"],
                    RequiresSubfaction = true,
                },
                new StoredFaction
                {
                    Id = EmpireId,
                    Name = "Empire of Man",
                    Color = "#F5D000",
                    Subfactions = ["Knightly Orders"],
                    RequiresSubfaction = false,
                },
                new StoredFaction
                {
                    Id = SkavenId,
                    Name = "Skaven",
                    Color = "#78716C",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
            ],
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = Now.AddDays(1),
            EndsUtc = Now.AddDays(57),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases =
            [
                new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new StoredRoundPhase { Kind = "Battle", DurationAmount = 4, DurationUnit = "Days" },
            ],
            MapGraph = new MapAndMuster.Application.Maps.StoredMapGraph { Territories = [], Adjacencies = [] },
            TerrainTypes = [],
            StructureTypes = [],
        };
    }

    private static IReadOnlyList<UserAccount> TestUsers(int count)
    {
        return
        [
            .. Enumerable.Range(1, count).Select(number => new UserAccount
            {
                Id = Guid.Parse($"eeeeeeee-eeee-eeee-eeee-{number:D12}"),
                Email = TestAccountCatalog.Email(number),
                Username = TestAccountCatalog.Username(number),
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
                TestAccountNumber = number,
            }),
        ];
    }
}
