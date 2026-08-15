import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type {
  CampaignDetail,
  CampaignListItem,
  CampaignPlayDetail,
  ChooseFactionPayload,
  ExtendCampaignSchedulePayload,
  MapGraphDetail,
  PlayRevisionPayload,
  SaveCampaignPayload,
  SaveMapGraphPayload,
  SaveOrderDraftPayload,
  SubmitBattleResultPayload,
  SubmitRetreatPayload,
} from './campaign.models';

@Injectable({ providedIn: 'root' })
export class CampaignService {
  private readonly http = inject(HttpClient);

  async list(): Promise<CampaignListItem[]> {
    return firstValueFrom(this.http.get<CampaignListItem[]>('/api/campaigns', { withCredentials: true }));
  }

  async listAll(): Promise<CampaignListItem[]> {
    return firstValueFrom(this.http.get<CampaignListItem[]>('/api/campaigns/all', { withCredentials: true }));
  }

  async join(campaignId: string, joinPassword: string | null = null): Promise<CampaignListItem> {
    return firstValueFrom(
      this.http.post<CampaignListItem>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/join`,
        { joinPassword },
        { withCredentials: true },
      ),
    );
  }

  async leave(campaignId: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`/api/campaigns/${encodeURIComponent(campaignId)}/leave`, {}, { withCredentials: true }),
    );
  }

  async get(campaignId: string): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.get<CampaignDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}`, { withCredentials: true }),
    );
  }

  async create(payload: SaveCampaignPayload): Promise<CampaignDetail> {
    return firstValueFrom(this.http.post<CampaignDetail>('/api/campaigns', payload, { withCredentials: true }));
  }

  async duplicate(campaignId: string): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.post<CampaignDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/duplicate`,
        {},
        {
          withCredentials: true,
        },
      ),
    );
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

  async getMapGraph(campaignId: string): Promise<MapGraphDetail> {
    return firstValueFrom(
      this.http.get<MapGraphDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/map/graph`, {
        withCredentials: true,
      }),
    );
  }

  async saveMapGraph(campaignId: string, payload: SaveMapGraphPayload): Promise<MapGraphDetail> {
    return firstValueFrom(
      this.http.put<MapGraphDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/map/graph`, payload, {
        withCredentials: true,
      }),
    );
  }

  structureImageUrl(campaignId: string, structureTypeId: string, revision: number): string {
    return `/api/campaigns/${encodeURIComponent(campaignId)}/structures/${encodeURIComponent(structureTypeId)}/image?v=${revision}`;
  }

  flagImageUrl(campaignId: string, factionId: string, revision: number): string {
    return `/api/campaigns/${encodeURIComponent(campaignId)}/factions/${encodeURIComponent(factionId)}/flag?v=${revision}`;
  }

  missionFileUrl(campaignId: string, missionId: string): string {
    return `/api/campaigns/${encodeURIComponent(campaignId)}/missions/${encodeURIComponent(missionId)}/file`;
  }

  async uploadStructureImage(
    campaignId: string,
    structureTypeId: string,
    file: File,
    revision: number,
  ): Promise<CampaignDetail> {
    const form = new FormData();
    form.set('image', file);
    form.set('revision', String(revision));
    return firstValueFrom(
      this.http.post<CampaignDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/structures/${encodeURIComponent(structureTypeId)}/image`,
        form,
        { withCredentials: true },
      ),
    );
  }

  async uploadFlagImage(campaignId: string, factionId: string, file: File, revision: number): Promise<CampaignDetail> {
    const form = new FormData();
    form.set('image', file);
    form.set('revision', String(revision));
    return firstValueFrom(
      this.http.post<CampaignDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/factions/${encodeURIComponent(factionId)}/flag`,
        form,
        { withCredentials: true },
      ),
    );
  }

  async uploadMissionFile(
    campaignId: string,
    missionId: string,
    file: File,
    revision: number,
  ): Promise<CampaignDetail> {
    const form = new FormData();
    form.set('file', file);
    form.set('revision', String(revision));
    return firstValueFrom(
      this.http.post<CampaignDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/missions/${encodeURIComponent(missionId)}/file`,
        form,
        { withCredentials: true },
      ),
    );
  }

  async getPlay(campaignId: string): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.get<CampaignPlayDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/play`, {
        withCredentials: true,
      }),
    );
  }

  async chooseFaction(campaignId: string, payload: ChooseFactionPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/play/faction`, payload, {
        withCredentials: true,
      }),
    );
  }

  async saveDraft(campaignId: string, payload: SaveOrderDraftPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/play/draft`, payload, {
        withCredentials: true,
      }),
    );
  }

  async commitOrders(campaignId: string, payload: PlayRevisionPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/play/commit`, payload, {
        withCredentials: true,
      }),
    );
  }

  async uncommitOrders(campaignId: string, payload: PlayRevisionPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/play/uncommit`, payload, {
        withCredentials: true,
      }),
    );
  }

  async submitBattleResult(campaignId: string, payload: SubmitBattleResultPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/battle-result`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async acceptBattleResult(campaignId: string, battleId: string, revision: number): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/accept-result`,
        { revision, battleId },
        { withCredentials: true },
      ),
    );
  }

  async submitRetreat(campaignId: string, payload: SubmitRetreatPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/play/retreat`, payload, {
        withCredentials: true,
      }),
    );
  }

  async resolveBattle(campaignId: string, payload: SubmitBattleResultPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/gm-resolve-battle`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async extendSchedule(campaignId: string, payload: ExtendCampaignSchedulePayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/extend-schedule`,
        payload,
        { withCredentials: true },
      ),
    );
  }
}
