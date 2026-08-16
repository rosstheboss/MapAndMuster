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
      availableActions: ['Hold', 'Move'],
    },
  ],
  myDrafts: [],
  orders: [],
  commitments: [{ userId: 'user-1', username: 'northplayer', isCommitted: false }],
  battles: [],
  log: [],
  playersMissingFaction: [],
  canChat: true,
  mentionableMembers: [{ userId: 'user-1', username: 'northplayer', displayName: 'northplayer' }],
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
    expect(compiled.textContent).toContain('The latest saved draft is what commits if time runs out.');
    expect(compiled.textContent).toContain('No draft saved yet. If time runs out, this force Holds.');
    const kinds = [...compiled.querySelectorAll<HTMLOptionElement>('#kind-force-1 option')].map(
      (option) => option.value,
    );
    expect(kinds).toEqual(['Hold', 'Move']);
    expect(kinds).not.toContain('Pillage');

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
    expect(compiled.textContent).toContain('Saved draft: Hold');

    const commit = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commit orders',
    );
    commit?.click();
    http.expectOne(`/api/campaigns/${campaignId}/play/commit`).flush({ ...play, revision: 4, isCommitted: true });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('return them to draft until this action window closes');
    http.verify();
  });

  it('saves a complete action selection as a draft', async () => {
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

    const page = fixture.componentInstance as unknown as { onDraftKind(forceId: string, kind: string): void };
    page.onDraftKind('force-1', 'Hold');
    const draft = http.expectOne(`/api/campaigns/${campaignId}/play/draft`);
    expect((draft.request.body as { kind: string }).kind).toBe('Hold');
    draft.flush({
      ...play,
      revision: 3,
      myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Saved draft: Hold');
    http.verify();
  });

  it('saves unsaved drafts before committing', async () => {
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
    const commit = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commit orders',
    );
    commit?.click();
    const draft = http.expectOne(`/api/campaigns/${campaignId}/play/draft`);
    expect((draft.request.body as { kind: string }).kind).toBe('Hold');
    draft.flush({
      ...play,
      revision: 3,
      myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
    });
    await fixture.whenStable();
    const commitReq = http.expectOne(`/api/campaigns/${campaignId}/play/commit`);
    expect((commitReq.request.body as { revision: number }).revision).toBe(3);
    commitReq.flush({ ...play, revision: 4, isCommitted: true });
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

  it('shows the player force on the map and in territory details', async () => {
    const fixture = TestBed.createComponent(CampaignPlayPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}/play`).flush({ ...play, hasMap: true });
    http.expectOne(`/api/campaigns/${campaignId}/map/graph`).flush({
      campaignId,
      revision: 2,
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
          ownerFactionId: 'north',
          spawnFactionId: 'north',
        },
      ],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const pin = compiled.querySelector('.force-pin.is-mine');
    expect(pin).toBeTruthy();
    expect(pin?.getAttribute('aria-label')).toContain('northplayer');
    const page = fixture.componentInstance as unknown as { hoveredTerritoryId: { set(id: string): void } };
    page.hoveredTerritoryId.set('t1');
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Forces: northplayer · North');
    http.verify();
  });

  it('posts chat without the save success banner', async () => {
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

    const page = fixture.componentInstance as unknown as { postChat(message: string): Promise<void> };
    const pending = page.postChat('Hold the coast');
    http.expectOne(`/api/campaigns/${campaignId}/chat`).flush({
      id: campaignId,
      revision: 3,
      canChat: true,
      mentionableMembers: play.mentionableMembers,
      log: [
        {
          id: 'log-2',
          occurredUtc: '2026-08-15T20:46:23-04:00',
          kind: 'PlayerChat',
          originator: 'northplayer',
          summary: 'Hold the coast',
          territoryId: null,
          forceId: null,
          battleId: null,
          isSystemAdjustment: false,
        },
      ],
    });
    await pending;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Hold the coast');
    expect(compiled.textContent).not.toContain('Successfully saved changes.');
    http.verify();
  });
});
