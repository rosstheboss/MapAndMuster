namespace MapAndMuster.Application.Ports;

/// <summary>
/// Supplies the authoritative clock used by application use cases.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC instant.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
