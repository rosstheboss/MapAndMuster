import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { CampaignListComponent } from './campaign-list.component';

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
    canJoin: false,
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

describe('CampaignListComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignListComponent],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('shows view and edit for a participating manager of an upcoming campaign', async () => {
    const fixture = TestBed.createComponent(CampaignListComponent);
    fixture.componentRef.setInput('campaigns', [
      item({
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        name: 'Border War',
        description: 'A contested frontier.',
        city: 'Halifax',
        region: 'Nova Scotia',
        country: 'Canada',
        canManage: true,
        isParticipant: true,
        canView: true,
        status: 'Scheduled',
        startsUtc: '2099-01-05T12:00:00+00:00',
        endsUtc: '2099-03-02T12:00:00+00:00',
      }),
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.campaign-body')).toBeNull();
    const toggle = compiled.querySelector<HTMLButtonElement>('button.campaign-toggle');
    toggle?.click();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('A contested frontier.');
    expect(compiled.textContent).toContain('1 of 8 players');
    expect(compiled.textContent).toContain('Halifax, Nova Scotia, Canada');
    expect(compiled.querySelector('a[href="/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"]')?.textContent).toContain(
      'View',
    );
    expect(
      compiled.querySelector('a[href="/campaigns/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/edit"]')?.textContent,
    ).toContain('Edit');
    expect(compiled.textContent).not.toContain('Join');
    expect(compiled.textContent).not.toContain('Leave');
    expect(compiled.textContent).not.toContain('Duplicate campaign');

    toggle?.click();
    fixture.detectChanges();
    expect(compiled.querySelector('.campaign-body')).toBeNull();
  });

  it('shows duplicate campaign on Your Campaigns listings', async () => {
    const fixture = TestBed.createComponent(CampaignListComponent);
    fixture.componentRef.setInput('allowDuplicate', true);
    fixture.componentRef.setInput('campaigns', [
      item({
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        name: 'Border War',
        canManage: true,
        isParticipant: true,
        canView: true,
        status: 'Scheduled',
        startsUtc: '2099-01-05T12:00:00+00:00',
        endsUtc: '2099-03-02T12:00:00+00:00',
      }),
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('button.campaign-toggle')?.click();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Duplicate campaign');
  });

  it('collapses a campaign group without removing the group heading', async () => {
    const fixture = TestBed.createComponent(CampaignListComponent);
    fixture.componentRef.setInput('campaigns', [
      item({
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        name: 'Border War',
        status: 'Scheduled',
        startsUtc: '2099-01-05T12:00:00+00:00',
        endsUtc: '2099-03-02T12:00:00+00:00',
      }),
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const groupToggle = compiled.querySelector<HTMLButtonElement>('button.group-toggle');
    expect(groupToggle?.textContent).toContain('Upcoming campaigns');
    expect(groupToggle?.getAttribute('aria-expanded')).toBe('true');
    expect(compiled.querySelector('button.campaign-toggle')?.textContent).toContain('Border War');

    groupToggle?.click();
    fixture.detectChanges();
    expect(groupToggle?.getAttribute('aria-expanded')).toBe('false');
    expect(compiled.querySelector('button.campaign-toggle')).toBeNull();
    expect(compiled.textContent).toContain('Upcoming campaigns');
  });

  it('shows view and join for a publicly viewable upcoming campaign', async () => {
    const fixture = TestBed.createComponent(CampaignListComponent);
    fixture.componentRef.setInput('campaigns', [
      item({
        id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        name: 'Open War',
        canJoin: true,
        canView: true,
        status: 'Scheduled',
        startsUtc: '2099-02-01T12:00:00+00:00',
        endsUtc: '2099-04-01T12:00:00+00:00',
      }),
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('button.campaign-toggle')?.click();
    fixture.detectChanges();

    expect(compiled.querySelector('a[href="/campaigns/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"]')?.textContent).toContain(
      'View',
    );
    const join = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Join');
    expect(join).toBeTruthy();
    join?.click();

    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne('/api/campaigns/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/join');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ joinPassword: null });
    request.flush(
      item({
        id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        name: 'Open War',
        isParticipant: true,
        canView: true,
        canLeave: true,
        occupiedPlayerSlots: 2,
        status: 'Scheduled',
        startsUtc: '2099-02-01T12:00:00+00:00',
        endsUtc: '2099-04-01T12:00:00+00:00',
      }),
    );
    await fixture.whenStable();
    http.verify();
  });

  it('asks for a password before joining a private campaign', async () => {
    const fixture = TestBed.createComponent(CampaignListComponent);
    fixture.componentRef.setInput('campaigns', [
      item({
        id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
        name: 'Secret War',
        isPrivate: true,
        canJoin: true,
        canView: false,
        status: 'Scheduled',
        startsUtc: '2099-02-01T12:00:00+00:00',
        endsUtc: '2099-04-01T12:00:00+00:00',
      }),
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('button.campaign-toggle')?.click();
    fixture.detectChanges();
    [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Join')?.click();
    fixture.detectChanges();

    expect(compiled.querySelector('[role="dialog"]')?.textContent).toContain('Join Secret War');
    const input = compiled.querySelector<HTMLInputElement>('#join-password');
    expect(input).toBeTruthy();
    input!.value = 'join-secret';
    input!.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    [...compiled.querySelectorAll('button')]
      .find((button) => button.textContent.trim() === 'Join' && button.closest('[role="dialog"]'))
      ?.click();

    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne('/api/campaigns/cccccccc-cccc-cccc-cccc-cccccccccccc/join');
    expect(request.request.body).toEqual({ joinPassword: 'join-secret' });
    request.flush(
      item({
        id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
        name: 'Secret War',
        isPrivate: true,
        isParticipant: true,
        canLeave: true,
        status: 'Scheduled',
        startsUtc: '2099-02-01T12:00:00+00:00',
        endsUtc: '2099-04-01T12:00:00+00:00',
      }),
    );
    await fixture.whenStable();
    http.verify();
  });

  it('shows round, phase, and a leave action for an active player', async () => {
    const fixture = TestBed.createComponent(CampaignListComponent);
    fixture.componentRef.setInput('campaigns', [
      item({
        id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
        name: 'Current War',
        isParticipant: true,
        canView: true,
        canLeave: true,
        occupiedPlayerSlots: 6,
        playerSlotCount: 6,
        status: 'InProgress',
        startsUtc: '2098-01-01T12:00:00+00:00',
        endsUtc: '2099-06-01T12:00:00+00:00',
        currentRound: 2,
        currentPhaseLabel: 'Action 1',
        currentPhaseEndsUtc: '2099-05-02T12:00:00+00:00',
        canPlay: true,
      }),
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('button.campaign-toggle')?.click();
    fixture.detectChanges();

    expect(
      compiled.querySelector('a[href="/campaigns/dddddddd-dddd-dddd-dddd-dddddddddddd/play"]')?.textContent,
    ).toContain('Play');
    expect(compiled.textContent).toContain('Round 2 · Action 1');
    expect(compiled.textContent).toContain('Phase ends in');
    const leave = [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Leave');
    expect(leave).toBeTruthy();
    leave?.click();

    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne('/api/campaigns/dddddddd-dddd-dddd-dddd-dddddddddddd/leave');
    expect(request.request.method).toBe('POST');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    http.verify();
  });
});
