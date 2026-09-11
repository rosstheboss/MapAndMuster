using MapAndMuster.Domain.Common;
using MapAndMuster.Domain.Maps;

namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Parses catalog tags and type-scoped assignments.
/// </summary>
public static class CatalogTagRules
{
    /// <summary>
    /// Parses one catalog of tags. Names are unique case-insensitively.
    /// </summary>
    public static List<CatalogTag> Parse(
        IReadOnlyList<CatalogTagInput>? input,
        CatalogTagKind kind,
        HashSet<Guid> usedIds,
        List<DomainError> errors)
    {
        ArgumentNullException.ThrowIfNull(usedIds);
        ArgumentNullException.ThrowIfNull(errors);
        var supplied = input ?? [];
        var field = FieldFor(kind);
        var parsed = new List<CatalogTag>();
        if (supplied.Count > CatalogTags.MaxPerKind)
        {
            errors.Add(new DomainError(
                $"{field}.invalid",
                $"At most {CatalogTags.MaxPerKind} {LabelFor(kind)} tags are allowed.",
                field));
            return parsed;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < supplied.Count; index++)
        {
            var name = supplied[index].Name?.Trim() ?? string.Empty;
            if (name.Length is < 1 or > CatalogTags.NameMaxLength)
            {
                errors.Add(new DomainError(
                    $"{field}.name.invalid",
                    $"{LabelFor(kind)} tag {index + 1} name must be between 1 and {CatalogTags.NameMaxLength} characters.",
                    $"{field}[{index}].name"));
                continue;
            }

            if (!seenNames.Add(name))
            {
                errors.Add(new DomainError(
                    $"{field}.duplicate",
                    $"{LabelFor(kind)} tag names must be unique.",
                    $"{field}[{index}].name"));
                continue;
            }

            parsed.Add(new CatalogTag(
                ResolveTagId(supplied[index].Id, usedIds, $"{field}[{index}].id", errors),
                name));
        }

        return parsed;
    }

    /// <summary>
    /// Returns assigned tag ids that exist in <paramref name="catalogIds"/>. Unknown ids are dropped
    /// and reported.
    /// </summary>
    public static IReadOnlyList<Guid> ParseAssigned(
        IReadOnlyList<Guid>? tagIds,
        IReadOnlySet<Guid> catalogIds,
        string field,
        List<DomainError> errors)
    {
        ArgumentNullException.ThrowIfNull(catalogIds);
        ArgumentNullException.ThrowIfNull(errors);
        if (tagIds is null || tagIds.Count == 0)
        {
            return [];
        }

        var assigned = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var id in tagIds)
        {
            if (id == Guid.Empty || !seen.Add(id))
            {
                continue;
            }

            if (!catalogIds.Contains(id))
            {
                errors.Add(new DomainError(
                    $"{field}.unknown",
                    "Tags must be chosen from this section's catalog.",
                    field));
                continue;
            }

            assigned.Add(id);
        }

        return assigned;
    }

    /// <summary>
    /// Ensures a Water terrain tag exists and is assigned to terrains that still use the legacy water flag.
    /// </summary>
    public static Guid EnsureWaterTag(
        List<CatalogTag> terrainTags,
        IReadOnlyList<TerrainTypeInput> terrainInputs,
        HashSet<Guid> usedIds)
    {
        ArgumentNullException.ThrowIfNull(terrainTags);
        ArgumentNullException.ThrowIfNull(terrainInputs);
        ArgumentNullException.ThrowIfNull(usedIds);
        var existing = terrainTags.FirstOrDefault(static tag => CatalogTags.IsWater(tag.Name));
        if (existing is not null)
        {
            return existing.Id;
        }

        var needsWater = terrainInputs.Any(static input =>
            input.IsWaterFeature == true
            || TerrainCatalog.IsWaterFeature(input.Name));
        if (!needsWater && terrainTags.Count > 0)
        {
            return Guid.Empty;
        }

        if (!needsWater)
        {
            var water = new CatalogTag(NewId(usedIds), CatalogTags.WaterName);
            terrainTags.Add(water);
            return water.Id;
        }

        var created = new CatalogTag(NewId(usedIds), CatalogTags.WaterName);
        terrainTags.Add(created);
        return created.Id;
    }

    /// <summary>
    /// Assigns the Water tag when a terrain still carries the legacy water-feature flag.
    /// </summary>
    public static IReadOnlyList<Guid> WithWaterMigration(
        IReadOnlyList<Guid> tagIds,
        string terrainName,
        bool? isWaterFeature,
        Guid waterTagId)
    {
        if (waterTagId == Guid.Empty)
        {
            return tagIds;
        }

        var needsWater = isWaterFeature == true
            || (isWaterFeature is null && TerrainCatalog.IsWaterFeature(terrainName) && tagIds.Count == 0);
        if (!needsWater || tagIds.Contains(waterTagId))
        {
            return tagIds;
        }

        return [.. tagIds, waterTagId];
    }

    private static Guid ResolveTagId(Guid? id, HashSet<Guid> usedIds, string field, List<DomainError> errors)
    {
        if (id is { } supplied && supplied != Guid.Empty)
        {
            if (!usedIds.Add(supplied))
            {
                errors.Add(new DomainError($"{field}.duplicate", "Identifiers must be unique.", field));
                return NewId(usedIds);
            }

            return supplied;
        }

        return NewId(usedIds);
    }

    private static Guid NewId(HashSet<Guid> usedIds)
    {
        Guid id;
        do
        {
            id = Guid.NewGuid();
        }
        while (!usedIds.Add(id));

        return id;
    }

    private static string FieldFor(CatalogTagKind kind)
    {
        return kind switch
        {
            CatalogTagKind.Terrain => "terrainTags",
            CatalogTagKind.Structure => "structureTags",
            CatalogTagKind.Faction => "factionTags",
            CatalogTagKind.Mission => "missionTags",
            _ => "tags",
        };
    }

    private static string LabelFor(CatalogTagKind kind)
    {
        return kind switch
        {
            CatalogTagKind.Terrain => "terrain",
            CatalogTagKind.Structure => "structure",
            CatalogTagKind.Faction => "faction",
            CatalogTagKind.Mission => "mission",
            _ => "catalog",
        };
    }
}
