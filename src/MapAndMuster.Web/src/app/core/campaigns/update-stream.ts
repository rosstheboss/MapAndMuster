/**
 * Shared constants and pure helpers for the Server-Sent Events update stream.
 *
 * The server pushes only `{ revision }`. Pages refetch an authorized endpoint when the pushed
 * revision is newer than what they have applied, so no campaign state travels on the stream.
 */

/**
 * How often to fall back to a direct refresh while the stream is disconnected. Long on purpose:
 * this is a safety net for proxies that refuse `text/event-stream`, not a primary update path.
 */
export const UPDATE_STREAM_FALLBACK_POLL_MS = 60_000;

/**
 * Named event the server writes on connect and every idle interval. EventSource does not expose
 * comment keep-alives, so the client can only detect a silent line if the beat is a real event.
 */
export const UPDATE_STREAM_HEARTBEAT_EVENT = 'heartbeat';

/**
 * How long an open stream may go without a heartbeat or update before it is treated as dead.
 * Longer than two server beats (20s) so a single delayed frame is not a reconnect, short enough
 * that a buffered Worker cannot leave the page sitting on a lying `open` state.
 */
export const UPDATE_STREAM_STALL_MS = 45_000;

/** Delay before the first reconnect attempt. */
export const UPDATE_STREAM_RECONNECT_MIN_MS = 1_000;

/** Ceiling on the reconnect backoff. */
export const UPDATE_STREAM_RECONNECT_MAX_MS = 30_000;

export type UpdateStreamState = 'connecting' | 'open' | 'disconnected';

/** Short label shown next to campaign and site-chat surfaces. */
export function updateStreamStatusLabel(state: UpdateStreamState): string {
  switch (state) {
    case 'open':
      return 'Live';
    case 'disconnected':
      return 'Reconnecting';
    case 'connecting':
      return 'Connecting';
  }
}

/** Longer explanation for the tooltip and accessible name. */
export function updateStreamStatusDetail(state: UpdateStreamState): string {
  switch (state) {
    case 'open':
      return 'Updates arrive as they happen.';
    case 'disconnected':
      return 'The live connection dropped. Checking for updates every minute until it returns.';
    case 'connecting':
      return 'Opening the live update connection.';
  }
}

export interface CampaignUpdateEvent {
  /** Campaign revision after the change, or 0 for updates that are not campaign-scoped. */
  revision: number;
}

/** Doubles the reconnect delay, capped, so a down backend is not hammered. */
export function nextReconnectDelayMs(current: number): number {
  return Math.min(Math.max(current, UPDATE_STREAM_RECONNECT_MIN_MS) * 2, UPDATE_STREAM_RECONNECT_MAX_MS);
}

/**
 * Reads the revision out of an event payload, returning 0 for anything unparseable so a
 * malformed frame still triggers a refresh rather than being silently dropped.
 */
export function parseUpdateEvent(data: string): CampaignUpdateEvent {
  try {
    const parsed: unknown = JSON.parse(data);
    if (typeof parsed === 'object' && parsed !== null && 'revision' in parsed) {
      const revision: unknown = parsed.revision;
      if (typeof revision === 'number' && Number.isFinite(revision)) {
        return { revision };
      }
    }
  } catch {
    // Fall through to the conservative default.
  }

  return { revision: 0 };
}
