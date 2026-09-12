import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { PUBLIC_RUNTIME_CONFIG } from '../config/public-runtime-config';
import { UpdateStreamService } from './update-stream.service';
import {
  UPDATE_STREAM_FALLBACK_POLL_MS,
  UPDATE_STREAM_HEARTBEAT_EVENT,
  UPDATE_STREAM_RECONNECT_MIN_MS,
  UPDATE_STREAM_STALL_MS,
} from './update-stream';

class FakeEventSource {
  static instances: FakeEventSource[] = [];

  onopen: (() => void) | null = null;
  onerror: (() => void) | null = null;
  readonly listeners = new Map<string, EventListener>();
  readonly close = vi.fn();

  constructor(
    readonly url: string,
    readonly init?: EventSourceInit,
  ) {
    FakeEventSource.instances.push(this);
  }

  addEventListener(type: string, listener: EventListener): void {
    this.listeners.set(type, listener);
  }

  emit(type: string, data = '{}'): void {
    this.listeners.get(type)?.(new MessageEvent(type, { data }));
  }
}

describe('UpdateStreamService', () => {
  const originalEventSource = globalThis.EventSource;
  let service: UpdateStreamService;
  let closeSubscription: (() => void) | null = null;

  beforeEach(() => {
    FakeEventSource.instances = [];
    closeSubscription = null;
    vi.useFakeTimers();
    vi.stubGlobal('EventSource', FakeEventSource);
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), { provide: PUBLIC_RUNTIME_CONFIG, useValue: { apiBaseUrl: '' } }],
    });
    service = TestBed.inject(UpdateStreamService);
  });

  afterEach(() => {
    closeSubscription?.();
    closeSubscription = null;
    vi.useRealTimers();
    vi.unstubAllGlobals();
    globalThis.EventSource = originalEventSource;
  });

  function watch(): {
    onUpdate: ReturnType<typeof vi.fn>;
    onFallbackPoll: ReturnType<typeof vi.fn>;
    source: FakeEventSource;
    close: () => void;
    state: () => string;
  } {
    const onUpdate = vi.fn();
    const onFallbackPoll = vi.fn();
    const subscription = service.watchCampaign('campaign-1', { onUpdate, onFallbackPoll });
    closeSubscription = subscription.close;
    const source = FakeEventSource.instances[0];
    expect(source).toBeDefined();
    expect(source.url).toBe('/api/campaigns/campaign-1/stream');
    expect(source.init?.withCredentials).toBe(true);
    return { onUpdate, onFallbackPoll, source, close: subscription.close, state: subscription.state };
  }

  it('refetches on a play event and ignores a heartbeat', () => {
    const { onUpdate, source } = watch();
    source.onopen?.();

    source.emit(UPDATE_STREAM_HEARTBEAT_EVENT);
    source.emit('play', '{"revision":4}');

    expect(onUpdate).toHaveBeenCalledTimes(1);
    expect(onUpdate).toHaveBeenCalledWith(4);
  });

  it('treats a silent open stream as disconnected and starts the fallback poll', () => {
    const { onFallbackPoll, source, state } = watch();
    source.onopen?.();
    expect(state()).toBe('open');

    vi.advanceTimersByTime(UPDATE_STREAM_STALL_MS - 1);
    expect(source.close).not.toHaveBeenCalled();
    expect(onFallbackPoll).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(source.close).toHaveBeenCalled();
    expect(state()).toBe('disconnected');

    vi.advanceTimersByTime(UPDATE_STREAM_FALLBACK_POLL_MS);
    expect(onFallbackPoll).toHaveBeenCalled();
  });

  it('resets the stall timer when a heartbeat arrives', () => {
    const { onFallbackPoll, source } = watch();
    source.onopen?.();

    vi.advanceTimersByTime(UPDATE_STREAM_STALL_MS - 1);
    source.emit(UPDATE_STREAM_HEARTBEAT_EVENT);
    vi.advanceTimersByTime(UPDATE_STREAM_STALL_MS - 1);

    expect(source.close).not.toHaveBeenCalled();
    expect(onFallbackPoll).not.toHaveBeenCalled();
  });

  it('reconnects after a stall without overlapping EventSource instances', () => {
    const { source } = watch();
    source.onopen?.();

    vi.advanceTimersByTime(UPDATE_STREAM_STALL_MS);
    expect(FakeEventSource.instances).toHaveLength(1);

    vi.advanceTimersByTime(UPDATE_STREAM_RECONNECT_MIN_MS * 2);
    expect(FakeEventSource.instances).toHaveLength(2);
  });

  it('does not schedule a second reconnect when error fires after a stall', () => {
    const { source } = watch();
    source.onopen?.();

    vi.advanceTimersByTime(UPDATE_STREAM_STALL_MS);
    source.onerror?.();

    vi.advanceTimersByTime(UPDATE_STREAM_RECONNECT_MIN_MS * 2);
    expect(FakeEventSource.instances).toHaveLength(2);
  });

  it('cancels the stall timer when the subscription is closed', () => {
    const { onFallbackPoll, source, close } = watch();
    source.onopen?.();
    close();

    vi.advanceTimersByTime(UPDATE_STREAM_STALL_MS + UPDATE_STREAM_FALLBACK_POLL_MS);
    expect(onFallbackPoll).not.toHaveBeenCalled();
    expect(FakeEventSource.instances).toHaveLength(1);
  });
});
