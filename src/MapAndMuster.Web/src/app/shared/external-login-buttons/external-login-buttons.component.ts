import { Component, input, output } from '@angular/core';

import type { ExternalProvider } from '../../core/auth/auth.models';

@Component({
  selector: 'app-external-login-buttons',
  templateUrl: './external-login-buttons.component.html',
  styleUrl: './external-login-buttons.component.css',
})
export class ExternalLoginButtonsComponent {
  readonly providers = input.required<readonly ExternalProvider[]>();
  readonly selected = output<string>();

  protected label(provider: ExternalProvider): string {
    return `Continue with ${provider.displayName}`;
  }

  protected kind(name: string): 'google' | 'discord' | 'facebook' | 'other' {
    switch (name.toLowerCase()) {
      case 'google':
        return 'google';
      case 'discord':
        return 'discord';
      case 'facebook':
        return 'facebook';
      default:
        return 'other';
    }
  }
}
