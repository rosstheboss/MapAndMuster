using Campaign.Application.Campaigns;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Play;

namespace Campaign.Application.Play;

/// <summary>
/// Maps stored catalogs onto play-time private-objective and item-choice rules.
/// </summary>
internal static class CampaignPlayCatalog
{
    public static Func<int, int> PickIndex { get; } = static count => count <= 0 ? 0 : Random.Shared.Next(count);

    public static IReadOnlyList<PrivateObjectiveTypePlayRules> PrivateTypes(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.PrivateObjectiveTypes.Select(static type => new PrivateObjectiveTypePlayRules(
                type.Id,
                type.Name,
                type.CampaignPoints,
                [
                    .. type.AllowedHolderKinds
                        .Select(static kind => Enum.TryParse<PrivateObjectiveHolderKind>(kind, true, out var parsed)
                            ? parsed
                            : (PrivateObjectiveHolderKind?)null)
                        .OfType<PrivateObjectiveHolderKind>(),
                ],
                Enum.TryParse<PrivateObjectiveScoringKind>(type.ScoringKind, true, out var scoring)
                    ? scoring
                    : PrivateObjectiveScoringKind.Manual,
                Enum.TryParse<PrivateObjectiveAutomaticKind>(type.AutomaticKind, true, out var automatic)
                    ? automatic
                    : PrivateObjectiveAutomaticKind.None,
                type.RequiredCount,
                type.StructureTypeId,
                type.TerritoryIds)),
        ];
    }

    public static IReadOnlyList<ItemObjectiveTypePlayRules> ItemPlayRules(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.ItemObjectiveTypes.Select(static type => new ItemObjectiveTypePlayRules(
                type.Id,
                type.Name,
                type.IsHiddenUntilFound,
                Enum.TryParse<ItemObjectivePlacementKind>(type.Placement, true, out var placement)
                    ? placement
                    : ItemObjectivePlacementKind.Random,
                type.AllowOnSpawn,
                type.FlavorText)),
        ];
    }

    public static IReadOnlyList<ItemObjectiveTypeSetup> ItemSetups(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return
        [
            .. campaign.ItemObjectiveTypes.Select(static type => new ItemObjectiveTypeSetup(
                type.Id,
                type.Name,
                type.IsHiddenUntilFound,
                Enum.TryParse<ItemObjectivePlacementKind>(type.Placement, true, out var placement)
                    ? placement
                    : ItemObjectivePlacementKind.Random,
                type.AllowOnSpawn,
                type.BuiltinSymbol,
                type.Color,
                clearImage: false,
                type.CampaignPoints,
                type.FlavorText,
                [
                    .. type.Choices.Select(static choice => new ItemObjectiveChoiceSetup(
                        choice.Id,
                        choice.Name,
                        [
                            .. choice.Results.Select(static result => new ItemObjectiveChoiceResultSetup(
                                result.Id,
                                result.FlavorText,
                                result.NewStateKey,
                                result.DestroyItem,
                                result.ReplacementItemTypeId,
                                result.GrantedPrivateObjectiveTypeId)),
                        ])),
                ],
                type.SpecialRuleIds)),
        ];
    }

    public static IReadOnlyDictionary<Guid, string> PrivateNames(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.PrivateObjectiveTypes.ToDictionary(static type => type.Id, static type => type.Name);
    }

    public static IReadOnlyDictionary<Guid, Guid?> AllyGroupByFaction(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        var groups = campaign.AllyGroups.ToDictionary(static group => group.Name, static group => group.Id, StringComparer.Ordinal);
        return campaign.Factions.ToDictionary(
            static faction => faction.Id,
            faction => faction.AllyGroupName is { } name && groups.TryGetValue(name, out var id)
                ? id
                : (Guid?)null);
    }

    public static IReadOnlyDictionary<Guid, Guid> FactionByPlayer(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return campaign.Memberships
            .Where(static member => member.IsPlayer && member.FactionId is not null)
            .ToDictionary(static member => member.UserId, static member => member.FactionId!.Value);
    }

    public static IReadOnlyList<PrivateObjectiveTerritory> Territories(PlayMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return
        [
            .. map.Territories.Select(static territory => new PrivateObjectiveTerritory(
                territory.Id,
                territory.OwnerFactionId,
                territory.StructureTypeId,
                territory.StructureCondition)),
        ];
    }

    public static CampaignPlayState ApplyEffects(
        StoredCampaign campaign,
        CampaignPlayState state,
        PlayMap map,
        DateTimeOffset utcNow,
        DateTimeOffset endsUtc)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        var types = PrivateTypes(campaign);
        var next = state;
        foreach (var player in campaign.Memberships.Where(static member => member.IsPlayer).OrderBy(static member => member.UserId))
        {
            next = PrivateObjectiveRules.EnsurePlayerAssignment(next, types, player.UserId, utcNow, PickIndex);
        }

        next = PrivateObjectiveRules.EvaluateAutomatic(
            next,
            types,
            Territories(map),
            FactionByPlayer(campaign),
            AllyGroupByFaction(campaign),
            next.BrokenAllyFactionIds.ToHashSet(),
            utcNow);
        var progress = next.Evaluate(campaign.StartsUtc, endsUtc, utcNow);
        if (progress.Status == CampaignStatus.Completed)
        {
            next = PrivateObjectiveRules.RevealRemainingAtCompletion(next, PrivateNames(campaign), utcNow);
        }

        return next;
    }
}
