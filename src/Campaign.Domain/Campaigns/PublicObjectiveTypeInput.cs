namespace Campaign.Domain.Campaigns;

/// <summary>
/// User-supplied public campaign objective that is not tied to a territory, structure, or item.
/// </summary>
public sealed class PublicObjectiveTypeInput
{
    /// <summary>Gets the client-assigned identifier, when present.</summary>
    public Guid? Id { get; init; }

    /// <summary>Gets the objective name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets an optional short description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets campaign points awarded when this objective is completed. Defaults to 0.</summary>
    public int? CampaignPoints { get; init; }
}
