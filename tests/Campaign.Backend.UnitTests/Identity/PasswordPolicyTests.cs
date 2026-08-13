using Campaign.Domain.Identity;

namespace Campaign.Backend.UnitTests.Identity;

public sealed class PasswordPolicyTests
{
    [Fact]
    public void AcceptsAComplexPassword()
    {
        Assert.True(PasswordPolicy.TryValidate("Correct-Horse-1", out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null, "Password is not filled in.")]
    [InlineData("", "Password is not filled in.")]
    [InlineData("Short-1a!", "Password is too short (minimum 12 characters).")]
    [InlineData("all-lowercase-1", "an uppercase letter")]
    [InlineData("ALL-UPPERCASE-1", "a lowercase letter")]
    [InlineData("NoDigits-Here!", "a number")]
    [InlineData("NoSpecialChar12", "a special character")]
    public void RejectsPasswordsThatMissARequirement(string? password, string expectedFragment)
    {
        Assert.False(PasswordPolicy.TryValidate(password, out var error));
        Assert.NotNull(error);
        Assert.Equal("password.invalid", error.Code);
        Assert.Equal("password", error.Field);
        Assert.Contains(expectedFragment, error.Message, StringComparison.Ordinal);
    }
}
