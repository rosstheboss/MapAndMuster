using MapAndMuster.Application.Common;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Lists named campaign presets an authenticated user may apply.
/// </summary>
public sealed class ListCampaignPresetsHandler
{
    private readonly ICampaignPresetStore _presets;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public ListCampaignPresetsHandler(ICampaignPresetStore presets)
    {
        ArgumentNullException.ThrowIfNull(presets);
        _presets = presets;
    }

    /// <summary>
    /// Returns saved presets, newest last so names stay stable for autocomplete.
    /// </summary>
    public async Task<OperationResult<IReadOnlyList<CampaignPresetListItem>>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var items = await _presets.ListAsync(cancellationToken).ConfigureAwait(false);
        return OperationResults.Success(items);
    }
}

/// <summary>
/// Loads a saved campaign preset as a campaign-detail catalog snapshot.
/// </summary>
public sealed class GetCampaignPresetHandler
{
    private readonly ICampaignPresetStore _presets;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public GetCampaignPresetHandler(ICampaignPresetStore presets, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(clock);
        _presets = presets;
        _clock = clock;
    }

    /// <summary>
    /// Returns the preset as a campaign detail without memberships or play state.
    /// </summary>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        Guid presetId,
        Guid viewerUserId,
        CancellationToken cancellationToken)
    {
        var preset = await _presets.FindByIdAsync(presetId, cancellationToken).ConfigureAwait(false);
        if (preset is null)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The campaign preset was not found.");
        }

        return OperationResults.Success(
            CampaignMapper.ToDetail(preset, viewerUserId, _clock.UtcNow, isAdministrator: true));
    }
}

/// <summary>
/// Saves the current campaign setup and map as a named preset. Administrators only.
/// </summary>
public sealed class SaveCampaignPresetHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignPresetStore _presets;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public SaveCampaignPresetHandler(ICampaignStore campaigns, ICampaignPresetStore presets, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _presets = presets;
        _clock = clock;
    }

    /// <summary>
    /// Copies the campaign's catalog and map into a preset, overwriting when the name matches.
    /// </summary>
    public async Task<OperationResult<CampaignPresetListItem>> HandleAsync(
        SaveCampaignPresetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.IsAdministrator)
        {
            return OperationResults.Failure<CampaignPresetListItem>(
                ErrorCodes.CampaignForbidden,
                "Only administrators can save a campaign as a preset.");
        }

        var name = CampaignSetupRules.CollapseName(command.Name);
        if (name.Length < CampaignSetupRules.NameMinLength || name.Length > CampaignSetupRules.NameMaxLength)
        {
            return OperationResults.Failure<CampaignPresetListItem>(
                ErrorCodes.ValidationFailed,
                $"Preset name must be {CampaignSetupRules.NameMinLength} to {CampaignSetupRules.NameMaxLength} characters.");
        }

        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null)
        {
            return OperationResults.Failure<CampaignPresetListItem>(
                ErrorCodes.CampaignNotFound,
                "The campaign was not found.");
        }

        var saved = await _presets
            .UpsertFromCampaignAsync(name, campaign, command.UserId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        return OperationResults.Success(saved);
    }
}

/// <summary>
/// Copies a saved preset's map, overlay graph, and catalog files onto a campaign.
/// </summary>
public sealed class ApplyCampaignPresetHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignPresetStore _presets;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public ApplyCampaignPresetHandler(ICampaignStore campaigns, ICampaignPresetStore presets, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(clock);
        _campaigns = campaigns;
        _presets = presets;
        _clock = clock;
    }

    /// <summary>
    /// Copies the preset map, overlay, and catalog files when the caller may manage the campaign.
    /// </summary>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        ApplyCampaignPresetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var campaign = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!CampaignAccess.CanStaffMembers(campaign, command.UserId, command.IsAdministrator))
        {
            return OperationResults.Failure<CampaignDetail>(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager or administrator can apply a preset map.");
        }

        var preset = await _presets.FindByIdAsync(command.PresetId, cancellationToken).ConfigureAwait(false);
        if (preset is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                ErrorCodes.CampaignNotFound,
                "The campaign preset was not found.");
        }

        var updated = CopyPreset(campaign, preset, _clock.UtcNow);
        var outcome = await _campaigns
            .UpdateAsync(updated, command.Revision, cancellationToken)
            .ConfigureAwait(false);
        if (outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.ConcurrencyConflict,
                outcome.Message ?? "The campaign was updated by someone else. Reload and try again.");
        }

        return OperationResults.Success(
            CampaignMapper.ToDetail(outcome.Campaign, command.UserId, _clock.UtcNow, isAdministrator: command.IsAdministrator));
    }

    private static StoredCampaign CopyPreset(StoredCampaign campaign, StoredCampaign preset, DateTimeOffset utcNow)
    {
        var withFiles = CampaignPresetCatalogFiles.CopyOnto(campaign, preset);
        return new StoredCampaign
        {
            Id = withFiles.Id,
            Name = withFiles.Name,
            Description = withFiles.Description,
            PlayerSlotCount = withFiles.PlayerSlotCount,
            IsPrivate = withFiles.IsPrivate,
            IsPubliclyViewable = withFiles.IsPubliclyViewable,
            JoinPasswordHash = withFiles.JoinPasswordHash,
            CreatorIsParticipant = withFiles.CreatorIsParticipant,
            City = withFiles.City,
            Region = withFiles.Region,
            Country = withFiles.Country,
            MapStorageKey = string.IsNullOrWhiteSpace(preset.MapStorageKey)
                ? campaign.MapStorageKey
                : preset.MapStorageKey,
            Revision = campaign.Revision,
            CreatedUtc = withFiles.CreatedUtc,
            UpdatedUtc = utcNow,
            CreatedByUserId = withFiles.CreatedByUserId,
            Memberships = withFiles.Memberships,
            Factions = withFiles.Factions,
            AllyGroups = withFiles.AllyGroups,
            Links = withFiles.Links,
            TimeZoneId = withFiles.TimeZoneId,
            StartsUtc = withFiles.StartsUtc,
            EndsUtc = withFiles.EndsUtc,
            ClosedUtc = withFiles.ClosedUtc,
            RoundCount = withFiles.RoundCount,
            RoundLengthAmount = withFiles.RoundLengthAmount,
            RoundLengthUnit = withFiles.RoundLengthUnit,
            Phases = withFiles.Phases,
            MapGraph = preset.MapGraph is null
                ? campaign.MapGraph
                : CampaignOverlayRemap.ForCampaign(preset.MapGraph, preset, withFiles),
            PlayState = withFiles.PlayState,
            TerrainTypes = withFiles.TerrainTypes,
            StructureTypes = withFiles.StructureTypes,
            ItemObjectiveTypes = withFiles.ItemObjectiveTypes,
            PublicObjectiveTypes = withFiles.PublicObjectiveTypes,
            SpecialRules = withFiles.SpecialRules,
            Missions = withFiles.Missions,
            ForceStatuses = withFiles.ForceStatuses,
            PrivateObjectiveTypes = withFiles.PrivateObjectiveTypes,
            BattleScoring = withFiles.BattleScoring,
            RankingObjectivePoints = withFiles.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = withFiles.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = withFiles.SplitForceSupplyPenaltyIsPercent,
            StandardBattleResultQuestions = withFiles.StandardBattleResultQuestions,
            ArmyEscalations = withFiles.ArmyEscalations,
        };
    }
}
