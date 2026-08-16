import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { CampaignDetailPage } from './campaign-detail.page';

const campaign = {
  id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  name: 'Border War',
  description: 'A contested frontier.',
  playerSlotCount: 8,
  occupiedPlayerSlots: 1,
  isPrivate: true,
  isPubliclyViewable: true,
  creatorIsParticipant: true,
  city: 'Halifax',
  region: 'Nova Scotia',
  country: 'Canada',
  hasMap: false,
  canManage: true,
  isParticipant: true,
  revision: 1,
  createdUtc: '2026-08-13T00:00:00+00:00',
  updatedUtc: '2026-08-13T00:00:00+00:00',
  factions: [
    {
      id: '1',
      name: 'North',
      color: '#2563EB',
      subfactions: ['Riders'],
      allyGroupName: null,
      requiresSubfaction: false,
      hasFlagImage: false,
    },
    {
      id: '2',
      name: 'South',
      color: '#DC2626',
      subfactions: [],
      allyGroupName: null,
      requiresSubfaction: false,
      hasFlagImage: false,
    },
  ],
  allyGroups: [],
  links: [{ id: '3', label: 'Notes', url: 'https://example.test/notes' }],
  terrainTypes: [],
  structureTypes: [],
  timeZoneId: 'UTC',
  startsAtLocal: '2099-01-05T12:00',
  startsUtc: '2099-01-05T12:00:00+00:00',
  endsUtc: '2099-03-02T12:00:00+00:00',
  roundCount: 8,
  roundLengthAmount: 1,
  roundLengthUnit: 'Weeks',
  phases: [
    { kind: 'Action', durationAmount: 3, durationUnit: 'Days' },
    { kind: 'Action', durationAmount: 3, durationUnit: 'Days' },
    { kind: 'Battle', durationAmount: 1, durationUnit: 'Days' },
  ],
  status: 'Scheduled',
  currentRound: null,
  currentPhaseNumber: null,
  currentPhaseKind: null,
  currentPhaseStartsUtc: null,
  currentPhaseEndsUtc: null,
  factionId: null,
  subfaction: null,
  canPlay: false,
  canChooseFaction: true,
  canChat: true,
  mentionableMembers: [{ userId: 'user-1', username: 'northplayer', displayName: 'northplayer' }],
  log: [],
};

function flushPlayUnavailable(http: HttpTestingController): void {
  http
    .expectOne(`/api/campaigns/${campaign.id}/play`)
    .flush(
      { code: 'play.not_started', message: 'This campaign has not started yet.' },
      { status: 400, statusText: 'Bad Request' },
    );
}

describe('CampaignDetailPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignDetailPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => campaign.id } }, paramMap: of() },
        },
      ],
    }).compileComponents();
  });

  it('shows setup metadata and asks before deleting', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Border War');
    expect(compiled.textContent).toContain('A contested frontier.');
    expect(compiled.textContent).toContain('Halifax, Nova Scotia, Canada');
    expect(compiled.textContent).toContain('North');
    expect(compiled.textContent).toContain('Private campaign');
    expect(compiled.textContent).toContain('Scheduled');
    expect(compiled.textContent).toContain('8');
    expect(compiled.textContent).toContain('1 week');
    expect(compiled.textContent).toContain('Action 1 · 3 days');
    expect(compiled.textContent).toContain('Battle phase · 1 day');
    expect(compiled.textContent).toContain('Choose your faction');
    expect(compiled.querySelector('#faction')).toBeTruthy();
    expect(compiled.textContent).toContain('Campaign log');
    expect(compiled.textContent).toContain("Your force starts at that faction's spawn");
    expect(compiled.querySelector('a.button')?.textContent).toContain('Edit campaign');
    expect(compiled.textContent).toContain('Edit map');

    const deleteButton = [...compiled.querySelectorAll('button')].find((button) =>
      button.textContent.includes('Delete campaign'),
    );
    expect(deleteButton).toBeTruthy();
    deleteButton!.click();
    fixture.detectChanges();
    expect(compiled.querySelector('[role="alertdialog"]')?.textContent).toContain('Delete this campaign?');
    expect(compiled.querySelector('app-campaign-map-preview')).toBeNull();
    expect(compiled.textContent).not.toContain('Download map');
    http.verify();
  });

  it('shows the campaign log and posts chat from an upcoming campaign', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      log: [
        {
          id: 'log-1',
          occurredUtc: '2026-08-15T20:45:23-04:00',
          kind: 'PlayerChat',
          originator: 'northplayer',
          summary: 'Hey, everybody! This is a message to all of you.',
          territoryId: null,
          forceId: null,
          battleId: null,
          isSystemAdjustment: false,
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('northplayer:');
    expect(compiled.textContent).toContain('Hey, everybody! This is a message to all of you.');
    const page = fixture.componentInstance as unknown as { postChat(message: string): Promise<void> };
    const pending = page.postChat('Ready to play');
    const posted = http.expectOne(`/api/campaigns/${campaign.id}/chat`);
    expect((posted.request.body as { message: string }).message).toBe('Ready to play');
    posted.flush({
      ...campaign,
      revision: 2,
      log: [
        {
          id: 'log-2',
          occurredUtc: '2026-08-15T20:46:23-04:00',
          kind: 'PlayerChat',
          originator: 'northplayer',
          summary: 'Ready to play',
          territoryId: null,
          forceId: null,
          battleId: null,
          isSystemAdjustment: false,
        },
      ],
    });
    await pending;
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Ready to play');
    expect(compiled.textContent).not.toContain('Successfully saved changes.');
    expect(compiled.textContent).not.toContain('Saving');

    const pageLog = fixture.componentInstance as unknown as { pullLog(): Promise<void> };
    const pendingLog = pageLog.pullLog();
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      revision: 3,
      log: [
        {
          id: 'log-3',
          occurredUtc: '2026-08-15T20:47:23-04:00',
          kind: 'PlayerChat',
          originator: 'southplayer',
          summary: 'See you on the map.',
          territoryId: null,
          forceId: null,
          battleId: null,
          isSystemAdjustment: false,
        },
      ],
    });
    await pendingLog;
    fixture.detectChanges();
    expect(compiled.textContent).toContain('See you on the map.');
    http.verify();
  });

  it('shows a chat error without the save success banner', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as { postChat(message: string): Promise<void> };
    const pending = page.postChat('Ready to play');
    http
      .expectOne(`/api/campaigns/${campaign.id}/chat`)
      .flush(
        { code: 'campaign.concurrency', message: 'This campaign changed. Reload and try again.' },
        { status: 409, statusText: 'Conflict' },
      );
    await pending;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('This campaign changed. Reload and try again.');
    expect(compiled.textContent).not.toContain('Successfully saved changes.');
    http.verify();
  });

  it('shows a read-only play board with the campaign log during an active campaign', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
      currentRound: 1,
      currentPhaseNumber: 1,
      currentPhaseKind: 'Action',
      currentPhaseStartsUtc: '2026-08-14T12:00:00+00:00',
      currentPhaseEndsUtc: '2026-08-14T12:06:00+00:00',
      log: [
        {
          id: 'log-1',
          occurredUtc: '2026-08-14T12:00:00+00:00',
          kind: 'CampaignStarted',
          originator: 'Campaign',
          summary: 'The campaign started.',
          territoryId: null,
          forceId: null,
          battleId: null,
          isSystemAdjustment: false,
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [
        {
          id: 't1',
          displayNumber: 1,
          name: 'Coast',
          description: null,
          polygon: [
            { x: 0.1, y: 0.1 },
            { x: 0.4, y: 0.1 },
            { x: 0.4, y: 0.4 },
            { x: 0.1, y: 0.4 },
          ],
          terrainTypeId: 'plains',
          structureTypeId: null,
          structureCondition: 'Operational',
          overlayColor: null,
          ownerFactionId: '1',
          spawnFactionId: '1',
        },
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush({
      id: campaign.id,
      name: campaign.name,
      revision: campaign.revision,
      canManage: true,
      isParticipant: true,
      canChat: true,
      mentionableMembers: campaign.mentionableMembers,
      status: 'InProgress',
      currentRound: 1,
      currentPhaseNumber: 1,
      currentPhaseKind: 'Action',
      currentPhaseLabel: 'Action 1',
      currentPhaseStartsUtc: '2026-08-14T12:00:00+00:00',
      currentPhaseEndsUtc: '2026-08-14T12:06:00+00:00',
      currentWindowId: 'window-1',
      hasMap: true,
      factionId: '1',
      canChooseFaction: false,
      isCommitted: false,
      roundCount: 8,
      minRoundCount: 1,
      remainingWindows: [],
      factions: campaign.factions,
      structureTypes: [],
      forces: [
        {
          id: 'force-1',
          controllerUserId: 'user-1',
          controllerUsername: 'northplayer',
          factionId: '1',
          territoryId: 't1',
          isMine: true,
          inBattle: false,
          moveTargets: [],
          availableActions: ['Hold'],
        },
      ],
      myDrafts: [],
      orders: [],
      commitments: [{ userId: 'user-1', username: 'northplayer', isCommitted: false }],
      battles: [],
      log: [],
      playersMissingFaction: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Campaign log');
    expect(compiled.textContent).toContain('The campaign started.');
    expect(compiled.textContent).toContain('Campaign:');
    expect(compiled.textContent).toContain('Phase ends in');
    expect(compiled.textContent).toContain('Round 1 · Action 1');
    expect(compiled.querySelector('a.button')?.textContent).toContain('Play');
    expect(compiled.textContent).not.toContain('Commit orders');
    expect(compiled.textContent).not.toContain('Choose your faction');
    expect(compiled.textContent).not.toContain('Download map');
    const page = fixture.componentInstance as unknown as { hoveredTerritoryId: { set(id: string): void } };
    page.hoveredTerritoryId.set('t1');
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Forces: northplayer · North');
    http.verify();
  });

  it('shows a static map preview and a download control when a map exists', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({ ...campaign, hasMap: true, revision: 4 });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: 4,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Border War');
    const preview = compiled.querySelector('app-campaign-map-preview img');
    expect(preview).toBeTruthy();
    expect(preview?.getAttribute('src')).toContain(`/api/campaigns/${campaign.id}/map?v=4`);
    expect(compiled.textContent).toContain('Download map');
    http.verify();
  });

  it('lets a scheduled player save a faction choice', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      factionChoice: { set(value: string): void };
    };
    page.factionChoice.set('1');
    fixture.detectChanges();

    const save = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Save faction',
    );
    expect(save).toBeTruthy();
    save!.click();

    const posted = http.expectOne(`/api/campaigns/${campaign.id}/play/faction`);
    expect(posted.request.method).toBe('POST');
    expect((posted.request.body as { factionId: string }).factionId).toBe('1');
    posted.flush({ ...campaign, factionId: '1', canChooseFaction: false, revision: 2 });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Your faction');
    expect(compiled.textContent).toContain('North');
    expect(compiled.querySelector('#faction')).toBeNull();

    for (const request of http.match(() => true)) {
      if (request.request.url.endsWith('/map/graph')) {
        request.flush({
          campaignId: campaign.id,
          revision: 2,
          canManage: true,
          territories: [],
          adjacencies: [],
        });
      } else if (request.request.url.endsWith('/play')) {
        request.flush(
          { code: 'play.not_started', message: 'This campaign has not started yet.' },
          { status: 400, statusText: 'Bad Request' },
        );
      } else {
        request.flush({
          ...campaign,
          factionId: '1',
          canChooseFaction: false,
          revision: 2,
        });
      }
    }
    await fixture.whenStable();
    http.verify();
  });
});
