import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type {
  CampaignDetail,
  CampaignListItem,
  CampaignPlayDetail,
  CampaignPresetListItem,
  ChooseFactionPayload,
  ExtendCampaignSchedulePayload,
  InjectRingerBattlePayload,
  MapGraphDetail,
  PlayRevisionPayload,
  PostCampaignChatPayload,
  SaveCampaignPayload,
  SaveMapGraphPayload,
  SaveOrderDraftPayload,
  SetPublicObjectiveAwardPayload,
  GrantPrivateObjectivePayload,
  ClaimPrivateObjectivePayload,
  ModeratePrivateObjectivePayload,
  ResolveItemObjectiveChoicePayload,
  SubmitBattleResultPayload,
  SubmitRetreatPayload,
  ParseArmyListPayload,
  ParseArmyListResult,
  UserSearchHit,
} from './campaign.models';
import { filenameFromContentDisposition, type CampaignLogExportRequest, type CampaignLogSync } from './campaign-log';

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

  async searchUsers(campaignId: string, query: string): Promise<UserSearchHit[]> {
    return firstValueFrom(
      this.http.get<UserSearchHit[]>(`/api/campaigns/${encodeURIComponent(campaignId)}/members/search`, {
        params: { q: query },
        withCredentials: true,
      }),
    );
  }

  async addMember(
    campaignId: string,
    userId: string,
    revision: number,
    options?: { isGameMaster?: boolean; isPlayer?: boolean },
  ): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.post<CampaignDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/members`,
        {
          userId,
          revision,
          isGameMaster: options?.isGameMaster ?? false,
          isPlayer: options?.isPlayer ?? true,
        },
        { withCredentials: true },
      ),
    );
  }

  async kickMember(campaignId: string, userId: string, revision: number): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.post<CampaignDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/members/kick`,
        { userId, revision },
        { withCredentials: true },
      ),
    );
  }

  async get(campaignId: string): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.get<CampaignDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}`, { withCredentials: true }),
    );
  }

  async getLog(campaignId: string): Promise<CampaignLogSync> {
    return firstValueFrom(
      this.http.get<CampaignLogSync>(`/api/campaigns/${encodeURIComponent(campaignId)}/log`, {
        withCredentials: true,
      }),
    );
  }

  async markLogRead(campaignId: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`/api/campaigns/${encodeURIComponent(campaignId)}/log/read`, {}, { withCredentials: true }),
    );
  }

  async postChat(campaignId: string, payload: PostCampaignChatPayload): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.post<CampaignDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/chat`, payload, {
        withCredentials: true,
      }),
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

  async end(campaignId: string, revision: number): Promise<void> {
    await firstValueFrom(
      this.http.post(`/api/campaigns/${encodeURIComponent(campaignId)}/end`, { revision }, { withCredentials: true }),
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

  structureImageUrl(campaignId: string, structureTypeId: string, revision: number, pillaged = false): string {
    const kind = pillaged ? 'pillaged-image' : 'image';
    return `/api/campaigns/${encodeURIComponent(campaignId)}/structures/${encodeURIComponent(structureTypeId)}/${kind}?v=${revision}`;
  }

  itemObjectiveImageUrl(campaignId: string, itemObjectiveTypeId: string, revision: number): string {
    return `/api/campaigns/${encodeURIComponent(campaignId)}/item-objectives/${encodeURIComponent(itemObjectiveTypeId)}/image?v=${revision}`;
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
    pillaged = false,
  ): Promise<CampaignDetail> {
    const form = new FormData();
    form.set('image', file);
    form.set('revision', String(revision));
    const kind = pillaged ? 'pillaged-image' : 'image';
    return firstValueFrom(
      this.http.post<CampaignDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/structures/${encodeURIComponent(structureTypeId)}/${kind}`,
        form,
        { withCredentials: true },
      ),
    );
  }

  async uploadItemObjectiveImage(
    campaignId: string,
    itemObjectiveTypeId: string,
    file: File,
    revision: number,
  ): Promise<CampaignDetail> {
    const form = new FormData();
    form.set('image', file);
    form.set('revision', String(revision));
    return firstValueFrom(
      this.http.post<CampaignDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/item-objectives/${encodeURIComponent(itemObjectiveTypeId)}/image`,
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

  async assignFaction(
    campaignId: string,
    payload: ChooseFactionPayload & { userId: string },
  ): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/faction/assign`,
        payload,
        { withCredentials: true },
      ),
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

  async parseArmyList(campaignId: string, payload: ParseArmyListPayload): Promise<ParseArmyListResult> {
    return firstValueFrom(
      this.http.post<ParseArmyListResult>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/parse-army-list`,
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

  async submitSurrender(campaignId: string, payload: SubmitRetreatPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/play/surrender`, payload, {
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

  async injectRingerBattle(campaignId: string, payload: InjectRingerBattlePayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/inject-ringer`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async enterDebug(campaignId: string, payload: PlayRevisionPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/play/debug/enter`, payload, {
        withCredentials: true,
      }),
    );
  }

  async exitDebug(campaignId: string, payload: PlayRevisionPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(`/api/campaigns/${encodeURIComponent(campaignId)}/play/debug/exit`, payload, {
        withCredentials: true,
      }),
    );
  }

  async debugCorrectOrder(campaignId: string, payload: SaveOrderDraftPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/debug/correct-order`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async revealHiddenObjectives(campaignId: string, payload: PlayRevisionPayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/debug/reveal-hidden-objectives`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async setPublicObjectiveAward(
    campaignId: string,
    payload: SetPublicObjectiveAwardPayload,
  ): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/public-objectives/awards`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async grantPrivateObjective(campaignId: string, payload: GrantPrivateObjectivePayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/private-objectives/grant`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async claimPrivateObjective(campaignId: string, payload: ClaimPrivateObjectivePayload): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/private-objectives/claim`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async moderatePrivateObjective(
    campaignId: string,
    payload: ModeratePrivateObjectivePayload,
  ): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/private-objectives/moderate`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async resolveItemObjectiveChoice(
    campaignId: string,
    payload: ResolveItemObjectiveChoicePayload,
  ): Promise<CampaignPlayDetail> {
    return firstValueFrom(
      this.http.post<CampaignPlayDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/play/item-objectives/choices`,
        payload,
        { withCredentials: true },
      ),
    );
  }

  async listPresets(): Promise<CampaignPresetListItem[]> {
    return firstValueFrom(this.http.get<CampaignPresetListItem[]>('/api/campaign-presets', { withCredentials: true }));
  }

  async getPreset(presetId: string): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.get<CampaignDetail>(`/api/campaign-presets/${encodeURIComponent(presetId)}`, {
        withCredentials: true,
      }),
    );
  }

  async saveAsPreset(campaignId: string, name: string): Promise<CampaignPresetListItem> {
    return firstValueFrom(
      this.http.post<CampaignPresetListItem>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/presets`,
        { name },
        { withCredentials: true },
      ),
    );
  }

  async applyPresetMap(campaignId: string, presetId: string, revision: number): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.post<CampaignDetail>(
        `/api/campaigns/${encodeURIComponent(campaignId)}/apply-preset`,
        { presetId, revision },
        { withCredentials: true },
      ),
    );
  }

  async downloadCampaignPresetPackage(campaignId: string): Promise<{ blob: Blob; filename: string }> {
    const response = await firstValueFrom(
      this.http.get(`/api/campaigns/${encodeURIComponent(campaignId)}/preset-package`, {
        observe: 'response',
        responseType: 'blob',
        withCredentials: true,
      }),
    );
    return {
      blob: response.body ?? new Blob(),
      filename: filenameFromContentDisposition(
        response.headers.get('content-disposition'),
        'campaign-preset.mapandmuster-preset',
      ),
    };
  }

  async importPresetPackage(file: File): Promise<CampaignPresetListItem> {
    const form = new FormData();
    form.set('package', file);
    return firstValueFrom(
      this.http.post<CampaignPresetListItem>('/api/campaign-presets/package', form, { withCredentials: true }),
    );
  }

  async exportLog(campaignId: string, request: CampaignLogExportRequest): Promise<{ blob: Blob; filename: string }> {
    const response = await firstValueFrom(
      this.http.get(`/api/campaigns/${encodeURIComponent(campaignId)}/log-export`, {
        params: {
          includePublicChat: String(request.includePublicChat),
          includeGameLog: String(request.includeGameLog),
          format: request.format,
        },
        observe: 'response',
        responseType: 'blob',
        withCredentials: true,
      }),
    );
    return {
      blob: response.body ?? new Blob(),
      filename: filenameFromContentDisposition(
        response.headers.get('content-disposition'),
        `campaign-log.${request.format}`,
      ),
    };
  }
}
