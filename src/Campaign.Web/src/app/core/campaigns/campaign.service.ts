import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type { CampaignDetail, CampaignListItem, SaveCampaignPayload } from './campaign.models';

@Injectable({ providedIn: 'root' })
export class CampaignService {
  private readonly http = inject(HttpClient);

  async list(): Promise<CampaignListItem[]> {
    return firstValueFrom(this.http.get<CampaignListItem[]>('/api/campaigns', { withCredentials: true }));
  }

  async get(campaignId: string): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.get<CampaignDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}`, { withCredentials: true }),
    );
  }

  async create(payload: SaveCampaignPayload): Promise<CampaignDetail> {
    return firstValueFrom(this.http.post<CampaignDetail>('/api/campaigns', payload, { withCredentials: true }));
  }

  async update(campaignId: string, payload: SaveCampaignPayload): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.put<CampaignDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}`, payload, {
        withCredentials: true,
      }),
    );
  }

  async delete(campaignId: string): Promise<void> {
    await firstValueFrom(
      this.http.delete(`/api/campaigns/${encodeURIComponent(campaignId)}`, { withCredentials: true }),
    );
  }

  async uploadMap(campaignId: string, file: File, revision: number): Promise<CampaignDetail> {
    const form = new FormData();
    form.set('map', file);
    form.set('revision', String(revision));
    return firstValueFrom(
      this.http.post<CampaignDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/map`, form, {
        withCredentials: true,
      }),
    );
  }

  mapUrl(campaignId: string, revision: number): string {
    return `/api/campaigns/${encodeURIComponent(campaignId)}/map?v=${revision}`;
  }
}
