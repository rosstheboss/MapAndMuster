import { afterNextRender, Component, ElementRef, inject, signal } from '@angular/core';

import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'app-password-input',
  imports: [IconComponent],
  templateUrl: './password-input.component.html',
  styleUrl: './password-input.component.css',
})
export class PasswordInputComponent {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly visible = signal(false);
  protected readonly inputId = signal<string | null>(null);

  constructor() {
    afterNextRender(() => {
      const input = this.inputElement();
      const id = input?.id;
      this.inputId.set(id && id.length > 0 ? id : null);
      if (input && !this.visible()) {
        input.type = 'password';
      }
    });
  }

  protected toggle(): void {
    const next = !this.visible();
    this.visible.set(next);
    const input = this.inputElement();
    if (input) {
      input.type = next ? 'text' : 'password';
    }
  }

  private inputElement(): HTMLInputElement | null {
    return this.host.nativeElement.querySelector('input');
  }
}
