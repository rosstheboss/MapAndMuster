using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Common;
using Campaign.Domain.Identity;

namespace Campaign.Domain.Campaigns;

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

    /// <summary>Maximum missions nested under one terrain type or structure.</summary>
    public const int MaxMissionsPerCatalogItem = 20;

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

    /// <summary>Maximum action and battle steps in a round.</summary>
    public const int MaxPhaseCount = 16;

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
            country);
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
        string? country = null)
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

        var parsedGroups = ParseAllyGroups(allyGroups, collected);
        var usedIds = new HashSet<Guid>();
        var missionIndex = new MissionIndex();
        var parsedFactions = ParseFactions(factions, parsedGroups, usedIds, collected);
        ValidateAllyMembership(parsedFactions, parsedGroups, collected);
        var parsedLinks = ParseLinks(links, collected);
        var parsedTerrain = ParseTerrainTypes(terrainTypes, usedIds, missionIndex, collected);
        var parsedStructures = ParseStructureTypes(structureTypes, usedIds, missionIndex, collected);
        var parsedSchedule = ParseSchedule(schedule, collected);

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
            parsedSchedule!);
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

    private static List<AllyGroupSetup> ParseAllyGroups(IReadOnlyList<AllyGroupInput>? allyGroups, List<DomainError> errors)
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
            if (name is null)
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

            parsed.Add(new AllyGroupSetup(name));
        }

        return parsed;
    }

    private static List<FactionSetup> ParseFactions(
        IReadOnlyList<FactionInput>? factions,
        IReadOnlyList<AllyGroupSetup> allyGroups,
        HashSet<Guid> usedIds,
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
            var allyGroupName = string.IsNullOrWhiteSpace(faction.AllyGroupName)
                ? null
                : faction.AllyGroupName.Trim();
            if (allyGroupName is not null && !groupNames.Contains(allyGroupName))
            {
                errors.Add(new DomainError(
                    "factions.ally_group.unknown",
                    $"Faction {index + 1} references an ally group that was not created.",
                    $"factions[{index}].allyGroupName"));
                allyGroupName = null;
            }

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
                faction.ClearFlagImage));
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
                missionsForType));
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

            parsed.Add(new StructureTypeSetup(
                ResolveId(input.Id, usedIds, $"structureTypes[{index}].id", errors),
                name,
                builtin,
                input.ClearImage,
                missionsForType));
        }

        return parsed;
    }

    private static List<MissionSetup> ParseMissions(
        IReadOnlyList<MissionInput>? missions,
        HashSet<Guid> usedIds,
        MissionIndex index,
        string field,
        string ownerLabel,
        bool requireAtLeastOne,
        List<DomainError> errors)
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

        if (supplied.Length > MaxMissionsPerCatalogItem)
        {
            errors.Add(new DomainError(
                "missions.invalid",
                $"{ownerLabel} can have at most {MaxMissionsPerCatalogItem} missions.",
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
            var created = new MissionSetup(id, name, url, mission.ClearFile);
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

        if (errors.Count > 0)
        {
            return null;
        }

        var endsUtc = startsUtc;
        for (var round = 0; round < schedule.RoundCount; round++)
        {
            endsUtc = CampaignCalendar.Add(endsUtc, timeZone, roundLength);
        }

        return new CampaignSchedule(timeZone, startsUtc, endsUtc, schedule.RoundCount, roundLength, phases);
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

            parsed.Add(new RoundPhaseSetup(kind, duration));
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

    private sealed class MissionIndex
    {
        public Dictionary<Guid, MissionSetup> ById { get; } = [];

        public Dictionary<string, Guid> Names { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
