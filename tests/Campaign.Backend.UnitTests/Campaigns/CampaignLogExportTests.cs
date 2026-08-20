using System.Text;
using Campaign.Application.Campaigns;
using Campaign.Application.Common;
using Campaign.Application.Identity;
using Campaign.Application.Maps;
using Campaign.Application.Play;
using Campaign.Application.Ports;
using Campaign.Domain.Identity;
using Campaign.Domain.Play;

namespace Campaign.Backend.UnitTests.Campaigns;

public sealed class CampaignLogExportTests
{
    private static readonly Guid ManagerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PlayerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StrangerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CampaignId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset ChatAt = new(2026, 8, 15, 20, 45, 23, TimeSpan.Zero);
    private static readonly DateTimeOffset StartedAt = new(2026, 8, 15, 20, 46, 23, TimeSpan.Zero);

    [Fact]
    public void SelectOmitsPrivateChatAndHonorsPublicAndGameFilters()
    {
        var entries = new[]
        {
            Entry("PlayerChat", "Hello everyone", isPrivate: false),
            Entry("PlayerChat", "Keep this between us", isPrivate: true),
            Entry("CampaignStarted", "The campaign started."),
        };

        var both = CampaignLogExport.Select(entries, includePublicChat: true, includeGameLog: true);
        Assert.Collection(
            both,
            item => Assert.Equal("Hello everyone", item.Summary),
            item => Assert.Equal("The campaign started.", item.Summary));

        var chatOnly = CampaignLogExport.Select(entries, includePublicChat: true, includeGameLog: false);
        Assert.Equal("Hello everyone", Assert.Single(chatOnly).Summary);

        var gameOnly = CampaignLogExport.Select(entries, includePublicChat: false, includeGameLog: true);
        Assert.Equal("The campaign started.", Assert.Single(gameOnly).Summary);
    }

    [Fact]
    public void TextFileMatchesTheVisibleLogLine()
    {
        var file = CampaignLogExport.Write(
            "Border War",
            "UTC",
            [Entry("PlayerChat", "Hey, everybody!", isPrivate: false, originator: "northplayer")],
            CampaignLogExportFormat.Text);

        Assert.Equal("text/plain; charset=utf-8", file.ContentType);
        Assert.Equal("border-war-log.txt", file.DownloadName);
        Assert.Equal(
            "(2026-08-15 08:45:23 PM UTC) northplayer: Hey, everybody!\n",
            Encoding.UTF8.GetString(file.Content));
    }

    [Fact]
    public void CsvFileQuotesCommasAndNeverIncludesPrivateChat()
    {
        var selected = CampaignLogExport.Select(
            [
                Entry("PlayerChat", "Hello, frontier", isPrivate: false, originator: "northplayer"),
                Entry("PlayerChat", "Secret", isPrivate: true, originator: "northplayer"),
                Entry("CampaignStarted", "The campaign started.", originator: "Campaign", occurredUtc: StartedAt),
            ],
            includePublicChat: true,
            includeGameLog: true);
        var file = CampaignLogExport.Write("Border War", "UTC", selected, CampaignLogExportFormat.Csv);

        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal("border-war-log.csv", file.DownloadName);
        var text = Encoding.UTF8.GetString(file.Content);
        Assert.StartsWith("OccurredUtc,LocalTimestamp,Source,Kind,Originator,Summary\r\n", text, StringComparison.Ordinal);
        Assert.Contains("PublicChat", text, StringComparison.Ordinal);
        Assert.Contains("GameLog", text, StringComparison.Ordinal);
        Assert.Contains("\"Hello, frontier\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Secret", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ManagerCanExportPublicChatAndGameLogWithoutPrivateMessages()
    {
        var handler = new ExportCampaignLogHandler(new FakeCampaignStore { Existing = CampaignWithLog() }, new FakeAccounts());

        var result = await handler.HandleAsync(
            new ExportCampaignLogCommand
            {
                CampaignId = CampaignId,
                UserId = ManagerId,
                IsAdministrator = false,
                IncludePublicChat = true,
                IncludeGameLog = true,
                Format = CampaignLogExportFormat.Text,
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var text = Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("Hello everyone", text, StringComparison.Ordinal);
        Assert.Contains("The campaign started.", text, StringComparison.Ordinal);
        Assert.Contains("Campaign:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Keep this between us", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlayerCannotExportTheLog()
    {
        var handler = new ExportCampaignLogHandler(new FakeCampaignStore { Existing = CampaignWithLog() }, new FakeAccounts());

        var result = await handler.HandleAsync(
            Command(PlayerId, isAdministrator: false, includePublicChat: true, includeGameLog: true),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task AdministratorCanExportACampaignTheyDoNotBelongTo()
    {
        var handler = new ExportCampaignLogHandler(new FakeCampaignStore { Existing = CampaignWithLog() }, new FakeAccounts());

        var result = await handler.HandleAsync(
            Command(StrangerId, isAdministrator: true, includePublicChat: false, includeGameLog: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var text = Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("The campaign started.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Hello everyone", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HiddenCampaignIsNotFoundForANonMember()
    {
        var handler = new ExportCampaignLogHandler(new FakeCampaignStore { Existing = CampaignWithLog() }, new FakeAccounts());

        var result = await handler.HandleAsync(
            Command(StrangerId, isAdministrator: false, includePublicChat: true, includeGameLog: true),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CampaignNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task ExportRequiresAtLeastOneLogSource()
    {
        var handler = new ExportCampaignLogHandler(new FakeCampaignStore { Existing = CampaignWithLog() }, new FakeAccounts());

        var result = await handler.HandleAsync(
            Command(ManagerId, isAdministrator: false, includePublicChat: false, includeGameLog: false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.ValidationFailed, result.ErrorCode);
    }

    [Theory]
    [InlineData(null, CampaignLogExportFormat.Text)]
    [InlineData("txt", CampaignLogExportFormat.Text)]
    [InlineData("CSV", CampaignLogExportFormat.Csv)]
    public void ParsesExportFormatNames(string? raw, CampaignLogExportFormat expected)
    {
        Assert.True(CampaignLogExport.TryParseFormat(raw, out var format));
        Assert.Equal(expected, format);
    }

    [Fact]
    public void RejectsUnknownExportFormat()
    {
        Assert.False(CampaignLogExport.TryParseFormat("pdf", out _));
    }

    private static ExportCampaignLogCommand Command(
        Guid userId,
        bool isAdministrator,
        bool includePublicChat,
        bool includeGameLog)
    {
        return new ExportCampaignLogCommand
        {
            CampaignId = CampaignId,
            UserId = userId,
            IsAdministrator = isAdministrator,
            IncludePublicChat = includePublicChat,
            IncludeGameLog = includeGameLog,
            Format = CampaignLogExportFormat.Text,
        };
    }

    private static PlayLogEntryDetail Entry(
        string kind,
        string summary,
        bool isPrivate = false,
        string originator = "Campaign",
        DateTimeOffset? occurredUtc = null)
    {
        return new PlayLogEntryDetail
        {
            Id = Guid.NewGuid(),
            OccurredUtc = occurredUtc ?? ChatAt,
            Kind = kind,
            Originator = originator,
            Summary = summary,
            TerritoryId = null,
            ForceId = null,
            BattleId = null,
            IsSystemAdjustment = false,
            IsPrivate = isPrivate,
        };
    }

    private static StoredCampaign CampaignWithLog()
    {
        var members = new CampaignChatMember[]
        {
            new(ManagerId, "northplayer", "northplayer"),
            new(PlayerId, "southplayer", "southplayer"),
        };
        var memberships = new CampaignChatMembership[]
        {
            new(ManagerId, null, null),
            new(PlayerId, null, null),
        };
        Assert.True(CampaignChatRules.TryPost(
            CampaignPlayState.Empty,
            ManagerId,
            "Hello everyone",
            members,
            ChatAt,
            out var afterPublic,
            out _));
        Assert.True(CampaignChatRules.TryPost(
            afterPublic!,
            ManagerId,
            "Keep this between us",
            members,
            ChatAt.AddSeconds(1),
            out var afterPrivate,
            out _,
            new ChatChannel(ChatChannelKind.Direct, PlayerId),
            memberships));
        var play = afterPrivate!.AppendLog(new PlayLogEntry(
            Guid.NewGuid(),
            StartedAt,
            PlayLogKind.CampaignStarted,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            []));

        return new StoredCampaign
        {
            Id = CampaignId,
            Name = "Border War",
            Description = "A contested frontier.",
            PlayerSlotCount = 8,
            IsPrivate = true,
            IsPubliclyViewable = false,
            CreatorIsParticipant = true,
            Revision = 1,
            CreatedUtc = ChatAt,
            UpdatedUtc = ChatAt,
            CreatedByUserId = ManagerId,
            Memberships =
            [
                new StoredCampaignMembership { UserId = ManagerId, IsGameMaster = true, IsPlayer = true },
                new StoredCampaignMembership { UserId = PlayerId, IsGameMaster = false, IsPlayer = true },
            ],
            Factions = [],
            AllyGroups = [],
            Links = [],
            TimeZoneId = "UTC",
            StartsUtc = ChatAt.AddDays(1),
            EndsUtc = ChatAt.AddDays(30),
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases = [new StoredRoundPhase { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" }],
            PlayState = play,
            TerrainTypes = [],
            StructureTypes = [],
        };
    }

    private sealed class FakeCampaignStore : ICampaignStore
    {
        public StoredCampaign? Existing { get; init; }

        public Task<StoredCampaign> AddAsync(StoredCampaign campaign, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StoredCampaign?> FindByIdAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Existing is not null && Existing.Id == campaignId ? Existing : null);
        }

        public Task<IReadOnlyList<StoredCampaign>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<StoredCampaign>> ListDiscoverableAsync(
            Guid userId,
            bool isAdministrator,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpdateStoredCampaignOutcome> UpdateAsync(
            StoredCampaign campaign,
            int expectedRevision,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> IsStorageKeyInUseAsync(
            string storageKey,
            Guid? excludingCampaignId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpdateStoredCampaignOutcome> UpdateMapGraphAsync(
            Guid campaignId,
            StoredMapGraph graph,
            int expectedRevision,
            DateTimeOffset updatedUtc,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpdateStoredCampaignOutcome> UpdatePlayStateAsync(
            Guid campaignId,
            CampaignPlayState playState,
            StoredMapGraph? mapGraph,
            DateTimeOffset endsUtc,
            int roundCount,
            int expectedRevision,
            DateTimeOffset updatedUtc,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeAccounts : IUserAccountStore
    {
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> UsernameExistsAsync(string username, Guid? userIdToIgnore, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CreateLocalAccountOutcome> CreateLocalAccountAsync(
            CreateLocalAccountRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CreateLocalAccountOutcome> CreateExternalAccountAsync(
            CreateExternalAccountRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<UserAccount?>(new UserAccount
            {
                Id = userId,
                Email = $"{userId:N}@example.test",
                Username = userId == ManagerId ? "northplayer" : "southplayer",
                FirstName = "Test",
                LastName = "User",
                City = "Halifax",
                Country = "Canada",
                DisplayNameMode = DisplayNameMode.Username,
                CreatedUtc = ChatAt,
                UpdatedUtc = ChatAt,
                ProfileRevision = 1,
                EmailConfirmed = true,
            });
        }

        public Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<UpdateProfileOutcome> UpdateProfileAsync(
            UpdateStoredProfileRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ChangePasswordOutcome> ChangePasswordAsync(
            Guid userId,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string?> ReplaceAvatarKeyAsync(
            Guid userId,
            string? avatarStorageKey,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
