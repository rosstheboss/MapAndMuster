using Campaign.Domain.Campaigns;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Command to create a campaign and make the caller its manager.
/// </summary>
public sealed class CreateCampaignCommand
{
    /// <summary>Gets the authenticated user creating the campaign.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerCount { get; init; }

    /// <summary>Gets whether a join password is required.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether non-members may view the campaign.</summary>
    public required bool IsPubliclyViewable { get; init; }

    /// <summary>Gets the join password for a private campaign.</summary>
    public string? JoinPassword { get; init; }

    /// <summary>Gets whether the creator also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the factions.</summary>
    public required IReadOnlyList<FactionInput> Factions { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public IReadOnlyList<AllyGroupInput>? AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public IReadOnlyList<CampaignLinkInput>? Links { get; init; }

    /// <summary>Gets the round schedule.</summary>
    public required CampaignScheduleInput Schedule { get; init; }

    /// <summary>Gets the terrain types. Defaults are used when omitted.</summary>
    public IReadOnlyList<TerrainTypeInput>? TerrainTypes { get; init; }

    /// <summary>Gets the structure types. Defaults are used when omitted.</summary>
    public IReadOnlyList<StructureTypeInput>? StructureTypes { get; init; }
}

/// <summary>
/// Command to replace campaign setup fields. The join password may be omitted to keep the current hash.
/// </summary>
public sealed class UpdateCampaignCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the campaign name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the configured player-slot count.</summary>
    public required int PlayerCount { get; init; }

    /// <summary>Gets whether a join password is required.</summary>
    public required bool IsPrivate { get; init; }

    /// <summary>Gets whether non-members may view the campaign.</summary>
    public required bool IsPubliclyViewable { get; init; }

    /// <summary>Gets the replacement join password, or null to keep the current hash.</summary>
    public string? JoinPassword { get; init; }

    /// <summary>Gets whether the creator also occupies a player slot.</summary>
    public required bool CreatorIsParticipant { get; init; }

    /// <summary>Gets the optional city.</summary>
    public string? City { get; init; }

    /// <summary>Gets the optional state, province, or region.</summary>
    public string? Region { get; init; }

    /// <summary>Gets the optional country.</summary>
    public string? Country { get; init; }

    /// <summary>Gets the factions.</summary>
    public required IReadOnlyList<FactionInput> Factions { get; init; }

    /// <summary>Gets the ally groups.</summary>
    public IReadOnlyList<AllyGroupInput>? AllyGroups { get; init; }

    /// <summary>Gets the external links.</summary>
    public IReadOnlyList<CampaignLinkInput>? Links { get; init; }

    /// <summary>Gets the round schedule.</summary>
    public required CampaignScheduleInput Schedule { get; init; }

    /// <summary>Gets the terrain types. Defaults are used when omitted.</summary>
    public IReadOnlyList<TerrainTypeInput>? TerrainTypes { get; init; }

    /// <summary>Gets the structure types. Defaults are used when omitted.</summary>
    public IReadOnlyList<StructureTypeInput>? StructureTypes { get; init; }
}

/// <summary>
/// Command to replace a campaign map image.
/// </summary>
public sealed class UploadCampaignMapCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the uploaded map stream.</summary>
    public required Stream Content { get; init; }

    /// <summary>Gets the declared content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>Gets the declared length, if known.</summary>
    public long? Length { get; init; }
}

/// <summary>
/// Command to join a campaign as a player.
/// </summary>
public sealed class JoinCampaignCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the join password for a private campaign.</summary>
    public string? JoinPassword { get; init; }
}

/// <summary>
/// Command to post a public chat message in a campaign log.
/// </summary>
public sealed class PostCampaignChatCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets whether the caller is a system administrator.</summary>
    public required bool IsAdministrator { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }

    /// <summary>Gets the last observed campaign revision.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets the chat message.</summary>
    public required string Message { get; init; }
}

/// <summary>
/// Command to leave a campaign the caller plays in but does not manage.
/// </summary>
public sealed class LeaveCampaignCommand
{
    /// <summary>Gets the authenticated user.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the campaign identifier.</summary>
    public required Guid CampaignId { get; init; }
}

/// <summary>
/// Command to copy a campaign's setup, map overlay, and catalog while sharing stored art files.
/// </summary>
public sealed class DuplicateCampaignCommand
{
    /// <summary>Gets the authenticated user creating the copy.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the source campaign identifier.</summary>
    public required Guid CampaignId { get; init; }
}
