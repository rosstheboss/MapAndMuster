using MapAndMuster.Application.Notifications;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Chat;
using MapAndMuster.Domain.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace MapAndMuster.Api.IntegrationTests;

/// <summary>
/// Covers the store reads and writes that previously loaded whole tables. Each test asserts the
/// behavior the narrowed query must keep, and the fan-out test also asserts the statement count so
/// a regression to per-recipient writes fails here rather than only showing up as host load.
/// </summary>
[Collection("api")]
public sealed class StoreQueryShapeTests
{
    private readonly MapAndMusterApiFactory _factory;

    public StoreQueryShapeTests(MapAndMusterApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotificationFanOutCostDoesNotGrowWithRecipientCount()
    {
        var oneStatements = await CountFanOutStatementsAsync(recipients: 1);
        var manyStatements = await CountFanOutStatementsAsync(recipients: 8);

        Assert.True(oneStatements > 0, "No database statements were observed for the fan-out.");
        Assert.True(
            oneStatements == manyStatements,
            $"Notifying 1 user issued {oneStatements} statements and notifying 8 issued {manyStatements}. "
                + "A fan-out must be one dedupe read plus one write.");
    }

    [Fact]
    public async Task ANoticeAlreadyStoredIsNotInsertedAgain()
    {
        var userId = Guid.NewGuid();
        var notice = Notice(userId, "shape:duplicate");

        using var scope = _factory.Services.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<IUserNotificationStore>();

        var first = await notifications.TryAddManyAsync([notice], DateTimeOffset.UnixEpoch, TestToken);
        Assert.Contains("shape:duplicate", first);

        var second = await notifications.TryAddManyAsync([notice], DateTimeOffset.UnixEpoch, TestToken);
        Assert.Empty(second);

        var unread = await notifications.ListUnreadAsync(userId, TestToken);
        Assert.Single(unread);
    }

    [Fact]
    public async Task MarkingEveryNoticeReadReportsHowManyChanged()
    {
        var userId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<IUserNotificationStore>();
        await notifications.TryAddManyAsync(
            [Notice(userId, "shape:read:1"), Notice(userId, "shape:read:2")],
            DateTimeOffset.UnixEpoch,
            TestToken);

        var marked = await notifications.MarkAllReadAsync(userId, DateTimeOffset.UnixEpoch.AddHours(1), TestToken);
        Assert.Equal(2, marked);
        Assert.Empty(await notifications.ListUnreadAsync(userId, TestToken));

        // Nothing left to change, so the second pass must report no rows rather than re-stamping.
        Assert.Equal(0, await notifications.MarkAllReadAsync(userId, DateTimeOffset.UnixEpoch.AddHours(2), TestToken));
    }

    [Fact]
    public async Task BlocksAreReadForOneViewerInBothDirections()
    {
        var viewer = Guid.NewGuid();
        var blockedByViewer = Guid.NewGuid();
        var blockerOfViewer = Guid.NewGuid();
        var unrelatedOne = Guid.NewGuid();
        var unrelatedTwo = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var chat = scope.ServiceProvider.GetRequiredService<ISiteChatStore>();
        await chat.SetBlockAsync(viewer, blockedByViewer, blocked: true, TestToken);
        await chat.SetBlockAsync(blockerOfViewer, viewer, blocked: true, TestToken);
        await chat.SetBlockAsync(unrelatedOne, unrelatedTwo, blocked: true, TestToken);

        var forViewer = await chat.ListBlocksForUserAsync(viewer, TestToken);

        Assert.Contains(new SiteChatBlock(viewer, blockedByViewer), forViewer);
        Assert.Contains(new SiteChatBlock(blockerOfViewer, viewer), forViewer);
        Assert.DoesNotContain(new SiteChatBlock(unrelatedOne, unrelatedTwo), forViewer);
    }

    private async Task<int> CountFanOutStatementsAsync(int recipients)
    {
        var run = Guid.NewGuid();
        var notices = Enumerable.Range(0, recipients)
            .Select(index => Notice(Guid.NewGuid(), $"shape:fanout:{run:N}:{index}"))
            .ToArray();

        using var scope = _factory.Services.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<IUserNotificationStore>();

        // Warm the query cache first so compilation is not attributed to the measured call.
        await notifications.TryAddManyAsync([Notice(Guid.NewGuid(), $"shape:warm:{run:N}")], DateTimeOffset.UnixEpoch, TestToken);

        using var counter = new EfCommandCounter();
        counter.Start();
        var accepted = await notifications.TryAddManyAsync(notices, DateTimeOffset.UnixEpoch, TestToken);
        counter.Stop();

        Assert.Equal(recipients, accepted.Count);
        return counter.Count;
    }

    private static NewUserNotification Notice(Guid userId, string dedupeKey)
    {
        return new NewUserNotification
        {
            UserId = userId,
            Kind = NotificationKind.Mention,
            CampaignId = null,
            CampaignName = null,
            Title = "A frontier dispatch",
            Body = "Someone mentioned you on the board.",
            Path = "/chat",
            DedupeKey = dedupeKey,
        };
    }

    private static CancellationToken TestToken => CancellationToken.None;
}
