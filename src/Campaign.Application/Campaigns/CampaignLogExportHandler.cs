using Campaign.Application.Common;
using Campaign.Application.Play;
using Campaign.Application.Ports;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Downloads public chat and/or game-log facts for a campaign manager, administrator,
/// or a later outbound sender. Private chats are never included.
/// </summary>
public sealed class ExportCampaignLogHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IUserAccountStore _accounts;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="accounts">The user account store.</param>
    public ExportCampaignLogHandler(ICampaignStore campaigns, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _accounts = accounts;
    }

    /// <summary>
    /// Returns a text or CSV file of the selected public log sources.
    /// </summary>
    public async Task<OperationResult<ExportedCampaignLog>> HandleAsync(
        ExportCampaignLogCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.IncludePublicChat && !command.IncludeGameLog)
        {
            return OperationResults.Failure<ExportedCampaignLog>(
                ErrorCodes.ValidationFailed,
                "Choose public chat, the game log, or both.");
        }

        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResults.Failure<ExportedCampaignLog>(
                ErrorCodes.CampaignNotFound,
                "The campaign was not found.");
        }

        if (!CampaignAccess.CanStaffMembers(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResults.Failure<ExportedCampaignLog>(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager or administrator can download the campaign log.");
        }

        var names = await CampaignPlayMapper.UsernamesAsync(campaign, _accounts, cancellationToken)
            .ConfigureAwait(false);
        var entries = CampaignLogExport.Select(
            CampaignPlayMapper.ToLogEntries(campaign, names, command.UserId, inspectPrivateChat: false),
            command.IncludePublicChat,
            command.IncludeGameLog);
        return OperationResults.Success(CampaignLogExport.Write(
            campaign.Name,
            campaign.TimeZoneId,
            entries,
            command.Format));
    }
}
