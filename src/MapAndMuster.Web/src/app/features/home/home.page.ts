import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService, readApiError } from '../../core/auth/auth.service';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { CampaignService } from '../../core/campaigns/campaign.service';
import { statusLabel } from '../../core/campaigns/campaign-schedule';
import { HomeBoardService, type HomeAttentionItem, type NewsPage } from '../../core/home/home-board.service';
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
  protected readonly campaigns = signal<CampaignListItem[]>([]);
  protected readonly news = signal<NewsPage | null>(null);
  protected readonly newsError = signal<string | null>(null);
  protected readonly noticeError = signal<string | null>(null);
  protected readonly campaignError = signal<string | null>(null);
  protected readonly campaignsLoading = signal(true);
  protected readonly editingNews = signal(false);
  protected readonly creatingNews = signal(false);
  protected readonly newsTitle = signal('');
  protected readonly newsBody = signal('');
  protected readonly newsSaving = signal(false);
  protected readonly attentionCampaigns = computed(() => campaignAttentionItems(this.campaigns()));
  protected readonly statusText = statusLabel;
  protected readonly roundPhaseText = campaignRoundPhaseText;
  protected readonly commitLabel = campaignCommitLabel;
  protected readonly remainingSetupLabel = campaignRemainingSetupLabel;

  constructor() {
    void this.loadBoard();
  }

  protected async openNotice(item: HomeAttentionItem): Promise<void> {
    try {
      await this.board.markRead(item.id);
    } catch {
      // Live attention items are not stored notices; still open the campaign.
    }

    await this.router.navigateByUrl(item.path);
  }

  protected async loadNews(page: number): Promise<void> {
    try {
      this.news.set(await this.board.getNews(page));
      this.newsError.set(null);
    } catch (error: unknown) {
      this.newsError.set(readApiError(error, 'Unable to load news.'));
    }
  }

  protected startNewsEdit(): void {
    const article = this.news()?.article;
    this.creatingNews.set(false);
    this.newsTitle.set(article?.title ?? '');
    this.newsBody.set(article?.bodyMarkdown ?? '');
    this.editingNews.set(true);
  }

  protected startNewsCreate(): void {
    this.creatingNews.set(true);
    this.newsTitle.set('');
    this.newsBody.set('');
    this.editingNews.set(true);
  }

  protected cancelNewsEdit(): void {
    this.editingNews.set(false);
  }

  protected async saveNews(): Promise<void> {
    this.newsSaving.set(true);
    this.newsError.set(null);
    try {
      const payload = { title: this.newsTitle().trim(), bodyMarkdown: this.newsBody().trim() };
      const current = this.news()?.article;
      if (!this.creatingNews() && current) {
        await this.board.updateNews(current.id, payload);
      } else {
        await this.board.createNews(payload);
      }

      this.editingNews.set(false);
      await this.loadNews(1);
    } catch (error: unknown) {
      this.newsError.set(readApiError(error, 'Unable to save the news article.'));
    } finally {
      this.newsSaving.set(false);
    }
  }

  protected async deleteNews(): Promise<void> {
    const article = this.news()?.article;
    if (!article) {
      return;
    }

    this.newsSaving.set(true);
    try {
      await this.board.deleteNews(article.id);
      this.editingNews.set(false);
      await this.loadNews(1);
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
