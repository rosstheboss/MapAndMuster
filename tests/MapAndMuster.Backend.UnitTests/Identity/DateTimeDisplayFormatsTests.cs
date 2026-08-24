using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Backend.UnitTests.Identity;

public sealed class DateTimeDisplayFormatsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BlankValuesBecomeTheDefault(string? value)
    {
        var parsed = DateTimeDisplayFormats.TryParse(value, out var error, out var format);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.Equal(DateTimeDisplayFormat.MonthDayYear12h, format);
    }

    [Theory]
    [InlineData("MonthDayYear12h", DateTimeDisplayFormat.MonthDayYear12h)]
    [InlineData("daymonthyear12h", DateTimeDisplayFormat.DayMonthYear12h)]
    [InlineData("IsoSortable24h", DateTimeDisplayFormat.IsoSortable24h)]
    [InlineData("NumericEu24h", DateTimeDisplayFormat.NumericEu24h)]
    public void AcceptsDefinedFormats(string value, DateTimeDisplayFormat expected)
    {
        var parsed = DateTimeDisplayFormats.TryParse(value, out var error, out var format);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.Equal(expected, format);
    }

    [Fact]
    public void RejectsUnknownFormats()
    {
        var parsed = DateTimeDisplayFormats.TryParse("not-a-format", out var error, out var format);

        Assert.False(parsed);
        Assert.Equal(DateTimeDisplayFormat.MonthDayYear12h, format);
        Assert.NotNull(error);
        Assert.Equal("dateTimeDisplayFormat", error.Field);
        Assert.Equal("profile.dateTimeDisplayFormat.invalid", error.Code);
    }
}
