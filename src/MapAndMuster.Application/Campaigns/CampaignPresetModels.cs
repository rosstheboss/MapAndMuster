namespace MapAndMuster.Application.Campaigns;

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

/// <summary>
/// A downloadable campaign preset package.
/// </summary>
public sealed class CampaignPresetPackageFile
{
    /// <summary>
    /// Gets the callback that writes the ZIP to a destination stream.
    /// </summary>
    /// <remarks>
    /// The archive is produced while the response is being written rather than buffered first,
    /// so a large export never occupies memory proportional to its size.
    /// </remarks>
    public required Func<Stream, CancellationToken, Task> WriteToAsync { get; init; }

    /// <summary>Gets the download file name.</summary>
    public required string DownloadName { get; init; }

    /// <summary>Gets the MIME type.</summary>
    public const string ContentType = "application/zip";
}

/// <summary>
/// Command for an administrator to download a named preset or the current campaign as a portable package.
/// </summary>
public sealed class ExportCampaignPresetCommand
{
    /// <summary>Gets the campaign to export when a named preset is not specified.</summary>
    public Guid? CampaignId { get; init; }

    /// <summary>Gets the named preset to export.</summary>
    public Guid? PresetId { get; init; }

    /// <summary>Gets the administrator.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }
}

/// <summary>
/// Command for an administrator to import a portable preset package into the named-preset library.
/// </summary>
public sealed class ImportCampaignPresetCommand
{
    /// <summary>Gets the administrator.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the uploaded package stream. The handler reads it but does not own it.</summary>
    public required Stream Content { get; init; }

    /// <summary>Gets the uploaded size in bytes, when the caller knows it.</summary>
    public long? Length { get; init; }

    /// <summary>Gets the original file name, when known.</summary>
    public string? FileName { get; init; }
}
