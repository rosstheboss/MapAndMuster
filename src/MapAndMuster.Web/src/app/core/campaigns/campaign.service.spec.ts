import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CampaignService } from './campaign.service';

describe('CampaignService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('lists campaigns for the signed-in user', async () => {
    const service = TestBed.inject(CampaignService);
    const http = TestBed.inject(HttpTestingController);
    const pending = service.list();
    const request = http.expectOne('/api/campaigns');
    request.flush([
      {
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        name: 'Border War',
        playerSlotCount: 8,
        occupiedPlayerSlots: 1,
        isPrivate: false,
        canManage: true,
        isParticipant: true,
        status: 'Scheduled',
        startsUtc: '2099-01-05T12:00:00+00:00',
        endsUtc: '2099-03-02T12:00:00+00:00',
      },
    ]);

    const campaigns = await pending;
    expect(campaigns[0]?.name).toBe('Border War');
    http.verify();
  });

  it('lists all discoverable campaigns and can join or leave', async () => {
    const service = TestBed.inject(CampaignService);
    const http = TestBed.inject(HttpTestingController);
    const listing = service.listAll();
    http.expectOne('/api/campaigns/all').flush([]);
    expect(await listing).toEqual([]);

    const joining = service.join('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'join-secret');
    const join = http.expectOne('/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/join');
    expect(join.request.method).toBe('POST');
    expect(join.request.body).toEqual({ joinPassword: 'join-secret' });
    join.flush({
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      name: 'Border War',
      description: null,
      playerSlotCount: 8,
      occupiedPlayerSlots: 2,
      isPrivate: true,
      isPubliclyViewable: true,
      canManage: false,
      isParticipant: true,
      canView: true,
      canJoin: false,
      canLeave: true,
      city: null,
      region: null,
      country: null,
      status: 'Scheduled',
      startsUtc: '2099-01-05T12:00:00+00:00',
      endsUtc: '2099-03-02T12:00:00+00:00',
      currentRound: null,
      currentPhaseLabel: null,
      currentPhaseEndsUtc: null,
    });
    expect((await joining).canLeave).toBe(true);

    const leaving = service.leave('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    const leave = http.expectOne('/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/leave');
    expect(leave.request.method).toBe('POST');
    leave.flush(null, { status: 204, statusText: 'No Content' });
    await leaving;
    http.verify();
  });

  it('loads and saves a map graph', async () => {
    const service = TestBed.inject(CampaignService);
    const http = TestBed.inject(HttpTestingController);
    const pending = service.getMapGraph('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    http.expectOne('/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/map/graph').flush({
      campaignId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      revision: 2,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    const graph = await pending;
    expect(graph.revision).toBe(2);

    const saving = service.saveMapGraph('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', {
      revision: 2,
      territories: [],
      adjacencies: [],
    });
    const put = http.expectOne('/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/map/graph');
    expect(put.request.method).toBe('PUT');
    put.flush({
      campaignId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      revision: 3,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    expect((await saving).revision).toBe(3);
    http.verify();
  });

  it('downloads and uploads a campaign preset package', async () => {
    const service = TestBed.inject(CampaignService);
    const http = TestBed.inject(HttpTestingController);

    const downloading = service.downloadCampaignPresetPackage('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    const download = http.expectOne('/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/preset-package');
    expect(download.request.method).toBe('GET');
    download.flush(new Blob(['zip']), {
      headers: { 'Content-Disposition': 'attachment; filename="border-war-preset.mapandmuster-preset"' },
    });
    const file = await downloading;
    expect(file.filename).toBe('border-war-preset.mapandmuster-preset');

    const uploading = service.importPresetPackage(new File(['zip'], 'border-war-preset.mapandmuster-preset'));
    const upload = http.expectOne('/api/campaign-presets/package');
    expect(upload.request.method).toBe('POST');
    expect(upload.request.body).toBeInstanceOf(FormData);
    upload.flush({ id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: 'Border War', hasMap: true });
    expect((await uploading).name).toBe('Border War');
    http.verify();
  });

  it('loads a campaign log separately from campaign detail', async () => {
    const service = TestBed.inject(CampaignService);
    const http = TestBed.inject(HttpTestingController);
    const pending = service.getLog('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    const request = http.expectOne('/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/log');
    expect(request.request.method).toBe('GET');
    request.flush({
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      revision: 1,
      canChat: true,
      mentionableMembers: [],
      chatChannels: [],
      log: [],
      lastReadUtc: null,
      unreadMentionCount: 0,
      unreadPrivateCount: 0,
    });
    expect((await pending).canChat).toBe(true);
    http.verify();
  });

  it('ends a campaign with its current revision', async () => {
    const service = TestBed.inject(CampaignService);
    const http = TestBed.inject(HttpTestingController);
    const pending = service.end('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 4);
    const request = http.expectOne('/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/end');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ revision: 4 });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await pending;
    http.verify();
  });

  it('adds a campaign manager without occupying a player slot', async () => {
    const service = TestBed.inject(CampaignService);
    const http = TestBed.inject(HttpTestingController);
    const pending = service.addMember('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'user-3', 2, {
      isGameMaster: true,
      isPlayer: false,
    });
    const request = http.expectOne('/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/members');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      userId: 'user-3',
      revision: 2,
      isGameMaster: true,
      isPlayer: false,
    });
    request.flush({
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      name: 'Border War',
      description: null,
      playerSlotCount: 8,
      occupiedPlayerSlots: 1,
      isPrivate: false,
      isPubliclyViewable: true,
      canManage: true,
      isParticipant: true,
      revision: 3,
      status: 'Scheduled',
      startsUtc: '2099-01-05T12:00:00+00:00',
      endsUtc: '2099-03-02T12:00:00+00:00',
    });
    expect((await pending).revision).toBe(3);
    http.verify();
  });

  it('marks a campaign log read', async () => {
    const service = TestBed.inject(CampaignService);
    const http = TestBed.inject(HttpTestingController);
    const pending = service.markLogRead('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    const request = http.expectOne('/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/log/read');
    expect(request.request.method).toBe('POST');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await pending;
    http.verify();
  });

  it('downloads a campaign log export', async () => {
    const service = TestBed.inject(CampaignService);
    const http = TestBed.inject(HttpTestingController);
    const pending = service.exportLog('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', {
      includePublicChat: true,
      includeGameLog: false,
      format: 'csv',
    });
    const request = http.expectOne(
      (item) =>
        item.method === 'GET' &&
        item.url === '/api/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/log-export' &&
        item.params.get('includePublicChat') === 'true' &&
        item.params.get('includeGameLog') === 'false' &&
        item.params.get('format') === 'csv',
    );
    request.flush(new Blob(['OccurredUtc']), {
      headers: { 'Content-Disposition': 'attachment; filename="border-war-log.csv"' },
    });
    const file = await pending;
    expect(file.filename).toBe('border-war-log.csv');
    http.verify();
  });
});
