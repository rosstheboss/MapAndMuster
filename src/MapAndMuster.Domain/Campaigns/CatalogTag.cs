namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// Which catalog a user-defined tag belongs to.
/// </summary>
public enum CatalogTagKind
{
    /// <summary>Assigned only to terrain types.</summary>
    Terrain = 0,

    /// <summary>Assigned only to structure types.</summary>
    Structure = 1,

    /// <summary>Assigned to factions and subfactions.</summary>
    Faction = 2,

    /// <summary>Assigned only to missions.</summary>
    Mission = 3,
}

/// <summary>
/// Limits and well-known names for campaign catalog tags.
/// </summary>
public static class CatalogTags
{
    /// <summary>Maximum tags in one kind's catalog.</summary>
    public const int MaxPerKind = 9999;

    /// <summary>Maximum tag name length.</summary>
    public const int NameMaxLength = 60;

    /// <summary>Terrain tag used in place of the former water-feature flag.</summary>
    public const string WaterName = "Water";

    /// <summary>
    /// Returns whether <paramref name="name"/> is the Water terrain tag.
    /// </summary>
    public static bool IsWater(string? name)
    {
        return string.Equals(name?.Trim(), WaterName, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// A user-defined catalog tag. Tags do nothing unless other rules reference them.
/// </summary>
public sealed class CatalogTag
{
    /// <summary>
    /// Initializes a validated catalog tag.
    /// </summary>
    public CatalogTag(Guid id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name.Trim();
    }

    /// <summary>Gets the tag identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the unique tag name within its kind.</summary>
    public string Name { get; }
}

/// <summary>
/// User-supplied catalog tag.
/// </summary>
public sealed class CatalogTagInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the tag name.</summary>
    public required string Name { get; init; }
}

/// <summary>
/// Tags assigned to one named subfaction, in addition to the parent faction's tags.
/// </summary>
public sealed class SubfactionTagsSetup
{
    /// <summary>
    /// Initializes validated extra tags for a named subfaction.
    /// </summary>
    public SubfactionTagsSetup(string name, IReadOnlyList<Guid> tagIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(tagIds);
        Name = name.Trim();
        TagIds = [.. tagIds.Where(static id => id != Guid.Empty).Distinct()];
    }

    /// <summary>Gets the subfaction name.</summary>
    public string Name { get; }

    /// <summary>Gets extra faction-catalog tags for this subfaction.</summary>
    public IReadOnlyList<Guid> TagIds { get; }
}

/// <summary>
/// User-supplied extra tags for one named subfaction.
/// </summary>
public sealed class SubfactionTagsInput
{
    /// <summary>Gets the subfaction name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets extra faction-catalog tag identifiers.</summary>
    public IReadOnlyList<Guid>? TagIds { get; init; }
}
