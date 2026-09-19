import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { GUEST_PREVIEW_MESSAGE } from '../../core/auth/guest-preview';

@Component({
  selector: 'app-guest-preview-banner',
  imports: [RouterLink],
  templateUrl: './guest-preview-banner.component.html',
  styleUrl: './guest-preview-banner.component.css',
})
export class GuestPreviewBannerComponent {
  protected readonly message = GUEST_PREVIEW_MESSAGE;
}
