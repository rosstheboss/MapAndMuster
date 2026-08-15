import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { CampaignPlayPage } from './campaign-play.page';

const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const play = {
  id: campaignId,
  name: 'Border War',
  revision: 2,
  canManage: true,
  isParticipant: true,
  status: 'InProgress',
  currentRound: 1,
  currentPhaseNumber: 1,
  currentPhaseKind: 'Action',
  currentPhaseLabel: 'Action 1',
  currentPhaseStartsUtc: '2026-08-14T12:00:00+00:00',
  currentPhaseEndsUtc: '2026-08-14T12:06:00+00:00',
  currentWindowId: 'window-1',
  hasMap: false,
  factionId: 'north',
  canChooseFaction: false,
  isCommitted: false,
  roundCount: 3,
  minRoundCount: 1,
  remainingWindows: [
    {
      id: 'window-1',
      roundNumber: 1,
      phaseNumber: 1,
      kind: 'Action',
      label: 'Action 1',
      endsUtc: '2026-08-14T12:06:00+00:00',
    },
  ],
  factions: [
    {
      id: 'north',
      name: 'North',
      color: '#2563EB',
      subfactions: [],
      allyGroupName: null,
      requiresSubfaction: false,
      hasFlagImage: false,
    },
  ],
  structureTypes: [],
  forces: [
    {
      id: 'force-1',
      controllerUserId: 'user-1',
      controllerUsername: 'northplayer',
      factionId: 'north',
      territoryId: 't1',
      isMine: true,
      inBattle: false,
      moveTargets: ['t2'],
    },
  ],
  myDrafts: [],
  orders: [],
  commitments: [{ userId: 'user-1', username: 'northplayer', isCommitted: false }],
  battles: [],
  log: [],
  playersMissingFaction: [],
};

describe('CampaignPlayPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignPlayPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => campaignId } }, paramMap: of({ get: () => campaignId }) },
        },
      ],
    }).compileComponents();
  });

  it('loads the play surface and can save a draft then commit', async () => {
    const fixture = TestBed.createComponent(CampaignPlayPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}/play`).flush(play);
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      campaignId,
      revision: 2,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Border War');
    expect(compiled.textContent).toContain('Action 1');
    expect(compiled.textContent).toContain('Commit orders');
    expect(compiled.textContent).toContain('Campaign log');

    const saveDraft = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Save draft',
    );
    saveDraft?.click();
    const draft = http.expectOne(`/api/campaigns/${campaignId}/play/draft`);
    expect((draft.request.body as { kind: string }).kind).toBe('Hold');
    draft.flush({
      ...play,
      revision: 3,
      myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Successfully saved changes.');

    const commit = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commit orders',
    );
    commit?.click();
    http.expectOne(`/api/campaigns/${campaignId}/play/commit`).flush({ ...play, revision: 4, isCommitted: true });
    await fixture.whenStable();
    http.verify();
  });

  it('opens the campaign log with resolved events and omits unrevealed orders', async () => {
    const fixture = TestBed.createComponent(CampaignPlayPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}/play`).flush({
      ...play,
      log: [
        {
          id: 'log-1',
          occurredUtc: '2026-08-14T12:00:00+00:00',
          kind: 'ResolvedAction',
          summary: 'North held in Coast.',
          territoryId: 't1',
          forceId: 'force-1',
          battleId: null,
          isSystemAdjustment: false,
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      campaignId,
      revision: 2,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const openLog = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Campaign log',
    );
    openLog?.click();
    fixture.detectChanges();

    expect(compiled.querySelector('#log-title')?.textContent).toContain('Campaign log');
    expect(compiled.textContent).toContain('North held in Coast.');
    expect(compiled.textContent).toContain('Unrevealed secret orders are omitted.');
    http.verify();
  });

  it('hides orders until a player chooses a faction', async () => {
    const fixture = TestBed.createComponent(CampaignPlayPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}/play`).flush({
      ...play,
      factionId: null,
      canChooseFaction: true,
      playersMissingFaction: ['southplayer'],
      forces: [],
    });
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      campaignId,
      revision: 2,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Choose your faction');
    expect(compiled.textContent).toContain('You cannot submit orders until you choose a faction');
    expect(compiled.textContent).toContain('Players without a faction');
    expect(compiled.textContent).toContain('southplayer');
    expect(compiled.textContent).not.toContain('Commit orders');
    http.verify();
  });
});
