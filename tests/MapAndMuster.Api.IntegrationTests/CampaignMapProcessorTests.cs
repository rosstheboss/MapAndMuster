using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Ports;
using MapAndMuster.Infrastructure.Storage;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class CampaignMapProcessorTests
{
    [Fact]
    public async Task RejectsADeclaredLengthOverTheUploadCap()
    {
        var processor = new CampaignMapProcessor();
        await using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);
        var result = await processor.ProcessAsync(
            stream,
            "image/png",
            ICampaignMapProcessor.MaxUploadBytes + 1,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.UploadTooLarge, result.ErrorCode);
        Assert.Equal("Campaign maps must be 20 MB or smaller.", result.Message);
    }

    [Fact]
    public async Task AllowsAStoredMapLargerThanTheUploadCapWhenImportingAPackage()
    {
        var processor = new CampaignMapProcessor();
        await using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var result = await processor.ProcessAsync(
            stream,
            "image/png",
            (20 * 1024 * 1024) + 1,
            CancellationToken.None,
            ICampaignMapProcessor.MapMaxDimension,
            ImportCampaignPresetHandler.MaxPackageBytes);

        Assert.False(result.IsSuccess);
        Assert.NotEqual(ErrorCodes.UploadTooLarge, result.ErrorCode);
        Assert.Equal(ErrorCodes.UploadInvalidImage, result.ErrorCode);
    }
}
