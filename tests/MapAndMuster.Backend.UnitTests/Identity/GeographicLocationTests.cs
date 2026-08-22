using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Backend.UnitTests.Identity;

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

    [Fact]
    public void OptionalLocationAllowsCountryOnly()
    {
        Assert.True(GeographicLocation.TryNormalizeOptional(
            null,
            null,
            "Canada",
            out var city,
            out var region,
            out var country,
            out var errors));
        Assert.Empty(errors);
        Assert.Null(city);
        Assert.Null(region);
        Assert.Equal("Canada", country);
        Assert.Equal("Canada", GeographicLocation.FormatOptional(null, null, "Canada"));
    }

    [Fact]
    public void OptionalLocationRequiresStateWhenCityIsProvided()
    {
        Assert.False(GeographicLocation.TryNormalizeOptional(
            "Halifax",
            null,
            "Canada",
            out _,
            out _,
            out _,
            out var errors));
        Assert.Contains(errors, error => error.Code == "region.invalid");
    }

    [Fact]
    public void OptionalLocationRequiresCountryWhenStateIsProvided()
    {
        Assert.False(GeographicLocation.TryNormalizeOptional(
            null,
            "Nova Scotia",
            null,
            out _,
            out _,
            out _,
            out var errors));
        Assert.Contains(errors, error => error.Code == "country.invalid");
    }
}
