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
    const int MaxUploadBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Attempts to process an uploaded map image.
    /// </summary>
    /// <param name="content">The uploaded stream.</param>
    /// <param name="contentType">The declared content type.</param>
    /// <param name="length">The declared length, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The processed image, or a failure.</returns>
    Task<ProcessedCampaignMapResult> ProcessAsync(
        Stream content,
        string contentType,
        long? length,
        CancellationToken cancellationToken);
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
