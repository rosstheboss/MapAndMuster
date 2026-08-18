namespace Campaign.Application.Campaigns;

/// <summary>
/// A named saved campaign setup preset.
/// </summary>
public sealed class CampaignPresetListItem
{
    /// <summary>Gets the preset identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the preset name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets whether the preset includes a map image or overlay graph.</summary>
    public required bool HasMap { get; init; }
}

/// <summary>
/// Command for an administrator to save the current campaign as a named preset.
/// </summary>
public sealed class SaveCampaignPresetCommand
{
    /// <summary>Gets the campaign to copy.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the administrator.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the preset name. Matching an existing name overwrites that preset.</summary>
    public required string Name { get; init; }
}

/// <summary>
/// Command to copy a saved preset's map onto a campaign.
/// </summary>
public sealed class ApplyCampaignPresetCommand
{
    /// <summary>Gets the campaign receiving the map.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the preset to copy from.</summary>
    public required Guid PresetId { get; init; }

    /// <summary>Gets the caller.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }
}
