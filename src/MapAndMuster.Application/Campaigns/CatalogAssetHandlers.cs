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

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="processor">The image processor.</param>
    /// <param name="assets">The asset storage.</param>
    /// <param name="clock">The clock.</param>
    public UploadStructureImageHandler(
        ICampaignStore campaigns,
        ICampaignMapProcessor processor,
        ICampaignAssetStorage assets,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _assets = assets;
        _clock = clock;
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
                cancellationToken).ConfigureAwait(false);
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
    /// <returns>The stored image.</returns>
    public async Task<OperationResult<StoredCampaignAsset>> HandleAsync(
        Guid campaignId,
        Guid structureTypeId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false,
        bool pillaged = false)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var structure = campaign.StructureTypes.FirstOrDefault(type => type.Id == structureTypeId);
        var storageKey = pillaged ? structure?.PillagedImageStorageKey : structure?.ImageStorageKey;
        if (structure is null || string.IsNullOrWhiteSpace(storageKey))
        {
            return OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The structure image was not found.");
        }

        var file = await _assets.OpenReadAsync(storageKey, cancellationToken).ConfigureAwait(false);
        return file is null
            ? OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The structure image was not found.")
            : OperationResults.Success(file);
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

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    public UploadItemObjectiveImageHandler(
        ICampaignStore campaigns,
        ICampaignMapProcessor processor,
        ICampaignAssetStorage assets,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _assets = assets;
        _clock = clock;
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
                cancellationToken).ConfigureAwait(false);
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
    public async Task<OperationResult<StoredCampaignAsset>> HandleAsync(
        Guid campaignId,
        Guid itemObjectiveTypeId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var item = campaign.ItemObjectiveTypes.FirstOrDefault(type => type.Id == itemObjectiveTypeId);
        if (item is null || string.IsNullOrWhiteSpace(item.ImageStorageKey))
        {
            return OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The item objective image was not found.");
        }

        var file = await _assets.OpenReadAsync(item.ImageStorageKey, cancellationToken).ConfigureAwait(false);
        return file is null
            ? OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The item objective image was not found.")
            : OperationResults.Success(file);
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

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="processor">The image processor.</param>
    /// <param name="assets">The asset storage.</param>
    /// <param name="clock">The clock.</param>
    public UploadFactionFlagHandler(
        ICampaignStore campaigns,
        ICampaignMapProcessor processor,
        ICampaignAssetStorage assets,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _assets = assets;
        _clock = clock;
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
        var previousKey = previous.FlagImageStorageKey;
        factions[index] = new StoredFaction
        {
            Id = previous.Id,
            Name = previous.Name,
            Color = previous.Color,
            Subfactions = previous.Subfactions,
            AllyGroupName = previous.AllyGroupName,
            RequiresSubfaction = previous.RequiresSubfaction,
            FlagImageStorageKey = newKey,
            TintFlagImage = previous.TintFlagImage,
            SpecialRuleIds = previous.SpecialRuleIds,
            SubfactionSpecialRules = previous.SubfactionSpecialRules,
        };

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
                cancellationToken).ConfigureAwait(false);
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
    /// <returns>The stored image.</returns>
    public async Task<OperationResult<StoredCampaignAsset>> HandleAsync(
        Guid campaignId,
        Guid factionId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var faction = campaign.Factions.FirstOrDefault(item => item.Id == factionId);
        if (faction is null || string.IsNullOrWhiteSpace(faction.FlagImageStorageKey))
        {
            return OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The faction flag was not found.");
        }

        var file = await _assets.OpenReadAsync(faction.FlagImageStorageKey, cancellationToken).ConfigureAwait(false);
        return file is null
            ? OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The faction flag was not found.")
            : OperationResults.Success(file);
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

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="processor">The document processor.</param>
    /// <param name="assets">The asset storage.</param>
    /// <param name="clock">The clock.</param>
    public UploadMissionFileHandler(
        ICampaignStore campaigns,
        ICampaignDocumentProcessor processor,
        ICampaignAssetStorage assets,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _processor = processor;
        _assets = assets;
        _clock = clock;
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
                cancellationToken).ConfigureAwait(false);
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
                missions[index] = new StoredMission
                {
                    Id = missions[index].Id,
                    Name = missions[index].Name,
                    Url = fileStorageKey is null ? missions[index].Url : null,
                    FileStorageKey = fileStorageKey ?? missions[index].FileStorageKey,
                    FileName = fileStorageKey is null ? missions[index].FileName : processed.FileName,
                };
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
                missions[index] = new StoredMission
                {
                    Id = missions[index].Id,
                    Name = missions[index].Name,
                    Url = fileStorageKey is null ? missions[index].Url : null,
                    FileStorageKey = fileStorageKey ?? missions[index].FileStorageKey,
                    FileName = fileStorageKey is null ? missions[index].FileName : processed.FileName,
                };
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
    /// <returns>The stored document.</returns>
    public async Task<OperationResult<StoredCampaignAsset>> HandleAsync(
        Guid campaignId,
        Guid missionId,
        Guid userId,
        CancellationToken cancellationToken,
        bool isAdministrator = false)
    {
        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, userId, isAdministrator))
        {
            return OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        var mission = CampaignPlayCatalog.FindMission(campaign, missionId);
        if (mission is null || string.IsNullOrWhiteSpace(mission.FileStorageKey))
        {
            return OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The mission file was not found.");
        }

        var file = await _assets.OpenReadAsync(mission.FileStorageKey, cancellationToken).ConfigureAwait(false);
        return file is null
            ? OperationResults.Failure<StoredCampaignAsset>(ErrorCodes.CampaignNotFound, "The mission file was not found.")
            : OperationResults.Success(new StoredCampaignAsset(file.Content, file.ContentType, mission.FileName));
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
