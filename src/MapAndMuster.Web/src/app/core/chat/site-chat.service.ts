import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type { PostSiteChatPayload, SiteChatBoard } from './site-chat.models';

@Injectable({ providedIn: 'root' })
export class SiteChatService {
  private readonly http = inject(HttpClient);

  async getBoard(): Promise<SiteChatBoard> {
    return firstValueFrom(this.http.get<SiteChatBoard>('/api/site-chat', { withCredentials: true }));
  }

  async post(payload: PostSiteChatPayload): Promise<SiteChatBoard> {
    return firstValueFrom(this.http.post<SiteChatBoard>('/api/site-chat', payload, { withCredentials: true }));
  }

  async setBlock(userId: string, blocked: boolean): Promise<SiteChatBoard> {
    return firstValueFrom(
      this.http.put<SiteChatBoard>(
        `/api/site-chat/blocks/${encodeURIComponent(userId)}`,
        { blocked },
        { withCredentials: true },
      ),
    );
  }
}
