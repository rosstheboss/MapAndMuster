import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CookieConsentService } from '../../core/cookies/cookie-consent.service';

@Component({
  selector: 'app-cookies-page',
  imports: [RouterLink],
  templateUrl: './cookies.page.html',
  styleUrl: './cookies.page.css',
})
export class CookiesPage {
  protected readonly consent = inject(CookieConsentService);
}
