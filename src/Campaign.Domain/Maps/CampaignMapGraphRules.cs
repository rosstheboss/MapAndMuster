using System.Diagnostics.CodeAnalysis;
using Campaign.Domain.Campaigns;
using Campaign.Domain.Common;
using Campaign.Domain.Identity;
using Campaign.Domain.Play;

namespace Campaign.Domain.Maps;

/// <summary>
/// Validates overlay territories and explicit adjacencies without failing on the first error.
/// </summary>
public static class CampaignMapGraphRules
{
    /// <summary>Maximum territories on one campaign map.</summary>
    public const int MaxTerritoryCount = 500;

    /// <summary>Maximum vertices in one territory polygon.</summary>
    public const int MaxPolygonVertices = 256;

    /// <summary>Maximum territory name length.</summary>
    public const int NameMaxLength = CampaignSetupRules.NamedItemMaxLength;

    /// <summary>Maximum territory description length.</summary>
    public const int DescriptionMaxLength = CampaignSetupRules.DescriptionMaxLength;

    /// <summary>
    /// Validates a map graph. Known faction identifiers are the campaign's current factions.
    /// </summary>
    /// <param name="territories">The territory inputs.</param>
    /// <param name="adjacencies">The adjacency inputs.</param>
    /// <param name="knownFactionIds">Faction identifiers that may own a territory or use it as a spawn.</param>
    /// <param name="knownTerrainTypeIds">Terrain type identifiers from the campaign catalog.</param>
    /// <param name="knownStructureTypeIds">Structure type identifiers from the campaign catalog.</param>
    /// <param name="graph">The validated graph when successful.</param>
    /// <param name="errors">Every field error, in a stable order.</param>
    /// <returns><see langword="true"/> when the graph is valid.</returns>
    public static bool TryCreate(
        IReadOnlyList<TerritoryInput>? territories,
        IReadOnlyList<AdjacencyInput>? adjacencies,
        IReadOnlySet<Guid> knownFactionIds,
        IReadOnlySet<Guid> knownTerrainTypeIds,
        IReadOnlySet<Guid> knownStructureTypeIds,
        [NotNullWhen(true)] out CampaignMapGraph? graph,
        out IReadOnlyList<DomainError> errors)
    {
        ArgumentNullException.ThrowIfNull(knownFactionIds);
        ArgumentNullException.ThrowIfNull(knownTerrainTypeIds);
        ArgumentNullException.ThrowIfNull(knownStructureTypeIds);
        var collected = new List<DomainError>();
        graph = null;

        var parsedTerritories = ParseTerritories(
            territories,
            knownFactionIds,
            knownTerrainTypeIds,
            knownStructureTypeIds,
            collected);
        var parsedAdjacencies = ParseAdjacencies(adjacencies, parsedTerritories, collected);

        if (collected.Count > 0)
        {
            errors = collected;
            return false;
        }

        graph = new CampaignMapGraph(parsedTerritories, parsedAdjacencies);
        errors = [];
        return true;
    }

    private static List<Territory> ParseTerritories(
        IReadOnlyList<TerritoryInput>? territories,
        IReadOnlySet<Guid> knownFactionIds,
        IReadOnlySet<Guid> knownTerrainTypeIds,
        IReadOnlySet<Guid> knownStructureTypeIds,
        List<DomainError> errors)
    {
        var parsed = new List<Territory>();
        if (territories is null || territories.Count == 0)
        {
            return parsed;
        }

        if (territories.Count > MaxTerritoryCount)
        {
            errors.Add(new DomainError(
                "territories.invalid",
                $"A campaign map can have at most {MaxTerritoryCount} territories.",
                "territories"));
            return parsed;
        }

        var usedIds = new HashSet<Guid>();
        var usedNumbers = new HashSet<int>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var spawnByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < territories.Count; index++)
        {
            var input = territories[index];
            var field = $"territories[{index}]";
            var id = input.Id is { } suppliedId && suppliedId != Guid.Empty ? suppliedId : Guid.NewGuid();
            if (!usedIds.Add(id))
            {
                errors.Add(new DomainError("territories.id.duplicate", "Territory identifiers must be unique.", field));
                continue;
            }

            var displayNumber = input.DisplayNumber > 0 ? input.DisplayNumber : index + 1;
            if (!usedNumbers.Add(displayNumber))
            {
                errors.Add(new DomainError(
                    "territories.displayNumber.duplicate",
                    "Territory numbers must be unique.",
                    $"{field}.displayNumber"));
            }

            var name = ParseOptionalName(input.Name, $"{field}.name", $"Territory {displayNumber} name", errors);
            if (name is not null && !usedNames.Add(name))
            {
                errors.Add(new DomainError(
                    "territories.name.duplicate",
                    "Territory names must be unique for the campaign.",
                    $"{field}.name"));
                name = null;
            }

            var description = ParseOptionalDescription(input.Description, $"{field}.description", errors);
            var polygon = ParsePolygon(input.Polygon, field, displayNumber, errors);
            Guid? terrainTypeId = null;
            if (input.TerrainTypeId is not { } suppliedTerrain || suppliedTerrain == Guid.Empty)
            {
                errors.Add(new DomainError(
                    "territories.terrain.required",
                    $"Territory {displayNumber} requires a terrain type.",
                    $"{field}.terrainTypeId"));
            }
            else if (!knownTerrainTypeIds.Contains(suppliedTerrain))
            {
                errors.Add(new DomainError(
                    "territories.terrain.invalid",
                    $"Territory {displayNumber} terrain is not a terrain type in this campaign.",
                    $"{field}.terrainTypeId"));
            }
            else
            {
                terrainTypeId = suppliedTerrain;
            }

            Guid? structureTypeId = null;
            if (input.StructureTypeId is { } suppliedStructure && suppliedStructure != Guid.Empty)
            {
                if (!knownStructureTypeIds.Contains(suppliedStructure))
                {
                    errors.Add(new DomainError(
                        "territories.structure.invalid",
                        $"Territory {displayNumber} structure is not a structure type in this campaign.",
                        $"{field}.structureTypeId"));
                }
                else
                {
                    structureTypeId = suppliedStructure;
                }
            }

            var overlayColor = ParseOverlayColor(input.OverlayColor, field, displayNumber, errors);
            var ownerSubfaction = ParseOptionalSubfaction(
                input.OwnerSubfaction,
                $"{field}.ownerSubfaction",
                $"Territory {displayNumber} owner subfaction",
                errors);
            var spawnSubfaction = ParseOptionalSubfaction(
                input.SpawnSubfaction,
                $"{field}.spawnSubfaction",
                $"Territory {displayNumber} spawn subfaction",
                errors);
            Guid? ownerFactionId = input.OwnerFactionId is { } suppliedOwner && suppliedOwner != Guid.Empty
                ? suppliedOwner
                : null;
            Guid? spawnFactionId = input.SpawnFactionId is { } suppliedSpawn && suppliedSpawn != Guid.Empty
                ? suppliedSpawn
                : null;

            if (ownerFactionId is { } ownerId && !knownFactionIds.Contains(ownerId))
            {
                errors.Add(new DomainError(
                    "territories.owner.invalid",
                    $"Territory {displayNumber} owner is not a faction in this campaign.",
                    $"{field}.ownerFactionId"));
                ownerFactionId = null;
                ownerSubfaction = null;
            }

            if (ownerSubfaction is not null && ownerFactionId is null)
            {
                errors.Add(new DomainError(
                    "territories.ownerSubfaction.invalid",
                    $"Territory {displayNumber} owner subfaction requires an owner faction.",
                    $"{field}.ownerSubfaction"));
                ownerSubfaction = null;
            }

            if (spawnSubfaction is not null && spawnFactionId is null)
            {
                errors.Add(new DomainError(
                    "territories.spawnSubfaction.invalid",
                    $"Territory {displayNumber} spawn subfaction requires a spawn faction.",
                    $"{field}.spawnSubfaction"));
                spawnSubfaction = null;
            }

            if (spawnFactionId is { } spawnId)
            {
                if (!knownFactionIds.Contains(spawnId))
                {
                    errors.Add(new DomainError(
                        "territories.spawn.invalid",
                        $"Territory {displayNumber} spawn faction is not a faction in this campaign.",
                        $"{field}.spawnFactionId"));
                    spawnFactionId = null;
                    spawnSubfaction = null;
                }
                else
                {
                    var spawnKey = SpawnKey(spawnId, spawnSubfaction);
                    if (spawnByKey.TryGetValue(spawnKey, out var otherNumber))
                    {
                        errors.Add(new DomainError(
                            "territories.spawn.duplicate",
                            spawnSubfaction is null
                                ? $"A faction can have only one spawn territory. Territories {otherNumber} and {displayNumber} both assign the same spawn."
                                : $"A required subfaction can have only one spawn territory. Territories {otherNumber} and {displayNumber} both assign the same spawn.",
                            $"{field}.spawnFactionId"));
                    }
                    else
                    {
                        spawnByKey[spawnKey] = displayNumber;
                    }

                    ownerFactionId = spawnId;
                    ownerSubfaction = spawnSubfaction;
                }
            }

            if (polygon is null || terrainTypeId is null)
            {
                continue;
            }

            parsed.Add(new Territory(
                id,
                displayNumber,
                name,
                description,
                polygon,
                terrainTypeId.Value,
                structureTypeId,
                overlayColor,
                ownerFactionId,
                spawnFactionId,
                ParseStructureCondition(input.StructureCondition, structureTypeId.HasValue, field, displayNumber, errors),
                ownerSubfaction,
                spawnSubfaction));
        }

        for (var i = 0; i < parsed.Count; i++)
        {
            for (var j = i + 1; j < parsed.Count; j++)
            {
                if (!PolygonGeometry.InteriorsOverlap(parsed[i].Polygon, parsed[j].Polygon))
                {
                    continue;
                }

                errors.Add(new DomainError(
                    "territories.overlap",
                    $"Territories {parsed[i].DisplayLabel} and {parsed[j].DisplayLabel} overlap. Shared borders are allowed.",
                    $"territories[{i}].polygon"));
            }
        }

        return parsed;
    }

    private static List<TerritoryAdjacency> ParseAdjacencies(
        IReadOnlyList<AdjacencyInput>? adjacencies,
        List<Territory> territories,
        List<DomainError> errors)
    {
        var parsed = new List<TerritoryAdjacency>();
        if (adjacencies is null || adjacencies.Count == 0)
        {
            return parsed;
        }

        var territoryIds = territories.Select(static territory => territory.Id).ToHashSet();
        var usedPairs = new HashSet<(Guid A, Guid B)>();
        var usedIds = new HashSet<Guid>();
        for (var index = 0; index < adjacencies.Count; index++)
        {
            var input = adjacencies[index];
            var field = $"adjacencies[{index}]";
            if (input.TerritoryAId == input.TerritoryBId)
            {
                errors.Add(new DomainError(
                    "adjacencies.invalid",
                    "An adjacency requires two distinct territories.",
                    field));
                continue;
            }

            if (!territoryIds.Contains(input.TerritoryAId) || !territoryIds.Contains(input.TerritoryBId))
            {
                errors.Add(new DomainError(
                    "adjacencies.territory.invalid",
                    "Adjacency arrows must connect territories on this map.",
                    field));
                continue;
            }

            if (!Enum.TryParse<AdjacencyOrigin>(input.Origin, ignoreCase: true, out var origin) || !Enum.IsDefined(origin))
            {
                errors.Add(new DomainError(
                    "adjacencies.origin.invalid",
                    "Adjacency origin must be Generated or Manual.",
                    $"{field}.origin"));
                origin = AdjacencyOrigin.Manual;
            }

            var id = input.Id is { } suppliedId && suppliedId != Guid.Empty ? suppliedId : Guid.NewGuid();
            if (!usedIds.Add(id))
            {
                id = Guid.NewGuid();
            }

            var edge = new TerritoryAdjacency(
                id,
                input.TerritoryAId,
                input.TerritoryBId,
                origin,
                new MapPoint(input.MarkerX, input.MarkerY));
            var pair = (edge.TerritoryAId, edge.TerritoryBId);
            if (!usedPairs.Add(pair))
            {
                errors.Add(new DomainError(
                    "adjacencies.duplicate",
                    "Each pair of territories can have only one adjacency.",
                    field));
                continue;
            }

            parsed.Add(edge);
        }

        return parsed;
    }

    private static List<MapPoint>? ParsePolygon(
        IReadOnlyList<MapPointInput>? polygon,
        string field,
        int displayNumber,
        List<DomainError> errors)
    {
        if (polygon is null || polygon.Count < 3)
        {
            errors.Add(new DomainError(
                "territories.polygon.invalid",
                $"Territory {displayNumber} needs at least three points.",
                $"{field}.polygon"));
            return null;
        }

        if (polygon.Count > MaxPolygonVertices)
        {
            errors.Add(new DomainError(
                "territories.polygon.invalid",
                $"Territory {displayNumber} has too many points (maximum {MaxPolygonVertices}).",
                $"{field}.polygon"));
            return null;
        }

        var points = new List<MapPoint>(polygon.Count);
        for (var index = 0; index < polygon.Count; index++)
        {
            var vertex = new MapPoint(polygon[index].X, polygon[index].Y);
            if (!vertex.IsOnMap)
            {
                errors.Add(new DomainError(
                    "territories.polygon.bounds",
                    $"Territory {displayNumber} cannot extend past the map image.",
                    $"{field}.polygon[{index}]"));
                continue;
            }

            points.Add(vertex);
        }

        if (points.Count >= 2 && points[0].DistanceSquaredTo(points[^1]) <= PolygonGeometry.Epsilon * PolygonGeometry.Epsilon)
        {
            points.RemoveAt(points.Count - 1);
        }

        if (!PolygonGeometry.IsValidTerritoryPolygon(points))
        {
            errors.Add(new DomainError(
                "territories.polygon.invalid",
                $"Territory {displayNumber} must be a closed region that does not cross itself.",
                $"{field}.polygon"));
            return null;
        }

        return points;
    }

    private static string? ParseOptionalName(string? raw, string field, string label, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            errors.Add(new DomainError(
                "territories.name.invalid",
                $"{label} is too long (maximum {NameMaxLength} characters).",
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

    private static string? ParseOptionalSubfaction(string? raw, string field, string label, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            errors.Add(new DomainError(
                "territories.subfaction.invalid",
                $"{label} is too long (maximum {NameMaxLength} characters).",
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

    private static string SpawnKey(Guid factionId, string? subfaction)
    {
        return string.IsNullOrEmpty(subfaction)
            ? factionId.ToString("N")
            : $"{factionId:N}:{subfaction}";
    }

    private static string? ParseOptionalDescription(string? raw, string field, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > DescriptionMaxLength)
        {
            errors.Add(new DomainError(
                "territories.description.invalid",
                $"Description is too long (maximum {DescriptionMaxLength} characters).",
                field));
            return null;
        }

        if (ProhibitedLanguage.ContainsProhibitedTerm(trimmed))
        {
            errors.Add(ProhibitedLanguage.ErrorFor(field, "Territory description"));
            return null;
        }

        return trimmed;
    }

    private static StructureCondition ParseStructureCondition(
        string? raw,
        bool hasStructure,
        string field,
        int displayNumber,
        List<DomainError> errors)
    {
        if (!hasStructure)
        {
            return StructureCondition.Operational;
        }

        if (string.IsNullOrWhiteSpace(raw)
            || string.Equals(raw, nameof(StructureCondition.Operational), StringComparison.OrdinalIgnoreCase))
        {
            return StructureCondition.Operational;
        }

        if (string.Equals(raw, nameof(StructureCondition.Pillaged), StringComparison.OrdinalIgnoreCase))
        {
            return StructureCondition.Pillaged;
        }

        if (string.Equals(raw, nameof(StructureCondition.Destroyed), StringComparison.OrdinalIgnoreCase))
        {
            return StructureCondition.Destroyed;
        }

        errors.Add(new DomainError(
            "territories.structureCondition.invalid",
            $"Territory {displayNumber} structure condition must be Operational, Pillaged, or Destroyed.",
            $"{field}.structureCondition"));
        return StructureCondition.Operational;
    }

    private static string? ParseOverlayColor(string? raw, string field, int displayNumber, List<DomainError> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!HexColor.TryNormalize(raw, out var color) || color is null)
        {
            errors.Add(new DomainError(
                "territories.overlayColor.invalid",
                $"Territory {displayNumber} overlay color must be a six-digit hex value.",
                $"{field}.overlayColor"));
            return null;
        }

        return color;
    }
}
