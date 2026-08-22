using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Backend.UnitTests.Identity;

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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsMissingUsernames(string? value)
    {
        var created = Username.TryCreate(value, out var username, out var error);

        Assert.False(created);
        Assert.Null(username);
        Assert.NotNull(error);
        Assert.Equal("username.invalid", error.Code);
        Assert.Equal("Username is not filled in.", error.Message);
    }

    [Fact]
    public void RejectsProhibitedLanguageWithADedicatedCode()
    {
        var created = Username.TryCreate("fuckyou", out var username, out var error);

        Assert.False(created);
        Assert.Null(username);
        Assert.NotNull(error);
        Assert.Equal("username.prohibited", error.Code);
    }

    [Theory]
    [InlineData("everyone")]
    [InlineData("Everyone")]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("here")]
    [InlineData("admin")]
    [InlineData("channel")]
    public void RejectsReservedChatAndSystemKeywords(string value)
    {
        var created = Username.TryCreate(value, out var username, out var error);

        Assert.False(created);
        Assert.Null(username);
        Assert.NotNull(error);
        Assert.Equal("username.reserved", error.Code);
        Assert.Equal("That username is reserved.", error.Message);
        Assert.Equal("username", error.Field);
    }

    [Fact]
    public void EqualityIgnoresCase()
    {
        Assert.True(Username.TryCreate("Ada", out var left, out _));
        Assert.True(Username.TryCreate("ada", out var right, out _));

        Assert.Equal(left, right);
    }
}
