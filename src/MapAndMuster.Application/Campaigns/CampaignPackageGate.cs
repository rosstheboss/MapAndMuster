namespace MapAndMuster.Application.Campaigns;

/// <summary>
/// Bounds how many preset packages are built or unpacked at once.
/// </summary>
/// <remarks>
/// Rate limiting caps requests per caller, but several administrators, or one administrator with
/// several tabs, can still start concurrent 64 MB packages. Each one holds file handles and runs
/// image re-encoding, so on a small instance they compete for the same CPU and disk. Serializing
/// them keeps a burst slow rather than making the whole site slow.
/// </remarks>
public sealed class CampaignPackageGate : IDisposable
{
    /// <summary>How many package operations may run at once.</summary>
    private const int MaxConcurrent = 2;

    private readonly SemaphoreSlim _slots = new(MaxConcurrent, MaxConcurrent);

    /// <summary>
    /// Waits for a slot.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token. A caller that disconnects frees its place in line.</param>
    /// <returns>A handle that releases the slot when disposed.</returns>
    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await _slots.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Slot(_slots);
    }

    /// <inheritdoc />
    public void Dispose() => _slots.Dispose();

    private sealed class Slot : IDisposable
    {
        private readonly SemaphoreSlim _slots;
        private bool _released;

        public Slot(SemaphoreSlim slots) => _slots = slots;

        public void Dispose()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            _slots.Release();
        }
    }
}
