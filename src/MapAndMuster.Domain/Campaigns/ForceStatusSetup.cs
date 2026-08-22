namespace MapAndMuster.Domain.Campaigns;

/// <summary>
/// A validated force status other than Normal. Effects text is shown with the force; enable and
/// clear triggers are applied during resolution.
/// </summary>
public sealed class ForceStatusSetup
{
    /// <summary>
    /// Initializes a validated force status.
    /// </summary>
    /// <param name="id">The status identifier.</param>
    /// <param name="name">The unique status name.</param>
    /// <param name="effects">Tabletop effect text shown to players.</param>
    /// <param name="enableTrigger">When this status is applied.</param>
    /// <param name="clearTrigger">When this status returns to Normal.</param>
    public ForceStatusSetup(
        Guid id,
        string name,
        string effects,
        ForceStatusEnableTrigger enableTrigger,
        ForceStatusClearTrigger clearTrigger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(effects);
        Id = id;
        Name = name;
        Effects = effects;
        EnableTrigger = enableTrigger;
        ClearTrigger = clearTrigger;
    }

    /// <summary>Gets the status identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the unique status name.</summary>
    public string Name { get; }

    /// <summary>Gets tabletop effect text shown to players.</summary>
    public string Effects { get; }

    /// <summary>Gets when this status is applied.</summary>
    public ForceStatusEnableTrigger EnableTrigger { get; }

    /// <summary>Gets when this status returns to Normal.</summary>
    public ForceStatusClearTrigger ClearTrigger { get; }
}
