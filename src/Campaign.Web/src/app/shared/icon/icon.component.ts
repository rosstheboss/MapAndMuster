import { Component, input } from '@angular/core';

export type AppIconName =
  'alert' | 'castle' | 'check' | 'chevron-down' | 'helm' | 'home' | 'key' | 'logout' | 'shield' | 'sword';

@Component({
  selector: 'app-icon',
  templateUrl: './icon.component.html',
  styleUrl: './icon.component.css',
})
export class IconComponent {
  readonly name = input.required<AppIconName>();
  readonly label = input<string | undefined>(undefined);
}
