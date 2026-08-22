using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Backend.UnitTests.Identity;

public sealed class NameSuffixTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyInputMeansNoSuffix(string? raw)
    {
        Assert.True(NameSuffix.TryCreateOptional(raw, out var suffix, out var error));
        Assert.Null(suffix);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("Jr.", "Jr.")]
    [InlineData("jr", "Jr.")]
    [InlineData("Sr", "Sr.")]
    [InlineData("iii", "III")]
    [InlineData("X", "X")]
    public void AcceptsCanonicalSuffixes(string raw, string expected)
    {
        Assert.True(NameSuffix.TryCreateOptional(raw, out var suffix, out var error));
        Assert.Equal(expected, suffix);
        Assert.Null(error);
    }

    [Fact]
    public void RejectsUnknownSuffixes()
    {
        Assert.False(NameSuffix.TryCreateOptional("PhD", out var suffix, out var error));
        Assert.Null(suffix);
        Assert.NotNull(error);
        Assert.Equal("suffix.invalid", error.Code);
    }
}
