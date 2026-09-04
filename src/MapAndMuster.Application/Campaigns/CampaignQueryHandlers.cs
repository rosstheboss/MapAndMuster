using MapAndMuster.Application.Common;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Notifications;
using MapAndMuster.Application.Play;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Lists campaigns the caller manages or participates in.
/// </summary>
public sealed class ListCampaignsHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    public ListCampaignsHandler(ICampaignStore campaigns, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _clock = clock;
    }

    /// <summary>
    /// Returns the caller's campaigns.
    /// </summary>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The campaign list.</returns>
    public async Task<OperationResult<IReadOnlyList<CampaignListItem>>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var campaigns = await _campaigns.ListForUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var items = campaigns.Select(campaign => CampaignMapper.ToListItem(campaign, userId, _clock.UtcNow)).ToArray();
        return OperationResults.Success<IReadOnlyList<CampaignListItem>>(items);
    }
}

/// <summary>
/// Lists campaigns a signed-in user may discover: upcoming campaigns, publicly viewable
/// active and completed campaigns, plus campaigns they belong to.
/// </summary>
public sealed class ListDiscoverableCampaignsHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    public ListDiscoverableCampaignsHandler(ICampaignStore campaigns, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _clock = clock;
    }

    /// <summary>
    /// Returns discoverable campaigns for the caller.
    /// </summary>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The campaign list.</returns>
    public async Task<OperationResult<IReadOnlyList<CampaignListItem>>> HandleAsync(
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var campaigns = await _campaigns.ListDiscoverableAsync(userId, isAdministrator, utcNow, cancellationToken)
            .ConfigureAwait(false);
        var items = campaigns
            .Where(campaign => CampaignAccess.CanList(campaign, userId, isAdministrator, utcNow))
            .Select(campaign => CampaignMapper.ToListItem(campaign, userId, utcNow, isAdministrator))
            .ToArray();
        return OperationResults.Success<IReadOnlyList<CampaignListItem>>(items);
    }
}

/// <summary>
/// Reads a campaign the caller manages or participates in.
/// </summary>
public sealed class GetCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly IUserAccountStore _accounts;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="accounts">The user account store.</param>
    public GetCampaignHandler(ICampaignStore campaigns, IClock clock, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accounts);
        _campaigns = campaigns;
        _clock = clock;
        _accounts = accounts;
    }

    /// <summary>
    /// Returns campaign metadata for a member. Non-members receive not-found.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <returns>The campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        Guid campaignId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var names = await CampaignPlayMapper.UsernamesAsync(campaign, _accounts, cancellationToken).ConfigureAwait(false);
        var participants = await CampaignPlayMapper.ParticipantsAsync(campaign, _accounts, cancellationToken)
            .ConfigureAwait(false);
        var members = CampaignPlayMapper.ToChatMembers(participants);
        var inspect = CampaignChatContext.CanInspectPrivateChat(isAdministrator, userId, campaign.PlayState);
        var membership = CampaignMapper.MembershipFor(campaign, userId);
        var staffView = (membership?.IsGameMaster == true || isAdministrator)
            && campaign.PlayState?.DebugActorUserId is not null;
        return OperationResults.Success(CampaignMapper.ToDetail(
            campaign,
            userId,
            _clock.UtcNow,
            CampaignPlayMapper.ToLogEntries(campaign, names, userId, inspect),
            members,
            CampaignChatContext.Channels(campaign, userId, members),
            inspect,
            participants,
            staffView,
            isAdministrator));
    }
}

/// <summary>
/// Reads the campaign log and chat the caller is allowed to see.
/// </summary>
public sealed class GetCampaignLogHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IUserAccountStore _accounts;
    private readonly ICampaignLogReadStore _reads;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="accounts">The user account store.</param>
    /// <param name="reads">The log last-read store.</param>
    public GetCampaignLogHandler(ICampaignStore campaigns, IUserAccountStore accounts, ICampaignLogReadStore reads)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(reads);
        _campaigns = campaigns;
        _accounts = accounts;
        _reads = reads;
    }

    /// <summary>
    /// Returns the campaign log for a viewer. Non-viewers receive not-found.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <returns>The campaign log.</returns>
    public async Task<OperationResult<CampaignLogDetail>> HandleAsync(
        Guid campaignId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<CampaignLogDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var names = await CampaignPlayMapper.UsernamesAsync(campaign, _accounts, cancellationToken).ConfigureAwait(false);
        var participants = await CampaignPlayMapper.ParticipantsAsync(campaign, _accounts, cancellationToken)
            .ConfigureAwait(false);
        var members = CampaignPlayMapper.ToChatMembers(participants);
        var inspect = CampaignChatContext.CanInspectPrivateChat(isAdministrator, userId, campaign.PlayState);
        var membership = CampaignMapper.MembershipFor(campaign, userId);
        var lastReadUtc = await _reads.GetLastReadUtcAsync(campaign.Id, userId, cancellationToken).ConfigureAwait(false);
        var chatMembers = members
            .Select(static member => new CampaignChatMember(member.UserId, member.Username, member.DisplayName))
            .ToArray();
        var unread = CampaignChatRules.CountUnread(
            CampaignPlayMapper.VisiblePlayLogEntries(campaign, userId, inspect),
            userId,
            lastReadUtc,
            chatMembers);
        return OperationResults.Success(new CampaignLogDetail
        {
            Id = campaign.Id,
            Revision = campaign.Revision,
            CanChat = membership is not null,
            CanInspectPrivateChat = inspect,
            MentionableMembers = members,
            ChatChannels = CampaignChatContext.Channels(campaign, userId, members),
            Log = CampaignPlayMapper.ToLogEntries(campaign, names, userId, inspect),
            LastReadUtc = lastReadUtc,
            UnreadMentionCount = unread.MentionCount,
            UnreadPrivateCount = unread.PrivateCount,
        });
    }
}

/// <summary>
/// Records when a viewer marked the campaign log read. Does not change campaign revision.
/// </summary>
public sealed class MarkCampaignLogReadHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignLogReadStore _reads;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="reads">The log last-read store.</param>
    /// <param name="clock">The authoritative clock.</param>
    public MarkCampaignLogReadHandler(ICampaignStore campaigns, ICampaignLogReadStore reads, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(reads);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _reads = reads;
        _clock = clock;
    }

    /// <summary>
    /// Marks the campaign log read when the caller may view it. Non-viewers receive not-found.
    /// </summary>
    public async Task<OperationResult> HandleAsync(
        Guid campaignId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResult.Failure(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        await _reads.MarkReadAsync(campaign.Id, userId, _clock.UtcNow, cancellationToken).ConfigureAwait(false);
        return OperationResult.Success();
    }
}

/// <summary>
/// Closes a campaign while keeping its final stored state for logs and duplication.
/// </summary>
public sealed class EndCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly CampaignNotificationPublisher _notifications;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public EndCampaignHandler(
        ICampaignStore campaigns,
        IClock clock,
        CampaignNotificationPublisher notifications)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(notifications);
        _campaigns = campaigns;
        _clock = clock;
        _notifications = notifications;
    }

    /// <summary>
    /// Closes the campaign when the caller is a manager or administrator.
    /// </summary>
    public async Task<OperationResult> HandleAsync(EndCampaignCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResult.Failure(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!CampaignAccess.CanStaffMembers(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResult.Failure(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager or administrator can end this campaign.");
        }

        if (campaign.ClosedUtc is not null)
        {
            return OperationResult.Success();
        }

        if (command.ExpectedRevision is { } expected && campaign.Revision != expected)
        {
            return OperationResult.Failure(
                ErrorCodes.ConcurrencyConflict,
                "The campaign was updated by another request. Reload and try again.");
        }

        var utcNow = _clock.UtcNow;
        var play = campaign.PlayState is null
            ? null
            : campaign.PlayState.AppendLog(new PlayLogEntry(
                Guid.NewGuid(),
                utcNow,
                PlayLogKind.CampaignClosed,
                null,
                null,
                command.UserId,
                null,
                null,
                null,
                null,
                []));
        var updated = CampaignMapClone.CloneWithClosed(campaign, utcNow, utcNow, play);
        var outcome = await _campaigns
            .UpdateAsync(updated, command.ExpectedRevision ?? campaign.Revision, cancellationToken)
            .ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResult.Failure(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The campaign could not be ended.");
        }

        await _notifications.PublishPlayAdvanceAsync(campaign, outcome.Campaign, cancellationToken)
            .ConfigureAwait(false);
        return OperationResult.Success();
    }
}

/// <summary>
/// Replaces the single campaign map image.
/// </summary>
public sealed class UploadCampaignMapHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignMapProcessor _processor;
    private readonly ICampaignMapStorage _maps;
    private readonly IClock _clock;
    private readonly ICampaignPresetStore? _presets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="processor">The map processor.</param>
    /// <param name="maps">The map storage.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="presets">The campaign-preset store used to keep shared maps.</param>
    public UploadCampaignMapHandler(
        ICampaignStore campaigns,
        ICampaignMapProcessor processor,
        ICampaignMapStorage maps,
        IClock clock,
        ICampaignPresetStore? presets = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _maps = maps;
        _clock = clock;
        _presets = presets;
    }

    /// <summary>
    /// Replaces the campaign map after validating and re-encoding the upload.
    /// </summary>
    /// <param name="command">The upload command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        UploadCampaignMapCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        var membership = existing is null ? null : CampaignMapper.MembershipFor(existing, command.UserId);
        if (existing is null || membership is null)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!membership.IsGameMaster)
        {
            return OperationResults.Failure<CampaignDetail>(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager can replace the campaign map.");
        }

        if (CampaignLifecycle.HasLaunched(existing, _clock.UtcNow))
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignLocked, CampaignLifecycle.LockedMessage);
        }

        var processed = await _processor
            .ProcessAsync(command.Content, command.ContentType, command.Length, cancellationToken)
            .ConfigureAwait(false);
        if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                processed.ErrorCode ?? ErrorCodes.UploadInvalidImage,
                processed.Message ?? "The campaign map could not be processed.");
        }

        var newKey = await _maps.SaveAsync(processed.Content, processed.FileExtension, cancellationToken).ConfigureAwait(false);
        var previousKey = existing.MapStorageKey;
        var updated = CampaignMapClone.CloneWithMap(existing, newKey, _clock.UtcNow);
        var outcome = await _campaigns.UpdateAsync(updated, command.ExpectedRevision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            await _maps.DeleteAsync(newKey, cancellationToken).ConfigureAwait(false);
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The campaign map could not be saved.");
        }

        if (CatalogFileBinder.IsUserUploadedFileKey(previousKey))
        {
            await CampaignAssetRetention.DeleteIfUnreferencedAsync(
                _campaigns,
                _maps.DeleteAsync,
                previousKey,
                command.CampaignId,
                cancellationToken,
                _presets).ConfigureAwait(false);
        }

        return OperationResults.Success(CampaignMapper.ToDetail(outcome.Campaign, command.UserId, _clock.UtcNow));
    }
}

/// <summary>
/// Opens a campaign map for a member.
/// </summary>
public sealed class GetCampaignMapHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignMapStorage _maps;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="maps">The map storage.</param>
    public GetCampaignMapHandler(ICampaignStore campaigns, ICampaignMapStorage maps)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(maps);
        _campaigns = campaigns;
        _maps = maps;
    }

    /// <summary>
    /// Returns the stored map for a member.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <returns>The stored map.</returns>
    public async Task<OperationResult<StoredCampaignMap>> HandleAsync(
        Guid campaignId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<StoredCampaignMap>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (string.IsNullOrWhiteSpace(campaign.MapStorageKey))
        {
            return OperationResults.Failure<StoredCampaignMap>(ErrorCodes.CampaignNotFound, "The campaign map was not found.");
        }

        var file = await _maps.OpenReadAsync(campaign.MapStorageKey, cancellationToken).ConfigureAwait(false);
        if (file is null)
        {
            return OperationResults.Failure<StoredCampaignMap>(ErrorCodes.CampaignNotFound, "The campaign map was not found.");
        }

        return OperationResults.Success(file);
    }
}

internal static class CampaignMapClone
{
    public static StoredCampaign CloneWithMap(StoredCampaign existing, string mapStorageKey, DateTimeOffset updatedUtc)
    {
        return new StoredCampaign
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            PlayerSlotCount = existing.PlayerSlotCount,
            IsPrivate = existing.IsPrivate,
            IsPubliclyViewable = existing.IsPubliclyViewable,
            JoinPasswordHash = existing.JoinPasswordHash,
            CreatorIsParticipant = existing.CreatorIsParticipant,
            City = existing.City,
            Region = existing.Region,
            Country = existing.Country,
            MapStorageKey = mapStorageKey,
            Revision = existing.Revision,
            CreatedUtc = existing.CreatedUtc,
            UpdatedUtc = updatedUtc,
            CreatedByUserId = existing.CreatedByUserId,
            Memberships = existing.Memberships,
            Factions = existing.Factions,
            AllyGroups = existing.AllyGroups,
            Links = existing.Links,
            TimeZoneId = existing.TimeZoneId,
            StartsUtc = existing.StartsUtc,
            EndsUtc = existing.EndsUtc,
            ClosedUtc = existing.ClosedUtc,
            RoundCount = existing.RoundCount,
            RoundLengthAmount = existing.RoundLengthAmount,
            RoundLengthUnit = existing.RoundLengthUnit,
            Phases = existing.Phases,
            MapGraph = existing.MapGraph,
            TerrainTypes = existing.TerrainTypes,
            StructureTypes = existing.StructureTypes,
            ItemObjectiveTypes = existing.ItemObjectiveTypes,
            PublicObjectiveTypes = existing.PublicObjectiveTypes,
            SpecialRules = existing.SpecialRules,
            Missions = existing.Missions,
            ForceStatuses = existing.ForceStatuses,
            PrivateObjectiveTypes = existing.PrivateObjectiveTypes,
            BattleScoring = existing.BattleScoring,
            RankingObjectivePoints = existing.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = existing.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = existing.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = existing.BattleReportRules,
            ArmyEscalations = existing.ArmyEscalations,
            PlayState = existing.PlayState,
        };
    }

    public static StoredCampaign CloneWithCatalogs(
        StoredCampaign existing,
        IReadOnlyList<StoredTerrainType> terrainTypes,
        IReadOnlyList<StoredStructureType> structureTypes,
        DateTimeOffset updatedUtc,
        IReadOnlyList<StoredItemObjectiveType>? itemObjectiveTypes = null)
    {
        return new StoredCampaign
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            PlayerSlotCount = existing.PlayerSlotCount,
            IsPrivate = existing.IsPrivate,
            IsPubliclyViewable = existing.IsPubliclyViewable,
            JoinPasswordHash = existing.JoinPasswordHash,
            CreatorIsParticipant = existing.CreatorIsParticipant,
            City = existing.City,
            Region = existing.Region,
            Country = existing.Country,
            MapStorageKey = existing.MapStorageKey,
            Revision = existing.Revision,
            CreatedUtc = existing.CreatedUtc,
            UpdatedUtc = updatedUtc,
            CreatedByUserId = existing.CreatedByUserId,
            Memberships = existing.Memberships,
            Factions = existing.Factions,
            AllyGroups = existing.AllyGroups,
            Links = existing.Links,
            TimeZoneId = existing.TimeZoneId,
            StartsUtc = existing.StartsUtc,
            EndsUtc = existing.EndsUtc,
            ClosedUtc = existing.ClosedUtc,
            RoundCount = existing.RoundCount,
            RoundLengthAmount = existing.RoundLengthAmount,
            RoundLengthUnit = existing.RoundLengthUnit,
            Phases = existing.Phases,
            MapGraph = existing.MapGraph,
            TerrainTypes = terrainTypes,
            StructureTypes = structureTypes,
            ItemObjectiveTypes = itemObjectiveTypes ?? existing.ItemObjectiveTypes,
            PublicObjectiveTypes = existing.PublicObjectiveTypes,
            SpecialRules = existing.SpecialRules,
            Missions = existing.Missions,
            ForceStatuses = existing.ForceStatuses,
            PrivateObjectiveTypes = existing.PrivateObjectiveTypes,
            BattleScoring = existing.BattleScoring,
            RankingObjectivePoints = existing.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = existing.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = existing.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = existing.BattleReportRules,
            ArmyEscalations = existing.ArmyEscalations,
            PlayState = existing.PlayState,
        };
    }

    public static StoredCampaign CloneWithFactions(
        StoredCampaign existing,
        IReadOnlyList<StoredFaction> factions,
        DateTimeOffset updatedUtc)
    {
        return new StoredCampaign
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            PlayerSlotCount = existing.PlayerSlotCount,
            IsPrivate = existing.IsPrivate,
            IsPubliclyViewable = existing.IsPubliclyViewable,
            JoinPasswordHash = existing.JoinPasswordHash,
            CreatorIsParticipant = existing.CreatorIsParticipant,
            City = existing.City,
            Region = existing.Region,
            Country = existing.Country,
            MapStorageKey = existing.MapStorageKey,
            Revision = existing.Revision,
            CreatedUtc = existing.CreatedUtc,
            UpdatedUtc = updatedUtc,
            CreatedByUserId = existing.CreatedByUserId,
            Memberships = existing.Memberships,
            Factions = factions,
            AllyGroups = existing.AllyGroups,
            Links = existing.Links,
            TimeZoneId = existing.TimeZoneId,
            StartsUtc = existing.StartsUtc,
            EndsUtc = existing.EndsUtc,
            ClosedUtc = existing.ClosedUtc,
            RoundCount = existing.RoundCount,
            RoundLengthAmount = existing.RoundLengthAmount,
            RoundLengthUnit = existing.RoundLengthUnit,
            Phases = existing.Phases,
            MapGraph = existing.MapGraph,
            TerrainTypes = existing.TerrainTypes,
            StructureTypes = existing.StructureTypes,
            ItemObjectiveTypes = existing.ItemObjectiveTypes,
            PublicObjectiveTypes = existing.PublicObjectiveTypes,
            SpecialRules = existing.SpecialRules,
            Missions = existing.Missions,
            ForceStatuses = existing.ForceStatuses,
            PrivateObjectiveTypes = existing.PrivateObjectiveTypes,
            BattleScoring = existing.BattleScoring,
            RankingObjectivePoints = existing.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = existing.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = existing.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = existing.BattleReportRules,
            ArmyEscalations = existing.ArmyEscalations,
            PlayState = existing.PlayState,
        };
    }

    public static StoredCampaign CloneWithMemberships(
        StoredCampaign existing,
        IReadOnlyList<StoredCampaignMembership> memberships,
        DateTimeOffset updatedUtc,
        CampaignPlayState? playState = null)
    {
        return new StoredCampaign
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            PlayerSlotCount = existing.PlayerSlotCount,
            IsPrivate = existing.IsPrivate,
            IsPubliclyViewable = existing.IsPubliclyViewable,
            JoinPasswordHash = existing.JoinPasswordHash,
            CreatorIsParticipant = existing.CreatorIsParticipant,
            City = existing.City,
            Region = existing.Region,
            Country = existing.Country,
            MapStorageKey = existing.MapStorageKey,
            Revision = existing.Revision,
            CreatedUtc = existing.CreatedUtc,
            UpdatedUtc = updatedUtc,
            CreatedByUserId = existing.CreatedByUserId,
            Memberships = memberships,
            Factions = existing.Factions,
            AllyGroups = existing.AllyGroups,
            Links = existing.Links,
            TimeZoneId = existing.TimeZoneId,
            StartsUtc = existing.StartsUtc,
            EndsUtc = existing.EndsUtc,
            ClosedUtc = existing.ClosedUtc,
            RoundCount = existing.RoundCount,
            RoundLengthAmount = existing.RoundLengthAmount,
            RoundLengthUnit = existing.RoundLengthUnit,
            Phases = existing.Phases,
            MapGraph = existing.MapGraph,
            TerrainTypes = existing.TerrainTypes,
            StructureTypes = existing.StructureTypes,
            ItemObjectiveTypes = existing.ItemObjectiveTypes,
            PublicObjectiveTypes = existing.PublicObjectiveTypes,
            SpecialRules = existing.SpecialRules,
            Missions = existing.Missions,
            ForceStatuses = existing.ForceStatuses,
            PrivateObjectiveTypes = existing.PrivateObjectiveTypes,
            BattleScoring = existing.BattleScoring,
            RankingObjectivePoints = existing.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = existing.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = existing.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = existing.BattleReportRules,
            ArmyEscalations = existing.ArmyEscalations,
            PlayState = playState ?? existing.PlayState,
        };
    }

    public static StoredCampaign WithPlay(
        StoredCampaign existing,
        CampaignPlayState play,
        StoredMapGraph? graph = null)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(play);
        return new StoredCampaign
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            PlayerSlotCount = existing.PlayerSlotCount,
            IsPrivate = existing.IsPrivate,
            IsPubliclyViewable = existing.IsPubliclyViewable,
            JoinPasswordHash = existing.JoinPasswordHash,
            CreatorIsParticipant = existing.CreatorIsParticipant,
            City = existing.City,
            Region = existing.Region,
            Country = existing.Country,
            MapStorageKey = existing.MapStorageKey,
            Revision = existing.Revision,
            CreatedUtc = existing.CreatedUtc,
            UpdatedUtc = existing.UpdatedUtc,
            CreatedByUserId = existing.CreatedByUserId,
            Memberships = existing.Memberships,
            Factions = existing.Factions,
            AllyGroups = existing.AllyGroups,
            Links = existing.Links,
            TimeZoneId = existing.TimeZoneId,
            StartsUtc = existing.StartsUtc,
            EndsUtc = existing.EndsUtc,
            ClosedUtc = existing.ClosedUtc,
            RoundCount = existing.RoundCount,
            RoundLengthAmount = existing.RoundLengthAmount,
            RoundLengthUnit = existing.RoundLengthUnit,
            Phases = existing.Phases,
            MapGraph = graph ?? existing.MapGraph,
            TerrainTypes = existing.TerrainTypes,
            StructureTypes = existing.StructureTypes,
            ItemObjectiveTypes = existing.ItemObjectiveTypes,
            PublicObjectiveTypes = existing.PublicObjectiveTypes,
            SpecialRules = existing.SpecialRules,
            Missions = existing.Missions,
            ForceStatuses = existing.ForceStatuses,
            PrivateObjectiveTypes = existing.PrivateObjectiveTypes,
            BattleScoring = existing.BattleScoring,
            RankingObjectivePoints = existing.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = existing.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = existing.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = existing.BattleReportRules,
            ArmyEscalations = existing.ArmyEscalations,
            PlayState = play,
        };
    }

    public static StoredCampaign CloneWithClosed(
        StoredCampaign existing,
        DateTimeOffset closedUtc,
        DateTimeOffset updatedUtc,
        CampaignPlayState? playState = null)
    {
        ArgumentNullException.ThrowIfNull(existing);
        return new StoredCampaign
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            PlayerSlotCount = existing.PlayerSlotCount,
            IsPrivate = existing.IsPrivate,
            IsPubliclyViewable = existing.IsPubliclyViewable,
            JoinPasswordHash = existing.JoinPasswordHash,
            CreatorIsParticipant = existing.CreatorIsParticipant,
            City = existing.City,
            Region = existing.Region,
            Country = existing.Country,
            MapStorageKey = existing.MapStorageKey,
            Revision = existing.Revision,
            CreatedUtc = existing.CreatedUtc,
            UpdatedUtc = updatedUtc,
            CreatedByUserId = existing.CreatedByUserId,
            Memberships = existing.Memberships,
            Factions = existing.Factions,
            AllyGroups = existing.AllyGroups,
            Links = existing.Links,
            TimeZoneId = existing.TimeZoneId,
            StartsUtc = existing.StartsUtc,
            EndsUtc = existing.EndsUtc,
            ClosedUtc = closedUtc,
            RoundCount = existing.RoundCount,
            RoundLengthAmount = existing.RoundLengthAmount,
            RoundLengthUnit = existing.RoundLengthUnit,
            Phases = existing.Phases,
            MapGraph = existing.MapGraph,
            TerrainTypes = existing.TerrainTypes,
            StructureTypes = existing.StructureTypes,
            ItemObjectiveTypes = existing.ItemObjectiveTypes,
            PublicObjectiveTypes = existing.PublicObjectiveTypes,
            SpecialRules = existing.SpecialRules,
            Missions = existing.Missions,
            ForceStatuses = existing.ForceStatuses,
            PrivateObjectiveTypes = existing.PrivateObjectiveTypes,
            BattleScoring = existing.BattleScoring,
            RankingObjectivePoints = existing.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = existing.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = existing.SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = existing.BattleReportRules,
            ArmyEscalations = existing.ArmyEscalations,
            PlayState = playState ?? existing.PlayState,
        };
    }
}
