using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Backend.UnitTests.Identity;

public sealed class GuestUsernamesTests
{
    [Theory]
    [InlineData("Guest001")]
    [InlineData("guest1")]
    [InlineData("Guest10000")]
    public void MatchesAllocatedGuestHandles(string value)
    {
        Assert.True(GuestUsernames.Matches(value));
    }

    [Theory]
    [InlineData("guest")]
    [InlineData("Guest")]
    [InlineData("GuestOne")]
    [InlineData("player1")]
    [InlineData("")]
    [InlineData(null)]
    public void RejectsNonGuestHandles(string? value)
    {
        Assert.False(GuestUsernames.Matches(value));
    }
}
