using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MapAndMuster.Infrastructure.Email;

/// <summary>
/// Delivers mail through the Resend HTTPS API. Does not log API keys or message bodies.
/// </summary>
public sealed class ResendEmailSender : IEmailSender
{
    /// <summary>
    /// Named HTTP client used for Resend.
    /// </summary>
    public const string HttpClientName = "resend";

    /// <summary>
    /// Configuration key named in errors when the API key is missing.
    /// </summary>
    public const string ApiKeyConfigurationKey = "Email:Resend:ApiKey";

    private readonly HttpClient _httpClient;
    private readonly EmailOptions _options;

    /// <summary>
    /// Initializes the sender.
    /// </summary>
    /// <param name="httpClient">The HTTP client. Base address should be https://api.resend.com/.</param>
    /// <param name="options">Email options.</param>
    public ResendEmailSender(HttpClient httpClient, EmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (string.IsNullOrWhiteSpace(_options.Resend.ApiKey))
        {
            throw new InvalidOperationException($"Missing required configuration key {ApiKeyConfigurationKey}.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new ResendSendRequest(
                EmailAddressFormatter.FormatFrom(_options),
                [message.To],
                message.Subject,
                message.Body)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Resend.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Resend rejected the message with HTTP {(int)response.StatusCode}.");
        }
    }

    private sealed record ResendSendRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("text")] string Text);
}
