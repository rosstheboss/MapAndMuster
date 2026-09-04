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
    public void SeedSkipsAHolderKindWhenItsPoolIsEmpty()
    {
        var playerType = Manual(
            "Player hunt",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            PrivateObjectiveHolderKind.Player);
        var allyType = Manual(
            "Ally hunt",
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            PrivateObjectiveHolderKind.AllyGroup);
        var player = Guid.NewGuid();
        var faction = Guid.NewGuid();
        var ally = Guid.NewGuid();

        var seeded = PrivateObjectiveRules.SeedInitial(
            [playerType, allyType],
            [player],
            [faction],
            [ally],
            DateTimeOffset.UtcNow,
            static _ => 0);

        Assert.Equal(2, seeded.Count);
        Assert.Contains(seeded, item => item.HolderKind == PrivateObjectiveHolderKind.Player && item.TypeId == playerType.Id);
        Assert.Contains(seeded, item => item.HolderKind == PrivateObjectiveHolderKind.AllyGroup && item.TypeId == allyType.Id);
        Assert.DoesNotContain(seeded, item => item.HolderKind == PrivateObjectiveHolderKind.Faction);
    }

    [Fact]
    public void SeedGivesUniqueThenReshuffledDuplicatesUntilEveryHolderHasOne()
    {
        var first = Manual(
            "First",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            PrivateObjectiveHolderKind.Player);
        var second = Manual(
            "Second",
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            PrivateObjectiveHolderKind.Player);
        var players = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).OrderBy(id => id).ToArray();

        var seeded = PrivateObjectiveRules.SeedInitial(
            [first, second],
            players,
            [],
            [],
            DateTimeOffset.UtcNow,
            static _ => 0);

        Assert.Equal(5, seeded.Count);
        Assert.Equal(players.Length, seeded.Select(item => item.HolderId).Distinct().Count());
        Assert.Equal(
            [first.Id, second.Id, first.Id, second.Id, first.Id],
            seeded.OrderBy(item => item.HolderId).Select(item => item.TypeId).ToArray());
        Assert.Equal(2, seeded.Select(item => item.TypeId).Distinct().Count());
    }

    [Fact]
    public void SeedAllowsTheSameCatalogEntryInSeparateHolderKindPools()
    {
        var shared = Manual(
            "Shared hunt",
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            PrivateObjectiveHolderKind.Player,
            PrivateObjectiveHolderKind.Faction);
        var player = Guid.NewGuid();
        var faction = Guid.NewGuid();

        var seeded = PrivateObjectiveRules.SeedInitial(
            [shared],
            [player],
            [faction],
            [],
            DateTimeOffset.UtcNow,
            static _ => 0);

        Assert.Equal(2, seeded.Count);
        Assert.All(seeded, item => Assert.Equal(shared.Id, item.TypeId));
        Assert.Contains(seeded, item => item.HolderKind == PrivateObjectiveHolderKind.Player);
        Assert.Contains(seeded, item => item.HolderKind == PrivateObjectiveHolderKind.Faction);
    }

    [Fact]
    public void DuplicateAssignmentsScoreIndependently()
    {
        var type = Manual("Shared hunt", PrivateObjectiveHolderKind.Player);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var seeded = PrivateObjectiveRules.SeedInitial(
            [type],
            [first, second],
            [],
            [],
            now,
            static _ => 0);
        var names = new Dictionary<Guid, string> { [type.Id] = type.Name };
        var points = new Dictionary<Guid, int> { [type.Id] = 5 };
        var state = CampaignPlayState.Empty.With(privateObjectives: seeded);
        var firstAssignment = seeded.Single(item => item.HolderId == first);
        var secondAssignment = seeded.Single(item => item.HolderId == second);

        Assert.True(PrivateObjectiveRules.TryApprove(state, firstAssignment.Id, first, now, names, out var afterFirst, out _));
        Assert.Equal(5, PrivateObjectiveRules.PointsForPlayer(
            afterFirst.PrivateObjectives,
            points,
            first,
            factionId: null,
            allyGroupId: null,
            campaignCompleted: false));
        Assert.Equal(0, PrivateObjectiveRules.PointsForPlayer(
            afterFirst.PrivateObjectives,
            points,
            second,
            factionId: null,
            allyGroupId: null,
            campaignCompleted: false));

        Assert.True(PrivateObjectiveRules.TryApprove(afterFirst, secondAssignment.Id, second, now, names, out var afterBoth, out _));
        Assert.Equal(5, PrivateObjectiveRules.PointsForPlayer(
            afterBoth.PrivateObjectives,
            points,
            first,
            factionId: null,
            allyGroupId: null,
            campaignCompleted: false));
        Assert.Equal(5, PrivateObjectiveRules.PointsForPlayer(
            afterBoth.PrivateObjectives,
            points,
            second,
            factionId: null,
            allyGroupId: null,
            campaignCompleted: false));
    }

    [Fact]
    public void GrantAllowsTheSameCatalogEntryForAnotherHolder()
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

        Assert.True(PrivateObjectiveRules.TryGrant(
            state,
            [type],
            PrivateObjectiveHolderKind.Player,
            second,
            type.Id,
            DateTimeOffset.UtcNow,
            static _ => 0,
            out var granted,
            out _));
        Assert.Equal(2, granted.PrivateObjectives.Count);
        Assert.All(granted.PrivateObjectives, item => Assert.Equal(type.Id, item.TypeId));
        Assert.NotEqual(granted.PrivateObjectives[0].Id, granted.PrivateObjectives[1].Id);
    }

    [Fact]
    public void GrantRejectsGivingTheSameCatalogEntryToTheSameHolderTwice()
    {
        var type = Manual("Unique", PrivateObjectiveHolderKind.Player);
        var player = Guid.NewGuid();
        var assigned = PrivateObjectiveRules.SeedInitial(
            [type],
            [player],
            [],
            [],
            DateTimeOffset.UtcNow,
            static _ => 0);
        var state = CampaignPlayState.Empty.With(privateObjectives: assigned);

        Assert.False(PrivateObjectiveRules.TryGrant(
            state,
            [type],
            PrivateObjectiveHolderKind.Player,
            player,
            type.Id,
            DateTimeOffset.UtcNow,
            static _ => 0,
            out _,
            out var error));
        Assert.Equal("privateObjective.unavailable", error?.Code);
    }

    [Fact]
    public void LateJoiningPlayerReceivesADuplicateWhenThePlayerPoolIsExhausted()
    {
        var type = Manual("Player hunt", PrivateObjectiveHolderKind.Player);
        var first = Guid.NewGuid();
        var late = Guid.NewGuid();
        var seeded = PrivateObjectiveRules.SeedInitial(
            [type],
            [first],
            [],
            [],
            DateTimeOffset.UtcNow,
            static _ => 0);
        var state = CampaignPlayState.Empty.With(privateObjectives: seeded);

        var next = PrivateObjectiveRules.EnsurePlayerAssignment(
            state,
            [type],
            late,
            DateTimeOffset.UtcNow,
            static _ => 0);

        Assert.Equal(2, next.PrivateObjectives.Count);
        Assert.Contains(next.PrivateObjectives, item => item.HolderId == late && item.TypeId == type.Id);
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
    public void AutomaticDuplicatesCompleteOnlyForTheHolderWhoMeetsTheCriterion()
    {
        var type = new PrivateObjectiveTypePlayRules(
            Guid.NewGuid(),
            "Hold towns",
            4,
            [PrivateObjectiveHolderKind.Faction],
            PrivateObjectiveScoringKind.Automatic,
            PrivateObjectiveAutomaticKind.ControlStructureType,
            requiredCount: 2,
            Guid.NewGuid(),
            []);
        var north = Guid.NewGuid();
        var south = Guid.NewGuid();
        var northPlayer = Guid.NewGuid();
        var southPlayer = Guid.NewGuid();
        var structure = type.StructureTypeId!.Value;
        var seeded = PrivateObjectiveRules.SeedInitial(
            [type],
            [],
            [north, south],
            [],
            DateTimeOffset.UtcNow,
            static _ => 0);
        var state = CampaignPlayState.Empty.With(privateObjectives: seeded);
        var next = PrivateObjectiveRules.EvaluateAutomatic(
            state,
            [type],
            [
                new PrivateObjectiveTerritory(Guid.NewGuid(), north, structure, StructureCondition.Operational),
                new PrivateObjectiveTerritory(Guid.NewGuid(), north, structure, StructureCondition.Pillaged),
            ],
            new Dictionary<Guid, Guid> { [northPlayer] = north, [southPlayer] = south },
            new Dictionary<Guid, Guid?>(),
            new HashSet<Guid>(),
            DateTimeOffset.UtcNow);

        Assert.Equal(
            PrivateObjectiveAssignmentStatus.Revealed,
            next.PrivateObjectives.Single(item => item.HolderId == north).Status);
        Assert.Equal(
            PrivateObjectiveAssignmentStatus.Assigned,
            next.PrivateObjectives.Single(item => item.HolderId == south).Status);
        Assert.Equal(4, PrivateObjectiveRules.PointsForPlayer(
            next.PrivateObjectives,
            new Dictionary<Guid, int> { [type.Id] = 4 },
            northPlayer,
            north,
            allyGroupId: null,
            campaignCompleted: false));
        Assert.Equal(0, PrivateObjectiveRules.PointsForPlayer(
            next.PrivateObjectives,
            new Dictionary<Guid, int> { [type.Id] = 4 },
            southPlayer,
            south,
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

    [Fact]
    public void BattleWinsCompleteFromFinalizedVictories()
    {
        var type = Automatic("Win two", PrivateObjectiveAutomaticKind.BattleWinCount, requiredCount: 2);
        var player = Guid.NewGuid();
        var winner = new CampaignForce(Guid.NewGuid(), player, Guid.NewGuid(), Guid.NewGuid(), false);
        var loser = new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false);
        var assignment = Assigned(type, PrivateObjectiveHolderKind.Player, player);
        var now = DateTimeOffset.UtcNow;
        var state = CampaignPlayState.Empty.With(
            forces: [winner, loser],
            battles:
            [
                Battle(winner.Id, loser.Id, now),
                Battle(winner.Id, loser.Id, now.AddHours(1)),
            ],
            privateObjectives: [assignment]);

        var next = Evaluate(state, type, []);

        Assert.Equal(PrivateObjectiveAssignmentStatus.Revealed, next.PrivateObjectives[0].Status);
    }

    [Fact]
    public void PlayerChosenRetreatsIgnoreDefaultAndStaffRetreats()
    {
        var type = Automatic("Retreat twice", PrivateObjectiveAutomaticKind.PlayerRetreatCount, requiredCount: 2);
        var player = Guid.NewGuid();
        var force = new CampaignForce(Guid.NewGuid(), player, Guid.NewGuid(), Guid.NewGuid(), false);
        var assignment = Assigned(type, PrivateObjectiveHolderKind.Player, player);
        var now = DateTimeOffset.UtcNow;
        var incomplete = CampaignPlayState.Empty.With(
            forces: [force],
            retreats:
            [
                new RetreatOrder(Guid.NewGuid(), Guid.NewGuid(), force.Id, Guid.NewGuid(), isDefault: false, now),
                new RetreatOrder(Guid.NewGuid(), Guid.NewGuid(), force.Id, Guid.NewGuid(), isDefault: true, now),
                new RetreatOrder(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    force.Id,
                    Guid.NewGuid(),
                    isDefault: false,
                    now,
                    isStaffCorrection: true),
            ],
            privateObjectives: [assignment]);

        Assert.Equal(
            PrivateObjectiveAssignmentStatus.Assigned,
            Evaluate(incomplete, type, []).PrivateObjectives[0].Status);

        var complete = incomplete.With(
            retreats:
            [
                .. incomplete.Retreats,
                new RetreatOrder(Guid.NewGuid(), Guid.NewGuid(), force.Id, Guid.NewGuid(), isDefault: false, now),
            ]);

        Assert.Equal(
            PrivateObjectiveAssignmentStatus.Revealed,
            Evaluate(complete, type, []).PrivateObjectives[0].Status);
    }

    [Fact]
    public void RelicAdjacencyCountsTheSameTerritoryAndDirectNeighbors()
    {
        var relicType = Guid.NewGuid();
        var type = Automatic(
            "Near the crown",
            PrivateObjectiveAutomaticKind.AdjacentToRelic,
            itemObjectiveTypeId: relicType);
        var player = Guid.NewGuid();
        var home = Guid.NewGuid();
        var neighbor = Guid.NewGuid();
        var far = Guid.NewGuid();
        var force = new CampaignForce(Guid.NewGuid(), player, Guid.NewGuid(), home, false);
        var relic = new CampaignItemObjective(
            Guid.NewGuid(),
            relicType,
            "Crown",
            neighbor,
            possessorForceId: null,
            isRevealed: true,
            neighbor,
            wasHiddenUntilFound: false);
        var map = new PlayMap(
            [
                new PlayTerritory(home, 1, null, null, null, null, StructureCondition.Operational),
                new PlayTerritory(neighbor, 2, null, null, null, null, StructureCondition.Operational),
                new PlayTerritory(far, 3, null, null, null, null, StructureCondition.Operational),
            ],
            [(home, neighbor)]);
        var assignment = Assigned(type, PrivateObjectiveHolderKind.Player, player);
        var state = CampaignPlayState.Empty.With(
            forces: [force],
            itemObjectives: [relic],
            privateObjectives: [assignment]);

        var next = Evaluate(
            state,
            type,
            [new PrivateObjectiveTerritory(home, force.FactionId, null, StructureCondition.Operational)],
            map: map);

        Assert.Equal(PrivateObjectiveAssignmentStatus.Revealed, next.PrivateObjectives[0].Status);
    }

    [Fact]
    public void BuildAnyStructureTypeCompletesFromWorkFacts()
    {
        var type = Automatic(
            "Build two",
            PrivateObjectiveAutomaticKind.BuildStructureType,
            requiredCount: 2,
            matchesAnyStructureType: true);
        var player = Guid.NewGuid();
        var faction = Guid.NewGuid();
        var assignment = Assigned(type, PrivateObjectiveHolderKind.Player, player);
        var now = DateTimeOffset.UtcNow;
        var state = CampaignPlayState.Empty.With(
            structureWorks:
            [
                new StructureWorkFact(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ActionKind.Build, faction, player, now),
                new StructureWorkFact(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ActionKind.Build, faction, player, now),
            ],
            privateObjectives: [assignment]);

        var next = Evaluate(state, type, []);

        Assert.Equal(PrivateObjectiveAssignmentStatus.Revealed, next.PrivateObjectives[0].Status);
    }

    [Fact]
    public void DefeatRandomOpponentResolvesAtAssignmentAndScoresThatOpponent()
    {
        var north = Guid.NewGuid();
        var south = Guid.NewGuid();
        var type = Automatic(
            "Defeat a rival",
            PrivateObjectiveAutomaticKind.DefeatOpponent,
            targetKind: PrivateObjectiveTargetKind.Faction,
            targetSelection: PrivateObjectiveTargetSelection.Random);
        var player = Guid.NewGuid();
        var winner = new CampaignForce(Guid.NewGuid(), player, north, Guid.NewGuid(), false);
        var loser = new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), south, Guid.NewGuid(), false);
        var seeded = PrivateObjectiveRules.SeedInitial(
            [type],
            [player],
            [north, south],
            [],
            DateTimeOffset.UtcNow,
            static _ => 0,
            new Dictionary<Guid, Guid> { [player] = north });
        var assignment = Assert.Single(seeded, item => item.HolderKind == PrivateObjectiveHolderKind.Player);
        Assert.Equal(south, assignment.ResolvedTargetId);
        var state = CampaignPlayState.Empty.With(
            forces: [winner, loser],
            battles: [Battle(winner.Id, loser.Id, DateTimeOffset.UtcNow)],
            privateObjectives: [assignment]);

        var next = Evaluate(
            state,
            type,
            [],
            factionByPlayer: new Dictionary<Guid, Guid> { [player] = north, [loser.ControllerUserId] = south });

        Assert.Equal(PrivateObjectiveAssignmentStatus.Revealed, next.PrivateObjectives[0].Status);
    }

    [Fact]
    public void ForceStatusGainedAfterALostPrerequisiteCompletes()
    {
        var shaken = Guid.NewGuid();
        var inspired = Guid.NewGuid();
        var type = Automatic(
            "Recover",
            PrivateObjectiveAutomaticKind.ForceStatus,
            forceStatusTypeIds: [inspired],
            statusMatchKind: PrivateObjectiveStatusMatchKind.GainedAfter,
            prerequisiteForceStatusTypeId: shaken,
            prerequisiteWasLost: true);
        var player = Guid.NewGuid();
        var force = new CampaignForce(Guid.NewGuid(), player, Guid.NewGuid(), Guid.NewGuid(), false);
        var assignment = Assigned(type, PrivateObjectiveHolderKind.Player, player);
        var now = DateTimeOffset.UtcNow;
        var state = CampaignPlayState.Empty.With(
            forces: [force],
            forceStatusChanges:
            [
                new ForceStatusChangeFact(
                    Guid.NewGuid(),
                    force.Id,
                    force.FactionId,
                    player,
                    shaken,
                    previousStatusName: null,
                    "Shaken",
                    force.Id,
                    force.FactionId,
                    player,
                    now,
                    previousStatusTypeId: null),
                new ForceStatusChangeFact(
                    Guid.NewGuid(),
                    force.Id,
                    force.FactionId,
                    player,
                    statusTypeId: null,
                    "Shaken",
                    nextStatusName: null,
                    force.Id,
                    force.FactionId,
                    player,
                    now.AddMinutes(1),
                    shaken),
                new ForceStatusChangeFact(
                    Guid.NewGuid(),
                    force.Id,
                    force.FactionId,
                    player,
                    inspired,
                    previousStatusName: null,
                    "Inspired",
                    force.Id,
                    force.FactionId,
                    player,
                    now.AddMinutes(2),
                    previousStatusTypeId: null),
            ],
            privateObjectives: [assignment]);

        var next = Evaluate(state, type, []);

        Assert.Equal(PrivateObjectiveAssignmentStatus.Revealed, next.PrivateObjectives[0].Status);
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

    private static PrivateObjectiveTypePlayRules Automatic(
        string name,
        PrivateObjectiveAutomaticKind kind,
        int requiredCount = 1,
        Guid? structureTypeId = null,
        bool matchesAnyStructureType = false,
        Guid? itemObjectiveTypeId = null,
        PrivateObjectiveTargetKind targetKind = PrivateObjectiveTargetKind.None,
        PrivateObjectiveTargetSelection targetSelection = PrivateObjectiveTargetSelection.Specific,
        IReadOnlyList<Guid>? forceStatusTypeIds = null,
        PrivateObjectiveStatusMatchKind statusMatchKind = PrivateObjectiveStatusMatchKind.None,
        Guid? prerequisiteForceStatusTypeId = null,
        bool prerequisiteWasLost = false)
    {
        return new PrivateObjectiveTypePlayRules(
            Guid.NewGuid(),
            name,
            4,
            [PrivateObjectiveHolderKind.Player],
            PrivateObjectiveScoringKind.Automatic,
            kind,
            requiredCount,
            structureTypeId,
            [],
            matchesAnyStructureType,
            itemObjectiveTypeId,
            matchesAnyItemObjective: itemObjectiveTypeId is null
                && kind is PrivateObjectiveAutomaticKind.AdjacentToRelic or PrivateObjectiveAutomaticKind.ControlRelic,
            targetKind,
            targetSelection,
            targetId: null,
            forceStatusTypeIds,
            statusMatchKind,
            prerequisiteForceStatusTypeId,
            prerequisiteWasLost);
    }

    private static PrivateObjectiveAssignment Assigned(
        PrivateObjectiveTypePlayRules type,
        PrivateObjectiveHolderKind holderKind,
        Guid holderId)
    {
        return new PrivateObjectiveAssignment(
            Guid.NewGuid(),
            type.Id,
            holderKind,
            holderId,
            PrivateObjectiveScoringKind.Automatic,
            PrivateObjectiveAssignmentStatus.Assigned,
            DateTimeOffset.UtcNow);
    }

    private static CampaignBattle Battle(Guid winnerForceId, Guid loserForceId, DateTimeOffset createdUtc)
    {
        return new CampaignBattle(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            BattleStatus.Finalized,
            [winnerForceId, loserForceId],
            winnerForceId,
            isDraw: false,
            createdUtc);
    }

    private static CampaignPlayState Evaluate(
        CampaignPlayState state,
        PrivateObjectiveTypePlayRules type,
        IReadOnlyList<PrivateObjectiveTerritory> territories,
        IReadOnlyDictionary<Guid, Guid>? factionByPlayer = null,
        PlayMap? map = null)
    {
        return PrivateObjectiveRules.EvaluateAutomatic(
            state,
            [type],
            territories,
            factionByPlayer ?? new Dictionary<Guid, Guid>(),
            new Dictionary<Guid, Guid?>(),
            new HashSet<Guid>(),
            DateTimeOffset.UtcNow,
            map);
    }
}
