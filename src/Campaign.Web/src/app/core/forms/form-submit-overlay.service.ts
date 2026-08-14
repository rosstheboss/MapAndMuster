import { computed, Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class FormSubmitOverlayService {
  private readonly pending = signal(0);

  readonly busy = computed(() => this.pending() > 0);

  async run<T>(work: () => Promise<T>): Promise<T> {
    this.pending.update((count) => count + 1);
    try {
      return await work();
    } finally {
      this.pending.update((count) => Math.max(0, count - 1));
    }
  }
}
