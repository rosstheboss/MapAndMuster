using Campaign.Domain.Campaigns;

namespace Campaign.Backend.UnitTests.Campaigns;

public sealed class CampaignSetupRulesTests
{
    [Fact]
    public void AcceptsMinimalPublicCampaign()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Border War",
            description: null,
            playerCount: 8,
            isPrivate: false,
            joinPassword: null,
            joinPasswordRequired: false,
            creatorIsParticipant: true,
            occupiedPlayerSlotsExcludingCreator: 0,
            factions: TwoFactions(),
            allyGroups: null,
            links: null,
            schedule: WeekSchedule(),
            out var setup,
            out var password,
            out var errors);

        Assert.True(succeeded);
        Assert.Empty(errors);
        Assert.Null(password);
        Assert.NotNull(setup);
        Assert.Equal("Border War", setup.Name);
        Assert.Equal(8, setup.PlayerSlotCount);
        Assert.False(setup.IsPrivate);
        Assert.True(setup.CreatorIsParticipant);
        Assert.Equal(2, setup.Factions.Count);
        Assert.Equal(8, setup.Schedule.RoundCount);
        Assert.Equal(DurationUnit.Weeks, setup.Schedule.RoundLength.Unit);
        Assert.Equal(3, setup.Schedule.Phases.Count);
    }

    [Fact]
    public void CollectsEveryInvalidField()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "ab",
            new string('x', 501),
            playerCount: 1,
            isPrivate: true,
            joinPassword: "short",
            joinPasswordRequired: true,
            creatorIsParticipant: true,
            occupiedPlayerSlotsExcludingCreator: 0,
            factions: [new FactionInput { Name = "" }],
            allyGroups: null,
            links:
            [
                new CampaignLinkInput { Label = "", Url = "javascript:alert(1)" },
            ],
            schedule: WeekSchedule(),
            out var setup,
            out _,
            out var errors);

        Assert.False(succeeded);
        Assert.Null(setup);
        Assert.Contains(errors, error => error.Field == "name");
        Assert.Contains(errors, error => error.Field == "description");
        Assert.Contains(errors, error => error.Field == "playerCount");
        Assert.Contains(errors, error => error.Field == "joinPassword");
        Assert.Contains(errors, error => error.Field == "factions");
        Assert.Contains(errors, error => error.Message.Contains("at least 2 factions", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsProhibitedCampaignName()
    {
        var succeeded = TryMinimal("fuck war", out var errors);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Message.Contains("prohibited language", StringComparison.Ordinal));
    }

    [Fact]
    public void PrivateCampaignRequiresJoinPasswordWhenAsked()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Hidden War",
            null,
            4,
            isPrivate: true,
            joinPassword: null,
            joinPasswordRequired: true,
            creatorIsParticipant: false,
            occupiedPlayerSlotsExcludingCreator: 0,
            TwoFactions(),
            null,
            null,
            WeekSchedule(),
            out _,
            out _,
            out var errors);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Field == "joinPassword");
    }

    [Fact]
    public void PrivateCampaignCanKeepExistingPassword()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Hidden War",
            null,
            4,
            isPrivate: true,
            joinPassword: null,
            joinPasswordRequired: false,
            creatorIsParticipant: false,
            occupiedPlayerSlotsExcludingCreator: 0,
            TwoFactions(),
            null,
            null,
            WeekSchedule(),
            out var setup,
            out var password,
            out var errors);

        Assert.True(succeeded);
        Assert.Empty(errors);
        Assert.NotNull(setup);
        Assert.True(setup.IsPrivate);
        Assert.Null(password);
    }

    [Fact]
    public void RejectsWhenAllFactionsShareOneAllyGroup()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Allied War",
            null,
            4,
            false,
            null,
            false,
            true,
            0,
            [
                new FactionInput { Name = "North", AllyGroupName = "Pact" },
                new FactionInput { Name = "South", AllyGroupName = "Pact" },
            ],
            [new AllyGroupInput { Name = "Pact" }],
            null,
            WeekSchedule(),
            out _,
            out _,
            out var errors);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Code == "allyGroups.covers_all");
    }

    [Fact]
    public void AllowsSplitAllyGroupsAndUnalignedFactions()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Three Sides",
            "A contested border.",
            6,
            false,
            null,
            false,
            true,
            0,
            [
                new FactionInput { Name = "North", Subfactions = ["Riders"], AllyGroupName = "Pact" },
                new FactionInput { Name = "East", AllyGroupName = "Pact" },
                new FactionInput { Name = "South" },
            ],
            [new AllyGroupInput { Name = "Pact" }],
            [
                new CampaignLinkInput { Label = "Rules", Url = "https://example.test/rules" },
            ],
            WeekSchedule(),
            out var setup,
            out _,
            out var errors);

        Assert.True(succeeded, string.Join('\n', errors.Select(error => error.Message)));
        Assert.NotNull(setup);
        Assert.Equal("A contested border.", setup.Description);
        Assert.Equal(3, setup.Factions.Count);
        Assert.Equal("Pact", setup.Factions[0].AllyGroupName);
        Assert.Equal("Riders", setup.Factions[0].Subfactions[0]);
        Assert.Null(setup.Factions[2].AllyGroupName);
        Assert.Equal("https://example.test/rules", setup.Links[0].Url);
    }

    [Fact]
    public void RejectsAllyGroupWithFewerThanTwoFactions()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Thin Pact",
            null,
            3,
            false,
            null,
            false,
            false,
            0,
            [
                new FactionInput { Name = "North", AllyGroupName = "Pact" },
                new FactionInput { Name = "South" },
            ],
            [new AllyGroupInput { Name = "Pact" }],
            null,
            WeekSchedule(),
            out _,
            out _,
            out var errors);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Code == "allyGroups.members.invalid");
    }

    [Fact]
    public void RejectsPlayerCountBelowOccupiedSlots()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Crowded War",
            null,
            2,
            false,
            null,
            false,
            creatorIsParticipant: true,
            occupiedPlayerSlotsExcludingCreator: 2,
            TwoFactions(),
            null,
            null,
            WeekSchedule(),
            out _,
            out _,
            out var errors);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Code == "campaign.player_count.occupied");
    }

    [Fact]
    public void RejectsJavascriptLinksAndTooManyLinks()
    {
        var links = Enumerable.Range(1, 21)
            .Select(index => new CampaignLinkInput { Label = $"Link {index}", Url = "https://example.test" })
            .ToArray();

        var succeeded = CampaignSetupRules.TryCreate(
            "Linked War",
            null,
            2,
            false,
            null,
            false,
            false,
            0,
            TwoFactions(),
            null,
            links,
            WeekSchedule(),
            out _,
            out _,
            out var errors);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Field == "links");
    }

    [Fact]
    public void RejectsDuplicateFactionNames()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Mirror War",
            null,
            2,
            false,
            null,
            false,
            false,
            0,
            [
                new FactionInput { Name = "North" },
                new FactionInput { Name = "north" },
            ],
            null,
            null,
            WeekSchedule(),
            out _,
            out _,
            out var errors);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Code == "factions.duplicate");
    }

    [Fact]
    public void RejectsUnknownAllyGroupReference()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Lost Pact",
            null,
            2,
            false,
            null,
            false,
            false,
            0,
            [
                new FactionInput { Name = "North", AllyGroupName = "Missing" },
                new FactionInput { Name = "South" },
            ],
            null,
            null,
            WeekSchedule(),
            out _,
            out _,
            out var errors);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Code == "factions.ally_group.unknown");
    }

    [Fact]
    public void StoresValidatedJoinPasswordForPrivateCampaigns()
    {
        var succeeded = CampaignSetupRules.TryCreate(
            "Hidden War",
            null,
            4,
            isPrivate: true,
            joinPassword: "join-secret",
            joinPasswordRequired: true,
            creatorIsParticipant: false,
            occupiedPlayerSlotsExcludingCreator: 0,
            TwoFactions(),
            null,
            null,
            WeekSchedule(),
            out var setup,
            out var password,
            out var errors);

        Assert.True(succeeded);
        Assert.Empty(errors);
        Assert.NotNull(setup);
        Assert.Equal("join-secret", password);
    }

    [Fact]
    public void RejectsRoundCountOutsideRange()
    {
        var schedule = WeekSchedule() with { RoundCount = 2 };
        var succeeded = TryMinimal("Border War", out var errors, schedule);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Field == "roundCount");
    }

    [Fact]
    public void RejectsWhenActionsExceedRoundLength()
    {
        var schedule = WeekSchedule() with
        {
            Phases =
            [
                new RoundPhaseInput { Kind = "Action", DurationAmount = 4, DurationUnit = "Days" },
                new RoundPhaseInput { Kind = "Action", DurationAmount = 4, DurationUnit = "Days" },
                new RoundPhaseInput { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
        };
        var succeeded = TryMinimal("Border War", out var errors, schedule);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Code == "phases.actions_too_long");
        Assert.Contains(errors, error => error.Code == "phases.duration_mismatch");
    }

    [Fact]
    public void RejectsWhenPhasesDoNotAddUpToRoundLength()
    {
        var schedule = WeekSchedule() with
        {
            Phases =
            [
                new RoundPhaseInput { Kind = "Action", DurationAmount = 1, DurationUnit = "Days" },
                new RoundPhaseInput { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
        };
        var succeeded = TryMinimal("Border War", out var errors, schedule);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Code == "phases.duration_mismatch");
    }

    [Fact]
    public void RejectsRoundWithoutABattlePhase()
    {
        var schedule = WeekSchedule() with
        {
            Phases =
            [
                new RoundPhaseInput { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new RoundPhaseInput { Kind = "Action", DurationAmount = 4, DurationUnit = "Days" },
            ],
        };
        var succeeded = TryMinimal("Border War", out var errors, schedule);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Field == "phases");
    }

    [Fact]
    public void RejectsInvalidDurationAmounts()
    {
        var schedule = WeekSchedule() with { RoundLengthAmount = 8, RoundLengthUnit = "Days" };
        var succeeded = TryMinimal("Border War", out var errors, schedule);

        Assert.False(succeeded);
        Assert.Contains(errors, error => error.Field == "roundLength");
    }

    [Fact]
    public void AcceptsBattleBetweenActionWindows()
    {
        var schedule = WeekSchedule() with
        {
            Phases =
            [
                new RoundPhaseInput { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new RoundPhaseInput { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
                new RoundPhaseInput { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
            ],
        };
        var succeeded = TryMinimal("Border War", out var errors, schedule);

        Assert.True(succeeded, string.Join('\n', errors.Select(error => error.Message)));
    }

    [Fact]
    public void AcceptsMonthLengthRoundsWhenPhasesMatch()
    {
        var schedule = new CampaignScheduleInput
        {
            TimeZoneId = "UTC",
            StartsAtLocal = "2026-01-15T12:00",
            RoundCount = 3,
            RoundLengthAmount = 2,
            RoundLengthUnit = "Months",
            Phases =
            [
                new RoundPhaseInput { Kind = "Action", DurationAmount = 1, DurationUnit = "Months" },
                new RoundPhaseInput { Kind = "Battle", DurationAmount = 1, DurationUnit = "Months" },
            ],
        };
        var succeeded = TryMinimal("Long War", out var errors, schedule);

        Assert.True(succeeded, string.Join('\n', errors.Select(error => error.Message)));
    }

    [Fact]
    public void DefaultsBlankTimeZoneToUtc()
    {
        var schedule = WeekSchedule() with { TimeZoneId = null };
        var succeeded = TryMinimal("Border War", out var errors, schedule, out var setup);

        Assert.True(succeeded, string.Join('\n', errors.Select(error => error.Message)));
        Assert.NotNull(setup);
        Assert.Equal("UTC", setup.Schedule.TimeZone.Id);
    }

    private static bool TryMinimal(string name, out IReadOnlyList<Campaign.Domain.Common.DomainError> errors)
    {
        return TryMinimal(name, out errors, WeekSchedule(), out _);
    }

    private static bool TryMinimal(
        string name,
        out IReadOnlyList<Campaign.Domain.Common.DomainError> errors,
        CampaignScheduleInput schedule)
    {
        return TryMinimal(name, out errors, schedule, out _);
    }

    private static bool TryMinimal(
        string name,
        out IReadOnlyList<Campaign.Domain.Common.DomainError> errors,
        CampaignScheduleInput schedule,
        out CampaignSetup? setup)
    {
        return CampaignSetupRules.TryCreate(
            name,
            null,
            8,
            false,
            null,
            false,
            true,
            0,
            TwoFactions(),
            null,
            null,
            schedule,
            out setup,
            out _,
            out errors);
    }

    internal static CampaignScheduleInput WeekSchedule()
    {
        return new CampaignScheduleInput
        {
            TimeZoneId = "UTC",
            StartsAtLocal = "2026-09-01T12:00",
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases =
            [
                new RoundPhaseInput { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new RoundPhaseInput { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new RoundPhaseInput { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
        };
    }

    private static IReadOnlyList<FactionInput> TwoFactions()
    {
        return
        [
            new FactionInput { Name = "North" },
            new FactionInput { Name = "South" },
        ];
    }
}
