using MapAndMuster.Application.Common;
using MapAndMuster.Application.Play;
using MapAndMuster.Application.Ports;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Uploads a custom structure logo for a campaign manager.
/// </summary>
public sealed class UploadStructureImageHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignMapProcessor _processor;
    private readonly ICampaignAssetStorage _assets;
    private readonly IClock _clock;
    private readonly ICampaignPresetStore? _presets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="processor">The image processor.</param>
    /// <param name="assets">The asset storage.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="presets">The campaign-preset store used to keep shared logos.</param>
    public UploadStructureImageHandler(
        ICampaignStore campaigns,
        ICampaignMapProcessor processor,
        ICampaignAssetStorage assets,
        IClock clock,
        ICampaignPresetStore? presets = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _assets = assets;
        _clock = clock;
        _presets = presets;
    }

    /// <summary>
    /// Replaces the structure logo after validating and re-encoding the upload.
    /// </summary>
    /// <param name="command">The upload command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        UploadStructureImageCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var access = await CatalogAssetAccess.RequireManagerAsync(_campaigns, command.CampaignId, command.UserId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(access.ErrorCode ?? ErrorCodes.CampaignNotFound, access.Message ?? "The campaign was not found.");
        }

        var processed = await _processor
            .ProcessAsync(
                command.Content,
                command.ContentType,
                command.Length,
                cancellationToken,
                ICampaignMapProcessor.StructureLogoMaxDimension)
            .ConfigureAwait(false);
        if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                processed.ErrorCode ?? ErrorCodes.UploadInvalidImage,
                processed.Message ?? "The structure image could not be processed.");
        }

        var structures = access.Campaign.StructureTypes.ToList();
        var index = structures.FindIndex(type => type.Id == command.StructureTypeId);
        if (index < 0)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The structure type was not found.");
        }

        var newKey = await _assets
            .SaveAsync("structures", processed.Content, processed.FileExtension, "image/png", cancellationToken)
            .ConfigureAwait(false);
        var previousKey = command.Pillaged ? structures[index].PillagedImageStorageKey : structures[index].ImageStorageKey;
        structures[index] = new StoredStructureType
        {
            Id = structures[index].Id,
            Name = structures[index].Name,
            BuiltinSymbol = structures[index].BuiltinSymbol,
            ImageStorageKey = command.Pillaged ? structures[index].ImageStorageKey : newKey,
            PillagedImageStorageKey = command.Pillaged ? newKey : structures[index].PillagedImageStorageKey,
            IsBuildable = structures[index].IsBuildable,
            IsPillageable = structures[index].IsPillageable,
            IsDestructible = structures[index].IsDestructible,
            Missions = structures[index].Missions,
            CampaignPoints = structures[index].CampaignPoints,
        };

        var updated = CampaignMapClone.CloneWithCatalogs(access.Campaign, access.Campaign.TerrainTypes, structures, _clock.UtcNow);
        var outcome = await _campaigns.UpdateAsync(updated, command.ExpectedRevision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            await _assets.DeleteAsync(newKey, cancellationToken).ConfigureAwait(false);
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The structure image could not be saved.");
        }

        if (CatalogFileBinder.IsUserUploadedFileKey(previousKey))
        {
            await CampaignAssetRetention.DeleteIfUnreferencedAsync(
                _campaigns,
                _assets.DeleteAsync,
                previousKey,
                command.CampaignId,
                cancellationToken,
                _presets).ConfigureAwait(false);
        }

        return OperationResults.Success(CampaignMapper.ToDetail(outcome.Campaign, command.UserId, _clock.UtcNow));
    }
}

/// <summary>
/// Opens a stored structure logo for a campaign member.
/// </summary>
public sealed class GetStructureImageHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignAssetStorage _assets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="assets">The asset storage.</param>
    public GetStructureImageHandler(ICampaignStore campaigns, ICampaignAssetStorage assets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(assets);
        _campaigns = campaigns;
        _assets = assets;
    }

    /// <summary>
    /// Returns the stored structure logo for a member.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="structureTypeId">The structure type identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <param name="pillaged">Whether to return the pillaged logo instead of the operational logo.</param>
    /// <param name="ifNoneMatch">The caller's cached asset tag, if any.</param>
    /// <returns>The stored image.</returns>
    public async Task<OperationResult<CampaignAssetRead>> HandleAsync(
        Guid campaignId,
        Guid structureTypeId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false,
        bool pillaged = false,
        string? ifNoneMatch = null)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var structure = campaign.StructureTypes.FirstOrDefault(type => type.Id == structureTypeId);
        var storageKey = pillaged ? structure?.PillagedImageStorageKey : structure?.ImageStorageKey;
        if (structure is null || string.IsNullOrWhiteSpace(storageKey))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The structure image was not found.");
        }

        var read = await CampaignAssetReader
            .ReadAsync(_assets.OpenStreamAsync, storageKey, ifNoneMatch, downloadName: null, cancellationToken)
            .ConfigureAwait(false);
        return read is null
            ? OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The structure image was not found.")
            : OperationResults.Success(read);
    }
}

/// <summary>
/// Uploads a custom item-objective logo for a campaign manager.
/// </summary>
public sealed class UploadItemObjectiveImageHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignMapProcessor _processor;
    private readonly ICampaignAssetStorage _assets;
    private readonly IClock _clock;
    private readonly ICampaignPresetStore? _presets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public UploadItemObjectiveImageHandler(
        ICampaignStore campaigns,
        ICampaignMapProcessor processor,
        ICampaignAssetStorage assets,
        IClock clock,
        ICampaignPresetStore? presets = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _assets = assets;
        _clock = clock;
        _presets = presets;
    }

    /// <summary>
    /// Replaces the item-objective logo after validating and re-encoding the upload.
    /// </summary>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        UploadItemObjectiveImageCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var access = await CatalogAssetAccess.RequireManagerAsync(_campaigns, command.CampaignId, command.UserId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(access.ErrorCode ?? ErrorCodes.CampaignNotFound, access.Message ?? "The campaign was not found.");
        }

        var processed = await _processor
            .ProcessAsync(
                command.Content,
                command.ContentType,
                command.Length,
                cancellationToken,
                ICampaignMapProcessor.StructureLogoMaxDimension)
            .ConfigureAwait(false);
        if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                processed.ErrorCode ?? ErrorCodes.UploadInvalidImage,
                processed.Message ?? "The item objective image could not be processed.");
        }

        var items = access.Campaign.ItemObjectiveTypes.ToList();
        var index = items.FindIndex(type => type.Id == command.ItemObjectiveTypeId);
        if (index < 0)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The item objective type was not found.");
        }

        var newKey = await _assets
            .SaveAsync("items", processed.Content, processed.FileExtension, "image/png", cancellationToken)
            .ConfigureAwait(false);
        var previousKey = items[index].ImageStorageKey;
        items[index] = new StoredItemObjectiveType
        {
            Id = items[index].Id,
            Name = items[index].Name,
            IsHiddenUntilFound = items[index].IsHiddenUntilFound,
            Placement = items[index].Placement,
            AllowOnSpawn = items[index].AllowOnSpawn,
            BuiltinSymbol = items[index].BuiltinSymbol,
            Color = items[index].Color,
            ImageStorageKey = newKey,
            CampaignPoints = items[index].CampaignPoints,
            FlavorText = items[index].FlavorText,
            Choices = items[index].Choices,
            SpecialRuleIds = items[index].SpecialRuleIds,
            Effects = items[index].Effects,
        };

        var updated = CampaignMapClone.CloneWithCatalogs(
            access.Campaign,
            access.Campaign.TerrainTypes,
            access.Campaign.StructureTypes,
            _clock.UtcNow,
            items);
        var outcome = await _campaigns.UpdateAsync(updated, command.ExpectedRevision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            await _assets.DeleteAsync(newKey, cancellationToken).ConfigureAwait(false);
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The item objective image could not be saved.");
        }

        if (CatalogFileBinder.IsUserUploadedFileKey(previousKey))
        {
            await CampaignAssetRetention.DeleteIfUnreferencedAsync(
                _campaigns,
                _assets.DeleteAsync,
                previousKey,
                command.CampaignId,
                cancellationToken,
                _presets).ConfigureAwait(false);
        }

        return OperationResults.Success(CampaignMapper.ToDetail(outcome.Campaign, command.UserId, _clock.UtcNow));
    }
}

/// <summary>
/// Opens a stored item-objective logo for a campaign member.
/// </summary>
public sealed class GetItemObjectiveImageHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignAssetStorage _assets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public GetItemObjectiveImageHandler(ICampaignStore campaigns, ICampaignAssetStorage assets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(assets);
        _campaigns = campaigns;
        _assets = assets;
    }

    /// <summary>
    /// Returns the stored item-objective logo for a member.
    /// </summary>
    public async Task<OperationResult<CampaignAssetRead>> HandleAsync(
        Guid campaignId,
        Guid itemObjectiveTypeId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false,
        string? ifNoneMatch = null)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var item = campaign.ItemObjectiveTypes.FirstOrDefault(type => type.Id == itemObjectiveTypeId);
        if (item is null || string.IsNullOrWhiteSpace(item.ImageStorageKey))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The item objective image was not found.");
        }

        var read = await CampaignAssetReader
            .ReadAsync(_assets.OpenStreamAsync, item.ImageStorageKey, ifNoneMatch, downloadName: null, cancellationToken)
            .ConfigureAwait(false);
        return read is null
            ? OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The item objective image was not found.")
            : OperationResults.Success(read);
    }
}

/// <summary>
/// Uploads a custom force-status chit or token for a campaign manager.
/// </summary>
public sealed class UploadForceStatusTokenHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignMapProcessor _processor;
    private readonly ICampaignAssetStorage _assets;
    private readonly IClock _clock;
    private readonly ICampaignPresetStore? _presets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public UploadForceStatusTokenHandler(
        ICampaignStore campaigns,
        ICampaignMapProcessor processor,
        ICampaignAssetStorage assets,
        IClock clock,
        ICampaignPresetStore? presets = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _assets = assets;
        _clock = clock;
        _presets = presets;
    }

    /// <summary>
    /// Replaces the force-status token after validating and re-encoding the upload.
    /// </summary>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        UploadForceStatusTokenCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var access = await CatalogAssetAccess.RequireManagerAsync(_campaigns, command.CampaignId, command.UserId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(access.ErrorCode ?? ErrorCodes.CampaignNotFound, access.Message ?? "The campaign was not found.");
        }

        var processed = await _processor
            .ProcessAsync(
                command.Content,
                command.ContentType,
                command.Length,
                cancellationToken,
                ICampaignMapProcessor.StructureLogoMaxDimension)
            .ConfigureAwait(false);
        if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                processed.ErrorCode ?? ErrorCodes.UploadInvalidImage,
                processed.Message ?? "The force status token could not be processed.");
        }

        var statuses = access.Campaign.ForceStatuses.ToList();
        var index = statuses.FindIndex(status => status.Id == command.ForceStatusId);
        if (index < 0)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The force status was not found.");
        }

        var newKey = await _assets
            .SaveAsync("status-tokens", processed.Content, processed.FileExtension, "image/png", cancellationToken)
            .ConfigureAwait(false);
        var previous = statuses[index];
        var previousKey = previous.TokenImageStorageKey;
        statuses[index] = new StoredForceStatus
        {
            Id = previous.Id,
            Name = previous.Name,
            Effects = previous.Effects,
            EnableTrigger = previous.EnableTrigger,
            ClearTrigger = previous.ClearTrigger,
            EnableConditions = previous.EnableConditions,
            ClearConditions = previous.ClearConditions,
            Priority = previous.Priority,
            CancelsStatusIds = previous.CancelsStatusIds,
            EnableOccurrences = previous.EnableOccurrences,
            ClearOccurrences = previous.ClearOccurrences,
            ImmuneFactionIds = previous.ImmuneFactionIds,
            ImmuneSubfactions = previous.ImmuneSubfactions,
            TokenImageStorageKey = newKey,
        };

        var updated = CampaignMapClone.CloneWithCatalogs(
            access.Campaign,
            access.Campaign.TerrainTypes,
            access.Campaign.StructureTypes,
            _clock.UtcNow,
            access.Campaign.ItemObjectiveTypes,
            statuses);
        var outcome = await _campaigns.UpdateAsync(updated, command.ExpectedRevision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            await _assets.DeleteAsync(newKey, cancellationToken).ConfigureAwait(false);
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The force status token could not be saved.");
        }

        if (CatalogFileBinder.IsUserUploadedFileKey(previousKey))
        {
            await CampaignAssetRetention.DeleteIfUnreferencedAsync(
                _campaigns,
                _assets.DeleteAsync,
                previousKey,
                command.CampaignId,
                cancellationToken,
                _presets).ConfigureAwait(false);
        }

        return OperationResults.Success(CampaignMapper.ToDetail(outcome.Campaign, command.UserId, _clock.UtcNow));
    }
}

/// <summary>
/// Opens a stored force-status chit or token for a campaign member.
/// </summary>
public sealed class GetForceStatusTokenHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignAssetStorage _assets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public GetForceStatusTokenHandler(ICampaignStore campaigns, ICampaignAssetStorage assets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(assets);
        _campaigns = campaigns;
        _assets = assets;
    }

    /// <summary>
    /// Returns the stored force-status token for a member.
    /// </summary>
    public async Task<OperationResult<CampaignAssetRead>> HandleAsync(
        Guid campaignId,
        Guid forceStatusId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false,
        string? ifNoneMatch = null)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var status = campaign.ForceStatuses.FirstOrDefault(item => item.Id == forceStatusId);
        if (status is null || string.IsNullOrWhiteSpace(status.TokenImageStorageKey))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The force status token was not found.");
        }

        var read = await CampaignAssetReader
            .ReadAsync(_assets.OpenStreamAsync, status.TokenImageStorageKey, ifNoneMatch, downloadName: null, cancellationToken)
            .ConfigureAwait(false);
        return read is null
            ? OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The force status token was not found.")
            : OperationResults.Success(read);
    }
}

/// <summary>
/// Uploads a custom faction flag for a campaign manager.
/// </summary>
public sealed class UploadFactionFlagHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignMapProcessor _processor;
    private readonly ICampaignAssetStorage _assets;
    private readonly IClock _clock;
    private readonly ICampaignPresetStore? _presets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="processor">The image processor.</param>
    /// <param name="assets">The asset storage.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="presets">The campaign-preset store used to keep shared logos.</param>
    public UploadFactionFlagHandler(
        ICampaignStore campaigns,
        ICampaignMapProcessor processor,
        ICampaignAssetStorage assets,
        IClock clock,
        ICampaignPresetStore? presets = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _assets = assets;
        _clock = clock;
        _presets = presets;
    }

    /// <summary>
    /// Replaces the faction flag after validating and re-encoding the upload.
    /// </summary>
    /// <param name="command">The upload command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        UploadFactionFlagCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var access = await CatalogAssetAccess.RequireManagerAsync(_campaigns, command.CampaignId, command.UserId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(access.ErrorCode ?? ErrorCodes.CampaignNotFound, access.Message ?? "The campaign was not found.");
        }

        var processed = await _processor
            .ProcessAsync(
                command.Content,
                command.ContentType,
                command.Length,
                cancellationToken,
                ICampaignMapProcessor.StructureLogoMaxDimension)
            .ConfigureAwait(false);
        if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                processed.ErrorCode ?? ErrorCodes.UploadInvalidImage,
                processed.Message ?? "The faction flag image could not be processed.");
        }

        var factions = access.Campaign.Factions.ToList();
        var index = factions.FindIndex(faction => faction.Id == command.FactionId);
        if (index < 0)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The faction was not found.");
        }

        var newKey = await _assets
            .SaveAsync("flags", processed.Content, processed.FileExtension, "image/png", cancellationToken)
            .ConfigureAwait(false);
        var previous = factions[index];
        string? previousKey;
        if (!string.IsNullOrWhiteSpace(command.SubfactionName))
        {
            if (!previous.Subfactions.Contains(command.SubfactionName, StringComparer.OrdinalIgnoreCase))
            {
                await _assets.DeleteAsync(newKey, cancellationToken).ConfigureAwait(false);
                return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The subfaction was not found.");
            }

            previousKey = FactionAppearance.Find(previous, command.SubfactionName)?.FlagImageStorageKey;
            factions[index] = FactionAppearance.WithSubfactionFlag(previous, command.SubfactionName, newKey);
        }
        else
        {
            previousKey = previous.FlagImageStorageKey;
            factions[index] = new StoredFaction
            {
                Id = previous.Id,
                Name = previous.Name,
                Color = previous.Color,
                Subfactions = previous.Subfactions,
                SubfactionAppearances = previous.SubfactionAppearances,
                AllyGroupName = previous.AllyGroupName,
                RequiresSubfaction = previous.RequiresSubfaction,
                FlagImageStorageKey = newKey,
                TintFlagImage = previous.TintFlagImage,
                SpecialRuleIds = previous.SpecialRuleIds,
                SubfactionSpecialRules = previous.SubfactionSpecialRules,
                ForceMovementSpeed = previous.ForceMovementSpeed,
                SubfactionMovementSpeeds = previous.SubfactionMovementSpeeds,
            };
        }

        var updated = CampaignMapClone.CloneWithFactions(access.Campaign, factions, _clock.UtcNow);
        var outcome = await _campaigns.UpdateAsync(updated, command.ExpectedRevision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            await _assets.DeleteAsync(newKey, cancellationToken).ConfigureAwait(false);
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The faction flag could not be saved.");
        }

        if (CatalogFileBinder.IsUserUploadedFileKey(previousKey))
        {
            await CampaignAssetRetention.DeleteIfUnreferencedAsync(
                _campaigns,
                _assets.DeleteAsync,
                previousKey,
                command.CampaignId,
                cancellationToken,
                _presets).ConfigureAwait(false);
        }

        return OperationResults.Success(CampaignMapper.ToDetail(outcome.Campaign, command.UserId, _clock.UtcNow));
    }
}

/// <summary>
/// Opens a stored faction flag for a campaign member.
/// </summary>
public sealed class GetFactionFlagHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignAssetStorage _assets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="assets">The asset storage.</param>
    public GetFactionFlagHandler(ICampaignStore campaigns, ICampaignAssetStorage assets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(assets);
        _campaigns = campaigns;
        _assets = assets;
    }

    /// <summary>
    /// Returns the stored faction flag for a member.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="factionId">The faction identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <param name="subfactionName">The subfaction name when reading a subfaction logo.</param>
    /// <param name="ifNoneMatch">The caller's cached asset tag, if any.</param>
    /// <returns>The stored image.</returns>
    public async Task<OperationResult<CampaignAssetRead>> HandleAsync(
        Guid campaignId,
        Guid factionId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false,
        string? subfactionName = null,
        string? ifNoneMatch = null)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var faction = campaign.Factions.FirstOrDefault(item => item.Id == factionId);
        if (faction is null)
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The faction flag was not found.");
        }

        var key = FactionAppearance.Resolve(faction, subfactionName).FlagImageStorageKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The faction flag was not found.");
        }

        var read = await CampaignAssetReader
            .ReadAsync(_assets.OpenStreamAsync, key, ifNoneMatch, downloadName: null, cancellationToken)
            .ConfigureAwait(false);
        return read is null
            ? OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The faction flag was not found.")
            : OperationResults.Success(read);
    }
}

/// <summary>
/// Uploads a mission document for a campaign manager.
/// </summary>
public sealed class UploadMissionFileHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignDocumentProcessor _processor;
    private readonly ICampaignAssetStorage _assets;
    private readonly IClock _clock;
    private readonly ICampaignPresetStore? _presets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="processor">The document processor.</param>
    /// <param name="assets">The asset storage.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="presets">The campaign-preset store used to keep shared files.</param>
    public UploadMissionFileHandler(
        ICampaignStore campaigns,
        ICampaignDocumentProcessor processor,
        ICampaignAssetStorage assets,
        IClock clock,
        ICampaignPresetStore? presets = null)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _assets = assets;
        _clock = clock;
        _presets = presets;
    }

    /// <summary>
    /// Attaches a PDF or Word document to a mission, replacing any previous file and clearing a URL.
    /// </summary>
    /// <param name="command">The upload command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        UploadMissionFileCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var access = await CatalogAssetAccess.RequireManagerAsync(_campaigns, command.CampaignId, command.UserId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!access.IsSuccess || access.Campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(access.ErrorCode ?? ErrorCodes.CampaignNotFound, access.Message ?? "The campaign was not found.");
        }

        var processed = await _processor
            .ProcessAsync(command.Content, command.ContentType, command.FileName, command.Length, cancellationToken)
            .ConfigureAwait(false);
        if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null || processed.ContentType is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                processed.ErrorCode ?? ErrorCodes.UploadInvalidType,
                processed.Message ?? "The mission file could not be processed.");
        }

        var terrains = access.Campaign.TerrainTypes.ToList();
        var structures = access.Campaign.StructureTypes.ToList();
        if (!TryReplaceMission(terrains, structures, command.MissionId, processed, out var previousKey, out var newTerrains, out var newStructures))
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The mission was not found.");
        }

        var newKey = await _assets
            .SaveAsync("missions", processed.Content, processed.FileExtension, processed.ContentType, cancellationToken)
            .ConfigureAwait(false);
        if (!TryReplaceMission([.. newTerrains], [.. newStructures], command.MissionId, processed, out _, out var boundTerrains, out var boundStructures, newKey))
        {
            await _assets.DeleteAsync(newKey, cancellationToken).ConfigureAwait(false);
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The mission was not found.");
        }

        var updated = CampaignMapClone.CloneWithCatalogs(access.Campaign, boundTerrains, boundStructures, _clock.UtcNow);
        var outcome = await _campaigns.UpdateAsync(updated, command.ExpectedRevision, cancellationToken).ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            await _assets.DeleteAsync(newKey, cancellationToken).ConfigureAwait(false);
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The mission file could not be saved.");
        }

        if (CatalogFileBinder.IsUserUploadedFileKey(previousKey))
        {
            await CampaignAssetRetention.DeleteIfUnreferencedAsync(
                _campaigns,
                _assets.DeleteAsync,
                previousKey,
                command.CampaignId,
                cancellationToken,
                _presets).ConfigureAwait(false);
        }

        return OperationResults.Success(CampaignMapper.ToDetail(outcome.Campaign, command.UserId, _clock.UtcNow));
    }

    private static bool TryReplaceMission(
        List<StoredTerrainType> terrains,
        List<StoredStructureType> structures,
        Guid missionId,
        ProcessedCampaignDocumentResult processed,
        out string? previousKey,
        out IReadOnlyList<StoredTerrainType> nextTerrains,
        out IReadOnlyList<StoredStructureType> nextStructures,
        string? fileStorageKey = null)
    {
        previousKey = null;
        var found = false;
        for (var i = 0; i < terrains.Count; i++)
        {
            var missions = terrains[i].Missions.ToList();
            var replaced = false;
            for (var index = 0; index < missions.Count; index++)
            {
                if (missions[index].Id != missionId)
                {
                    continue;
                }

                previousKey ??= missions[index].FileStorageKey;
                missions[index] = WithMissionFile(missions[index], processed, fileStorageKey);
                replaced = true;
                found = true;
            }

            if (replaced)
            {
                terrains[i] = new StoredTerrainType
                {
                    Id = terrains[i].Id,
                    Name = terrains[i].Name,
                    Color = terrains[i].Color,
                    Missions = missions,
                };
            }
        }

        for (var i = 0; i < structures.Count; i++)
        {
            var missions = structures[i].Missions.ToList();
            var replaced = false;
            for (var index = 0; index < missions.Count; index++)
            {
                if (missions[index].Id != missionId)
                {
                    continue;
                }

                previousKey ??= missions[index].FileStorageKey;
                missions[index] = WithMissionFile(missions[index], processed, fileStorageKey);
                replaced = true;
                found = true;
            }

            if (replaced)
            {
                structures[i] = new StoredStructureType
                {
                    Id = structures[i].Id,
                    Name = structures[i].Name,
                    BuiltinSymbol = structures[i].BuiltinSymbol,
                    ImageStorageKey = structures[i].ImageStorageKey,
                    PillagedImageStorageKey = structures[i].PillagedImageStorageKey,
                    IsBuildable = structures[i].IsBuildable,
                    IsPillageable = structures[i].IsPillageable,
                    IsDestructible = structures[i].IsDestructible,
                    Missions = missions,
                    CampaignPoints = structures[i].CampaignPoints,
                };
            }
        }

        nextTerrains = terrains;
        nextStructures = structures;
        return found;
    }

    private static StoredMission WithMissionFile(
        StoredMission mission,
        ProcessedCampaignDocumentResult processed,
        string? fileStorageKey)
    {
        return new StoredMission
        {
            Id = mission.Id,
            Name = mission.Name,
            Url = fileStorageKey is null ? mission.Url : null,
            FileStorageKey = fileStorageKey ?? mission.FileStorageKey,
            FileName = fileStorageKey is null ? mission.FileName : processed.FileName,
            ResultQuestions = mission.ResultQuestions,
            IsAttackerDefender = mission.IsAttackerDefender,
            HasArmyPointsAdvantage = mission.HasArmyPointsAdvantage,
            ArmyPointsAdvantageSide = mission.ArmyPointsAdvantageSide,
            ArmyPointsAdvantageIsPercent = mission.ArmyPointsAdvantageIsPercent,
            ArmyPointsAdvantageAmount = mission.ArmyPointsAdvantageAmount,
            HasSupplyPointsAdvantage = mission.HasSupplyPointsAdvantage,
            SupplyPointsAdvantageSide = mission.SupplyPointsAdvantageSide,
            SupplyPointsAdvantageAmount = mission.SupplyPointsAdvantageAmount,
            StatusChanges = mission.StatusChanges,
        };
    }
}

/// <summary>
/// Opens a stored mission document for a campaign member.
/// </summary>
public sealed class GetMissionFileHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignAssetStorage _assets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="assets">The asset storage.</param>
    public GetMissionFileHandler(ICampaignStore campaigns, ICampaignAssetStorage assets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(assets);
        _campaigns = campaigns;
        _assets = assets;
    }

    /// <summary>
    /// Returns the stored mission document for a member.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="missionId">The mission identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <param name="ifNoneMatch">The caller's cached asset tag, if any.</param>
    /// <returns>The stored document.</returns>
    public async Task<OperationResult<CampaignAssetRead>> HandleAsync(
        Guid campaignId,
        Guid missionId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false,
        string? ifNoneMatch = null)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var mission = CampaignPlayCatalog.FindMission(campaign, missionId);
        if (mission is null || string.IsNullOrWhiteSpace(mission.FileStorageKey))
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The mission file was not found.");
        }

        var read = await CampaignAssetReader
            .ReadAsync(_assets.OpenStreamAsync, mission.FileStorageKey, ifNoneMatch, mission.FileName, cancellationToken)
            .ConfigureAwait(false);
        return read is null
            ? OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The mission file was not found.")
            : OperationResults.Success(read);
    }
}

internal static class CatalogAssetAccess
{
    public static async Task<(bool IsSuccess, StoredCampaign? Campaign, string? ErrorCode, string? Message)> RequireManagerAsync(
        ICampaignStore campaigns,
        Guid campaignId,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        var membership = existing is null ? null : CampaignMapper.MembershipFor(existing, userId);
        if (existing is null || membership is null)
        {
            return (false, null, ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!membership.IsGameMaster)
        {
            return (false, null, ErrorCodes.CampaignForbidden, "Only a campaign manager can change campaign files.");
        }

        if (CampaignLifecycle.HasLaunched(existing, utcNow))
        {
            return (false, null, ErrorCodes.CampaignLocked, CampaignLifecycle.LockedMessage);
        }

        return (true, existing, null, null);
    }
}

/// <summary>
/// Command to replace a structure logo.
/// </summary>
public sealed class UploadStructureImageCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the structure type identifier.</summary>
    public required Guid StructureTypeId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the uploaded image stream.</summary>
    public required Stream Content { get; init; }

    /// <summary>Gets the declared content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the declared length, if known.</summary>
    public long? Length { get; init; }

    /// <summary>Gets whether this upload replaces the pillaged logo.</summary>
    public bool Pillaged { get; init; }
}

/// <summary>
/// Command to replace an item-objective logo.
/// </summary>
public sealed class UploadItemObjectiveImageCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the item objective type identifier.</summary>
    public required Guid ItemObjectiveTypeId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the uploaded image stream.</summary>
    public required Stream Content { get; init; }

    /// <summary>Gets the declared content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the declared length, if known.</summary>
    public long? Length { get; init; }
}

/// <summary>
/// Command to replace a force-status chit or token image.
/// </summary>
public sealed class UploadForceStatusTokenCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the force status identifier.</summary>
    public required Guid ForceStatusId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the uploaded image stream.</summary>
    public required Stream Content { get; init; }

    /// <summary>Gets the declared content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the declared length, if known.</summary>
    public long? Length { get; init; }
}

/// <summary>
/// Command to replace a faction flag image.
/// </summary>
public sealed class UploadFactionFlagCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the faction identifier.</summary>
    public required Guid FactionId { get; init; }

    /// <summary>Gets the subfaction name when replacing a subfaction logo.</summary>
    public string? SubfactionName { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the uploaded image stream.</summary>
    public required Stream Content { get; init; }

    /// <summary>Gets the declared content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the declared length, if known.</summary>
    public long? Length { get; init; }
}

/// <summary>
/// Command to attach a mission document.
/// </summary>
public sealed class UploadMissionFileCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the mission identifier.</summary>
    public required Guid MissionId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the uploaded document stream.</summary>
    public required Stream Content { get; init; }

    /// <summary>Gets the declared content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the original file name.</summary>
    public required string FileName { get; init; }

    /// <summary>Gets the declared length, if known.</summary>
    public long? Length { get; init; }
}

/// <summary>
/// Which catalog image a saved campaign preset should return.
/// </summary>
public enum CampaignPresetAssetKind
{
    /// <summary>A faction or subfaction flag.</summary>
    FactionFlag = 0,

    /// <summary>An operational structure logo.</summary>
    StructureImage = 1,

    /// <summary>A pillaged structure logo.</summary>
    StructurePillagedImage = 2,

    /// <summary>An item-objective logo.</summary>
    ItemObjectiveImage = 3,

    /// <summary>A force-status chit or token image.</summary>
    ForceStatusToken = 4,
}

/// <summary>
/// Reads a stored catalog image from a named campaign preset.
/// </summary>
public sealed class GetCampaignPresetAssetHandler
{
    private readonly ICampaignPresetStore _presets;
    private readonly ICampaignAssetStorage _assets;

    /// <summary>Initializes a handler.</summary>
    public GetCampaignPresetAssetHandler(ICampaignPresetStore presets, ICampaignAssetStorage assets)
    {
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(assets);
        _presets = presets;
        _assets = assets;
    }

    /// <summary>
    /// Returns a preset catalog image for any authenticated user who may list presets.
    /// </summary>
    public async Task<OperationResult<CampaignAssetRead>> HandleAsync(
        Guid presetId,
        Guid catalogId,
        CampaignPresetAssetKind kind,
        CancellationToken cancellationToken,
        string? subfactionName = null,
        string? ifNoneMatch = null)
    {
        var preset = await _presets.FindByIdAsync(presetId, cancellationToken).ConfigureAwait(false);
        if (preset is null)
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The campaign preset was not found.");
        }

        string? storageKey = kind switch
        {
            CampaignPresetAssetKind.FactionFlag => ResolveFactionFlag(preset, catalogId, subfactionName),
            CampaignPresetAssetKind.StructureImage =>
                preset.StructureTypes.FirstOrDefault(type => type.Id == catalogId)?.ImageStorageKey,
            CampaignPresetAssetKind.StructurePillagedImage =>
                preset.StructureTypes.FirstOrDefault(type => type.Id == catalogId)?.PillagedImageStorageKey,
            CampaignPresetAssetKind.ItemObjectiveImage =>
                preset.ItemObjectiveTypes.FirstOrDefault(type => type.Id == catalogId)?.ImageStorageKey,
            CampaignPresetAssetKind.ForceStatusToken =>
                preset.ForceStatuses.FirstOrDefault(status => status.Id == catalogId)?.TokenImageStorageKey,
            _ => null,
        };
        if (!CatalogFileBinder.IsUserUploadedFileKey(storageKey) || storageKey is null)
        {
            return OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The image was not found.");
        }

        var read = await CampaignAssetReader
            .ReadAsync(_assets.OpenStreamAsync, storageKey, ifNoneMatch, downloadName: null, cancellationToken)
            .ConfigureAwait(false);
        return read is null
            ? OperationResults.Failure<CampaignAssetRead>(ErrorCodes.CampaignNotFound, "The image was not found.")
            : OperationResults.Success(read);
    }

    private static string? ResolveFactionFlag(StoredCampaign preset, Guid factionId, string? subfactionName)
    {
        var faction = preset.Factions.FirstOrDefault(item => item.Id == factionId);
        return faction is null ? null : FactionAppearance.Resolve(faction, subfactionName).FlagImageStorageKey;
    }
}
