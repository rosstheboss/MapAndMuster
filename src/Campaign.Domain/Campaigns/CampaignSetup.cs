namespace Campaign.Domain.Campaigns;

/// <summary>
/// Validated campaign configuration collected during setup or a later manager edit.
/// </summary>
public sealed class CampaignSetup
{
    /// <summary>
    /// Initializes a validated campaign setup.
    /// </summary>
    /// <param name="name">The campaign name.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="playerSlotCount">The configured player-slot count.</param>
    /// <param name="isPrivate">Whether a join password is required.</param>
    /// <param name="creatorIsParticipant">Whether the creating manager also occupies a player slot.</param>
    /// <param name="factions">The factions.</param>
    /// <param name="allyGroups">The ally groups.</param>
    /// <param name="links">The external links.</param>
    public CampaignSetup(
        string name,
        string? description,
        int playerSlotCount,
        bool isPrivate,
        bool creatorIsParticipant,
        IReadOnlyList<FactionSetup> factions,
        IReadOnlyList<AllyGroupSetup> allyGroups,
        IReadOnlyList<CampaignExternalLink> links)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factions);
        ArgumentNullException.ThrowIfNull(allyGroups);
        ArgumentNullException.ThrowIfNull(links);
        Name = name;
        Description = description;
        PlayerSlotCount = playerSlotCount;
        IsPrivate = isPrivate;
        CreatorIsParticipant = creatorIsParticipant;
        Factions = factions;
        AllyGroups = allyGroups;
        Links = links;
    }

    /// <summary>Gets the campaign name.</summary>
    public string Name { get; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; }

    /// <summary>Gets the configured player-slot count.</summary>
    public int PlayerSlotCount { get; }

    /// <summary>Gets a value indicating whether a join password is required.</summary>
    public bool IsPrivate { get; }

    /// <summary>Gets a value indicating whether the creating manager also occupies a player slot.</summary>
    public bool CreatorIsParticipant { get; }

    /// <summary>Gets the factions.</summary>
    public IReadOnlyList<FactionSetup> Factions { get; }

    /// <summary>Gets the ally groups.</summary>
    public IReadOnlyList<AllyGroupSetup> AllyGroups { get; }

    /// <summary>Gets the external links.</summary>
    public IReadOnlyList<CampaignExternalLink> Links { get; }
}
