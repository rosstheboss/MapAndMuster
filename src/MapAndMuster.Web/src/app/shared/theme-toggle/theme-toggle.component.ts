import { Component, inject } from '@angular/core';

import { ThemeService } from '../../core/theme/theme.service';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'app-theme-toggle',
  imports: [IconComponent],
  templateUrl: './theme-toggle.component.html',
  styleUrl: './theme-toggle.component.css',
})
export class ThemeToggleComponent {
  protected readonly theme = inject(ThemeService);

  protected toggle(): void {
    this.theme.toggle();
  }
}
