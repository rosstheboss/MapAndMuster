using System.Buffers.Binary;
using System.IO.Compression;
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

    [Fact]
    public async Task ReencodesAPngAndShrinksItToTheRequestedBound()
    {
        var processor = new CampaignMapProcessor();
        await using var stream = new MemoryStream(CreateSolidPng(width: 4, height: 2));

        var result = await processor.ProcessAsync(
            stream,
            "image/png",
            stream.Length,
            CancellationToken.None,
            maxDimension: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(".png", result.FileExtension);
        Assert.NotNull(result.Content);
        Assert.Equal((2, 1), ReadPngSize(result.Content));
    }

    private static (int Width, int Height) ReadPngSize(byte[] png)
    {
        Assert.True(png.Length >= 24);
        Assert.Equal(0x89, png[0]);
        Assert.Equal((byte)'P', png[1]);
        Assert.Equal((byte)'N', png[2]);
        Assert.Equal((byte)'G', png[3]);
        return (
            BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
    }

    private static byte[] CreateSolidPng(int width, int height)
    {
        var rowStride = (width * 3) + 1;
        var raw = new byte[rowStride * height];
        for (var y = 0; y < height; y++)
        {
            var row = y * rowStride;
            raw[row] = 0;
            for (var x = 0; x < width; x++)
            {
                var pixel = row + 1 + (x * 3);
                raw[pixel] = 180;
                raw[pixel + 1] = 20;
                raw[pixel + 2] = 20;
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        var idat = compressed.ToArray();
        using var png = new MemoryStream();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        WriteChunk(png, "IHDR", Ihdr(width, height));
        WriteChunk(png, "IDAT", idat);
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static byte[] Ihdr(int width, int height)
    {
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 8;
        header[9] = 2;
        return header;
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);
        var crcInput = new byte[typeBytes.Length + data.Length];
        typeBytes.CopyTo(crcInput, 0);
        data.CopyTo(crcInput, typeBytes.Length);
        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(crcInput));
        output.Write(crc);
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                var mask = (uint)-(int)(crc & 1u);
                crc = (crc >> 1) ^ (0xEDB88320u & mask);
            }
        }

        return ~crc;
    }
}
