namespace MapAndMuster.Infrastructure.Campaigns;

/// <summary>
/// Wakes the phase-deadline worker after a campaign write, so a newly created, extended, or
/// early-closed phase window is picked up without waiting for the worker's idle ceiling.
/// </summary>
public sealed class CampaignDeadlineSignal : WakeSignal
{
}
