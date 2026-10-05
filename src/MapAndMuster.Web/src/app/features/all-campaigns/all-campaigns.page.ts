import {
  afterNextRender,
  afterRenderEffect,
  Component,
  computed,
  DestroyRef,
  inject,
  Injector,
  signal,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { UpdateStreamService, type UpdateStreamSubscription } from '../../core/campaigns/update-stream.service';
import { CHAT_LANGUAGES, type ChatLanguage } from '../../core/chat/chat-languages';
import { SiteChatPrefsService } from '../../core/chat/site-chat-prefs.service';
import { SiteChatService } from '../../core/chat/site-chat.service';
import type { SiteChatBoard, SiteChatSend } from '../../core/chat/site-chat.models';
import { CampaignListFiltersComponent } from '../../shared/campaign-list/campaign-list-filters.component';
import { CampaignListComponent } from '../../shared/campaign-list/campaign-list.component';
import { SiteChatComponent } from '../../shared/site-chat/site-chat.component';

@Component({
  selector: 'app-all-campaigns-page',
  imports: [CampaignListComponent, CampaignListFiltersComponent, SiteChatComponent, RouterLink],
  templateUrl: './all-campaigns.page.html',
  styleUrl: './all-campaigns.page.css',
})
export class AllCampaignsPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly siteChatApi = inject(SiteChatService);
  private readonly siteChatPrefs = inject(SiteChatPrefsService);
  private readonly updates = inject(UpdateStreamService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  protected readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly loading = signal(true);
  protected readonly chatLoading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly chatLoadError = signal<string | null>(null);
  protected readonly campaigns = signal<CampaignListItem[]>([]);
  protected readonly filteredCampaigns = signal<readonly CampaignListItem[]>([]);
  protected readonly pinnedCampaignId = signal<string | null>(null);
  protected readonly listedCampaigns = computed(() => {
    const filtered = this.filteredCampaigns();
    const pinnedId = this.pinnedCampaignId();
    if (!pinnedId || filtered.some((campaign) => campaign.id === pinnedId)) {
      return filtered;
    }

    const pinned = this.campaigns().find((campaign) => campaign.id === pinnedId);
    return pinned ? [pinned, ...filtered] : filtered;
  });
  private readonly list = viewChild(CampaignListComponent);
  private joinFocused = false;

  protected expandAll(): void {
    this.list()?.expandAll();
  }

  protected collapseAll(): void {
    this.list()?.collapseAll();
  }
  protected readonly chat = signal<SiteChatBoard | null>(null);
  protected readonly chatSending = signal(false);
  protected readonly chatError = signal<string | null>(null);
  protected readonly chatExpanded = signal(false);
  protected readonly composeLanguage = signal<ChatLanguage>('English');
  protected readonly visibleLanguages = signal<ChatLanguage[]>([...CHAT_LANGUAGES]);
  protected readonly chatStream = signal<UpdateStreamSubscription | null>(null);

  constructor() {
    this.pinnedCampaignId.set(this.route.snapshot.queryParamMap.get('join'));
    afterRenderEffect(() => this.focusSharedCampaign());
    const prefs = this.siteChatPrefs.read(this.auth.currentUser()?.preferredChatLanguage);
    this.composeLanguage.set(prefs.composeLanguage);
    this.visibleLanguages.set([...prefs.visibleLanguages]);
    void this.loadCampaigns();
    void this.loadChat();
  }

  private focusSharedCampaign(): void {
    const id = this.pinnedCampaignId();
    if (!id || this.joinFocused || this.loading()) {
      return;
    }

    if (this.error() || !this.campaigns().some((campaign) => campaign.id === id)) {
      this.joinFocused = true;
      this.pinnedCampaignId.set(null);
      void this.router.navigateByUrl('/');
      return;
    }

    const list = this.list();
    if (!list) {
      return;
    }

    this.joinFocused = true;
    list.focusForJoin(id);
    afterNextRender(
      () => {
        document.getElementById(`campaign-${id}`)?.scrollIntoView({ block: 'center', inline: 'nearest' });
      },
      { injector: this.injector },
    );
    void this.clearJoinQuery();
  }

  private async clearJoinQuery(): Promise<void> {
    try {
      await this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { join: null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    } catch {
      // The prompt is already open. A leftover query only repeats it on refresh.
    }
  }

  protected viewerUserId(): string | null {
    return this.auth.currentUser()?.id ?? null;
  }

  protected timeZoneId(): string | null {
    return this.auth.currentUser()?.timeZoneId ?? null;
  }

  protected reload(): void {
    void this.loadCampaigns();
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

  private async loadCampaigns(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.campaigns.set(await this.campaignsApi.listAll());
    } catch (error: unknown) {
      this.error.set(readApiError(error, 'Unable to load campaigns.'));
    } finally {
      this.loading.set(false);
    }
  }

  private async loadChat(): Promise<void> {
    this.chatLoading.set(true);
    this.chatLoadError.set(null);
    try {
      this.chat.set(await this.siteChatApi.getBoard());
      this.startChatStream();
    } catch (error: unknown) {
      this.chatLoadError.set(readApiError(error, 'Unable to load public chat.'));
    } finally {
      this.chatLoading.set(false);
    }
  }

  private startChatStream(): void {
    if (this.chatStream()) {
      return;
    }

    // Site-chat events carry no message content, so a push always means "refetch the board",
    // which reapplies this viewer's block filtering server-side.
    this.chatStream.set(
      this.updates.watchSiteChat({
        onUpdate: () => void this.refreshChat(),
        onFallbackPoll: () => void this.refreshChat(),
      }),
    );
    this.destroyRef.onDestroy(() => {
      this.chatStream()?.close();
      this.chatStream.set(null);
    });
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
