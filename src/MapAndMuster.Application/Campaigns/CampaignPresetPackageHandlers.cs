using MapAndMuster.Application.Common;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Downloads a named preset or the current campaign as a portable ZIP. Administrators only.
/// </summary>
public sealed class ExportCampaignPresetHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly ICampaignPresetStore _presets;
    private readonly ICampaignMapStorage _maps;
    private readonly ICampaignAssetStorage _assets;
    private readonly ICampaignPresetPackageCodec _codec;

    /// <summary>Initializes a handler.</summary>
    public ExportCampaignPresetHandler(
        ICampaignStore campaigns,
        ICampaignPresetStore presets,
        ICampaignMapStorage maps,
        ICampaignAssetStorage assets,
        ICampaignPresetPackageCodec codec)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(codec);
        _campaigns = campaigns;
        _presets = presets;
        _maps = maps;
        _assets = assets;
        _codec = codec;
    }

    /// <summary>Returns a ZIP of catalog, overlay, map image, and referenced files.</summary>
    public async Task<OperationResult<CampaignPresetPackageFile>> HandleAsync(
        ExportCampaignPresetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.IsAdministrator)
        {
            return OperationResults.Failure<CampaignPresetPackageFile>(
                ErrorCodes.CampaignForbidden,
                "Only administrators can download a campaign preset.");
        }

        var source = await LoadSourceAsync(command, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return OperationResults.Failure<CampaignPresetPackageFile>(
                ErrorCodes.CampaignNotFound,
                "The campaign preset was not found.");
        }

        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var key in CatalogFileBinder.CollectCampaignStorageKeys(source).Distinct(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = await ReadFileAsync(key, cancellationToken).ConfigureAwait(false);
            if (bytes is not null)
            {
                files[key] = bytes;
            }
        }

        return OperationResults.Success(
            new CampaignPresetPackageFile
            {
                Content = _codec.Write(source, files),
                DownloadName = $"{CampaignLogExport.FileSlug(source.Name)}-preset.mapandmuster-preset",
            });
    }

    private async Task<StoredCampaign?> LoadSourceAsync(
        ExportCampaignPresetCommand command,
        CancellationToken cancellationToken)
    {
        if (command.PresetId is { } presetId)
        {
            return await _presets.FindByIdAsync(presetId, cancellationToken).ConfigureAwait(false);
        }

        if (command.CampaignId is not { } campaignId)
        {
            return null;
        }

        var campaign = await _campaigns.FindByIdAsync(campaignId, cancellationToken).ConfigureAwait(false);
        if (campaign is null || !CampaignAccess.CanView(campaign, command.UserId, command.IsAdministrator))
        {
            return null;
        }

        return campaign;
    }

    private async Task<byte[]?> ReadFileAsync(string storageKey, CancellationToken cancellationToken)
    {
        if (storageKey.StartsWith("maps/", StringComparison.Ordinal))
        {
            var map = await _maps.OpenReadAsync(storageKey, cancellationToken).ConfigureAwait(false);
            return map?.Content;
        }

        var asset = await _assets.OpenReadAsync(storageKey, cancellationToken).ConfigureAwait(false);
        return asset?.Content;
    }
}

/// <summary>
/// Imports a portable preset ZIP into the named-preset library. Administrators only.
/// </summary>
public sealed class ImportCampaignPresetHandler
{
    /// <summary>
    /// Maximum accepted package size. User map uploads stay at 20 MB, but stored PNGs can be larger
    /// after re-encoding. The ZIP must fit that stored map plus overlay and catalog files.
    /// Other API uploads stay on the 24 MB host limit.
    /// </summary>
    public const int MaxPackageBytes = 64 * 1024 * 1024;

    private readonly ICampaignPresetStore _presets;
    private readonly ICampaignMapStorage _maps;
    private readonly ICampaignAssetStorage _assets;
    private readonly ICampaignMapProcessor _images;
    private readonly ICampaignDocumentProcessor _documents;
    private readonly ICampaignPresetPackageCodec _codec;
    private readonly IClock _clock;

    /// <summary>Initializes a handler.</summary>
    public ImportCampaignPresetHandler(
        ICampaignPresetStore presets,
        ICampaignMapStorage maps,
        ICampaignAssetStorage assets,
        ICampaignMapProcessor images,
        ICampaignDocumentProcessor documents,
        ICampaignPresetPackageCodec codec,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(images);
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentNullException.ThrowIfNull(clock);
        _presets = presets;
        _maps = maps;
        _assets = assets;
        _images = images;
        _documents = documents;
        _codec = codec;
        _clock = clock;
    }

    /// <summary>Stores the package as a named preset, copying files onto this host.</summary>
    public async Task<OperationResult<CampaignPresetListItem>> HandleAsync(
        ImportCampaignPresetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.IsAdministrator)
        {
            return OperationResults.Failure<CampaignPresetListItem>(
                ErrorCodes.CampaignForbidden,
                "Only administrators can upload a campaign preset.");
        }

        if (command.Content.Length == 0 || command.Content.Length > MaxPackageBytes)
        {
            return OperationResults.Failure<CampaignPresetListItem>(
                command.Content.Length == 0 ? ErrorCodes.CampaignPresetPackageInvalid : ErrorCodes.UploadTooLarge,
                command.Content.Length == 0
                    ? "Upload a Map & Muster campaign preset file."
                    : "The campaign preset file is too large.");
        }

        var unpacked = _codec.Read(command.Content);
        if (!unpacked.IsSuccess || unpacked.Value is null)
        {
            return OperationResults.Failure<CampaignPresetListItem>(
                unpacked.ErrorCode ?? ErrorCodes.CampaignPresetPackageInvalid,
                unpacked.Message ?? "The campaign preset file is not valid.");
        }

        var name = CampaignSetupRules.CollapseName(unpacked.Value.Name);
        if (name.Length < CampaignSetupRules.NameMinLength || name.Length > CampaignSetupRules.NameMaxLength)
        {
            return OperationResults.Failure<CampaignPresetListItem>(
                ErrorCodes.ValidationFailed,
                $"Preset name must be {CampaignSetupRules.NameMinLength} to {CampaignSetupRules.NameMaxLength} characters.");
        }

        var keyMap = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (oldKey, bytes) in unpacked.Value.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stored = await StoreFileAsync(oldKey, bytes, cancellationToken).ConfigureAwait(false);
            if (!stored.IsSuccess || stored.Value is null)
            {
                return OperationResults.Failure<CampaignPresetListItem>(
                    stored.ErrorCode ?? ErrorCodes.CampaignPresetPackageInvalid,
                    stored.Message ?? "A file in the campaign preset could not be stored.");
            }

            keyMap[oldKey] = stored.Value;
        }

        var snapshot = CampaignPresetKeyRemap.Remap(unpacked.Value.Campaign, keyMap);
        var saved = await _presets
            .UpsertFromCampaignAsync(name, snapshot, command.UserId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        return OperationResults.Success(saved);
    }

    private async Task<OperationResult<string>> StoreFileAsync(
        string oldKey,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        if (!CatalogFileBinder.IsUserUploadedFileKey(oldKey))
        {
            return OperationResults.Failure<string>(
                ErrorCodes.CampaignPresetPackageInvalid,
                "A file in the campaign preset is not valid.");
        }

        var slash = oldKey.IndexOf('/', StringComparison.Ordinal);
        var folder = oldKey[..slash];
        var extension = Path.GetExtension(oldKey);
        if (string.Equals(folder, "missions", StringComparison.Ordinal))
        {
            await using var stream = new MemoryStream(bytes, writable: false);
            var processed = await _documents
                .ProcessAsync(stream, ContentTypeFor(extension), Path.GetFileName(oldKey), bytes.Length, cancellationToken)
                .ConfigureAwait(false);
            if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null || processed.ContentType is null)
            {
                return OperationResults.Failure<string>(
                    processed.ErrorCode ?? ErrorCodes.UploadInvalidType,
                    processed.Message ?? "A mission document in the campaign preset is not valid.");
            }

            var key = await _assets
                .SaveAsync(folder, processed.Content, processed.FileExtension, processed.ContentType, cancellationToken)
                .ConfigureAwait(false);
            return OperationResults.Success(key);
        }

        var isMap = string.Equals(folder, "maps", StringComparison.Ordinal);
        var maxDimension = isMap
            ? ICampaignMapProcessor.MapMaxDimension
            : ICampaignMapProcessor.StructureLogoMaxDimension;
        var maxBytes = isMap ? MaxPackageBytes : ICampaignMapProcessor.MaxUploadBytes;
        await using (var stream = new MemoryStream(bytes, writable: false))
        {
            var processed = await _images
                .ProcessAsync(stream, "image/png", bytes.Length, cancellationToken, maxDimension, maxBytes)
                .ConfigureAwait(false);
            if (!processed.IsSuccess || processed.Content is null || processed.FileExtension is null)
            {
                return OperationResults.Failure<string>(
                    processed.ErrorCode ?? ErrorCodes.UploadInvalidImage,
                    processed.Message ?? "An image in the campaign preset is not valid.");
            }

            if (isMap)
            {
                var mapKey = await _maps.SaveAsync(processed.Content, processed.FileExtension, cancellationToken)
                    .ConfigureAwait(false);
                return OperationResults.Success(mapKey);
            }

            var assetKey = await _assets
                .SaveAsync(folder, processed.Content, processed.FileExtension, "image/png", cancellationToken)
                .ConfigureAwait(false);
            return OperationResults.Success(assetKey);
        }
    }

    private static string ContentTypeFor(string extension)
    {
        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "application/pdf";
        }

        if (string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
        {
            return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        }

        return "application/octet-stream";
    }
}
