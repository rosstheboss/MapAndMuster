using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Campaign.Api.Contracts;
using Campaign.Infrastructure.Email;
using Campaign.Infrastructure.Identity;
using Campaign.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Campaign.Api.IntegrationTests;

[Collection("api")]
public sealed class CampaignEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly CampaignApiFactory _factory;

    public CampaignEndpointTests(CampaignApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateListGetUpdateAndDeleteCampaign()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("gm");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var createdResponse = await client.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Border War"));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Border War", created.Name);
        Assert.Equal(8, created.PlayerSlotCount);
        Assert.Equal(1, created.OccupiedPlayerSlots);
        Assert.True(created.CanManage);
        Assert.True(created.IsParticipant);
        Assert.Equal(2, created.Factions.Count);
        Assert.Equal(1, created.Revision);
        Assert.Equal("Scheduled", created.Status);
        Assert.Equal(8, created.RoundCount);
        Assert.Equal("Weeks", created.RoundLengthUnit);
        Assert.Equal(3, created.Phases.Count);
        Assert.Equal("Action", created.Phases[0].Kind);
        Assert.Equal("Battle", created.Phases[2].Kind);
        Assert.Equal(12, created.TerrainTypes.Count);
        Assert.Equal("Beach", created.TerrainTypes[0].Name);
        Assert.Contains(created.TerrainTypes, type => type.Name == "Cave");
        Assert.Contains(created.TerrainTypes, type => type.Name == "Forest");
        Assert.Contains(created.TerrainTypes, type => type.Name == "Jungle");
        Assert.NotEmpty(created.TerrainTypes[0].Missions);
        Assert.Equal(6, created.StructureTypes.Count);
        Assert.Equal("#2563EB", created.Factions[0].Color);
        Assert.NotEqual(created.Factions[0].Color, created.Factions[1].Color);
        Assert.True(created.IsPubliclyViewable);
        Assert.Null(created.City);
        Assert.True(created.CanChooseFaction);
        Assert.Null(created.FactionId);

        var list = await client.GetFromJsonAsync<CampaignListItemResponse[]>("/api/campaigns", JsonOptions);
        Assert.NotNull(list);
        Assert.Contains(list, item => item.Id == created.Id && item.Name == "Border War");

        var detail = await client.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaigns/{created.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal("North", detail.Factions[0].Name);
        Assert.Equal(1, detail.Revision);

        using var updatedResponse = await client.PutAsJsonAsync(
            $"/api/campaigns/{created.Id}",
            ValidCampaignBody("Renamed War", detail.Revision, playerCount: 12, creatorIsParticipant: false));
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Renamed War", updated.Name);
        Assert.Equal(12, updated.PlayerSlotCount);
        Assert.Equal(0, updated.OccupiedPlayerSlots);
        Assert.False(updated.IsParticipant);
        Assert.Equal(8, updated.RoundCount);

        using var deleted = await client.DeleteAsync($"/api/campaigns/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var missing = await client.GetAsync($"/api/campaigns/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<CampaignListItemResponse[]>("/api/campaigns", JsonOptions);
        Assert.NotNull(afterDelete);
        Assert.DoesNotContain(afterDelete, item => item.Id == created.Id);
    }

    [Fact]
    public async Task DuplicateCampaignCopiesSetupAndStartsOneWeekLater()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("gm");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var createdResponse = await client.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Border War"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var duplicateResponse = await client.PostAsJsonAsync($"/api/campaigns/{created.Id}/duplicate", new { });
        Assert.Equal(HttpStatusCode.Created, duplicateResponse.StatusCode);
        var copy = await duplicateResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(copy);
        Assert.NotEqual(created.Id, copy.Id);
        Assert.Equal(created.Name, copy.Name);
        Assert.True(copy.CanManage);
        Assert.True(copy.IsParticipant);
        Assert.Equal(created.Factions.Count, copy.Factions.Count);
        Assert.Equal(created.TerrainTypes.Count, copy.TerrainTypes.Count);
        Assert.True(copy.StartsUtc - DateTimeOffset.UtcNow > TimeSpan.FromDays(6));
        Assert.True(copy.StartsUtc - DateTimeOffset.UtcNow < TimeSpan.FromDays(8));

        using var stranger = _factory.CreateClient();
        var strangerName = UniqueName("stranger");
        await RegisterConfirmAndLoginAsync(stranger, $"{strangerName}@example.test", strangerName);
        using var forbidden = await stranger.PostAsJsonAsync($"/api/campaigns/{created.Id}/duplicate", new { });
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
    }

    [Fact]
    public async Task CreatedCampaignUsesHuntInEstaliaSupplyDefaults()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("gm");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var createdResponse = await client.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Border War"));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(25, created.SplitForceSupplyPenaltyPercent);
        Assert.True(created.AlwaysAskGeneralKill);
        Assert.True(created.AlwaysAskSupplyLineDestroyed);
        Assert.Equal(1, created.GeneralKillCampaignPoints);
        Assert.Equal(1, created.SupplyLineDestroyedCampaignPoints);
        Assert.Equal(8, created.RoundEscalations.Count);
        Assert.Equal(1000, created.RoundEscalations[0].MaxArmyPoints);
        Assert.Equal(0, created.RoundEscalations[0].FreeSupplyPoints);
        Assert.Equal(1, created.RoundEscalations[2].FreeSupplyPoints);
        Assert.Equal(1, created.RoundEscalations[2].FreeCharacterCount);
        Assert.Equal(3, created.RoundEscalations[6].FreeSupplyPoints);
        Assert.All(created.TerrainTypes, type => Assert.Equal(1, type.SupplyPoints));
        Assert.All(created.StructureTypes, type =>
        {
            Assert.Equal(1, type.SupplyPoints);
            Assert.Equal(1, type.PillageSupplyPoints);
            Assert.Equal(1, type.DestroySupplyPoints);
        });
    }

    [Fact]
    public async Task AdministratorCanSaveListAndApplyACampaignPreset()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("adm");
        var email = $"{username}@example.test";
        await RegisterConfirmAndLoginAsync(client, email, username);

        using var createdResponse = await client.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Border War"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var forbidden = await client.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/presets",
            new SaveCampaignPresetRequest { Name = "Frontier War" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var forbiddenBody = await forbidden.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.Equal("campaign.forbidden", forbiddenBody?.Code);

        await MakeAdministratorAsync(username);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var presetName = UniqueName("Frontier");
        using var savedResponse = await client.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/presets",
            new SaveCampaignPresetRequest { Name = presetName });
        Assert.Equal(HttpStatusCode.Created, savedResponse.StatusCode);
        var saved = await savedResponse.Content.ReadFromJsonAsync<CampaignPresetListItemResponse>(JsonOptions);
        Assert.NotNull(saved);
        Assert.Equal(presetName, saved.Name);

        var listed = await client.GetFromJsonAsync<CampaignPresetListItemResponse[]>("/api/campaign-presets", JsonOptions);
        Assert.NotNull(listed);
        Assert.Contains(listed, item => item.Id == saved.Id && item.Name == presetName);

        var preset = await client.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaign-presets/{saved.Id}", JsonOptions);
        Assert.NotNull(preset);
        Assert.Equal(25, preset.SplitForceSupplyPenaltyPercent);
        Assert.Equal(created.TerrainTypes.Count, preset.TerrainTypes.Count);

        using var appliedResponse = await client.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/apply-preset",
            new ApplyCampaignPresetRequest { PresetId = saved.Id, Revision = created.Revision });
        Assert.Equal(HttpStatusCode.OK, appliedResponse.StatusCode);
        var applied = await appliedResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(applied);
        Assert.Equal(created.Revision + 1, applied.Revision);
    }

    [Fact]
    public async Task UnauthenticatedAccessIsRejected()
    {
        using var client = _factory.CreateClient();
        using var list = await client.GetAsync("/api/campaigns");
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
    }

    [Fact]
    public async Task NonMembersCanViewPublicCampaignsButCannotChangeThem()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("owner");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        using var createdResponse = await owner.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Secret War"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var stranger = _factory.CreateClient();
        var strangerName = UniqueName("stranger");
        await RegisterConfirmAndLoginAsync(stranger, $"{strangerName}@example.test", strangerName);

        using var get = await stranger.GetAsync($"/api/campaigns/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        using var update = await stranger.PutAsJsonAsync(
            $"/api/campaigns/{created.Id}",
            ValidCampaignBody("Hijacked", created.Revision));
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        using var delete = await stranger.DeleteAsync($"/api/campaigns/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task PrivateJoinPasswordIsOmittedFromResponses()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("priv");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var createdResponse = await client.PostAsJsonAsync(
            "/api/campaigns",
            ValidCampaignBody("Hidden War", isPrivate: true, joinPassword: "join-secret"));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var json = await createdResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("join-secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("JoinPassword", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);

        var created = JsonSerializer.Deserialize<CampaignDetailResponse>(json, JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.IsPrivate);
    }

    [Fact]
    public async Task InvalidSetupIsRejected()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("bad");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        using var response = await client.PostAsJsonAsync(
            "/api/campaigns",
            new
            {
                name = "x",
                playerCount = 1,
                isPrivate = true,
                creatorIsParticipant = true,
                factions = new[] { new { name = "North" } },
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("at least 2 factions", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapUploadRejectsSvgAndAcceptsPng()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("map");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);
        using var createdResponse = await client.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Mapped War"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var svgContent = new MultipartFormDataContent();
        var svg = new ByteArrayContent(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"));
        svg.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");
        svgContent.Add(svg, "map", "map.svg");
        svgContent.Add(new StringContent(created.Revision.ToString(CultureInfo.InvariantCulture)), "revision");
        using var svgResponse = await client.PostAsync($"/api/campaigns/{created.Id}/map", svgContent);
        Assert.Equal(HttpStatusCode.BadRequest, svgResponse.StatusCode);

        using var pngContent = new MultipartFormDataContent();
        var png = new ByteArrayContent(PngBytes);
        png.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        pngContent.Add(png, "map", "map.png");
        pngContent.Add(new StringContent(created.Revision.ToString(CultureInfo.InvariantCulture)), "revision");
        using var pngResponse = await client.PostAsync($"/api/campaigns/{created.Id}/map", pngContent);
        Assert.Equal(HttpStatusCode.OK, pngResponse.StatusCode);

        using var mapResponse = await client.GetAsync($"/api/campaigns/{created.Id}/map");
        Assert.Equal(HttpStatusCode.OK, mapResponse.StatusCode);
        Assert.Equal("image/png", mapResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MapGraphRejectsOverlapAndPersistsSharedBorders()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("graph");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);
        using var createdResponse = await client.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Mapped Border"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        var leftId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var rightId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var plainsId = created.TerrainTypes.Single(type => type.Name == "Plains").Id;
        var townId = created.StructureTypes.Single(type => type.Name == "Town").Id;
        using var overlap = await client.PutAsJsonAsync(
            $"/api/campaigns/{created.Id}/map/graph",
            new SaveMapGraphRequest
            {
                Revision = created.Revision,
                Territories =
                [
                    GraphTerritory(leftId, 1, 0.1, 0.1, 0.4, terrainTypeId: plainsId),
                    GraphTerritory(rightId, 2, 0.3, 0.1, 0.4, terrainTypeId: plainsId),
                ],
            });
        Assert.Equal(HttpStatusCode.BadRequest, overlap.StatusCode);

        using var savedResponse = await client.PutAsJsonAsync(
            $"/api/campaigns/{created.Id}/map/graph",
            new SaveMapGraphRequest
            {
                Revision = created.Revision,
                Territories =
                [
                    GraphTerritory(leftId, 1, 0.1, 0.1, 0.3, "Northmarch", plainsId, townId),
                    GraphTerritory(rightId, 2, 0.4, 0.1, 0.3, terrainTypeId: plainsId),
                ],
                Adjacencies =
                [
                    new AdjacencyRequest
                    {
                        TerritoryAId = leftId,
                        TerritoryBId = rightId,
                        Origin = "Manual",
                        MarkerX = 0.4,
                        MarkerY = 0.25,
                    },
                ],
            });
        Assert.Equal(HttpStatusCode.OK, savedResponse.StatusCode);
        var saved = await savedResponse.Content.ReadFromJsonAsync<MapGraphResponse>(JsonOptions);
        Assert.NotNull(saved);
        Assert.Equal(2, saved.Territories.Count);
        Assert.Equal("Northmarch", saved.Territories[0].Name);
        Assert.Equal(plainsId, saved.Territories[0].TerrainTypeId);
        Assert.Equal(townId, saved.Territories[0].StructureTypeId);
        Assert.Equal("Manual", saved.Adjacencies[0].Origin);

        var loaded = await client.GetFromJsonAsync<MapGraphResponse>($"/api/campaigns/{created.Id}/map/graph", JsonOptions);
        Assert.NotNull(loaded);
        Assert.Equal("Northmarch", loaded.Territories[0].Name);
        Assert.Single(loaded.Adjacencies);
    }

    [Fact]
    public async Task NonMembersCannotReadOrChangeAMapGraph()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("gowner");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        using var createdResponse = await owner.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Secret Map"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var stranger = _factory.CreateClient();
        var strangerName = UniqueName("gstranger");
        await RegisterConfirmAndLoginAsync(stranger, $"{strangerName}@example.test", strangerName);

        using var get = await stranger.GetAsync($"/api/campaigns/{created.Id}/map/graph");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        using var save = await stranger.PutAsJsonAsync(
            $"/api/campaigns/{created.Id}/map/graph",
            new SaveMapGraphRequest { Revision = created.Revision, Territories = [] });
        Assert.Equal(HttpStatusCode.NotFound, save.StatusCode);
    }

    [Fact]
    public async Task PlayersCanJoinPublicUpcomingCampaignsAndLeave()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("host");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        using var createdResponse = await owner.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Open War"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var player = _factory.CreateClient();
        var playerName = UniqueName("joiner");
        await RegisterConfirmAndLoginAsync(player, $"{playerName}@example.test", playerName);

        var all = await player.GetFromJsonAsync<CampaignListItemResponse[]>("/api/campaigns/all", JsonOptions);
        Assert.NotNull(all);
        var listed = Assert.Single(all, item => item.Id == created.Id);
        Assert.True(listed.CanJoin);
        Assert.True(listed.CanView);
        Assert.False(listed.CanLeave);

        using var joinedResponse = await player.PostAsJsonAsync($"/api/campaigns/{created.Id}/join", new JoinCampaignRequest());
        Assert.Equal(HttpStatusCode.OK, joinedResponse.StatusCode);
        var joined = await joinedResponse.Content.ReadFromJsonAsync<CampaignListItemResponse>(JsonOptions);
        Assert.NotNull(joined);
        Assert.True(joined.IsParticipant);
        Assert.False(joined.CanJoin);
        Assert.True(joined.CanLeave);
        Assert.Equal(2, joined.OccupiedPlayerSlots);

        using var left = await player.PostAsync($"/api/campaigns/{created.Id}/leave", null);
        Assert.Equal(HttpStatusCode.NoContent, left.StatusCode);

        using var managerLeave = await owner.PostAsync($"/api/campaigns/{created.Id}/leave", null);
        Assert.Equal(HttpStatusCode.Forbidden, managerLeave.StatusCode);
    }

    [Fact]
    public async Task ManagerCanAddKickAndAssignFactionOnAPrivateCampaign()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("host");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        using var createdResponse = await owner.PostAsJsonAsync(
            "/api/campaigns",
            ValidCampaignBody("Staff War", isPrivate: true, joinPassword: "join-secret", isPubliclyViewable: false));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var player = _factory.CreateClient();
        var playerName = UniqueName("joiner");
        await RegisterConfirmAndLoginAsync(player, $"{playerName}@example.test", playerName);
        var playerProfile = await player.GetFromJsonAsync<OwnProfileResponse>("/api/auth/me", JsonOptions);
        Assert.NotNull(playerProfile);

        using var joinWithoutPassword = await player.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/join",
            new JoinCampaignRequest());
        Assert.Equal(HttpStatusCode.BadRequest, joinWithoutPassword.StatusCode);

        var hits = await owner.GetFromJsonAsync<UserSearchHitResponse[]>(
            $"/api/campaigns/{created.Id}/members/search?q={Uri.EscapeDataString(playerName)}",
            JsonOptions);
        Assert.NotNull(hits);
        Assert.Contains(hits, hit => hit.UserId == playerProfile.Id);

        using var added = await owner.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/members",
            new AddCampaignMemberRequest { Revision = created.Revision, UserId = playerProfile.Id });
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var afterAdd = await added.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(afterAdd);
        Assert.Contains(afterAdd.Participants, participant => participant.UserId == playerProfile.Id && participant.IsPlayer);

        var south = Assert.Single(afterAdd.Factions, faction => faction.Name == "South");
        using var assigned = await owner.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/play/faction/assign",
            new AssignPlayerFactionRequest
            {
                Revision = afterAdd.Revision,
                UserId = playerProfile.Id,
                FactionId = south.Id,
            });
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);

        var afterAssign = await owner.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaigns/{created.Id}", JsonOptions);
        Assert.NotNull(afterAssign);
        Assert.Contains(
            afterAssign.Participants,
            participant => participant.UserId == playerProfile.Id && participant.FactionName == "South");

        using var kicked = await owner.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/members/kick",
            new KickCampaignMemberRequest { Revision = afterAssign.Revision, UserId = playerProfile.Id });
        Assert.Equal(HttpStatusCode.OK, kicked.StatusCode);
        var afterKick = await kicked.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(afterKick);
        Assert.DoesNotContain(afterKick.Participants, participant => participant.UserId == playerProfile.Id);

        var notices = await player.GetFromJsonAsync<HomeAttentionItemResponse[]>("/api/notifications", JsonOptions);
        Assert.NotNull(notices);
        Assert.Contains(notices, item => item.Kind == "CampaignKicked" && item.Title == "Removed from campaign");
    }

    [Fact]
    public async Task MembersCanChatInAnUpcomingCampaignLog()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("host");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        using var createdResponse = await owner.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Chat War"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.CanChat);

        using var stranger = _factory.CreateClient();
        var strangerName = UniqueName("watch");
        await RegisterConfirmAndLoginAsync(stranger, $"{strangerName}@example.test", strangerName);
        using var forbidden = await stranger.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/chat",
            new PostCampaignChatRequest { Revision = created.Revision, Message = "Hello" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var player = _factory.CreateClient();
        var playerName = UniqueName("joiner");
        await RegisterConfirmAndLoginAsync(player, $"{playerName}@example.test", playerName);
        using var joined = await player.PostAsJsonAsync($"/api/campaigns/{created.Id}/join", new JoinCampaignRequest());
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);

        var beforeChat = await owner.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaigns/{created.Id}", JsonOptions);
        Assert.NotNull(beforeChat);
        Assert.Contains(
            beforeChat.Participants,
            participant => participant.Username == ownerName && participant.IsGameMaster && participant.IsPlayer);
        Assert.Contains(beforeChat.MentionableMembers, member => member.Username == ownerName);
        Assert.Contains(beforeChat.MentionableMembers, member => member.Username == playerName);
        Assert.DoesNotContain(beforeChat.MentionableMembers, member => member.Username == strangerName);

        using var posted = await owner.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/chat",
            new PostCampaignChatRequest
            {
                Revision = beforeChat.Revision,
                Message = $"Hey, everybody! Hello @{playerName}.",
            });
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var afterPost = await posted.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(afterPost);
        var chat = Assert.Single(afterPost.Log);
        Assert.Equal("PlayerChat", chat.Kind);
        Assert.Equal(ownerName, chat.Originator);
        Assert.Equal(ownerName, chat.OriginatorUsername);
        Assert.Equal($"Hey, everybody! Hello @{playerName}.", chat.Summary);

        using var unknown = await owner.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/chat",
            new PostCampaignChatRequest { Revision = afterPost.Revision, Message = "Hi @stranger" });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    [Fact]
    public async Task PrivateChatIsOmittedFromUnauthorizedCampaignPayloads()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("host");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        using var createdResponse = await owner.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Private Chat War"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var player = _factory.CreateClient();
        var playerName = UniqueName("joiner");
        await RegisterConfirmAndLoginAsync(player, $"{playerName}@example.test", playerName);
        using var joined = await player.PostAsJsonAsync($"/api/campaigns/{created.Id}/join", new JoinCampaignRequest());
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);

        using var outsider = _factory.CreateClient();
        var outsiderName = UniqueName("other");
        await RegisterConfirmAndLoginAsync(outsider, $"{outsiderName}@example.test", outsiderName);
        using var outsiderJoined = await outsider.PostAsJsonAsync($"/api/campaigns/{created.Id}/join", new JoinCampaignRequest());
        Assert.Equal(HttpStatusCode.OK, outsiderJoined.StatusCode);

        var beforeChat = await owner.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaigns/{created.Id}", JsonOptions);
        Assert.NotNull(beforeChat);
        var playerId = beforeChat.MentionableMembers.Single(member => member.Username == playerName).UserId;

        using var posted = await owner.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/chat",
            new PostCampaignChatRequest
            {
                Revision = beforeChat.Revision,
                Message = "Keep this between us",
                ChannelKind = "Direct",
                TargetId = playerId,
            });
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);
        var ownerView = await posted.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(ownerView);
        Assert.Contains(ownerView.Log, item => item.Summary == "Keep this between us" && item.IsPrivate);

        var playerView = await player.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaigns/{created.Id}", JsonOptions);
        Assert.NotNull(playerView);
        Assert.Contains(playerView.Log, item => item.Summary == "Keep this between us");

        var outsiderView = await outsider.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaigns/{created.Id}", JsonOptions);
        Assert.NotNull(outsiderView);
        Assert.DoesNotContain(outsiderView.Log, item => item.Summary == "Keep this between us");
        Assert.DoesNotContain(JsonSerializer.Serialize(outsiderView), "Keep this between us");
    }

    [Fact]
    public async Task MembersCannotEditSiteNews()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("reader");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);

        var page = await client.GetFromJsonAsync<NewsPageResponse>("/api/news?page=1", JsonOptions);
        Assert.NotNull(page);
        Assert.Equal(1, page.Page);

        using var created = await client.PostAsJsonAsync(
            "/api/news",
            new SaveNewsArticleRequest { Title = "Secret patch", BodyMarkdown = "Do not publish this." });
        Assert.Equal(HttpStatusCode.Forbidden, created.StatusCode);
    }

    [Fact]
    public async Task HiddenCampaignsCanBeJoinedButNotViewedByStrangers()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("hidden");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        using var createdResponse = await owner.PostAsJsonAsync(
            "/api/campaigns",
            ValidCampaignBody("Hidden War", isPubliclyViewable: false));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.False(created.IsPubliclyViewable);

        using var stranger = _factory.CreateClient();
        var strangerName = UniqueName("seeker");
        await RegisterConfirmAndLoginAsync(stranger, $"{strangerName}@example.test", strangerName);

        var all = await stranger.GetFromJsonAsync<CampaignListItemResponse[]>("/api/campaigns/all", JsonOptions);
        Assert.NotNull(all);
        Assert.Contains(all, item => item.Id == created.Id && item.CanJoin);

        using var get = await stranger.GetAsync($"/api/campaigns/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task ConcurrentUpdatesReturnConflict()
    {
        var username = UniqueName("race");
        var email = $"{username}@example.test";
        using var first = _factory.CreateClient();
        await RegisterConfirmAndLoginAsync(first, email, username);
        using var createdResponse = await first.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Race War"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var second = _factory.CreateClient();
        using var login = await second.PostAsJsonAsync("/api/auth/login", new { email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = ValidCampaignBody("Race War", created.Revision);
        using var firstUpdate = await first.PutAsJsonAsync($"/api/campaigns/{created.Id}", body);
        using var secondUpdate = await second.PutAsJsonAsync($"/api/campaigns/{created.Id}", body);
        var statuses = new[] { firstUpdate.StatusCode, secondUpdate.StatusCode };
        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);
    }

    [Fact]
    public async Task PlaySeedsAndRejectsSetupAfterLaunch()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("play");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);
        var started = DateTime.UtcNow.AddHours(-1);
        using var createdResponse = await client.PostAsJsonAsync(
            "/api/campaigns",
            ValidCampaignBody("Live War", startsAtLocal: started.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.CanPlay);

        var play = await client.GetFromJsonAsync<CampaignPlayResponse>($"/api/campaigns/{created.Id}/play", JsonOptions);
        Assert.NotNull(play);
        Assert.Equal("InProgress", play.Status);
        Assert.Equal(1, play.CurrentRound);
        Assert.Equal("Action", play.CurrentPhaseKind);
        Assert.Contains(play.Log, item => item.Kind == "CampaignStarted");
        Assert.DoesNotContain(play.Log, item => item.Kind == "ResolvedAction");

        using var update = await client.PutAsJsonAsync(
            $"/api/campaigns/{created.Id}",
            ValidCampaignBody("Renamed Live War", play.Revision));
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        var error = await update.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.Equal("campaign.locked", error?.Code);
    }

    [Fact]
    public async Task PublicViewersCanReadPlayWithoutDrafting()
    {
        using var owner = _factory.CreateClient();
        var ownerName = UniqueName("host");
        await RegisterConfirmAndLoginAsync(owner, $"{ownerName}@example.test", ownerName);
        var started = DateTime.UtcNow.AddHours(-1);
        using var createdResponse = await owner.PostAsJsonAsync(
            "/api/campaigns",
            ValidCampaignBody("Open Live", startsAtLocal: started.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)));
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);

        using var stranger = _factory.CreateClient();
        var strangerName = UniqueName("watch");
        await RegisterConfirmAndLoginAsync(stranger, $"{strangerName}@example.test", strangerName);

        var play = await stranger.GetFromJsonAsync<CampaignPlayResponse>($"/api/campaigns/{created.Id}/play", JsonOptions);
        Assert.NotNull(play);
        Assert.Equal("InProgress", play.Status);
        Assert.False(play.CanChat);
        Assert.False(play.IsParticipant);
        Assert.Empty(play.MyDrafts);

        using var draft = await stranger.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/play/draft",
            new { revision = play.Revision, forceId = Guid.NewGuid(), kind = "Hold" });
        Assert.Equal(HttpStatusCode.Forbidden, draft.StatusCode);
    }

    [Fact]
    public async Task PlayerChoosesFactionBeforeTheCampaignStarts()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("pick");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);
        using var createdResponse = await client.PostAsJsonAsync("/api/campaigns", ValidCampaignBody("Faction War"));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.CanChooseFaction);
        Assert.Equal("Scheduled", created.Status);

        var south = created.Factions.Single(faction => faction.Name == "South");
        using var chosen = await client.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/play/faction",
            new ChooseFactionRequest { Revision = created.Revision, FactionId = south.Id });
        Assert.Equal(HttpStatusCode.OK, chosen.StatusCode);

        var detail = await client.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaigns/{created.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(south.Id, detail.FactionId);
        Assert.True(detail.CanChooseFaction);
        Assert.Equal("South", detail.Factions.Single(faction => faction.Id == detail.FactionId).Name);

        var north = detail.Factions.Single(faction => faction.Name == "North");
        using var changed = await client.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/play/faction",
            new ChooseFactionRequest { Revision = detail.Revision, FactionId = north.Id });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        var updated = await client.GetFromJsonAsync<CampaignDetailResponse>($"/api/campaigns/{created.Id}", JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(north.Id, updated.FactionId);
        Assert.True(updated.CanChooseFaction);
        Assert.Equal("North", updated.Factions.Single(faction => faction.Id == updated.FactionId).Name);
    }

    [Fact]
    public async Task PlayerCannotChangeFactionAfterTheCampaignStarts()
    {
        using var client = _factory.CreateClient();
        var username = UniqueName("lock");
        await RegisterConfirmAndLoginAsync(client, $"{username}@example.test", username);
        var started = DateTime.UtcNow.AddHours(-1);
        using var createdResponse = await client.PostAsJsonAsync(
            "/api/campaigns",
            ValidCampaignBody("Started War", startsAtLocal: started.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("InProgress", created.Status);
        Assert.True(created.CanChooseFaction);

        var south = created.Factions.Single(faction => faction.Name == "South");
        using var chosen = await client.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/play/faction",
            new ChooseFactionRequest { Revision = created.Revision, FactionId = south.Id });
        Assert.Equal(HttpStatusCode.OK, chosen.StatusCode);

        var play = await client.GetFromJsonAsync<CampaignPlayResponse>($"/api/campaigns/{created.Id}/play", JsonOptions);
        Assert.NotNull(play);
        Assert.Equal(south.Id, play.FactionId);
        Assert.False(play.CanChooseFaction);

        var north = created.Factions.Single(faction => faction.Name == "North");
        using var changed = await client.PostAsJsonAsync(
            $"/api/campaigns/{created.Id}/play/faction",
            new ChooseFactionRequest { Revision = play.Revision, FactionId = north.Id });
        Assert.Equal(HttpStatusCode.BadRequest, changed.StatusCode);
        var error = await changed.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.Equal("faction.already_chosen", error?.Code);
    }

    private async Task RegisterConfirmAndLoginAsync(HttpClient client, string email, string username)
    {
        using var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            username,
            password = ValidPassword,
            firstName = "Ada",
            lastName = "Lovelace",
            city = "Halifax",
            region = "Nova Scotia",
            country = "Canada",
            timeZoneId = "America/Halifax",
            displayNameMode = "Username",
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        await ConfirmEmailAsync(email);
        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    private async Task ConfirmEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();
        var message = dbContext.OutboxMessages
            .Where(item => item.Type == EmailOutbox.ConfirmEmailType)
            .OrderByDescending(item => item.CreatedUtc)
            .AsEnumerable()
            .First(item => item.Payload.Contains(email, StringComparison.Ordinal));
        var payload = JsonSerializer.Deserialize<OutboxEmailPayload>(message.Payload);
        Assert.NotNull(payload);

        using var client = _factory.CreateClient();
        using var confirm = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = payload.UserId, token = payload.Token });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
    }

    private async Task MakeAdministratorAsync(string username)
    {
        using var scope = _factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roles.RoleExistsAsync("Administrator").ConfigureAwait(false))
        {
            Assert.True((await roles.CreateAsync(new IdentityRole<Guid>("Administrator")).ConfigureAwait(false)).Succeeded);
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByNameAsync(username).ConfigureAwait(false);
        Assert.NotNull(user);
        Assert.True((await users.AddToRoleAsync(user, "Administrator").ConfigureAwait(false)).Succeeded);
    }

    private static SaveCampaignRequest ValidCampaignBody(
        string name,
        int? revision = null,
        int playerCount = 8,
        bool creatorIsParticipant = true,
        bool isPrivate = false,
        string? joinPassword = null,
        bool isPubliclyViewable = true,
        string? startsAtLocal = null)
    {
        return new SaveCampaignRequest
        {
            Name = name,
            Description = "A contested frontier.",
            PlayerCount = playerCount,
            IsPrivate = isPrivate,
            IsPubliclyViewable = isPubliclyViewable,
            JoinPassword = joinPassword,
            CreatorIsParticipant = creatorIsParticipant,
            Factions =
            [
                new FactionRequest { Name = "North", Subfactions = ["Riders"] },
                new FactionRequest { Name = "South" },
            ],
            Revision = revision,
            TimeZoneId = "UTC",
            StartsAtLocal = startsAtLocal ?? "2099-01-05T12:00",
            RoundCount = 8,
            RoundLengthAmount = 1,
            RoundLengthUnit = "Weeks",
            Phases =
            [
                new RoundPhaseRequest { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new RoundPhaseRequest { Kind = "Action", DurationAmount = 3, DurationUnit = "Days" },
                new RoundPhaseRequest { Kind = "Battle", DurationAmount = 1, DurationUnit = "Days" },
            ],
        };
    }

    private static TerritoryRequest GraphTerritory(
        Guid id,
        int number,
        double x,
        double y,
        double size,
        string? name = null,
        Guid? terrainTypeId = null,
        Guid? structureTypeId = null)
    {
        return new TerritoryRequest
        {
            Id = id,
            DisplayNumber = number,
            Name = name,
            TerrainTypeId = terrainTypeId,
            StructureTypeId = structureTypeId,
            Polygon =
            [
                new MapPointRequest { X = x, Y = y },
                new MapPointRequest { X = x + size, Y = y },
                new MapPointRequest { X = x + size, Y = y + size },
                new MapPointRequest { X = x, Y = y + size },
            ],
        };
    }

    private static string UniqueName(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}"[..20];
    }

    private const string ValidPassword = "Correct-Horse-1";

    private static readonly byte[] PngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
}
