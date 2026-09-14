using MapAndMuster.Application.Campaigns;
using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Backend.UnitTests.Campaigns;

public sealed class FactionAppearanceTests
{
    [Fact]
    public void ResolveDoesNotInheritTheParentLogoForARequiredSubfaction()
    {
        var resolved = FactionAppearance.Resolve(Daemons(), "Khorne");

        Assert.Equal("#AD1457", resolved.Color);
        Assert.False(resolved.HasFlagImage);
        Assert.Null(resolved.FlagImageStorageKey);
    }

    [Fact]
    public void ResolveUsesARequiredSubfactionColorFlag()
    {
        var resolved = FactionAppearance.Resolve(
            Daemons(
                requiresSubfaction: true,
                appearances:
                [
                    new StoredSubfactionAppearance
                    {
                        Name = "Khorne",
                        Color = "#B91C1C",
                        FlagSource = SubfactionFlagSource.Color,
                    },
                ]),
            "Khorne");

        Assert.Equal("#B91C1C", resolved.Color);
        Assert.False(resolved.HasFlagImage);
        Assert.Null(resolved.FlagImageStorageKey);
    }

    [Fact]
    public void ResolveInheritsTheParentLogoForAnOptionalSubfaction()
    {
        var resolved = FactionAppearance.Resolve(Daemons(requiresSubfaction: false), "Khorne");

        Assert.True(resolved.HasFlagImage);
        Assert.Equal("daemons-flag", resolved.FlagImageStorageKey);
    }

    private static StoredFaction Daemons(
        bool requiresSubfaction = true,
        IReadOnlyList<StoredSubfactionAppearance>? appearances = null)
    {
        return new StoredFaction
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Daemons of Chaos",
            Color = "#AD1457",
            Subfactions = ["Khorne"],
            RequiresSubfaction = requiresSubfaction,
            FlagImageStorageKey = "daemons-flag",
            TintFlagImage = true,
            SubfactionAppearances = appearances ?? [],
        };
    }
}
