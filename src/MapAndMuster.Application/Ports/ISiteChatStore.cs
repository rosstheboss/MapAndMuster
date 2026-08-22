using MapAndMuster.Domain.Chat;

namespace MapAndMuster.Application.Ports;

/// <summary>
/// Persistence for public site-wide chat. Campaign logs are never stored here.
/// </summary>
public interface ISiteChatStore
{
    /// <summary>
    /// Returns the newest messages, oldest first, limited to <see cref="SiteChatRules.RecentMessageLimit"/>.
    /// </summary>
    Task<IReadOnlyList<SiteChatMessage>> ListRecentAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Appends a message.
    /// </summary>
    Task AddAsync(SiteChatMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Returns every directed block. Visibility treats either direction as mutual hiding.
    /// </summary>
    Task<IReadOnlyList<SiteChatBlock>> ListBlocksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Adds or removes the viewer's block of another user.
    /// </summary>
    Task SetBlockAsync(Guid blockerUserId, Guid blockedUserId, bool blocked, CancellationToken cancellationToken);
}
