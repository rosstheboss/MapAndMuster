using Campaign.Application.Common;
using Campaign.Application.Ports;
using Campaign.Domain.Campaigns;

namespace Campaign.Application.Campaigns;

/// <summary>
/// Creates a campaign and records the caller as its manager, optionally occupying a player slot.
/// </summary>
public sealed class CreateCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly ISecretHasher _secrets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="secrets">The secret hasher.</param>
    public CreateCampaignHandler(ICampaignStore campaigns, IClock clock, ISecretHasher secrets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(secrets);
        _campaigns = campaigns;
        _clock = clock;
        _secrets = secrets;
    }

    /// <summary>
    /// Creates a campaign from validated setup input.
    /// </summary>
    /// <param name="command">The create command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        CreateCampaignCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!CampaignSetupRules.TryCreate(
                command.Name,
                command.Description,
                command.PlayerCount,
                command.IsPrivate,
                command.JoinPassword,
                joinPasswordRequired: command.IsPrivate,
                command.CreatorIsParticipant,
                occupiedPlayerSlotsExcludingCreator: 0,
                command.Factions,
                command.AllyGroups,
                command.Links,
                command.Schedule,
                out var setup,
                out var joinPassword,
                out var errors))
        {
            return OperationResults.Failure<CampaignDetail>(errors);
        }

        var now = _clock.UtcNow;
        var stored = CampaignPersistenceFactory.FromSetup(
            Guid.NewGuid(),
            setup,
            joinPasswordHash: joinPassword is null ? null : _secrets.Hash(joinPassword),
            mapStorageKey: null,
            revision: 1,
            createdUtc: now,
            updatedUtc: now,
            createdByUserId: command.UserId);

        var created = await _campaigns.AddAsync(stored, cancellationToken).ConfigureAwait(false);
        return OperationResults.Success(CampaignMapper.ToDetail(created, command.UserId, now));
    }
}

/// <summary>
/// Replaces campaign setup for a manager. Original memberships other than the creator's player flag are preserved.
/// </summary>
public sealed class UpdateCampaignHandler
{
    private readonly ICampaignStore _campaigns;
    private readonly IClock _clock;
    private readonly ISecretHasher _secrets;

    /// <summary>
    /// Initializes a new handler.
    /// </summary>
    /// <param name="campaigns">The campaign store.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="secrets">The secret hasher.</param>
    public UpdateCampaignHandler(ICampaignStore campaigns, IClock clock, ISecretHasher secrets)
    {
        ArgumentNullException.ThrowIfNull(campaigns);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(secrets);
        _campaigns = campaigns;
        _clock = clock;
        _secrets = secrets;
    }

    /// <summary>
    /// Updates campaign setup when the caller is a manager and the revision matches.
    /// </summary>
    /// <param name="command">The update command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated campaign detail.</returns>
    public async Task<OperationResult<CampaignDetail>> HandleAsync(
        UpdateCampaignCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _campaigns.FindByIdAsync(command.CampaignId, cancellationToken).ConfigureAwait(false);
        var membership = existing is null ? null : CampaignMapper.MembershipFor(existing, command.UserId);
        if (existing is null || membership is null)
        {
            return OperationResults.Failure<CampaignDetail>(ErrorCodes.CampaignNotFound, "The campaign was not found.");
        }

        if (!membership.IsGameMaster)
        {
            return OperationResults.Failure<CampaignDetail>(
                ErrorCodes.CampaignForbidden,
                "Only a campaign manager can change this campaign.");
        }

        var occupiedExcludingCreator = existing.Memberships.Count(member =>
            member.IsPlayer && member.UserId != existing.CreatedByUserId);

        if (!CampaignSetupRules.TryCreate(
                command.Name,
                command.Description,
                command.PlayerCount,
                command.IsPrivate,
                command.JoinPassword,
                joinPasswordRequired: command.IsPrivate && string.IsNullOrWhiteSpace(existing.JoinPasswordHash),
                command.CreatorIsParticipant,
                occupiedExcludingCreator,
                command.Factions,
                command.AllyGroups,
                command.Links,
                command.Schedule,
                out var setup,
                out var joinPassword,
                out var errors))
        {
            return OperationResults.Failure<CampaignDetail>(errors);
        }

        var joinPasswordHash = existing.JoinPasswordHash;
        if (!setup.IsPrivate)
        {
            joinPasswordHash = null;
        }
        else if (joinPassword is not null)
        {
            joinPasswordHash = _secrets.Hash(joinPassword);
        }

        var memberships = existing.Memberships
            .Select(member => member.UserId == existing.CreatedByUserId
                ? new StoredCampaignMembership
                {
                    UserId = member.UserId,
                    IsGameMaster = true,
                    IsPlayer = setup.CreatorIsParticipant,
                }
                : member)
            .ToArray();

        if (memberships.All(member => member.UserId != existing.CreatedByUserId))
        {
            memberships =
            [
                .. memberships,
                new StoredCampaignMembership
                {
                    UserId = existing.CreatedByUserId,
                    IsGameMaster = true,
                    IsPlayer = setup.CreatorIsParticipant,
                },
            ];
        }

        var updated = CampaignPersistenceFactory.FromSetup(
            existing.Id,
            setup,
            joinPasswordHash,
            existing.MapStorageKey,
            existing.Revision,
            existing.CreatedUtc,
            _clock.UtcNow,
            existing.CreatedByUserId,
            memberships);

        var outcome = await _campaigns
            .UpdateAsync(updated, command.ExpectedRevision, cancellationToken)
            .ConfigureAwait(false);
        if (!outcome.IsSuccess || outcome.Campaign is null)
        {
            return OperationResults.Failure<CampaignDetail>(
                outcome.ErrorCode ?? ErrorCodes.CampaignNotFound,
                outcome.Message ?? "The campaign could not be updated.");
        }

        return OperationResults.Success(CampaignMapper.ToDetail(outcome.Campaign, command.UserId, _clock.UtcNow));
    }
}

/// <summary>
/// Builds a stored campaign from validated setup.
/// </summary>
internal static class CampaignPersistenceFactory
{
    public static StoredCampaign FromSetup(
        Guid campaignId,
        CampaignSetup setup,
        string? joinPasswordHash,
        string? mapStorageKey,
        int revision,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc,
        Guid createdByUserId,
        IReadOnlyList<StoredCampaignMembership>? memberships = null)
    {
        var allyGroups = setup.AllyGroups
            .Select(group => new StoredAllyGroup { Id = Guid.NewGuid(), Name = group.Name })
            .ToArray();

        return new StoredCampaign
        {
            Id = campaignId,
            Name = setup.Name,
            Description = setup.Description,
            PlayerSlotCount = setup.PlayerSlotCount,
            IsPrivate = setup.IsPrivate,
            JoinPasswordHash = joinPasswordHash,
            CreatorIsParticipant = setup.CreatorIsParticipant,
            MapStorageKey = mapStorageKey,
            Revision = revision,
            CreatedUtc = createdUtc,
            UpdatedUtc = updatedUtc,
            CreatedByUserId = createdByUserId,
            Memberships = memberships ??
            [
                new StoredCampaignMembership
                {
                    UserId = createdByUserId,
                    IsGameMaster = true,
                    IsPlayer = setup.CreatorIsParticipant,
                },
            ],
            Factions =
            [
                .. setup.Factions.Select(faction => new StoredFaction
                {
                    Id = Guid.NewGuid(),
                    Name = faction.Name,
                    Subfactions = faction.Subfactions,
                    AllyGroupName = faction.AllyGroupName,
                }),
            ],
            AllyGroups = allyGroups,
            Links =
            [
                .. setup.Links.Select(link => new StoredCampaignLink
                {
                    Id = Guid.NewGuid(),
                    Label = link.Label,
                    Url = link.Url,
                }),
            ],
            TimeZoneId = setup.Schedule.TimeZone.Id,
            StartsUtc = setup.Schedule.StartsUtc,
            EndsUtc = setup.Schedule.EndsUtc,
            RoundCount = setup.Schedule.RoundCount,
            RoundLengthAmount = setup.Schedule.RoundLength.Amount,
            RoundLengthUnit = setup.Schedule.RoundLength.Unit.ToString(),
            Phases =
            [
                .. setup.Schedule.Phases.Select(phase => new StoredRoundPhase
                {
                    Kind = phase.Kind.ToString(),
                    DurationAmount = phase.Duration.Amount,
                    DurationUnit = phase.Duration.Unit.ToString(),
                }),
            ],
        };
    }
}
