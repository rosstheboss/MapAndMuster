using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Play;
using MapAndMuster.Application.Ports;
using MapAndMuster.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MapAndMuster.Infrastructure.Campaigns;

/// <summary>
/// Development-only copies of the Estalia map campaign at several live stages, filled with test players.
/// </summary>
public sealed partial class LocalTestCampaignSeeder
{
    /// <summary>Configuration key that disables Development Estalia test-campaign copies when set to false.</summary>
    public const string SeedConfigurationKey = "LocalTestData:SeedEstaliaCampaigns";

    private static readonly LocalTestCampaignStage[] Stages =
    [
        LocalTestCampaignStage.NotStarted,
        LocalTestCampaignStage.Action1,
        LocalTestCampaignStage.Action2,
        LocalTestCampaignStage.Battle,
    ];

    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ICampaignStore _campaigns;
    private readonly IUserAccountStore _accounts;
    private readonly IClock _clock;
    private readonly DuplicateCampaignHandler _duplicate;
    private readonly GetCampaignPlayHandler _play;
    private readonly ILogger<LocalTestCampaignSeeder> _logger;

    /// <summary>Initializes a Development test-campaign seeder.</summary>
    /// <param name="environment">The host environment.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="accounts">The account store.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="duplicate">The duplicate-campaign handler.</param>
    /// <param name="play">The play-board handler used to launch started copies.</param>
    /// <param name="logger">The logger.</param>
    public LocalTestCampaignSeeder(
        IHostEnvironment environment,
        IConfiguration configuration,
        ICampaignStore campaigns,
        IUserAccountStore accounts,
        IClock clock,
        DuplicateCampaignHandler duplicate,
        GetCampaignPlayHandler play,
        ILogger<LocalTestCampaignSeeder> logger)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(duplicate);
        ArgumentNullException.ThrowIfNull(play);
        ArgumentNullException.ThrowIfNull(logger);
        _environment = environment;
        _configuration = configuration;
        _campaigns = campaigns;
        _accounts = accounts;
        _clock = clock;
        _duplicate = duplicate;
        _play = play;
        _logger = logger;
    }

    /// <summary>
    /// Duplicates the manager's Estalia-map campaign into named local test copies when they are missing.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when missing copies have been considered.</returns>
    public async Task EnsureAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() || !_configuration.GetValue(SeedConfigurationKey, true))
        {
            return;
        }

        var manager = await _accounts.FindByUsernameAsync(IdentityMaintenance.PrivilegedUsername, cancellationToken)
            .ConfigureAwait(false);
        if (manager is null)
        {
            LogMissingManager(_logger, IdentityMaintenance.PrivilegedUsername);
            return;
        }

        var existing = await _campaigns.ListForUserAsync(manager.Id, cancellationToken).ConfigureAwait(false);
        var missing = Stages.Where(stage => existing.All(campaign => campaign.Name != LocalTestCampaignCopy.NameFor(stage))).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        var source = ChooseSource(existing);
        if (source is null)
        {
            LogMissingSource(_logger, IdentityMaintenance.PrivilegedUsername);
            return;
        }

        var mapped = await _campaigns.FindByIdAsync(source.Id, cancellationToken).ConfigureAwait(false);
        if (mapped?.MapGraph is null)
        {
            LogMissingSource(_logger, IdentityMaintenance.PrivilegedUsername);
            return;
        }

        source = mapped;

        var testUsers = await _accounts.ListTestAccountsAsync(cancellationToken).ConfigureAwait(false);
        if (testUsers.Count == 0)
        {
            LogMissingTestAccounts(_logger);
            return;
        }

        var utcNow = _clock.UtcNow;
        foreach (var stage in missing)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var duplicated = await _duplicate.HandleAsync(
                    new DuplicateCampaignCommand { UserId = manager.Id, CampaignId = source.Id },
                    cancellationToken)
                .ConfigureAwait(false);
            if (!duplicated.IsSuccess || duplicated.Value is null)
            {
                LogDuplicateFailed(_logger, source.Name, stage, duplicated.Message);
                continue;
            }

            var stored = await _campaigns.FindByIdAsync(duplicated.Value.Id, cancellationToken).ConfigureAwait(false);
            if (stored is null)
            {
                continue;
            }

            var configured = LocalTestCampaignCopy.Configure(stored, stage, manager.Id, testUsers, utcNow);
            var updated = await _campaigns.UpdateAsync(configured, stored.Revision, cancellationToken).ConfigureAwait(false);
            if (!updated.IsSuccess)
            {
                LogConfigureFailed(_logger, configured.Name, updated.Message);
                continue;
            }

            if (stage != LocalTestCampaignStage.NotStarted)
            {
                var play = await _play.HandleAsync(configured.Id, manager.Id, isAdministrator: true, cancellationToken)
                    .ConfigureAwait(false);
                if (!play.IsSuccess)
                {
                    LogPlayFailed(_logger, configured.Name, play.Message);
                }
            }

            LogSeeded(_logger, configured.Name, source.Name);
        }
    }

    private static StoredCampaign? ChooseSource(IReadOnlyList<StoredCampaign> campaigns)
    {
        return campaigns
            .Where(static campaign =>
                !string.IsNullOrWhiteSpace(campaign.MapStorageKey)
                && !campaign.Name.StartsWith(LocalTestCampaignCopy.NamePrefix, StringComparison.Ordinal))
            .OrderByDescending(static campaign =>
                campaign.Name.Contains("Estalia", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(static campaign => campaign.UpdatedUtc)
            .FirstOrDefault();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Skipped local Estalia test campaigns because {Username} was not found.")]
    private static partial void LogMissingManager(ILogger logger, string username);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Skipped local Estalia test campaigns because no mapped campaign managed by {Username} was found to duplicate.")]
    private static partial void LogMissingSource(ILogger logger, string username);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Skipped local Estalia test campaigns because no test accounts were seeded.")]
    private static partial void LogMissingTestAccounts(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Could not duplicate {Source} for {Stage}: {Message}")]
    private static partial void LogDuplicateFailed(ILogger logger, string source, LocalTestCampaignStage stage, string? message);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Could not configure {Name}: {Message}")]
    private static partial void LogConfigureFailed(ILogger logger, string name, string? message);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Could not launch play for {Name}: {Message}")]
    private static partial void LogPlayFailed(ILogger logger, string name, string? message);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Seeded local test campaign {Name} from {Source}.")]
    private static partial void LogSeeded(ILogger logger, string name, string source);
}
