namespace Campaign.Infrastructure.Persistence.Entities;

/// <summary>
/// A named campaign setup snapshot, including catalog JSON and map assets.
/// </summary>
public sealed class CampaignPresetRecord
{
    /// <summary>Gets or sets the preset identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique case-insensitive name key.</summary>
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>Gets or sets terrain, structure, and scoring catalog JSON.</summary>
    public string? CatalogJson { get; set; }

    /// <summary>Gets or sets factions, ally groups, links, phases, and slot counts.</summary>
    public string? SettingsJson { get; set; }

    /// <summary>Gets or sets the overlay territory graph JSON.</summary>
    public string? MapGraphJson { get; set; }

    /// <summary>Gets or sets the generated map storage key.</summary>
    public string? MapStorageKey { get; set; }

    /// <summary>Gets or sets when the preset was created, in UTC.</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>Gets or sets when the preset was last saved, in UTC.</summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>Gets or sets the administrator who last saved the preset.</summary>
    public Guid CreatedByUserId { get; set; }
}
