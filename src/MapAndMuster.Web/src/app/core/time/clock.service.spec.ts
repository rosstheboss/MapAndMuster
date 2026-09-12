import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { CLOCK_TICK_MS, ClockService } from './clock.service';

describe('ClockService', () => {
  let clock: ClockService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({});
    clock = TestBed.inject(ClockService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('advances the shared time on each tick', () => {
    const release = clock.subscribe();
    const start = clock.nowMs();

    vi.advanceTimersByTime(CLOCK_TICK_MS * 3);

    expect(clock.nowMs()).toBeGreaterThan(start);
    release();
  });

  it('runs one timer no matter how many subscribers there are', () => {
    const spy = vi.spyOn(globalThis, 'setInterval');

    const first = clock.subscribe();
    const second = clock.subscribe();

    expect(spy).toHaveBeenCalledTimes(1);
    first();
    second();
  });

  it('stops ticking once the last subscriber releases', () => {
    const release = clock.subscribe();
    release();

    const stopped = clock.nowMs();
    vi.advanceTimersByTime(CLOCK_TICK_MS * 5);

    expect(clock.nowMs()).toBe(stopped);
  });

  it('ignores a repeated release so the count cannot drift negative', () => {
    const first = clock.subscribe();
    first();
    first();

    const second = clock.subscribe();
    const start = clock.nowMs();
    vi.advanceTimersByTime(CLOCK_TICK_MS * 2);

    expect(clock.nowMs()).toBeGreaterThan(start);
    second();
  });
});
