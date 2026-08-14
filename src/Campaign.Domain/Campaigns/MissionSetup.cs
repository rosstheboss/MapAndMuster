namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated mission nested under a terrain type or structure.
/// </summary>
public sealed class MissionSetup
{
    /// <summary>
    /// Initializes a validated mission.
    /// </summary>
    /// <param name="id">The mission identifier.</param>
    /// <param name="name">The mission name.</param>
    /// <param name="url">The optional http or https link.</param>
    /// <param name="clearFile">Whether an existing uploaded file should be removed.</param>
    public MissionSetup(Guid id, string name, string? url, bool clearFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
        Url = url;
        ClearFile = clearFile;
    }

    /// <summary>Gets the mission identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the mission name.</summary>
    public string Name { get; }

    /// <summary>Gets the optional http or https link.</summary>
    public string? Url { get; }

    /// <summary>Gets whether an existing uploaded file should be removed.</summary>
    public bool ClearFile { get; }
}
