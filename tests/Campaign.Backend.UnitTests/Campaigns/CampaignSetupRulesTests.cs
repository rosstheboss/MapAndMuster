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
        var succeeded = CampaignSetupRules.TryCreate(
            "fuck war",
            null,
            2,
            false,
            null,
            false,
            false,
            0,
            TwoFactions(),
            null,
            null,
            out _,
            out _,
            out var errors);

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
            out var setup,
            out var password,
            out var errors);

        Assert.True(succeeded);
        Assert.Empty(errors);
        Assert.NotNull(setup);
        Assert.Equal("join-secret", password);
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
