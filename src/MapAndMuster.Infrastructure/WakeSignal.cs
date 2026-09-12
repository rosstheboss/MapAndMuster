namespace MapAndMuster.Infrastructure;

/// <summary>
/// Lets a background service idle on a long delay and still react promptly when work arrives.
/// </summary>
/// <remarks>
/// The signal is advisory. A missed wake only delays processing until the next idle poll;
/// database state stays authoritative and a worker always re-reads it when it runs.
/// </remarks>
public abstract class WakeSignal : IDisposable
{
    private readonly SemaphoreSlim _pending = new(0, 1);
    private bool _disposed;

    /// <summary>
    /// Records that work may be waiting. Repeated calls coalesce into one wake.
    /// </summary>
    public void Notify()
    {
        try
        {
            _pending.Release();
        }
        catch (SemaphoreFullException)
        {
            // A wake is already pending; the worker re-reads all outstanding work when it runs.
        }
        catch (ObjectDisposedException)
        {
            // The host is shutting down.
        }
    }

    /// <summary>
    /// Waits for a wake or the supplied timeout, whichever comes first.
    /// </summary>
    /// <param name="timeout">Maximum time to idle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when work was signalled before the timeout.</returns>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return _pending.WaitAsync(timeout, cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the underlying wait handle.
    /// </summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (disposing)
        {
            _pending.Dispose();
        }
    }
}
