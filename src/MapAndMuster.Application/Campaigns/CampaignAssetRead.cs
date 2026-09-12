using MapAndMuster.Application.Common;
using MapAndMuster.Application.Ports;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// The outcome of reading a stored campaign file, including the tag a client can cache against.
/// </summary>
public sealed class CampaignAssetRead : IAsyncDisposable
{
    /// <summary>Gets the opaque tag for the stored file. Changes only when the file is replaced.</summary>
    public required string AssetTag { get; init; }

    /// <summary>
    /// Gets a value indicating whether the caller already holds this exact file.
    /// </summary>
    /// <remarks>When set, <see cref="File"/> is null and storage was never touched.</remarks>
    public required bool IsNotModified { get; init; }

    /// <summary>Gets the open file, or <see langword="null"/> for a not-modified result.</summary>
    public StoredCampaignFile? File { get; init; }

    /// <summary>Releases the open file.</summary>
    /// <returns>A task that completes when the file is closed.</returns>
    public async ValueTask DisposeAsync()
    {
        if (File is not null)
        {
            await File.Content.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Shared logic for serving a stored campaign file with a cache tag.
/// </summary>
internal static class CampaignAssetReader
{
    /// <summary>
    /// Answers a conditional request without reading the file, or opens it.
    /// </summary>
    /// <param name="open">Opens the file behind <paramref name="storageKey"/>.</param>
    /// <param name="storageKey">The storage key of the requested file.</param>
    /// <param name="ifNoneMatch">The caller's cached tag, if any.</param>
    /// <param name="downloadName">The original file name, when the caller should be offered one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read, or <see langword="null"/> when the file is gone.</returns>
    public static async Task<CampaignAssetRead?> ReadAsync(
        Func<string, CancellationToken, Task<StoredCampaignFile?>> open,
        string storageKey,
        string? ifNoneMatch,
        string? downloadName,
        CancellationToken cancellationToken)
    {
        var tag = AssetTags.For(storageKey);
        if (tag is null)
        {
            return null;
        }

        // Short-circuit before touching storage. This is the whole point of the tag: a repeat
        // view costs one authorization query and no disk read at all.
        if (Matches(ifNoneMatch, tag))
        {
            return new CampaignAssetRead { AssetTag = tag, IsNotModified = true };
        }

        var file = await open(storageKey, cancellationToken).ConfigureAwait(false);
        if (file is null)
        {
            return null;
        }

        return new CampaignAssetRead
        {
            AssetTag = tag,
            IsNotModified = false,
            File = downloadName is null ? file : file with { DownloadName = downloadName },
        };
    }

    private static bool Matches(string? ifNoneMatch, string tag)
    {
        if (string.IsNullOrWhiteSpace(ifNoneMatch))
        {
            return false;
        }

        // A client or proxy may send several tags, and may weaken them with a W/ prefix.
        foreach (var candidate in ifNoneMatch.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = candidate.StartsWith("W/", StringComparison.Ordinal) ? candidate[2..] : candidate;
            normalized = normalized.Trim('"');
            if (string.Equals(normalized, tag, StringComparison.Ordinal) || string.Equals(normalized, "*", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
