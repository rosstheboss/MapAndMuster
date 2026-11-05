using SkiaSharp;

namespace MapAndMuster.Infrastructure.Storage;

/// <summary>
/// Decodes a raster image, fits it inside a square bound, and re-encodes the pixels.
/// </summary>
internal static class RasterImageTranscoder
{
    /// <summary>
    /// Fits the image inside <paramref name="maxDimension"/> and returns the encoded bytes.
    /// </summary>
    /// <param name="content">The uploaded image, positioned at the start.</param>
    /// <param name="maxDimension">The largest width or height to keep.</param>
    /// <param name="format">The output encoding.</param>
    /// <param name="quality">The encoder quality, from 0 through 100.</param>
    /// <returns>The encoded image, or <see langword="null"/> when the bytes cannot be decoded.</returns>
    public static byte[]? EncodeFitted(Stream content, int maxDimension, SKEncodedImageFormat format, int quality)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            using var data = SKData.Create(content);
            if (data is null || data.IsEmpty)
            {
                return null;
            }

            using var codec = SKCodec.Create(data);
            if (codec is null || codec.Info.Width < 1 || codec.Info.Height < 1)
            {
                return null;
            }

            using var bitmap = SKBitmap.Decode(codec);
            if (bitmap is null)
            {
                return null;
            }

            var (width, height) = FitWithin(bitmap.Width, bitmap.Height, maxDimension);
            SKBitmap? resized = null;
            try
            {
                var source = bitmap;
                if (width != bitmap.Width || height != bitmap.Height)
                {
                    resized = bitmap.Resize(
                        new SKImageInfo(width, height, bitmap.ColorType, bitmap.AlphaType),
                        new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
                    if (resized is null)
                    {
                        return null;
                    }

                    source = resized;
                }

                using var image = SKImage.FromBitmap(source);
                using var encoded = image.Encode(format, quality);
                return encoded?.ToArray();
            }
            finally
            {
                resized?.Dispose();
            }
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static (int Width, int Height) FitWithin(int width, int height, int maxDimension)
    {
        if (width <= maxDimension && height <= maxDimension)
        {
            return (width, height);
        }

        var scale = Math.Min(maxDimension / (double)width, maxDimension / (double)height);
        var fittedWidth = Math.Max(1, (int)Math.Floor(width * scale));
        var fittedHeight = Math.Max(1, (int)Math.Floor(height * scale));
        return (fittedWidth, fittedHeight);
    }
}
