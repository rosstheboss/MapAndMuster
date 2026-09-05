import { HttpClient, HttpErrorResponse } from '@angular/common/http';
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
  articles?: NewsArticle[];
  article?: NewsArticle | null;
}

export interface SaveNewsArticlePayload {
  title: string;
  bodyMarkdown: string;
}

const dashedGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const compactGuid = /^[0-9a-f]{32}$/i;

export function storedNotificationRouteId(id: string): string | null {
  if (dashedGuid.test(id)) {
    return id.toLowerCase();
  }

  if (compactGuid.test(id)) {
    const value = id.toLowerCase();
    return `${value.slice(0, 8)}-${value.slice(8, 12)}-${value.slice(12, 16)}-${value.slice(16, 20)}-${value.slice(20)}`;
  }

  return null;
}

@Injectable({ providedIn: 'root' })
export class HomeBoardService {
  private readonly http = inject(HttpClient);

  async listNotifications(): Promise<HomeAttentionItem[]> {
    return firstValueFrom(this.http.get<HomeAttentionItem[]>('/api/notifications', { withCredentials: true }));
  }

  async markRead(notificationId: string): Promise<void> {
    const id = storedNotificationRouteId(notificationId);
    if (!id) {
      return;
    }

    await firstValueFrom(
      this.http.post(`/api/notifications/${encodeURIComponent(id)}/read`, {}, { withCredentials: true }),
    );
  }

  async markAllRead(ids: readonly string[] = []): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/notifications/read-all', {}, { withCredentials: true }));
    } catch (error: unknown) {
      if (!isMissingDismissAllEndpoint(error)) {
        throw error;
      }

      const storedIds = ids.map((id) => storedNotificationRouteId(id)).filter((id): id is string => id !== null);
      await Promise.all(storedIds.map((id) => this.markRead(id)));
    }
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

function isMissingDismissAllEndpoint(error: unknown): boolean {
  return error instanceof HttpErrorResponse && (error.status === 404 || error.status === 405);
}
