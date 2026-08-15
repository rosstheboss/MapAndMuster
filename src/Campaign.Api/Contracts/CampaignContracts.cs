using Campaign.Application.Campaigns;
using Campaign.Application.Maps;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Maps;

namespace Campaign.Api.Contracts;

/// <summary>
/// Request to create or update campaign setup. Join passwords are never returned.
/// </summary>
public sealed class SaveCampaignRequest
{
    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerCount { get; init; }

    /// <summary>Gets whether a join password is required.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether non-members may view the campaign. Defaults to true.</summary>
    public bool IsPubliclyViewable { get; init; } = true;

    /// <summary>Gets the join password. Omit on update to keep the current password.</summary>
    public string? JoinPassword { get; init; }

    /// <summary>Gets whether the creator also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the factions.</summary>
    public required IReadOnlyList<FactionRequest> Factions { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public IReadOnlyList<AllyGroupRequest>? AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public IReadOnlyList<LinkRequest>? Links { get; init; }

    /// <summary>Gets the last observed campaign revision. Required for updates.</summary>
    public int? Revision { get; init; }

    /// <summary>Gets the IANA time zone used to interpret the start wall-clock time. Defaults to UTC.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>Gets the start date and time in the campaign time zone, without an offset.</summary>
    public string? StartsAtLocal { get; init; }

    /// <summary>Gets the number of rounds.</summary>
    public int RoundCount { get; init; }

    /// <summary>Gets the round-length amount.</summary>
    public int RoundLengthAmount { get; init; }

    /// <summary>Gets the round-length unit name.</summary>
    public string? RoundLengthUnit { get; init; }

    /// <summary>Gets the ordered action and battle steps that make up one round.</summary>
    public IReadOnlyList<RoundPhaseRequest>? Phases { get; init; }

    /// <summary>Gets the terrain types. Defaults are used when omitted.</summary>
    public IReadOnlyList<TerrainTypeRequest>? TerrainTypes { get; init; }

    /// <summary>Gets the structure types. Defaults are used when omitted.</summary>
    public IReadOnlyList<StructureTypeRequest>? StructureTypes { get; init; }
}

/// <summary>
/// An action or battle step in a save request.
/// </summary>
public sealed class RoundPhaseRequest
{
    /// <summary>Gets the phase kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the duration amount.</summary>
    public required int DurationAmount { get; init; }

    /// <summary>Gets the duration unit name.</summary>
    public required string DurationUnit { get; init; }
}

/// <summary>
/// Faction configuration in a save request.
/// </summary>
public sealed class FactionRequest
{
    /// <summary>Gets the faction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets optional subfaction names.</summary>
    public IReadOnlyList<string>? Subfactions { get; init; }

    /// <summary>Gets the optional ally-group name this faction joins.</summary>
    public string? AllyGroupName { get; init; }

    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the unique faction color as #RRGGBB.</summary>
    public string? Color { get; init; }

    /// <summary>Gets whether a player who chooses this faction must pick a subfaction.</summary>
    public bool RequiresSubfaction { get; init; }

    /// <summary>Gets whether an existing uploaded flag image should be removed.</summary>
    public bool ClearFlagImage { get; init; }
}

/// <summary>
/// Ally-group configuration in a save request.
/// </summary>
public sealed class AllyGroupRequest
{
    /// <summary>Gets the ally-group name.</summary>
    public required string Name { get; init; }
}

/// <summary>
/// Labeled external link in a save request.
/// </summary>
public sealed class LinkRequest
{
    /// <summary>Gets the display label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the destination URL.</summary>
    public required string Url { get; init; }
}

/// <summary>
/// Terrain type configuration in a save request.
/// </summary>
public sealed class TerrainTypeRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the terrain type name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets nested missions. At least one is required.</summary>
    public IReadOnlyList<MissionRequest>? Missions { get; init; }
}

/// <summary>
/// Structure type configuration in a save request.
/// </summary>
public sealed class StructureTypeRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the structure name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the built-in logo key used until a custom image is uploaded.</summary>
    public string? BuiltinSymbol { get; init; }

    /// <summary>Gets whether an existing uploaded logo should be removed.</summary>
    public bool ClearImage { get; init; }

    /// <summary>Gets nested missions.</summary>
    public IReadOnlyList<MissionRequest>? Missions { get; init; }
}

/// <summary>
/// Mission configuration nested under a terrain type or structure.
/// </summary>
public sealed class MissionRequest
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the mission name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets an optional http or https link.</summary>
    public string? Url { get; init; }

    /// <summary>Gets whether an existing uploaded file should be removed.</summary>
    public bool ClearFile { get; init; }
}

/// <summary>
/// A campaign in the caller's list.
/// </summary>
public sealed class CampaignListItemResponse
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerSlotCount { get; init; }

    /// <summary>Gets the number of occupied player slots.</summary>
    public required int OccupiedPlayerSlots { get; init; }

    /// <summary>Gets whether the campaign is private.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether non-members may view the campaign.</summary>
    public required bool IsPubliclyViewable { get; init; }

    /// <summary>Gets whether the current user can manage the campaign.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets whether the current user occupies a player slot.</summary>
    public required bool IsParticipant { get; init; }

    /// <summary>Gets whether the current user may view the campaign page.</summary>
    public required bool CanView { get; init; }

    /// <summary>Gets whether the current user may join as a player.</summary>
    public required bool CanJoin { get; init; }

    /// <summary>Gets whether the current user may leave the campaign.</summary>
    public required bool CanLeave { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the campaign lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the campaign start instant, in UTC.</summary>
    public required DateTimeOffset StartsUtc { get; init; }

    /// <summary>Gets the campaign end instant, in UTC.</summary>
    public required DateTimeOffset EndsUtc { get; init; }

    /// <summary>Gets the 1-based current round when the campaign is in progress.</summary>
    public int? CurrentRound { get; init; }

    /// <summary>Gets the display label for the current phase when the campaign is in progress.</summary>
    public string? CurrentPhaseLabel { get; init; }

    /// <summary>Gets when the current phase closes, in UTC.</summary>
    public DateTimeOffset? CurrentPhaseEndsUtc { get; init; }
}

/// <summary>
/// Member-visible campaign metadata. Join passwords are omitted.
/// </summary>
public sealed class CampaignDetailResponse
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerSlotCount { get; init; }

    /// <summary>Gets the number of occupied player slots.</summary>
    public required int OccupiedPlayerSlots { get; init; }

    /// <summary>Gets whether the campaign is private.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether non-members may view the campaign.</summary>
    public required bool IsPubliclyViewable { get; init; }

    /// <summary>Gets whether the creating manager also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets whether a map image is stored.</summary>
    public required bool HasMap { get; init; }

    /// <summary>Gets whether the current user can manage the campaign.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets whether the current user occupies a player slot.</summary>
    public required bool IsParticipant { get; init; }

    /// <summary>Gets the optimistic concurrency revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets when the campaign was created, in UTC.</summary>
    public required DateTimeOffset CreatedUtc { get; init; }

    /// <summary>Gets when the campaign was last edited, in UTC.</summary>
    public required DateTimeOffset UpdatedUtc { get; init; }

    /// <summary>Gets the factions.</summary>
    public required IReadOnlyList<FactionResponse> Factions { get; init; }

    /// <summary>Gets the terrain types.</summary>
    public required IReadOnlyList<TerrainTypeResponse> TerrainTypes { get; init; }

    /// <summary>Gets the structure types.</summary>
    public required IReadOnlyList<StructureTypeResponse> StructureTypes { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public required IReadOnlyList<AllyGroupResponse> AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public required IReadOnlyList<LinkResponse> Links { get; init; }

    /// <summary>Gets the IANA time zone used when the schedule was configured.</summary>
    public required string TimeZoneId { get; init; }

    /// <summary>Gets the start as a local wall-clock value in the campaign time zone.</summary>
    public required string StartsAtLocal { get; init; }

    /// <summary>Gets the campaign start instant, in UTC.</summary>
    public required DateTimeOffset StartsUtc { get; init; }

    /// <summary>Gets the campaign end instant, in UTC.</summary>
    public required DateTimeOffset EndsUtc { get; init; }

    /// <summary>Gets the number of rounds.</summary>
    public required int RoundCount { get; init; }

    /// <summary>Gets the round-length amount.</summary>
    public required int RoundLengthAmount { get; init; }

    /// <summary>Gets the round-length unit name.</summary>
    public required string RoundLengthUnit { get; init; }

    /// <summary>Gets the ordered action and battle steps in a round.</summary>
    public required IReadOnlyList<RoundPhaseResponse> Phases { get; init; }

    /// <summary>Gets the campaign lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the 1-based current round when the campaign is in progress.</summary>
    public int? CurrentRound { get; init; }

    /// <summary>Gets the 1-based current phase in the round when the campaign is in progress.</summary>
    public int? CurrentPhaseNumber { get; init; }

    /// <summary>Gets the current phase kind when the campaign is in progress.</summary>
    public string? CurrentPhaseKind { get; init; }

    /// <summary>Gets when the current phase opened, in UTC.</summary>
    public DateTimeOffset? CurrentPhaseStartsUtc { get; init; }

    /// <summary>Gets when the current phase closes, in UTC.</summary>
    public DateTimeOffset? CurrentPhaseEndsUtc { get; init; }
}

/// <summary>
/// An action or battle step in a campaign response.
/// </summary>
public sealed class RoundPhaseResponse
{
    /// <summary>Gets the phase kind name.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the duration amount.</summary>
    public required int DurationAmount { get; init; }

    /// <summary>Gets the duration unit name.</summary>
    public required string DurationUnit { get; init; }
}

/// <summary>
/// A faction in a campaign response.
/// </summary>
public sealed class FactionResponse
{
    /// <summary>Gets the faction identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the faction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the subfaction names.</summary>
    public required IReadOnlyList<string> Subfactions { get; init; }

    /// <summary>Gets the ally-group name this faction joins, if any.</summary>
    public string? AllyGroupName { get; init; }

    /// <summary>Gets the unique faction color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets whether a player who chooses this faction must pick a subfaction.</summary>
    public required bool RequiresSubfaction { get; init; }

    /// <summary>Gets whether the faction has an uploaded flag image.</summary>
    public required bool HasFlagImage { get; init; }
}

/// <summary>
/// An ally group in a campaign response.
/// </summary>
public sealed class AllyGroupResponse
{
    /// <summary>Gets the ally-group identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the ally-group name.</summary>
    public required string Name { get; init; }
}

/// <summary>
/// A labeled external link in a campaign response.
/// </summary>
public sealed class LinkResponse
{
    /// <summary>Gets the link identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the display label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the destination URL.</summary>
    public required string Url { get; init; }
}

/// <summary>
/// A terrain type in a campaign response.
/// </summary>
public sealed class TerrainTypeResponse
{
    /// <summary>Gets the terrain type identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the terrain type name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the unique overlay color as #RRGGBB.</summary>
    public required string Color { get; init; }

    /// <summary>Gets the missions.</summary>
    public required IReadOnlyList<MissionResponse> Missions { get; init; }
}

/// <summary>
/// A structure type in a campaign response.
/// </summary>
public sealed class StructureTypeResponse
{
    /// <summary>Gets the structure type identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the structure name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the built-in logo key, when no custom image is stored.</summary>
    public string? BuiltinSymbol { get; init; }

    /// <summary>Gets whether a custom logo image is stored.</summary>
    public required bool HasImage { get; init; }

    /// <summary>Gets the missions.</summary>
    public required IReadOnlyList<MissionResponse> Missions { get; init; }
}

/// <summary>
/// A mission nested under a terrain type or structure.
/// </summary>
public sealed class MissionResponse
{
    /// <summary>Gets the mission identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the mission name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional http or https link.</summary>
    public string? Url { get; init; }

    /// <summary>Gets whether a document file is stored.</summary>
    public required bool HasFile { get; init; }

    /// <summary>Gets the original uploaded file name, when a file is stored.</summary>
    public string? FileName { get; init; }
}

/// <summary>
/// Request to join a campaign as a player.
/// </summary>
public sealed class JoinCampaignRequest
{
    /// <summary>Gets the join password for a private campaign.</summary>
    public string? JoinPassword { get; init; }
}

/// <summary>
/// Maps campaign application models onto HTTP contracts.
/// </summary>
public static class CampaignResponses
{
    /// <summary>
    /// Maps a list item.
    /// </summary>
    /// <param name="item">The list item.</param>
    /// <returns>The HTTP response.</returns>
    public static CampaignListItemResponse FromListItem(CampaignListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new CampaignListItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            PlayerSlotCount = item.PlayerSlotCount,
            OccupiedPlayerSlots = item.OccupiedPlayerSlots,
            IsPrivate = item.IsPrivate,
            IsPubliclyViewable = item.IsPubliclyViewable,
            CanManage = item.CanManage,
            IsParticipant = item.IsParticipant,
            CanView = item.CanView,
            CanJoin = item.CanJoin,
            CanLeave = item.CanLeave,
            City = item.City,
            Region = item.Region,
            Country = item.Country,
            Status = item.Status,
            StartsUtc = item.StartsUtc,
            EndsUtc = item.EndsUtc,
            CurrentRound = item.CurrentRound,
            CurrentPhaseLabel = item.CurrentPhaseLabel,
            CurrentPhaseEndsUtc = item.CurrentPhaseEndsUtc,
        };
    }

    /// <summary>
    /// Maps a campaign detail. Join password hashes are not present on the source model.
    /// </summary>
    /// <param name="detail">The detail.</param>
    /// <returns>The HTTP response.</returns>
    public static CampaignDetailResponse FromDetail(CampaignDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return new CampaignDetailResponse
        {
            Id = detail.Id,
            Name = detail.Name,
            Description = detail.Description,
            PlayerSlotCount = detail.PlayerSlotCount,
            OccupiedPlayerSlots = detail.OccupiedPlayerSlots,
            IsPrivate = detail.IsPrivate,
            IsPubliclyViewable = detail.IsPubliclyViewable,
            CreatorIsParticipant = detail.CreatorIsParticipant,
            City = detail.City,
            Region = detail.Region,
            Country = detail.Country,
            HasMap = detail.HasMap,
            CanManage = detail.CanManage,
            IsParticipant = detail.IsParticipant,
            Revision = detail.Revision,
            CreatedUtc = detail.CreatedUtc,
            UpdatedUtc = detail.UpdatedUtc,
            Factions =
            [
                .. detail.Factions.Select(static faction => new FactionResponse
                {
                    Id = faction.Id,
                    Name = faction.Name,
                    Subfactions = faction.Subfactions,
                    AllyGroupName = faction.AllyGroupName,
                    Color = faction.Color,
                    RequiresSubfaction = faction.RequiresSubfaction,
                    HasFlagImage = faction.HasFlagImage,
                }),
            ],
            TerrainTypes =
            [
                .. detail.TerrainTypes.Select(static type => new TerrainTypeResponse
                {
                    Id = type.Id,
                    Name = type.Name,
                    Color = type.Color,
                    Missions =
                    [
                        .. type.Missions.Select(static mission => new MissionResponse
                        {
                            Id = mission.Id,
                            Name = mission.Name,
                            Url = mission.Url,
                            HasFile = mission.HasFile,
                            FileName = mission.FileName,
                        }),
                    ],
                }),
            ],
            StructureTypes =
            [
                .. detail.StructureTypes.Select(static type => new StructureTypeResponse
                {
                    Id = type.Id,
                    Name = type.Name,
                    BuiltinSymbol = type.BuiltinSymbol,
                    HasImage = type.HasImage,
                    Missions =
                    [
                        .. type.Missions.Select(static mission => new MissionResponse
                        {
                            Id = mission.Id,
                            Name = mission.Name,
                            Url = mission.Url,
                            HasFile = mission.HasFile,
                            FileName = mission.FileName,
                        }),
                    ],
                }),
            ],
            AllyGroups =
            [
                .. detail.AllyGroups.Select(static group => new AllyGroupResponse
                {
                    Id = group.Id,
                    Name = group.Name,
                }),
            ],
            Links =
            [
                .. detail.Links.Select(static link => new LinkResponse
                {
                    Id = link.Id,
                    Label = link.Label,
                    Url = link.Url,
                }),
            ],
            TimeZoneId = detail.TimeZoneId,
            StartsAtLocal = detail.StartsAtLocal,
            StartsUtc = detail.StartsUtc,
            EndsUtc = detail.EndsUtc,
            RoundCount = detail.RoundCount,
            RoundLengthAmount = detail.RoundLengthAmount,
            RoundLengthUnit = detail.RoundLengthUnit,
            Phases =
            [
                .. detail.Phases.Select(static phase => new RoundPhaseResponse
                {
                    Kind = phase.Kind,
                    DurationAmount = phase.DurationAmount,
                    DurationUnit = phase.DurationUnit,
                }),
            ],
            Status = detail.Status,
            CurrentRound = detail.CurrentRound,
            CurrentPhaseNumber = detail.CurrentPhaseNumber,
            CurrentPhaseKind = detail.CurrentPhaseKind,
            CurrentPhaseStartsUtc = detail.CurrentPhaseStartsUtc,
            CurrentPhaseEndsUtc = detail.CurrentPhaseEndsUtc,
        };
    }

    /// <summary>
    /// Maps a map-graph detail onto an HTTP response.
    /// </summary>
    /// <param name="detail">The detail.</param>
    /// <returns>The HTTP response.</returns>
    public static MapGraphResponse FromMapGraph(CampaignMapGraphDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return new MapGraphResponse
        {
            CampaignId = detail.CampaignId,
            Revision = detail.Revision,
            CanManage = detail.CanManage,
            Territories =
            [
                .. detail.Territories.Select(static territory => new TerritoryResponse
                {
                    Id = territory.Id,
                    DisplayNumber = territory.DisplayNumber,
                    Name = territory.Name,
                    Description = territory.Description,
                    Polygon =
                    [
                        .. territory.Polygon.Select(static point => new MapPointResponse { X = point.X, Y = point.Y }),
                    ],
                    TerrainTypeId = territory.TerrainTypeId,
                    StructureTypeId = territory.StructureTypeId,
                    OverlayColor = territory.OverlayColor,
                    OwnerFactionId = territory.OwnerFactionId,
                    SpawnFactionId = territory.SpawnFactionId,
                }),
            ],
            Adjacencies =
            [
                .. detail.Adjacencies.Select(static edge => new AdjacencyResponse
                {
                    Id = edge.Id,
                    TerritoryAId = edge.TerritoryAId,
                    TerritoryBId = edge.TerritoryBId,
                    Origin = edge.Origin,
                    MarkerX = edge.MarkerX,
                    MarkerY = edge.MarkerY,
                }),
            ],
        };
    }

    /// <summary>
    /// Maps HTTP territory requests onto domain inputs.
    /// </summary>
    /// <param name="territories">The request territories.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<TerritoryInput> ToTerritoryInputs(IReadOnlyList<TerritoryRequest>? territories)
    {
        if (territories is null)
        {
            return [];
        }

        return
        [
            .. territories.Select(static territory => new TerritoryInput
            {
                Id = territory.Id,
                DisplayNumber = territory.DisplayNumber,
                Name = territory.Name,
                Description = territory.Description,
                Polygon =
                [
                    .. territory.Polygon.Select(static point => new MapPointInput { X = point.X, Y = point.Y }),
                ],
                TerrainTypeId = territory.TerrainTypeId,
                StructureTypeId = territory.StructureTypeId,
                OverlayColor = territory.OverlayColor,
                OwnerFactionId = territory.OwnerFactionId,
                SpawnFactionId = territory.SpawnFactionId,
            }),
        ];
    }

    /// <summary>
    /// Maps HTTP adjacency requests onto domain inputs.
    /// </summary>
    /// <param name="adjacencies">The request adjacencies.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<AdjacencyInput> ToAdjacencyInputs(IReadOnlyList<AdjacencyRequest>? adjacencies)
    {
        if (adjacencies is null)
        {
            return [];
        }

        return
        [
            .. adjacencies.Select(static edge => new AdjacencyInput
            {
                Id = edge.Id,
                TerritoryAId = edge.TerritoryAId,
                TerritoryBId = edge.TerritoryBId,
                Origin = edge.Origin,
                MarkerX = edge.MarkerX,
                MarkerY = edge.MarkerY,
            }),
        ];
    }

    /// <summary>
    /// Maps HTTP faction requests onto domain inputs.
    /// </summary>
    /// <param name="factions">The request factions.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<FactionInput> ToFactionInputs(IReadOnlyList<FactionRequest>? factions)
    {
        if (factions is null)
        {
            return [];
        }

        return
        [
            .. factions.Select(static faction => new FactionInput
            {
                Id = faction.Id,
                Name = faction.Name,
                Color = faction.Color,
                Subfactions = faction.Subfactions,
                AllyGroupName = faction.AllyGroupName,
                RequiresSubfaction = faction.RequiresSubfaction,
                ClearFlagImage = faction.ClearFlagImage,
            }),
        ];
    }

    /// <summary>
    /// Maps HTTP ally-group requests onto domain inputs.
    /// </summary>
    /// <param name="groups">The request groups.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<AllyGroupInput>? ToAllyGroupInputs(IReadOnlyList<AllyGroupRequest>? groups)
    {
        return groups?
            .Select(static group => new AllyGroupInput { Name = group.Name })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP terrain-type requests onto domain inputs.
    /// </summary>
    /// <param name="types">The request terrain types.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<TerrainTypeInput>? ToTerrainTypeInputs(IReadOnlyList<TerrainTypeRequest>? types)
    {
        return types?
            .Select(static type => new TerrainTypeInput
            {
                Id = type.Id,
                Name = type.Name,
                Color = type.Color,
                Missions = ToMissionInputs(type.Missions),
            })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP structure-type requests onto domain inputs.
    /// </summary>
    /// <param name="types">The request structure types.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<StructureTypeInput>? ToStructureTypeInputs(IReadOnlyList<StructureTypeRequest>? types)
    {
        return types?
            .Select(static type => new StructureTypeInput
            {
                Id = type.Id,
                Name = type.Name,
                BuiltinSymbol = type.BuiltinSymbol,
                ClearImage = type.ClearImage,
                Missions = ToMissionInputs(type.Missions),
            })
            .ToArray();
    }

    private static MissionInput[]? ToMissionInputs(IReadOnlyList<MissionRequest>? missions)
    {
        return missions?
            .Select(static mission => new MissionInput
            {
                Id = mission.Id,
                Name = mission.Name,
                Url = mission.Url,
                ClearFile = mission.ClearFile,
            })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP link requests onto domain inputs.
    /// </summary>
    /// <param name="links">The request links.</param>
    /// <returns>The domain inputs.</returns>
    public static IReadOnlyList<CampaignLinkInput>? ToLinkInputs(IReadOnlyList<LinkRequest>? links)
    {
        return links?
            .Select(static link => new CampaignLinkInput { Label = link.Label, Url = link.Url })
            .ToArray();
    }

    /// <summary>
    /// Maps HTTP schedule fields onto a domain schedule input.
    /// </summary>
    /// <param name="request">The save request.</param>
    /// <returns>The domain schedule input.</returns>
    public static CampaignScheduleInput ToScheduleInput(SaveCampaignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CampaignScheduleInput
        {
            TimeZoneId = request.TimeZoneId,
            StartsAtLocal = request.StartsAtLocal,
            RoundCount = request.RoundCount,
            RoundLengthAmount = request.RoundLengthAmount,
            RoundLengthUnit = request.RoundLengthUnit,
            Phases = request.Phases?
                .Select(static phase => new RoundPhaseInput
                {
                    Kind = phase.Kind,
                    DurationAmount = phase.DurationAmount,
                    DurationUnit = phase.DurationUnit,
                })
                .ToArray(),
        };
    }
}

/// <summary>
/// Request to replace overlay territories and adjacencies.
/// </summary>
public sealed class SaveMapGraphRequest
{
    /// <summary>Gets the last observed campaign revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the territories.</summary>
    public required IReadOnlyList<TerritoryRequest> Territories { get; init; }

    /// <summary>Gets the adjacencies.</summary>
    public IReadOnlyList<AdjacencyRequest>? Adjacencies { get; init; }
}

/// <summary>
/// Territory fields in a map-graph save request.
/// </summary>
public sealed class TerritoryRequest
{
    /// <summary>Gets the territory identifier, when the client already assigned one.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the unique display number used when no name is set.</summary>
    public int DisplayNumber { get; init; }

    /// <summary>Gets the optional unique name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the polygon vertices.</summary>
    public required IReadOnlyList<MapPointRequest> Polygon { get; init; }

    /// <summary>Gets the campaign terrain type identifier.</summary>
    public Guid? TerrainTypeId { get; init; }

    /// <summary>Gets the optional campaign structure type identifier.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets the optional overlay color as #RRGGBB.</summary>
    public string? OverlayColor { get; init; }

    /// <summary>Gets the owning faction, or null when the territory is neutral.</summary>
    public Guid? OwnerFactionId { get; init; }

    /// <summary>Gets the spawn-location faction, if any.</summary>
    public Guid? SpawnFactionId { get; init; }
}

/// <summary>
/// A normalized map coordinate in a request.
/// </summary>
public sealed class MapPointRequest
{
    /// <summary>Gets the horizontal coordinate.</summary>
    public double X { get; init; }

    /// <summary>Gets the vertical coordinate.</summary>
    public double Y { get; init; }
}

/// <summary>
/// Adjacency fields in a map-graph save request.
/// </summary>
public sealed class AdjacencyRequest
{
    /// <summary>Gets the adjacency identifier, when the client already assigned one.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets one territory identifier.</summary>
    public required Guid TerritoryAId { get; init; }

    /// <summary>Gets the other territory identifier.</summary>
    public required Guid TerritoryBId { get; init; }

    /// <summary>Gets Generated or Manual.</summary>
    public string? Origin { get; init; }

    /// <summary>Gets the editor arrow marker X coordinate.</summary>
    public double MarkerX { get; init; }

    /// <summary>Gets the editor arrow marker Y coordinate.</summary>
    public double MarkerY { get; init; }
}

/// <summary>
/// Member-visible overlay graph for a campaign map.
/// </summary>
public sealed class MapGraphResponse
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the optimistic concurrency revision.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets whether the current user can edit the map graph.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets the overlay territories.</summary>
    public required IReadOnlyList<TerritoryResponse> Territories { get; init; }

    /// <summary>Gets the explicit adjacencies.</summary>
    public required IReadOnlyList<AdjacencyResponse> Adjacencies { get; init; }
}

/// <summary>
/// A territory in a map-graph response.
/// </summary>
public sealed class TerritoryResponse
{
    /// <summary>Gets the territory identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the unique display number used when no name is set.</summary>
    public required int DisplayNumber { get; init; }

    /// <summary>Gets the optional unique name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the polygon vertices.</summary>
    public required IReadOnlyList<MapPointResponse> Polygon { get; init; }

    /// <summary>Gets the campaign terrain type identifier.</summary>
    public required Guid TerrainTypeId { get; init; }

    /// <summary>Gets the optional campaign structure type identifier.</summary>
    public Guid? StructureTypeId { get; init; }

    /// <summary>Gets the optional overlay color as #RRGGBB.</summary>
    public string? OverlayColor { get; init; }

    /// <summary>Gets the owning faction, or null when the territory is neutral.</summary>
    public Guid? OwnerFactionId { get; init; }

    /// <summary>Gets the spawn-location faction, if any.</summary>
    public Guid? SpawnFactionId { get; init; }
}

/// <summary>
/// A normalized map coordinate in a response.
/// </summary>
public sealed class MapPointResponse
{
    /// <summary>Gets the horizontal coordinate.</summary>
    public required double X { get; init; }

    /// <summary>Gets the vertical coordinate.</summary>
    public required double Y { get; init; }
}

/// <summary>
/// An explicit adjacency in a map-graph response.
/// </summary>
public sealed class AdjacencyResponse
{
    /// <summary>Gets the adjacency identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets one territory identifier.</summary>
    public required Guid TerritoryAId { get; init; }

    /// <summary>Gets the other territory identifier.</summary>
    public required Guid TerritoryBId { get; init; }

    /// <summary>Gets Generated or Manual.</summary>
    public required string Origin { get; init; }

    /// <summary>Gets the editor arrow marker X coordinate.</summary>
    public required double MarkerX { get; init; }

    /// <summary>Gets the editor arrow marker Y coordinate.</summary>
    public required double MarkerY { get; init; }
}
