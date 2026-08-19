using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Play;

public sealed class ArmyListRulesTests
{
    [Fact]
    public void BlankTextIsOmitted()
    {
        Assert.True(ArmyListRules.TryNormalizeText("  \n", out var error, out var normalized));
        Assert.Null(error);
        Assert.Null(normalized);
    }

    [Fact]
    public void OversizedTextIsRejected()
    {
        Assert.False(ArmyListRules.TryNormalizeText(new string('a', ArmyListRules.TextMaxLength + 1), out var error, out _));
        Assert.Equal("armyListText.length", error.Code);
    }

    [Fact]
    public void BuilderNamesMapToKnownValues()
    {
        Assert.Equal(ArmyListBuilder.NewRecruit, ArmyListRules.ParseBuilder("NewRecruit"));
        Assert.Equal(ArmyListBuilder.OldWorldBuilder, ArmyListRules.ParseBuilder("Old World Builder"));
        Assert.Equal(ArmyListBuilder.Other, ArmyListRules.ParseBuilder("Unknown"));
    }
}
