using System.Text.Json;
using Campaign.Application.Campaigns;

namespace Campaign.Infrastructure.Campaigns;

/// <summary>
/// Serializes campaign factions, schedule, and related setup for preset snapshots.
/// </summary>
internal static class CampaignPresetSettingsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize(StoredCampaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return JsonSerializer.Serialize(
            new PresetSettingsDocument
            {
                Description = campaign.Description,
                PlayerSlotCount = campaign.PlayerSlotCount,
                CreatorIsParticipant = campaign.CreatorIsParticipant,
                TimeZoneId = campaign.TimeZoneId,
                RoundCount = campaign.RoundCount,
                RoundLengthAmount = campaign.RoundLengthAmount,
                RoundLengthUnit = campaign.RoundLengthUnit,
                Factions = [.. campaign.Factions],
                AllyGroups = [.. campaign.AllyGroups],
                Links = [.. campaign.Links],
                Phases = [.. campaign.Phases],
            },
            Options);
    }

    public static PresetSettingsDocument Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new PresetSettingsDocument();
        }

        return JsonSerializer.Deserialize<PresetSettingsDocument>(json, Options) ?? new PresetSettingsDocument();
    }

    internal sealed class PresetSettingsDocument
    {
        public string? Description { get; set; }

        public int PlayerSlotCount { get; set; } = 8;

        public bool CreatorIsParticipant { get; set; } = true;

        public string TimeZoneId { get; set; } = "UTC";

        public int RoundCount { get; set; } = 8;

        public int RoundLengthAmount { get; set; } = 1;

        public string RoundLengthUnit { get; set; } = "Weeks";

        public List<StoredFaction> Factions { get; set; } = [];

        public List<StoredAllyGroup> AllyGroups { get; set; } = [];

        public List<StoredCampaignLink> Links { get; set; } = [];

        public List<StoredRoundPhase> Phases { get; set; } = [];
    }
}
