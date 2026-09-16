using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Play;
using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class HuntInEstaliaSpecialRuleBinderTests
{
    private static readonly Guid ViewerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid EmpireId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
    private static readonly Guid DaemonsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02");
    private static readonly Guid NorthId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03");
    private static readonly Guid PreparedId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01");
    private static readonly Guid BloodId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02");
    private static readonly Guid CustomId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc03");
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EmptyAssignmentsTakeMatchingCatalogNames()
    {
        var catalog = Catalog();
        var empire = Faction("Empire of Man", EmpireId);
        var daemons = Faction("Daemons of Chaos", DaemonsId, ["Khorne"]);

        Assert.Equal([PreparedId], HuntInEstaliaSpecialRuleBinder.FactionRuleIds(empire, catalog));
        Assert.Equal(
            [BloodId],
            Assert.Single(HuntInEstaliaSpecialRuleBinder.SubfactionRuleAssignments(daemons, catalog)).SpecialRuleIds);
    }

    [Fact]
    public void StoredIdentifiersWinOverHuntNames()
    {
        var catalog = Catalog(new StoredSpecialRule
        {
            Id = CustomId,
            Name = "Custom March",
            Text = "A fictional extra order.",
        });
        var empire = Faction("Empire of Man", EmpireId, specialRuleIds: [CustomId]);

        Assert.Equal([CustomId], HuntInEstaliaSpecialRuleBinder.FactionRuleIds(empire, catalog));
    }

    [Fact]
    public void StaleIdentifiersFallBackToHuntNames()
    {
        var catalog = Catalog();
        var empire = Faction("Empire of Man", EmpireId, specialRuleIds: [Guid.NewGuid()]);

        Assert.Equal([PreparedId], HuntInEstaliaSpecialRuleBinder.FactionRuleIds(empire, catalog));
    }

    [Fact]
    public void UnknownFactionNamesStayUnassigned()
    {
        var catalog = Catalog();
        var north = Faction("North", NorthId);

        Assert.Empty(HuntInEstaliaSpecialRuleBinder.FactionRuleIds(north, catalog));
    }

    [Fact]
    public void CampaignDetailAndPlayCatalogExposeHydratedAssignments()
    {
        var campaign = Campaign(
            [
                Faction("Empire of Man", EmpireId),
                Faction("Daemons of Chaos", DaemonsId, ["Khorne"]),
            ],
            Catalog());

        var detail = CampaignMapper.ToDetail(campaign, ViewerId, Now);
        Assert.Equal([PreparedId], Assert.Single(detail.Factions, faction => faction.Id == EmpireId).SpecialRuleIds);
        Assert.Equal(
            [BloodId],
            Assert.Single(
                Assert.Single(detail.Factions, faction => faction.Id == DaemonsId).SubfactionSpecialRules).SpecialRuleIds);

        var rules = CampaignPlayCatalog.SpecialRules(campaign);
        Assert.True(rules.Has(EmpireId, null, SpecialRuleEffectKeys.PreparedForBattle));
        Assert.True(rules.Has(DaemonsId, "Khorne", SpecialRuleEffectKeys.OnlyBloodSatisfies));
    }

    private static IReadOnlyList<StoredSpecialRule> Catalog(params StoredSpecialRule[] extra)
    {
        return
        [
            new StoredSpecialRule
            {
                Id = PreparedId,
                Name = "Prepared for Battle",
                Text = "Ready before the first clash.",
                EffectKey = SpecialRuleEffectKeys.PreparedForBattle,
            },
            new StoredSpecialRule
            {
                Id = BloodId,
                Name = "Only Blood Satisfies!",
                Text = "A fictional blood rite.",
                EffectKey = SpecialRuleEffectKeys.OnlyBloodSatisfies,
            },
            .. extra,
        ];
    }

    private static StoredFaction Faction(
        string name,
        Guid id,
        IReadOnlyList<string>? subfactions = null,
        IReadOnlyList<Guid>? specialRuleIds = null)
    {
        return new StoredFaction
        {
            Id = id,
            Name = name,
            Color = "#2563EB",
            Subfactions = subfactions ?? [],
            RequiresSubfaction = false,
            SpecialRuleIds = specialRuleIds ?? [],
        };
    }

    private static StoredCampaign Campaign(
        IReadOnlyList<StoredFaction> factions,
        IReadOnlyList<StoredSpecialRule> specialRules)
    {
        return new StoredCampaign
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Border War",
            Description = "A contested frontier.",
            PlayerSlotCount = 8,
            IsPrivate = true,
            IsPubliclyViewable = false,
            CreatorIsParticipant = true,
            Revision = 1,
            CreatedUtc = Now,
            UpdatedUtc = Now,
            CreatedByUserId = ViewerId,
            Memberships = [],
            Factions = factions,
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = Now,
            EndsUtc = Now.AddDays(40),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases = [new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" }],
            TerrainTypes = [],
            StructureTypes = [],
            SpecialRules = specialRules,
        };
    }
}
