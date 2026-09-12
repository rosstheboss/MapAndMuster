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
    /// Returns the directed blocks with this user on either side.
    /// </summary>
    /// <param name="userId">The user whose blocks matter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Blocks the user placed and blocks placed against the user.</returns>
    /// <remarks>
    /// Hiding is decided per viewer, so no caller needs the rest of the table. The default
    /// implementation filters in memory; real storage should filter in the query.
    /// </remarks>
    async Task<IReadOnlyList<SiteChatBlock>> ListBlocksForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var all = await ListBlocksAsync(cancellationToken).ConfigureAwait(false);
        return [.. all.Where(block => block.BlockerUserId == userId || block.BlockedUserId == userId)];
    }

    /// <summary>
    /// Adds or removes the viewer's block of another user.
    /// </summary>
    Task SetBlockAsync(Guid blockerUserId, Guid blockedUserId, bool blocked, CancellationToken cancellationToken);
}
