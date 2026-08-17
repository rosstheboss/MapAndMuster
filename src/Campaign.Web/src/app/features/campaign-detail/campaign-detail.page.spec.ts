import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { CampaignDetailPage } from './campaign-detail.page';
import type { CampaignPlayDetail } from '../../core/campaigns/campaign.models';
import { cookieNameFor, writeStoredPrefs } from '../../core/campaigns/campaign-view-prefs.service';

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
  participants: [
    {
      userId: 'user-1',
      username: 'northplayer',
      displayName: 'northplayer',
      isPlayer: true,
      isGameMaster: true,
      isAdministrator: false,
      factionName: 'North',
      subfaction: 'Riders',
    },
  ],
  log: [],
  standings: [
    {
      userId: 'user-1',
      username: 'northplayer',
      displayName: 'northplayer',
      factionId: '1',
      factionName: 'North',
      factionColor: '#2563EB',
      hasFlagImage: false,
      allyGroupName: null,
      territoryAndStructurePoints: 4,
      battlesWonPoints: 2,
      publicObjectivePoints: 1,
      otherPoints: 3,
      total: 10,
      heldItems: [{ typeId: 'crown', name: 'Crown', builtinSymbol: 'Crown', color: '#C45C26', hasImage: false }],
    },
    {
      userId: 'user-2',
      username: 'southplayer',
      displayName: 'Ada',
      factionId: '2',
      factionName: 'South',
      factionColor: '#DC2626',
      hasFlagImage: false,
      allyGroupName: null,
      territoryAndStructurePoints: 1,
      battlesWonPoints: 0,
      publicObjectivePoints: 0,
      otherPoints: 0,
      total: 1,
      heldItems: [],
    },
  ],
};

function flushPlayUnavailable(http: HttpTestingController): void {
  http
    .expectOne(`/api/campaigns/${campaign.id}/play`)
    .flush(
      { code: 'play.not_started', message: 'This campaign has not started yet.' },
      { status: 400, statusText: 'Bad Request' },
    );
}

function playState(overrides: Partial<CampaignPlayDetail> = {}): CampaignPlayDetail {
  return {
    id: campaign.id,
    name: campaign.name,
    revision: campaign.revision,
    canManage: true,
    canDebug: true,
    isDebugActive: false,
    debugActorUserId: null,
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
        moveTargets: ['t2'],
        availableActions: ['Hold', 'Move'],
      },
    ],
    myDrafts: [],
    orders: [],
    debugDrafts: [],
    commitments: [{ userId: 'user-1', username: 'northplayer', isCommitted: false }],
    battles: [],
    log: [],
    playersMissingFaction: [],
    ...overrides,
  };
}

describe('CampaignDetailPage', () => {
  beforeEach(async () => {
    document.cookie.split(';').forEach((part) => {
      const name = part.split('=')[0]?.trim();
      if (name) {
        document.cookie = `${name}=; Path=/; Max-Age=0`;
      }
    });
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
    expect(compiled.textContent).toContain('Participants');
    expect(compiled.querySelector('a[href^="/users/northplayer"]')?.textContent.trim()).toBe('northplayer');
    expect(compiled.textContent).toContain('Manager, Player');
    expect(compiled.textContent).toContain("Your force starts at that faction's spawn");
    expect(compiled.textContent).toContain('Campaign points');
    expect(compiled.textContent).toContain('Structures captured');
    expect(compiled.querySelector('.standings-table')?.textContent).toContain('northplayer');
    expect(compiled.querySelector('.standings-table')?.textContent).toContain('Ada');
    expect(compiled.textContent).toContain('Collapse All');
    expect(compiled.querySelector('a.button')?.textContent).toContain('Edit campaign');
    expect(compiled.textContent).toContain('Edit map');
    expect([...compiled.querySelectorAll('a, button')].some((element) => element.textContent.trim() === 'Play')).toBe(
      false,
    );
    expect([...compiled.querySelectorAll('a, button')].some((element) => element.textContent.trim() === 'View')).toBe(
      false,
    );

    const deleteButton = compiled.querySelector<HTMLButtonElement>('button.button-danger');
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
    const page = fixture.componentInstance as unknown as {
      postChat(payload: { message: string; channelKind: string; targetId: string | null }): Promise<void>;
    };
    const pending = page.postChat({ message: 'Ready to play', channelKind: 'Public', targetId: null });
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

    const page = fixture.componentInstance as unknown as {
      postChat(payload: { message: string; channelKind: string; targetId: string | null }): Promise<void>;
    };
    const pending = page.postChat({ message: 'Ready to play', channelKind: 'Public', targetId: null });
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

  it('shows orders on the campaign page during an active campaign', async () => {
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
      structureTypes: [
        {
          id: 'town',
          name: 'Town',
          builtinSymbol: 'Town',
          hasImage: false,
          hasPillagedImage: false,
          isBuildable: false,
          isPillageable: true,
          isDestructible: true,
          missions: [],
        },
      ],
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
          structureTypeId: 'town',
          structureCondition: 'Pillaged',
          overlayColor: null,
          ownerFactionId: '1',
          spawnFactionId: '1',
        },
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
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
      }),
    );
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Campaign log');
    expect(compiled.textContent).toContain('The campaign started.');
    expect(compiled.textContent).toContain('Campaign:');
    expect(compiled.textContent).toContain('Phase ends in');
    expect(compiled.textContent).toContain('Round 1 - Action 1');
    expect(compiled.textContent).toContain('Commit Actions');
    expect(compiled.textContent).toContain('Debug');
    expect(compiled.textContent).toContain('Spawn location is at Coast');
    expect(compiled.textContent).not.toContain('Choose your faction');
    expect([...compiled.querySelectorAll('a, button')].some((element) => element.textContent.trim() === 'Play')).toBe(
      false,
    );
    const page = fixture.componentInstance as unknown as { hoveredTerritoryId: { set(id: string): void } };
    page.hoveredTerritoryId.set('t1');
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Town (pillaged)');
    expect(compiled.textContent).toContain('Forces: northplayer · North');
    http.verify();
  });

  it('shows a full-width map and a download control when a map exists', async () => {
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
    expect(compiled.querySelector('app-campaign-map-view')).toBeTruthy();
    expect(compiled.querySelector('app-campaign-map-preview')).toBeNull();
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
    posted.flush({ ...campaign, factionId: '1', canChooseFaction: true, revision: 2 });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Your faction');
    expect(compiled.textContent).toContain('You can change your faction until the campaign starts');
    expect(compiled.textContent).toContain('North');
    expect(compiled.querySelector('#faction')).toBeTruthy();

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
          canChooseFaction: true,
          revision: 2,
        });
      }
    }
    await fixture.whenStable();
    http.verify();
  });

  it('saves a draft and commits orders on the campaign page', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: false,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
      currentRound: 1,
      currentPhaseNumber: 1,
      currentPhaseKind: 'Action',
      currentPhaseStartsUtc: '2026-08-14T12:00:00+00:00',
      currentPhaseEndsUtc: '2026-08-14T12:06:00+00:00',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState({ hasMap: false }));
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Commit Actions');
    const commitBefore = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commit Actions',
    );
    expect(commitBefore?.hasAttribute('disabled')).toBe(true);
    const saveDraft = compiled.querySelector('button[aria-label^="Save draft"]');
    expect(saveDraft).toBeTruthy();
    (saveDraft as HTMLButtonElement).click();
    const draft = http.expectOne(`/api/campaigns/${campaign.id}/play/draft`);
    expect((draft.request.body as { kind: string }).kind).toBe('Hold');
    draft.flush(
      playState({
        hasMap: false,
        revision: 3,
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
      }),
    );
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: 3,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Successfully saved changes.');
    expect(compiled.textContent).toContain('Draft: Hold');

    const commit = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commit Actions',
    );
    expect(commit?.hasAttribute('disabled')).toBe(false);
    commit?.click();
    http
      .expectOne(`/api/campaigns/${campaign.id}/play/commit`)
      .flush(playState({ hasMap: false, revision: 4, isCommitted: true }));
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: 4,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Uncommit returns them to draft until this action window closes');
    const uncommit = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Uncommit',
    );
    expect(uncommit).toBeTruthy();
    uncommit?.click();
    http.expectOne(`/api/campaigns/${campaign.id}/play/uncommit`).flush(
      playState({
        hasMap: false,
        revision: 5,
        isCommitted: false,
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
      }),
    );
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: 5,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Commit Actions');
    expect([...compiled.querySelectorAll('button')].some((button) => button.textContent.trim() === 'Uncommit')).toBe(
      false,
    );
    http.verify();
  });

  it('drafts a map action from a territory menu and cancels from the map background', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [
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
      {
        id: 't2',
        displayNumber: 2,
        name: 'Ridge',
        description: null,
        polygon: [
          { x: 0.5, y: 0.1 },
          { x: 0.8, y: 0.1 },
          { x: 0.8, y: 0.4 },
          { x: 0.5, y: 0.4 },
        ],
        terrainTypeId: 'plains',
        structureTypeId: null,
        structureCondition: 'Operational',
        overlayColor: null,
        ownerFactionId: '2',
        spawnFactionId: '2',
      },
    ];
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
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories,
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState());
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean; clientX: number; clientY: number }) => void;
      onMapActionKind: (kind: string) => void;
      onMapBackgroundSelect: () => void;
      confirmMapAction: () => Promise<void>;
      mapAction: () => { step: string; kind: string; targetTerritoryId: string } | null;
    };

    page.onTerritorySelect({ id: 't1', additive: false, clientX: 40, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('menu');
    expect(compiled.querySelector('.action-context-menu')?.textContent).toContain('Move');

    page.onMapBackgroundSelect();
    fixture.detectChanges();
    expect(page.mapAction()).toBeNull();

    page.onTerritorySelect({ id: 't1', additive: false, clientX: 40, clientY: 12 });
    page.onMapActionKind('Move');
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('pick-target');
    expect(compiled.textContent).toContain('Select a destination for Move.');

    page.onTerritorySelect({ id: 't2', additive: false, clientX: 90, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('confirm');
    expect(compiled.textContent).toContain('Confirm action');
    expect(compiled.textContent).toContain('Move from Coast to Ridge?');

    const pending = page.confirmMapAction();
    const draft = http.expectOne(`/api/campaigns/${campaign.id}/play/draft`);
    expect((draft.request.body as { kind: string; targetTerritoryId: string }).kind).toBe('Move');
    expect((draft.request.body as { kind: string; targetTerritoryId: string }).targetTerritoryId).toBe('t2');
    draft.flush(
      playState({
        revision: 3,
        myDrafts: [{ forceId: 'force-1', kind: 'Move', targetTerritoryId: 't2', structureTypeId: null }],
      }),
    );
    await pending;
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: 3,
      canManage: true,
      territories,
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Draft: Move to Ridge');
    http.verify();
  });

  it('reveals hidden item objectives from debug mode', async () => {
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
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        isDebugActive: true,
        itemObjectives: [
          {
            id: 'item-1',
            typeId: 'crown',
            name: 'Crown',
            territoryId: 't1',
            possessorForceId: null,
            isRevealed: false,
          },
        ],
      }),
    );
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Reveal hidden objectives');
    expect(compiled.textContent).toContain('Crown');
    expect(compiled.textContent).toContain('(hidden)');

    const page = fixture.componentInstance as unknown as { revealHiddenObjectives: () => Promise<void> };
    const pending = page.revealHiddenObjectives();
    const request = http.expectOne(`/api/campaigns/${campaign.id}/play/debug/reveal-hidden-objectives`);
    request.flush(
      playState({
        isDebugActive: true,
        itemObjectives: [
          {
            id: 'item-1',
            typeId: 'crown',
            name: 'Crown',
            territoryId: 't1',
            possessorForceId: null,
            isRevealed: true,
          },
        ],
      }),
    );
    await pending;
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Crown');
    expect(compiled.textContent).not.toContain('(hidden)');
    http.verify();
  });

  it('sorts campaign point standings when a column header is clicked', async () => {
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
    const names = (): (string | undefined)[] =>
      [...compiled.querySelectorAll('.standings-table tbody tr')].map((row) =>
        row.querySelector('.profile-link')?.textContent.trim(),
      );
    expect(names()).toEqual(['northplayer', 'Ada']);

    const nameHeader = [...compiled.querySelectorAll<HTMLButtonElement>('.standings-table th button')].find(
      (button) => button.textContent.trim() === 'Display name',
    );
    expect(nameHeader).toBeTruthy();
    nameHeader!.click();
    fixture.detectChanges();
    expect(names()).toEqual(['Ada', 'northplayer']);
    http.verify();
  });

  it('restores map highlight mode and collapsed panels from the view cookie', async () => {
    writeStoredPrefs(campaign.id, {
      highlightMode: 'faction',
      sections: { map: false, standings: true },
      standingsSort: { column: 'displayName', direction: 'asc' },
      chatChannelKey: 'Public:',
      chatScrollTop: 12,
    });
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({ ...campaign, hasMap: true });
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

    const page = fixture.componentInstance as unknown as {
      highlightMode: () => string;
      isOpen: (id: string) => boolean;
      standingsSort: () => { column: string; direction: string };
    };
    expect(page.highlightMode()).toBe('faction');
    expect(page.isOpen('map')).toBe(false);
    expect(page.isOpen('standings')).toBe(true);
    expect(page.standingsSort()).toEqual({ column: 'displayName', direction: 'asc' });
    expect(document.cookie).toContain(cookieNameFor(campaign.id));
    http.verify();
  });
});
