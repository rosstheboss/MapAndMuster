using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using MapAndMuster.Application.Campaigns;
using MapAndMuster.Application.Common;
using MapAndMuster.Application.Maps;
using MapAndMuster.Application.Ports;
using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Infrastructure.Campaigns;

/// <summary>
/// ZIP codec for administrator campaign-preset transfer between hosts.
/// </summary>
public sealed class CampaignPresetPackageCodec : ICampaignPresetPackageCodec
{
    /// <summary>Stable format identifier stored in the package manifest.</summary>
    public const string FormatName = "mapandmuster.campaign-preset";

    /// <summary>Current package schema version.</summary>
    public const int FormatVersion = 1;

    /// <summary>Maximum uncompressed ZIP payload.</summary>
    public const int MaxUncompressedBytes = 64 * 1024 * 1024;

    /// <summary>Maximum ZIP entries.</summary>
    public const int MaxEntries = 256;

    private const string ManifestEntry = "manifest.json";
    private const string CatalogEntry = "catalog.json";
    private const string SettingsEntry = "settings.json";
    private const string OverlayJsonEntry = "overlay.json";
    private const string OverlaySvgEntry = "overlay.svg";
    private const string MapEntry = "map.png";
    private const string AssetsPrefix = "assets/";

    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <inheritdoc />
    public byte[] Write(StoredCampaign campaign, IReadOnlyDictionary<string, byte[]> files)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(files);

        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(
                zip,
                ManifestEntry,
                JsonSerializer.SerializeToUtf8Bytes(
                    new ManifestDocument { Format = FormatName, Version = FormatVersion, Name = campaign.Name },
                    ManifestOptions));
            WriteEntry(
                zip,
                CatalogEntry,
                Encoding.UTF8.GetBytes(
                    CatalogJson.Serialize(
                        campaign.TerrainTypes,
                        campaign.StructureTypes,
                        campaign.ItemObjectiveTypes,
                        campaign.PublicObjectiveTypes,
                        campaign.BattleScoring,
                        campaign.RankingObjectivePoints,
                        campaign.SpecialRules,
                        campaign.PrivateObjectiveTypes,
                        campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.SpecialRuleIds),
                        campaign.ForceStatuses,
                        campaign.SplitForceSupplyPenaltyPercent,
                        campaign.SplitForceSupplyPenaltyIsPercent,
                        campaign.BattleReportRules,
                        campaign.ArmyEscalations,
                        campaign.Missions,
                        campaign.Factions.ToDictionary(static faction => faction.Id, static faction => faction.SubfactionSpecialRules))));
            WriteEntry(zip, SettingsEntry, Encoding.UTF8.GetBytes(CampaignPresetSettingsJson.Serialize(campaign)));
            if (campaign.MapGraph is not null)
            {
                WriteEntry(zip, OverlayJsonEntry, Encoding.UTF8.GetBytes(MapGraphJson.Serialize(campaign.MapGraph)));
                WriteEntry(zip, OverlaySvgEntry, Encoding.UTF8.GetBytes(WriteOverlaySvg(campaign.MapGraph)));
            }

            if (!string.IsNullOrWhiteSpace(campaign.MapStorageKey)
                && files.TryGetValue(campaign.MapStorageKey, out var mapBytes))
            {
                WriteEntry(zip, MapEntry, mapBytes);
            }

            foreach (var (key, bytes) in files)
            {
                if (string.Equals(key, campaign.MapStorageKey, StringComparison.Ordinal))
                {
                    continue;
                }

                WriteEntry(zip, AssetsPrefix + key, bytes);
            }
        }

        return output.ToArray();
    }

    /// <inheritdoc />
    public OperationResult<CampaignPresetPackageContents> Read(ReadOnlyMemory<byte> content)
    {
        if (content.Length == 0)
        {
            return OperationResults.Failure<CampaignPresetPackageContents>(
                ErrorCodes.CampaignPresetPackageInvalid,
                "Upload a Map & Muster campaign preset file.");
        }

        try
        {
            using var input = new MemoryStream(content.ToArray(), writable: false);
            using var zip = new ZipArchive(input, ZipArchiveMode.Read);
            if (zip.Entries.Count == 0 || zip.Entries.Count > MaxEntries)
            {
                return InvalidPackage();
            }

            var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var total = 0L;
            foreach (var entry in zip.Entries)
            {
                var name = NormalizeEntryName(entry.FullName);
                if (name is null)
                {
                    return InvalidPackage();
                }

                if (entry.Length > MaxUncompressedBytes || total + entry.Length > MaxUncompressedBytes)
                {
                    return OperationResults.Failure<CampaignPresetPackageContents>(
                        ErrorCodes.UploadTooLarge,
                        "The campaign preset file is too large.");
                }

                using var stream = entry.Open();
                using var copy = new MemoryStream();
                stream.CopyTo(copy);
                total += copy.Length;
                entries[name] = copy.ToArray();
            }

            if (!entries.TryGetValue(ManifestEntry, out var manifestBytes)
                || !entries.TryGetValue(CatalogEntry, out var catalogBytes)
                || !entries.TryGetValue(SettingsEntry, out var settingsBytes))
            {
                return InvalidPackage();
            }

            var manifest = JsonSerializer.Deserialize<ManifestDocument>(manifestBytes, ManifestOptions);
            if (manifest is null
                || !string.Equals(manifest.Format, FormatName, StringComparison.Ordinal)
                || manifest.Version != FormatVersion
                || string.IsNullOrWhiteSpace(manifest.Name))
            {
                return InvalidPackage();
            }

            entries.TryGetValue(OverlayJsonEntry, out var overlayBytes);
            var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var (name, bytes) in entries)
            {
                if (!name.StartsWith(AssetsPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var key = name[AssetsPrefix.Length..];
                if (!IsPackageFileKey(key))
                {
                    return InvalidPackage();
                }

                files[key] = bytes;
            }

            var campaign = ToCampaign(manifest.Name, catalogBytes, settingsBytes, overlayBytes);
            if (entries.TryGetValue(MapEntry, out var mapPng))
            {
                const string importedMapKey = "maps/imported.png";
                files[importedMapKey] = mapPng;
                campaign = new StoredCampaign
                {
                    Id = campaign.Id,
                    Name = campaign.Name,
                    Description = campaign.Description,
                    PlayerSlotCount = campaign.PlayerSlotCount,
                    IsPrivate = campaign.IsPrivate,
                    IsPubliclyViewable = campaign.IsPubliclyViewable,
                    CreatorIsParticipant = campaign.CreatorIsParticipant,
                    MapStorageKey = importedMapKey,
                    Revision = campaign.Revision,
                    CreatedUtc = campaign.CreatedUtc,
                    UpdatedUtc = campaign.UpdatedUtc,
                    CreatedByUserId = campaign.CreatedByUserId,
                    Memberships = campaign.Memberships,
                    Factions = campaign.Factions,
                    AllyGroups = campaign.AllyGroups,
                    Links = campaign.Links,
                    TimeZoneId = campaign.TimeZoneId,
                    StartsUtc = campaign.StartsUtc,
                    EndsUtc = campaign.EndsUtc,
                    RoundCount = campaign.RoundCount,
                    RoundLengthAmount = campaign.RoundLengthAmount,
                    RoundLengthUnit = campaign.RoundLengthUnit,
                    Phases = campaign.Phases,
                    MapGraph = campaign.MapGraph,
                    TerrainTypes = campaign.TerrainTypes,
                    StructureTypes = campaign.StructureTypes,
                    ItemObjectiveTypes = campaign.ItemObjectiveTypes,
                    PublicObjectiveTypes = campaign.PublicObjectiveTypes,
                    SpecialRules = campaign.SpecialRules,
                    Missions = campaign.Missions,
                    ForceStatuses = campaign.ForceStatuses,
                    PrivateObjectiveTypes = campaign.PrivateObjectiveTypes,
                    BattleScoring = campaign.BattleScoring,
                    RankingObjectivePoints = campaign.RankingObjectivePoints,
                    SplitForceSupplyPenaltyPercent = campaign.SplitForceSupplyPenaltyPercent,
                    SplitForceSupplyPenaltyIsPercent = campaign.SplitForceSupplyPenaltyIsPercent,
                    BattleReportRules = campaign.BattleReportRules,
                    ArmyEscalations = campaign.ArmyEscalations,
                };
            }

            return OperationResults.Success(
                new CampaignPresetPackageContents
                {
                    Name = CampaignSetupRules.CollapseName(manifest.Name),
                    Campaign = campaign,
                    Files = files,
                });
        }
        catch (InvalidDataException)
        {
            return InvalidPackage();
        }
        catch (JsonException)
        {
            return InvalidPackage();
        }
    }

    private static StoredCampaign ToCampaign(string name, byte[] catalogBytes, byte[] settingsBytes, byte[]? overlayBytes)
    {
        var catalogJson = Encoding.UTF8.GetString(catalogBytes);
        var settingsJson = Encoding.UTF8.GetString(settingsBytes);
        var overlayJson = overlayBytes is null ? null : Encoding.UTF8.GetString(overlayBytes);
        var (TerrainTypes, StructureTypes, ItemObjectiveTypes, PublicObjectiveTypes, BattleScoring, RankingObjectivePoints, SpecialRules, PrivateObjectiveTypes, FactionSpecialRuleIds, SubfactionSpecialRuleIds, ForceStatuses, SplitForceSupplyPenaltyPercent, SplitForceSupplyPenaltyIsPercent, BattleReportRules, ArmyEscalations, Missions) = CatalogJson.Deserialize(catalogJson);
        var settings = CampaignPresetSettingsJson.Deserialize(settingsJson);
        var created = DateTimeOffset.UnixEpoch;
        return new StoredCampaign
        {
            Id = Guid.Empty,
            Name = CampaignSetupRules.CollapseName(name),
            Description = settings.Description,
            PlayerSlotCount = Math.Max(2, settings.PlayerSlotCount),
            IsPrivate = false,
            IsPubliclyViewable = true,
            CreatorIsParticipant = settings.CreatorIsParticipant,
            MapStorageKey = null,
            Revision = 1,
            CreatedUtc = created,
            UpdatedUtc = created,
            CreatedByUserId = Guid.Empty,
            Memberships = [],
            Factions =
            [
                .. settings.Factions.Select(faction => new StoredFaction
                {
                    Id = faction.Id,
                    Name = faction.Name,
                    Color = faction.Color,
                    Subfactions = faction.Subfactions,
                    SubfactionAppearances = faction.SubfactionAppearances,
                    AllyGroupName = faction.AllyGroupName,
                    RequiresSubfaction = faction.RequiresSubfaction,
                    FlagImageStorageKey = faction.FlagImageStorageKey,
                    TintFlagImage = faction.TintFlagImage,
                    SpecialRuleIds = faction.SpecialRuleIds.Count > 0
                        ? faction.SpecialRuleIds
                        : FactionSpecialRuleIds.GetValueOrDefault(faction.Id) ?? [],
                    SubfactionSpecialRules = faction.SubfactionSpecialRules.Count > 0
                        ? faction.SubfactionSpecialRules
                        : SubfactionSpecialRuleIds.GetValueOrDefault(faction.Id) ?? [],
                }),
            ],
            AllyGroups = settings.AllyGroups,
            Links = settings.Links,
            TimeZoneId = string.IsNullOrWhiteSpace(settings.TimeZoneId) ? "UTC" : settings.TimeZoneId,
            StartsUtc = created,
            EndsUtc = created,
            RoundCount = Math.Max(3, settings.RoundCount),
            RoundLengthAmount = Math.Max(1, settings.RoundLengthAmount),
            RoundLengthUnit = string.IsNullOrWhiteSpace(settings.RoundLengthUnit) ? "Weeks" : settings.RoundLengthUnit,
            Phases = settings.Phases,
            MapGraph = MapGraphJson.Deserialize(overlayJson),
            TerrainTypes = TerrainTypes,
            StructureTypes = StructureTypes,
            ItemObjectiveTypes = ItemObjectiveTypes,
            PublicObjectiveTypes = PublicObjectiveTypes,
            SpecialRules = SpecialRules,
            ForceStatuses = ForceStatuses,
            PrivateObjectiveTypes = PrivateObjectiveTypes,
            BattleScoring = BattleScoring,
            RankingObjectivePoints = RankingObjectivePoints,
            SplitForceSupplyPenaltyPercent = SplitForceSupplyPenaltyPercent,
            SplitForceSupplyPenaltyIsPercent = SplitForceSupplyPenaltyIsPercent,
            BattleReportRules = BattleReportRules,
            ArmyEscalations = ArmyEscalations,
            Missions = Missions,
        };
    }

    private static void WriteEntry(ZipArchive zip, string name, byte[] bytes)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string? NormalizeEntryName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        var name = fullName.Replace('\\', '/').TrimStart('/');
        if (name.Contains("..", StringComparison.Ordinal) || name.EndsWith('/'))
        {
            return null;
        }

        return name;
    }

    private static bool IsPackageFileKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var slash = storageKey.IndexOf('/', StringComparison.Ordinal);
        if (slash <= 0 || slash != storageKey.LastIndexOf('/'))
        {
            return false;
        }

        return storageKey[..slash] is "maps" or "structures" or "flags" or "missions" or "items";
    }

    private static OperationResult<CampaignPresetPackageContents> InvalidPackage()
    {
        return OperationResults.Failure<CampaignPresetPackageContents>(
            ErrorCodes.CampaignPresetPackageInvalid,
            "The campaign preset file is not a valid Map & Muster package.");
    }

    private static string WriteOverlaySvg(StoredMapGraph graph)
    {
        var builder = new StringBuilder();
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\">");
        foreach (var territory in graph.Territories)
        {
            if (territory.Polygon.Count < 3)
            {
                continue;
            }

            var fill = OverlayFill(territory.OverlayColor);
            builder.Append("<polygon fill=\"");
            builder.Append(fill);
            builder.Append("\" stroke=\"#1c1917\" stroke-width=\"0.002\" points=\"");
            builder.Append(
                string.Join(
                    ' ',
                    territory.Polygon.Select(static point =>
                        string.Create(CultureInfo.InvariantCulture, $"{point.X},{point.Y}"))));
            builder.Append("\" />");
        }

        builder.Append("</svg>");
        return builder.ToString();
    }

    private static string OverlayFill(string? color)
    {
        if (string.IsNullOrWhiteSpace(color) || color.Length != 7 || color[0] != '#')
        {
            return "rgba(120,113,108,0.35)";
        }

        for (var index = 1; index < color.Length; index++)
        {
            if (!Uri.IsHexDigit(color[index]))
            {
                return "rgba(120,113,108,0.35)";
            }
        }

        return WebUtility.HtmlEncode(color);
    }

    private sealed class ManifestDocument
    {
        public string Format { get; set; } = string.Empty;

        public int Version { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
