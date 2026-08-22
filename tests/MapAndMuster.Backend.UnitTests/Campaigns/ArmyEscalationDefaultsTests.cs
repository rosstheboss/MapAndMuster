using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class ArmyEscalationDefaultsTests
{
    [Fact]
    public void ForRoundCountUsesGenericValues()
    {
        var rows = ArmyEscalationDefaults.ForRoundCount(8);

        Assert.Equal(8, rows.Count);
        Assert.All(
            rows,
            row =>
            {
                Assert.Equal(1000, row.MaxArmyPoints);
                Assert.Equal(1, row.FreeSupplyPoints);
                Assert.Equal(1, row.FreeCharacterCount);
            });
        Assert.Equal(Enumerable.Range(1, 8), rows.Select(row => row.RoundNumber));
    }

    [Fact]
    public void PadToRoundCountKeepsOverlappingRowsAndFillsTheRest()
    {
        IReadOnlyList<RoundArmyEscalationSetup> existing =
        [
            new RoundArmyEscalationSetup(1, 500, 2, 3),
            new RoundArmyEscalationSetup(2, 750, 2, 3),
            new RoundArmyEscalationSetup(3, 900, 2, 3),
        ];

        var rows = ArmyEscalationDefaults.PadToRoundCount(existing, 8);

        Assert.Equal(8, rows.Count);
        Assert.Equal(500, rows[0].MaxArmyPoints);
        Assert.Equal(2, rows[0].FreeSupplyPoints);
        Assert.Equal(3, rows[0].FreeCharacterCount);
        Assert.Equal(900, rows[2].MaxArmyPoints);
        Assert.Equal(1000, rows[3].MaxArmyPoints);
        Assert.Equal(1, rows[3].FreeSupplyPoints);
        Assert.Equal(1, rows[3].FreeCharacterCount);
        Assert.Equal(1000, rows[7].MaxArmyPoints);
    }
}
