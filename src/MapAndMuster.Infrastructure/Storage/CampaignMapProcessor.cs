using System.Text;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Ports;
using SkiaSharp;

namespace MapAndMuster.Infrastructure.Storage;

/// <summary>
/// Validates and re-encodes raster campaign maps. SVG and other active content are rejected.
/// </summary>
public sealed class CampaignMapProcessor : ICampaignMapProcessor
{
    private const int DefaultMaxDimension = ICampaignMapProcessor.MapMaxDimension;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
    };

    /// <inheritdoc />
    public async Task<ProcessedCampaignMapResult> ProcessAsync(
        Stream content,
        string contentType,
        long? length,
        CancellationToken cancellationToken,
        int maxDimension = DefaultMaxDimension,
        long maxBytes = ICampaignMapProcessor.MaxUploadBytes)
    {
        ArgumentNullException.ThrowIfNull(content);
        var limit = maxBytes < 1 ? ICampaignMapProcessor.MaxUploadBytes : maxBytes;

        if (length > limit)
        {
            return Fail(ErrorCodes.UploadTooLarge, MapTooLargeMessage(limit));
        }

        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            return Fail(ErrorCodes.UploadInvalidType, "Campaign maps must be JPEG, PNG, or WebP images.");
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (buffer.Length > limit)
        {
            return Fail(ErrorCodes.UploadTooLarge, MapTooLargeMessage(limit));
        }

        if (buffer.Length == 0 || LooksLikeMarkup(buffer))
        {
            return Fail(ErrorCodes.UploadInvalidType, "Campaign maps must be JPEG, PNG, or WebP images.");
        }

        if (!HasAllowedMagicBytes(buffer))
        {
            return Fail(ErrorCodes.UploadInvalidType, "Campaign maps must be JPEG, PNG, or WebP images.");
        }

        buffer.Position = 0;
        var dimension = maxDimension < 1 ? DefaultMaxDimension : maxDimension;
        cancellationToken.ThrowIfCancellationRequested();
        var encoded = RasterImageTranscoder.EncodeFitted(buffer, dimension, SKEncodedImageFormat.Png, quality: 100);
        if (encoded is null)
        {
            return Fail(ErrorCodes.UploadInvalidImage, "The campaign map could not be read.");
        }

        return new ProcessedCampaignMapResult
        {
            IsSuccess = true,
            Content = encoded,
            FileExtension = ".png",
        };
    }

    private static ProcessedCampaignMapResult Fail(string code, string message)
    {
        return new ProcessedCampaignMapResult
        {
            IsSuccess = false,
            ErrorCode = code,
            Message = message,
        };
    }

    private static string MapTooLargeMessage(long maxBytes)
    {
        return $"Campaign maps must be {maxBytes / (1024 * 1024)} MB or smaller.";
    }

    private static bool LooksLikeMarkup(MemoryStream buffer)
    {
        var prefixLength = (int)Math.Min(64, buffer.Length);
        var prefix = Encoding.UTF8.GetString(buffer.GetBuffer().AsSpan(0, prefixLength)).TrimStart();
        return prefix.StartsWith('<');
    }

    private static bool HasAllowedMagicBytes(MemoryStream buffer)
    {
        var bytes = buffer.GetBuffer().AsSpan(0, (int)Math.Min(12, buffer.Length));
        var isJpeg = bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        var isPng = bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47;
        var isWebp = bytes.Length >= 12
            && bytes[0] == (byte)'R'
            && bytes[1] == (byte)'I'
            && bytes[2] == (byte)'F'
            && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W'
            && bytes[9] == (byte)'E'
            && bytes[10] == (byte)'B'
            && bytes[11] == (byte)'P';
        return isJpeg || isPng || isWebp;
    }
}
