using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Backend.UnitTests.Identity;

public sealed class ProhibitedLanguageTests
{
    [Theory]
    [InlineData("Ada")]
    [InlineData("assistant")]
    [InlineData("classic")]
    [InlineData("Hancock")]
    [InlineData("Shelley")]
    public void AllowsOrdinaryNamesAndUsernames(string value)
    {
        Assert.False(ProhibitedLanguage.ContainsProhibitedTerm(value));
    }

    [Theory]
    [InlineData("shit")]
    [InlineData("fuck_you")]
    [InlineData("asshole")]
    [InlineData("nigger")]
    public void RejectsProhibitedTerms(string value)
    {
        Assert.True(ProhibitedLanguage.ContainsProhibitedTerm(value));
    }
}
