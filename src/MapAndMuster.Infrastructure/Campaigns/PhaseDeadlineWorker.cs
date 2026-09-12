using MapAndMuster.Application.Play;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MapAndMuster.Infrastructure.Campaigns;

/// <summary>
/// Opens and closes phase windows when their deadlines pass.
/// </summary>
/// <remarks>
/// Clients used to trigger these transitions incidentally by polling <c>GET /play</c> every few
/// seconds. With Server-Sent Events they no longer poll, so a campaign whose players are all
/// offline would otherwise sit on an expired window indefinitely. This worker sleeps until the
/// next stored deadline rather than waking on a short fixed interval, and the advance it runs is
/// idempotent and re-checks database state. See
/// <c>docs/adr/0004-server-sent-events-for-campaign-updates.md</c>.
/// </remarks>
public sealed partial class PhaseDeadlineWorker : BackgroundService
{
    /// <summary>Floor on the sleep, so a stuck deadline cannot become a spin loop.</summary>
    private static readonly TimeSpan MinDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Ceiling on the sleep. Bounds the effect of clock drift or a missed wake signal; the worker
    /// re-reads deadlines from the database at least this often.
    /// </summary>
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CampaignDeadlineSignal _signal;
    private readonly ILogger<PhaseDeadlineWorker> _logger;

    /// <summary>
    /// Initializes the worker.
    /// </summary>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="signal">Wake signal raised after a campaign write.</param>
    /// <param name="logger">The logger.</param>
    public PhaseDeadlineWorker(
        IServiceScopeFactory scopeFactory,
        CampaignDeadlineSignal signal,
        ILogger<PhaseDeadlineWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _signal = signal;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = MaxDelay;
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<AdvanceDueCampaignsHandler>();
                var next = await handler.RunAsync(stoppingToken).ConfigureAwait(false);
                if (next is { } dueUtc)
                {
                    delay = dueUtc - DateTimeOffset.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogPassFailure(_logger, exception);
            }

            if (delay < MinDelay)
            {
                delay = MinDelay;
            }
            else if (delay > MaxDelay)
            {
                delay = MaxDelay;
            }

            try
            {
                await _signal.WaitAsync(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Error,
        Message = "A phase-deadline pass failed. Deadlines stay in the database and will be retried.")]
    private static partial void LogPassFailure(ILogger logger, Exception exception);
}
