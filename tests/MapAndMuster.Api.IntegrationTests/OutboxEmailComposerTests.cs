using MapAndMuster.Infrastructure.Email;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class OutboxEmailComposerTests
{
    [Fact]
    public void BuildsConfirmationLinkFromThePublicOrigin()
    {
        var message = OutboxEmailComposer.Compose(
            EmailOutbox.ConfirmEmailType,
            new OutboxEmailPayload
            {
                Email = "ada@example.test",
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Token = "secret-token",
            },
            new PublicWebOptions { Origin = "https://campaign.example.test/" });

        Assert.Equal("ada@example.test", message.To);
        Assert.Equal("Confirm your campaign account", message.Subject);
        Assert.Contains("https://campaign.example.test/confirm-email?userId=11111111-1111-1111-1111-111111111111", message.Body, StringComparison.Ordinal);
        Assert.Contains("token=secret-token", message.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildsNoticeLinkWithoutCopyingTheBodyIntoTheSubject()
    {
        var message = OutboxEmailComposer.Compose(
            EmailOutbox.UserNoticeType,
            new OutboxEmailPayload
            {
                Email = "ada@example.test",
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Subject = "Phase opened",
                Body = "A new phase is ready.",
                Path = "/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            },
            new PublicWebOptions { Origin = "https://campaign.example.test" });

        Assert.Equal("Phase opened", message.Subject);
        Assert.Contains("A new phase is ready.", message.Body, StringComparison.Ordinal);
        Assert.Contains("https://campaign.example.test/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", message.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void SmtpProviderIsSelectedWhenResendIsNotConfigured()
    {
        var smtp = new EmailOptions { Provider = EmailProviders.Smtp, SmtpHost = "localhost" };
        var resend = new EmailOptions { Provider = EmailProviders.Resend, Resend = { ApiKey = "re_not_a_real_secret" } };
        var emptyResend = new EmailOptions { Provider = EmailProviders.Resend };

        Assert.True(smtp.IsDeliveryConfigured);
        Assert.False(smtp.UsesResend);
        Assert.True(resend.UsesResend);
        Assert.True(resend.IsDeliveryConfigured);
        Assert.False(emptyResend.IsDeliveryConfigured);
    }

    [Fact]
    public void FormatsFromAddressesWithoutHeaderInjection()
    {
        var formatted = EmailAddressFormatter.FormatFrom(new EmailOptions
        {
            FromAddress = "campaign@example.test",
            FromName = "Campaign\nBcc: evildoer@example.test",
        });

        Assert.Equal("CampaignBcc: evildoer@example.test <campaign@example.test>", formatted);
        Assert.DoesNotContain('\n', formatted);
    }
}
