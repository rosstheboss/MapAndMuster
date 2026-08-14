using Campaign.Application.Common;
using Campaign.Application.Ports;

namespace Campaign.Infrastructure.Storage;

/// <summary>
/// Validates uploaded mission documents as PDF or DOCX.
/// </summary>
public sealed class CampaignDocumentProcessor : ICampaignDocumentProcessor
{
    private static readonly byte[] PdfMagic = "%PDF"u8.ToArray();
    private static readonly byte[] ZipMagic = [0x50, 0x4B, 0x03, 0x04];

    /// <inheritdoc />
    public async Task<ProcessedCampaignDocumentResult> ProcessAsync(
        Stream content,
        string contentType,
        string fileName,
        long? length,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (length is > ICampaignDocumentProcessor.MaxUploadBytes)
        {
            return Fail(ErrorCodes.UploadTooLarge, "The mission file is too large (maximum 10 MB).");
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (buffer.Length > ICampaignDocumentProcessor.MaxUploadBytes)
        {
            return Fail(ErrorCodes.UploadTooLarge, "The mission file is too large (maximum 10 MB).");
        }

        var bytes = buffer.ToArray();
        if (bytes.Length < 4)
        {
            return Fail(ErrorCodes.UploadInvalidType, "The mission file must be a PDF or Word document.");
        }

        var extension = Path.GetExtension(fileName);
        var declared = contentType.Split(';', 2)[0].Trim();
        if (LooksLikePdf(bytes, declared, extension))
        {
            return new ProcessedCampaignDocumentResult
            {
                IsSuccess = true,
                Content = bytes,
                FileExtension = ".pdf",
                ContentType = "application/pdf",
                FileName = SafeFileName(fileName, ".pdf"),
            };
        }

        if (LooksLikeDocx(bytes, declared, extension))
        {
            return new ProcessedCampaignDocumentResult
            {
                IsSuccess = true,
                Content = bytes,
                FileExtension = ".docx",
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileName = SafeFileName(fileName, ".docx"),
            };
        }

        return Fail(ErrorCodes.UploadInvalidType, "The mission file must be a PDF or Word document.");
    }

    private static bool LooksLikePdf(byte[] bytes, string contentType, string extension)
    {
        var header = bytes.AsSpan(0, Math.Min(5, bytes.Length));
        return header.StartsWith(PdfMagic)
            && (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(contentType));
    }

    private static bool LooksLikeDocx(byte[] bytes, string contentType, string extension)
    {
        var header = bytes.AsSpan(0, Math.Min(4, bytes.Length));
        var zip = header.SequenceEqual(ZipMagic);
        var named = string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("officedocument", StringComparison.OrdinalIgnoreCase);
        return zip && named;
    }

    private static string SafeFileName(string fileName, string extension)
    {
        var name = Path.GetFileNameWithoutExtension(string.IsNullOrWhiteSpace(fileName) ? "mission" : fileName);
        name = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (name.Length == 0)
        {
            name = "mission";
        }

        if (name.Length > 80)
        {
            name = name[..80];
        }

        return name + extension;
    }

    private static ProcessedCampaignDocumentResult Fail(string code, string message)
    {
        return new ProcessedCampaignDocumentResult
        {
            IsSuccess = false,
            ErrorCode = code,
            Message = message,
        };
    }
}
