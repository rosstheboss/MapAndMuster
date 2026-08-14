using Campaign.Application.Ports;
using Microsoft.Extensions.Options;

namespace Campaign.Infrastructure.Storage;

/// <summary>
/// Stores campaign maps on the local filesystem outside the web root.
/// </summary>
public sealed class FileCampaignMapStorage : ICampaignMapStorage
{
    private readonly string _rootPath;

    /// <summary>
    /// Initializes storage under the configured root path.
    /// </summary>
    /// <param name="options">Storage options.</param>
    public FileCampaignMapStorage(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(GetMapDirectory());
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(ReadOnlyMemory<byte> content, string fileExtension, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);
        if (!string.Equals(fileExtension, ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(fileExtension), "Only re-encoded PNG campaign maps can be stored.");
        }

        var key = $"maps/{Guid.NewGuid():N}.png";
        var path = GetFullPath(key);
        await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken).ConfigureAwait(false);
        return key;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetFullPath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<StoredCampaignMap?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var path = GetFullPath(storageKey);
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return new StoredCampaignMap(bytes, "image/png");
    }

    private string GetMapDirectory()
    {
        return Path.Combine(_rootPath, "maps");
    }

    private string GetFullPath(string storageKey)
    {
        if (storageKey.Contains("..", StringComparison.Ordinal) || !storageKey.StartsWith("maps/", StringComparison.Ordinal))
        {
            throw new ArgumentException("The storage key is not a generated campaign map name.", nameof(storageKey));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(_rootPath) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The storage key is not a generated campaign map name.", nameof(storageKey));
        }

        return fullPath;
    }
}
