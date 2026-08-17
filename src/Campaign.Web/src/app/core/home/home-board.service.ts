import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface HomeAttentionItem {
  id: string;
  kind: string;
  campaignId: string | null;
  campaignName: string | null;
  title: string;
  body: string;
  path: string;
  createdUtc: string;
}

export interface NewsArticle {
  id: string;
  title: string;
  bodyMarkdown: string;
  bodyHtml: string;
  publishedUtc: string;
  updatedUtc: string;
}

export interface NewsPage {
  page: number;
  totalPages: number;
  article: NewsArticle | null;
}

export interface SaveNewsArticlePayload {
  title: string;
  bodyMarkdown: string;
}

@Injectable({ providedIn: 'root' })
export class HomeBoardService {
  private readonly http = inject(HttpClient);

  async listNotifications(): Promise<HomeAttentionItem[]> {
    return firstValueFrom(this.http.get<HomeAttentionItem[]>('/api/notifications', { withCredentials: true }));
  }

  async markRead(notificationId: string): Promise<void> {
    if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(notificationId)) {
      return;
    }

    await firstValueFrom(
      this.http.post(`/api/notifications/${encodeURIComponent(notificationId)}/read`, {}, { withCredentials: true }),
    );
  }

  async getNews(page: number): Promise<NewsPage> {
    return firstValueFrom(
      this.http.get<NewsPage>('/api/news', { params: { page: String(page) }, withCredentials: true }),
    );
  }

  async createNews(payload: SaveNewsArticlePayload): Promise<NewsArticle> {
    return firstValueFrom(this.http.post<NewsArticle>('/api/news', payload, { withCredentials: true }));
  }

  async updateNews(articleId: string, payload: SaveNewsArticlePayload): Promise<NewsArticle> {
    return firstValueFrom(
      this.http.put<NewsArticle>(`/api/news/${encodeURIComponent(articleId)}`, payload, { withCredentials: true }),
    );
  }

  async deleteNews(articleId: string): Promise<void> {
    await firstValueFrom(this.http.delete(`/api/news/${encodeURIComponent(articleId)}`, { withCredentials: true }));
  }
}
