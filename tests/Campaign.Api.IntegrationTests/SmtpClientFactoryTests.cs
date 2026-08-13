using Campaign.Infrastructure.Email;

namespace Campaign.Api.IntegrationTests;

public sealed class SmtpClientFactoryTests
{
    [Fact]
    public void CreatesUnauthenticatedClientForLocalCatchers()
    {
        using var client = SmtpClientFactory.Create(new EmailOptions
        {
            SmtpHost = "localhost",
            SmtpPort = 1025,
            EnableSsl = false,
        });

        Assert.Equal("localhost", client.Host);
        Assert.Equal(1025, client.Port);
        Assert.False(client.EnableSsl);
        Assert.Null(client.Credentials);
    }

    [Fact]
    public void AppliesCredentialsAndSslWhenConfigured()
    {
        using var client = SmtpClientFactory.Create(new EmailOptions
        {
            SmtpHost = "smtp.example.test",
            SmtpPort = 587,
            EnableSsl = true,
            SmtpUsername = "user@example.test",
            SmtpPassword = "not-a-real-secret",
        });

        Assert.Equal("smtp.example.test", client.Host);
        Assert.Equal(587, client.Port);
        Assert.True(client.EnableSsl);
        Assert.NotNull(client.Credentials);
    }
}
