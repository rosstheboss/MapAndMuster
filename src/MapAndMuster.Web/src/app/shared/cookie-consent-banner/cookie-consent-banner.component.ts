import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CookieConsentService } from '../../core/cookies/cookie-consent.service';
import { AppDialogComponent } from '../dialog/dialog.component';

@Component({
  selector: 'app-cookie-consent-banner',
  imports: [RouterLink, AppDialogComponent],
  templateUrl: './cookie-consent-banner.component.html',
  styleUrl: './cookie-consent-banner.component.css',
})
export class CookieConsentBannerComponent {
  protected readonly consent = inject(CookieConsentService);
  protected readonly settingsPreferences = signal(false);

  protected openSettings(): void {
    this.settingsPreferences.set(this.consent.allowsPreferences());
    this.consent.openSettings();
  }

  protected saveSettings(): void {
    this.consent.saveSettings(this.settingsPreferences());
  }

  protected onPreferencesChange(event: Event): void {
    this.settingsPreferences.set(event.target instanceof HTMLInputElement && event.target.checked);
  }
}
