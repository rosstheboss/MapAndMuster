using Campaign.Domain.Campaigns;
using Campaign.Domain.Identity;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Maps stored campaigns onto member-visible models. Join password hashes are omitted.
/// </summary>
public static class CampaignMapper
{
    /// <summary>
    /// Maps a stored campaign onto a list item for the specified viewer.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="viewerUserId">The viewing user's identifier.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <param name="isAdministrator">Whether the caller is a system administrator.</param>
    /// <returns>The list item.</returns>
    public static CampaignListItem ToListItem(
        StoredCampaign campaign,
        Guid viewerUserId,
        DateTimeOffset utcNow,
        bool isAdministrator = false)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var membership = MembershipFor(campaign, viewerUserId);
        var progress = ToSchedule(campaign).Evaluate(utcNow);
        return new CampaignListItem
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            OccupiedPlayerSlots = OccupiedPlayerSlots(campaign),
            IsPrivate = campaign.IsPrivate,
            IsPubliclyViewable = campaign.IsPubliclyViewable,
            CanManage = membership?.IsGameMaster == true,
            IsParticipant = membership?.IsPlayer == true,
            CanView = CampaignAccess.CanView(campaign, viewerUserId, isAdministrator),
            CanJoin = CampaignAccess.CanJoin(campaign, viewerUserId, utcNow),
            CanLeave = CampaignAccess.CanLeave(campaign, viewerUserId),
            City = campaign.City,
            Region = campaign.Region,
            Country = campaign.Country,
            Status = progress.Status.ToString(),
            StartsUtc = campaign.StartsUtc,
            EndsUtc = campaign.EndsUtc,
            CurrentRound = progress.CurrentRound,
            CurrentPhaseLabel = FormatCurrentPhaseLabel(campaign, progress),
            CurrentPhaseEndsUtc = progress.CurrentPhaseEndsUtc,
        };
    }

    /// <summary>
    /// Maps a stored campaign onto a member detail for the specified viewer.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="viewerUserId">The viewing user's identifier.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <returns>The detail.</returns>
    public static CampaignDetail ToDetail(StoredCampaign campaign, Guid viewerUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var membership = MembershipFor(campaign, viewerUserId);
        var schedule = ToSchedule(campaign);
        var progress = schedule.Evaluate(utcNow);
        return new CampaignDetail
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            PlayerSlotCount = campaign.PlayerSlotCount,
            OccupiedPlayerSlots = OccupiedPlayerSlots(campaign),
            IsPrivate = campaign.IsPrivate,
            IsPubliclyViewable = campaign.IsPubliclyViewable,
            CreatorIsParticipant = campaign.CreatorIsParticipant,
            City = campaign.City,
            Region = campaign.Region,
            Country = campaign.Country,
            HasMap = !string.IsNullOrWhiteSpace(campaign.MapStorageKey),
            CanManage = membership?.IsGameMaster == true,
            IsParticipant = membership?.IsPlayer == true,
            Revision = campaign.Revision,
            CreatedUtc = campaign.CreatedUtc,
            UpdatedUtc = campaign.UpdatedUtc,
            Factions = [.. campaign.Factions.Select(static faction => new FactionDetail
            {
                Id = faction.Id,
                Name = faction.Name,
                Color = faction.Color,
                Subfactions = faction.Subfactions,
                AllyGroupName = faction.AllyGroupName,
                RequiresSubfaction = faction.RequiresSubfaction,
                HasFlagImage = !string.IsNullOrWhiteSpace(faction.FlagImageStorageKey),
            })],
            TerrainTypes = [.. campaign.TerrainTypes.Select(static type => new TerrainTypeDetail
            {
                Id = type.Id,
                Name = type.Name,
                Color = type.Color,
                Missions = [.. type.Missions.Select(ToMission)],
            })],
            StructureTypes = [.. campaign.StructureTypes.Select(static type => new StructureTypeDetail
            {
                Id = type.Id,
                Name = type.Name,
                BuiltinSymbol = type.ImageStorageKey is null ? type.BuiltinSymbol : null,
                HasImage = !string.IsNullOrWhiteSpace(type.ImageStorageKey),
                Missions = [.. type.Missions.Select(ToMission)],
            })],
            AllyGroups = [.. campaign.AllyGroups.Select(static group => new AllyGroupDetail
            {
                Id = group.Id,
                Name = group.Name,
            })],
            Links = [.. campaign.Links.Select(static link => new CampaignLinkDetail
            {
                Id = link.Id,
                Label = link.Label,
                Url = link.Url,
            })],
            TimeZoneId = schedule.TimeZone.Id,
            StartsAtLocal = schedule.StartsAtLocal,
            StartsUtc = schedule.StartsUtc,
            EndsUtc = schedule.EndsUtc,
            RoundCount = schedule.RoundCount,
            RoundLengthAmount = schedule.RoundLength.Amount,
            RoundLengthUnit = schedule.RoundLength.Unit.ToString(),
            Phases =
            [
                .. schedule.Phases.Select(static phase => new RoundPhaseDetail
                {
                    Kind = phase.Kind.ToString(),
                    DurationAmount = phase.Duration.Amount,
                    DurationUnit = phase.Duration.Unit.ToString(),
                }),
            ],
            Status = progress.Status.ToString(),
            CurrentRound = progress.CurrentRound,
            CurrentPhaseNumber = progress.CurrentPhaseNumber,
            CurrentPhaseKind = progress.CurrentPhaseKind?.ToString(),
            CurrentPhaseStartsUtc = progress.CurrentPhaseStartsUtc,
            CurrentPhaseEndsUtc = progress.CurrentPhaseEndsUtc,
        };
    }

    /// <summary>
    /// Counts memberships that occupy a player slot.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <returns>The occupied slot count.</returns>
    public static int OccupiedPlayerSlots(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.Memberships.Count(static membership => membership.IsPlayer);
    }

    /// <summary>
    /// Returns the viewer's membership, if any.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <param name="viewerUserId">The viewing user's identifier.</param>
    /// <returns>The membership, or <see langword="null"/>.</returns>
    public static StoredCampaignMembership? MembershipFor(StoredCampaign campaign, Guid viewerUserId)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.Memberships.FirstOrDefault(membership => membership.UserId == viewerUserId);
    }

    private static string? FormatCurrentPhaseLabel(StoredCampaign campaign, CampaignProgress progress)
    {
        if (progress.Status != CampaignStatus.InProgress
            || progress.CurrentPhaseKind is null
            || progress.CurrentPhaseNumber is null)
        {
            return null;
        }

        var schedule = ToSchedule(campaign);
        return CampaignPhaseLabels.Format(schedule.Phases, progress.CurrentPhaseNumber.Value, progress.CurrentPhaseKind.Value);
    }

    /// <summary>
    /// Rebuilds the validated schedule from persistence fields.
    /// </summary>
    /// <param name="campaign">The stored campaign.</param>
    /// <returns>The schedule.</returns>
    public static CampaignSchedule ToSchedule(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (!IanaTimeZone.TryCreate(campaign.TimeZoneId, out var timeZone, out _))
        {
            timeZone = IanaTimeZone.TryCreate(IanaTimeZone.UtcId, out var utc, out _)
                ? utc
                : throw new InvalidOperationException("UTC is required.");
        }

        var roundLength = new ScheduleDuration(
            campaign.RoundLengthAmount,
            Enum.Parse<DurationUnit>(campaign.RoundLengthUnit, ignoreCase: true));
        var phases = campaign.Phases
            .Select(phase => new RoundPhaseSetup(
                Enum.Parse<RoundPhaseKind>(phase.Kind, ignoreCase: true),
                new ScheduleDuration(phase.DurationAmount, Enum.Parse<DurationUnit>(phase.DurationUnit, ignoreCase: true))))
            .ToArray();

        return new CampaignSchedule(
            timeZone,
            campaign.StartsUtc,
            campaign.EndsUtc,
            campaign.RoundCount,
            roundLength,
            phases);
    }

    private static MissionDetail ToMission(StoredMission mission)
    {
        return new MissionDetail
        {
            Id = mission.Id,
            Name = mission.Name,
            Url = mission.Url,
            HasFile = !string.IsNullOrWhiteSpace(mission.FileStorageKey),
            FileName = mission.FileName,
        };
    }
}
