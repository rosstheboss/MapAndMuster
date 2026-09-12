import { Component, computed, DestroyRef, effect, inject, input, output, untracked } from '@angular/core';

import { formatCountdown } from '../../core/campaigns/campaign-schedule';
import { ClockService } from '../../core/time/clock.service';

@Component({
  selector: 'app-phase-countdown',
  template: `{{ label() }}`,
})
export class PhaseCountdownComponent {
  readonly endsUtc = input.required<string>();
  readonly expired = output<void>();
  private readonly clock = inject(ClockService);
  private emittedExpiry = false;
  protected readonly label = computed(() => formatCountdown(this.endsUtc(), this.clock.nowMs()));

  constructor() {
    const release = this.clock.subscribe();
    inject(DestroyRef).onDestroy(release);

    effect(() => {
      this.endsUtc();
      this.emittedExpiry = false;
    });

    // Emitting from an effect keeps this on the shared clock instead of a per-instance timer.
    // The first run is the initial render rather than a tick: the page has just loaded state for
    // this deadline, so announcing an already-past deadline there would only force a redundant
    // refetch. Reading the deadline untracked keeps ticks the only trigger.
    let seenTick = false;
    effect(() => {
      const nowMs = this.clock.nowMs();
      if (!seenTick) {
        seenTick = true;
        return;
      }

      if (!this.emittedExpiry && Date.parse(untracked(this.endsUtc)) <= nowMs) {
        this.emittedExpiry = true;
        this.expired.emit();
      }
    });
  }
}
