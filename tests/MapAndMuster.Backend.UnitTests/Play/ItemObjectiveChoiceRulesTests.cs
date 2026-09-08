using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class ItemObjectiveChoiceRulesTests
{
    [Fact]
    public void OpeningDestroysTheItemAndSpawnsAReplacement()
    {
        var originalId = Guid.NewGuid();
        var replacementId = Guid.NewGuid();
        var choiceId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var forceId = Guid.NewGuid();
        var player = Guid.NewGuid();
        var territory = Guid.NewGuid();
        var original = new ItemObjectiveTypeSetup(
            originalId,
            "Sealed relic",
            isHiddenUntilFound: false,
            ItemObjectivePlacementKind.Random,
            allowOnSpawn: false,
            campaignPoints: 5,
            flavorText: "A sealed casket.",
            choices:
            [
                new ItemObjectiveChoiceSetup(
                    choiceId,
                    "Open",
                    [
                        new ItemObjectiveChoiceResultSetup(
                            resultId,
                            "The casket crumbles.",
                            "Opened",
                            destroyItem: true,
                            replacementId,
                            grantedPrivateObjectiveTypeId: null),
                    ]),
            ]);
        var replacement = new ItemObjectiveTypeSetup(
            replacementId,
            "Opened relic",
            isHiddenUntilFound: false,
            ItemObjectivePlacementKind.Random,
            allowOnSpawn: false,
            campaignPoints: 2,
            flavorText: "A broken casket.");
        var item = new CampaignItemObjective(
            Guid.NewGuid(),
            originalId,
            "Sealed relic",
            territoryId: null,
            possessorForceId: forceId,
            isRevealed: true,
            territory,
            wasHiddenUntilFound: false,
            "A sealed casket.");
        var state = CampaignPlayState.Empty.With(
            forces: [new CampaignForce(forceId, player, Guid.NewGuid(), territory, false)],
            itemObjectives: [item]);

        Assert.True(ItemObjectiveChoiceRules.TryResolve(
            state,
            item.Id,
            choiceId,
            player,
            [original, replacement],
            DateTimeOffset.UtcNow,
            static _ => 0,
            out var next,
            out _));

        var destroyed = Assert.Single(next.ItemObjectives, entry => entry.Id == item.Id);
        Assert.True(destroyed.IsDestroyed);
        Assert.Null(destroyed.PossessorForceId);
        var spawned = Assert.Single(next.ItemObjectives, entry => entry.TypeId == replacementId);
        Assert.Equal(forceId, spawned.PossessorForceId);
        Assert.Contains(next.Log, entry => entry.Kind == PlayLogKind.ItemObjectiveDestroyed);
    }

    [Fact]
    public void ChoiceResultCanSetForceStatusIncludingOverDiseased()
    {
        var originalId = Guid.NewGuid();
        var choiceId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var forceId = Guid.NewGuid();
        var player = Guid.NewGuid();
        var territory = Guid.NewGuid();
        var confidentId = Guid.NewGuid();
        var type = new ItemObjectiveTypeSetup(
            originalId,
            "Relic",
            isHiddenUntilFound: false,
            ItemObjectivePlacementKind.Random,
            allowOnSpawn: false,
            campaignPoints: 1,
            flavorText: "A relic.",
            choices:
            [
                new ItemObjectiveChoiceSetup(
                    choiceId,
                    "Claim",
                    [
                        new ItemObjectiveChoiceResultSetup(
                            resultId,
                            "The relic steadies the force.",
                            "Claimed",
                            destroyItem: false,
                            replacementItemTypeId: null,
                            grantedPrivateObjectiveTypeId: null,
                            setForceStatusName: "Confident"),
                    ]),
            ]);
        var catalog = new ForceStatusSetup(
            confidentId,
            "Confident",
            "Confident.",
            ForceStatusEnableTrigger.BattleWon,
            ForceStatusClearTrigger.BattleLostOrRetreat);
        var item = new CampaignItemObjective(
            Guid.NewGuid(),
            originalId,
            "Relic",
            territoryId: null,
            possessorForceId: forceId,
            isRevealed: true,
            territory,
            wasHiddenUntilFound: false,
            "A relic.");
        var state = CampaignPlayState.Empty.With(
            forces: [new CampaignForce(forceId, player, Guid.NewGuid(), territory, false, "Diseased")],
            itemObjectives: [item]);

        Assert.True(ItemObjectiveChoiceRules.TryResolve(
            state,
            item.Id,
            choiceId,
            player,
            [type],
            DateTimeOffset.UtcNow,
            static _ => 0,
            out var next,
            out _,
            [catalog]));

        Assert.Equal("Confident", Assert.Single(next.Forces).StatusName);
        Assert.Contains(next.Log, entry => entry.Kind == PlayLogKind.ForceStatusChanged);
        Assert.Contains(next.ForceStatusChanges, fact => fact.Source == ForceStatusChangeSource.ItemObjective);
    }

    [Fact]
    public void DestroyedItemsAwardNoStandingsPoints()
    {
        var typeId = Guid.NewGuid();
        var forceId = Guid.NewGuid();
        var player = Guid.NewGuid();
        var item = new CampaignItemObjective(
            Guid.NewGuid(),
            typeId,
            "Gone",
            territoryId: null,
            possessorForceId: forceId,
            isRevealed: true,
            Guid.NewGuid(),
            wasHiddenUntilFound: false,
            isDestroyed: true);
        var result = CampaignPointStandingsRules.Calculate(new CampaignPointScoringState
        {
            Players = [new CampaignPointPlayer(player, Guid.NewGuid())],
            Territories = [],
            StructurePoints = new Dictionary<Guid, int>(),
            ItemPoints = new Dictionary<Guid, int> { [typeId] = 9 },
            PublicObjectivePoints = new Dictionary<Guid, int>(),
            BattleScoring = BattleScoringSetup.Default,
            RankingObjectivePoints = GeneralPublicObjectivePoints.None,
            Battles = [],
            Forces = [new CampaignForce(forceId, player, Guid.NewGuid(), Guid.NewGuid(), false)],
            VisibleItems = [item],
            Awards = [],
        });

        var standing = Assert.Single(result.Standings);
        Assert.Equal(0, standing.OtherPoints);
        Assert.Equal(0, standing.PrivateObjectivePoints);
        Assert.Equal(0, standing.Total);
    }
}
