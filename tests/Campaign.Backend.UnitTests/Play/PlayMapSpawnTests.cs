using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class PlayMapSpawnTests
{
    private static readonly Guid Daemons = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10");
    private static readonly Guid KhorneLand = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid NurgleLand = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void SpawnForPrefersTheMatchingRequiredSubfaction()
    {
        var map = new PlayMap(
            [
                new PlayTerritory(KhorneLand, 1, Daemons, Daemons, null, null, StructureCondition.Operational, spawnSubfaction: "Khorne"),
                new PlayTerritory(NurgleLand, 2, Daemons, Daemons, null, null, StructureCondition.Operational, spawnSubfaction: "Nurgle"),
            ],
            []);

        Assert.Equal(KhorneLand, map.SpawnFor(Daemons, "Khorne")?.Id);
        Assert.Equal(NurgleLand, map.SpawnFor(Daemons, "Nurgle")?.Id);
        Assert.Equal(KhorneLand, map.SpawnFor(Daemons)?.Id);
    }

    [Fact]
    public void CanEnterTreatsRequiredSubfactionSpawnsAsSeparateFactions()
    {
        var khorne = new CampaignForce(Guid.NewGuid(), Guid.NewGuid(), Daemons, KhorneLand, false, subfaction: "Khorne");
        var map = new PlayMap(
            [
                new PlayTerritory(KhorneLand, 1, Daemons, Daemons, null, null, StructureCondition.Operational, spawnSubfaction: "Khorne"),
                new PlayTerritory(NurgleLand, 2, Daemons, Daemons, null, null, StructureCondition.Operational, spawnSubfaction: "Nurgle"),
            ],
            [(KhorneLand, NurgleLand)]);

        Assert.True(FactionSpecialRulePolicies.CanEnter(map, khorne, KhorneLand));
        Assert.False(FactionSpecialRulePolicies.CanEnter(map, khorne, NurgleLand));
        Assert.True(FactionSpecialRulePolicies.IsEnemySpawn(map.Territory(NurgleLand)!, khorne));
    }
}
