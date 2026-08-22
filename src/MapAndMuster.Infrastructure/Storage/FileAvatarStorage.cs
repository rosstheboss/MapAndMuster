using MapAndMuster.Application.Ports;
using Microsoft.Extensions.Options;

namespace MapAndMuster.Infrastructure.Storage;

/// <summary>
/// Stores avatars on the local filesystem outside the web root.
/// </summary>
public sealed class FileAvatarStorage : IAvatarStorage
{
    private readonly string _rootPath;

    /// <summary>
    /// Initializes storage under the configured root path.
    /// </summary>
    /// <param name="options">Storage options.</param>
    public FileAvatarStorage(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(GetAvatarDirectory());
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(ReadOnlyMemory<byte> content, string fileExtension, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);
        if (!string.Equals(fileExtension, ".jpg", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(fileExtension), "Only re-encoded JPEG avatars can be stored.");
        }

        var key = $"avatars/{Guid.NewGuid():N}.jpg";
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
    public async Task<StoredAvatar?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var path = GetFullPath(storageKey);
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return new StoredAvatar(bytes, "image/jpeg");
    }

    private string GetAvatarDirectory()
    {
        return Path.Combine(_rootPath, "avatars");
    }

    private string GetFullPath(string storageKey)
    {
        if (storageKey.Contains("..", StringComparison.Ordinal) || !storageKey.StartsWith("avatars/", StringComparison.Ordinal))
        {
            throw new ArgumentException("The storage key is not a generated avatar name.", nameof(storageKey));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(_rootPath) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The storage key is not a generated avatar name.", nameof(storageKey));
        }

        return fullPath;
    }
}

/// <summary>
/// Filesystem storage options.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// Gets or sets the root directory for uploaded files. This must not be the web root.
    /// </summary>
    public string RootPath { get; set; } = "app-data";
}
