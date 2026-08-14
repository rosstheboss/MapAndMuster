namespace Campaign.Application.Ports;

/// <summary>
/// Stores campaign map files outside the web root using generated names.
/// </summary>
public interface ICampaignMapStorage
{
    /// <summary>
    /// Saves processed map bytes and returns the generated storage key.
    /// </summary>
    /// <param name="content">The processed image bytes.</param>
    /// <param name="fileExtension">The file extension including the leading period.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated storage key.</returns>
    Task<string> SaveAsync(ReadOnlyMemory<byte> content, string fileExtension, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a stored map if it exists.
    /// </summary>
    /// <param name="storageKey">The storage key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when deletion is attempted.</returns>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a stored map for reading.
    /// </summary>
    /// <param name="storageKey">The storage key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file, or <see langword="null"/> when it does not exist.</returns>
    Task<StoredCampaignMap?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}

/// <summary>
/// Stores campaign catalog files (structure logos, faction flags, and mission documents) outside the web root.
/// </summary>
public interface ICampaignAssetStorage
{
    /// <summary>
    /// Saves bytes under a generated key in the specified folder.
    /// </summary>
    /// <param name="folder">The storage folder, such as structures, flags, or missions.</param>
    /// <param name="content">The file bytes.</param>
    /// <param name="fileExtension">The file extension including the leading period.</param>
    /// <param name="contentType">The content type to store with the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated storage key.</returns>
    Task<string> SaveAsync(
        string folder,
        ReadOnlyMemory<byte> content,
        string fileExtension,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a stored asset if it exists.
    /// </summary>
    /// <param name="storageKey">The storage key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when deletion is attempted.</returns>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a stored asset for reading.
    /// </summary>
    /// <param name="storageKey">The storage key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file, or <see langword="null"/> when it does not exist.</returns>
    Task<StoredCampaignAsset?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}

/// <summary>
/// A stored campaign catalog file.
/// </summary>
/// <param name="Content">The file bytes.</param>
/// <param name="ContentType">The content type to serve.</param>
/// <param name="DownloadName">The original file name, when known.</param>
public sealed record StoredCampaignAsset(byte[] Content, string ContentType, string? DownloadName = null);

/// <summary>
/// A stored campaign map file.
/// </summary>
/// <param name="Content">The file bytes.</param>
/// <param name="ContentType">The content type to serve.</param>
public sealed record StoredCampaignMap(byte[] Content, string ContentType);

/// <summary>
/// Validates, re-encodes, and strips metadata from uploaded raster campaign maps.
/// </summary>
public interface ICampaignMapProcessor
{
    /// <summary>
    /// Maximum accepted upload size in bytes.
    /// </summary>
    const int MaxUploadBytes = 20 * 1024 * 1024;

    /// <summary>
    /// Maximum width and height for a structure logo after shrinking.
    /// </summary>
    const int StructureLogoMaxDimension = 50;

    /// <summary>
    /// Maximum width and height for a campaign map after shrinking.
    /// </summary>
    const int MapMaxDimension = 8192;

    /// <summary>
    /// Attempts to process an uploaded map image.
    /// </summary>
    /// <param name="content">The uploaded stream.</param>
    /// <param name="contentType">The declared content type.</param>
    /// <param name="length">The declared length, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="maxDimension">The maximum width or height to keep.</param>
    /// <returns>The processed image, or a failure.</returns>
    Task<ProcessedCampaignMapResult> ProcessAsync(
        Stream content,
        string contentType,
        long? length,
        CancellationToken cancellationToken,
        int maxDimension = MapMaxDimension);
}

/// <summary>
/// Result of processing a campaign map upload.
/// </summary>
public sealed class ProcessedCampaignMapResult
{
    /// <summary>Gets a value indicating whether processing succeeded.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Gets the processed image bytes.</summary>
    public byte[]? Content { get; init; }

    /// <summary>Gets the file extension including the leading period.</summary>
    public string? FileExtension { get; init; }

    /// <summary>Gets the error code when processing failed.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Gets the error message when processing failed.</summary>
    public string? Message { get; init; }
}

/// <summary>
/// Validates uploaded mission PDF and DOCX files.
/// </summary>
public interface ICampaignDocumentProcessor
{
    /// <summary>
    /// Maximum accepted upload size in bytes.
    /// </summary>
    const int MaxUploadBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Attempts to process an uploaded mission document.
    /// </summary>
    /// <param name="content">The uploaded stream.</param>
    /// <param name="contentType">The declared content type.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="length">The declared length, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The processed document, or a failure.</returns>
    Task<ProcessedCampaignDocumentResult> ProcessAsync(
        Stream content,
        string contentType,
        string fileName,
        long? length,
        CancellationToken cancellationToken);
}

/// <summary>
/// Result of processing a mission document upload.
/// </summary>
public sealed class ProcessedCampaignDocumentResult
{
    /// <summary>Gets a value indicating whether processing succeeded.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Gets the document bytes.</summary>
    public byte[]? Content { get; init; }

    /// <summary>Gets the file extension including the leading period.</summary>
    public string? FileExtension { get; init; }

    /// <summary>Gets the content type to store and serve.</summary>
    public string? ContentType { get; init; }

    /// <summary>Gets a safe original file name.</summary>
    public string? FileName { get; init; }

    /// <summary>Gets the error code when processing failed.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Gets the error message when processing failed.</summary>
    public string? Message { get; init; }
}
