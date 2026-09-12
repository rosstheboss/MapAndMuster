namespace MapAndMuster.Infrastructure.Email;

/// <summary>
/// Wakes the outbox processor when a message is queued so it can idle on a long delay
/// instead of polling the database on a short fixed interval.
/// </summary>
public sealed class OutboxSignal : WakeSignal
{
}
