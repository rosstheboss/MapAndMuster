using System.Text.Json;
using MapAndMuster.Domain.Campaigns;

namespace MapAndMuster.Infrastructure.Campaigns;

/// <summary>
/// Stores a faction's preferred terrain and structures on the faction row.
/// </summary>
internal static class FactionPreferenceJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string? Serialize(FactionPreference preference)
    {
        ArgumentNullException.ThrowIfNull(preference);
        if (preference.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(new Document(
            preference.TerrainTypeIds.ToArray(),
            preference.TerrainTagIds.ToArray(),
            preference.StructureTypeIds.ToArray(),
            preference.StructureTagIds.ToArray()), Options);
    }

    public static FactionPreference Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return FactionPreference.None;
        }

        var document = JsonSerializer.Deserialize<Document>(json, Options);
        if (document is null)
        {
            return FactionPreference.None;
        }

        return new FactionPreference(
            document.TerrainTypeIds,
            document.TerrainTagIds,
            document.StructureTypeIds,
            document.StructureTagIds);
    }

    private sealed record Document(
        Guid[]? TerrainTypeIds,
        Guid[]? TerrainTagIds,
        Guid[]? StructureTypeIds,
        Guid[]? StructureTagIds);
}
