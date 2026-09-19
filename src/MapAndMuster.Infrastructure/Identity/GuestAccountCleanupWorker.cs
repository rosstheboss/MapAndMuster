using MapAndMuster.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MapAndMuster.Infrastructure.Identity;

/// <summary>
/// Returns expired guest numbers to the pool so preview sessions cannot accumulate.
/// </summary>
public sealed partial class GuestAccountCleanupWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GuestAccountCleanupWorker> _logger;

    /// <summary>Initializes the worker.</summary>
    public GuestAccountCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<GuestAccountCleanupWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
                await accounts.RecycleExpiredGuestAccountsAsync(stoppingToken).ConfigureAwait(false);
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

            try
            {
                await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Guest-account cleanup failed.")]
    private static partial void LogPassFailure(ILogger logger, Exception exception);
}
