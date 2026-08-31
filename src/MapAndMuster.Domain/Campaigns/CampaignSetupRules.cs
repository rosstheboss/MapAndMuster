using System.Diagnostics.CodeAnalysis;
using MapAndMuster.Domain.Common;
using MapAndMuster.Domain.Identity;
using MapAndMuster.Domain.Maps;

namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Validates campaign setup fields without failing on the first error.
/// </summary>
public static class CampaignSetupRules
{
    /// <summary>Minimum campaign name length.</summary>
    public const int NameMinLength = 3;

    /// <summary>Maximum campaign name length.</summary>
    public const int NameMaxLength = 80;

    /// <summary>Maximum campaign description length.</summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>Minimum configured player-slot count.</summary>
    public const int MinPlayerCount = 2;

    /// <summary>Maximum configured player-slot count.</summary>
    public const int MaxPlayerCount = 100;

    /// <summary>Minimum number of factions.</summary>
    public const int MinFactionCount = 2;

    /// <summary>Maximum number of factions.</summary>
    public const int MaxFactionCount = 50;

    /// <summary>Maximum subfactions per faction.</summary>
    public const int MaxSubfactionsPerFaction = 20;

    /// <summary>Maximum ally groups.</summary>
    public const int MaxAllyGroupCount = 25;

    /// <summary>Maximum labeled external links.</summary>
    public const int MaxLinkCount = 20;

    /// <summary>Minimum number of terrain types.</summary>
    public const int MinTerrainTypeCount = 1;

    /// <summary>Maximum number of terrain types.</summary>
    public const int MaxTerrainTypeCount = 50;

    /// <summary>Maximum number of structure types.</summary>
    public const int MaxStructureTypeCount = 50;

    /// <summary>Maximum number of item objective types.</summary>
    public const int MaxItemObjectiveTypeCount = 50;

    /// <summary>Maximum number of public campaign objectives.</summary>
    public const int MaxPublicObjectiveTypeCount = 50;

    /// <summary>Maximum number of private campaign objectives.</summary>
    public const int MaxPrivateObjectiveTypeCount = 50;

    /// <summary>Maximum number of reusable special rules.</summary>
    public const int MaxSpecialRuleCount = 80;

    /// <summary>Maximum number of configured force statuses.</summary>
    public const int MaxForceStatusCount = 20;

    /// <summary>Maximum campaign points for one configured source.</summary>
    public const int MaxCampaignPoints = 999;

    /// <summary>Minimum per-round army-point cap.</summary>
    public const int MinRoundArmyPoints = 10;

    /// <summary>Maximum per-round army-point cap.</summary>
    public const int MaxRoundArmyPoints = 100000;

    /// <summary>Maximum length of special-rule, flavor, and private-objective text.</summary>
    public const int CatalogTextMaxLength = 2000;

    /// <summary>Maximum holder choices on one item objective.</summary>
    public const int MaxItemObjectiveChoiceCount = 10;

    /// <summary>Maximum results on one item-objective choice.</summary>
    public const int MaxItemObjectiveChoiceResultCount = 12;

    /// <summary>Maximum missions nested under one terrain type or structure.</summary>
    public const int MaxMissionsPerCatalogItem = 20;

    /// <summary>Maximum missions in the campaign catalog.</summary>
    public const int MaxMissionCatalogCount = 80;

    /// <summary>Maximum length of faction, subfaction, and ally-group names.</summary>
    public const int NamedItemMaxLength = 60;

    /// <summary>Maximum link label length.</summary>
    public const int LinkLabelMaxLength = 80;

    /// <summary>Maximum link URL length.</summary>
    public const int LinkUrlMaxLength = 2048;

    /// <summary>Minimum private-campaign join password length.</summary>
    public const int JoinPasswordMinLength = 8;

    /// <summary>Maximum private-campaign join password length.</summary>
    public const int JoinPasswordMaxLength = 128;

    /// <summary>Minimum number of rounds.</summary>
    public const int MinRoundCount = 3;

    /// <summary>Maximum number of rounds.</summary>
    public const int MaxRoundCount = 52;

    /// <summary>Minimum action windows in a round.</summary>
    public const int MinActionPhaseCount = 1;

    /// <summary>Minimum battle phases in a round.</summary>
    public const int MinBattlePhaseCount = 1;

    /// <summary>Maximum result questions nested under one mission.</summary>
    public const int MaxMissionResultQuestionCount = 20;

    /// <summary>Maximum action and battle steps in a round.</summary>
    public const int MaxPhaseCount = 16;

    /// <summary>
    /// Collapses surrounding and repeated whitespace so names compare as one after trimming.
    /// </summary>
    public static string CollapseName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Case-insensitive uniqueness key for a campaign or preset name.
    /// </summary>
    public static string UniqueNameKey(string name)
    {
        return CollapseName(name).ToUpperInvariant();
    }

    /// <summary>
    /// Validates campaign setup and, when successful, returns the normalized configuration plus any new join password.
    /// </summary>
    /// <param name="name">The campaign name.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="playerCount">The configured player-slot count.</param>
    /// <param name="isPrivate">Whether the campaign requires a join password.</param>
    /// <param name="joinPassword">The proposed join password.</param>
    /// <param name="joinPasswordRequired">Whether a join password must be supplied for a private campaign.</param>
    /// <param name="creatorIsParticipant">Whether the creating manager occupies a player slot.</param>
    /// <param name="occupiedPlayerSlotsExcludingCreator">Player memberships that already occupy slots, excluding the creator toggle.</param>
    /// <param name="factions">The faction inputs.</param>
    /// <param name="allyGroups">The ally-group inputs.</param>
    /// <param name="links">The external-link inputs.</param>
    /// <param name="schedule">The round-schedule inputs.</param>
    /// <param name="setup">The validated setup when successful.</param>
    /// <param name="validatedJoinPassword">The join password to hash when a new password was supplied.</param>
    /// <param name="errors">Every field error, in a stable order.</param>
    /// <param name="isPubliclyViewable">Whether non-members may view the campaign after it starts.</param>
    /// <param name="city">The optional city.</param>
    /// <param name="region">The optional state, province, or region.</param>
    /// <param name="country">The optional country.</param>
    /// <returns><see langword="true"/> when the setup is valid.</returns>
    public static bool TryCreate(
        string? name,
        string? description,
        int playerCount,
        bool isPrivate,
        string? joinPassword,
        bool joinPasswordRequired,
        bool creatorIsParticipant,
        int occupiedPlayerSlotsExcludingCreator,
        IReadOnlyList<FactionInput>? factions,
        IReadOnlyList<AllyGroupInput>? allyGroups,
        IReadOnlyList<CampaignLinkInput>? links,
        CampaignScheduleInput? schedule,
        [NotNullWhen(true)] out CampaignSetup? setup,
        out string? validatedJoinPassword,
        out IReadOnlyList<DomainError> errors,
        bool isPubliclyViewable = true,
        string? city = null,
        string? region = null,
        string? country = null)
    {
        return TryCreate(
            name,
            description,
            playerCount,
            isPrivate,
            joinPassword,
            joinPasswordRequired,
            creatorIsParticipant,
            occupiedPlayerSlotsExcludingCreator,
            factions,
            allyGroups,
            links,
            schedule,
            terrainTypes: null,
            structureTypes: null,
            out setup,
            out validatedJoinPassword,
            out errors,
            isPubliclyViewable,
            city,
            region,
            country,
            itemObjectiveTypes: null);
    }

    /// <summary>
    /// Validates campaign setup including terrain and structure catalogs.
    /// </summary>
    /// <param name="name">The campaign name.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="playerCount">The configured player-slot count.</param>
    /// <param name="isPrivate">Whether the campaign requires a join password.</param>
    /// <param name="joinPassword">The proposed join password.</param>
    /// <param name="joinPasswordRequired">Whether a join password must be supplied for a private campaign.</param>
    /// <param name="creatorIsParticipant">Whether the creating manager occupies a player slot.</param>
    /// <param name="occupiedPlayerSlotsExcludingCreator">Player memberships that already occupy slots, excluding the creator toggle.</param>
    /// <param name="factions">The faction inputs.</param>
    /// <param name="allyGroups">The ally-group inputs.</param>
    /// <param name="links">The external-link inputs.</param>
    /// <param name="schedule">The round-schedule inputs.</param>
    /// <param name="terrainTypes">The terrain-type inputs. Defaults are used when omitted.</param>
    /// <param name="structureTypes">The structure-type inputs. Defaults are used when omitted.</param>
    /// <param name="itemObjectiveTypes">The item-objective inputs. Omitted or empty means none.</param>
    /// <param name="publicObjectiveTypes">The public-objective inputs. Omitted or empty means none.</param>
    /// <param name="pointsPerBattleWon">Straight campaign points for a battle win. Defaults to 2.</param>
    /// <param name="pointsPerBattleDraw">Campaign points for each participant of a draw. Defaults to 1.</param>
    /// <param name="useDifferentialBattleScoring">Whether battle campaign points use score differential. Defaults to true.</param>
    /// <param name="differentialMultiplier">Multiplier applied to the winner-minus-loser score. Never 0. Defaults to 1.</param>
    /// <param name="differentialMinimum">Inclusive lower clamp for differential campaign points. Defaults to 0.</param>
    /// <param name="differentialMaximum">Inclusive upper clamp for differential campaign points. Defaults to 10.</param>
    /// <param name="allowNegativeDifferential">Whether the loser can receive negative campaign points. Defaults to false.</param>
    /// <param name="mostTerritoriesCampaignPoints">Points for most territories currently controlled. Zero ignores the objective.</param>
    /// <param name="longestTerritoryChainCampaignPoints">Points for the longest owned territory chain. Zero ignores the objective.</param>
    /// <param name="mostBattlesWonCampaignPoints">Points for most battle wins. Zero ignores the objective.</param>
    /// <param name="mostStructurePointsCampaignPoints">Points for most structure campaign points. Zero ignores the objective.</param>
    /// <param name="pointsPerTerritoryCampaignPoints">Points awarded for each currently owned territory. Zero ignores the objective.</param>
    /// <param name="alliedRelicControlCampaignPoints">Points for each revealed relic held by an ally or faction-mate other than the player. Zero ignores the objective.</param>
    /// <param name="specialRules">Reusable special-rule inputs. Omitted or empty means none.</param>
    /// <param name="privateObjectiveTypes">Private-objective inputs. Omitted or empty means none.</param>
    /// <param name="forceStatuses">Force-status inputs other than Normal. Omitted or empty means none.</param>
    /// <param name="splitForceSupplyPenaltyPercent">Supply subtracted from map supply when a player has split forces.</param>
    /// <param name="splitForceSupplyPenaltyIsPercent">Whether the split-force penalty is a percent of map supply. The default is a raw amount.</param>
    /// <param name="alwaysAskGeneralKill">Whether every battle report asks if the enemy general was slain.</param>
    /// <param name="alwaysAskSupplyLineDestroyed">Whether every battle report asks if the enemy supply line was destroyed.</param>
    /// <param name="generalKillCampaignPoints">Campaign points awarded for a slain enemy general.</param>
    /// <param name="supplyLineDestroyedCampaignPoints">Campaign points awarded for destroying the enemy supply line.</param>
    /// <param name="missions">Reusable mission catalog inputs. Omitted means nested terrain and structure missions only.</param>
    /// <param name="setup">The validated setup when successful.</param>
    /// <param name="validatedJoinPassword">The join password to hash when a new password was supplied.</param>
    /// <param name="errors">Every field error, in a stable order.</param>
    /// <param name="isPubliclyViewable">Whether non-members may view the campaign after it starts.</param>
    /// <param name="city">The optional city.</param>
    /// <param name="region">The optional state, province, or region.</param>
    /// <param name="country">The optional country.</param>
    /// <returns><see langword="true"/> when the setup is valid.</returns>
    public static bool TryCreate(
        string? name,
        string? description,
        int playerCount,
        bool isPrivate,
        string? joinPassword,
        bool joinPasswordRequired,
        bool creatorIsParticipant,
        int occupiedPlayerSlotsExcludingCreator,
        IReadOnlyList<FactionInput>? factions,
        IReadOnlyList<AllyGroupInput>? allyGroups,
        IReadOnlyList<CampaignLinkInput>? links,
        CampaignScheduleInput? schedule,
        IReadOnlyList<TerrainTypeInput>? terrainTypes,
        IReadOnlyList<StructureTypeInput>? structureTypes,
        [NotNullWhen(true)] out CampaignSetup? setup,
        out string? validatedJoinPassword,
        out IReadOnlyList<DomainError> errors,
        bool isPubliclyViewable = true,
        string? city = null,
        string? region = null,
        string? country = null,
        IReadOnlyList<ItemObjectiveTypeInput>? itemObjectiveTypes = null,
        IReadOnlyList<PublicObjectiveTypeInput>? publicObjectiveTypes = null,
        int? pointsPerBattleWon = null,
        int? pointsPerBattleDraw = null,
        bool? useDifferentialBattleScoring = null,
        decimal? differentialMultiplier = null,
        int? differentialMinimum = null,
        int? differentialMaximum = null,
        bool? allowNegativeDifferential = null,
        int? mostTerritoriesCampaignPoints = null,
        int? longestTerritoryChainCampaignPoints = null,
        int? mostBattlesWonCampaignPoints = null,
        int? mostStructurePointsCampaignPoints = null,
        int? pointsPerTerritoryCampaignPoints = null,
        int? alliedRelicControlCampaignPoints = null,
        IReadOnlyList<SpecialRuleInput>? specialRules = null,
        IReadOnlyList<PrivateObjectiveTypeInput>? privateObjectiveTypes = null,
        IReadOnlyList<ForceStatusInput>? forceStatuses = null,
        int? splitForceSupplyPenaltyPercent = null,
        bool? splitForceSupplyPenaltyIsPercent = null,
        bool? alwaysAskGeneralKill = null,
        bool? alwaysAskSupplyLineDestroyed = null,
        int? generalKillCampaignPoints = null,
        int? supplyLineDestroyedCampaignPoints = null,
        IReadOnlyList<MissionInput>? missions = null)
    {
        var collected = new List<DomainError>();
        setup = null;
        validatedJoinPassword = null;

        var parsedName = ParseRequiredName(name, "name", "Campaign name", NameMinLength, NameMaxLength, collected);
        var parsedDescription = ParseOptionalDescription(description, collected);

        if (playerCount < MinPlayerCount || playerCount > MaxPlayerCount)
        {
            collected.Add(new DomainError(
                "campaign.player_count.invalid",
                $"Number of players must be between {MinPlayerCount} and {MaxPlayerCount}.",
                "playerCount"));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(occupiedPlayerSlotsExcludingCreator);
        var occupiedSlots = occupiedPlayerSlotsExcludingCreator + (creatorIsParticipant ? 1 : 0);

        if (playerCount >= MinPlayerCount && occupiedSlots > playerCount)
        {
            collected.Add(new DomainError(
                "campaign.player_count.occupied",
                "Number of players cannot be lower than the number of occupied player slots.",
                "playerCount"));
        }

        if (isPrivate)
        {
            validatedJoinPassword = ParseJoinPassword(joinPassword, joinPasswordRequired, collected);
        }

        if (!GeographicLocation.TryNormalizeOptional(
                city,
                region,
                country,
                out var parsedCity,
                out var parsedRegion,
                out var parsedCountry,
                out var locationErrors))
        {
            collected.AddRange(locationErrors);
        }

        var usedIds = new HashSet<Guid>();
        var parsedGroups = ParseAllyGroups(allyGroups, usedIds, collected);
        var missionIndex = new MissionIndex();
        var parsedSpecialRules = ParseSpecialRules(specialRules, usedIds, collected);
        var parsedForceStatuses = ParseForceStatuses(forceStatuses, usedIds, collected);
        var specialRuleIds = parsedSpecialRules.Select(static rule => rule.Id).ToHashSet();
        var parsedFactions = ParseFactions(factions, parsedGroups, usedIds, specialRuleIds, collected);
        ValidateAllyMembership(parsedFactions, parsedGroups, collected);
        var parsedLinks = ParseLinks(links, collected);
        _ = ParseMissions(
            missions,
            usedIds,
            missionIndex,
            "missions",
            "Mission catalog",
            requireAtLeastOne: false,
            collected,
            maxCount: MaxMissionCatalogCount);
        var parsedTerrain = ParseTerrainTypes(terrainTypes, usedIds, missionIndex, collected);
        var parsedStructures = ParseStructureTypes(structureTypes, usedIds, missionIndex, collected);
        var structureTypeIds = parsedStructures.Select(static type => type.Id).ToHashSet();
        var parsedPrivate = ParsePrivateObjectiveTypes(privateObjectiveTypes, usedIds, structureTypeIds, collected);
        var privateObjectiveIds = parsedPrivate.Select(static type => type.Id).ToHashSet();
        var parsedItems = ParseItemObjectiveTypes(
            itemObjectiveTypes,
            usedIds,
            specialRuleIds,
            privateObjectiveIds,
            collected);
        var parsedPublic = ParsePublicObjectiveTypes(publicObjectiveTypes, usedIds, collected);
        var parsedBattleScoring = ParseBattleScoring(
            pointsPerBattleWon,
            pointsPerBattleDraw,
            useDifferentialBattleScoring,
            differentialMultiplier,
            differentialMinimum,
            differentialMaximum,
            allowNegativeDifferential,
            collected);
        var parsedRanking = ParseRankingObjectives(
            mostTerritoriesCampaignPoints,
            longestTerritoryChainCampaignPoints,
            mostBattlesWonCampaignPoints,
            mostStructurePointsCampaignPoints,
            pointsPerTerritoryCampaignPoints,
            alliedRelicControlCampaignPoints,
            collected);
        var parsedSchedule = ParseSchedule(schedule, collected);
        var parsedSplitForce = ParseSplitForcePenalty(splitForceSupplyPenaltyPercent, collected);

        if (collected.Count > 0)
        {
            errors = collected;
            validatedJoinPassword = null;
            return false;
        }

        setup = new CampaignSetup(
            parsedName!,
            parsedDescription,
            playerCount,
            isPrivate,
            isPubliclyViewable,
            creatorIsParticipant,
            parsedCity,
            parsedRegion,
            parsedCountry,
            parsedFactions,
            parsedGroups,
            parsedLinks,
            parsedTerrain,
            parsedStructures,
            parsedItems,
            parsedSchedule!,
            parsedPublic,
            parsedBattleScoring,
            parsedRanking,
            parsedSpecialRules,
            parsedPrivate,
            parsedForceStatuses,
            parsedSplitForce,
            splitForceSupplyPenaltyIsPercent ?? HuntInEstaliaDefaults.SplitForceSupplyPenaltyIsPercent,
            ParseBattleReportRules(
                alwaysAskGeneralKill,
                alwaysAskSupplyLineDestroyed,
                generalKillCampaignPoints,
                supplyLineDestroyedCampaignPoints,
                collected),
            [.. missionIndex.ById.Values]);
        errors = collected;
        return true;
    }

    private static string? ParseRequiredName(
        string? raw,
        string field,
        string label,
        int minLength,
        int maxLength,
        List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            errors.Add(new DomainError($"{field}.invalid", $"{label} is not filled in.", field));
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length < minLength)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} is too short (minimum {minLength} characters).",
                field));
            return null;
        }

        if (trimmed.Length > maxLength)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} is too long (maximum {maxLength} characters).",
                field));
            return null;
        }

        if (ProhibitedLanguage.ContainsProhibitedTerm(trimmed))
        {
            errors.Add(ProhibitedLanguage.ErrorFor(field, label));
            return null;
        }

        return trimmed;
    }

    private static string? ParseOptionalDescription(string? raw, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > DescriptionMaxLength)
        {
            errors.Add(new DomainError(
                "description.invalid",
                $"Description is too long (maximum {DescriptionMaxLength} characters).",
                "description"));
            return null;
        }

        return trimmed;
    }

    private static string? ParseJoinPassword(string? raw, bool required, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (required)
            {
                errors.Add(new DomainError(
                    "joinPassword.invalid",
                    "Private campaigns require a join password.",
                    "joinPassword"));
            }

            return null;
        }

        if (raw.Length < JoinPasswordMinLength)
        {
            errors.Add(new DomainError(
                "joinPassword.invalid",
                $"Join password is too short (minimum {JoinPasswordMinLength} characters).",
                "joinPassword"));
            return null;
        }

        if (raw.Length > JoinPasswordMaxLength)
        {
            errors.Add(new DomainError(
                "joinPassword.invalid",
                $"Join password is too long (maximum {JoinPasswordMaxLength} characters).",
                "joinPassword"));
            return null;
        }

        return raw;
    }

    private static List<AllyGroupSetup> ParseAllyGroups(
        IReadOnlyList<AllyGroupInput>? allyGroups,
        HashSet<Guid> usedIds,
        List<DomainError> errors)
    {
        var parsed = new List<AllyGroupSetup>();
        if (allyGroups is null || allyGroups.Count == 0)
        {
            return parsed;
        }

        if (allyGroups.Count > MaxAllyGroupCount)
        {
            errors.Add(new DomainError(
                "allyGroups.invalid",
                $"At most {MaxAllyGroupCount} ally groups are allowed.",
                "allyGroups"));
            return parsed;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < allyGroups.Count; index++)
        {
            var field = $"allyGroups[{index}].name";
            var name = ParseRequiredName(
                allyGroups[index].Name,
                field,
                $"Ally group {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            var color = ParseUniqueColor(
                allyGroups[index].Color,
                usedColors,
                $"allyGroups[{index}].color",
                $"Ally group {index + 1} color",
                assignDefault: true,
                errors);
            if (name is null || color is null)
            {
                continue;
            }

            if (!seen.Add(name))
            {
                errors.Add(new DomainError(
                    "allyGroups.duplicate",
                    "Ally group names must be unique.",
                    field));
                continue;
            }

            parsed.Add(new AllyGroupSetup(
                ResolveId(allyGroups[index].Id, usedIds, $"allyGroups[{index}].id", errors),
                name,
                color));
        }

        return parsed;
    }

    private static List<FactionSetup> ParseFactions(
        IReadOnlyList<FactionInput>? factions,
        IReadOnlyList<AllyGroupSetup> allyGroups,
        HashSet<Guid> usedIds,
        HashSet<Guid> knownSpecialRuleIds,
        List<DomainError> errors)
    {
        var parsed = new List<FactionSetup>();
        if (factions is null || factions.Count < MinFactionCount)
        {
            errors.Add(new DomainError(
                "factions.invalid",
                $"At least {MinFactionCount} factions are required.",
                "factions"));
            return parsed;
        }

        if (factions.Count > MaxFactionCount)
        {
            errors.Add(new DomainError(
                "factions.invalid",
                $"At most {MaxFactionCount} factions are allowed.",
                "factions"));
            return parsed;
        }

        var groupNames = allyGroups.Select(group => group.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var groupsById = allyGroups.ToDictionary(static group => group.Id);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < factions.Count; index++)
        {
            var faction = factions[index];
            var nameField = $"factions[{index}].name";
            var name = ParseRequiredName(
                faction.Name,
                nameField,
                $"Faction {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);

            var color = ParseUniqueColor(
                faction.Color,
                usedColors,
                $"factions[{index}].color",
                $"Faction {index + 1} color",
                assignDefault: true,
                errors);

            var subfactions = ParseSubfactions(faction.Subfactions, index, errors);
            var allyGroupName = ResolveAllyGroupName(faction, allyGroups, groupsById, groupNames, index, errors);

            if (name is null || color is null)
            {
                continue;
            }

            if (!seen.Add(name))
            {
                errors.Add(new DomainError(
                    "factions.duplicate",
                    "Faction names must be unique.",
                    nameField));
                continue;
            }

            var canonicalGroup = allyGroupName is null
                ? null
                : allyGroups.First(group => string.Equals(group.Name, allyGroupName, StringComparison.OrdinalIgnoreCase)).Name;
            if (faction.RequiresSubfaction && subfactions.Count == 0)
            {
                errors.Add(new DomainError(
                    "factions.subfaction.required",
                    $"Faction {index + 1} requires players to pick a subfaction, so at least one subfaction must be listed.",
                    $"factions[{index}].subfactions"));
            }

            parsed.Add(new FactionSetup(
                ResolveId(faction.Id, usedIds, $"factions[{index}].id", errors),
                name,
                color,
                subfactions,
                canonicalGroup,
                faction.RequiresSubfaction,
                faction.ClearFlagImage,
                faction.TintFlagImage,
                ParseAssignedSpecialRuleIds(
                    faction.SpecialRuleIds,
                    knownSpecialRuleIds,
                    $"factions[{index}].specialRuleIds",
                    $"Faction {index + 1}",
                    errors),
                ParseSubfactionSpecialRules(
                    faction.SubfactionSpecialRules,
                    subfactions,
                    knownSpecialRuleIds,
                    index,
                    errors)));
        }

        return parsed;
    }

    private static List<string> ParseSubfactions(
        IReadOnlyList<string>? subfactions,
        int factionIndex,
        List<DomainError> errors)
    {
        if (subfactions is null || subfactions.Count == 0)
        {
            return [];
        }

        var supplied = subfactions
            .Select(static value => value?.Trim() ?? string.Empty)
            .Where(static value => value.Length > 0)
            .ToArray();
        if (supplied.Length > MaxSubfactionsPerFaction)
        {
            errors.Add(new DomainError(
                "factions.subfactions.invalid",
                $"Faction {factionIndex + 1} can have at most {MaxSubfactionsPerFaction} subfactions.",
                $"factions[{factionIndex}].subfactions"));
            return [];
        }

        var parsed = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Length; index++)
        {
            var field = $"factions[{factionIndex}].subfactions[{index}]";
            var name = ParseRequiredName(
                supplied[index],
                field,
                $"Faction {factionIndex + 1} subfaction {index + 1}",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            if (name is null)
            {
                continue;
            }

            if (!seen.Add(name))
            {
                errors.Add(new DomainError(
                    "factions.subfactions.duplicate",
                    $"Faction {factionIndex + 1} subfaction names must be unique.",
                    field));
                continue;
            }

            parsed.Add(name);
        }

        return parsed;
    }

    private static List<TerrainTypeSetup> ParseTerrainTypes(
        IReadOnlyList<TerrainTypeInput>? terrainTypes,
        HashSet<Guid> usedIds,
        MissionIndex missions,
        List<DomainError> errors)
    {
        var supplied = terrainTypes is null || terrainTypes.Count == 0
            ? CampaignCatalogDefaults.TerrainTypes()
            : terrainTypes;
        var parsed = new List<TerrainTypeSetup>();
        if (supplied.Count < MinTerrainTypeCount)
        {
            errors.Add(new DomainError(
                "terrainTypes.invalid",
                $"At least {MinTerrainTypeCount} terrain type is required.",
                "terrainTypes"));
            return parsed;
        }

        if (supplied.Count > MaxTerrainTypeCount)
        {
            errors.Add(new DomainError(
                "terrainTypes.invalid",
                $"At most {MaxTerrainTypeCount} terrain types are allowed.",
                "terrainTypes"));
            return parsed;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Count; index++)
        {
            var input = supplied[index];
            var name = ParseRequiredName(
                input.Name,
                $"terrainTypes[{index}].name",
                $"Terrain type {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            var color = ParseUniqueColor(
                input.Color,
                usedColors,
                $"terrainTypes[{index}].color",
                $"Terrain type {index + 1} color",
                assignDefault: false,
                errors);
            var missionsForType = ParseMissions(
                input.Missions,
                usedIds,
                missions,
                $"terrainTypes[{index}].missions",
                $"Terrain type {index + 1}",
                requireAtLeastOne: true,
                errors);
            if (name is null || color is null)
            {
                continue;
            }

            if (!seenNames.Add(name))
            {
                errors.Add(new DomainError(
                    "terrainTypes.duplicate",
                    "Terrain type names must be unique.",
                    $"terrainTypes[{index}].name"));
                continue;
            }

            parsed.Add(new TerrainTypeSetup(
                ResolveId(input.Id, usedIds, $"terrainTypes[{index}].id", errors),
                name,
                color,
                missionsForType,
                input.IsWaterFeature ?? TerrainCatalog.IsWaterFeature(name),
                ParseSupplyPoints(
                    input.SupplyPoints,
                    $"terrainTypes[{index}].supplyPoints",
                    $"Terrain type {index + 1} supply points",
                    errors)));
        }

        return parsed;
    }

    private static List<StructureTypeSetup> ParseStructureTypes(
        IReadOnlyList<StructureTypeInput>? structureTypes,
        HashSet<Guid> usedIds,
        MissionIndex missions,
        List<DomainError> errors)
    {
        var supplied = structureTypes is null
            ? CampaignCatalogDefaults.StructureTypes()
            : structureTypes;
        var parsed = new List<StructureTypeSetup>();
        if (supplied.Count > MaxStructureTypeCount)
        {
            errors.Add(new DomainError(
                "structureTypes.invalid",
                $"At most {MaxStructureTypeCount} structure types are allowed.",
                "structureTypes"));
            return parsed;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Count; index++)
        {
            var input = supplied[index];
            var name = ParseRequiredName(
                input.Name,
                $"structureTypes[{index}].name",
                $"Structure {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            var builtin = CampaignCatalogDefaults.CanonicalBuiltinSymbol(input.BuiltinSymbol);
            if (!string.IsNullOrWhiteSpace(input.BuiltinSymbol) && builtin is null)
            {
                errors.Add(new DomainError(
                    "structureTypes.symbol.invalid",
                    $"Structure {index + 1} logo is not a recognized built-in symbol.",
                    $"structureTypes[{index}].builtinSymbol"));
            }

            var missionsForType = ParseMissions(
                input.Missions,
                usedIds,
                missions,
                $"structureTypes[{index}].missions",
                $"Structure {index + 1}",
                requireAtLeastOne: false,
                errors);
            if (name is null)
            {
                continue;
            }

            if (!seenNames.Add(name))
            {
                errors.Add(new DomainError(
                    "structureTypes.duplicate",
                    "Structure names must be unique.",
                    $"structureTypes[{index}].name"));
                continue;
            }

            var (IsBuildable, IsPillageable, IsDestructible) = StructureCatalog.DefaultFlags(name, builtin);
            parsed.Add(new StructureTypeSetup(
                ResolveId(input.Id, usedIds, $"structureTypes[{index}].id", errors),
                name,
                builtin,
                input.ClearImage,
                input.ClearPillagedImage,
                input.IsBuildable ?? IsBuildable,
                input.IsPillageable ?? IsPillageable,
                input.IsDestructible ?? IsDestructible,
                missionsForType,
                ParseCampaignPoints(
                    input.CampaignPoints,
                    $"structureTypes[{index}].campaignPoints",
                    $"Structure {index + 1} campaign points",
                    errors),
                ParseSupplyPoints(
                    input.SupplyPoints,
                    $"structureTypes[{index}].supplyPoints",
                    $"Structure {index + 1} supply points",
                    errors),
                ParseSupplyPoints(
                    input.PillageSupplyPoints,
                    $"structureTypes[{index}].pillageSupplyPoints",
                    $"Structure {index + 1} pillage supply points",
                    errors),
                ParseSupplyPoints(
                    input.DestroySupplyPoints,
                    $"structureTypes[{index}].destroySupplyPoints",
                    $"Structure {index + 1} destroy supply points",
                    errors)));
        }

        return parsed;
    }

    private static List<ItemObjectiveTypeSetup> ParseItemObjectiveTypes(
        IReadOnlyList<ItemObjectiveTypeInput>? itemObjectiveTypes,
        HashSet<Guid> usedIds,
        HashSet<Guid> knownSpecialRuleIds,
        HashSet<Guid> knownPrivateObjectiveIds,
        List<DomainError> errors)
    {
        var supplied = itemObjectiveTypes ?? [];
        var parsed = new List<ItemObjectiveTypeSetup>();
        if (supplied.Count > MaxItemObjectiveTypeCount)
        {
            errors.Add(new DomainError(
                "itemObjectiveTypes.invalid",
                $"At most {MaxItemObjectiveTypeCount} item objectives are allowed.",
                "itemObjectiveTypes"));
            return parsed;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Count; index++)
        {
            var input = supplied[index];
            var name = ParseRequiredName(
                input.Name,
                $"itemObjectiveTypes[{index}].name",
                $"Item objective {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            if (name is null)
            {
                continue;
            }

            if (!seenNames.Add(name))
            {
                errors.Add(new DomainError(
                    "itemObjectiveTypes.duplicate",
                    "Item objective names must be unique.",
                    $"itemObjectiveTypes[{index}].name"));
                continue;
            }

            if (!TryParsePlacement(input.Placement, index, errors, out var placement))
            {
                continue;
            }

            var builtin = ItemObjectiveCatalog.CanonicalSymbol(input.BuiltinSymbol) ?? nameof(ItemObjectiveSymbol.Crown);
            if (!string.IsNullOrWhiteSpace(input.BuiltinSymbol) && ItemObjectiveCatalog.CanonicalSymbol(input.BuiltinSymbol) is null)
            {
                errors.Add(new DomainError(
                    "itemObjectiveTypes.symbol.invalid",
                    $"Item objective {index + 1} logo is not a recognized built-in symbol.",
                    $"itemObjectiveTypes[{index}].builtinSymbol"));
            }

            var color = ParseOptionalItemColor(input.Color, $"itemObjectiveTypes[{index}].color", $"Item objective {index + 1} color", errors);
            var flavor = ParseOptionalCatalogText(
                input.FlavorText,
                $"itemObjectiveTypes[{index}].flavorText",
                $"Item objective {index + 1} flavor text",
                errors);
            parsed.Add(new ItemObjectiveTypeSetup(
                ResolveId(input.Id, usedIds, $"itemObjectiveTypes[{index}].id", errors),
                name,
                input.IsHiddenUntilFound ?? true,
                placement,
                input.AllowOnSpawn ?? false,
                builtin,
                color,
                input.ClearImage,
                ParseCampaignPoints(
                    input.CampaignPoints,
                    $"itemObjectiveTypes[{index}].campaignPoints",
                    $"Item objective {index + 1} campaign points",
                    errors),
                flavor,
                ParseItemObjectiveChoices(
                    input.Choices,
                    usedIds,
                    knownPrivateObjectiveIds,
                    index,
                    errors),
                ParseAssignedSpecialRuleIds(
                    input.SpecialRuleIds,
                    knownSpecialRuleIds,
                    $"itemObjectiveTypes[{index}].specialRuleIds",
                    $"Item objective {index + 1}",
                    errors)));
        }

        ValidateItemReplacementReferences(parsed, errors);
        return parsed;
    }

    private static List<PublicObjectiveTypeSetup> ParsePublicObjectiveTypes(
        IReadOnlyList<PublicObjectiveTypeInput>? publicObjectiveTypes,
        HashSet<Guid> usedIds,
        List<DomainError> errors)
    {
        var supplied = publicObjectiveTypes ?? [];
        var parsed = new List<PublicObjectiveTypeSetup>();
        if (supplied.Count > MaxPublicObjectiveTypeCount)
        {
            errors.Add(new DomainError(
                "publicObjectiveTypes.invalid",
                $"At most {MaxPublicObjectiveTypeCount} public objectives are allowed.",
                "publicObjectiveTypes"));
            return parsed;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Count; index++)
        {
            var input = supplied[index];
            var name = ParseRequiredName(
                input.Name,
                $"publicObjectiveTypes[{index}].name",
                $"Public objective {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            if (name is null)
            {
                continue;
            }

            if (!seenNames.Add(name))
            {
                errors.Add(new DomainError(
                    "publicObjectiveTypes.duplicate",
                    "Public objective names must be unique.",
                    $"publicObjectiveTypes[{index}].name"));
                continue;
            }

            string? description = null;
            if (!string.IsNullOrWhiteSpace(input.Description))
            {
                var trimmed = input.Description.Trim();
                if (trimmed.Length > DescriptionMaxLength)
                {
                    errors.Add(new DomainError(
                        $"publicObjectiveTypes[{index}].description.invalid",
                        $"Public objective {index + 1} description is too long (maximum {DescriptionMaxLength} characters).",
                        $"publicObjectiveTypes[{index}].description"));
                }
                else
                {
                    description = trimmed;
                }
            }

            parsed.Add(new PublicObjectiveTypeSetup(
                ResolveId(input.Id, usedIds, $"publicObjectiveTypes[{index}].id", errors),
                name,
                description,
                ParseCampaignPoints(
                    input.CampaignPoints,
                    $"publicObjectiveTypes[{index}].campaignPoints",
                    $"Public objective {index + 1} campaign points",
                    errors)));
        }

        return parsed;
    }

    private static BattleScoringSetup ParseBattleScoring(
        int? pointsPerWin,
        int? pointsPerDraw,
        bool? useDifferential,
        decimal? multiplier,
        int? minimum,
        int? maximum,
        bool? allowNegative,
        List<DomainError> errors)
    {
        var parsedWin = ParseCampaignPoints(
            pointsPerWin,
            "pointsPerBattleWon",
            "Points per battle won",
            errors,
            BattleScoringSetup.DefaultPointsPerWin);
        var parsedDraw = ParseCampaignPoints(
            pointsPerDraw,
            "pointsPerBattleDraw",
            "Points per battle draw",
            errors,
            BattleScoringSetup.DefaultPointsPerDraw);
        var parsedMultiplier = ParseDifferentialMultiplier(multiplier, errors);
        var parsedMinimum = ParseDifferentialBound(
            minimum,
            "differentialMinimum",
            "Differential minimum",
            errors,
            0);
        var parsedMaximum = ParseDifferentialBound(
            maximum,
            "differentialMaximum",
            "Differential maximum",
            errors,
            10);
        if (parsedMaximum < parsedMinimum)
        {
            errors.Add(new DomainError(
                "differentialMaximum.invalid",
                "The differential maximum cannot be below the minimum.",
                "differentialMaximum"));
            parsedMaximum = parsedMinimum;
        }

        return new BattleScoringSetup(
            parsedWin,
            parsedDraw,
            useDifferential ?? true,
            parsedMultiplier,
            parsedMinimum,
            parsedMaximum,
            allowNegative ?? false);
    }

    private static string? ResolveAllyGroupName(
        FactionInput faction,
        IReadOnlyList<AllyGroupSetup> allyGroups,
        Dictionary<Guid, AllyGroupSetup> groupsById,
        HashSet<string> groupNames,
        int index,
        List<DomainError> errors)
    {
        if (faction.AllyGroupId is { } groupId && groupId != Guid.Empty)
        {
            if (groupsById.TryGetValue(groupId, out var byId))
            {
                return byId.Name;
            }

            errors.Add(new DomainError(
                "factions.ally_group.unknown",
                $"Faction {index + 1} references an ally group that was not created.",
                $"factions[{index}].allyGroupId"));
            return null;
        }

        var allyGroupName = string.IsNullOrWhiteSpace(faction.AllyGroupName)
            ? null
            : faction.AllyGroupName.Trim();
        if (allyGroupName is not null && !groupNames.Contains(allyGroupName))
        {
            errors.Add(new DomainError(
                "factions.ally_group.unknown",
                $"Faction {index + 1} references an ally group that was not created.",
                $"factions[{index}].allyGroupName"));
            return null;
        }

        return allyGroupName is null
            ? null
            : allyGroups.First(group => string.Equals(group.Name, allyGroupName, StringComparison.OrdinalIgnoreCase)).Name;
    }

    private static GeneralPublicObjectivePoints ParseRankingObjectives(
        int? mostTerritories,
        int? longestChain,
        int? mostBattlesWon,
        int? mostStructurePoints,
        int? pointsPerTerritory,
        int? alliedRelicControlPoints,
        List<DomainError> errors)
    {
        return new GeneralPublicObjectivePoints(
            ParseCampaignPoints(
                mostTerritories,
                "mostTerritoriesCampaignPoints",
                "Most territories campaign points",
                errors),
            ParseCampaignPoints(
                longestChain,
                "longestTerritoryChainCampaignPoints",
                "Longest territory chain campaign points",
                errors),
            ParseCampaignPoints(
                mostBattlesWon,
                "mostBattlesWonCampaignPoints",
                "Most battles won campaign points",
                errors),
            ParseCampaignPoints(
                mostStructurePoints,
                "mostStructurePointsCampaignPoints",
                "Most structure points campaign points",
                errors),
            ParseCampaignPoints(
                pointsPerTerritory,
                "pointsPerTerritoryCampaignPoints",
                "Points per territory",
                errors),
            ParseCampaignPoints(
                alliedRelicControlPoints,
                "alliedRelicControlCampaignPoints",
                "Allied relic control campaign points",
                errors));
    }

    private static decimal ParseDifferentialMultiplier(decimal? value, List<DomainError> errors)
    {
        if (value is null)
        {
            return BattleScoringSetup.DefaultMultiplier;
        }

        if (value < BattleScoringSetup.MinMultiplier || value > BattleScoringSetup.MaxMultiplier)
        {
            errors.Add(new DomainError(
                "differentialMultiplier.invalid",
                $"The differential multiplier must be between {BattleScoringSetup.MinMultiplier} and {BattleScoringSetup.MaxMultiplier}.",
                "differentialMultiplier"));
            return BattleScoringSetup.DefaultMultiplier;
        }

        return value.Value;
    }

    private static int ParseDifferentialBound(int? value, string field, string label, List<DomainError> errors, int fallback)
    {
        if (value is null)
        {
            return fallback;
        }

        if (value < -MaxCampaignPoints || value > MaxCampaignPoints)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} must be between {-MaxCampaignPoints} and {MaxCampaignPoints}.",
                field));
            return fallback;
        }

        return value.Value;
    }

    private static int ParseRoundArmyPoints(
        int? value,
        string field,
        string label,
        List<DomainError> errors,
        int fallback)
    {
        if (value is null)
        {
            return fallback;
        }

        if (value < MinRoundArmyPoints || value > MaxRoundArmyPoints)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} must be between {MinRoundArmyPoints} and {MaxRoundArmyPoints}.",
                field));
            return fallback;
        }

        return value.Value;
    }

    private static int ParseCampaignPoints(
        int? value,
        string field,
        string label,
        List<DomainError> errors,
        int fallback = 0)
    {
        if (value is null)
        {
            return fallback;
        }

        if (value < 0 || value > MaxCampaignPoints)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} must be between 0 and {MaxCampaignPoints}.",
                field));
            return fallback;
        }

        return value.Value;
    }

    private static int ParseSupplyPoints(int? value, string field, string label, List<DomainError> errors)
    {
        return ParseCampaignPoints(value, field, label, errors, HuntInEstaliaDefaults.SupplyPoints);
    }

    private static int ParseSplitForcePenalty(int? value, List<DomainError> errors)
    {
        if (value is null)
        {
            return HuntInEstaliaDefaults.SplitForceSupplyPenaltyValue;
        }

        if (value < 0 || value > 100)
        {
            errors.Add(new DomainError(
                "splitForceSupplyPenaltyPercent.invalid",
                "The split-force supply penalty must be between 0 and 100.",
                "splitForceSupplyPenaltyPercent"));
            return HuntInEstaliaDefaults.SplitForceSupplyPenaltyValue;
        }

        return value.Value;
    }

    private static BattleReportRulesSetup ParseBattleReportRules(
        bool? alwaysAskGeneralKill,
        bool? alwaysAskSupplyLineDestroyed,
        int? generalKillCampaignPoints,
        int? supplyLineDestroyedCampaignPoints,
        List<DomainError> errors)
    {
        return new BattleReportRulesSetup(
            alwaysAskGeneralKill ?? HuntInEstaliaDefaults.AlwaysAskGeneralKill,
            alwaysAskSupplyLineDestroyed ?? HuntInEstaliaDefaults.AlwaysAskSupplyLineDestroyed,
            ParseCampaignPoints(
                generalKillCampaignPoints,
                "generalKillCampaignPoints",
                "General-kill campaign points",
                errors,
                HuntInEstaliaDefaults.GeneralKillCampaignPoints),
            ParseCampaignPoints(
                supplyLineDestroyedCampaignPoints,
                "supplyLineDestroyedCampaignPoints",
                "Supply-line campaign points",
                errors,
                HuntInEstaliaDefaults.SupplyLineDestroyedCampaignPoints));
    }

    private static IReadOnlyList<RoundArmyEscalationSetup> ParseArmyEscalations(
        IReadOnlyList<RoundArmyEscalationInput>? inputs,
        int roundCount,
        List<DomainError> errors)
    {
        var defaults = ArmyEscalationDefaults.ForRoundCount(Math.Max(roundCount, 1));
        if (inputs is null || inputs.Count == 0)
        {
            return defaults;
        }

        if (inputs.Count > MaxRoundCount)
        {
            errors.Add(new DomainError(
                "roundEscalations.invalid",
                $"At most {MaxRoundCount} round army rows are allowed.",
                "roundEscalations"));
            return defaults;
        }

        var byRound = new Dictionary<int, RoundArmyEscalationSetup>();
        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            var roundNumber = input.RoundNumber ?? index + 1;
            if (roundNumber < 1 || roundNumber > MaxRoundCount)
            {
                errors.Add(new DomainError(
                    "roundEscalations.round.invalid",
                    $"Round army row {index + 1} must use a round between 1 and {MaxRoundCount}.",
                    $"roundEscalations[{index}].roundNumber"));
                continue;
            }

            if (!byRound.TryAdd(
                    roundNumber,
                    new RoundArmyEscalationSetup(
                        roundNumber,
                        ParseRoundArmyPoints(
                            input.MaxArmyPoints,
                            $"roundEscalations[{index}].maxArmyPoints",
                            $"Round {roundNumber} max army points",
                            errors,
                            defaults.FirstOrDefault(row => row.RoundNumber == roundNumber)?.MaxArmyPoints
                                ?? defaults[^1].MaxArmyPoints),
                        ParseSupplyPoints(
                            input.FreeSupplyPoints,
                            $"roundEscalations[{index}].freeSupplyPoints",
                            $"Round {roundNumber} free supply points",
                            errors),
                        ParseCampaignPoints(
                            input.FreeCharacterCount,
                            $"roundEscalations[{index}].freeCharacterCount",
                            $"Round {roundNumber} free characters",
                            errors,
                            defaults.FirstOrDefault(row => row.RoundNumber == roundNumber)?.FreeCharacterCount
                                ?? defaults[^1].FreeCharacterCount))))
            {
                errors.Add(new DomainError(
                    "roundEscalations.duplicate",
                    $"Round {roundNumber} already has an army-escalation row.",
                    $"roundEscalations[{index}].roundNumber"));
            }
        }

        var rows = new RoundArmyEscalationSetup[Math.Max(roundCount, 1)];
        for (var round = 1; round <= rows.Length; round++)
        {
            if (byRound.TryGetValue(round, out var row))
            {
                rows[round - 1] = row;
                continue;
            }

            var fallback = defaults.FirstOrDefault(item => item.RoundNumber == round) ?? defaults[^1];
            rows[round - 1] = new RoundArmyEscalationSetup(
                round,
                fallback.MaxArmyPoints,
                fallback.FreeSupplyPoints,
                fallback.FreeCharacterCount);
        }

        return rows;
    }

    private static List<MissionResultQuestionSetup> ParseMissionResultQuestions(
        IReadOnlyList<MissionResultQuestionInput>? inputs,
        HashSet<Guid> usedIds,
        string field,
        string ownerLabel,
        List<DomainError> errors)
    {
        var supplied = inputs?
            .Where(static question => !string.IsNullOrWhiteSpace(question.Prompt) || question.Id is not null)
            .ToArray() ?? [];
        if (supplied.Length == 0)
        {
            return [];
        }

        if (supplied.Length > MaxMissionResultQuestionCount)
        {
            errors.Add(new DomainError(
                "missions.questions.invalid",
                $"{ownerLabel} can have at most {MaxMissionResultQuestionCount} result questions.",
                field));
            return [];
        }

        var parsed = new List<MissionResultQuestionSetup>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Length; index++)
        {
            var input = supplied[index];
            var prompt = ParseRequiredName(
                input.Prompt,
                $"{field}[{index}].prompt",
                $"{ownerLabel} question {index + 1}",
                minLength: 1,
                CatalogTextMaxLength,
                errors);
            if (prompt is null)
            {
                continue;
            }

            if (!seen.Add(prompt))
            {
                errors.Add(new DomainError(
                    "missions.questions.duplicate",
                    $"{ownerLabel} question names must be unique.",
                    $"{field}[{index}].prompt"));
                continue;
            }

            if (!TryParseQuestionKind(input.Kind, index, field, ownerLabel, errors, out var kind))
            {
                continue;
            }

            parsed.Add(new MissionResultQuestionSetup(
                ResolveId(input.Id, usedIds, $"{field}[{index}].id", errors),
                prompt,
                kind,
                ParseCampaignPoints(
                    input.BattlePoints,
                    $"{field}[{index}].battlePoints",
                    $"{ownerLabel} question {index + 1} battle points",
                    errors),
                ParseCampaignPoints(
                    input.CampaignPoints,
                    $"{field}[{index}].campaignPoints",
                    $"{ownerLabel} question {index + 1} campaign points",
                    errors)));
        }

        return parsed;
    }

    private static bool TryParseQuestionKind(
        string? raw,
        int index,
        string field,
        string ownerLabel,
        List<DomainError> errors,
        out MissionResultQuestionKind kind)
    {
        kind = MissionResultQuestionKind.Boolean;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (Enum.TryParse(raw, ignoreCase: true, out kind))
        {
            return true;
        }

        kind = MissionResultQuestionKind.Boolean;
        errors.Add(new DomainError(
            "missions.questions.kind.invalid",
            $"{ownerLabel} question {index + 1} must be Boolean or BattlePoints.",
            $"{field}[{index}].kind"));
        return false;
    }

    private static string ParseOptionalItemColor(string? raw, string field, string label, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ItemObjectiveCatalog.DefaultColor;
        }

        if (!HexColor.TryNormalize(raw, out var color) || color is null)
        {
            errors.Add(new DomainError($"{field}.invalid", $"{label} must be a six-digit hex value.", field));
            return ItemObjectiveCatalog.DefaultColor;
        }

        return color;
    }

    private static bool TryParsePlacement(
        string? value,
        int index,
        List<DomainError> errors,
        out ItemObjectivePlacementKind placement)
    {
        placement = ItemObjectivePlacementKind.Random;
        if (string.IsNullOrWhiteSpace(value)
            || value.Equals(nameof(ItemObjectivePlacementKind.Random), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Equals(nameof(ItemObjectivePlacementKind.Placed), StringComparison.OrdinalIgnoreCase))
        {
            placement = ItemObjectivePlacementKind.Placed;
            return true;
        }

        errors.Add(new DomainError(
            "itemObjectiveTypes.placement.invalid",
            $"Item objective {index + 1} placement must be Random or Placed.",
            $"itemObjectiveTypes[{index}].placement"));
        return false;
    }

    private static List<MissionSetup> ParseMissions(
        IReadOnlyList<MissionInput>? missions,
        HashSet<Guid> usedIds,
        MissionIndex index,
        string field,
        string ownerLabel,
        bool requireAtLeastOne,
        List<DomainError> errors,
        int? maxCount = null)
    {
        var supplied = missions?
            .Where(static mission =>
                !string.IsNullOrWhiteSpace(mission.Name) || !string.IsNullOrWhiteSpace(mission.Url) || mission.Id is not null)
            .ToArray() ?? [];
        if (requireAtLeastOne && supplied.Length == 0)
        {
            errors.Add(new DomainError(
                "missions.invalid",
                $"{ownerLabel} requires at least one mission.",
                field));
            return [];
        }

        var limit = maxCount ?? MaxMissionsPerCatalogItem;
        if (supplied.Length > limit)
        {
            errors.Add(new DomainError(
                "missions.invalid",
                $"{ownerLabel} can have at most {limit} missions.",
                field));
            return [];
        }

        var parsed = new List<MissionSetup>();
        var seenOnOwner = new HashSet<Guid>();
        for (var missionIndex = 0; missionIndex < supplied.Length; missionIndex++)
        {
            var mission = supplied[missionIndex];
            var nameField = $"{field}[{missionIndex}].name";
            var name = ParseRequiredName(
                mission.Name,
                nameField,
                $"{ownerLabel} mission {missionIndex + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            var url = ParseOptionalHttpUrl(
                mission.Url,
                $"{field}[{missionIndex}].url",
                $"{ownerLabel} mission {missionIndex + 1} URL",
                errors);
            if (name is null)
            {
                continue;
            }

            var reused = TryReuseMission(
                mission.Id,
                name,
                index,
                nameField,
                errors);
            if (reused is not null)
            {
                if (!seenOnOwner.Add(reused.Id))
                {
                    errors.Add(new DomainError(
                        "missions.duplicate",
                        $"{ownerLabel} already includes that mission.",
                        nameField));
                    continue;
                }

                parsed.Add(reused);
                continue;
            }

            if (errors.Exists(error => error.Field == nameField && error.Code == "missions.duplicate"))
            {
                continue;
            }

            var id = ResolveId(mission.Id, usedIds, $"{field}[{missionIndex}].id", errors);
            var questions = ParseMissionResultQuestions(
                mission.ResultQuestions,
                usedIds,
                $"{field}[{missionIndex}].resultQuestions",
                $"{ownerLabel} mission {missionIndex + 1}",
                errors);
            var created = new MissionSetup(
                id,
                name,
                url,
                mission.ClearFile,
                questions,
                mission.IsAttackerDefender,
                mission.HasArmyPointsAdvantage,
                ParseAdvantageSide(
                    mission.ArmyPointsAdvantageSide,
                    $"{field}[{missionIndex}].armyPointsAdvantageSide",
                    errors),
                mission.ArmyPointsAdvantageIsPercent,
                mission.ArmyPointsAdvantageAmount,
                mission.HasSupplyPointsAdvantage,
                ParseAdvantageSide(
                    mission.SupplyPointsAdvantageSide,
                    $"{field}[{missionIndex}].supplyPointsAdvantageSide",
                    errors),
                mission.SupplyPointsAdvantageAmount);
            index.ById[id] = created;
            index.Names[name] = id;
            seenOnOwner.Add(id);
            parsed.Add(created);
        }

        if (requireAtLeastOne && parsed.Count == 0 && supplied.Length > 0)
        {
            errors.Add(new DomainError(
                "missions.invalid",
                $"{ownerLabel} requires at least one mission.",
                field));
        }

        return parsed;
    }

    private static MissionAdvantageSide ParseAdvantageSide(string? raw, string field, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw)
            || string.Equals(raw, nameof(MissionAdvantageSide.Defender), StringComparison.OrdinalIgnoreCase))
        {
            return MissionAdvantageSide.Defender;
        }

        if (string.Equals(raw, nameof(MissionAdvantageSide.Attacker), StringComparison.OrdinalIgnoreCase))
        {
            return MissionAdvantageSide.Attacker;
        }

        errors.Add(new DomainError(
            "missions.advantageSide.invalid",
            "Advantage side must be Attacker or Defender.",
            field));
        return MissionAdvantageSide.Defender;
    }

    private static MissionSetup? TryReuseMission(
        Guid? suppliedId,
        string name,
        MissionIndex index,
        string nameField,
        List<DomainError> errors)
    {
        if (suppliedId is { } id && id != Guid.Empty && index.ById.TryGetValue(id, out var byId))
        {
            if (!string.Equals(byId.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new DomainError(
                    "missions.duplicate",
                    "Mission names must be unique.",
                    nameField));
                return null;
            }

            return byId;
        }

        if (index.Names.ContainsKey(name))
        {
            errors.Add(new DomainError(
                "missions.duplicate",
                "Mission names must be unique.",
                nameField));
        }

        return null;
    }

    private static string? ParseUniqueColor(
        string? raw,
        HashSet<string> usedColors,
        string field,
        string label,
        bool assignDefault,
        List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (!assignDefault)
            {
                errors.Add(new DomainError($"{field}.invalid", $"{label} is not filled in.", field));
                return null;
            }

            foreach (var candidate in DefaultFactionPalette)
            {
                if (usedColors.Add(candidate))
                {
                    return candidate;
                }
            }

            errors.Add(new DomainError($"{field}.duplicate", $"{label} must be unique.", field));
            return null;
        }

        if (!HexColor.TryNormalize(raw, out var color) || color is null)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} must be a six-digit hex value.",
                field));
            return null;
        }

        if (!usedColors.Add(color))
        {
            errors.Add(new DomainError($"{field}.duplicate", $"{label} must be unique.", field));
            return null;
        }

        return color;
    }

    private static Guid ResolveId(Guid? id, HashSet<Guid> usedIds, string field, List<DomainError> errors)
    {
        var value = id is { } supplied && supplied != Guid.Empty ? supplied : Guid.NewGuid();
        if (usedIds.Add(value))
        {
            return value;
        }

        errors.Add(new DomainError("catalog.id.duplicate", "Catalog identifiers must be unique.", field));
        var replacement = Guid.NewGuid();
        while (!usedIds.Add(replacement))
        {
            replacement = Guid.NewGuid();
        }

        return replacement;
    }

    private static string? ParseOptionalHttpUrl(string? raw, string field, string label, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > LinkUrlMaxLength)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} is too long (maximum {LinkUrlMaxLength} characters).",
                field));
            return null;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} must be an http or https address.",
                field));
            return null;
        }

        return uri.AbsoluteUri;
    }

    private static readonly string[] DefaultFactionPalette =
    [
        "#2563EB", "#DC2626", "#16A34A", "#CA8A04", "#7C3AED", "#EA580C", "#0891B2", "#BE185D",
        "#4B5563", "#65A30D", "#C026D3", "#0F766E", "#1D4ED8", "#B45309", "#15803D", "#6D28D9",
        "#9F1239", "#0369A1", "#A16207", "#334155", "#DB2777", "#047857", "#7C2D12", "#4338CA",
        "#0E7490", "#854D0E", "#166534", "#6B21A8", "#9A3412", "#1E3A8A", "#115E59", "#831843",
        "#3F6212", "#701A75", "#9A2A2A", "#1E40AF", "#365314", "#4C1D95", "#7F1D1D", "#164E63",
        "#713F12", "#14532D", "#581C87", "#7C2D43", "#1E3A5F", "#44403C", "#0F172A", "#78716C",
        "#A8A29E", "#292524",
    ];

    private static void ValidateAllyMembership(
        List<FactionSetup> factions,
        List<AllyGroupSetup> allyGroups,
        List<DomainError> errors)
    {
        if (allyGroups.Count == 0 || factions.Count == 0)
        {
            return;
        }

        foreach (var group in allyGroups)
        {
            var members = factions
                .Count(faction => string.Equals(faction.AllyGroupName, group.Name, StringComparison.OrdinalIgnoreCase));
            if (members < 2)
            {
                errors.Add(new DomainError(
                    "allyGroups.members.invalid",
                    $"Ally group '{group.Name}' must include at least two factions.",
                    "allyGroups"));
            }

            if (members == factions.Count)
            {
                errors.Add(new DomainError(
                    "allyGroups.covers_all",
                    "All factions cannot belong to a single ally group.",
                    "allyGroups"));
            }
        }
    }

    private static List<CampaignExternalLink> ParseLinks(IReadOnlyList<CampaignLinkInput>? links, List<DomainError> errors)
    {
        var parsed = new List<CampaignExternalLink>();
        if (links is null || links.Count == 0)
        {
            return parsed;
        }

        var supplied = links
            .Where(static link => !string.IsNullOrWhiteSpace(link.Label) || !string.IsNullOrWhiteSpace(link.Url))
            .ToArray();
        if (supplied.Length > MaxLinkCount)
        {
            errors.Add(new DomainError(
                "links.invalid",
                $"At most {MaxLinkCount} external links are allowed.",
                "links"));
            return parsed;
        }

        for (var index = 0; index < supplied.Length; index++)
        {
            var link = supplied[index];
            var labelField = $"links[{index}].label";
            var urlField = $"links[{index}].url";
            var label = ParseRequiredName(
                link.Label,
                labelField,
                $"Link {index + 1} label",
                minLength: 1,
                LinkLabelMaxLength,
                errors);

            if (string.IsNullOrWhiteSpace(link.Url))
            {
                errors.Add(new DomainError("links.url.invalid", $"Link {index + 1} URL is not filled in.", urlField));
                continue;
            }

            var trimmedUrl = link.Url.Trim();
            if (trimmedUrl.Length > LinkUrlMaxLength)
            {
                errors.Add(new DomainError(
                    "links.url.invalid",
                    $"Link {index + 1} URL is too long (maximum {LinkUrlMaxLength} characters).",
                    urlField));
                continue;
            }

            if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || !string.IsNullOrEmpty(uri.UserInfo))
            {
                errors.Add(new DomainError(
                    "links.url.invalid",
                    $"Link {index + 1} URL must be an http or https address.",
                    urlField));
                continue;
            }

            if (label is not null)
            {
                parsed.Add(new CampaignExternalLink(label, uri.AbsoluteUri));
            }
        }

        return parsed;
    }

    private static CampaignSchedule? ParseSchedule(CampaignScheduleInput? schedule, List<DomainError> errors)
    {
        if (schedule is null)
        {
            errors.Add(new DomainError(
                "schedule.invalid",
                "Round schedule is required.",
                "schedule"));
            return null;
        }

        if (!IanaTimeZone.TryCreate(
                string.IsNullOrWhiteSpace(schedule.TimeZoneId) ? IanaTimeZone.UtcId : schedule.TimeZoneId,
                out var timeZone,
                out var timeZoneError))
        {
            errors.Add(timeZoneError);
            timeZone = null;
        }

        DateTimeOffset startsUtc = default;
        if (timeZone is not null
            && !CampaignCalendar.TryParseLocalStart(schedule.StartsAtLocal, timeZone, out startsUtc, out var startError))
        {
            errors.Add(startError);
        }

        if (schedule.RoundCount < MinRoundCount || schedule.RoundCount > MaxRoundCount)
        {
            errors.Add(new DomainError(
                "roundCount.invalid",
                $"Number of rounds must be between {MinRoundCount} and {MaxRoundCount}.",
                "roundCount"));
        }

        var roundLength = ParseDuration(
            schedule.RoundLengthAmount,
            schedule.RoundLengthUnit,
            "roundLength",
            "Round length",
            errors);
        var phases = ParsePhases(schedule.Phases, errors);

        if (timeZone is null || roundLength is null || phases.Count == 0 || startsUtc == default)
        {
            return null;
        }

        var roundEnd = CampaignCalendar.Add(startsUtc, timeZone, roundLength);
        var actionCursor = startsUtc;
        foreach (var phase in phases.Where(static phase => phase.Kind == RoundPhaseKind.Action))
        {
            actionCursor = CampaignCalendar.Add(actionCursor, timeZone, phase.Duration);
        }

        if (actionCursor > roundEnd)
        {
            errors.Add(new DomainError(
                "phases.actions_too_long",
                "Action lengths added together cannot be longer than the round.",
                "phases"));
        }

        var phaseCursor = startsUtc;
        foreach (var phase in phases)
        {
            phaseCursor = CampaignCalendar.Add(phaseCursor, timeZone, phase.Duration);
        }

        if (phaseCursor != roundEnd)
        {
            errors.Add(new DomainError(
                "phases.duration_mismatch",
                "Action and battle-phase lengths must add up to the round length.",
                "phases"));
        }

        var armyEscalations = ParseArmyEscalations(schedule.RoundEscalations, schedule.RoundCount, errors);

        if (errors.Count > 0)
        {
            return null;
        }

        var endsUtc = startsUtc;
        for (var round = 0; round < schedule.RoundCount; round++)
        {
            endsUtc = CampaignCalendar.Add(endsUtc, timeZone, roundLength);
        }

        return new CampaignSchedule(
            timeZone,
            startsUtc,
            endsUtc,
            schedule.RoundCount,
            roundLength,
            phases,
            armyEscalations);
    }

    private static List<RoundPhaseSetup> ParsePhases(IReadOnlyList<RoundPhaseInput>? phases, List<DomainError> errors)
    {
        var parsed = new List<RoundPhaseSetup>();
        if (phases is null || phases.Count == 0)
        {
            errors.Add(new DomainError(
                "phases.invalid",
                "A round must include at least one action and one battle phase.",
                "phases"));
            return parsed;
        }

        if (phases.Count > MaxPhaseCount)
        {
            errors.Add(new DomainError(
                "phases.invalid",
                $"At most {MaxPhaseCount} action and battle steps are allowed in a round.",
                "phases"));
            return parsed;
        }

        for (var index = 0; index < phases.Count; index++)
        {
            var phase = phases[index];
            var field = $"phases[{index}].kind";
            if (!TryParsePhaseKind(phase.Kind, out var kind))
            {
                errors.Add(new DomainError(
                    "phases.kind.invalid",
                    $"Round step {index + 1} must be an action or a battle phase.",
                    field));
                continue;
            }

            var duration = ParseDuration(
                phase.DurationAmount,
                phase.DurationUnit,
                $"phases[{index}].duration",
                $"Round step {index + 1} length",
                errors);
            if (duration is null)
            {
                continue;
            }

            parsed.Add(new RoundPhaseSetup(kind, duration, phase.EndPhaseEarlyIfAble ?? true));
        }

        var actionCount = parsed.Count(static phase => phase.Kind == RoundPhaseKind.Action);
        var battleCount = parsed.Count(static phase => phase.Kind == RoundPhaseKind.Battle);
        if (actionCount < MinActionPhaseCount || battleCount < MinBattlePhaseCount)
        {
            errors.Add(new DomainError(
                "phases.invalid",
                "A round must include at least one action and one battle phase.",
                "phases"));
        }

        return parsed;
    }

    private static ScheduleDuration? ParseDuration(
        int amount,
        string? unitName,
        string field,
        string label,
        List<DomainError> errors)
    {
        if (!TryParseDurationUnit(unitName, out var unit))
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} must use minutes, hours, days, weeks, or months.",
                field));
            return null;
        }

        var (min, max) = RangeFor(unit);
        if (amount < min || amount > max)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} must be between {min} and {max} {unit.ToString().ToLowerInvariant()}.",
                field));
            return null;
        }

        return new ScheduleDuration(amount, unit);
    }

    private static (int Min, int Max) RangeFor(DurationUnit unit)
    {
        return unit switch
        {
            DurationUnit.Minutes => (1, 60),
            DurationUnit.Hours => (1, 24),
            DurationUnit.Days => (1, 7),
            DurationUnit.Weeks => (1, 52),
            DurationUnit.Months => (1, 12),
            _ => (1, 1),
        };
    }

    private static bool TryParseDurationUnit(string? raw, out DurationUnit unit)
    {
        unit = default;
        if (string.IsNullOrWhiteSpace(raw) || int.TryParse(raw, out _))
        {
            return false;
        }

        return Enum.TryParse(raw.Trim(), ignoreCase: true, out unit) && Enum.IsDefined(unit);
    }

    private static bool TryParsePhaseKind(string? raw, out RoundPhaseKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(raw) || int.TryParse(raw, out _))
        {
            return false;
        }

        return Enum.TryParse(raw.Trim(), ignoreCase: true, out kind) && Enum.IsDefined(kind);
    }

    private static List<SpecialRuleSetup> ParseSpecialRules(
        IReadOnlyList<SpecialRuleInput>? specialRules,
        HashSet<Guid> usedIds,
        List<DomainError> errors)
    {
        var supplied = specialRules ?? [];
        var parsed = new List<SpecialRuleSetup>();
        if (supplied.Count > MaxSpecialRuleCount)
        {
            errors.Add(new DomainError(
                "specialRules.invalid",
                $"At most {MaxSpecialRuleCount} special rules are allowed.",
                "specialRules"));
            return parsed;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Count; index++)
        {
            var input = supplied[index];
            var name = ParseRequiredName(
                input.Name,
                $"specialRules[{index}].name",
                $"Special rule {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            if (name is null)
            {
                continue;
            }

            if (!seenNames.Add(name))
            {
                errors.Add(new DomainError(
                    "specialRules.duplicate",
                    "Special rule names must be unique.",
                    $"specialRules[{index}].name"));
                continue;
            }

            var text = ParseOptionalCatalogText(
                input.Text,
                $"specialRules[{index}].text",
                $"Special rule {index + 1} text",
                errors) ?? string.Empty;
            parsed.Add(new SpecialRuleSetup(
                ResolveId(input.Id, usedIds, $"specialRules[{index}].id", errors),
                name,
                text,
                ParseEffectKey(input.EffectKey, index, errors)));
        }

        return parsed;
    }

    private static List<ForceStatusSetup> ParseForceStatuses(
        IReadOnlyList<ForceStatusInput>? forceStatuses,
        HashSet<Guid> usedIds,
        List<DomainError> errors)
    {
        var supplied = forceStatuses ?? [];
        var parsed = new List<ForceStatusSetup>();
        if (supplied.Count > MaxForceStatusCount)
        {
            errors.Add(new DomainError(
                "forceStatuses.invalid",
                $"At most {MaxForceStatusCount} force statuses are allowed.",
                "forceStatuses"));
            return parsed;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Count; index++)
        {
            var input = supplied[index];
            var name = ParseRequiredName(
                input.Name,
                $"forceStatuses[{index}].name",
                $"Force status {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            if (name is null)
            {
                continue;
            }

            if (string.Equals(name, "Normal", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new DomainError(
                    "forceStatuses.normal",
                    "Normal is the absence of a status and cannot be configured.",
                    $"forceStatuses[{index}].name"));
                continue;
            }

            if (!seenNames.Add(name))
            {
                errors.Add(new DomainError(
                    "forceStatuses.duplicate",
                    "Force status names must be unique.",
                    $"forceStatuses[{index}].name"));
                continue;
            }

            var effects = ParseOptionalCatalogText(
                input.Effects,
                $"forceStatuses[{index}].effects",
                $"Force status {index + 1} effects",
                errors) ?? string.Empty;
            if (!TryParseForceStatusEnable(input.EnableTrigger, out var enable))
            {
                errors.Add(new DomainError(
                    "forceStatuses.enable.invalid",
                    "Choose how this force status is enabled.",
                    $"forceStatuses[{index}].enableTrigger"));
                continue;
            }

            if (!TryParseForceStatusClear(input.ClearTrigger, out var clear))
            {
                errors.Add(new DomainError(
                    "forceStatuses.clear.invalid",
                    "Choose how this force status is cleared.",
                    $"forceStatuses[{index}].clearTrigger"));
                continue;
            }

            parsed.Add(new ForceStatusSetup(
                ResolveId(input.Id, usedIds, $"forceStatuses[{index}].id", errors),
                name,
                effects,
                enable,
                clear));
        }

        return parsed;
    }

    private static bool TryParseForceStatusEnable(string? raw, out ForceStatusEnableTrigger trigger)
    {
        trigger = default;
        return !string.IsNullOrWhiteSpace(raw)
            && Enum.TryParse(raw.Trim(), ignoreCase: true, out trigger)
            && Enum.IsDefined(trigger);
    }

    private static bool TryParseForceStatusClear(string? raw, out ForceStatusClearTrigger trigger)
    {
        trigger = default;
        return !string.IsNullOrWhiteSpace(raw)
            && Enum.TryParse(raw.Trim(), ignoreCase: true, out trigger)
            && Enum.IsDefined(trigger);
    }

    private static List<PrivateObjectiveTypeSetup> ParsePrivateObjectiveTypes(
        IReadOnlyList<PrivateObjectiveTypeInput>? privateObjectiveTypes,
        HashSet<Guid> usedIds,
        HashSet<Guid> knownStructureTypeIds,
        List<DomainError> errors)
    {
        var supplied = privateObjectiveTypes ?? [];
        var parsed = new List<PrivateObjectiveTypeSetup>();
        if (supplied.Count > MaxPrivateObjectiveTypeCount)
        {
            errors.Add(new DomainError(
                "privateObjectiveTypes.invalid",
                $"At most {MaxPrivateObjectiveTypeCount} private objectives are allowed.",
                "privateObjectiveTypes"));
            return parsed;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Count; index++)
        {
            var input = supplied[index];
            var name = ParseRequiredName(
                input.Name,
                $"privateObjectiveTypes[{index}].name",
                $"Private objective {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            if (name is null)
            {
                continue;
            }

            if (!seenNames.Add(name))
            {
                errors.Add(new DomainError(
                    "privateObjectiveTypes.duplicate",
                    "Private objective names must be unique.",
                    $"privateObjectiveTypes[{index}].name"));
                continue;
            }

            var description = ParseOptionalCatalogText(
                input.Description,
                $"privateObjectiveTypes[{index}].description",
                $"Private objective {index + 1} description",
                errors);
            var holderKinds = ParseHolderKinds(input.AllowedHolderKinds, index, errors);
            if (!TryParseScoringKind(input.ScoringKind, index, errors, out var scoringKind))
            {
                continue;
            }

            var automaticKind = PrivateObjectiveAutomaticKind.None;
            if (scoringKind == PrivateObjectiveScoringKind.Automatic
                && !TryParseAutomaticKind(input.AutomaticKind, index, errors, out automaticKind))
            {
                continue;
            }

            if (scoringKind == PrivateObjectiveScoringKind.Manual)
            {
                automaticKind = PrivateObjectiveAutomaticKind.None;
            }

            var requiredCount = input.RequiredCount ?? 1;
            if (requiredCount < 1 || requiredCount > MaxCampaignPoints)
            {
                errors.Add(new DomainError(
                    $"privateObjectiveTypes[{index}].requiredCount.invalid",
                    $"Private objective {index + 1} required count must be between 1 and {MaxCampaignPoints}.",
                    $"privateObjectiveTypes[{index}].requiredCount"));
                requiredCount = 1;
            }

            var structureTypeId = input.StructureTypeId;
            if (automaticKind is PrivateObjectiveAutomaticKind.ControlStructureType
                or PrivateObjectiveAutomaticKind.PillageStructureType
                or PrivateObjectiveAutomaticKind.DestroyStructureType)
            {
                if (structureTypeId is not { } structureId || !knownStructureTypeIds.Contains(structureId))
                {
                    errors.Add(new DomainError(
                        $"privateObjectiveTypes[{index}].structureTypeId.invalid",
                        $"Private objective {index + 1} must name a structure type from this campaign.",
                        $"privateObjectiveTypes[{index}].structureTypeId"));
                    structureTypeId = null;
                }
            }
            else
            {
                structureTypeId = null;
            }

            var territoryIds = (input.TerritoryIds ?? [])
                .Where(static id => id != Guid.Empty)
                .Distinct()
                .ToArray();
            if (automaticKind == PrivateObjectiveAutomaticKind.ControlNamedTerritories && territoryIds.Length == 0)
            {
                errors.Add(new DomainError(
                    $"privateObjectiveTypes[{index}].territoryIds.invalid",
                    $"Private objective {index + 1} must list at least one territory.",
                    $"privateObjectiveTypes[{index}].territoryIds"));
            }

            parsed.Add(new PrivateObjectiveTypeSetup(
                ResolveId(input.Id, usedIds, $"privateObjectiveTypes[{index}].id", errors),
                name,
                description,
                ParseCampaignPoints(
                    input.CampaignPoints,
                    $"privateObjectiveTypes[{index}].campaignPoints",
                    $"Private objective {index + 1} campaign points",
                    errors),
                holderKinds,
                scoringKind,
                automaticKind,
                requiredCount,
                structureTypeId,
                territoryIds));
        }

        return parsed;
    }

    private static List<PrivateObjectiveHolderKind> ParseHolderKinds(
        IReadOnlyList<string>? raw,
        int index,
        List<DomainError> errors)
    {
        if (raw is null || raw.Count == 0)
        {
            return
            [
                PrivateObjectiveHolderKind.Player,
                PrivateObjectiveHolderKind.Faction,
                PrivateObjectiveHolderKind.AllyGroup,
            ];
        }

        var parsed = new List<PrivateObjectiveHolderKind>();
        foreach (var value in raw)
        {
            if (!Enum.TryParse<PrivateObjectiveHolderKind>(value, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
            {
                errors.Add(new DomainError(
                    $"privateObjectiveTypes[{index}].allowedHolderKinds.invalid",
                    $"Private objective {index + 1} holder must be Player, Faction, or AllyGroup.",
                    $"privateObjectiveTypes[{index}].allowedHolderKinds"));
                continue;
            }

            if (!parsed.Contains(kind))
            {
                parsed.Add(kind);
            }
        }

        return parsed.Count == 0
            ?
            [
                PrivateObjectiveHolderKind.Player,
                PrivateObjectiveHolderKind.Faction,
                PrivateObjectiveHolderKind.AllyGroup,
            ]
            : parsed;
    }

    private static bool TryParseScoringKind(
        string? raw,
        int index,
        List<DomainError> errors,
        out PrivateObjectiveScoringKind kind)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            kind = PrivateObjectiveScoringKind.Manual;
            return true;
        }

        if (Enum.TryParse(raw.Trim(), ignoreCase: true, out kind) && Enum.IsDefined(kind))
        {
            return true;
        }

        errors.Add(new DomainError(
            $"privateObjectiveTypes[{index}].scoringKind.invalid",
            $"Private objective {index + 1} scoring must be Manual or Automatic.",
            $"privateObjectiveTypes[{index}].scoringKind"));
        kind = PrivateObjectiveScoringKind.Manual;
        return false;
    }

    private static bool TryParseAutomaticKind(
        string? raw,
        int index,
        List<DomainError> errors,
        out PrivateObjectiveAutomaticKind kind)
    {
        if (Enum.TryParse(raw, ignoreCase: true, out kind)
            && Enum.IsDefined(kind)
            && kind != PrivateObjectiveAutomaticKind.None)
        {
            return true;
        }

        errors.Add(new DomainError(
            $"privateObjectiveTypes[{index}].automaticKind.invalid",
            $"Private objective {index + 1} needs an automatic criterion.",
            $"privateObjectiveTypes[{index}].automaticKind"));
        kind = PrivateObjectiveAutomaticKind.None;
        return false;
    }

    private static List<Guid> ParseAssignedSpecialRuleIds(
        IReadOnlyList<Guid>? ids,
        HashSet<Guid> knownSpecialRuleIds,
        string field,
        string ownerLabel,
        List<DomainError> errors)
    {
        if (ids is null || ids.Count == 0)
        {
            return [];
        }

        var parsed = new List<Guid>();
        foreach (var id in ids.Distinct())
        {
            if (!knownSpecialRuleIds.Contains(id))
            {
                errors.Add(new DomainError(
                    $"{field}.unknown",
                    $"{ownerLabel} references a special rule that was not created.",
                    field));
                continue;
            }

            parsed.Add(id);
        }

        return parsed;
    }

    private static List<SubfactionSpecialRulesSetup> ParseSubfactionSpecialRules(
        IReadOnlyList<SubfactionSpecialRulesInput>? assignments,
        IReadOnlyList<string> subfactions,
        HashSet<Guid> knownSpecialRuleIds,
        int factionIndex,
        List<DomainError> errors)
    {
        if (assignments is null || assignments.Count == 0)
        {
            return [];
        }

        var knownNames = subfactions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var parsed = new List<SubfactionSpecialRulesSetup>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < assignments.Count; index++)
        {
            var input = assignments[index];
            var field = $"factions[{factionIndex}].subfactionSpecialRules[{index}].name";
            var name = ParseRequiredName(
                input.Name,
                field,
                $"Faction {factionIndex + 1} subfaction special-rule assignment {index + 1}",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            if (name is null)
            {
                continue;
            }

            if (!knownNames.Contains(name))
            {
                errors.Add(new DomainError(
                    $"{field}.unknown",
                    $"Faction {factionIndex + 1} subfaction special rules reference a subfaction that was not listed.",
                    field));
                continue;
            }

            if (!seen.Add(name))
            {
                continue;
            }

            var canonical = subfactions.First(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
            parsed.Add(new SubfactionSpecialRulesSetup(
                canonical,
                ParseAssignedSpecialRuleIds(
                    input.SpecialRuleIds,
                    knownSpecialRuleIds,
                    $"factions[{factionIndex}].subfactionSpecialRules[{index}].specialRuleIds",
                    $"Faction {factionIndex + 1} subfaction {canonical}",
                    errors)));
        }

        return parsed;
    }

    private static string? ParseEffectKey(string? raw, int index, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var key = raw.Trim();
        if (!SpecialRuleEffectKeys.IsKnown(key))
        {
            errors.Add(new DomainError(
                $"specialRules[{index}].effectKey.invalid",
                $"Special rule {index + 1} effect key is not a known campaign policy.",
                $"specialRules[{index}].effectKey"));
            return null;
        }

        return key;
    }

    private static List<ItemObjectiveChoiceSetup> ParseItemObjectiveChoices(
        IReadOnlyList<ItemObjectiveChoiceInput>? choices,
        HashSet<Guid> usedIds,
        HashSet<Guid> knownPrivateObjectiveIds,
        int itemIndex,
        List<DomainError> errors)
    {
        var supplied = choices ?? [];
        if (supplied.Count > MaxItemObjectiveChoiceCount)
        {
            errors.Add(new DomainError(
                $"itemObjectiveTypes[{itemIndex}].choices.invalid",
                $"Item objective {itemIndex + 1} may have at most {MaxItemObjectiveChoiceCount} choices.",
                $"itemObjectiveTypes[{itemIndex}].choices"));
            return [];
        }

        var parsed = new List<ItemObjectiveChoiceSetup>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Count; index++)
        {
            var input = supplied[index];
            var name = ParseRequiredName(
                input.Name,
                $"itemObjectiveTypes[{itemIndex}].choices[{index}].name",
                $"Item objective {itemIndex + 1} choice {index + 1} name",
                minLength: 1,
                NamedItemMaxLength,
                errors);
            if (name is null)
            {
                continue;
            }

            if (!seenNames.Add(name))
            {
                errors.Add(new DomainError(
                    $"itemObjectiveTypes[{itemIndex}].choices.duplicate",
                    $"Item objective {itemIndex + 1} choice names must be unique.",
                    $"itemObjectiveTypes[{itemIndex}].choices[{index}].name"));
                continue;
            }

            var results = ParseItemObjectiveChoiceResults(
                input.Results,
                usedIds,
                knownPrivateObjectiveIds,
                itemIndex,
                index,
                errors);
            if (results.Count == 0)
            {
                errors.Add(new DomainError(
                    $"itemObjectiveTypes[{itemIndex}].choices[{index}].results.invalid",
                    $"Item objective {itemIndex + 1} choice {index + 1} needs at least one result.",
                    $"itemObjectiveTypes[{itemIndex}].choices[{index}].results"));
                continue;
            }

            parsed.Add(new ItemObjectiveChoiceSetup(
                ResolveId(
                    input.Id,
                    usedIds,
                    $"itemObjectiveTypes[{itemIndex}].choices[{index}].id",
                    errors),
                name,
                results));
        }

        return parsed;
    }

    private static List<ItemObjectiveChoiceResultSetup> ParseItemObjectiveChoiceResults(
        IReadOnlyList<ItemObjectiveChoiceResultInput>? results,
        HashSet<Guid> usedIds,
        HashSet<Guid> knownPrivateObjectiveIds,
        int itemIndex,
        int choiceIndex,
        List<DomainError> errors)
    {
        var supplied = results ?? [];
        if (supplied.Count > MaxItemObjectiveChoiceResultCount)
        {
            errors.Add(new DomainError(
                $"itemObjectiveTypes[{itemIndex}].choices[{choiceIndex}].results.invalid",
                $"Item objective {itemIndex + 1} choice {choiceIndex + 1} may have at most {MaxItemObjectiveChoiceResultCount} results.",
                $"itemObjectiveTypes[{itemIndex}].choices[{choiceIndex}].results"));
            return [];
        }

        var parsed = new List<ItemObjectiveChoiceResultSetup>();
        for (var index = 0; index < supplied.Count; index++)
        {
            var input = supplied[index];
            var flavor = ParseOptionalCatalogText(
                input.FlavorText,
                $"itemObjectiveTypes[{itemIndex}].choices[{choiceIndex}].results[{index}].flavorText",
                $"Item objective {itemIndex + 1} choice result flavor text",
                errors);
            var stateKey = ParseOptionalCatalogText(
                input.NewStateKey,
                $"itemObjectiveTypes[{itemIndex}].choices[{choiceIndex}].results[{index}].newStateKey",
                $"Item objective {itemIndex + 1} choice result state",
                errors,
                NamedItemMaxLength);
            var granted = input.GrantedPrivateObjectiveTypeId;
            if (granted is { } grantedId && grantedId != Guid.Empty && !knownPrivateObjectiveIds.Contains(grantedId))
            {
                errors.Add(new DomainError(
                    $"itemObjectiveTypes[{itemIndex}].choices[{choiceIndex}].results[{index}].grantedPrivateObjectiveTypeId.unknown",
                    $"Item objective {itemIndex + 1} choice result references an unknown private objective.",
                    $"itemObjectiveTypes[{itemIndex}].choices[{choiceIndex}].results[{index}].grantedPrivateObjectiveTypeId"));
                granted = null;
            }

            parsed.Add(new ItemObjectiveChoiceResultSetup(
                ResolveId(
                    input.Id,
                    usedIds,
                    $"itemObjectiveTypes[{itemIndex}].choices[{choiceIndex}].results[{index}].id",
                    errors),
                flavor,
                stateKey,
                input.DestroyItem,
                input.ReplacementItemTypeId is { } replacement && replacement != Guid.Empty ? replacement : null,
                granted is { } id && id != Guid.Empty ? id : null));
        }

        return parsed;
    }

    private static void ValidateItemReplacementReferences(
        IReadOnlyList<ItemObjectiveTypeSetup> items,
        List<DomainError> errors)
    {
        var known = items.Select(static item => item.Id).ToHashSet();
        foreach (var item in items)
        {
            foreach (var choice in item.Choices)
            {
                foreach (var result in choice.Results)
                {
                    if (result.ReplacementItemTypeId is { } replacement
                        && replacement != item.Id
                        && !known.Contains(replacement))
                    {
                        errors.Add(new DomainError(
                            "itemObjectiveTypes.replacement.unknown",
                            $"Item objective '{item.Name}' choice '{choice.Name}' references an unknown replacement item.",
                            "itemObjectiveTypes"));
                    }
                }
            }
        }
    }

    private static string? ParseOptionalCatalogText(
        string? raw,
        string field,
        string label,
        List<DomainError> errors,
        int maxLength = CatalogTextMaxLength)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > maxLength)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"{label} is too long (maximum {maxLength} characters).",
                field));
            return null;
        }

        return trimmed;
    }

    private sealed class MissionIndex
    {
        public Dictionary<Guid, MissionSetup> ById { get; } = [];

        public Dictionary<string, Guid> Names { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
