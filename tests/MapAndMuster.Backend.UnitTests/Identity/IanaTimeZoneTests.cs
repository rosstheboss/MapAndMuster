using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Backend.UnitTests.Identity;

public sealed class IanaTimeZoneTests
{
    [Fact]
    public void RequiredInputRejectsEmpty()
    {
        Assert.False(IanaTimeZone.TryCreate(null, out var timeZone, out var error));
        Assert.Null(timeZone);
        Assert.NotNull(error);
        Assert.Equal("timeZone.invalid", error.Code);
        Assert.Equal("Time zone is not filled in.", error.Message);
    }

    [Fact]
    public void EmptyOptionalInputLeavesThePreferenceUnset()
    {
        Assert.True(IanaTimeZone.TryCreateOptional(null, out var timeZone, out var error));
        Assert.Null(timeZone);
        Assert.Null(error);
        Assert.Equal(IanaTimeZone.UtcId, IanaTimeZone.DisplayId(timeZone));
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("utc")]
    public void AcceptsUtc(string raw)
    {
        Assert.True(IanaTimeZone.TryCreateOptional(raw, out var timeZone, out var error));
        Assert.NotNull(timeZone);
        Assert.Null(error);
        Assert.Equal(IanaTimeZone.UtcId, timeZone.Id);
    }

    [Fact]
    public void AcceptsANamedIanaZone()
    {
        Assert.True(IanaTimeZone.TryCreateOptional("America/Halifax", out var timeZone, out var error));
        Assert.NotNull(timeZone);
        Assert.Null(error);
        Assert.Equal("America/Halifax", timeZone.Id);
    }

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("America/Halifax<script>")]
    public void RejectsUnknownZones(string raw)
    {
        Assert.False(IanaTimeZone.TryCreateOptional(raw, out var timeZone, out var error));
        Assert.Null(timeZone);
        Assert.NotNull(error);
        Assert.Equal("timeZone.invalid", error.Code);
    }
}
