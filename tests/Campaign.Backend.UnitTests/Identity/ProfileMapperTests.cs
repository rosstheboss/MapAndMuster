using Campaign.Application.Identity;
using Campaign.Domain.Identity;

namespace Campaign.Backend.UnitTests.Identity;

public sealed class ProfileMapperTests
{
    [Fact]
    public void PublicProfileOmitsEmailTimestampsAndLegalNameWhenUsernameIsSelected()
    {
        var account = CreateAccount(DisplayNameMode.Username);

        var publicProfile = ProfileMapper.ToPublic(account);

        Assert.Equal("ada", publicProfile.Username);
        Assert.Equal("ada", publicProfile.DisplayName);
        Assert.False(publicProfile.ShowsFullName);
        Assert.Equal("Halifax", publicProfile.City);
        Assert.Equal("Nova Scotia", publicProfile.Region);
        Assert.Equal("Canada", publicProfile.Country);
        Assert.True(publicProfile.HasAvatar);

        var serialized = System.Text.Json.JsonSerializer.Serialize(publicProfile);
        Assert.DoesNotContain("ada@example.test", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CreatedUtc", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdatedUtc", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("TimeZoneId", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("America/Halifax", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("Lovelace", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("Email", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicProfileUsesFullNameWhenOwnerOptsIn()
    {
        var account = CreateAccount(DisplayNameMode.FullName);

        var publicProfile = ProfileMapper.ToPublic(account);

        Assert.Equal("ada", publicProfile.Username);
        Assert.Equal("Ada L. Lovelace", publicProfile.DisplayName);
        Assert.True(publicProfile.ShowsFullName);
    }

    private static UserAccount CreateAccount(DisplayNameMode displayNameMode)
    {
        return new UserAccount
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "ada@example.test",
            Username = "ada",
            FirstName = "Ada",
            MiddleInitial = 'L',
            LastName = "Lovelace",
            City = "Halifax",
            Region = "Nova Scotia",
            Country = "Canada",
            TimeZoneId = "America/Halifax",
            DisplayNameMode = displayNameMode,
            AvatarStorageKey = "avatars/abc.jpg",
            CreatedUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedUtc = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            ProfileRevision = 3,
            EmailConfirmed = true,
        };
    }
}
