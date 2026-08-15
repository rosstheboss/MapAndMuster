import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { CampaignsPage } from './campaigns.page';

function item(
  overrides: Partial<CampaignListItem> & Pick<CampaignListItem, 'id' | 'name' | 'status' | 'startsUtc' | 'endsUtc'>,
): CampaignListItem {
  return {
    description: null,
    playerSlotCount: 8,
    occupiedPlayerSlots: 1,
    isPrivate: false,
    isPubliclyViewable: true,
    canManage: true,
    isParticipant: true,
    canView: true,
    canJoin: false,
    canLeave: false,
    city: null,
    region: null,
    country: null,
    currentRound: null,
    currentPhaseLabel: null,
    currentPhaseEndsUtc: null,
    ...overrides,
  };
}

describe('CampaignsPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignsPage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('groups campaigns and keeps them collapsed until expanded', async () => {
    const fixture = TestBed.createComponent(CampaignsPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/campaigns').flush([
      item({
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
      }),
      item({
        id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        name: 'Later War',
        playerSlotCount: 4,
        occupiedPlayerSlots: 2,
        isPrivate: true,
        canManage: false,
        isParticipant: true,
        canLeave: true,
        status: 'Scheduled',
        startsUtc: '2099-02-01T12:00:00+00:00',
        endsUtc: '2099-04-01T12:00:00+00:00',
      }),
      item({
        id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
        name: 'Current War',
        playerSlotCount: 6,
        occupiedPlayerSlots: 6,
        status: 'InProgress',
        startsUtc: '2098-01-01T12:00:00+00:00',
        endsUtc: '2099-06-01T12:00:00+00:00',
        currentRound: 2,
        currentPhaseLabel: 'Battle',
        currentPhaseEndsUtc: '2099-05-20T12:00:00+00:00',
      }),
      item({
        id: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
        name: 'Sooner War',
        playerSlotCount: 4,
        occupiedPlayerSlots: 4,
        status: 'InProgress',
        startsUtc: '2098-01-01T12:00:00+00:00',
        endsUtc: '2099-05-01T12:00:00+00:00',
        currentRound: 3,
        currentPhaseLabel: 'Action 2',
        currentPhaseEndsUtc: '2099-04-20T12:00:00+00:00',
      }),
      item({
        id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
        name: 'Old War',
        playerSlotCount: 2,
        occupiedPlayerSlots: 2,
        status: 'Completed',
        startsUtc: '2097-01-01T12:00:00+00:00',
        endsUtc: '2098-12-01T12:00:00+00:00',
      }),
      item({
        id: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
        name: 'Recent War',
        playerSlotCount: 3,
        occupiedPlayerSlots: 3,
        canManage: false,
        isParticipant: true,
        canLeave: true,
        status: 'Completed',
        startsUtc: '2098-02-01T12:00:00+00:00',
        endsUtc: '2099-01-01T12:00:00+00:00',
      }),
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Your campaigns');
    expect(compiled.querySelector('a.button')?.textContent).toContain('Create campaign');
    expect(compiled.textContent).toContain('Upcoming campaigns');
    expect(compiled.textContent).toContain('Active campaigns');
    expect(compiled.textContent).toContain('Completed campaigns');
    const groups = [...compiled.querySelectorAll('button.group-toggle')].map((button) => button.textContent.trim());
    expect(groups).toEqual(['Active campaigns', 'Upcoming campaigns', 'Completed campaigns']);
    const names = [...compiled.querySelectorAll('button.campaign-toggle')].map((button) => button.textContent.trim());
    expect(names).toEqual(['Sooner War', 'Current War', 'Border War', 'Later War', 'Recent War', 'Old War']);
    const upcoming = [...compiled.querySelectorAll<HTMLButtonElement>('button.campaign-toggle')].find((button) =>
      button.textContent.includes('Border War'),
    );
    expect(upcoming?.getAttribute('aria-expanded')).toBe('false');
    expect(upcoming?.nextElementSibling).toBeNull();

    upcoming?.click();
    fixture.detectChanges();
    expect(upcoming?.getAttribute('aria-expanded')).toBe('true');
    expect(compiled.textContent).toContain('1 of 8 players');
    expect(compiled.querySelector('a[href="/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"]')?.textContent).toContain(
      'View',
    );
    expect(
      compiled.querySelector('a[href="/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/edit"]')?.textContent,
    ).toContain('Edit');
    http.verify();
  });
});
