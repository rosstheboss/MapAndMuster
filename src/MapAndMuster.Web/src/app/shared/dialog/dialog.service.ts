import { computed, Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AppDialogService {
  private readonly openCount = signal(0);
  readonly hasOpen = computed(() => this.openCount() > 0);

  register(): void {
    this.openCount.update((count) => count + 1);
  }

  unregister(): void {
    this.openCount.update((count) => Math.max(0, count - 1));
  }
}
