using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MapAndMuster.Application.Common;

/// <summary>
/// Derives opaque, stable cache tags for stored campaign files.
/// </summary>
/// <remarks>
/// Asset URLs used to carry <c>?v={campaignRevision}</c>. The revision changes on every chat
/// message, so every flag and structure logo appeared to be a brand-new resource several times a
/// minute and was downloaded again in full. A tag derived from the storage key changes only when
/// the file behind it is replaced, which lets the response be cached immutably.
///
/// The tag is a truncated SHA-256 of the storage key rather than the key itself, so an internal
/// path never reaches a client, a log, or a proxy cache key.
/// </remarks>
public static class AssetTags
{
    /// <summary>Number of hex characters kept from the hash. 16 hex characters is 64 bits.</summary>
    private const int TagLength = 16;

    /// <summary>
    /// Returns the opaque tag for a storage key.
    /// </summary>
    /// <param name="storageKey">The storage key, or <see langword="null"/> when no file is stored.</param>
    /// <returns>The tag, or <see langword="null"/> when there is no file to tag.</returns>
    public static string? For(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(storageKey));
        return Convert.ToHexStringLower(hash)[..TagLength];
    }

    /// <summary>
    /// Formats a tag as an HTTP entity tag, including the required quotes.
    /// </summary>
    /// <param name="tag">The opaque tag.</param>
    /// <returns>A quoted strong entity tag.</returns>
    public static string ToEntityTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        return string.Create(CultureInfo.InvariantCulture, $"\"{tag}\"");
    }
}
