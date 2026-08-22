using System.IO.Compression;
using System.Text;
using MapAndMuster.Application.Common;
using MapAndMuster.Infrastructure.Campaigns;

namespace MapAndMuster.Api.IntegrationTests;

public sealed class CampaignPresetPackageCodecTests
{
    [Fact]
    public void ReadRejectsAnEmptyArchive()
    {
        var codec = new CampaignPresetPackageCodec();
        var result = codec.Read(Array.Empty<byte>());
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignPresetPackageInvalid, result.ErrorCode);
    }

    [Fact]
    public void ReadRejectsAZipWithoutAManifest()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("catalog.json");
            using var stream = entry.Open();
            stream.Write(Encoding.UTF8.GetBytes("{}"));
        }

        var codec = new CampaignPresetPackageCodec();
        var result = codec.Read(output.ToArray());
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignPresetPackageInvalid, result.ErrorCode);
    }

    [Fact]
    public void ReadRejectsZipSlipPaths()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("assets/maps/../../secret.txt");
            using var stream = entry.Open();
            stream.Write(Encoding.UTF8.GetBytes("nope"));
        }

        var codec = new CampaignPresetPackageCodec();
        var result = codec.Read(output.ToArray());
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignPresetPackageInvalid, result.ErrorCode);
    }
}
