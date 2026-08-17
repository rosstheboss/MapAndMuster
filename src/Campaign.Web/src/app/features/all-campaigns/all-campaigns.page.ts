import { Component, DestroyRef, inject, signal } from '@angular/core';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { CAMPAIGN_LOG_POLL_MS } from '../../core/campaigns/campaign-log';
import { CHAT_LANGUAGES, type ChatLanguage } from '../../core/chat/chat-languages';
import { SiteChatPrefsService } from '../../core/chat/site-chat-prefs.service';
import { SiteChatService } from '../../core/chat/site-chat.service';
import type { SiteChatBoard, SiteChatSend } from '../../core/chat/site-chat.models';
import { CampaignListComponent } from '../../shared/campaign-list/campaign-list.component';
import { SiteChatComponent } from '../../shared/site-chat/site-chat.component';

@Component({
  selector: 'app-all-campaigns-page',
  imports: [CampaignListComponent, SiteChatComponent],
  templateUrl: './all-campaigns.page.html',
  styleUrl: './all-campaigns.page.css',
})
export class AllCampaignsPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly siteChatApi = inject(SiteChatService);
  private readonly siteChatPrefs = inject(SiteChatPrefsService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly campaigns = signal<CampaignListItem[]>([]);
  protected readonly chat = signal<SiteChatBoard | null>(null);
  protected readonly chatSending = signal(false);
  protected readonly chatError = signal<string | null>(null);
  protected readonly chatExpanded = signal(true);
  protected readonly composeLanguage = signal<ChatLanguage>('English');
  protected readonly visibleLanguages = signal<ChatLanguage[]>([...CHAT_LANGUAGES]);
  private chatPollStarted = false;

  constructor() {
    const prefs = this.siteChatPrefs.read(this.auth.currentUser()?.preferredChatLanguage);
    this.composeLanguage.set(prefs.composeLanguage);
    this.visibleLanguages.set([...prefs.visibleLanguages]);
    void this.load();
  }

  protected viewerUserId(): string | null {
    return this.auth.currentUser()?.id ?? null;
  }

  protected timeZoneId(): string | null {
    return this.auth.currentUser()?.timeZoneId ?? null;
  }

  protected reload(): void {
    void this.load();
  }

  protected onChatExpanded(open: boolean): void {
    this.chatExpanded.set(open);
  }

  protected onComposeLanguage(language: ChatLanguage): void {
    this.composeLanguage.set(language);
    this.persistChatPrefs();
  }

  protected onVisibleLanguages(languages: ChatLanguage[]): void {
    this.visibleLanguages.set(languages);
    this.persistChatPrefs();
  }

  protected async postChat(payload: SiteChatSend): Promise<void> {
    this.chatSending.set(true);
    this.chatError.set(null);
    try {
      this.chat.set(await this.siteChatApi.post(payload));
    } catch (error: unknown) {
      this.chatError.set(readApiError(error, 'Unable to send that chat message.'));
    } finally {
      this.chatSending.set(false);
    }
  }

  protected async onBlockChange(event: { userId: string; blocked: boolean }): Promise<void> {
    this.chatError.set(null);
    try {
      this.chat.set(await this.siteChatApi.setBlock(event.userId, event.blocked));
    } catch (error: unknown) {
      this.chatError.set(readApiError(error, 'Unable to update that block.'));
    }
  }

  private persistChatPrefs(): void {
    this.siteChatPrefs.write({
      composeLanguage: this.composeLanguage(),
      visibleLanguages: this.visibleLanguages(),
    });
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const [campaigns, chat] = await Promise.all([this.campaignsApi.listAll(), this.siteChatApi.getBoard()]);
      this.campaigns.set(campaigns);
      this.chat.set(chat);
      this.startChatPolling();
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load campaigns.'));
    } finally {
      this.loading.set(false);
    }
  }

  private startChatPolling(): void {
    if (this.chatPollStarted) {
      return;
    }

    this.chatPollStarted = true;
    const timer = globalThis.setInterval(() => void this.refreshChat(), CAMPAIGN_LOG_POLL_MS);
    this.destroyRef.onDestroy(() => globalThis.clearInterval(timer));
  }

  private async refreshChat(): Promise<void> {
    if (this.chatSending() || globalThis.document.visibilityState === 'hidden') {
      return;
    }

    try {
      this.chat.set(await this.siteChatApi.getBoard());
    } catch {
      // Keep the visible chat; the next poll retries.
    }
  }
}
