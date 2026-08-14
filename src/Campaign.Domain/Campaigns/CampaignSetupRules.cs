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
    /// <param name="setup">The validated setup when successful.</param>
    /// <param name="validatedJoinPassword">The join password to hash when a new password was supplied.</param>
    /// <param name="errors">Every field error, in a stable order.</param>
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
        [NotNullWhen(true)] out CampaignSetup? setup,
        out string? validatedJoinPassword,
        out IReadOnlyList<DomainError> errors)
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

        var parsedGroups = ParseAllyGroups(allyGroups, collected);
        var parsedFactions = ParseFactions(factions, parsedGroups, collected);
        ValidateAllyMembership(parsedFactions, parsedGroups, collected);
        var parsedLinks = ParseLinks(links, collected);

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
            creatorIsParticipant,
            parsedFactions,
            parsedGroups,
            parsedLinks);
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

            if (name is null)
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
            parsed.Add(new FactionSetup(name, subfactions, canonicalGroup));
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
}
