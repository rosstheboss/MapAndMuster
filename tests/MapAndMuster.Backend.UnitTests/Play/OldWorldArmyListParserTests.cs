using MapAndMuster.Domain.Play;

namespace MapAndMuster.Backend.UnitTests.Play;

public sealed class OldWorldArmyListParserTests
{
    private const string OldWorldBuilderList =
        """
        ===
        Frontier Host [1500 points]
        The Old World, North Kingdom, Open War
        ===

        ++ Characters [185 points] ++

        General [145 points]
        - Hand weapon
        - Shield

        Wizard [40 points]
        - Level 1

        ++ Core [240 points] ++

        20 Spearmen [120 points]
        - Full Command

        20 Spearmen [120 points]

        ++ Special [295 points] ++

        10 Knights [195 points]
        - Full Command

        Cannon [100 points]

        ++ Rare [200 points] ++

        Great Cannon [100 points]
        Steam Engine [100 points]

        ---
        Created with "Old World Builder"

        [https://old-world-builder.com]
        """;

    private const string NewRecruitList =
        """
        ++ North Kingdom (Warhammer: The Old World) [1,500pts] ++

        + Characters + [185pts]

        General [1] [145pts]
        . Hand weapon
        . Shield
        Wizard [1] [40pts]
        . Level 1

        + Core + [240pts]

        Spearmen [20] [120pts]
        . Full Command
        Spearmen [20] [120pts]

        + Special + [295pts]

        Knights [10] [195pts]
        . Full Command
        Cannon [1] [100pts]

        + Rare + [200pts]

        Great Cannon [1] [100pts]
        Steam Engine [1] [100pts]

        ++ Total: [1,500pts] ++

        Created with New Recruit
        https://www.newrecruit.eu
        """;

    [Fact]
    public void OtherBuilderDoesNotParse()
    {
        var result = OldWorldArmyListParser.Parse(OldWorldBuilderList, ArmyListBuilder.Other);
        Assert.False(result.Parsed);
    }

    [Fact]
    public void OldWorldBuilderFillsArmyPointsAndSupplyCategories()
    {
        var result = OldWorldArmyListParser.Parse(OldWorldBuilderList, ArmyListBuilder.OldWorldBuilder);
        Assert.True(result.Parsed);
        Assert.Equal(1500, result.ArmyPoints);
        Assert.Equal(4, result.SupplyCostingUnitCount);
        Assert.Equal(0, Category(result, "Characters").SupplyPoints);
        Assert.Equal(2, Category(result, "Characters").UnitCount);
        Assert.Equal(2, Category(result, "Special").SupplyPoints);
        Assert.Equal(2, Category(result, "Rare").SupplyPoints);
        Assert.False(Category(result, "Core").CostsSupply);
    }

    [Fact]
    public void NewRecruitFillsArmyPointsAndSupplyCategories()
    {
        var result = OldWorldArmyListParser.Parse(NewRecruitList, ArmyListBuilder.NewRecruit);
        Assert.True(result.Parsed);
        Assert.Equal(1500, result.ArmyPoints);
        Assert.Equal(4, result.SupplyCostingUnitCount);
        Assert.Equal(2, Category(result, "Special").UnitCount);
        Assert.Equal(2, Category(result, "Rare").SupplyPoints);
    }

    [Fact]
    public void WrongBuilderFails()
    {
        Assert.False(OldWorldArmyListParser.Parse(OldWorldBuilderList, ArmyListBuilder.NewRecruit).Parsed);
        Assert.False(OldWorldArmyListParser.Parse(NewRecruitList, ArmyListBuilder.OldWorldBuilder).Parsed);
    }

    [Fact]
    public void UnrecognizedTextFails()
    {
        var result = OldWorldArmyListParser.Parse("20 Spearmen and a cannon", ArmyListBuilder.NewRecruit);
        Assert.False(result.Parsed);
    }

    [Fact]
    public void EmptyTextFails()
    {
        Assert.False(OldWorldArmyListParser.Parse("  ", ArmyListBuilder.OldWorldBuilder).Parsed);
    }

    private static ArmyListSupplyCategory Category(ArmyListParseResult result, string name)
    {
        return result.Categories.Single(category => category.Name == name);
    }
}
