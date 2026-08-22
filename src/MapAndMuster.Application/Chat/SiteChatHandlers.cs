using MapAndMuster.Application.Common;
using MapAndMuster.Application.Identity;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Chat;
using MapAndMuster.Domain.Play;

namespace MapAndMuster.Application.Chat;

/// <summary>
/// Loads the public site-chat board for the authenticated viewer.
/// </summary>
public sealed class GetSiteChatHandler
{
    private readonly ISiteChatStore _chat;
    private readonly IUserAccountStore _accounts;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public GetSiteChatHandler(ISiteChatStore chat, IUserAccountStore accounts)
    {
        ArgumentNullException.ThrowIfNull(chat);
        ArgumentNullException.ThrowIfNull(accounts);
        _chat = chat;
        _accounts = accounts;
    }

    /// <summary>
    /// Returns recent messages the viewer may see, plus mention and block lists.
    /// </summary>
    public async Task<OperationResult<SiteChatBoard>> HandleAsync(
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return OperationResults.Failure<SiteChatBoard>(ErrorCodes.ProfileNotFound, "The profile was not found.");
        }

        return OperationResults.Success(await LoadBoardAsync(account, isAdministrator, cancellationToken).ConfigureAwait(false));
    }

    internal async Task<SiteChatBoard> LoadBoardAsync(
        UserAccount account,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        var members = await SiteChatMembers.LoadAsync(_accounts, cancellationToken).ConfigureAwait(false);
        var messages = await _chat.ListRecentAsync(cancellationToken).ConfigureAwait(false);
        var blocks = await _chat.ListBlocksAsync(cancellationToken).ConfigureAwait(false);
        var preferred = ChatLanguages.TryParse(account.PreferredChatLanguage, out _, out var language)
            ? language.ToString()
            : ChatLanguages.Default.ToString();
        return SiteChatMapper.ToBoard(messages, members, blocks, account.Id, isAdministrator, preferred);
    }
}

/// <summary>
/// Posts a public site-chat message.
/// </summary>
public sealed class PostSiteChatHandler
{
    private readonly ISiteChatStore _chat;
    private readonly IUserAccountStore _accounts;
    private readonly IClock _clock;
    private readonly GetSiteChatHandler _board;
    private readonly SiteChatNotificationPublisher _notifications;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public PostSiteChatHandler(
        ISiteChatStore chat,
        IUserAccountStore accounts,
        IClock clock,
        GetSiteChatHandler board,
        SiteChatNotificationPublisher notifications)
    {
        ArgumentNullException.ThrowIfNull(chat);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(notifications);
        _chat = chat;
        _accounts = accounts;
        _clock = clock;
        _board = board;
        _notifications = notifications;
    }

    /// <summary>
    /// Appends a message when the caller is signed in.
    /// </summary>
    public async Task<OperationResult<SiteChatBoard>> HandleAsync(
        PostSiteChatCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var account = await _accounts.FindByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return OperationResults.Failure<SiteChatBoard>(ErrorCodes.ProfileNotFound, "The profile was not found.");
        }

        if (account.IsTestAccount)
        {
            return OperationResults.Failure<SiteChatBoard>(
                "sitechat.test_account",
                "Test accounts cannot use public site chat.");
        }

        var members = await SiteChatMembers.LoadAsync(_accounts, cancellationToken).ConfigureAwait(false);
        if (!SiteChatRules.TryPost(
                command.UserId,
                command.Message,
                command.Language,
                members,
                _clock.UtcNow,
                command.IsAdministrator,
                command.SendAsAdministrator,
                command.TargetUserId,
                out var posted,
                out var error))
        {
            return OperationResults.Failure<SiteChatBoard>(error.Code, error.Message);
        }

        await _chat.AddAsync(posted, cancellationToken).ConfigureAwait(false);
        var blocks = await _chat.ListBlocksAsync(cancellationToken).ConfigureAwait(false);
        await _notifications.PublishAsync(posted, members, blocks, cancellationToken).ConfigureAwait(false);
        return OperationResults.Success(await _board.LoadBoardAsync(account, command.IsAdministrator, cancellationToken).ConfigureAwait(false));
    }
}

/// <summary>
/// Adds or removes a person from the viewer's site-chat block list.
/// </summary>
public sealed class SetSiteChatBlockHandler
{
    private readonly ISiteChatStore _chat;
    private readonly IUserAccountStore _accounts;
    private readonly GetSiteChatHandler _board;

    /// <summary>
    /// Initializes a handler.
    /// </summary>
    public SetSiteChatBlockHandler(ISiteChatStore chat, IUserAccountStore accounts, GetSiteChatHandler board)
    {
        ArgumentNullException.ThrowIfNull(chat);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(board);
        _chat = chat;
        _accounts = accounts;
        _board = board;
    }

    /// <summary>
    /// Updates the viewer's block list.
    /// </summary>
    public async Task<OperationResult<SiteChatBoard>> HandleAsync(
        SetSiteChatBlockCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var account = await _accounts.FindByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return OperationResults.Failure<SiteChatBoard>(ErrorCodes.ProfileNotFound, "The profile was not found.");
        }

        var members = await SiteChatMembers.LoadAsync(_accounts, cancellationToken).ConfigureAwait(false);
        if (!SiteChatRules.TryValidateBlock(command.UserId, command.TargetUserId, members, out var error))
        {
            return OperationResults.Failure<SiteChatBoard>(error.Code, error.Message);
        }

        await _chat.SetBlockAsync(command.UserId, command.TargetUserId, command.Blocked, cancellationToken)
            .ConfigureAwait(false);
        return OperationResults.Success(
            await _board.LoadBoardAsync(account, command.IsAdministrator, cancellationToken).ConfigureAwait(false));
    }
}

internal static class SiteChatMembers
{
    public static async Task<IReadOnlyList<CampaignChatMember>> LoadAsync(
        IUserAccountStore accounts,
        CancellationToken cancellationToken)
    {
        var mentionable = await accounts.ListMentionableAsync(cancellationToken).ConfigureAwait(false);
        return
        [
            .. mentionable
                .OrderBy(static item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(static item => new CampaignChatMember(item.UserId, item.Username, item.DisplayName)),
        ];
    }
}
