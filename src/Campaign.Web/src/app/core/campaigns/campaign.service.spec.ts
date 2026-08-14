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
      },
    ]);

    const campaigns = await pending;
    expect(campaigns[0]?.name).toBe('Border War');
    http.verify();
  });
});
