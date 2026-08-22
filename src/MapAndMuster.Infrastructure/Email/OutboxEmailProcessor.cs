using System.Text.Json;
using MapAndMuster.Application.Ports;
using MapAndMuster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MapAndMuster.Infrastructure.Email;

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
    /// <param name="emailOptions">Email options.</param>
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
        if (!_emailOptions.Value.IsDeliveryConfigured)
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
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

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
                var mail = OutboxEmailComposer.Compose(message.Type, payload, _webOptions.Value);
                await emailSender.SendAsync(mail, cancellationToken).ConfigureAwait(false);
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

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "The email outbox processor failed a batch.")]
    private static partial void LogBatchFailure(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Identity email delivery failed for outbox message {MessageId}.")]
    private static partial void LogDeliveryFailure(ILogger logger, Guid messageId, Exception exception);
}
