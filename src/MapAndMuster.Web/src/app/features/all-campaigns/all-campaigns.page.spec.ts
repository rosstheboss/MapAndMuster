import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { emptySiteChatBoard } from '../../core/chat/site-chat.fixtures';
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
    currentPhaseKind: null,
    currentPhaseEndsUtc: null,
    canPlay: false,
    canChooseFaction: false,
    isCommitted: false,
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
    http.expectOne('/api/site-chat').flush(emptySiteChatBoard());
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('All campaigns');
    expect(compiled.querySelector('#all-campaigns-list-heading')?.textContent).toContain('Campaigns');
    expect(compiled.textContent).toContain('Site chat');
    const campaignsHeading = compiled.querySelector('#all-campaigns-list-heading');
    const siteChat = compiled.querySelector('app-site-chat');
    expect(campaignsHeading && siteChat).toBeTruthy();
    expect(
      Boolean(
        campaignsHeading &&
        siteChat &&
        siteChat.compareDocumentPosition(campaignsHeading) & Node.DOCUMENT_POSITION_FOLLOWING,
      ),
    ).toBe(true);
    expect(compiled.querySelector<HTMLDetailsElement>('app-site-chat details')?.open).toBe(false);
    expect(compiled.textContent).toContain('Upcoming campaigns');
    expect(compiled.querySelector('button.group-toggle')?.textContent).toContain('Upcoming campaigns');
    const toggle = compiled.querySelector<HTMLButtonElement>('button.campaign-toggle');
    expect(toggle?.textContent).toContain('Open War');
    expect(compiled.querySelector('a[href="/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"]')?.textContent).toContain(
      'Open',
    );
    toggle?.click();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Austin, Texas, United States');
    expect([...compiled.querySelectorAll('button')].some((button) => button.textContent.trim() === 'Join')).toBe(true);
    http.verify();
  });

  it('explains when no campaigns are listed', async () => {
    const fixture = TestBed.createComponent(AllCampaignsPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/campaigns/all').flush([]);
    http.expectOne('/api/site-chat').flush(emptySiteChatBoard());
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No campaigns are available to join or view right now.');
    expect(compiled.querySelector('a[href="/campaigns/new"]')?.textContent).toContain('Create a campaign');
    expect(compiled.textContent).toContain('Site chat');
    http.verify();
  });

  it('shows public chat above campaigns, including while chat is still loading', async () => {
    const fixture = TestBed.createComponent(AllCampaignsPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Campaigns');
    expect(compiled.textContent).toContain('Loading campaigns…');
    expect(compiled.textContent).toContain('Loading public chat...');
    const chatLoading = [...compiled.querySelectorAll('p')].find((p) => p.textContent === 'Loading public chat...');
    const campaignsHeading = compiled.querySelector('#all-campaigns-list-heading');
    expect(chatLoading && campaignsHeading).toBeTruthy();
    expect(
      Boolean(
        chatLoading &&
        campaignsHeading &&
        chatLoading.compareDocumentPosition(campaignsHeading) & Node.DOCUMENT_POSITION_FOLLOWING,
      ),
    ).toBe(true);

    http.expectOne('/api/campaigns/all').flush([]);
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(compiled.textContent).toContain('No campaigns are available to join or view right now.');
    expect(compiled.textContent).not.toContain('Loading campaigns…');
    expect(compiled.textContent).toContain('Loading public chat...');
    expect(compiled.querySelector('app-campaign-list')).toBeNull();

    http.expectOne('/api/site-chat').flush(emptySiteChatBoard());
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Site chat');
    expect(compiled.textContent).not.toContain('Loading public chat...');
    http.verify();
  });
});
