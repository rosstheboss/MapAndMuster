import { Injectable, signal, type Signal } from '@angular/core';

/** Tick interval for the shared clock. Countdowns need second resolution. */
export const CLOCK_TICK_MS = 1_000;

/**
 * One interval timer shared by every countdown and relative timestamp on the page.
 *
 * Countdown components used to own a `setInterval` each, so a list of in-progress campaigns ran
 * one timer per row plus a separate timer on the campaign page, all waking the browser for the
 * same second boundary. Subscribers share a single signal and the timer only runs while at least
 * one of them is alive.
 */
@Injectable({ providedIn: 'root' })
export class ClockService {
  private readonly current = signal(Date.now());
  private timer: ReturnType<typeof globalThis.setInterval> | null = null;
  private subscribers = 0;

  /** The shared current time in milliseconds. Read this inside a computed. */
  get nowMs(): Signal<number> {
    return this.current.asReadonly();
  }

  /**
   * Registers interest in clock ticks and starts the timer if it is not already running.
   *
   * @returns A release function. Call it on destroy; the timer stops when the last caller does.
   */
  subscribe(): () => void {
    this.subscribers += 1;
    if (this.timer === null) {
      this.current.set(Date.now());
      this.timer = globalThis.setInterval(() => this.current.set(Date.now()), CLOCK_TICK_MS);
    }

    let released = false;
    return (): void => {
      if (released) {
        return;
      }

      released = true;
      this.subscribers -= 1;
      if (this.subscribers <= 0 && this.timer !== null) {
        globalThis.clearInterval(this.timer);
        this.timer = null;
        this.subscribers = 0;
      }
    };
  }
}
