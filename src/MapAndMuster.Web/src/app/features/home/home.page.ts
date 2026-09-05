import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { CampaignService } from '../../core/campaigns/campaign.service';
import { statusLabel } from '../../core/campaigns/campaign-schedule';
import {
  HomeBoardService,
  storedNotificationRouteId,
  type HomeAttentionItem,
  type NewsPage,
} from '../../core/home/home-board.service';
import {
  campaignAttentionItems,
  campaignCommitLabel,
  campaignRemainingSetupLabel,
  campaignRoundPhaseText,
} from '../../shared/campaign-list/campaign-list.summary';
import { PhaseCountdownComponent } from '../../shared/phase-countdown/phase-countdown.component';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';

@Component({
  selector: 'app-home-page',
  imports: [InstantDatePipe, FormsModule, RouterLink, PhaseCountdownComponent],
  templateUrl: './home.page.html',
  styleUrl: './home.page.css',
})
export class HomePage {
  protected readonly auth = inject(AuthService);
  private readonly board = inject(HomeBoardService);
  private readonly campaignsApi = inject(CampaignService);
  private readonly router = inject(Router);
  protected readonly discordInviteUrl = 'https://discord.gg/ATVt97DMnx';

  protected readonly notifications = signal<HomeAttentionItem[]>([]);
  protected readonly notificationPage = signal(1);
  protected readonly campaigns = signal<CampaignListItem[]>([]);
  protected readonly news = signal<NewsPage | null>(null);
  protected readonly newsError = signal<string | null>(null);
  protected readonly noticeError = signal<string | null>(null);
  protected readonly campaignError = signal<string | null>(null);
  protected readonly campaignsLoading = signal(true);
  protected readonly editingNews = signal(false);
  protected readonly creatingNews = signal(false);
  protected readonly editingNewsId = signal<string | null>(null);
  protected readonly newsTitle = signal('');
  protected readonly newsBody = signal('');
  protected readonly newsSaving = signal(false);
  protected readonly attentionCampaigns = computed(() => campaignAttentionItems(this.campaigns()));
  protected readonly noticePageSize = 5;
  protected readonly noticePageCount = computed(() =>
    Math.max(1, Math.ceil(this.notifications().length / this.noticePageSize)),
  );
  protected readonly visibleNotifications = computed(() => {
    const page = Math.min(this.notificationPage(), this.noticePageCount());
    const start = (page - 1) * this.noticePageSize;
    return this.notifications().slice(start, start + this.noticePageSize);
  });
  protected readonly newsArticles = computed(() => {
    const page = this.news();
    if (!page) {
      return [];
    }

    return page.articles ?? (page.article ? [page.article] : []);
  });
  protected readonly statusText = statusLabel;
  protected readonly roundPhaseText = campaignRoundPhaseText;
  protected readonly commitLabel = campaignCommitLabel;
  protected readonly remainingSetupLabel = campaignRemainingSetupLabel;

  constructor() {
    void this.loadBoard();
  }

  protected async openNotice(item: HomeAttentionItem): Promise<void> {
    await this.dismissNotice(item);
    await this.router.navigateByUrl(item.path);
  }

  protected async dismissNotice(item: HomeAttentionItem): Promise<void> {
    try {
      await this.board.markRead(item.id);
    } catch {
      // Live attention items are not stored notices.
    }

    this.notifications.update((items) => items.filter((notice) => notice.id !== item.id));
    if (this.notificationPage() > this.noticePageCount()) {
      this.notificationPage.set(this.noticePageCount());
    }
  }

  protected async dismissAllNotices(): Promise<void> {
    const ids = this.notifications().map((item) => item.id);
    try {
      await this.board.markAllRead(ids);
      this.noticeError.set(null);
    } catch (error: unknown) {
      this.noticeError.set(readApiError(error, 'Unable to dismiss notifications.'));
      return;
    }

    this.notifications.update((items) => items.filter((item) => storedNotificationRouteId(item.id) === null));
    this.notificationPage.set(1);
  }

  protected setNotificationPage(page: number): void {
    this.notificationPage.set(Math.min(this.noticePageCount(), Math.max(1, page)));
  }

  protected async loadNews(page: number): Promise<void> {
    try {
      this.news.set(await this.board.getNews(page));
      this.newsError.set(null);
    } catch (error: unknown) {
      this.newsError.set(readApiError(error, 'Unable to load news.'));
    }
  }

  protected startNewsEdit(article: { id: string; title: string; bodyMarkdown: string }): void {
    this.creatingNews.set(false);
    this.editingNewsId.set(article.id);
    this.newsTitle.set(article.title);
    this.newsBody.set(article.bodyMarkdown);
    this.editingNews.set(true);
  }

  protected startNewsCreate(): void {
    this.creatingNews.set(true);
    this.editingNewsId.set(null);
    this.newsTitle.set('');
    this.newsBody.set('');
    this.editingNews.set(true);
  }

  protected cancelNewsEdit(): void {
    this.editingNews.set(false);
    this.editingNewsId.set(null);
  }

  protected async saveNews(): Promise<void> {
    this.newsSaving.set(true);
    this.newsError.set(null);
    try {
      const payload = { title: this.newsTitle().trim(), bodyMarkdown: this.newsBody().trim() };
      const currentId = this.editingNewsId();
      if (!this.creatingNews() && currentId) {
        await this.board.updateNews(currentId, payload);
      } else {
        await this.board.createNews(payload);
      }

      this.editingNews.set(false);
      this.editingNewsId.set(null);
      await this.loadNews(1);
    } catch (error: unknown) {
      this.newsError.set(readApiError(error, 'Unable to save the news article.'));
    } finally {
      this.newsSaving.set(false);
    }
  }

  protected async deleteNews(articleId: string): Promise<void> {
    this.newsSaving.set(true);
    try {
      await this.board.deleteNews(articleId);
      this.editingNews.set(false);
      this.editingNewsId.set(null);
      await this.loadNews(this.news()?.page ?? 1);
    } catch (error: unknown) {
      this.newsError.set(readApiError(error, 'Unable to delete the news article.'));
    } finally {
      this.newsSaving.set(false);
    }
  }

  private async loadBoard(): Promise<void> {
    await Promise.all([this.loadNotifications(), this.loadNews(1), this.loadCampaigns()]);
  }

  private async loadNotifications(): Promise<void> {
    try {
      this.notifications.set(await this.board.listNotifications());
      this.notificationPage.set(1);
      this.noticeError.set(null);
    } catch (error: unknown) {
      this.noticeError.set(readApiError(error, 'Unable to load notifications.'));
    }
  }

  private async loadCampaigns(): Promise<void> {
    this.campaignsLoading.set(true);
    try {
      this.campaigns.set(await this.campaignsApi.list());
      this.campaignError.set(null);
    } catch (error: unknown) {
      this.campaignError.set(readApiError(error, 'Unable to load your campaigns.'));
    } finally {
      this.campaignsLoading.set(false);
    }
  }
}
