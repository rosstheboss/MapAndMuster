namespace Campaign.Domain.Play;

/// <summary>
/// Campaign catalog flags used to validate Build, Pillage, and destroy-on-pillage.
/// </summary>
public sealed class StructureTypePlayRules
{
    /// <summary>
    /// Initializes structure play rules.
    /// </summary>
    public StructureTypePlayRules(
        Guid id,
        string name,
        bool isBuildable,
        bool isPillageable,
        bool isDestructible)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
        IsBuildable = isBuildable;
        IsPillageable = isPillageable;
        IsDestructible = isDestructible;
    }

    /// <summary>Gets the structure type identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the display name.</summary>
    public string Name { get; }

    /// <summary>Gets whether players may Build this structure.</summary>
    public bool IsBuildable { get; }

    /// <summary>Gets whether players may Pillage this structure.</summary>
    public bool IsPillageable { get; }

    /// <summary>Gets whether a second Pillage may destroy and remove this structure.</summary>
    public bool IsDestructible { get; }
}
