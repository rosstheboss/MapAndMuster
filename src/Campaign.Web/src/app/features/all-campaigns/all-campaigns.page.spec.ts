import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { AllCampaignsPage } from './all-campaigns.page';

function item(
  overrides: Partial<CampaignListItem> & Pick<CampaignListItem, 'id' | 'name' | 'status' | 'startsUtc' | 'endsUtc'>,
): CampaignListItem {
  return {
    description: null,
    playerSlotCount: 8,
    occupiedPlayerSlots: 1,
    isPrivate: false,
    isPubliclyViewable: true,
    canManage: false,
    isParticipant: false,
    canView: true,
    canJoin: true,
    canLeave: false,
    city: null,
    region: null,
    country: null,
    currentRound: null,
    currentPhaseLabel: null,
    currentPhaseEndsUtc: null,
    canPlay: false,
    ...overrides,
  };
}

describe('AllCampaignsPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AllCampaignsPage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('loads discoverable campaigns from the all-campaigns endpoint', async () => {
    const fixture = TestBed.createComponent(AllCampaignsPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/campaigns/all').flush([
      item({
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        name: 'Open War',
        city: 'Austin',
        region: 'Texas',
        country: 'United States',
        status: 'Scheduled',
        startsUtc: '2099-01-05T12:00:00+00:00',
        endsUtc: '2099-03-02T12:00:00+00:00',
      }),
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('All campaigns');
    expect(compiled.textContent).toContain('Upcoming campaigns');
    expect(compiled.querySelector('button.group-toggle')?.textContent).toContain('Upcoming campaigns');
    const toggle = compiled.querySelector<HTMLButtonElement>('button.campaign-toggle');
    expect(toggle?.textContent).toContain('Open War');
    toggle?.click();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Austin, Texas, United States');
    expect(compiled.querySelector('a[href="/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"]')?.textContent).toContain(
      'View',
    );
    expect([...compiled.querySelectorAll('button')].some((button) => button.textContent.trim() === 'Join')).toBe(true);
    http.verify();
  });

  it('explains when no campaigns are listed', async () => {
    const fixture = TestBed.createComponent(AllCampaignsPage);
    TestBed.inject(HttpTestingController).expectOne('/api/campaigns/all').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No campaigns are available to join or view right now.');
  });
});
