using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;

namespace MapAndMuster.Application.Ports;

/// <summary>
/// Opens a referenced campaign file for reading, or returns <see langword="null"/> when it is gone.
/// </summary>
/// <param name="storageKey">The storage key to open.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>An open stream the codec owns and disposes, or <see langword="null"/>.</returns>
public delegate Task<Stream?> CampaignPresetFileOpener(string storageKey, CancellationToken cancellationToken);

/// <summary>
/// Packs and unpacks administrator campaign-preset ZIP files.
/// </summary>
public interface ICampaignPresetPackageCodec
{
    /// <summary>
    /// Writes a ZIP containing catalog, settings, overlay graph, an SVG rendering of that overlay,
    /// the map image, and referenced catalog files.
    /// </summary>
    /// <param name="destination">The stream to write the archive to.</param>
    /// <param name="campaign">The campaign or preset being exported.</param>
    /// <param name="storageKeys">The distinct storage keys the campaign references.</param>
    /// <param name="openFile">Opens each referenced file on demand.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the archive is written.</returns>
    /// <remarks>
    /// Files are copied straight from storage into the archive. A 64 MB package is never held in
    /// memory, which matters on a small instance where a single buffered export lands on the
    /// large object heap.
    /// </remarks>
    Task WriteAsync(
        Stream destination,
        StoredCampaign campaign,
        IReadOnlyList<string> storageKeys,
        CampaignPresetFileOpener openFile,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads a ZIP written by <see cref="WriteAsync"/>. Overlay SVG is ignored; overlay JSON is the schema.
    /// </summary>
    /// <param name="content">The uploaded archive. Non-seekable streams are spooled first.</param>
    /// <returns>The decoded package, or a failure.</returns>
    OperationResult<CampaignPresetPackageContents> Read(Stream content);
}

/// <summary>
/// A decoded portable campaign preset.
/// </summary>
public sealed class CampaignPresetPackageContents
{
    /// <summary>Gets the preset name from the package manifest.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the reconstructed campaign snapshot, using the package's original storage keys.</summary>
    public required StoredCampaign Campaign { get; init; }

    /// <summary>Gets file bytes keyed by the original storage keys.</summary>
    public required IReadOnlyDictionary<string, byte[]> Files { get; init; }
}
