using Campaign.Domain.Identity;

namespace Campaign.Backend.UnitTests.Identity;

public sealed class UsernameTests
{
    [Theory]
    [InlineData("Ada")]
    [InlineData("player_one")]
    [InlineData("A12")]
    [InlineData("Abcdefghijklmnopqrstuvwxyz_12345")]
    public void AcceptsValidUsernames(string value)
    {
        var created = Username.TryCreate(value, out var username, out var error);

        Assert.True(created);
        Assert.NotNull(username);
        Assert.Null(error);
        Assert.Equal(value, username.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("1player")]
    [InlineData("_player")]
    [InlineData("player-name")]
    [InlineData("player name")]
    [InlineData("player@host")]
    [InlineData("Abcdefghijklmnopqrstuvwxyz_123456")]
    public void RejectsInvalidUsernames(string? value)
    {
        var created = Username.TryCreate(value, out var username, out var error);

        Assert.False(created);
        Assert.Null(username);
        Assert.NotNull(error);
        Assert.Equal("username.invalid", error.Code);
    }

    [Fact]
    public void EqualityIgnoresCase()
    {
        Assert.True(Username.TryCreate("Ada", out var left, out _));
        Assert.True(Username.TryCreate("ada", out var right, out _));

        Assert.Equal(left, right);
    }
}
