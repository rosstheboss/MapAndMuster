using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class ForceStatusConditionCollapseTests
{
    [Fact]
    public void DropsExactDuplicateFingerprints()
    {
        var hold = new ForceStatusEnableCondition(ForceStatusEnableTrigger.Hold, 1);
        var copy = new ForceStatusEnableCondition(ForceStatusEnableTrigger.Hold, 1);
        var clear = new ForceStatusClearCondition(ForceStatusClearTrigger.AfterMove, 1);
        var (enables, clears) = ForceStatusConditionCollapse.Collapse([hold, copy], [clear], _ => []);

        Assert.Single(enables);
        Assert.Single(clears);
    }

    [Fact]
    public void DropsANarrowerLocationWhenAnySubsumesIt()
    {
        var any = new ForceStatusEnableCondition(ForceStatusEnableTrigger.Hold, 1, location: ConditionLocation.Any);
        var typed = new ForceStatusEnableCondition(
            ForceStatusEnableTrigger.Hold,
            2,
            location: new ConditionLocation(ConditionLocationKind.TerrainType, Guid.NewGuid(), null));
        var clear = new ForceStatusClearCondition(ForceStatusClearTrigger.AfterMove, 1);
        var (enables, _) = ForceStatusConditionCollapse.Collapse([any, typed], [clear], _ => []);

        Assert.Equal(any.Id, Assert.Single(enables).Id);
    }

    [Fact]
    public void TagSubsumesATypeThatCurrentlyHasThatTag()
    {
        var typeId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var tag = new ForceStatusEnableCondition(
            ForceStatusEnableTrigger.Hold,
            1,
            location: new ConditionLocation(ConditionLocationKind.TerrainTag, null, tagId));
        var typed = new ForceStatusEnableCondition(
            ForceStatusEnableTrigger.Hold,
            1,
            location: new ConditionLocation(ConditionLocationKind.TerrainType, typeId, null));
        var clear = new ForceStatusClearCondition(ForceStatusClearTrigger.AfterMove, 1);
        var (enables, _) = ForceStatusConditionCollapse.Collapse(
            [tag, typed],
            [clear],
            id => id == typeId ? [tagId] : []);

        Assert.Equal(tag.Id, Assert.Single(enables).Id);
    }

    [Fact]
    public void KeepsEnableAndDropsTheMatchingClearFingerprint()
    {
        var location = new ConditionLocation(ConditionLocationKind.TerrainTag, null, Guid.NewGuid());
        var enable = new ForceStatusEnableCondition(ForceStatusEnableTrigger.Hold, 1, location: location);
        var clear = new ForceStatusClearCondition(ForceStatusClearTrigger.Hold, 1, location: location);
        var fallback = new ForceStatusClearCondition(ForceStatusClearTrigger.AfterMove, 1);
        var (enables, clears) = ForceStatusConditionCollapse.Collapse([enable], [clear, fallback], _ => []);

        Assert.Equal(enable.Id, Assert.Single(enables).Id);
        Assert.Equal(fallback.Id, Assert.Single(clears).Id);
    }

    [Fact]
    public void KeepsTheSameTriggerWhenLocationsDiffer()
    {
        var beach = Guid.NewGuid();
        var sea = Guid.NewGuid();
        var first = new ForceStatusEnableCondition(
            ForceStatusEnableTrigger.Hold,
            1,
            location: new ConditionLocation(ConditionLocationKind.TerrainType, beach, null));
        var second = new ForceStatusEnableCondition(
            ForceStatusEnableTrigger.Hold,
            1,
            location: new ConditionLocation(ConditionLocationKind.TerrainType, sea, null));
        var clear = new ForceStatusClearCondition(ForceStatusClearTrigger.AfterMove, 1);
        var (enables, _) = ForceStatusConditionCollapse.Collapse([first, second], [clear], _ => []);

        Assert.Equal(2, enables.Count);
    }
}
