namespace Campaign.Infrastructure.Email;

/// <summary>
/// A queued email or other external side-effect written in the same database as account changes.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the message type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON payload. Tokens in this payload must not be logged.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the message was queued, in UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message was processed, in UTC.
    /// </summary>
    public DateTimeOffset? ProcessedUtc { get; set; }

    /// <summary>
    /// Gets or sets the last processing error, if any.
    /// </summary>
    public string? LastError { get; set; }
}
