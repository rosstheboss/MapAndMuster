import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-faction-logo',
  templateUrl: './faction-logo.component.html',
  styleUrl: './faction-logo.component.css',
})
export class FactionLogoComponent {
  readonly src = input.required<string>();
  readonly color = input.required<string>();
  readonly tint = input(false);
  readonly alt = input('');

  protected readonly maskUrl = computed(() => `url(${JSON.stringify(this.src())})`);
}
