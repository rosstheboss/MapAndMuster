namespace Campaign.Domain.Campaigns;

/// <summary>
/// A validated public campaign objective with configured campaign points.
/// </summary>
public sealed class PublicObjectiveTypeSetup
{
    /// <summary>
    /// Initializes a validated public objective.
    /// </summary>
    /// <param name="id">The objective identifier.</param>
    /// <param name="name">The objective name.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="campaignPoints">Campaign points awarded on completion.</param>
    public PublicObjectiveTypeSetup(Guid id, string name, string? description, int campaignPoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(campaignPoints);
        Id = id;
        Name = name;
        Description = description;
        CampaignPoints = campaignPoints;
    }

    /// <summary>Gets the objective identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the objective name.</summary>
    public string Name { get; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; }

    /// <summary>Gets campaign points awarded when this objective is completed.</summary>
    public int CampaignPoints { get; }
}
