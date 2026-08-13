using Campaign.Domain.Identity;

namespace Campaign.Backend.UnitTests.Identity;

public sealed class PersonNameTests
{
    [Fact]
    public void FormatsFullNameWithOptionalMiddleInitial()
    {
        Assert.True(PersonName.TryCreate("Jane", "q", "Public", out var withInitial, out _));
        Assert.Equal("Jane Q. Public", withInitial!.FormatFullName());

        Assert.True(PersonName.TryCreate("Jane", null, "Public", out var withoutInitial, out _));
        Assert.Equal("Jane Public", withoutInitial!.FormatFullName());

        Assert.True(PersonName.TryCreate("Jane", null, "Public", "III", out var withSuffix, out _));
        Assert.Equal("Jane Public III", withSuffix!.FormatFullName());
    }

    [Fact]
    public void RejectsSingleCharacterNames()
    {
        var created = PersonName.TryCreate("J", null, "Public", out var name, out var error);

        Assert.False(created);
        Assert.Null(name);
        Assert.NotNull(error);
        Assert.Equal("firstName.invalid", error.Code);
        Assert.Contains("too short", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, null, "Public", "firstName.invalid")]
    [InlineData("Jane", null, null, "lastName.invalid")]
    [InlineData("Jane", "QQ", "Public", "middleInitial.invalid")]
    [InlineData("Jane<script>", null, "Public", "firstName.invalid")]
    [InlineData("shit", null, "Public", "firstName.prohibited")]
    public void RejectsInvalidNames(string? firstName, string? middleInitial, string? lastName, string expectedCode)
    {
        var created = PersonName.TryCreate(firstName, middleInitial, lastName, out var name, out var error);

        Assert.False(created);
        Assert.Null(name);
        Assert.NotNull(error);
        Assert.Equal(expectedCode, error.Code);
    }
}
