using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class ForceStatusRulesTests
{
    private static readonly Guid ForceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid FactionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid TerritoryId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void HoldEnablesWellRestedAndClearsShaken()
    {
        var catalog = Catalog();
        var shaken = Force(statusName: "Shaken");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([shaken], catalog, facts));
        Assert.Equal("Well Rested", next.StatusName);
    }

    [Fact]
    public void BattleLossEnablesShakenInsteadOfExhausted()
    {
        var catalog = Catalog();
        var force = Force();
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(fought: true, won: false, lost: true, retreated: true, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Equal("Shaken", next.StatusName);
    }

    [Fact]
    public void BattleWinEnablesConfidentAndClearsWellRested()
    {
        var catalog = Catalog();
        var rested = Force(statusName: "Well Rested");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(fought: true, won: true, lost: false, retreated: false, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([rested], catalog, facts));
        Assert.Equal("Confident", next.StatusName);
    }

    [Fact]
    public void DrawEnablesExhaustedWhenNotShakenOrConfident()
    {
        var catalog = Catalog();
        var force = Force();
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromBattle(fought: true, won: false, lost: false, retreated: false, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Equal("Exhausted", next.StatusName);
    }

    [Fact]
    public void DiseasedStaysOnWaterEvenAfterHold()
    {
        var catalog = Catalog();
        var diseased = Force(statusName: "Diseased");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: true),
        };

        var next = Assert.Single(ForceStatusRules.Apply([diseased], catalog, facts));
        Assert.Equal("Diseased", next.StatusName);
    }

    [Fact]
    public void OccupyingWaterEnablesDiseasedFromNormal()
    {
        var catalog = Catalog();
        var force = Force();
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Move, occupiesWater: true),
        };

        var next = Assert.Single(ForceStatusRules.Apply([force], catalog, facts));
        Assert.Equal("Diseased", next.StatusName);
    }

    [Fact]
    public void HoldOffWaterClearsDiseasedThenAppliesWellRested()
    {
        var catalog = Catalog();
        var diseased = Force(statusName: "Diseased");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([diseased], catalog, facts));
        Assert.Equal("Well Rested", next.StatusName);
    }

    [Fact]
    public void EmptyCatalogLeavesStatusesUnchanged()
    {
        var shaken = Force(statusName: "Shaken");
        var facts = new Dictionary<Guid, ForceStatusRules.Facts>
        {
            [ForceId] = ForceStatusRules.FromAction(ActionKind.Hold, occupiesWater: false),
        };

        var next = Assert.Single(ForceStatusRules.Apply([shaken], [], facts));
        Assert.Equal("Shaken", next.StatusName);
    }

    private static CampaignForce Force(string? statusName = null)
    {
        return new CampaignForce(ForceId, UserId, FactionId, TerritoryId, false, statusName);
    }

    private static IReadOnlyList<ForceStatusSetup> Catalog()
    {
        return
        [
            .. ForceStatusCatalog.Standard.Select(status => new ForceStatusSetup(
                Guid.NewGuid(),
                status.Name,
                status.Effects,
                status.EnableTrigger,
                status.ClearTrigger)),
        ];
    }
}
