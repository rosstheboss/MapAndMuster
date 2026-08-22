using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Chat;
using Microsoft.EntityFrameworkCore;

namespace MapAndMuster.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL store for public site-wide chat.
/// </summary>
public sealed class SiteChatStore : ISiteChatStore
{
    private readonly CampaignDbContext _dbContext;

    /// <summary>
    /// Initializes a store.
    /// </summary>
    public SiteChatStore(CampaignDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SiteChatMessage>> ListRecentAsync(CancellationToken cancellationToken)
    {
        var records = await _dbContext.SiteChatMessages
            .AsNoTracking()
            .OrderByDescending(item => item.PostedUtc)
            .ThenByDescending(item => item.Id)
            .Take(SiteChatRules.RecentMessageLimit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        records.Reverse();
        return [.. records.Select(Map)];
    }

    /// <inheritdoc />
    public async Task AddAsync(SiteChatMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        _dbContext.SiteChatMessages.Add(new SiteChatMessageRecord
        {
            Id = message.Id,
            PostedUtc = message.PostedUtc,
            AuthorUserId = message.AuthorUserId,
            AuthorUsername = message.AuthorUsername,
            AuthorDisplayName = message.AuthorDisplayName,
            Body = message.Body,
            Language = message.Language.ToString(),
            Kind = message.Kind.ToString(),
            TargetUserId = message.TargetUserId,
            TargetUsername = message.TargetUsername,
            TargetDisplayName = message.TargetDisplayName,
        });
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SiteChatBlock>> ListBlocksAsync(CancellationToken cancellationToken)
    {
        var records = await _dbContext.SiteChatBlocks
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. records.Select(static item => new SiteChatBlock(item.BlockerUserId, item.BlockedUserId))];
    }

    /// <inheritdoc />
    public async Task SetBlockAsync(Guid blockerUserId, Guid blockedUserId, bool blocked, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.SiteChatBlocks
            .FirstOrDefaultAsync(
                item => item.BlockerUserId == blockerUserId && item.BlockedUserId == blockedUserId,
                cancellationToken)
            .ConfigureAwait(false);
        if (blocked)
        {
            if (existing is null)
            {
                _dbContext.SiteChatBlocks.Add(new SiteChatBlockRecord
                {
                    BlockerUserId = blockerUserId,
                    BlockedUserId = blockedUserId,
                });
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (existing is not null)
        {
            _dbContext.SiteChatBlocks.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static SiteChatMessage Map(SiteChatMessageRecord record)
    {
        _ = Enum.TryParse(record.Language, ignoreCase: true, out ChatLanguage language);
        if (!Enum.IsDefined(language))
        {
            language = ChatLanguages.Default;
        }

        _ = Enum.TryParse(record.Kind, ignoreCase: true, out SiteChatKind kind);
        if (!Enum.IsDefined(kind))
        {
            kind = SiteChatKind.Player;
        }

        return new SiteChatMessage(
            record.Id,
            record.PostedUtc,
            record.AuthorUserId,
            record.AuthorUsername,
            record.AuthorDisplayName,
            record.Body,
            language,
            kind,
            record.TargetUserId,
            record.TargetUsername,
            record.TargetDisplayName);
    }
}
