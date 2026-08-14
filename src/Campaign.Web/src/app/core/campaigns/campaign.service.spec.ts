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
});
