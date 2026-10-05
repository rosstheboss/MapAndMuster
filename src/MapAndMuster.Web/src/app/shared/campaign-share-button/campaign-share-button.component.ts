import { Component, DestroyRef, inject, input, signal } from '@angular/core';

import { campaignShareUrl, copyText } from '../../core/campaigns/campaign-share';
import { AppDialogComponent } from '../dialog/dialog.component';

@Component({
  selector: 'app-campaign-share-button',
  imports: [AppDialogComponent],
  templateUrl: './campaign-share-button.component.html',
  styleUrl: './campaign-share-button.component.css',
})
export class CampaignShareButtonComponent {
  readonly campaignId = input.required<string>();
  readonly name = input.required<string>();

  protected readonly copied = signal(false);
  protected readonly pendingLink = signal<string | null>(null);
  private copiedTimer: ReturnType<typeof globalThis.setTimeout> | null = null;

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      if (this.copiedTimer !== null) {
        globalThis.clearTimeout(this.copiedTimer);
      }
    });
  }

  protected share(): void {
    void this.copyLink(campaignShareUrl(this.campaignId()));
  }

  protected copyShownLink(): void {
    const link = this.pendingLink();
    if (!link) {
      return;
    }

    void this.copyLink(link);
  }

  protected dismissLink(): void {
    this.pendingLink.set(null);
  }

  private async copyLink(url: string): Promise<void> {
    if (await copyText(url)) {
      this.noteCopied();
      return;
    }

    this.pendingLink.set(url);
  }

  private noteCopied(): void {
    this.pendingLink.set(null);
    this.copied.set(true);
    if (this.copiedTimer !== null) {
      globalThis.clearTimeout(this.copiedTimer);
    }

    this.copiedTimer = globalThis.setTimeout(() => {
      this.copied.set(false);
      this.copiedTimer = null;
    }, 4000);
  }
}
