using System.Net;
using System.Text;
using Campaign.Infrastructure.Email;

namespace Campaign.Api.IntegrationTests;

public sealed class ResendEmailSenderTests
{
    [Fact]
    public async Task SendsPlainTextMessageWithoutRequiringALiveProvider()
    {
        var handler = new StubHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = new ResendEmailSender(client, new EmailOptions
        {
            Provider = EmailProviders.Resend,
            FromAddress = "campaign@example.test",
            FromName = "Campaign",
            Resend = { ApiKey = "re_not_a_real_secret" },
        });

        await sender.SendAsync(new EmailMessage("ada@example.test", "Subject", "Body"), CancellationToken.None);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.EndsWith("/emails", handler.Request.RequestUri?.ToString(), StringComparison.Ordinal);
        Assert.Equal("Bearer re_not_a_real_secret", handler.Authorization);
        Assert.Contains("campaign@example.test", handler.Body, StringComparison.Ordinal);
        Assert.Contains("ada@example.test", handler.Body, StringComparison.Ordinal);
        Assert.Contains("Subject", handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("re_not_a_real_secret", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NamesTheApiKeyConfigurationWhenMissing()
    {
        var handler = new StubHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = new ResendEmailSender(client, new EmailOptions
        {
            Provider = EmailProviders.Resend,
            FromAddress = "campaign@example.test",
            Resend = { ApiKey = "" },
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(new EmailMessage("ada@example.test", "Subject", "Body"), CancellationToken.None));

        Assert.Contains(ResendEmailSender.ApiKeyConfigurationKey, exception.Message, StringComparison.Ordinal);
        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task FailedProviderResponsesDoNotIncludeTheApiKey()
    {
        var handler = new StubHandler { Status = HttpStatusCode.Unauthorized };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = new ResendEmailSender(client, new EmailOptions
        {
            Provider = EmailProviders.Resend,
            FromAddress = "campaign@example.test",
            Resend = { ApiKey = "re_not_a_real_secret" },
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(new EmailMessage("ada@example.test", "Subject", "Body"), CancellationToken.None));

        Assert.Contains("401", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("re_not_a_real_secret", exception.Message, StringComparison.Ordinal);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Authorization { get; private set; }

        public string Body { get; private set; } = string.Empty;

        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Authorization = request.Headers.Authorization?.ToString();
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(Status)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
        }
    }
}
