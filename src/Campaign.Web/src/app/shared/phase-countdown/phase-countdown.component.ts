import { Component, computed, DestroyRef, inject, input, signal } from '@angular/core';

import { formatCountdown } from '../../core/campaigns/campaign-schedule';

@Component({
  selector: 'app-phase-countdown',
  template: `{{ label() }}`,
})
export class PhaseCountdownComponent {
  readonly endsUtc = input.required<string>();
  private readonly nowMs = signal(Date.now());
  protected readonly label = computed(() => formatCountdown(this.endsUtc(), this.nowMs()));

  constructor() {
    const id = globalThis.setInterval(() => this.nowMs.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => globalThis.clearInterval(id));
  }
}
