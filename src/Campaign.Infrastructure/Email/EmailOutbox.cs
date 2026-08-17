using System.Text.Json;
using Campaign.Application.Ports;
using Campaign.Infrastructure.Persistence;

namespace Campaign.Infrastructure.Email;

/// <summary>
/// Writes identity emails to the transactional outbox.
/// </summary>
public sealed class EmailOutbox : IEmailOutbox
{
    /// <summary>
    /// Outbox type for email confirmation messages.
    /// </summary>
    public const string ConfirmEmailType = "identity.confirm-email";

    /// <summary>
    /// Outbox type for password reset messages.
    /// </summary>
    public const string PasswordResetType = "identity.password-reset";

    /// <summary>
    /// Outbox type for campaign and chat notices. Bodies must not include secrets.
    /// </summary>
    public const string UserNoticeType = "campaign.user-notice";

    private readonly CampaignDbContext _dbContext;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new outbox.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="clock">The clock.</param>
    public EmailOutbox(CampaignDbContext dbContext, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(clock);
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc />
    public Task QueueEmailConfirmationAsync(string email, Guid userId, string token, CancellationToken cancellationToken)
    {
        return QueueAsync(ConfirmEmailType, email, userId, token, cancellationToken);
    }

    /// <inheritdoc />
    public Task QueuePasswordResetAsync(string email, Guid userId, string token, CancellationToken cancellationToken)
    {
        return QueueAsync(PasswordResetType, email, userId, token, cancellationToken);
    }

    /// <inheritdoc />
    public async Task QueueUserNoticeAsync(
        string email,
        Guid userId,
        string subject,
        string body,
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var payload = JsonSerializer.Serialize(new OutboxEmailPayload
        {
            Email = email,
            UserId = userId,
            Subject = subject,
            Body = body,
            Path = path,
        });

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = UserNoticeType,
            Payload = payload,
            CreatedUtc = _clock.UtcNow,
        });

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task QueueAsync(
        string type,
        string email,
        Guid userId,
        string token,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var payload = JsonSerializer.Serialize(new OutboxEmailPayload
        {
            Email = email,
            UserId = userId,
            Token = token,
        });

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            CreatedUtc = _clock.UtcNow,
        });

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// JSON payload for identity emails. Do not log this payload.
/// </summary>
public sealed class OutboxEmailPayload
{
    /// <summary>Gets or sets the recipient.</summary>
    public required string Email { get; init; }

    /// <summary>Gets or sets the user identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets or sets the secret token.</summary>
    public string? Token { get; init; }

    /// <summary>Gets or sets the notice subject.</summary>
    public string? Subject { get; init; }

    /// <summary>Gets or sets the notice body. Must not include hidden orders or private chat text.</summary>
    public string? Body { get; init; }

    /// <summary>Gets or sets the in-app path to open.</summary>
    public string? Path { get; init; }
}
