import { Injectable, inject, signal, type Signal } from '@angular/core';

import { PUBLIC_RUNTIME_CONFIG, apiUrl } from '../config/public-runtime-config';
import {
  UPDATE_STREAM_FALLBACK_POLL_MS,
  UPDATE_STREAM_HEARTBEAT_EVENT,
  UPDATE_STREAM_RECONNECT_MIN_MS,
  UPDATE_STREAM_STALL_MS,
  type UpdateStreamState,
  nextReconnectDelayMs,
  parseUpdateEvent,
} from './update-stream';

export interface UpdateStreamHandlers {
  /**
   * Called when the server reports a change. The revision is the campaign revision after the
   * change, or 0 when the update is not campaign-scoped.
   */
  onUpdate: (revision: number) => void;

  /**
   * Called on the fallback interval while the stream is disconnected, so a proxy that refuses
   * `text/event-stream` degrades to slower updates rather than breaking the page.
   */
  onFallbackPoll: () => void;
}

export interface UpdateStreamSubscription {
  /** Current connection state. A signal so the Live / Reconnecting chip updates. */
  readonly state: Signal<UpdateStreamState>;

  /** Stops the stream and cancels all timers. Safe to call more than once. */
  close: () => void;
}

/**
 * Subscribes to the API's Server-Sent Events endpoints and owns reconnect and fallback polling.
 *
 * `EventSource` cannot set headers and relies on cookies. The app calls same-origin `/api` and
 * the auth cookie is `SameSite=Lax`, so this works with the current topology; a cross-origin API
 * would need CORS with credentials. See
 * `docs/adr/0004-server-sent-events-for-campaign-updates.md`.
 */
@Injectable({ providedIn: 'root' })
export class UpdateStreamService {
  private readonly config = inject(PUBLIC_RUNTIME_CONFIG);

  /** Subscribes to one campaign's updates. */
  watchCampaign(campaignId: string, handlers: UpdateStreamHandlers): UpdateStreamSubscription {
    return this.watch(`/api/campaigns/${campaignId}/stream`, handlers);
  }

  /** Subscribes to public site-chat updates. */
  watchSiteChat(handlers: UpdateStreamHandlers): UpdateStreamSubscription {
    return this.watch('/api/site-chat/stream', handlers);
  }

  private watch(path: string, handlers: UpdateStreamHandlers): UpdateStreamSubscription {
    const state = signal<UpdateStreamState>('connecting');
    const url = apiUrl(path, this.config.apiBaseUrl);

    let source: EventSource | null = null;
    let reconnectTimer: ReturnType<typeof globalThis.setTimeout> | null = null;
    let fallbackTimer: ReturnType<typeof globalThis.setInterval> | null = null;
    let stallTimer: ReturnType<typeof globalThis.setTimeout> | null = null;
    let reconnectDelay = UPDATE_STREAM_RECONNECT_MIN_MS;
    let closed = false;

    const stopFallback = (): void => {
      if (fallbackTimer !== null) {
        globalThis.clearInterval(fallbackTimer);
        fallbackTimer = null;
      }
    };

    const startFallback = (): void => {
      if (fallbackTimer !== null || closed) {
        return;
      }

      fallbackTimer = globalThis.setInterval(() => handlers.onFallbackPoll(), UPDATE_STREAM_FALLBACK_POLL_MS);
    };

    const stopStall = (): void => {
      if (stallTimer !== null) {
        globalThis.clearTimeout(stallTimer);
        stallTimer = null;
      }
    };

    const armStall = (): void => {
      stopStall();
      if (closed) {
        return;
      }

      // EventSource.onopen fires when headers arrive. A buffering proxy still looks open, so
      // silence after this point is the signal that bytes are not reaching the page.
      stallTimer = globalThis.setTimeout(() => retry(), UPDATE_STREAM_STALL_MS);
    };

    const retry = (): void => {
      if (closed) {
        return;
      }

      stopStall();
      source?.close();
      source = null;
      state.set('disconnected');
      startFallback();

      if (reconnectTimer !== null) {
        return;
      }

      reconnectDelay = nextReconnectDelayMs(reconnectDelay);
      reconnectTimer = globalThis.setTimeout(() => {
        reconnectTimer = null;
        connect();
      }, reconnectDelay);
    };

    const connect = (): void => {
      if (closed || typeof globalThis.EventSource !== 'function') {
        // No EventSource support: rely on the fallback interval alone.
        state.set('disconnected');
        startFallback();
        return;
      }

      state.set('connecting');
      const next = new globalThis.EventSource(url, { withCredentials: true });
      source = next;

      next.onopen = (): void => {
        state.set('open');
        reconnectDelay = UPDATE_STREAM_RECONNECT_MIN_MS;
        stopFallback();
        armStall();
      };

      const handle = (event: MessageEvent<string>): void => {
        armStall();
        handlers.onUpdate(parseUpdateEvent(event.data).revision);
      };

      next.addEventListener('play', handle as EventListener);
      next.addEventListener('setup', handle as EventListener);
      next.addEventListener('site-chat', handle as EventListener);
      next.addEventListener(UPDATE_STREAM_HEARTBEAT_EVENT, () => armStall());

      next.onerror = (): void => {
        // The browser retries on its own, but only for transport failures. Close and schedule a
        // reconnect so an authorization change, a proxy rejection, or a stall is recovered from.
        if (source === next) {
          retry();
        }
      };
    };

    connect();

    return {
      state,
      close: (): void => {
        closed = true;
        stopFallback();
        stopStall();
        if (reconnectTimer !== null) {
          globalThis.clearTimeout(reconnectTimer);
          reconnectTimer = null;
        }

        source?.close();
        source = null;
        state.set('disconnected');
      },
    };
  }
}
