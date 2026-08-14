namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied mission configuration nested under a terrain type or structure.
/// </summary>
public sealed class MissionInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the mission name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets an optional http or https link for the mission.</summary>
    public string? Url { get; init; }

    /// <summary>Gets whether an existing uploaded file should be removed.</summary>
    public bool ClearFile { get; init; }
}
