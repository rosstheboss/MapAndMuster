import { Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';

import { formatCountdown } from '../../core/campaigns/campaign-schedule';

@Component({
  selector: 'app-phase-countdown',
  template: `{{ label() }}`,
})
export class PhaseCountdownComponent {
  readonly endsUtc = input.required<string>();
  readonly expired = output<void>();
  private readonly nowMs = signal(Date.now());
  private emittedExpiry = false;
  protected readonly label = computed(() => formatCountdown(this.endsUtc(), this.nowMs()));

  constructor() {
    effect(() => {
      this.endsUtc();
      this.emittedExpiry = false;
    });
    const id = globalThis.setInterval(() => {
      this.nowMs.set(Date.now());
      if (!this.emittedExpiry && Date.parse(this.endsUtc()) <= Date.now()) {
        this.emittedExpiry = true;
        this.expired.emit();
      }
    }, 1000);
    inject(DestroyRef).onDestroy(() => globalThis.clearInterval(id));
  }
}
