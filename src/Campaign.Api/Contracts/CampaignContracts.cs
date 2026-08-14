using Campaign.Application.Campaigns;
using Campaign.Domain.Campaigns;

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

    /// <summary>Gets the join password. Omit on update to keep the current password.</summary>
    public string? JoinPassword { get; init; }

    /// <summary>Gets whether the creator also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

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
/// A campaign in the caller's list.
/// </summary>
public sealed class CampaignListItemResponse
{
    /// <summary>Gets the campaign identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerSlotCount { get; init; }

    /// <summary>Gets the number of occupied player slots.</summary>
    public required int OccupiedPlayerSlots { get; init; }

    /// <summary>Gets whether the campaign is private.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether the current user can manage the campaign.</summary>
    public required bool CanManage { get; init; }

    /// <summary>Gets whether the current user occupies a player slot.</summary>
    public required bool IsParticipant { get; init; }

    /// <summary>Gets the campaign lifecycle status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the campaign start instant, in UTC.</summary>
    public required DateTimeOffset StartsUtc { get; init; }

    /// <summary>Gets the campaign end instant, in UTC.</summary>
    public required DateTimeOffset EndsUtc { get; init; }
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

    /// <summary>Gets whether the creating manager also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

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
            PlayerSlotCount = item.PlayerSlotCount,
            OccupiedPlayerSlots = item.OccupiedPlayerSlots,
            IsPrivate = item.IsPrivate,
            CanManage = item.CanManage,
            IsParticipant = item.IsParticipant,
            Status = item.Status,
            StartsUtc = item.StartsUtc,
            EndsUtc = item.EndsUtc,
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
            CreatorIsParticipant = detail.CreatorIsParticipant,
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
                Name = faction.Name,
                Subfactions = faction.Subfactions,
                AllyGroupName = faction.AllyGroupName,
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
