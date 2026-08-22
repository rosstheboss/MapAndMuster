using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class PrivateObjectiveRulesTests
{
    [Fact]
    public void SeedsOneAvailableObjectivePerPlayerFactionAndAllyGroup()
    {
        var playerType = Manual(
            "Player hunt",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            PrivateObjectiveHolderKind.Player);
        var factionType = Manual(
            "Faction hunt",
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            PrivateObjectiveHolderKind.Faction);
        var allyType = Manual(
            "Ally hunt",
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            PrivateObjectiveHolderKind.AllyGroup);
        var extra = Manual(
            "Spare",
            Guid.Parse("00000000-0000-0000-0000-000000000004"),
            PrivateObjectiveHolderKind.Player,
            PrivateObjectiveHolderKind.Faction);
        var player = Guid.NewGuid();
        var faction = Guid.NewGuid();
        var ally = Guid.NewGuid();

        var seeded = PrivateObjectiveRules.SeedInitial(
            [playerType, factionType, allyType, extra],
            [player],
            [faction],
            [ally],
            DateTimeOffset.UtcNow,
            static _ => 0);

        Assert.Equal(3, seeded.Count);
        Assert.Contains(seeded, item => item.HolderKind == PrivateObjectiveHolderKind.Player && item.HolderId == player && item.TypeId == playerType.Id);
        Assert.Contains(seeded, item => item.HolderKind == PrivateObjectiveHolderKind.Faction && item.HolderId == faction && item.TypeId == factionType.Id);
        Assert.Contains(seeded, item => item.HolderKind == PrivateObjectiveHolderKind.AllyGroup && item.HolderId == ally && item.TypeId == allyType.Id);
        Assert.DoesNotContain(seeded, item => item.TypeId == extra.Id);
    }

    [Fact]
    public void GrantRejectsAlreadyAssignedCatalogEntry()
    {
        var type = Manual("Unique", PrivateObjectiveHolderKind.Player);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var assigned = PrivateObjectiveRules.SeedInitial(
            [type],
            [first],
            [],
            [],
            DateTimeOffset.UtcNow,
            static _ => 0);
        var state = CampaignPlayState.Empty.With(privateObjectives: assigned);

        Assert.False(PrivateObjectiveRules.TryGrant(
            state,
            [type],
            PrivateObjectiveHolderKind.Player,
            second,
            type.Id,
            DateTimeOffset.UtcNow,
            static _ => 0,
            out _,
            out var error));
        Assert.Equal("privateObjective.unavailable", error?.Code);
    }

    [Fact]
    public void ManualClaimRequiresApprovalBeforePointsCount()
    {
        var type = Manual("Secret", PrivateObjectiveHolderKind.Player);
        var player = Guid.NewGuid();
        var manager = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        Assert.True(PrivateObjectiveRules.TryGrant(
            CampaignPlayState.Empty,
            [type],
            PrivateObjectiveHolderKind.Player,
            player,
            type.Id,
            now,
            static _ => 0,
            out var granted,
            out _));
        var assignment = Assert.Single(granted.PrivateObjectives);

        Assert.True(PrivateObjectiveRules.TryClaim(granted, assignment.Id, player, now, out var claimed, out _));
        Assert.Equal(PrivateObjectiveAssignmentStatus.Claimed, claimed.PrivateObjectives[0].Status);
        Assert.Equal(0, PrivateObjectiveRules.PointsForPlayer(
            claimed.PrivateObjectives,
            new Dictionary<Guid, int> { [type.Id] = 5 },
            player,
            factionId: null,
            allyGroupId: null,
            campaignCompleted: false));

        Assert.True(PrivateObjectiveRules.TryApprove(
            claimed,
            assignment.Id,
            manager,
            now,
            new Dictionary<Guid, string> { [type.Id] = type.Name },
            out var revealed,
            out _));
        Assert.Equal(5, PrivateObjectiveRules.PointsForPlayer(
            revealed.PrivateObjectives,
            new Dictionary<Guid, int> { [type.Id] = 5 },
            player,
            factionId: null,
            allyGroupId: null,
            campaignCompleted: false));
        Assert.Contains(revealed.Log, entry => entry.Kind == PlayLogKind.PrivateObjectiveRevealed);
        Assert.Empty(PrivateObjectiveRules.UnclaimedCounts(revealed.PrivateObjectives));
    }

    [Fact]
    public void AutomaticControlCompletesFromMapHoldings()
    {
        var structure = Guid.NewGuid();
        var type = new PrivateObjectiveTypePlayRules(
            Guid.NewGuid(),
            "Hold towns",
            4,
            [PrivateObjectiveHolderKind.Faction],
            PrivateObjectiveScoringKind.Automatic,
            PrivateObjectiveAutomaticKind.ControlStructureType,
            requiredCount: 2,
            structure,
            []);
        var faction = Guid.NewGuid();
        var player = Guid.NewGuid();
        var assignment = new PrivateObjectiveAssignment(
            Guid.NewGuid(),
            type.Id,
            PrivateObjectiveHolderKind.Faction,
            faction,
            PrivateObjectiveScoringKind.Automatic,
            PrivateObjectiveAssignmentStatus.Assigned,
            DateTimeOffset.UtcNow);
        var state = CampaignPlayState.Empty.With(privateObjectives: [assignment]);
        var next = PrivateObjectiveRules.EvaluateAutomatic(
            state,
            [type],
            [
                new PrivateObjectiveTerritory(Guid.NewGuid(), faction, structure, StructureCondition.Operational),
                new PrivateObjectiveTerritory(Guid.NewGuid(), faction, structure, StructureCondition.Pillaged),
            ],
            new Dictionary<Guid, Guid> { [player] = faction },
            new Dictionary<Guid, Guid?>(),
            new HashSet<Guid>(),
            DateTimeOffset.UtcNow);

        Assert.Equal(PrivateObjectiveAssignmentStatus.Revealed, next.PrivateObjectives[0].Status);
        Assert.Equal(4, PrivateObjectiveRules.PointsForPlayer(
            next.PrivateObjectives,
            new Dictionary<Guid, int> { [type.Id] = 4 },
            player,
            faction,
            allyGroupId: null,
            campaignCompleted: false));
    }

    [Fact]
    public void UnrevealedDetailsStayHiddenFromUnauthorizedViewers()
    {
        var assignment = new PrivateObjectiveAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PrivateObjectiveHolderKind.Player,
            Guid.NewGuid(),
            PrivateObjectiveScoringKind.Manual,
            PrivateObjectiveAssignmentStatus.Assigned,
            DateTimeOffset.UtcNow);

        Assert.False(PrivateObjectiveRules.CanViewDetails(
            assignment,
            Guid.NewGuid(),
            viewerFactionId: null,
            viewerAllyGroupId: null,
            staffView: false,
            campaignCompleted: false));
        Assert.True(PrivateObjectiveRules.CanViewDetails(
            assignment,
            assignment.HolderId,
            viewerFactionId: null,
            viewerAllyGroupId: null,
            staffView: false,
            campaignCompleted: false));
        Assert.True(PrivateObjectiveRules.CanViewDetails(
            assignment,
            Guid.NewGuid(),
            viewerFactionId: null,
            viewerAllyGroupId: null,
            staffView: true,
            campaignCompleted: false));
    }

    private static PrivateObjectiveTypePlayRules Manual(string name, params PrivateObjectiveHolderKind[] kinds)
    {
        return Manual(name, Guid.NewGuid(), kinds);
    }

    private static PrivateObjectiveTypePlayRules Manual(string name, Guid id, params PrivateObjectiveHolderKind[] kinds)
    {
        return new PrivateObjectiveTypePlayRules(
            id,
            name,
            3,
            kinds.Length > 0 ? kinds : [PrivateObjectiveHolderKind.Player],
            PrivateObjectiveScoringKind.Manual,
            PrivateObjectiveAutomaticKind.None,
            requiredCount: 1,
            structureTypeId: null,
            []);
    }
}
