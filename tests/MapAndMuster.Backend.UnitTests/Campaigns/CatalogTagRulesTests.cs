using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Common;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class CatalogTagRulesTests
{
    [Fact]
    public void ParseRejectsDuplicateNamesCaseInsensitively()
    {
        var errors = new List<DomainError>();
        var parsed = CatalogTagRules.Parse(
            [
                new CatalogTagInput { Name = "Water" },
                new CatalogTagInput { Name = "water" },
            ],
            CatalogTagKind.Terrain,
            [],
            errors);

        Assert.Single(parsed);
        Assert.Contains(errors, error => error.Code == "terrainTags.duplicate");
    }

    [Fact]
    public void ParseRejectsEmptyAndOverlongNames()
    {
        var errors = new List<DomainError>();
        CatalogTagRules.Parse(
            [
                new CatalogTagInput { Name = " " },
                new CatalogTagInput { Name = new string('a', CatalogTags.NameMaxLength + 1) },
            ],
            CatalogTagKind.Mission,
            [],
            errors);

        Assert.Equal(2, errors.Count);
        Assert.All(errors, error => Assert.Equal("missionTags.name.invalid", error.Code));
    }

    [Fact]
    public void ParseAssignedDropsIdsOutsideTheCatalog()
    {
        var known = Guid.NewGuid();
        var errors = new List<DomainError>();
        var assigned = CatalogTagRules.ParseAssigned(
            [known, Guid.NewGuid()],
            new HashSet<Guid> { known },
            "terrainTypes[0].tagIds",
            errors);

        Assert.Equal([known], assigned);
        Assert.Contains(errors, error => error.Code == "terrainTypes[0].tagIds.unknown");
    }

    [Fact]
    public void WithWaterMigrationAssignsWaterWhenTheLegacyFlagIsSet()
    {
        var water = Guid.NewGuid();
        var assigned = CatalogTagRules.WithWaterMigration([], "Beach", isWaterFeature: true, water);

        Assert.Equal([water], assigned);
    }

    [Fact]
    public void FactionEffectiveTagsUnionParentAndSubfactionExtras()
    {
        var parent = Guid.NewGuid();
        var extra = Guid.NewGuid();
        var faction = new FactionSetup(
            Guid.NewGuid(),
            "Daemons",
            "#111111",
            ["Nurgle"],
            allyGroupName: null,
            requiresSubfaction: true,
            clearFlagImage: false,
            tagIds: [parent],
            subfactionTags: [new SubfactionTagsSetup("Nurgle", [extra, parent])]);

        Assert.Equal([parent], faction.EffectiveTagIds(null));
        Assert.Equal([parent, extra], faction.EffectiveTagIds("Nurgle"));
    }
}
