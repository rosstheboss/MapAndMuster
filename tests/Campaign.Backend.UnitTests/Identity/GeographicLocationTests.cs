using Campaign.Domain.Identity;

namespace Campaign.Backend.UnitTests.Identity;

public sealed class GeographicLocationTests
{
    [Fact]
    public void FormatsCityRegionAndCountry()
    {
        Assert.True(GeographicLocation.TryCreate("Austin", "Texas", "United States", out var location, out _));
        Assert.Equal("Austin, Texas, United States", location!.Format());
    }

    [Fact]
    public void RejectsAMissingRegion()
    {
        var created = GeographicLocation.TryCreate("Halifax", null, "Canada", out var location, out var error);

        Assert.False(created);
        Assert.Null(location);
        Assert.NotNull(error);
        Assert.Equal("region.invalid", error.Code);
    }

    [Theory]
    [InlineData(null, "Nova Scotia", "Canada", "city.invalid")]
    [InlineData("Halifax", null, "Canada", "region.invalid")]
    [InlineData("Halifax", "Nova Scotia", null, "country.invalid")]
    [InlineData("Halifax<iframe>", "Nova Scotia", "Canada", "city.invalid")]
    public void RejectsInvalidLocations(string? city, string? region, string? country, string expectedCode)
    {
        var created = GeographicLocation.TryCreate(city, region, country, out var location, out var error);

        Assert.False(created);
        Assert.Null(location);
        Assert.NotNull(error);
        Assert.Equal(expectedCode, error.Code);
    }
}
