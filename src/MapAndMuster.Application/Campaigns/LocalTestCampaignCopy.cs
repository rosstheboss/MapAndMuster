using MapAndMuster.Application.Identity;
using MapAndMuster.Domain.Campaigns;
using MapAndMuster.Domain.Identity;

namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Named local copies of an Estalia-map campaign used to exercise live play at different phases.
/// </summary>
public enum LocalTestCampaignStage
{
    /// <summary>Start is one week away so setup, join, and faction assignment stay open.</summary>
    NotStarted,

    /// <summary>Round 1 Action 1 is open.</summary>
    Action1,

    /// <summary>Round 1 Action 2 is open after Action 1 has closed as Hold.</summary>
    Action2,

    /// <summary>Round 1 battle phase is open.</summary>
    Battle,
}

/// <summary>
/// Configures duplicated Estalia campaigns for local administrator testing.
/// </summary>
public static class LocalTestCampaignCopy
{
    /// <summary>Shared name prefix used to find and skip already-seeded copies.</summary>
    public const string NamePrefix = "[Test] Estalia";

    /// <summary>Action-window length for local test copies.</summary>
    public const int ActionMinutes = 10;

    /// <summary>Battle-phase length for local test copies.</summary>
    public const int BattleMinutes = 40;

    /// <summary>Round length matching two action windows plus the battle phase.</summary>
    public const int RoundMinutes = ActionMinutes + ActionMinutes + BattleMinutes;

    /// <summary>
    /// Returns the campaign name for a seeded stage.
    /// </summary>
    /// <param name="stage">The local test stage.</param>
    /// <returns>The campaign name.</returns>
    public static string NameFor(LocalTestCampaignStage stage)
    {
        return stage switch
        {
            LocalTestCampaignStage.NotStarted => $"{NamePrefix} (not started)",
            LocalTestCampaignStage.Action1 => $"{NamePrefix} (Action 1)",
            LocalTestCampaignStage.Action2 => $"{NamePrefix} (Action 2)",
            LocalTestCampaignStage.Battle => $"{NamePrefix} (Battle)",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown local test campaign stage."),
        };
    }

    /// <summary>
    /// Start instant that places the copy in the requested stage relative to <paramref name="utcNow"/>.
    /// </summary>
    /// <param name="stage">The local test stage.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <returns>The campaign start instant.</returns>
    public static DateTimeOffset StartsUtc(LocalTestCampaignStage stage, DateTimeOffset utcNow)
    {
        return stage switch
        {
            LocalTestCampaignStage.NotStarted => utcNow.AddDays(7),
            LocalTestCampaignStage.Action1 => utcNow.AddSeconds(-15),
            LocalTestCampaignStage.Action2 => utcNow.AddMinutes(-ActionMinutes).AddSeconds(-30),
            LocalTestCampaignStage.Battle => utcNow.AddMinutes(-(ActionMinutes * 2)).AddSeconds(-30),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown local test campaign stage."),
        };
    }

    /// <summary>
    /// Applies the local test schedule, player slots, and faction assignments onto a duplicate.
    /// </summary>
    /// <param name="duplicated">The freshly duplicated campaign.</param>
    /// <param name="stage">The local test stage.</param>
    /// <param name="managerUserId">The campaign manager, typically the privileged administrator.</param>
    /// <param name="testUsers">Seeded test accounts assigned as players.</param>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <returns>The configured campaign ready to persist.</returns>
    public static StoredCampaign Configure(
        StoredCampaign duplicated,
        LocalTestCampaignStage stage,
        Guid managerUserId,
        IReadOnlyList<UserAccount> testUsers,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(duplicated);
        ArgumentNullException.ThrowIfNull(testUsers);

        var startsUtc = StartsUtc(stage, utcNow);
        if (!IanaTimeZone.TryCreate(duplicated.TimeZoneId, out var timeZone, out _) || timeZone is null)
        {
            _ = IanaTimeZone.TryCreate(IanaTimeZone.UtcId, out timeZone, out _);
        }

        if (timeZone is null)
        {
            throw new InvalidOperationException("UTC is required.");
        }
        var roundCount = duplicated.RoundCount is >= CampaignSetupRules.MinRoundCount and <= CampaignSetupRules.MaxRoundCount
            ? duplicated.RoundCount
            : 8;
        var roundLength = new ScheduleDuration(RoundMinutes, DurationUnit.Minutes);
        var endsUtc = startsUtc;
        for (var round = 0; round < roundCount; round++)
        {
            endsUtc = CampaignCalendar.Add(endsUtc, timeZone, roundLength);
        }

        var slots = TestCampaignRoster.Slots(duplicated.Factions);
        var players = testUsers
            .Where(static user => user.IsTestAccount)
            .OrderBy(static user => user.TestAccountNumber ?? int.MaxValue)
            .Take(slots.Count)
            .ToArray();
        var factionsByName = duplicated.Factions.ToDictionary(static faction => faction.Name, StringComparer.Ordinal);
        var memberships = new List<StoredCampaignMembership>
        {
            new()
            {
                UserId = managerUserId,
                IsGameMaster = true,
                IsPlayer = false,
            },
        };
        for (var index = 0; index < players.Length; index++)
        {
            var slot = slots[index];
            var faction = factionsByName[slot.FactionName];
            memberships.Add(new StoredCampaignMembership
            {
                UserId = players[index].Id,
                IsGameMaster = false,
                IsPlayer = true,
                FactionId = faction.Id,
                Subfaction = slot.Subfaction,
            });
        }

        var playerSlots = Math.Max(CampaignSetupRules.MinPlayerCount, memberships.Count(static member => member.IsPlayer));
        return new StoredCampaign
        {
            Id = duplicated.Id,
            Name = NameFor(stage),
            Description =
                "Local test copy of the Estalia map. Two 10-minute action phases and a 40-minute battle phase. " +
                "Impersonate Test users to submit orders and battle reports.",
            PlayerSlotCount = playerSlots,
            IsPrivate = false,
            IsPubliclyViewable = true,
            JoinPasswordHash = null,
            CreatorIsParticipant = false,
            City = duplicated.City,
            Region = duplicated.Region,
            Country = duplicated.Country,
            MapStorageKey = duplicated.MapStorageKey,
            Revision = duplicated.Revision,
            CreatedUtc = duplicated.CreatedUtc,
            UpdatedUtc = utcNow,
            CreatedByUserId = managerUserId,
            Memberships = memberships,
            Factions = duplicated.Factions,
            AllyGroups = duplicated.AllyGroups,
            Links = duplicated.Links,
            TimeZoneId = timeZone.Id,
            StartsUtc = startsUtc,
            EndsUtc = endsUtc,
            RoundCount = roundCount,
            RoundLengthAmount = RoundMinutes,
            RoundLengthUnit = nameof(DurationUnit.Minutes),
            Phases =
            [
                new StoredRoundPhase
                {
                    Kind = nameof(RoundPhaseKind.Action),
                    DurationAmount = ActionMinutes,
                    DurationUnit = nameof(DurationUnit.Minutes),
                    EndPhaseEarlyIfAble = true,
                },
                new StoredRoundPhase
                {
                    Kind = nameof(RoundPhaseKind.Action),
                    DurationAmount = ActionMinutes,
                    DurationUnit = nameof(DurationUnit.Minutes),
                    EndPhaseEarlyIfAble = true,
                },
                new StoredRoundPhase
                {
                    Kind = nameof(RoundPhaseKind.Battle),
                    DurationAmount = BattleMinutes,
                    DurationUnit = nameof(DurationUnit.Minutes),
                    EndPhaseEarlyIfAble = false,
                },
            ],
            MapGraph = duplicated.MapGraph,
            PlayState = null,
            TerrainTypes = duplicated.TerrainTypes,
            StructureTypes = duplicated.StructureTypes,
            ItemObjectiveTypes = duplicated.ItemObjectiveTypes,
            PublicObjectiveTypes = duplicated.PublicObjectiveTypes,
            SpecialRules = duplicated.SpecialRules,
            Missions = duplicated.Missions,
            ForceStatuses = duplicated.ForceStatuses,
            PrivateObjectiveTypes = duplicated.PrivateObjectiveTypes,
            BattleScoring = duplicated.BattleScoring,
            RankingObjectivePoints = duplicated.RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = duplicated.SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = duplicated.SplitForceSupplyPenaltyIsPercent,
            StandardBattleResultQuestions = duplicated.StandardBattleResultQuestions,
            ArmyEscalations = duplicated.ArmyEscalations,
        };
    }
}
