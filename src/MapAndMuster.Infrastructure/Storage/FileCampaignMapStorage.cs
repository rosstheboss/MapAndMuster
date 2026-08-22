using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;
using Microsoft.Extensions.Options;

namespace MapAndMuster.Infrastructure.Storage;

/// <summary>
/// Stores campaign maps and catalog files on the local filesystem outside the web root.
/// </summary>
public sealed class FileCampaignMapStorage : ICampaignMapStorage, ICampaignAssetStorage
{
    private static readonly HashSet<string> AllowedFolders = new(StringComparer.Ordinal)
    {
        "maps",
        "structures",
        "flags",
        "missions",
        "items",
    };

    private readonly string _rootPath;

    /// <summary>
    /// Initializes storage under the configured root path.
    /// </summary>
    /// <param name="options">Storage options.</param>
    public FileCampaignMapStorage(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(Path.Combine(_rootPath, "maps"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "structures"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "flags"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "missions"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "items"));
    }

    /// <inheritdoc />
    public Task<string> SaveAsync(ReadOnlyMemory<byte> content, string fileExtension, CancellationToken cancellationToken)
    {
        return SaveAsync("maps", content, fileExtension, "image/png", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(
        string folder,
        ReadOnlyMemory<byte> content,
        string fileExtension,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (!AllowedFolders.Contains(folder))
        {
            throw new ArgumentOutOfRangeException(nameof(folder), "The storage folder is not allowed.");
        }

        if (string.Equals(folder, "maps", StringComparison.Ordinal)
            && !string.Equals(fileExtension, ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(fileExtension), "Only re-encoded PNG campaign maps can be stored.");
        }

        var extension = fileExtension.StartsWith('.')
            ? fileExtension.ToLowerInvariant()
            : $".{fileExtension.ToLowerInvariant()}";
        var key = $"{folder}/{Guid.NewGuid():N}{extension}";
        var path = GetFullPath(key);
        await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken).ConfigureAwait(false);
        return key;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        cancellationToken.ThrowIfCancellationRequested();
        if (CampaignCatalogDefaults.CanonicalBuiltinSymbol(storageKey) is not null)
        {
            return Task.CompletedTask;
        }

        var path = GetFullPath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    async Task<StoredCampaignMap?> ICampaignMapStorage.OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var asset = await OpenReadAsync(storageKey, cancellationToken).ConfigureAwait(false);
        return asset is null ? null : new StoredCampaignMap(asset.Content, asset.ContentType);
    }

    /// <inheritdoc />
    public async Task<StoredCampaignAsset?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var path = GetFullPath(storageKey);
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return new StoredCampaignAsset(bytes, ContentTypeFor(storageKey));
    }

    private string GetFullPath(string storageKey)
    {
        var slash = storageKey.IndexOf('/', StringComparison.Ordinal);
        var folder = slash > 0 ? storageKey[..slash] : string.Empty;
        if (storageKey.Contains("..", StringComparison.Ordinal) || !AllowedFolders.Contains(folder))
        {
            throw new ArgumentException("The storage key is not a generated campaign file name.", nameof(storageKey));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(_rootPath) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The storage key is not a generated campaign file name.", nameof(storageKey));
        }

        return fullPath;
    }

    private static string ContentTypeFor(string storageKey)
    {
        var extension = Path.GetExtension(storageKey);
        if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
        {
            return "image/png";
        }

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "application/pdf";
        }

        if (string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
        {
            return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        }

        return "application/octet-stream";
    }
}
