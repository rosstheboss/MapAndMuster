using Campaign.Domain.Identity;

namespace Campaign.Backend.UnitTests.Identity;

public sealed class AccountProfileRulesTests
{
    [Fact]
    public void CollectsEveryInvalidField()
    {
        var created = AccountProfileRules.TryCreate(
            username: "ab",
            firstName: "A",
            middleInitial: "QQ",
            lastName: "",
            suffix: "PhD",
            city: "",
            region: null,
            country: "Canada",
            timeZoneId: null,
            out var username,
            out var name,
            out var location,
            out var timeZone,
            out var errors);

        Assert.False(created);
        Assert.Null(username);
        Assert.Null(name);
        Assert.Null(location);
        Assert.Null(timeZone);
        Assert.Contains(errors, error => error.Field == "username");
        Assert.Contains(errors, error => error.Field == "firstName");
        Assert.Contains(errors, error => error.Field == "middleInitial");
        Assert.Contains(errors, error => error.Field == "lastName");
        Assert.Contains(errors, error => error.Field == "suffix");
        Assert.Contains(errors, error => error.Field == "city");
        Assert.Contains(errors, error => error.Field == "region");
        Assert.Contains(errors, error => error.Field == "timeZoneId");
        Assert.DoesNotContain(errors, error => error.Field == "country");
    }

    [Fact]
    public void AcceptsACompleteProfile()
    {
        var created = AccountProfileRules.TryCreate(
            "ada",
            "Ada",
            "L",
            "Lovelace",
            "Jr.",
            "Halifax",
            "Nova Scotia",
            "Canada",
            "America/Halifax",
            out var username,
            out var name,
            out var location,
            out var timeZone,
            out var errors);

        Assert.True(created);
        Assert.Empty(errors);
        Assert.Equal("ada", username!.Value);
        Assert.Equal("Ada L. Lovelace Jr.", name!.FormatFullName());
        Assert.Equal("Halifax, Nova Scotia, Canada", location!.Format());
        Assert.Equal("America/Halifax", timeZone!.Id);
    }
}
