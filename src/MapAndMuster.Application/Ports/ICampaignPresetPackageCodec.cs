using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;

namespace MapAndMuster.Application.Ports;

/// <summary>
/// Packs and unpacks administrator campaign-preset ZIP files.
/// </summary>
public interface ICampaignPresetPackageCodec
{
    /// <summary>
    /// Builds a ZIP containing catalog, settings, overlay graph, an SVG rendering of that overlay,
    /// the map image, and referenced catalog files.
    /// </summary>
    byte[] Write(StoredCampaign campaign, IReadOnlyDictionary<string, byte[]> files);

    /// <summary>
    /// Reads a ZIP written by <see cref="Write"/>. Overlay SVG is ignored; overlay JSON is the schema.
    /// </summary>
    OperationResult<CampaignPresetPackageContents> Read(ReadOnlyMemory<byte> content);
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
