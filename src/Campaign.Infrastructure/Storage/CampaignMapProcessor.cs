using System.Text;
using Campaign.Application.Common;
using Campaign.Application.Ports;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace Campaign.Infrastructure.Storage;

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
        int maxDimension = DefaultMaxDimension)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (length > ICampaignMapProcessor.MaxUploadBytes)
        {
            return Fail(ErrorCodes.UploadTooLarge, "Campaign maps must be 20 MB or smaller.");
        }

        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            return Fail(ErrorCodes.UploadInvalidType, "Campaign maps must be JPEG, PNG, or WebP images.");
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (buffer.Length > ICampaignMapProcessor.MaxUploadBytes)
        {
            return Fail(ErrorCodes.UploadTooLarge, "Campaign maps must be 20 MB or smaller.");
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
        try
        {
            using var image = await Image.LoadAsync(buffer, cancellationToken).ConfigureAwait(false);
            image.Mutate(processor => processor.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(dimension, dimension),
            }));

            await using var output = new MemoryStream();
            await image.SaveAsPngAsync(output, new PngEncoder(), cancellationToken).ConfigureAwait(false);
            return new ProcessedCampaignMapResult
            {
                IsSuccess = true,
                Content = output.ToArray(),
                FileExtension = ".png",
            };
        }
        catch (UnknownImageFormatException)
        {
            return Fail(ErrorCodes.UploadInvalidImage, "The campaign map could not be read.");
        }
        catch (InvalidImageContentException)
        {
            return Fail(ErrorCodes.UploadInvalidImage, "The campaign map could not be read.");
        }
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
