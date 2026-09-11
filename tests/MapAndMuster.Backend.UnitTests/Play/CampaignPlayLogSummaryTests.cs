using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Play;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class CampaignPlayLogSummaryTests
{
    private static readonly Guid CampaignId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid North = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid South = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Bob = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Leopold = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FromId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ToId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FormatsResolvedActionsInNaturalLanguage()
    {
        var log = new[]
        {
            Action(ActionKind.Move, FromId, ToId, at: Now),
            Action(ActionKind.Pillage, FromId, message: "Town", at: Now.AddSeconds(1)),
            Action(ActionKind.Pillage, FromId, message: PlayLogFacts.DestroyedStructure("City"), at: Now.AddSeconds(2)),
            Action(ActionKind.Split, FromId, ToId, at: Now.AddSeconds(3)),
            Action(ActionKind.Hold, FromId, at: Now.AddSeconds(4)),
            Action(ActionKind.Build, FromId, message: "Fortification", at: Now.AddSeconds(5)),
            Action(ActionKind.Repair, FromId, message: "Supply Depot", at: Now.AddSeconds(6)),
        };
        var summaries = Summaries(log);

        Assert.Equal("bob moved force at West Avila Coastline to Los Cabos.", summaries[0]);
        Assert.Equal("bob pillaged Town at West Avila Coastline.", summaries[1]);
        Assert.Equal("bob destroyed City at West Avila Coastline.", summaries[2]);
        Assert.Equal("bob split force at West Avila Coastline into Los Cabos.", summaries[3]);
        Assert.Equal("bob held in West Avila Coastline.", summaries[4]);
        Assert.Equal("bob built a Fortification at West Avila Coastline.", summaries[5]);
        Assert.Equal("bob repaired the Supply Depot at West Avila Coastline.", summaries[6]);
    }

    [Fact]
    public void FormatsMergeAndRetreat()
    {
        var summaries = Summaries(
        [
            new PlayLogEntry(Guid.NewGuid(), Now, PlayLogKind.ForcesRejoined, null, null, Bob, FromId, null, null, null, []),
            new PlayLogEntry(Guid.NewGuid(), Now.AddSeconds(1), PlayLogKind.PlayerRetreat, null, null, Bob, FromId, ToId, null, ActionKind.Retreat, []),
        ]);

        Assert.Equal("bob merged forces at West Avila Coastline.", summaries[0]);
        Assert.Equal("bob retreated force at West Avila Coastline to Los Cabos.", summaries[1]);
    }

    [Fact]
    public void FormatsTreacheryForAttackPillageAndClaim()
    {
        var summaries = Summaries(
        [
            new PlayLogEntry(
                Guid.NewGuid(),
                Now,
                PlayLogKind.AllianceBetrayed,
                null,
                null,
                Bob,
                FromId,
                null,
                null,
                ActionKind.Backstab,
                [],
                PlayLogFacts.Betrayal(PlayLogFacts.BetrayalAttack, Leopold, South)),
            new PlayLogEntry(
                Guid.NewGuid(),
                Now.AddSeconds(1),
                PlayLogKind.AllianceBetrayed,
                null,
                null,
                Bob,
                ToId,
                null,
                null,
                ActionKind.Backstab,
                [],
                PlayLogFacts.Betrayal(PlayLogFacts.BetrayalPillage, Leopold, South, "Castle")),
            new PlayLogEntry(
                Guid.NewGuid(),
                Now.AddSeconds(2),
                PlayLogKind.AllianceBetrayed,
                null,
                null,
                Bob,
                ToId,
                null,
                null,
                ActionKind.Backstab,
                [],
                PlayLogFacts.Betrayal(PlayLogFacts.BetrayalClaim, Leopold, South)),
        ]);

        Assert.Equal(
            "Treachery! bob launched a surprise attack against leopold at West Avila Coastline. They are now locked in battle. bob is no longer allies with Warriors of Chaos.",
            summaries[0]);
        Assert.Equal(
            "Treachery! bob betrayed leopold and pillaged the Castle at Los Cabos. bob is no longer allies with Warriors of Chaos.",
            summaries[1]);
        Assert.Equal(
            "Treachery! bob claimed the land held by their ally leopold. bob is no longer allies with Warriors of Chaos.",
            summaries[2]);
    }

    private static PlayLogEntry Action(
        ActionKind kind,
        Guid territoryId,
        Guid? targetId = null,
        string? message = null,
        DateTimeOffset? at = null)
    {
        return new PlayLogEntry(
            Guid.NewGuid(),
            at ?? Now,
            PlayLogKind.ResolvedAction,
            null,
            null,
            Bob,
            territoryId,
            targetId,
            null,
            kind,
            [],
            message);
    }

    private static IReadOnlyList<string> Summaries(IReadOnlyList<PlayLogEntry> log)
    {
        var campaign = Campaign(log);
        return
        [
            .. CampaignPlayMapper.ToLogEntries(
                    campaign,
                    new Dictionary<Guid, string> { [Bob] = "bob", [Leopold] = "leopold" },
                    Bob,
                    inspectPrivateChat: false)
                .Select(item => item.Summary),
        ];
    }

    private static StoredCampaign Campaign(IReadOnlyList<PlayLogEntry> log)
    {
        var play = CampaignPlayState.Empty;
        foreach (var entry in log)
        {
            play = play.AppendLog(entry);
        }

        play = play.With(
            forces:
            [
                new CampaignForce(Guid.NewGuid(), Bob, North, FromId, false),
                new CampaignForce(Guid.NewGuid(), Leopold, South, ToId, false),
            ]);

        return new StoredCampaign
        {
            Id = CampaignId,
            Name = "Border War",
            PlayerSlotCount = 8,
            IsPrivate = false,
            IsPubliclyViewable = true,
            CreatorIsParticipant = true,
            Revision = 1,
            CreatedUtc = Now,
            UpdatedUtc = Now,
            CreatedByUserId = Bob,
            Memberships =
            [
                new StoredCampaignMembership { UserId = Bob, IsGameMaster = false, IsPlayer = true, FactionId = North },
                new StoredCampaignMembership { UserId = Leopold, IsGameMaster = false, IsPlayer = true, FactionId = South },
            ],
            Factions =
            [
                new StoredFaction { Id = North, Name = "Empire", Color = "#111111", Subfactions = [], RequiresSubfaction = false },
                new StoredFaction { Id = South, Name = "Warriors of Chaos", Color = "#222222", Subfactions = [], RequiresSubfaction = false },
            ],
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = Now,
            EndsUtc = Now.AddDays(30),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases = [new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" }],
            PlayState = play,
            TerrainTypes = [],
            StructureTypes = [],
            MapGraph = new StoredMapGraph
            {
                Territories =
                [
                    NamedTerritory(FromId, 1, "West Avila Coastline"),
                    NamedTerritory(ToId, 2, "Los Cabos"),
                ],
                Adjacencies = [],
            },
        };
    }

    private static TerritoryDetail NamedTerritory(Guid id, int number, string name)
    {
        return new TerritoryDetail
        {
            Id = id,
            DisplayNumber = number,
            Name = name,
            Polygon =
            [
                new MapPointDetail { X = 0, Y = 0 },
                new MapPointDetail { X = 1, Y = 0 },
                new MapPointDetail { X = 1, Y = 1 },
                new MapPointDetail { X = 0, Y = 1 },
            ],
            TerrainTypeId = Guid.NewGuid(),
        };
    }
}
