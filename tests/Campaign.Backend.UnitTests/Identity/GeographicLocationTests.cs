using Campaign.Domain.Identity;

namespace Campaign.Backend.UnitTests.Identity;

public sealed class GeographicLocationTests
{
    [Fact]
    public void FormatsCityAndCountryWithoutRegion()
    {
        Assert.True(GeographicLocation.TryCreate("Halifax", null, "Canada", out var location, out _));
        Assert.Equal("Halifax, Canada", location!.Format());
    }

    [Fact]
    public void FormatsCityRegionAndCountry()
    {
        Assert.True(GeographicLocation.TryCreate("Austin", "Texas", "United States", out var location, out _));
        Assert.Equal("Austin, Texas, United States", location!.Format());
    }

    [Theory]
    [InlineData(null, null, "Canada", "city.invalid")]
    [InlineData("Halifax", null, null, "country.invalid")]
    [InlineData("Halifax<iframe>", null, "Canada", "city.invalid")]
    public void RejectsInvalidLocations(string? city, string? region, string? country, string expectedCode)
    {
        var created = GeographicLocation.TryCreate(city, region, country, out var location, out var error);

        Assert.False(created);
        Assert.Null(location);
        Assert.NotNull(error);
        Assert.Equal(expectedCode, error.Code);
    }
}
