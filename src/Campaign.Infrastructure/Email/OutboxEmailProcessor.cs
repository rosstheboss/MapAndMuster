using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Campaign.Application.Ports;
using Campaign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Campaign.Infrastructure.Email;

/// <summary>
/// Delivers queued identity emails. Delivery failure does not roll back account state.
/// </summary>
public sealed partial class OutboxEmailProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<EmailOptions> _emailOptions;
    private readonly IOptions<PublicWebOptions> _webOptions;
    private readonly ILogger<OutboxEmailProcessor> _logger;

    /// <summary>
    /// Initializes the processor.
    /// </summary>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="emailOptions">SMTP options.</param>
    /// <param name="webOptions">Public web origin options.</param>
    /// <param name="logger">The logger.</param>
    public OutboxEmailProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailOptions> emailOptions,
        IOptions<PublicWebOptions> webOptions,
        ILogger<OutboxEmailProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(emailOptions);
        ArgumentNullException.ThrowIfNull(webOptions);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _emailOptions = emailOptions;
        _webOptions = webOptions;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.Value.SmtpHost))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogBatchFailure(_logger, exception);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var pending = await dbContext.OutboxMessages
            .Where(message => message.ProcessedUtc == null)
            .OrderBy(message => message.CreatedUtc)
            .Take(20)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var message in pending)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<OutboxEmailPayload>(message.Payload)
                    ?? throw new InvalidOperationException("The outbox payload was empty.");
                using var client = CreateClient();
                using var mail = CreateMail(message.Type, payload);
                await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
                message.ProcessedUtc = clock.UtcNow;
                message.LastError = null;
            }
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                message.LastError = "Delivery failed.";
                LogDeliveryFailure(_logger, message.Id, exception);
            }
        }

        if (pending.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

#pragma warning disable SYSLIB0014
    private SmtpClient CreateClient()
    {
        return new SmtpClient(_emailOptions.Value.SmtpHost, _emailOptions.Value.SmtpPort)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
    }
#pragma warning restore SYSLIB0014

    private MailMessage CreateMail(string type, OutboxEmailPayload payload)
    {
        var origin = _webOptions.Value.Origin.TrimEnd('/');
        var encodedToken = WebUtility.UrlEncode(payload.Token);
        string subject;
        string body;
        if (type == EmailOutbox.ConfirmEmailType)
        {
            subject = "Confirm your campaign account";
            var link = $"{origin}/confirm-email?userId={payload.UserId}&token={encodedToken}";
            body = $"Confirm your email by opening this link: {link}";
        }
        else
        {
            subject = "Reset your campaign password";
            var link = $"{origin}/reset-password?userId={payload.UserId}&token={encodedToken}";
            body = $"Reset your password by opening this link: {link}";
        }

        return new MailMessage(_emailOptions.Value.FromAddress, payload.Email, subject, body);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "The email outbox processor failed a batch.")]
    private static partial void LogBatchFailure(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Identity email delivery failed for outbox message {MessageId}.")]
    private static partial void LogDeliveryFailure(ILogger logger, Guid messageId, Exception exception);
}
