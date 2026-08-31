import { Component, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-confirm-button',
  templateUrl: './confirm-button.component.html',
  styleUrl: './confirm-button.component.css',
})
export class ConfirmButtonComponent {
  readonly label = input.required<string>();
  readonly confirmLabel = input.required<string>();
  readonly appearance = input('button-secondary');
  readonly confirmAppearance = input('button-danger');
  readonly disabled = input(false);
  readonly confirmed = output<void>();

  protected readonly armed = signal(false);

  protected onClick(): void {
    if (this.disabled()) {
      return;
    }

    if (!this.armed()) {
      this.armed.set(true);
      return;
    }

    this.armed.set(false);
    this.confirmed.emit();
  }
}
