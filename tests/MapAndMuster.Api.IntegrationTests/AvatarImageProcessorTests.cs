using MapAndMuster.Application.Common;
using MapAndMuster.Infrastructure.Storage;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class AvatarImageProcessorTests
{
    [Fact]
    public async Task ReencodesAPngAvatarAsJpeg()
    {
        var processor = new AvatarImageProcessor();
        var png = CreateOnePixelPng();
        await using var stream = new MemoryStream(png);

        var result = await processor.ProcessAsync(stream, "image/png", png.Length, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(".jpg", result.FileExtension);
        Assert.NotNull(result.Content);
        Assert.True(result.Content.Length >= 3);
        Assert.Equal(0xFF, result.Content[0]);
        Assert.Equal(0xD8, result.Content[1]);
        Assert.Equal(0xFF, result.Content[2]);
    }

    [Fact]
    public async Task RejectsAPngSignatureThatIsNotAnImage()
    {
        var processor = new AvatarImageProcessor();
        await using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]);

        var result = await processor.ProcessAsync(stream, "image/png", stream.Length, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.UploadInvalidImage, result.ErrorCode);
    }

    private static byte[] CreateOnePixelPng()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
    }
}
