using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Identity;
using Campaign.Application.Maps;
using Campaign.Application.Notifications;
using Campaign.Application.Play;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Identity;
using Campaign.Domain.Notifications;

namespace Campaign.Backend.UnitTests.Campaigns;

public sealed class CampaignHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ThirdUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid NorthFactionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SouthFactionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid TownStructureId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePersistsManagerMembershipAndHashesJoinPassword()
    {
        var store = new FakeCampaignStore();
        var secrets = new FakeSecretHasher();
        var handler = new CreateCampaignHandler(store, new FakeClock(), secrets);

        var result = await handler.HandleAsync(ValidCreateCommand(isPrivate: true, joinPassword: "join-secret"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanManage);
        Assert.True(result.Value.IsParticipant);
        Assert.Equal(1, result.Value.OccupiedPlayerSlots);
        Assert.Null(GetHashProperty(result.Value));
        Assert.Equal("hash:join-secret", store.Added!.JoinPasswordHash);
        Assert.DoesNotContain("join-secret", result.Value.Name, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRejectsInvalidSetupBeforeSaving()
    {
        var store = new FakeCampaignStore();
        var handler = new CreateCampaignHandler(store, new FakeClock(), new FakeSecretHasher());
        var command = ValidCreateCommand();
        command = new CreateCampaignCommand
        {
            UserId = command.UserId,
            Name = "x",
            PlayerCount = 8,
            IsPrivate = false,
            IsPubliclyViewable = true,
            CreatorIsParticipant = true,
            Factions = command.Factions,
            Schedule = ValidSchedule(),
        };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(store.Added);
    }

    [Fact]
    public async Task GetReturnsNotFoundForNonMembers()
    {
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var handler = new GetCampaignHandler(store, new FakeClock(), new FakeAccounts());

        var result = await handler.HandleAsync(store.Existing.Id, OtherUserId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetReturnsPubliclyViewableCampaignsToNonMembers()
    {
        var campaign = StoredCampaignFor(UserId);
        campaign = WithPublicView(campaign, isPubliclyViewable: true);
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new GetCampaignHandler(store, new FakeClock(), new FakeAccounts());

        var result = await handler.HandleAsync(store.Existing.Id, OtherUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Border War", result.Value.Name);
        Assert.False(result.Value.CanManage);
        Assert.False(result.Value.IsParticipant);
        Assert.True(result.Value.IsPubliclyViewable);
        Assert.False(result.Value.CanChat);
        Assert.Contains(result.Value.MentionableMembers, member => member.Username == "northplayer");
        Assert.Contains(
            result.Value.Participants,
            participant => participant.Username == "northplayer" && participant.IsGameMaster && participant.IsPlayer);
    }

    [Fact]
    public async Task GetListsParticipantFactionAndAdministratorRole()
    {
        var campaign = WithCopied(
            StoredCampaignFor(UserId),
            memberships:
            [
                new StoredCampaignMembership
                {
                    UserId = UserId,
                    IsGameMaster = true,
                    IsPlayer = true,
                    FactionId = NorthFactionId,
                    Subfaction = "Riders",
                },
                new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
            ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var accounts = new FakeAccounts { AdministratorIds = { OtherUserId } };
        var handler = new GetCampaignHandler(store, new FakeClock(), accounts);

        var result = await handler.HandleAsync(campaign.Id, UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var manager = Assert.Single(result.Value.Participants, participant => participant.Username == "northplayer");
        Assert.True(manager.IsGameMaster);
        Assert.True(manager.IsPlayer);
        Assert.False(manager.IsAdministrator);
        Assert.Equal("North", manager.FactionName);
        Assert.Equal("Riders", manager.Subfaction);
        var player = Assert.Single(result.Value.Participants, participant => participant.Username == "southplayer");
        Assert.True(player.IsPlayer);
        Assert.False(player.IsGameMaster);
        Assert.True(player.IsAdministrator);
        Assert.Null(player.FactionName);
    }

    [Fact]
    public async Task PublicViewerCanReadPlayStateButCannotDraft()
    {
        var campaign = WithCopied(
            WithPublicView(StoredCampaignFor(UserId), isPubliclyViewable: true),
            isPrivate: false,
            joinPasswordHash: null,
            startsUtc: Now.AddHours(-1),
            endsUtc: Now.AddDays(40));
        var store = new FakeCampaignStore { Existing = campaign };
        var accounts = new FakeAccounts();
        var get = new GetCampaignPlayHandler(store, new FakeClock(), accounts);

        var viewed = await get.HandleAsync(campaign.Id, OtherUserId, false, CancellationToken.None);

        Assert.True(viewed.IsSuccess);
        Assert.NotNull(viewed.Value);
        Assert.False(viewed.Value.CanChat);
        Assert.False(viewed.Value.IsParticipant);
        Assert.Empty(viewed.Value.MyDrafts);

        var draft = new SaveOrderDraftHandler(store, new FakeClock(), accounts);
        var saved = await draft.HandleAsync(
            new SaveOrderDraftCommand
            {
                UserId = OtherUserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                ExpectedRevision = viewed.Value.Revision,
                ForceId = Guid.NewGuid(),
                Kind = "Hold",
            },
            CancellationToken.None);

        Assert.False(saved.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, saved.ErrorCode);
    }

    [Fact]
    public async Task MemberCanPostChatOnAnUpcomingCampaign()
    {
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var handler = new PostCampaignChatHandler(store, new FakeClock(), new FakeAccounts());

        var result = await handler.HandleAsync(
            new PostCampaignChatCommand
            {
                UserId = UserId,
                IsAdministrator = false,
                CampaignId = store.Existing.Id,
                ExpectedRevision = 1,
                Message = "Hey, everybody! This is a message to all of you.",
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var entry = Assert.Single(result.Value.Log);
        Assert.Equal("PlayerChat", entry.Kind);
        Assert.Equal("northplayer", entry.Originator);
        Assert.Equal("Hey, everybody! This is a message to all of you.", entry.Summary);
        Assert.True(result.Value.CanChat);
    }

    [Fact]
    public async Task NonMemberCannotPostChat()
    {
        var campaign = WithPublicView(StoredCampaignFor(UserId), isPubliclyViewable: true);
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new PostCampaignChatHandler(store, new FakeClock(), new FakeAccounts());

        var result = await handler.HandleAsync(
            new PostCampaignChatCommand
            {
                UserId = OtherUserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Message = "Hello",
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task ChatRejectsMentionsOfPeopleWhoHaveNotJoined()
    {
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var handler = new PostCampaignChatHandler(store, new FakeClock(), new FakeAccounts());

        var result = await handler.HandleAsync(
            new PostCampaignChatCommand
            {
                UserId = UserId,
                IsAdministrator = false,
                CampaignId = store.Existing.Id,
                ExpectedRevision = 1,
                Message = "Hi @stranger",
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("chat.mention.unknown", result.ErrorCode);
    }

    [Fact]
    public async Task PrivateChatIsVisibleToTheAudienceAndHiddenFromManagers()
    {
        var campaign = WithMemberships(
            StoredCampaignFor(UserId),
            [
                new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = true },
                new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
                new StoredCampaignMembership { UserId = ThirdUserId, IsGameMaster = false, IsPlayer = true },
            ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var accounts = new FakeAccounts();
        var chat = new PostCampaignChatHandler(store, new FakeClock(), accounts);

        var posted = await chat.HandleAsync(
            new PostCampaignChatCommand
            {
                UserId = OtherUserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Message = "Meet at the river",
                ChannelKind = "Direct",
                TargetId = ThirdUserId,
            },
            CancellationToken.None);

        Assert.True(posted.IsSuccess);
        Assert.NotNull(posted.Value);
        var privateEntry = Assert.Single(posted.Value.Log);
        Assert.True(privateEntry.IsPrivate);
        Assert.Equal("Direct", privateEntry.ChannelKind);
        Assert.Equal("southplayer", privateEntry.OriginatorUsername);

        var get = new GetCampaignHandler(store, new FakeClock(), accounts);
        var senderView = await get.HandleAsync(campaign.Id, OtherUserId, CancellationToken.None);
        var recipientView = await get.HandleAsync(campaign.Id, ThirdUserId, CancellationToken.None);
        var managerView = await get.HandleAsync(campaign.Id, UserId, CancellationToken.None);
        Assert.Contains(senderView.Value!.Log, item => item.Summary == "Meet at the river");
        Assert.Contains(recipientView.Value!.Log, item => item.Summary == "Meet at the river");
        Assert.DoesNotContain(managerView.Value!.Log, item => item.Summary == "Meet at the river");

        var adminWithoutDebug = await get.HandleAsync(campaign.Id, UserId, CancellationToken.None, isAdministrator: true);
        Assert.DoesNotContain(adminWithoutDebug.Value!.Log, item => item.Summary == "Meet at the river");

        store.Existing = WithCopied(
            store.Existing!,
            playState: (store.Existing!.PlayState ?? Campaign.Domain.Play.CampaignPlayState.Empty)
                .With(debugActorUserId: UserId, debugStartedUtc: Now));
        var adminDebug = await get.HandleAsync(campaign.Id, UserId, CancellationToken.None, isAdministrator: true);
        Assert.Contains(adminDebug.Value!.Log, item => item.Summary == "Meet at the river");
        Assert.True(adminDebug.Value.CanInspectPrivateChat);
    }

    [Fact]
    public async Task JoinAddsPlayerWhenUpcomingAndPublic()
    {
        var campaign = WithPublicView(StoredCampaignFor(UserId), isPubliclyViewable: true);
        campaign = WithPrivacy(campaign, isPrivate: false, joinPasswordHash: null);
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new JoinCampaignHandler(store, new FakeClock(), new FakeSecretHasher());

        var result = await handler.HandleAsync(
            new JoinCampaignCommand
            {
                UserId = OtherUserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsParticipant);
        Assert.False(result.Value.CanManage);
        Assert.Equal(2, result.Value.OccupiedPlayerSlots);
        Assert.Contains(store.Updated!.Memberships, member => member.UserId == OtherUserId && member.IsPlayer);
    }

    [Fact]
    public async Task JoinRejectsWrongPrivatePassword()
    {
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var handler = new JoinCampaignHandler(store, new FakeClock(), new FakeSecretHasher());

        var result = await handler.HandleAsync(
            new JoinCampaignCommand
            {
                UserId = OtherUserId,
                IsAdministrator = false,
                CampaignId = store.Existing.Id,
                JoinPassword = "wrong",
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignJoinPasswordInvalid, result.ErrorCode);
        Assert.Null(store.Updated);
    }

    [Fact]
    public async Task LeaveRemovesNonManagerPlayer()
    {
        var campaign = StoredCampaignFor(UserId);
        campaign = WithMemberships(campaign,
        [
            new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = true },
            new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
        ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new LeaveCampaignHandler(store, new FakeClock());

        var forbidden = await handler.HandleAsync(
            new LeaveCampaignCommand { UserId = UserId, CampaignId = campaign.Id },
            CancellationToken.None);
        Assert.False(forbidden.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, forbidden.ErrorCode);

        var left = await handler.HandleAsync(
            new LeaveCampaignCommand { UserId = OtherUserId, CampaignId = campaign.Id },
            CancellationToken.None);
        Assert.True(left.IsSuccess);
        Assert.DoesNotContain(store.Updated!.Memberships, member => member.UserId == OtherUserId);
    }

    [Fact]
    public async Task SearchOmitsCurrentMembersAndAllowsStaff()
    {
        var campaign = WithMemberships(
            StoredCampaignFor(UserId),
            [
                new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = true },
                new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
            ]);
        var accounts = new FakeAccounts
        {
            SearchHits =
            [
                new MentionableAccount { UserId = UserId, Username = "northplayer", DisplayName = "northplayer" },
                new MentionableAccount { UserId = OtherUserId, Username = "southplayer", DisplayName = "southplayer" },
                new MentionableAccount { UserId = ThirdUserId, Username = "test1", DisplayName = "Test 1" },
            ],
        };
        var handler = new SearchCampaignUsersHandler(new FakeCampaignStore { Existing = campaign }, accounts);

        var forbidden = await handler.HandleAsync(
            new SearchCampaignUsersCommand
            {
                UserId = OtherUserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                Query = "te",
            },
            CancellationToken.None);
        Assert.False(forbidden.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, forbidden.ErrorCode);

        var result = await handler.HandleAsync(
            new SearchCampaignUsersCommand
            {
                UserId = UserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                Query = "te",
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Value!);
        Assert.Equal(ThirdUserId, hit.UserId);
        Assert.Equal("Test 1", hit.DisplayName);
    }

    [Fact]
    public async Task ManagerAddsPlayerToPrivateCampaignWithoutPassword()
    {
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var handler = new AddCampaignMemberHandler(store, new FakeAccounts(), new FakeClock());

        var forbidden = await handler.HandleAsync(
            new AddCampaignMemberCommand
            {
                UserId = OtherUserId,
                IsAdministrator = false,
                CampaignId = store.Existing.Id,
                TargetUserId = ThirdUserId,
                ExpectedRevision = 1,
            },
            CancellationToken.None);
        Assert.False(forbidden.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignNotFound, forbidden.ErrorCode);

        var result = await handler.HandleAsync(
            new AddCampaignMemberCommand
            {
                UserId = UserId,
                IsAdministrator = false,
                CampaignId = store.Existing.Id,
                TargetUserId = OtherUserId,
                ExpectedRevision = 1,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(store.Updated!.Memberships, member => member.UserId == OtherUserId && member.IsPlayer && !member.IsGameMaster);
        Assert.Equal("hash:join-secret", store.Updated.JoinPasswordHash);
    }

    [Fact]
    public async Task AddMemberRejectsCompletedCampaignsAndExistingMembers()
    {
        var completed = WithCopied(StoredCampaignFor(UserId), startsUtc: Now.AddDays(-60), endsUtc: Now.AddHours(-1));
        var completedStore = new FakeCampaignStore { Existing = completed };
        var completedResult = await new AddCampaignMemberHandler(completedStore, new FakeAccounts(), new FakeClock())
            .HandleAsync(
                new AddCampaignMemberCommand
                {
                    UserId = UserId,
                    IsAdministrator = false,
                    CampaignId = completed.Id,
                    TargetUserId = OtherUserId,
                    ExpectedRevision = 1,
                },
                CancellationToken.None);
        Assert.False(completedResult.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignJoinClosed, completedResult.ErrorCode);

        var already = WithMemberships(
            StoredCampaignFor(UserId),
            [
                new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = true },
                new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
            ]);
        var alreadyStore = new FakeCampaignStore { Existing = already };
        var alreadyResult = await new AddCampaignMemberHandler(alreadyStore, new FakeAccounts(), new FakeClock())
            .HandleAsync(
                new AddCampaignMemberCommand
                {
                    UserId = UserId,
                    IsAdministrator = false,
                    CampaignId = already.Id,
                    TargetUserId = OtherUserId,
                    ExpectedRevision = 1,
                },
                CancellationToken.None);
        Assert.False(alreadyResult.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignAlreadyMember, alreadyResult.ErrorCode);
    }

    [Fact]
    public async Task KickRemovesPlayerAndNotifiesThem()
    {
        var campaign = WithMemberships(
            StoredCampaignFor(UserId),
            [
                new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = true },
                new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
            ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var notices = new FakeNoticeStore();
        var handler = new KickCampaignMemberHandler(
            store,
            new FakeClock(),
            new CampaignNotificationPublisher(notices, new FakeAccounts(), new FakeEmailOutbox(), new FakeClock()));

        var result = await handler.HandleAsync(
            new KickCampaignMemberCommand
            {
                UserId = UserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                TargetUserId = OtherUserId,
                ExpectedRevision = 1,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(store.Updated!.Memberships, member => member.UserId == OtherUserId);
        var notice = Assert.Single(notices.Added);
        Assert.Equal(NotificationKind.CampaignKicked, notice.Kind);
        Assert.Equal(OtherUserId, notice.UserId);
        Assert.Equal("/campaigns/all", notice.Path);
        Assert.Contains("Border War", notice.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task KickRejectsRemovingAManager()
    {
        var campaign = WithMemberships(
            StoredCampaignFor(UserId),
            [
                new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = true },
                new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = true, IsPlayer = true },
            ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new KickCampaignMemberHandler(
            store,
            new FakeClock(),
            new CampaignNotificationPublisher(new FakeNoticeStore(), new FakeAccounts(), new FakeEmailOutbox(), new FakeClock()));

        var result = await handler.HandleAsync(
            new KickCampaignMemberCommand
            {
                UserId = UserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                TargetUserId = OtherUserId,
                ExpectedRevision = 1,
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
        Assert.Null(store.Updated);
    }

    [Fact]
    public async Task UpdateRejectsParticipantsWhoAreNotManagers()
    {
        var campaign = StoredCampaignFor(UserId);
        campaign = WithMemberships(campaign,
        [
            new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = false },
            new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
        ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new UpdateCampaignHandler(store, new FakeClock(), new FakeSecretHasher(), new FakeAssetStorage());

        var result = await handler.HandleAsync(
            new UpdateCampaignCommand
            {
                UserId = OtherUserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Name = "Renamed",
                PlayerCount = 8,
                IsPrivate = false,
                IsPubliclyViewable = true,
                CreatorIsParticipant = false,
                Factions =
                [
                    new FactionInput { Name = "North" },
                    new FactionInput { Name = "South" },
                ],
                Schedule = ValidSchedule(),
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
        Assert.Null(store.Updated);
    }

    [Fact]
    public async Task UpdateRejectsSetupChangesAfterLaunch()
    {
        var campaign = WithCopied(
            StoredCampaignFor(UserId),
            startsUtc: Now.AddHours(-1),
            endsUtc: Now.AddDays(40));
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new UpdateCampaignHandler(store, new FakeClock(), new FakeSecretHasher(), new FakeAssetStorage());

        var result = await handler.HandleAsync(
            new UpdateCampaignCommand
            {
                UserId = UserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Name = "Renamed",
                PlayerCount = 8,
                IsPrivate = false,
                IsPubliclyViewable = true,
                CreatorIsParticipant = true,
                Factions =
                [
                    new FactionInput { Name = "North" },
                    new FactionInput { Name = "South" },
                ],
                Schedule = ValidSchedule(),
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignLocked, result.ErrorCode);
        Assert.Null(store.Updated);
    }

    [Fact]
    public async Task GetPlaySeedsForcesAndHidesOtherPlayersDrafts()
    {
        var northSpawn = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var southSpawn = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var midland = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var plainsId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var graph = new StoredMapGraph
        {
            Territories =
            [
                SquareTerritory(northSpawn, 1, 0.05, 0.05, 0.2, NorthFactionId, plainsId),
                SquareTerritory(midland, 2, 0.30, 0.05, 0.2, null, plainsId),
                SquareTerritory(southSpawn, 3, 0.55, 0.05, 0.2, SouthFactionId, plainsId),
            ],
            Adjacencies =
            [
                new AdjacencyDetail
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01"),
                    TerritoryAId = northSpawn,
                    TerritoryBId = midland,
                    Origin = "Manual",
                    MarkerX = 0.27,
                    MarkerY = 0.15,
                },
                new AdjacencyDetail
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02"),
                    TerritoryAId = midland,
                    TerritoryBId = southSpawn,
                    Origin = "Manual",
                    MarkerX = 0.52,
                    MarkerY = 0.15,
                },
            ],
        };
        var campaign = WithCopied(
            StoredCampaignFor(UserId),
            memberships:
            [
                new StoredCampaignMembership
                {
                    UserId = UserId,
                    IsGameMaster = true,
                    IsPlayer = true,
                    FactionId = NorthFactionId,
                },
                new StoredCampaignMembership
                {
                    UserId = OtherUserId,
                    IsGameMaster = false,
                    IsPlayer = true,
                    FactionId = SouthFactionId,
                },
            ],
            startsUtc: Now,
            endsUtc: Now.AddDays(40),
            mapGraph: graph);
        campaign = new StoredCampaign
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            IsPrivate = campaign.IsPrivate,
            IsPubliclyViewable = campaign.IsPubliclyViewable,
            JoinPasswordHash = campaign.JoinPasswordHash,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            City = campaign.City,
            Region = campaign.Region,
            Country = campaign.Country,
            MapStorageKey = campaign.MapStorageKey,
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            CreatedByUserId = campaign.CreatedByUserId,
            Memberships = campaign.Memberships,
            Factions = campaign.Factions,
            AllyGroups = campaign.AllyGroups,
            Links = campaign.Links,
            TimeZoneId = campaign.TimeZoneId,
            StartsUtc = campaign.StartsUtc,
            EndsUtc = campaign.EndsUtc,
            RoundCount = campaign.RoundCount,
            RoundLengthAmount = campaign.RoundLengthAmount,
            RoundLengthUnit = campaign.RoundLengthUnit,
            Phases = campaign.Phases,
            MapGraph = campaign.MapGraph,
            TerrainTypes =
            [
                new StoredTerrainType
                {
                    Id = plainsId,
                    Name = "Plains",
                    Color = "#7CB342",
                    Missions = [],
                },
            ],
            StructureTypes = campaign.StructureTypes,
            PlayState = campaign.PlayState,
        };
        var store = new FakeCampaignStore { Existing = campaign };
        var accounts = new FakeAccounts();
        var get = new GetCampaignPlayHandler(store, new FakeClock(), accounts);
        var seeded = await get.HandleAsync(campaign.Id, UserId, false, CancellationToken.None);
        Assert.True(seeded.IsSuccess);
        Assert.NotNull(seeded.Value);
        Assert.Equal(2, seeded.Value.Forces.Count);
        Assert.Contains(midland, seeded.Value.Forces.Single(force => force.IsMine).MoveTargets);

        var northForce = seeded.Value.Forces.Single(force => force.IsMine);
        var draft = new SaveOrderDraftHandler(store, new FakeClock(), accounts);
        var saved = await draft.HandleAsync(
            new SaveOrderDraftCommand
            {
                UserId = UserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                ExpectedRevision = seeded.Value.Revision,
                ForceId = northForce.Id,
                Kind = "Hold",
            },
            CancellationToken.None);
        Assert.True(saved.IsSuccess);
        Assert.Single(saved.Value!.MyDrafts);

        var otherView = await get.HandleAsync(campaign.Id, OtherUserId, false, CancellationToken.None);
        Assert.True(otherView.IsSuccess);
        Assert.Empty(otherView.Value!.MyDrafts);
        Assert.Empty(otherView.Value.Orders);
        Assert.DoesNotContain(otherView.Value.Commitments, item => item.IsCommitted);
    }

    [Fact]
    public async Task GetPlayHidesUnrevealedItemObjectivesUntilDebugReveal()
    {
        var northSpawn = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var southSpawn = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var midland = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var plainsId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var itemTypeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var graph = new StoredMapGraph
        {
            Territories =
            [
                SquareTerritory(northSpawn, 1, 0.05, 0.05, 0.2, NorthFactionId, plainsId),
                SquareTerritory(midland, 2, 0.30, 0.05, 0.2, null, plainsId),
                SquareTerritory(southSpawn, 3, 0.55, 0.05, 0.2, SouthFactionId, plainsId),
            ],
            Adjacencies =
            [
                new AdjacencyDetail
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01"),
                    TerritoryAId = northSpawn,
                    TerritoryBId = midland,
                    Origin = "Manual",
                    MarkerX = 0.27,
                    MarkerY = 0.15,
                },
                new AdjacencyDetail
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02"),
                    TerritoryAId = midland,
                    TerritoryBId = southSpawn,
                    Origin = "Manual",
                    MarkerX = 0.52,
                    MarkerY = 0.15,
                },
            ],
        };
        var campaign = WithCopied(
            StoredCampaignFor(UserId),
            memberships:
            [
                new StoredCampaignMembership
                {
                    UserId = UserId,
                    IsGameMaster = true,
                    IsPlayer = true,
                    FactionId = NorthFactionId,
                },
                new StoredCampaignMembership
                {
                    UserId = OtherUserId,
                    IsGameMaster = false,
                    IsPlayer = true,
                    FactionId = SouthFactionId,
                },
            ],
            startsUtc: Now,
            endsUtc: Now.AddDays(40),
            mapGraph: graph,
            terrainTypes:
            [
                new StoredTerrainType
                {
                    Id = plainsId,
                    Name = "Plains",
                    Color = "#7CB342",
                    Missions = [],
                },
            ],
            itemObjectiveTypes:
            [
                new StoredItemObjectiveType
                {
                    Id = itemTypeId,
                    Name = "Crown",
                    IsHiddenUntilFound = true,
                    Placement = "Random",
                    AllowOnSpawn = false,
                },
            ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var accounts = new FakeAccounts();
        var get = new GetCampaignPlayHandler(store, new FakeClock(), accounts);

        var playerView = await get.HandleAsync(campaign.Id, OtherUserId, false, CancellationToken.None);
        Assert.True(playerView.IsSuccess);
        Assert.Empty(playerView.Value!.ItemObjectives);

        var managerView = await get.HandleAsync(campaign.Id, UserId, false, CancellationToken.None);
        Assert.True(managerView.IsSuccess);
        Assert.Empty(managerView.Value!.ItemObjectives);

        store.Existing = WithCopied(
            store.Existing!,
            playState: store.Existing!.PlayState!.With(debugActorUserId: UserId, debugStartedUtc: Now));
        var debugView = await get.HandleAsync(campaign.Id, UserId, false, CancellationToken.None);
        Assert.True(debugView.IsSuccess);
        var hidden = Assert.Single(debugView.Value!.ItemObjectives);
        Assert.Equal("Crown", hidden.Name);
        Assert.False(hidden.IsRevealed);
        Assert.Equal(midland, hidden.TerritoryId);

        var reveal = new RevealHiddenItemObjectivesHandler(store, new FakeClock(), accounts);
        var revealed = await reveal.HandleAsync(
            new PlayCommand
            {
                UserId = UserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                ExpectedRevision = debugView.Value.Revision,
            },
            CancellationToken.None);
        Assert.True(revealed.IsSuccess);
        var visible = Assert.Single(revealed.Value!.ItemObjectives);
        Assert.True(visible.IsRevealed);

        var afterReveal = await get.HandleAsync(campaign.Id, OtherUserId, false, CancellationToken.None);
        Assert.True(afterReveal.IsSuccess);
        var playerItem = Assert.Single(afterReveal.Value!.ItemObjectives);
        Assert.Equal("Crown", playerItem.Name);
        Assert.True(playerItem.IsRevealed);
        Assert.Equal(midland, playerItem.TerritoryId);
    }

    [Fact]
    public async Task GetPlayOmitsHiddenItemPointsFromUnauthorizedStandings()
    {
        var northSpawn = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var southSpawn = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var midland = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var plainsId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var itemTypeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var graph = new StoredMapGraph
        {
            Territories =
            [
                SquareTerritory(northSpawn, 1, 0.05, 0.05, 0.2, NorthFactionId, plainsId, NorthFactionId),
                SquareTerritory(midland, 2, 0.30, 0.05, 0.2, null, plainsId),
                SquareTerritory(southSpawn, 3, 0.55, 0.05, 0.2, SouthFactionId, plainsId, SouthFactionId),
            ],
            Adjacencies =
            [
                new AdjacencyDetail
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01"),
                    TerritoryAId = northSpawn,
                    TerritoryBId = midland,
                    Origin = "Manual",
                    MarkerX = 0.27,
                    MarkerY = 0.15,
                },
                new AdjacencyDetail
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02"),
                    TerritoryAId = midland,
                    TerritoryBId = southSpawn,
                    Origin = "Manual",
                    MarkerX = 0.52,
                    MarkerY = 0.15,
                },
            ],
        };
        var campaign = WithCopied(
            StoredCampaignFor(UserId),
            memberships:
            [
                new StoredCampaignMembership
                {
                    UserId = UserId,
                    IsGameMaster = true,
                    IsPlayer = true,
                    FactionId = NorthFactionId,
                },
                new StoredCampaignMembership
                {
                    UserId = OtherUserId,
                    IsGameMaster = false,
                    IsPlayer = true,
                    FactionId = SouthFactionId,
                },
            ],
            startsUtc: Now,
            endsUtc: Now.AddDays(40),
            mapGraph: graph,
            terrainTypes:
            [
                new StoredTerrainType
                {
                    Id = plainsId,
                    Name = "Plains",
                    Color = "#7CB342",
                    Missions = [],
                    CampaignPoints = 0,
                },
            ],
            itemObjectiveTypes:
            [
                new StoredItemObjectiveType
                {
                    Id = itemTypeId,
                    Name = "Crown",
                    IsHiddenUntilFound = true,
                    Placement = "Random",
                    AllowOnSpawn = true,
                    CampaignPoints = 7,
                },
            ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var accounts = new FakeAccounts();
        var get = new GetCampaignPlayHandler(store, new FakeClock(), accounts);
        var seeded = await get.HandleAsync(campaign.Id, UserId, false, CancellationToken.None);
        Assert.True(seeded.IsSuccess);
        var northForceId = store.Existing!.PlayState!.Forces.Single(force => force.ControllerUserId == UserId).Id;
        store.Existing = WithCopied(
            store.Existing,
            playState: store.Existing.PlayState.With(
                itemObjectives:
                [
                    new Campaign.Domain.Play.CampaignItemObjective(
                        Guid.Parse("66666666-6666-6666-6666-666666666666"),
                        itemTypeId,
                        "Crown",
                        null,
                        northForceId,
                        false,
                        northSpawn,
                        true),
                ]));

        var holderView = await get.HandleAsync(campaign.Id, UserId, false, CancellationToken.None);
        Assert.True(holderView.IsSuccess);
        var holderRow = Assert.Single(holderView.Value!.Standings, row => row.UserId == UserId);
        Assert.Equal(7, holderRow.OtherPoints);
        Assert.Equal(holderRow.TerritoryAndStructurePoints + holderRow.BattlesWonPoints + holderRow.PublicObjectivePoints + holderRow.PrivateObjectivePoints + holderRow.OtherPoints, holderRow.Total);
        Assert.Equal("Crown", Assert.Single(holderRow.HeldItems).Name);

        var otherView = await get.HandleAsync(campaign.Id, OtherUserId, false, CancellationToken.None);
        Assert.True(otherView.IsSuccess);
        Assert.Empty(otherView.Value!.ItemObjectives);
        var hiddenFromRival = Assert.Single(otherView.Value.Standings, row => row.UserId == UserId);
        Assert.Equal(0, hiddenFromRival.OtherPoints);
        Assert.Empty(hiddenFromRival.HeldItems);
        Assert.Equal(
            hiddenFromRival.TerritoryAndStructurePoints
            + hiddenFromRival.BattlesWonPoints
            + hiddenFromRival.PublicObjectivePoints
            + hiddenFromRival.PrivateObjectivePoints
            + hiddenFromRival.OtherPoints,
            hiddenFromRival.Total);
    }

    [Fact]
    public async Task DeleteRemovesCampaignForManagersOnly()
    {
        var campaign = StoredCampaignFor(UserId);
        campaign = WithMemberships(campaign,
        [
            new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = true },
            new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true },
        ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var maps = new FakeMapStorage();
        var handler = new DeleteCampaignHandler(store, maps);

        var forbidden = await handler.HandleAsync(campaign.Id, OtherUserId, CancellationToken.None);
        Assert.False(forbidden.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, forbidden.ErrorCode);
        Assert.False(store.Deleted);

        var deleted = await handler.HandleAsync(campaign.Id, UserId, CancellationToken.None);
        Assert.True(deleted.IsSuccess);
        Assert.True(store.Deleted);
        Assert.Contains("maps/old.png", maps.DeletedKeys);
        Assert.Contains("flags/north.png", maps.DeletedKeys);
        Assert.Contains("structures/town.png", maps.DeletedKeys);
        Assert.DoesNotContain("Town", maps.DeletedKeys);
        Assert.DoesNotContain("Castle", maps.DeletedKeys);
    }

    [Fact]
    public async Task DuplicateCopiesSetupSharesAssetsAndStartsInOneWeek()
    {
        var plainsId = Guid.Parse("eeeeee01-eeee-eeee-eeee-eeeeeeeeeeee");
        var source = StoredCampaignFor(UserId);
        source = WithCopied(
            source,
            mapGraph: new StoredMapGraph
            {
                Territories =
                [
                    SquareTerritory(
                        Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        1,
                        0.1,
                        0.1,
                        0.2,
                        NorthFactionId,
                        plainsId),
                ],
                Adjacencies = [],
            });
        var store = new FakeCampaignStore { Existing = source };
        var handler = new DuplicateCampaignHandler(store, new FakeClock());

        var result = await handler.HandleAsync(
            new DuplicateCampaignCommand { UserId = UserId, CampaignId = source.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(source.Id, result.Value.Id);
        Assert.Equal("Border War", result.Value.Name);
        Assert.True(result.Value.CanManage);
        Assert.True(result.Value.IsParticipant);
        Assert.Equal(Now.AddDays(7), result.Value.StartsUtc);
        Assert.NotNull(store.Added);
        Assert.Equal(source.MapStorageKey, store.Added.MapStorageKey);
        Assert.Equal("flags/north.png", store.Added.Factions[0].FlagImageStorageKey);
        Assert.NotEqual(source.Factions[0].Id, store.Added.Factions[0].Id);
        Assert.Equal(source.Factions[0].Name, store.Added.Factions[0].Name);
        Assert.Equal("structures/town.png", store.Added.StructureTypes[0].ImageStorageKey);
        Assert.Null(store.Added.PlayState);
        Assert.Equal(source.JoinPasswordHash, store.Added.JoinPasswordHash);
        Assert.Single(store.Added.Memberships);
        Assert.Equal(UserId, store.Added.CreatedByUserId);
        Assert.NotNull(store.Added.MapGraph);
        Assert.Single(store.Added.MapGraph.Territories);
    }

    [Fact]
    public async Task DuplicateRejectsNonMembers()
    {
        var store = new FakeCampaignStore { Existing = StoredCampaignFor(UserId) };
        var handler = new DuplicateCampaignHandler(store, new FakeClock());

        var result = await handler.HandleAsync(
            new DuplicateCampaignCommand { UserId = OtherUserId, CampaignId = store.Existing!.Id },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignNotFound, result.ErrorCode);
        Assert.Null(store.Added);
    }

    [Fact]
    public async Task DeleteKeepsSharedAssetsStillUsedByADuplicate()
    {
        var source = StoredCampaignFor(UserId);
        var duplicate = new StoredCampaign
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Name = source.Name,
            Description = source.Description,
            PlayerSlotCount = source.PlayerSlotCount,
            IsPrivate = source.IsPrivate,
            IsPubliclyViewable = source.IsPubliclyViewable,
            JoinPasswordHash = source.JoinPasswordHash,
            CreatorIsParticipant = source.CreatorIsParticipant,
            City = source.City,
            Region = source.Region,
            Country = source.Country,
            MapStorageKey = source.MapStorageKey,
            Revision = 1,
            CreatedUtc = source.CreatedUtc,
            UpdatedUtc = source.UpdatedUtc,
            CreatedByUserId = OtherUserId,
            Memberships = [new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = true, IsPlayer = true }],
            Factions = source.Factions,
            AllyGroups = source.AllyGroups,
            Links = source.Links,
            TimeZoneId = source.TimeZoneId,
            StartsUtc = source.StartsUtc,
            EndsUtc = source.EndsUtc,
            RoundCount = source.RoundCount,
            RoundLengthAmount = source.RoundLengthAmount,
            RoundLengthUnit = source.RoundLengthUnit,
            Phases = source.Phases,
            MapGraph = source.MapGraph,
            TerrainTypes = source.TerrainTypes,
            StructureTypes = source.StructureTypes,
        };
        var store = new FakeCampaignStore { Existing = source };
        store.ForUser.Add(duplicate);
        var maps = new FakeMapStorage();
        var handler = new DeleteCampaignHandler(store, maps);

        var deleted = await handler.HandleAsync(source.Id, UserId, CancellationToken.None);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(maps.DeletedKeys);
    }

    [Fact]
    public async Task UpdateDeletesCatalogFilesThatAreNoLongerReferenced()
    {
        var campaign = StoredCampaignFor(UserId);
        var store = new FakeCampaignStore { Existing = campaign };
        var assets = new FakeAssetStorage();
        var handler = new UpdateCampaignHandler(store, new FakeClock(), new FakeSecretHasher(), assets);

        var result = await handler.HandleAsync(
            new UpdateCampaignCommand
            {
                UserId = UserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Name = campaign.Name,
                Description = campaign.Description,
                PlayerCount = campaign.PlayerSlotCount,
                IsPrivate = false,
                IsPubliclyViewable = true,
                CreatorIsParticipant = true,
                Factions =
                [
                    new FactionInput
                    {
                        Id = NorthFactionId,
                        Name = "North",
                        Color = "#2563EB",
                        ClearFlagImage = true,
                    },
                    new FactionInput
                    {
                        Id = SouthFactionId,
                        Name = "South",
                        Color = "#DC2626",
                    },
                ],
                Schedule = ValidSchedule(),
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("flags/north.png", assets.DeletedKeys);
        Assert.Contains("structures/town.png", assets.DeletedKeys);
        Assert.DoesNotContain("maps/old.png", assets.DeletedKeys);
        Assert.DoesNotContain("Town", assets.DeletedKeys);
        Assert.DoesNotContain("Castle", assets.DeletedKeys);
    }

    [Fact]
    public async Task UpdateDoesNotDeleteBuiltInStructureLogos()
    {
        var campaign = WithStructures(
            StoredCampaignFor(UserId),
            [
                new StoredStructureType
                {
                    Id = TownStructureId,
                    Name = "Town",
                    BuiltinSymbol = "Town",
                    ImageStorageKey = "Town",
                    IsBuildable = false,
                    IsPillageable = true,
                    IsDestructible = true,
                    Missions = [],
                },
                new StoredStructureType
                {
                    Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    Name = "Keep",
                    BuiltinSymbol = "Castle",
                    ImageStorageKey = "structures/keep.png",
                    IsBuildable = false,
                    IsPillageable = true,
                    IsDestructible = false,
                    Missions = [],
                },
            ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var assets = new FakeAssetStorage();
        var handler = new UpdateCampaignHandler(store, new FakeClock(), new FakeSecretHasher(), assets);

        var result = await handler.HandleAsync(
            new UpdateCampaignCommand
            {
                UserId = UserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Name = campaign.Name,
                Description = campaign.Description,
                PlayerCount = campaign.PlayerSlotCount,
                IsPrivate = false,
                IsPubliclyViewable = true,
                CreatorIsParticipant = true,
                Factions =
                [
                    new FactionInput { Id = NorthFactionId, Name = "North", Color = "#2563EB" },
                    new FactionInput { Id = SouthFactionId, Name = "South", Color = "#DC2626" },
                ],
                Schedule = ValidSchedule(),
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("structures/keep.png", assets.DeletedKeys);
        Assert.DoesNotContain("Town", assets.DeletedKeys);
        Assert.DoesNotContain("Castle", assets.DeletedKeys);
    }

    [Fact]
    public async Task UploadMapDeletesThePreviousMapFile()
    {
        var campaign = StoredCampaignFor(UserId);
        var store = new FakeCampaignStore { Existing = campaign };
        var maps = new FakeMapStorage();
        var handler = new UploadCampaignMapHandler(store, new FakeMapProcessor(), maps, new FakeClock());

        var result = await handler.HandleAsync(
            new UploadCampaignMapCommand
            {
                UserId = UserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                Content = Stream.Null,
                ContentType = "image/png",
                Length = 12,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("maps/new.png", store.Updated!.MapStorageKey);
        Assert.Contains("maps/old.png", maps.DeletedKeys);
        Assert.DoesNotContain("flags/north.png", maps.DeletedKeys);
    }

    [Fact]
    public void DetailAllowsFactionChangeWhileScheduled()
    {
        var campaign = WithMemberships(
            StoredCampaignFor(UserId),
            [new StoredCampaignMembership
            {
                UserId = UserId,
                IsGameMaster = true,
                IsPlayer = true,
                FactionId = NorthFactionId,
            }]);

        var detail = CampaignMapper.ToDetail(campaign, UserId, Now);

        Assert.True(detail.CanChooseFaction);
        Assert.Equal(NorthFactionId, detail.FactionId);
    }

    [Fact]
    public void DetailLocksChosenFactionAfterLaunch()
    {
        var campaign = WithCopied(
            WithMemberships(
                StoredCampaignFor(UserId),
                [new StoredCampaignMembership
                {
                    UserId = UserId,
                    IsGameMaster = true,
                    IsPlayer = true,
                    FactionId = NorthFactionId,
                }]),
            startsUtc: Now.AddHours(-1),
            endsUtc: Now.AddDays(40));

        var detail = CampaignMapper.ToDetail(campaign, UserId, Now);

        Assert.False(detail.CanChooseFaction);
        Assert.Equal(NorthFactionId, detail.FactionId);
    }

    [Fact]
    public async Task PlayerCanChangeFactionBeforeTheCampaignStarts()
    {
        var campaign = WithMemberships(
            StoredCampaignFor(UserId),
            [new StoredCampaignMembership
            {
                UserId = UserId,
                IsGameMaster = true,
                IsPlayer = true,
                FactionId = NorthFactionId,
            }]);
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new ChooseFactionHandler(store, new FakeClock(), new FakeAccounts());

        var result = await handler.HandleAsync(
            new ChooseFactionCommand
            {
                UserId = UserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                FactionId = SouthFactionId,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(SouthFactionId, store.Updated!.Memberships.Single().FactionId);
        Assert.Equal(SouthFactionId, result.Value.FactionId);
        Assert.True(result.Value.CanChooseFaction);
    }

    [Fact]
    public async Task PlayerCannotChangeFactionAfterTheCampaignStarts()
    {
        var campaign = WithCopied(
            WithMemberships(
                StoredCampaignFor(UserId),
                [new StoredCampaignMembership
                {
                    UserId = UserId,
                    IsGameMaster = true,
                    IsPlayer = true,
                    FactionId = NorthFactionId,
                }]),
            startsUtc: Now.AddHours(-1),
            endsUtc: Now.AddDays(40));
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new ChooseFactionHandler(store, new FakeClock(), new FakeAccounts());

        var result = await handler.HandleAsync(
            new ChooseFactionCommand
            {
                UserId = UserId,
                CampaignId = campaign.Id,
                ExpectedRevision = 1,
                FactionId = SouthFactionId,
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("faction.already_chosen", result.ErrorCode);
        Assert.Null(store.Updated);
    }

    [Fact]
    public async Task StaffCanAssignAnotherPlayersFaction()
    {
        var campaign = WithMemberships(
            StoredCampaignFor(UserId),
            [
                new StoredCampaignMembership { UserId = UserId, IsGameMaster = true, IsPlayer = true, FactionId = NorthFactionId },
                new StoredCampaignMembership { UserId = OtherUserId, IsGameMaster = false, IsPlayer = true, FactionId = NorthFactionId },
            ]);
        var store = new FakeCampaignStore { Existing = campaign };
        var handler = new AssignPlayerFactionHandler(store, new FakeClock(), new FakeAccounts());

        var forbidden = await handler.HandleAsync(
            new AssignPlayerFactionCommand
            {
                UserId = OtherUserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                TargetUserId = OtherUserId,
                ExpectedRevision = 1,
                FactionId = SouthFactionId,
            },
            CancellationToken.None);
        Assert.False(forbidden.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, forbidden.ErrorCode);

        var result = await handler.HandleAsync(
            new AssignPlayerFactionCommand
            {
                UserId = UserId,
                IsAdministrator = false,
                CampaignId = campaign.Id,
                TargetUserId = OtherUserId,
                ExpectedRevision = 1,
                FactionId = SouthFactionId,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SouthFactionId, store.Updated!.Memberships.Single(member => member.UserId == OtherUserId).FactionId);
        Assert.Equal(NorthFactionId, store.Updated.Memberships.Single(member => member.UserId == UserId).FactionId);
    }

    [Fact]
    public async Task ListReturnsOnlyMappedViewerFields()
    {
        var store = new FakeCampaignStore();
        store.ForUser.Add(StoredCampaignFor(UserId));
        var handler = new ListCampaignsHandler(store, new FakeClock());

        var result = await handler.HandleAsync(UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!);
        Assert.Equal("Border War", item.Name);
        Assert.True(item.CanManage);
        Assert.True(item.IsParticipant);
    }

    [Fact]
    public void DetailMappingOmitsJoinPasswordHash()
    {
        var campaign = StoredCampaignFor(UserId);
        var json = System.Text.Json.JsonSerializer.Serialize(CampaignMapper.ToDetail(campaign, UserId, Now));
        Assert.DoesNotContain("JoinPassword", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash:join-secret", json, StringComparison.Ordinal);
    }

    private static CreateCampaignCommand ValidCreateCommand(bool isPrivate = false, string? joinPassword = null)
    {
        return new CreateCampaignCommand
        {
            UserId = UserId,
            Name = "Border War",
            Description = "A contested frontier.",
            PlayerCount = 8,
            IsPrivate = isPrivate,
            IsPubliclyViewable = true,
            JoinPassword = joinPassword,
            CreatorIsParticipant = true,
            Factions =
            [
                new FactionInput { Name = "North", Subfactions = ["Riders"] },
                new FactionInput { Name = "South" },
            ],
            AllyGroups = null,
            Links = [new CampaignLinkInput { Label = "Notes", Url = "https://example.test/notes" }],
            Schedule = ValidSchedule(),
        };
    }

    private static CampaignScheduleInput ValidSchedule()
    {
        return CampaignSetupRulesTests.WeekSchedule();
    }

    private static StoredCampaign StoredCampaignFor(Guid userId)
    {
        return new StoredCampaign
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Border War",
            Description = "A contested frontier.",
            PlayerSlotCount = 8,
            IsPrivate = true,
            IsPubliclyViewable = false,
            JoinPasswordHash = "hash:join-secret",
            CreatorIsParticipant = true,
            MapStorageKey = "maps/old.png",
            Revision = 1,
            CreatedUtc = Now,
            UpdatedUtc = Now,
            CreatedByUserId = userId,
            Memberships =
            [
                new StoredCampaignMembership { UserId = userId, IsGameMaster = true, IsPlayer = true },
            ],
            Factions =
            [
                new StoredFaction
                {
                    Id = NorthFactionId,
                    Name = "North",
                    Color = "#2563EB",
                    Subfactions = ["Riders"],
                    RequiresSubfaction = false,
                    FlagImageStorageKey = "flags/north.png",
                },
                new StoredFaction
                {
                    Id = SouthFactionId,
                    Name = "South",
                    Color = "#DC2626",
                    Subfactions = [],
                    RequiresSubfaction = false,
                },
            ],
            AllyGroups = [],
            Links =
            [
                new StoredCampaignLink { Id = Guid.NewGuid(), Label = "Notes", Url = "https://example.test/notes" },
            ],
            TimeZoneId = "UTC",
            StartsUtc = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero),
            EndsUtc = new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases =
            [
                new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new StoredRoundPhase { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
            TerrainTypes = [],
            StructureTypes =
            [
                new StoredStructureType
                {
                    Id = TownStructureId,
                    Name = "Town",
                    BuiltinSymbol = "Town",
                    ImageStorageKey = "structures/town.png",
                    IsBuildable = false,
                    IsPillageable = true,
                    IsDestructible = true,
                    Missions = [],
                },
            ],
        };
    }

    private static StoredCampaign WithPublicView(StoredCampaign campaign, bool isPubliclyViewable)
    {
        return WithCopied(campaign, isPubliclyViewable: isPubliclyViewable);
    }

    private static StoredCampaign WithPrivacy(StoredCampaign campaign, bool isPrivate, string? joinPasswordHash)
    {
        return new StoredCampaign
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            IsPrivate = isPrivate,
            IsPubliclyViewable = campaign.IsPubliclyViewable,
            JoinPasswordHash = joinPasswordHash,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            City = campaign.City,
            Region = campaign.Region,
            Country = campaign.Country,
            MapStorageKey = campaign.MapStorageKey,
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            CreatedByUserId = campaign.CreatedByUserId,
            Memberships = campaign.Memberships,
            Factions = campaign.Factions,
            AllyGroups = campaign.AllyGroups,
            Links = campaign.Links,
            TimeZoneId = campaign.TimeZoneId,
            StartsUtc = campaign.StartsUtc,
            EndsUtc = campaign.EndsUtc,
            RoundCount = campaign.RoundCount,
            RoundLengthAmount = campaign.RoundLengthAmount,
            RoundLengthUnit = campaign.RoundLengthUnit,
            Phases = campaign.Phases,
            MapGraph = campaign.MapGraph,
            TerrainTypes = campaign.TerrainTypes,
            StructureTypes = campaign.StructureTypes,
            ItemObjectiveTypes = campaign.ItemObjectiveTypes,
            PublicObjectiveTypes = campaign.PublicObjectiveTypes,
            BattleScoring = campaign.BattleScoring,
            RankingObjectivePoints = campaign.RankingObjectivePoints,
            PlayState = campaign.PlayState,
        };
    }

    private static StoredCampaign WithCopied(
        StoredCampaign campaign,
        IReadOnlyList<StoredCampaignMembership>? memberships = null,
        IReadOnlyList<StoredStructureType>? structures = null,
        bool? isPubliclyViewable = null,
        bool? isPrivate = null,
        string? joinPasswordHash = null,
        DateTimeOffset? startsUtc = null,
        DateTimeOffset? endsUtc = null,
        StoredMapGraph? mapGraph = null,
        IReadOnlyList<StoredTerrainType>? terrainTypes = null,
        IReadOnlyList<StoredItemObjectiveType>? itemObjectiveTypes = null,
        Campaign.Domain.Play.CampaignPlayState? playState = null)
    {
        return new StoredCampaign
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            IsPrivate = isPrivate ?? campaign.IsPrivate,
            IsPubliclyViewable = isPubliclyViewable ?? campaign.IsPubliclyViewable,
            JoinPasswordHash = joinPasswordHash ?? campaign.JoinPasswordHash,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            City = campaign.City,
            Region = campaign.Region,
            Country = campaign.Country,
            MapStorageKey = campaign.MapStorageKey,
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            CreatedByUserId = campaign.CreatedByUserId,
            Memberships = memberships ?? campaign.Memberships,
            Factions = campaign.Factions,
            AllyGroups = campaign.AllyGroups,
            Links = campaign.Links,
            TimeZoneId = campaign.TimeZoneId,
            StartsUtc = startsUtc ?? campaign.StartsUtc,
            EndsUtc = endsUtc ?? campaign.EndsUtc,
            RoundCount = campaign.RoundCount,
            RoundLengthAmount = campaign.RoundLengthAmount,
            RoundLengthUnit = campaign.RoundLengthUnit,
            Phases = campaign.Phases,
            MapGraph = mapGraph ?? campaign.MapGraph,
            TerrainTypes = terrainTypes ?? campaign.TerrainTypes,
            StructureTypes = structures ?? campaign.StructureTypes,
            ItemObjectiveTypes = itemObjectiveTypes ?? campaign.ItemObjectiveTypes,
            PublicObjectiveTypes = campaign.PublicObjectiveTypes,
            BattleScoring = campaign.BattleScoring,
            RankingObjectivePoints = campaign.RankingObjectivePoints,
            PlayState = playState ?? campaign.PlayState,
        };
    }

    private static StoredCampaign WithMemberships(StoredCampaign campaign, IReadOnlyList<StoredCampaignMembership> memberships)
    {
        return WithCopied(campaign, memberships: memberships);
    }

    private static StoredCampaign WithStructures(StoredCampaign campaign, IReadOnlyList<StoredStructureType> structures)
    {
        return WithCopied(campaign, structures: structures);
    }

    private static TerritoryDetail SquareTerritory(
        Guid id,
        int number,
        double x,
        double y,
        double size,
        Guid? spawnFactionId,
        Guid terrainTypeId,
        Guid? owner = null)
    {
        return new TerritoryDetail
        {
            Id = id,
            DisplayNumber = number,
            Polygon =
            [
                new MapPointDetail { X = x, Y = y },
                new MapPointDetail { X = x + size, Y = y },
                new MapPointDetail { X = x + size, Y = y + size },
                new MapPointDetail { X = x, Y = y + size },
            ],
            TerrainTypeId = terrainTypeId,
            OwnerFactionId = owner ?? spawnFactionId,
            SpawnFactionId = spawnFactionId,
        };
    }

    private static object? GetHashProperty(CampaignDetail detail)
    {
        return detail.GetType().GetProperty("JoinPasswordHash")?.GetValue(detail);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeNoticeStore : IUserNotificationStore
    {
        public List<NewUserNotification> Added { get; } = [];

        public Task<bool> TryAddAsync(NewUserNotification notification, DateTimeOffset utcNow, CancellationToken cancellationToken)
        {
            Added.Add(notification);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<UserNotification>> ListUnreadAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserNotification>>([]);

        public Task<bool> MarkReadAsync(Guid notificationId, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class FakeEmailOutbox : IEmailOutbox
    {
        public Task QueueEmailConfirmationAsync(string email, Guid userId, string token, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task QueuePasswordResetAsync(string email, Guid userId, string token, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task QueueUserNoticeAsync(
            string email,
            Guid userId,
            string subject,
            string body,
            string path,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeAccounts : IUserAccountStore
    {
        public HashSet<Guid> AdministratorIds { get; } = [];

        public List<MentionableAccount> SearchHits { get; init; } = [];

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> UsernameExistsAsync(string username, Guid? userIdToIgnore, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<CreateLocalAccountOutcome> CreateLocalAccountAsync(CreateLocalAccountRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CreateLocalAccountOutcome> CreateExternalAccountAsync(CreateExternalAccountRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<UserAccount?>(new UserAccount
            {
                Id = userId,
                Email = $"{userId:N}@example.test",
                Username = userId == UserId
                    ? "northplayer"
                    : userId == OtherUserId
                        ? "southplayer"
                        : "eastplayer",
                FirstName = "Test",
                LastName = "User",
                City = "Halifax",
                Country = "Canada",
                DisplayNameMode = DisplayNameMode.Username,
                CreatedUtc = Now,
                UpdatedUtc = Now,
                ProfileRevision = 1,
                EmailConfirmed = true,
            });
        }

        public Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult<UserAccount?>(null);

        public Task<UpdateProfileOutcome> UpdateProfileAsync(UpdateStoredProfileRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ChangePasswordOutcome> ChangePasswordAsync(
            Guid userId,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<string?> ReplaceAvatarKeyAsync(Guid userId, string? avatarStorageKey, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        public Task<IReadOnlySet<Guid>> FindAdministratorIdsAsync(
            IReadOnlyList<Guid> userIds,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlySet<Guid>>(userIds.Where(AdministratorIds.Contains).ToHashSet());
        }

        public Task<IReadOnlyList<MentionableAccount>> SearchAsync(
            string query,
            int take,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<MentionableAccount>>(SearchHits);
        }
    }

    private sealed class FakeSecretHasher : ISecretHasher
    {
        public string Hash(string secret)
        {
            return $"hash:{secret}";
        }

        public bool Verify(string hash, string secret)
        {
            return hash == $"hash:{secret}";
        }
    }

    private sealed class FakeCampaignStore : ICampaignStore
    {
        public StoredCampaign? Added { get; private set; }

        public StoredCampaign? Existing { get; set; }

        public StoredCampaign? Updated { get; private set; }

        public bool Deleted { get; private set; }

        public List<StoredCampaign> ForUser { get; } = [];

        public Task<StoredCampaign> AddAsync(StoredCampaign campaign, CancellationToken cancellationToken)
        {
            Added = campaign;
            return Task.FromResult(campaign);
        }

        public Task<StoredCampaign?> FindByIdAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Existing is not null && Existing.Id == campaignId ? Existing : null);
        }

        public Task<IReadOnlyList<StoredCampaign>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<StoredCampaign>>(ForUser);
        }

        public Task<IReadOnlyList<StoredCampaign>> ListDiscoverableAsync(
            Guid userId,
            bool isAdministrator,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<StoredCampaign>>(ForUser);
        }

        public Task<UpdateStoredCampaignOutcome> UpdateAsync(
            StoredCampaign campaign,
            int expectedRevision,
            CancellationToken cancellationToken)
        {
            Updated = campaign;
            Existing = campaign;
            return Task.FromResult(new UpdateStoredCampaignOutcome { IsSuccess = true, Campaign = campaign });
        }

        public Task<bool> DeleteAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            Deleted = Existing is not null && Existing.Id == campaignId;
            return Task.FromResult(Deleted);
        }

        public Task<bool> IsStorageKeyInUseAsync(
            string storageKey,
            Guid? excludingCampaignId,
            CancellationToken cancellationToken)
        {
            var campaigns = new List<StoredCampaign>();
            if (Existing is not null && Existing.Id != excludingCampaignId)
            {
                campaigns.Add(Existing);
            }

            if (Added is not null && Added.Id != excludingCampaignId)
            {
                campaigns.Add(Added);
            }

            campaigns.AddRange(ForUser.Where(item => item.Id != excludingCampaignId));
            return Task.FromResult(campaigns.Any(item =>
                item.MapStorageKey == storageKey
                || item.Factions.Any(faction => faction.FlagImageStorageKey == storageKey)
                || item.StructureTypes.Any(type => type.ImageStorageKey == storageKey)
                || item.TerrainTypes.SelectMany(type => type.Missions).Any(mission => mission.FileStorageKey == storageKey)
                || item.StructureTypes.SelectMany(type => type.Missions).Any(mission => mission.FileStorageKey == storageKey)));
        }

        public Task<UpdateStoredCampaignOutcome> UpdateMapGraphAsync(
            Guid campaignId,
            StoredMapGraph graph,
            int expectedRevision,
            DateTimeOffset updatedUtc,
            CancellationToken cancellationToken)
        {
            if (Existing is null || Existing.Id != campaignId)
            {
                return Task.FromResult(new UpdateStoredCampaignOutcome
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.CampaignNotFound,
                    Message = "The campaign was not found.",
                });
            }

            if (Existing.Revision != expectedRevision)
            {
                return Task.FromResult(new UpdateStoredCampaignOutcome
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ConcurrencyConflict,
                    Message = "The campaign was changed by another request. Reload and try again.",
                });
            }

            Existing = new StoredCampaign
            {
                Id = Existing.Id,
                Name = Existing.Name,
                Description = Existing.Description,
                PlayerSlotCount = Existing.PlayerSlotCount,
                IsPrivate = Existing.IsPrivate,
                IsPubliclyViewable = Existing.IsPubliclyViewable,
                JoinPasswordHash = Existing.JoinPasswordHash,
                CreatorIsParticipant = Existing.CreatorIsParticipant,
                City = Existing.City,
                Region = Existing.Region,
                Country = Existing.Country,
                MapStorageKey = Existing.MapStorageKey,
                Revision = expectedRevision + 1,
                CreatedUtc = Existing.CreatedUtc,
                UpdatedUtc = updatedUtc,
                CreatedByUserId = Existing.CreatedByUserId,
                Memberships = Existing.Memberships,
                Factions = Existing.Factions,
                AllyGroups = Existing.AllyGroups,
                Links = Existing.Links,
                TimeZoneId = Existing.TimeZoneId,
                StartsUtc = Existing.StartsUtc,
                EndsUtc = Existing.EndsUtc,
                RoundCount = Existing.RoundCount,
                RoundLengthAmount = Existing.RoundLengthAmount,
                RoundLengthUnit = Existing.RoundLengthUnit,
                Phases = Existing.Phases,
                MapGraph = graph,
                TerrainTypes = Existing.TerrainTypes,
                StructureTypes = Existing.StructureTypes,
                ItemObjectiveTypes = Existing.ItemObjectiveTypes,
                PublicObjectiveTypes = Existing.PublicObjectiveTypes,
                BattleScoring = Existing.BattleScoring,
                RankingObjectivePoints = Existing.RankingObjectivePoints,
                PlayState = Existing.PlayState,
            };
            return Task.FromResult(new UpdateStoredCampaignOutcome { IsSuccess = true, Campaign = Existing });
        }

        public Task<UpdateStoredCampaignOutcome> UpdatePlayStateAsync(
            Guid campaignId,
            Campaign.Domain.Play.CampaignPlayState playState,
            StoredMapGraph? mapGraph,
            DateTimeOffset endsUtc,
            int roundCount,
            int expectedRevision,
            DateTimeOffset updatedUtc,
            CancellationToken cancellationToken)
        {
            if (Existing is null || Existing.Id != campaignId)
            {
                return Task.FromResult(new UpdateStoredCampaignOutcome
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.CampaignNotFound,
                    Message = "The campaign was not found.",
                });
            }

            if (Existing.Revision != expectedRevision)
            {
                return Task.FromResult(new UpdateStoredCampaignOutcome
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ConcurrencyConflict,
                    Message = "The campaign was changed by another request. Reload and try again.",
                });
            }

            Existing = new StoredCampaign
            {
                Id = Existing.Id,
                Name = Existing.Name,
                Description = Existing.Description,
                PlayerSlotCount = Existing.PlayerSlotCount,
                IsPrivate = Existing.IsPrivate,
                IsPubliclyViewable = Existing.IsPubliclyViewable,
                JoinPasswordHash = Existing.JoinPasswordHash,
                CreatorIsParticipant = Existing.CreatorIsParticipant,
                City = Existing.City,
                Region = Existing.Region,
                Country = Existing.Country,
                MapStorageKey = Existing.MapStorageKey,
                Revision = expectedRevision + 1,
                CreatedUtc = Existing.CreatedUtc,
                UpdatedUtc = updatedUtc,
                CreatedByUserId = Existing.CreatedByUserId,
                Memberships = Existing.Memberships,
                Factions = Existing.Factions,
                AllyGroups = Existing.AllyGroups,
                Links = Existing.Links,
                TimeZoneId = Existing.TimeZoneId,
                StartsUtc = Existing.StartsUtc,
                EndsUtc = endsUtc,
                RoundCount = roundCount,
                RoundLengthAmount = Existing.RoundLengthAmount,
                RoundLengthUnit = Existing.RoundLengthUnit,
                Phases = Existing.Phases,
                MapGraph = mapGraph ?? Existing.MapGraph,
                TerrainTypes = Existing.TerrainTypes,
                StructureTypes = Existing.StructureTypes,
                ItemObjectiveTypes = Existing.ItemObjectiveTypes,
                PublicObjectiveTypes = Existing.PublicObjectiveTypes,
                BattleScoring = Existing.BattleScoring,
                RankingObjectivePoints = Existing.RankingObjectivePoints,
                PlayState = playState,
            };
            Updated = Existing;
            return Task.FromResult(new UpdateStoredCampaignOutcome { IsSuccess = true, Campaign = Existing });
        }
    }

    private sealed class FakeMapStorage : ICampaignMapStorage
    {
        public List<string> DeletedKeys { get; } = [];

        public Task<string> SaveAsync(ReadOnlyMemory<byte> content, string fileExtension, CancellationToken cancellationToken)
        {
            return Task.FromResult("maps/new.png");
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(storageKey);
            return Task.CompletedTask;
        }

        public Task<StoredCampaignMap?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredCampaignMap?>(null);
        }
    }

    private sealed class FakeAssetStorage : ICampaignAssetStorage
    {
        public List<string> DeletedKeys { get; } = [];

        public Task<string> SaveAsync(
            string folder,
            ReadOnlyMemory<byte> content,
            string fileExtension,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult($"{folder}/new.png");
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(storageKey);
            return Task.CompletedTask;
        }

        public Task<StoredCampaignAsset?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredCampaignAsset?>(null);
        }
    }

    private sealed class FakeMapProcessor : ICampaignMapProcessor
    {
        public Task<ProcessedCampaignMapResult> ProcessAsync(
            Stream content,
            string contentType,
            long? length,
            CancellationToken cancellationToken,
            int maxDimension = ICampaignMapProcessor.MapMaxDimension)
        {
            return Task.FromResult(new ProcessedCampaignMapResult
            {
                IsSuccess = true,
                Content = [1, 2, 3],
                FileExtension = ".png",
            });
        }
    }
}
