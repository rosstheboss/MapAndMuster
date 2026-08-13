using Campaign.Application.Ports;

namespace Campaign.Infrastructure.Time;

/// <summary>
/// Clock adapter that reads <see cref="DateTimeOffset.UtcNow"/>.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
