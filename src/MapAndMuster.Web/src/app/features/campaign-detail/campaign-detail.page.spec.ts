import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';

import type { OwnProfile } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { cookieNameFor, writeStoredPrefs } from '../../core/campaigns/campaign-view-prefs.service';
import type { CampaignPlayDetail } from '../../core/campaigns/campaign.models';
import type { MapTerritory } from '../../core/maps/map-graph.models';
import type { CampaignMapViewComponent } from '../../shared/campaign-map-view/campaign-map-view.component';
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
      currentSupplyPoints: 4,
      temporarySupplyPoints: 1,
      contributions: [
        { kind: 'TerritoryTerrain', label: 'Coast terrain (Plains)', points: 1, isAllied: false },
        { kind: 'Temporary', label: 'Temporary supply', points: 1, isAllied: false },
      ],
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
      privateObjectivePoints: 0,
      otherPoints: 3,
      total: 10,
      territoryAndStructureSources: [{ label: 'Town', points: 4 }],
      battleSources: [{ label: 'Resolved battles', points: 2 }],
      publicObjectiveSources: [{ label: 'Most territories', points: 1 }],
      otherSources: [{ label: 'Crown', points: 3 }],
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
      privateObjectivePoints: 0,
      otherPoints: 0,
      total: 1,
      territoryAndStructureSources: [{ label: 'Town', points: 1 }],
      heldItems: [],
    },
  ],
};

function viewerProfile(userId: string): OwnProfile {
  return {
    id: userId,
    email: 'north@example.test',
    username: 'northplayer',
    firstName: 'North',
    middleInitial: null,
    lastName: 'Player',
    suffix: null,
    city: 'Halifax',
    region: null,
    country: 'Canada',
    displayNameMode: 'Username',
    timeZoneId: 'UTC',
    hasAvatar: false,
    createdUtc: '2026-08-13T00:00:00+00:00',
    updatedUtc: '2026-08-13T00:00:00+00:00',
    profileRevision: 1,
    emailConfirmed: true,
    isAdministrator: false,
    inAppNotificationsEnabled: true,
    emailNotificationsEnabled: true,
    preferredChatLanguage: 'English',
  };
}

function flushPlayUnavailable(http: HttpTestingController): void {
  http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(null, { status: 204, statusText: 'No Content' });
}

function flushLog(http: HttpTestingController, log: unknown[] = campaign.log, revision = campaign.revision): void {
  http.expectOne(`/api/campaigns/${campaign.id}/log`).flush({
    id: campaign.id,
    revision,
    canChat: campaign.canChat,
    mentionableMembers: campaign.mentionableMembers,
    chatChannels: [],
    log,
  });
}

function openSection(fixture: { componentInstance: unknown; detectChanges(): void }, id: string): void {
  (fixture.componentInstance as { setSection: (section: string, open: boolean) => void }).setSection(id, true);
  fixture.detectChanges();
}

function playState(overrides: Partial<CampaignPlayDetail> = {}): CampaignPlayDetail {
  return {
    id: campaign.id,
    name: campaign.name,
    revision: campaign.revision,
    assetTags: {},
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
    commitments: [
      { userId: 'user-1', username: 'northplayer', isCommitted: false },
      { userId: 'user-2', username: 'southplayer', isCommitted: false },
    ],
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
          useValue: {
            snapshot: {
              paramMap: { get: () => campaign.id },
              queryParamMap: { get: () => null },
            },
            paramMap: of(),
          },
        },
      ],
    }).compileComponents();
  });

  it('shows setup metadata and asks before ending', async () => {
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Border War');
    expect(compiled.textContent).toContain('A contested frontier.');
    expect(compiled.textContent).toContain('Halifax, Nova Scotia, Canada');
    expect(compiled.textContent).toContain('North');
    expect(compiled.textContent).toContain('Private campaign');
    const details = compiled.querySelector('dl.facts');
    expect(details?.querySelector('dt')?.textContent).toBeTruthy();
    expect(details?.textContent).toContain('Players');
    expect(details?.textContent).toContain('1 of 8 occupied');
    expect(details?.textContent).toContain('Visibility');
    expect(compiled.textContent).toContain('Scheduled');
    expect(compiled.textContent).toContain('8');
    expect(compiled.textContent).toContain('1 week');
    expect(compiled.textContent).toContain('Action 1 · 3 days');
    expect(compiled.textContent).toContain('Battle phase · 1 day');
    expect(compiled.textContent).toContain('Choose your faction');
    expect(compiled.querySelector('#faction')).toBeTruthy();
    expect(compiled.querySelector('app-back-to-top')?.textContent).toContain('Back to top');
    expect(compiled.textContent).toContain('Campaign chat');
    expect(compiled.textContent).toContain('Links');
    expect(compiled.querySelector('a[href="https://example.test/notes"]')?.textContent).toContain('Notes');
    expect(compiled.textContent).toContain('Participants');
    expect(compiled.textContent).toContain('Add a member');
    expect(compiled.querySelector('a[href^="/users/northplayer"]')?.textContent.trim()).toBe('northplayer');
    expect(compiled.textContent).toContain('Manager, Player');
    expect(compiled.textContent).toContain('Supply 4');
    expect(compiled.textContent).toContain('1 temporary');
    expect(compiled.textContent).toContain("Your force starts at that faction's spawn");
    expect(compiled.querySelector('.standings-table')).toBeNull();
    expect(compiled.textContent).not.toContain('Standings');
    expect(compiled.textContent).toContain('Collapse All');
    expect(compiled.querySelector('a.button')?.textContent).toContain('Edit campaign');
    expect(compiled.textContent).toContain('Edit map');
    expect([...compiled.querySelectorAll('a, button')].some((element) => element.textContent.trim() === 'Play')).toBe(
      false,
    );
    expect([...compiled.querySelectorAll('a, button')].some((element) => element.textContent.trim() === 'View')).toBe(
      false,
    );

    const endButton = compiled.querySelector<HTMLButtonElement>('button.button-danger');
    expect(endButton).toBeTruthy();
    expect(endButton!.textContent).toContain('End campaign');
    endButton!.click();
    fixture.detectChanges();
    expect(compiled.querySelector('[role="alertdialog"]')?.textContent).toContain('End this campaign?');
    expect(compiled.querySelector('[role="alertdialog"]')?.getAttribute('aria-modal')).toBe('true');
    expect(compiled.querySelector('app-campaign-map-preview')).toBeNull();
    expect(compiled.textContent).not.toContain('Download map');
    http.verify();
  });

  it('ends the campaign and returns to Your Campaigns', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('button.button-danger')!.click();
    fixture.detectChanges();
    const confirm = [...compiled.querySelectorAll<HTMLButtonElement>('[role="alertdialog"] button')].find(
      (element) => element.textContent.trim() === 'End campaign',
    );
    expect(confirm).toBeTruthy();
    confirm!.click();
    const end = http.expectOne(`/api/campaigns/${campaign.id}/end`);
    expect(end.request.method).toBe('POST');
    expect(end.request.body).toEqual({ revision: campaign.revision });
    end.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    expect(navigate).toHaveBeenCalledWith('/campaigns');
    fixture.detectChanges();
    expect(document.querySelector('[role="alertdialog"]')).toBeNull();
    fixture.destroy();
    expect(document.querySelector('.app-dialog-backdrop')).toBeNull();
    http.verify();
  });

  it('deletes a completed campaign after confirmation and returns to Your Campaigns', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'Completed',
      canPlay: false,
      canChooseFaction: false,
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'manage');
    openSection(fixture, 'delete');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('End campaign');
    const deleteButton = [...compiled.querySelectorAll<HTMLButtonElement>('button.button-danger')].find(
      (element) => element.textContent.trim() === 'Delete campaign',
    );
    expect(deleteButton).toBeTruthy();
    deleteButton!.click();
    fixture.detectChanges();
    expect(document.querySelector('[role="alertdialog"]')?.textContent).toContain('Delete this campaign?');
    const confirm = [...document.querySelectorAll<HTMLButtonElement>('[role="alertdialog"] button')].find(
      (element) => element.textContent.trim() === 'Delete campaign',
    );
    expect(confirm).toBeTruthy();
    confirm!.click();
    const deleted = http.expectOne(`/api/campaigns/${campaign.id}`);
    expect(deleted.request.method).toBe('DELETE');
    deleted.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    expect(navigate).toHaveBeenCalledWith('/campaigns');
    fixture.destroy();
    expect(document.querySelector('.app-dialog-backdrop')).toBeNull();
    http.verify();
  });

  it('ends an in-progress campaign using the play revision', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState({ hasMap: false, revision: 6 }));
    flushLog(http, campaign.log, 6);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'manage');

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.manage-campaign .button-danger')!.click();
    fixture.detectChanges();
    const confirm = [...compiled.querySelectorAll<HTMLButtonElement>('[role="alertdialog"] button')].find(
      (element) => element.textContent.trim() === 'End campaign',
    );
    expect(confirm).toBeTruthy();
    confirm!.click();
    const end = http.expectOne(`/api/campaigns/${campaign.id}/end`);
    expect(end.request.method).toBe('POST');
    expect(end.request.body).toEqual({ revision: 6 });
    end.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    expect(navigate).toHaveBeenCalledWith('/campaigns');
    fixture.destroy();
  });

  it('lets a manager promote a player or add a manager-only user', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const player = {
      userId: 'user-2',
      username: 'southplayer',
      displayName: 'Ada',
      isPlayer: true,
      isGameMaster: false,
      isAdministrator: false,
      factionName: 'South',
      subfaction: null,
    };
    const withPlayer = {
      ...campaign,
      occupiedPlayerSlots: 2,
      participants: [...campaign.participants, player],
    };
    http.expectOne(`/api/campaigns/${campaign.id}`).flush(withPlayer);
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const promote = [...compiled.querySelectorAll('button')].find(
      (element) => element.textContent.trim() === 'Make campaign manager',
    );
    expect(promote).toBeTruthy();
    promote!.click();
    const promoteRequest = http.expectOne(`/api/campaigns/${campaign.id}/members`);
    expect(promoteRequest.request.body).toEqual({
      userId: 'user-2',
      revision: campaign.revision,
      isGameMaster: true,
      isPlayer: true,
    });
    promoteRequest.flush({
      ...withPlayer,
      participants: [campaign.participants[0], { ...player, isGameMaster: true }],
    });
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...withPlayer,
      participants: [campaign.participants[0], { ...player, isGameMaster: true }],
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

    (fixture.componentInstance as unknown as { memberQuery: { set(value: string): void } }).memberQuery.set('west');
    fixture.detectChanges();
    const searchButton = [...compiled.querySelectorAll('button')].find(
      (element) => element.textContent.trim() === 'Search',
    );
    expect(searchButton).toBeTruthy();
    searchButton!.click();
    const lookup = http.expectOne(
      (item) =>
        item.method === 'GET' &&
        item.url.startsWith(`/api/campaigns/${campaign.id}/members/search`) &&
        item.params.get('q') === 'west',
    );
    lookup.flush([{ userId: 'user-3', username: 'westplayer', displayName: 'West' }]);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Add player');
    expect(compiled.textContent).toContain('Add as manager and player');
    const addManagerOnly = [...compiled.querySelectorAll('button')].find(
      (element) => element.textContent.trim() === 'Add as manager only',
    );
    expect(addManagerOnly).toBeTruthy();
    addManagerOnly!.click();
    const add = http.expectOne(`/api/campaigns/${campaign.id}/members`);
    expect(add.request.body).toEqual({
      userId: 'user-3',
      revision: campaign.revision,
      isGameMaster: true,
      isPlayer: false,
    });
    add.flush(withPlayer);
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}`).flush(withPlayer);
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    await fixture.whenStable();
    http.verify();
  });

  it('lists faction and differing subfaction spawn locations after faction names', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      hasMap: true,
      factions: [
        ...campaign.factions,
        {
          id: 'chaos',
          name: 'Chaos',
          color: '#7C3AED',
          subfactions: ['Nurgle', 'Khorne'],
          allyGroupName: null,
          requiresSubfaction: true,
          hasFlagImage: false,
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [
        { ...squareTerritory('t1', 'Coast', 0.1), spawnFactionId: '1', spawnSubfaction: null },
        { ...squareTerritory('t2', 'Ridge', 0.4), spawnFactionId: '1', spawnSubfaction: 'Riders' },
        { ...squareTerritory('t3', 'Marsh', 0.7), spawnFactionId: null, spawnSubfaction: null },
        { ...squareTerritory('t4', 'Harbor', 0.1), spawnFactionId: 'chaos', spawnSubfaction: 'Khorne' },
        { ...squareTerritory('t5', 'Garden', 0.4), spawnFactionId: 'chaos', spawnSubfaction: 'Nurgle' },
      ],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const factionsPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Factions'),
    );
    expect(factionsPanel).toBeTruthy();
    const items = [...(factionsPanel?.querySelectorAll(':scope > ul.plain-list > li') ?? [])];
    const namedItem = (name: string): Element | undefined =>
      items.find((item) => item.querySelector('.map-focus-button')?.textContent.trim() === name);
    expect(visibleText(namedItem('North')!)).toContain('North (Coast; Riders: Ridge)');
    expect(namedItem('South')?.querySelector('.territory-link')).toBeNull();
    expect(visibleText(namedItem('Chaos')!)).toContain('Chaos (Khorne: Harbor, Nurgle: Garden)');
    http.verify();
  });

  it('lists faction special rules and the players taking each faction or subfaction', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      specialRules: [
        { id: 'rule-1', name: 'Steady Advance', text: 'May move through hills without delay.' },
        { id: 'rule-2', name: 'Rider Ambush', text: 'Riders may start hidden.' },
      ],
      factions: [
        {
          ...campaign.factions[0],
          specialRuleIds: ['rule-1'],
          subfactionSpecialRules: [{ name: 'Riders', specialRuleIds: ['rule-2'] }],
        },
        campaign.factions[1],
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const factionsPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Factions'),
    );
    expect(factionsPanel?.textContent).toContain('Steady Advance');
    expect(factionsPanel?.textContent).toContain('May move through hills without delay.');
    expect(factionsPanel?.textContent).toContain('Rider Ambush');
    expect(factionsPanel?.textContent).toContain('northplayer');
    http.verify();
  });

  it('shows chosen subfaction special rules under the Summary selector on an upcoming campaign', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      specialRules: [
        { id: 'rule-1', name: 'Steady Advance', text: 'May move through hills without delay.' },
        { id: 'rule-2', name: 'Rider Ambush', text: 'Riders may start hidden.' },
      ],
      factions: [
        {
          ...campaign.factions[0],
          specialRuleIds: ['rule-1'],
          subfactionSpecialRules: [{ name: 'Riders', specialRuleIds: ['rule-2'] }],
        },
        campaign.factions[1],
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const select = compiled.querySelector<HTMLSelectElement>('#faction');
    expect(select).toBeTruthy();
    select!.value = '1::Riders';
    select!.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const choose = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Choose your faction'),
    );
    expect(choose?.textContent).toContain('Steady Advance');
    expect(choose?.textContent).toContain('Rider Ambush');
    expect(choose?.textContent).toContain('Riders may start hidden.');
    http.verify();
  });

  it('shows the selected faction special rules below the faction selector', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      specialRules: [{ id: 'rule-1', name: 'Steady Advance', text: 'May move through hills without delay.' }],
      factions: [{ ...campaign.factions[0], specialRuleIds: ['rule-1'] }, campaign.factions[1]],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as { factionChoice: { set(value: string): void } };
    page.factionChoice.set('1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const choose = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Choose your faction'),
    );
    expect(choose?.textContent).toContain('Steady Advance');
    expect(choose?.textContent).toContain('May move through hills without delay.');
    http.verify();
  });

  it('keeps campaign special rules when play returns an empty catalog', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: false,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
      subfaction: 'Riders',
      specialRules: [
        { id: 'rule-1', name: 'Steady Advance', text: 'May move through hills without delay.' },
        { id: 'rule-2', name: 'Rider Ambush', text: 'Riders may start hidden.' },
      ],
      factions: [
        {
          ...campaign.factions[0],
          specialRuleIds: ['rule-1'],
          subfactionSpecialRules: [{ name: 'Riders', specialRuleIds: ['rule-2'] }],
        },
        campaign.factions[1],
      ],
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
        hasMap: false,
        specialRules: [],
        factions: campaign.factions,
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const factionsPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Factions'),
    );
    const page = fixture.componentInstance as unknown as { setSection: (id: string, open: boolean) => void };
    page.setSection('factions', true);
    fixture.detectChanges();
    expect(factionsPanel?.textContent).toContain('Steady Advance');
    expect(factionsPanel?.textContent).toContain('Rider Ambush');

    const summary = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Summary'),
    );
    expect(summary?.textContent).toContain('Steady Advance');
    expect(summary?.textContent).toContain('Rider Ambush');
    http.verify();
  });

  it('lists faction, force location, chain supply, and spendable supply in the Summary panel', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      factionId: '1',
      canChooseFaction: false,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
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
          terrainTypeId: null,
          structureTypeId: null,
          structureCondition: 'Operational',
          overlayColor: null,
          ownerFactionId: '1',
        },
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        viewerSupply: {
          currentSupplyPoints: 4,
          temporarySupplyPoints: 1,
          mapSupplyPoints: 3,
          roundFreeSupplyPoints: 1,
          splitPenaltyPoints: 0,
          forceAllowancePoints: 3,
          contributions: [
            { kind: 'TerritoryTerrain', label: 'Coast terrain (Plains)', points: 1, isAllied: false },
            { kind: 'Temporary', label: 'Temporary supply', points: 1, isAllied: false },
          ],
        },
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
            supply: {
              currentSupplyPoints: 3,
              temporarySupplyPoints: 0,
              mapSupplyPoints: 3,
              roundFreeSupplyPoints: 1,
              splitPenaltyPoints: 0,
              forceAllowancePoints: 3,
              contributions: [
                { kind: 'TerritoryTerrain', label: 'Coast terrain (Plains)', points: 1, isAllied: false },
              ],
            },
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const summaryPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Summary'),
    );
    openSection(fixture, 'factions');
    const factionsPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Factions'),
    );
    const text = summaryPanel?.textContent ?? '';
    expect(text.indexOf('North')).toBeGreaterThanOrEqual(0);
    expect(text.indexOf('North')).toBeLessThan(text.indexOf('Coast'));
    expect(text.indexOf('Coast')).toBeLessThan(text.indexOf('Supply'));
    expect(text.indexOf('Supply')).toBeLessThan(text.indexOf('Spendable supply'));
    const forceDetails = summaryPanel?.querySelector('.summary-force .supply-points') as HTMLDetailsElement | undefined;
    const forceSummary = forceDetails?.querySelector('summary') as HTMLElement | undefined;
    expect(forceSummary?.textContent).toContain('Supply');
    expect(forceSummary?.textContent).toContain('3');
    expect(forceSummary?.textContent).not.toContain('temporary');
    expect(forceSummary?.getAttribute('title')).toContain('Coast terrain (Plains): +1');
    expect(forceDetails?.open).toBe(false);
    expect(factionsPanel?.querySelector('.supply-points')).toBeNull();
    forceSummary?.click();
    fixture.detectChanges();
    expect(forceDetails?.open).toBe(true);
    expect(visibleText(forceDetails!.querySelector('.supply-breakdown')!)).toContain('Coast terrain (Plains): +1');
    expect(visibleText(forceDetails!.querySelector('.supply-breakdown')!)).not.toContain('Temporary supply');
    const spendable = [...(summaryPanel?.querySelectorAll('.supply-points') ?? [])].find((item) =>
      item.querySelector('summary')?.textContent.includes('Spendable supply'),
    ) as HTMLDetailsElement | undefined;
    expect(spendable?.querySelector('summary')?.textContent).toContain('1');
    expect(spendable?.querySelector('summary')?.textContent).not.toContain('temporary');
    spendable?.querySelector('summary')?.click();
    fixture.detectChanges();
    expect(visibleText(spendable!.querySelector('.supply-breakdown')!)).toContain('Temporary supply: +1');
    http.verify();
  });

  it('lists a held item and its powers in Summary and places Item objectives under that panel', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      factionId: '1',
      canChooseFaction: false,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      itemObjectiveTypes: [
        {
          id: 'crown',
          name: 'Crown',
          isHiddenUntilFound: false,
          placement: 'Random',
          allowOnSpawn: false,
          effects: [{ id: 'e1', kind: 'TeleportToChosenNonSpawnOncePerRound', amount: 0 }],
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
          terrainTypeId: null,
          structureTypeId: null,
          structureCondition: 'Operational',
          overlayColor: null,
          ownerFactionId: '1',
        },
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
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
            availableActions: ['Hold', 'Move', 'Teleport'],
            canChooseTeleportDestination: true,
          },
        ],
        itemObjectives: [
          {
            id: 'item-1',
            typeId: 'crown',
            name: 'Crown',
            territoryId: null,
            possessorForceId: 'force-1',
            isRevealed: true,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'faction');
    openSection(fixture, 'itemObjectives');
    openSection(fixture, 'orders');

    const compiled = fixture.nativeElement as HTMLElement;
    const headings = [...compiled.querySelectorAll('.panel h2')].map((heading) => heading.textContent.trim());
    expect(headings.indexOf('Summary')).toBeGreaterThanOrEqual(0);
    expect(headings.indexOf('Item objectives')).toBeGreaterThan(headings.indexOf('Summary'));
    expect(headings.indexOf('Item objectives')).toBeLessThan(headings.indexOf('Links'));
    const summaryPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Summary'),
    );
    expect(summaryPanel?.textContent).toContain('Holding Crown');
    expect(summaryPanel?.textContent).toContain('Teleport to Specific Territory');
    const itemPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Item objectives'),
    );
    expect(itemPanel?.textContent).toContain('Crown');
    expect(itemPanel?.textContent).toContain('Carried by');
    expect(compiled.textContent).toContain('Teleport');
    http.verify();
  });

  it('shows campaign chat before campaign data finishes loading', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Loading campaign chat...');
    expect(compiled.textContent).toContain('Loading campaign…');

    flushLog(http, [
      {
        id: 'log-1',
        occurredUtc: '2026-08-15T20:45:23-04:00',
        kind: 'PlayerChat',
        originator: 'northplayer',
        summary: 'Chat is ready first.',
        territoryId: null,
        forceId: null,
        battleId: null,
        isSystemAdjustment: false,
      },
    ]);
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Campaign chat');
    expect(compiled.textContent).toContain('Chat is ready first.');
    expect(compiled.textContent).not.toContain('Loading campaign chat...');
    expect(compiled.textContent).toContain('Loading campaign…');
    expect(compiled.querySelector('h1')?.textContent).toContain('Campaign');

    http.expectOne(`/api/campaigns/${campaign.id}`).flush(campaign);
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(compiled.querySelector('h1')?.textContent).toContain('Border War');
    expect(compiled.textContent).not.toContain('Loading campaign…');
    expect(compiled.textContent).toContain('Chat is ready first.');
    http.verify();
  });

  it('asks a manager to confirm before removing a player', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      participants: [
        ...campaign.participants,
        {
          userId: 'user-2',
          username: 'southplayer',
          displayName: 'Ada',
          isPlayer: true,
          isGameMaster: false,
          isAdministrator: false,
          factionName: 'South',
          subfaction: null,
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Add a member');
    const remove = [...compiled.querySelectorAll('button')].find(
      (element) => element.textContent.trim() === 'Remove player',
    );
    expect(remove).toBeTruthy();
    expect(compiled.textContent).toContain('Make campaign manager');
    remove!.click();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Confirm remove');
    http.verify();
    [...compiled.querySelectorAll('button')]
      .find((element) => element.textContent.trim() === 'Confirm remove')
      ?.click();
    const kick = http.expectOne(`/api/campaigns/${campaign.id}/members/kick`);
    expect(kick.request.body).toEqual({ userId: 'user-2', revision: campaign.revision });
    kick.flush({
      ...campaign,
      participants: campaign.participants,
    });
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      participants: campaign.participants,
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
    flushLog(http, [
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
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('northplayer:');
    expect(compiled.textContent).toContain('Hey, everybody! This is a message to all of you.');
    expect(compiled.textContent).toContain('Download log');
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
    flushLog(
      http,
      [
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
      3,
    );
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
    flushLog(http);
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Campaign chat');
    expect(compiled.textContent).toContain('The campaign started.');
    expect(compiled.textContent).toContain('Campaign:');
    expect(compiled.textContent).toContain('Phase ends in');
    expect(compiled.textContent).toContain('Round 1 · Action 1');
    expect(compiled.textContent).toContain('Commit Actions');
    expect(compiled.textContent).toContain('Debug');
    expect(compiled.textContent).toContain('Go to your orders');
    expect(compiled.textContent).toContain('Not committed');
    expect(compiled.querySelector('.campaign-status-bar')).toBeTruthy();
    expect(compiled.querySelectorAll('app-update-stream-status').length).toBe(1);
    expect(compiled.querySelector('app-campaign-log app-update-stream-status')).toBeNull();
    openSection(fixture, 'faction');
    expect(visibleText(compiled)).toContain('Coast');
    expect(compiled.textContent).not.toContain('Spawn location is at Coast');
    expect(compiled.textContent).not.toContain('Choose your faction');
    expect([...compiled.querySelectorAll('a, button')].some((element) => element.textContent.trim() === 'Play')).toBe(
      false,
    );
    openSection(fixture, 'map');
    const map = fixture.debugElement.query(By.css('app-campaign-map-view'));
    expect((map.componentInstance as CampaignMapViewComponent).initialCamera()).toBe('first-force');
    const page = fixture.componentInstance as unknown as { hoveredTerritoryId: { set(id: string): void } };
    page.hoveredTerritoryId.set('t1');
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Town (pillaged)');
    expect(compiled.textContent).toContain('Forces: northplayer · North');
    openSection(fixture, 'manage');
    expect(compiled.textContent).not.toContain('Edit map');
    expect(compiled.textContent).not.toContain('Edit campaign');
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Border War');
    expect(compiled.querySelector('app-campaign-map-view')).toBeTruthy();
    expect(compiled.querySelector('app-campaign-map-preview')).toBeNull();
    expect(compiled.textContent).toContain('Download map');
    const map = fixture.debugElement.query(By.css('app-campaign-map-view'));
    expect((map.componentInstance as CampaignMapViewComponent).initialCamera()).toBe('fit');
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
    flushLog(http);
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

    expect(compiled.textContent).toContain('Summary');
    expect(compiled.textContent).toContain('You can change your faction until the campaign starts');
    expect(compiled.textContent).toContain('North');
    expect(compiled.querySelector('#faction')).toBeTruthy();
    expect([...compiled.querySelectorAll('#faction option')].map((option) => option.textContent.trim())).toContain(
      'North - Riders',
    );

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
        request.flush(null, { status: 204, statusText: 'No Content' });
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

  it('lists subfactions as Faction - Subfaction when staff assign a player faction', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      participants: [
        ...campaign.participants,
        {
          userId: 'user-2',
          username: 'southplayer',
          displayName: 'Ada',
          isPlayer: true,
          isGameMaster: false,
          isAdministrator: false,
          factionName: null,
          subfaction: null,
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const options = [...compiled.querySelectorAll('#staff-faction-user-2 option')].map((option) =>
      option.textContent.trim(),
    );
    expect(options).toContain('North');
    expect(options).toContain('North - Riders');
    expect(options).toContain('South');
    expect(compiled.querySelector('#staff-subfaction-user-2')).toBeNull();
    http.verify();
  });

  it('saves a staff faction selection without a second Assign click', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const player = {
      userId: 'user-2',
      username: 'southplayer',
      displayName: 'Ada',
      isPlayer: true,
      isGameMaster: false,
      isAdministrator: false,
      factionName: null as string | null,
      subfaction: null as string | null,
    };
    const withPlayer = {
      ...campaign,
      occupiedPlayerSlots: 2,
      participants: [...campaign.participants, player],
    };
    http.expectOne(`/api/campaigns/${campaign.id}`).flush(withPlayer);
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onStaffFaction: (participant: typeof player, value: string) => Promise<void>;
    };
    const pending = page.onStaffFaction(player, '2');
    const assign = http.expectOne(`/api/campaigns/${campaign.id}/play/faction/assign`);
    expect(assign.request.body).toEqual({
      revision: campaign.revision,
      userId: 'user-2',
      factionId: '2',
      subfaction: null,
    });
    assign.flush(playState({ hasMap: false, revision: 2 }));
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...withPlayer,
      revision: 2,
      participants: [...campaign.participants, { ...player, factionId: '2', factionName: 'South' }],
    });
    await pending;
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Ada');
    expect(compiled.textContent).toContain('South');
    expect(compiled.textContent).toContain('Successfully saved changes.');
    http.verify();
  });

  it('lets staff assign a faction to a player-manager', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      occupiedPlayerSlots: 2,
      participants: [
        ...campaign.participants,
        {
          userId: 'user-2',
          username: 'southplayer',
          displayName: 'Ada',
          isPlayer: true,
          isGameMaster: true,
          isAdministrator: false,
          factionName: null,
          subfaction: null,
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#staff-faction-user-2')).toBeTruthy();
    expect(
      [...compiled.querySelectorAll('button')].some((button) => button.textContent.trim() === 'Make campaign manager'),
    ).toBe(false);
    http.verify();
  });

  it('keeps an unsaved action and map pick while the board poll refreshes', async () => {
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onDraftKind: (forceId: string, kind: string) => void;
      onTerritorySelect: (event: { id: string; additive: boolean }) => void;
      draftFor: (forceId: string) => { kind: string };
      mapAction: () => { step: string } | null;
      refreshBoard: () => Promise<void>;
    };
    page.onDraftKind('force-1', 'Move');
    page.onTerritorySelect({ id: 't1', additive: false });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(page.draftFor('force-1').kind).toBe('Move');
    expect(page.mapAction()?.step).toBe('menu');
    expect(
      [...compiled.querySelectorAll<HTMLOptionElement>('#kind-force-1 option')].map((option) => option.value),
    ).toEqual(['Hold', 'Move']);

    const pending = page.refreshBoard();
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState({ hasMap: false }));
    await pending;
    await fixture.whenStable();
    fixture.detectChanges();
    expect(page.draftFor('force-1').kind).toBe('Move');
    expect(page.mapAction()?.step).toBe('menu');
    expect(compiled.querySelector<HTMLSelectElement>('#kind-force-1')?.value).toBe('Move');
    http.verify();
  });

  it('keeps an unsaved action after a later board revision arrives', async () => {
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onDraftKind: (forceId: string, kind: string) => void;
      draftFor: (forceId: string) => { kind: string };
      refreshBoard: () => Promise<void>;
    };
    page.onDraftKind('force-1', 'Move');
    const pending = page.refreshBoard();
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState({ hasMap: false, revision: 2 }));
    await pending;
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: 2,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(page.draftFor('force-1').kind).toBe('Move');
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Commit Actions');
    expect(compiled.textContent).not.toContain('Commit Actions and close the phase');
    const commitBefore = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commit Actions',
    );
    expect(commitBefore?.hasAttribute('disabled')).toBe(true);
    const saveDraft = compiled.querySelector('button[aria-label^="Save draft"]');
    expect(saveDraft).toBeTruthy();
    expect(saveDraft?.textContent.trim()).toBe('Save draft');
    expect(compiled.querySelector('.force-card')).toBeTruthy();
    expect(compiled.querySelector('.commitment-roster')).toBeNull();
    expect(compiled.textContent).toContain('0 of 2 players committed');
    const commitmentsToggle = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commitments',
    );
    expect(commitmentsToggle?.getAttribute('aria-expanded')).toBe('false');
    commitmentsToggle?.click();
    fixture.detectChanges();
    expect(compiled.querySelector('.commitment-roster .status-chip')?.textContent).toContain('Drafting');
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

  it('lists commitments alphabetically in a shared grid with faction and force locations', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
      participants: [
        ...campaign.participants,
        {
          userId: 'user-2',
          username: 'southplayer',
          displayName: 'Ada',
          isPlayer: true,
          isGameMaster: false,
          isAdministrator: false,
          factionId: '2',
          factionName: 'South',
          subfaction: 'Corsairs',
          currentSupplyPoints: 1,
          temporarySupplyPoints: 0,
          contributions: [],
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [
        squareTerritory('t1', 'Coast', 0.1),
        squareTerritory('guanier', 'Guanier', 0.4),
        squareTerritory('bidouze', 'Bidouze River', 0.7),
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        commitments: [
          { userId: 'user-1', username: 'northplayer', isCommitted: false },
          { userId: 'user-2', username: 'southplayer', isCommitted: true },
          { userId: 'user-3', username: 'ada', isCommitted: false },
        ],
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
            subfaction: 'Riders',
          },
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 'guanier',
            isMine: false,
            inBattle: false,
            moveTargets: [],
            availableActions: ['Hold'],
            subfaction: 'Corsairs',
          },
          {
            id: 'force-3',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 'bidouze',
            isMine: false,
            inBattle: false,
            moveTargets: [],
            availableActions: ['Hold'],
            subfaction: 'Corsairs',
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const commitmentsToggle = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commitments',
    );
    commitmentsToggle?.click();
    fixture.detectChanges();

    const roster = compiled.querySelector('.commitment-roster');
    expect(roster instanceof HTMLElement).toBe(true);
    if (!(roster instanceof HTMLElement)) {
      return;
    }

    expect(roster.querySelector('.commitment-column')).toBeNull();
    expect([...roster.querySelectorAll('a.profile-link')].map((link) => link.textContent.trim())).toEqual([
      'ada',
      'northplayer',
      'southplayer',
    ]);
    expect(visibleText(roster)).toContain('northplayer Drafting');
    expect(visibleText(roster)).not.toContain('((Drafting))');
    expect(visibleText(roster)).toContain('North - Riders');
    expect(visibleText(roster)).toContain('Coast');
    expect(roster.querySelector('a.profile-link')?.getAttribute('href')).toContain('/users/ada');
    expect(visibleText(roster)).toContain('southplayer Committed');
    expect(visibleText(roster)).not.toContain('((Committed))');
    expect(visibleText(roster)).toContain('South - Corsairs');
    expect(visibleText(roster)).toContain('Guanier and Bidouze River');
    expect(
      [...roster.querySelectorAll('button.territory-link')]
        .filter((button) => button.textContent.includes('Guanier') || button.textContent.includes('Bidouze'))
        .map((button) => button.textContent.trim()),
    ).toEqual(['Guanier', 'Bidouze River']);
    http.verify();
  });

  it('bolds faction power names in the Actions panel', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
      specialRules: [{ id: 'rule-1', name: 'Steady Advance', text: 'May move through hills without delay.' }],
      factions: [{ ...campaign.factions[0], specialRuleIds: ['rule-1'] }, campaign.factions[1]],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState({ hasMap: false }));
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const actions = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Actions'),
    );
    const power = [...(actions?.querySelectorAll('strong') ?? [])].find(
      (item) => item.textContent.trim() === 'Steady Advance',
    );
    expect(power).toBeTruthy();
    expect(actions?.textContent).toContain('May move through hills without delay.');
    http.verify();
  });

  it('jumps faction and ally-group names to their listings', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      factions: [
        { ...campaign.factions[0], allyGroupId: 'g-north', allyGroupName: 'Northern League' },
        campaign.factions[1],
      ],
      allyGroups: [{ id: 'g-north', name: 'Northern League', color: '#0F172A' }],
      participants: campaign.participants.map((participant) => ({
        ...participant,
        factionId: '1',
      })),
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      isOpen: (id: string) => boolean;
      setSection: (id: string, open: boolean) => void;
    };
    page.setSection('factions', false);
    page.setSection('allies', false);
    fixture.detectChanges();
    expect(compiled.querySelector('#campaign-faction-1')).toBeNull();

    const factionLink = [...compiled.querySelectorAll('a.territory-link')].find(
      (link) => link.getAttribute('href') === '#campaign-faction-1',
    );
    expect(factionLink?.textContent.trim()).toBe('North');
    (factionLink as HTMLAnchorElement | undefined)?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.isOpen('factions')).toBe(true);
    const factionTarget = compiled.querySelector('#campaign-faction-1');
    expect(factionTarget?.tagName).toBe('BUTTON');
    expect(factionTarget?.textContent.trim()).toBe('North');
    expect(factionTarget?.closest('li')?.id).toBe('');

    const allyLink = [...compiled.querySelectorAll('a.territory-link')].find(
      (link) => link.getAttribute('href') === '#campaign-ally-g-north',
    );
    expect(allyLink?.textContent.trim()).toBe('Northern League');
    (allyLink as HTMLAnchorElement | undefined)?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.isOpen('allies')).toBe(true);
    const allyTarget = compiled.querySelector('#campaign-ally-g-north');
    expect(allyTarget?.tagName).toBe('BUTTON');
    expect(allyTarget?.textContent.trim()).toBe('Northern League');
    expect(allyTarget?.closest('li')?.id).toBe('');
    http.verify();
  });

  it('refreshes the board when a closing commit races another play update', async () => {
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        hasMap: false,
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const commit = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commit Actions',
    );
    commit?.click();
    http.expectOne(`/api/campaigns/${campaign.id}/play/commit`).flush(
      {
        code: 'concurrency.conflict',
        message: 'The campaign was changed by another request. Reload and try again.',
      },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        hasMap: false,
        revision: 4,
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        isCommitted: false,
      }),
    );
    await fixture.whenStable();
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
    expect(compiled.textContent).not.toContain('The campaign was changed by another request. Reload and try again.');
    expect(compiled.textContent).toContain('Battle 1');
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

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
    const menu = compiled.querySelector('.action-context-menu')?.textContent ?? '';
    expect(menu).toContain('Move');
    expect(menu).not.toContain('Backstab');
    expect(menu).not.toContain('Build');
    expect(menu).not.toContain('Pillage');
    expect(menu).not.toContain('Repair');
    expect(menu).not.toContain('Split');

    page.onMapBackgroundSelect();
    fixture.detectChanges();
    expect(page.mapAction()).toBeNull();

    page.onTerritorySelect({ id: 't1', additive: false, clientX: 40, clientY: 12 });
    page.onMapActionKind('Move');
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('pick-target');
    expect(compiled.textContent).toContain('Pick a territory to move to...');

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
    expect(visibleText(compiled)).toContain('Draft: Move to Ridge');
    http.verify();
  });

  it('prompts on the map when splitting a force', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'Ridge', 0.4)];
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        forces: [
          {
            ...playState().forces[0],
            availableActions: ['Hold', 'Move', 'Split'],
            moveTargets: ['t2'],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean; clientX: number; clientY: number }) => void;
      onMapActionKind: (kind: string) => void;
    };
    page.onTerritorySelect({ id: 't1', additive: false, clientX: 40, clientY: 12 });
    page.onMapActionKind('Split');
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Pick a territory to split forces to...');
    expect(compiled.querySelector('.map-action-prompt')?.textContent).toContain(
      'Pick a territory to split forces to...',
    );
    http.verify();
  });

  it('marks your forces with a saved draft or committed action on the map', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'Ridge', 0.4)];
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        myDrafts: [{ forceId: 'force-1', kind: 'Move', targetTerritoryId: 't2', structureTypeId: null }],
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
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't2',
            isMine: false,
            inBattle: false,
            moveTargets: [],
            availableActions: [],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const page = fixture.componentInstance as unknown as {
      mapForces: () => {
        id: string;
        isMine: boolean;
        action?: { kind: string; status: string; detail?: string | null } | null;
        moveTargets?: readonly string[];
        routeSteps?: readonly string[];
        routeLabel?: string;
      }[];
    };
    expect(page.mapForces()).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          id: 'force-1',
          isMine: true,
          action: { kind: 'Move', status: 'draft', detail: 'to Ridge' },
          moveTargets: ['t2'],
          routeSteps: ['t1', 't2'],
          routeLabel: 'Move from Coast to Ridge',
        }),
        expect.objectContaining({ id: 'force-2', isMine: false, action: null }),
      ]),
    );
    expect(page.mapForces().find((force) => force.id === 'force-2')?.routeSteps).toBeUndefined();

    const compiled = fixture.nativeElement as HTMLElement;
    const mine = compiled.querySelector('.force-pin.is-mine');
    expect(mine?.querySelector('.force-action-mark')).toBeTruthy();
    expect(mine?.getAttribute('title')).toContain('Move to Ridge (draft)');
    expect(compiled.querySelector('.force-pin:not(.is-mine) .force-action-mark')).toBeNull();
    http.verify();
  });

  it('shows your retreat hop on the map after you pick a destination', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'Ridge', 0.4)];
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
      currentRound: 1,
      currentPhaseNumber: 1,
      currentPhaseKind: 'Battle',
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't1',
            isMine: false,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'Finalized',
            participantForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: 'force-2',
            isDraw: false,
            needsRetreat: true,
            retreatTargets: ['t2'],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const page = fixture.componentInstance as unknown as {
      onRetreatTarget: (battleId: string, targetTerritoryId: string) => void;
      mapForces: () => { id: string; routeSteps?: readonly string[]; routeLabel?: string }[];
    };
    page.onRetreatTarget('battle-1', 't2');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(page.mapForces().find((force) => force.id === 'force-1')).toEqual(
      expect.objectContaining({
        routeSteps: ['t1', 't2'],
        routeLabel: 'Retreat from Coast to Ridge',
      }),
    );
    expect(page.mapForces().find((force) => force.id === 'force-2')?.routeSteps).toBeUndefined();
    expect(compiled.querySelectorAll('.order-route')).toHaveLength(1);
    expect(compiled.querySelectorAll('.order-route-head')).toHaveLength(1);
    http.verify();
  });

  it('shows another force order route only while debug is on', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'Ridge', 0.4)];
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canManage: true,
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        isDebugActive: true,
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
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't2',
            isMine: false,
            inBattle: false,
            moveTargets: ['t1'],
            availableActions: ['Hold', 'Move'],
          },
        ],
        debugDrafts: [{ forceId: 'force-2', kind: 'Move', targetTerritoryId: 't1', structureTypeId: null }],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const page = fixture.componentInstance as unknown as {
      mapForces: () => { id: string; routeSteps?: readonly string[] }[];
    };
    expect(page.mapForces().find((force) => force.id === 'force-2')?.routeSteps).toEqual(['t2', 't1']);
    expect(page.mapForces().find((force) => force.id === 'force-1')?.routeSteps).toBeUndefined();
    http.verify();
  });

  it('cycles your forces on the map without opening the action menu', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [
      squareTerritory('t1', 'Coast', 0.1),
      squareTerritory('t2', 'Ridge', 0.4),
      squareTerritory('t3', 'Pass', 0.7),
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
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
          {
            id: 'force-2',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't3',
            isMine: true,
            inBattle: false,
            moveTargets: [],
            availableActions: ['Hold'],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      selectedIds: () => string[];
      mapAction: () => { step: string } | null;
    };
    const cycle = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Cycle forces',
    );
    expect(cycle).toBeTruthy();
    expect(cycle!.getAttribute('title')).toBe('Cycle through your forces (Y)');
    expect(cycle!.getAttribute('aria-keyshortcuts')).toBe('Y');
    const toolbarItems = [...compiled.querySelector('.map-toolbar')!.children].map((item) =>
      item.textContent.replace(/\s+/g, ' ').trim(),
    );
    const cycleIndex = toolbarItems.indexOf('Cycle forces');
    expect(toolbarItems[cycleIndex + 1]).toBe('Commit Actions');
    expect(toolbarItems[cycleIndex + 2]).toBe('Show names');
    const mapCommit = [...compiled.querySelectorAll<HTMLButtonElement>('.map-toolbar button')].find(
      (button) => button.textContent.trim() === 'Commit Actions',
    );
    expect(mapCommit?.hasAttribute('disabled')).toBe(true);
    expect(mapCommit?.getAttribute('title')).toBe('Commit Actions (C)');
    cycle!.click();
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t1']);
    expect(page.mapAction()).toBeNull();
    expect(compiled.querySelector('.action-context-menu')).toBeNull();
    expect(
      [...compiled.querySelectorAll('.territory.is-selected')].map((item) => item.getAttribute('data-id')),
    ).toEqual(['t1']);

    cycle!.click();
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t3']);
    expect(page.mapAction()).toBeNull();
    http.verify();
  });

  it('commits from the map toolbar Commit control', async () => {
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
      territories: [squareTerritory('t1', 'Coast', 0.1)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const mapCommit = [...compiled.querySelectorAll<HTMLButtonElement>('.map-toolbar button')].find(
      (button) => button.textContent.trim() === 'Commit Actions',
    );
    expect(mapCommit).toBeTruthy();
    expect(mapCommit!.hasAttribute('disabled')).toBe(false);
    expect(mapCommit!.getAttribute('title')).toBe('Commit Actions (C)');
    expect(mapCommit!.getAttribute('aria-keyshortcuts')).toBe('C');
    mapCommit!.click();
    http.expectOne(`/api/campaigns/${campaign.id}/play/commit`).flush(playState({ revision: 3, isCommitted: true }));
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: 3,
      canManage: true,
      territories: [squareTerritory('t1', 'Coast', 0.1)],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    const mapUncommit = [...compiled.querySelectorAll<HTMLButtonElement>('.map-toolbar button')].find(
      (button) => button.textContent.trim() === 'Uncommit',
    );
    expect(mapUncommit).toBeTruthy();
    expect(mapUncommit!.getAttribute('title')).toBe('Uncommit (C)');
    expect(mapUncommit!.getAttribute('aria-keyshortcuts')).toBe('C');
    expect(
      [...compiled.querySelectorAll<HTMLButtonElement>('.map-toolbar button')].some((button) =>
        button.textContent.trim().startsWith('Commit Actions'),
      ),
    ).toBe(false);
    mapUncommit!.click();
    http.expectOne(`/api/campaigns/${campaign.id}/play/uncommit`).flush(
      playState({
        revision: 4,
        isCommitted: false,
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
      }),
    );
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: 4,
      canManage: true,
      territories: [squareTerritory('t1', 'Coast', 0.1)],
      adjacencies: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(
      [...compiled.querySelectorAll<HTMLButtonElement>('.map-toolbar button')].some(
        (button) => button.textContent.trim() === 'Uncommit',
      ),
    ).toBe(false);
    const mapCommitAgain = [...compiled.querySelectorAll<HTMLButtonElement>('.map-toolbar button')].find(
      (button) => button.textContent.trim() === 'Commit Actions',
    );
    expect(mapCommitAgain).toBeTruthy();
    http.verify();
  });

  it('treats a committed order as committed on your force pin', async () => {
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
      territories: [squareTerritory('t1', 'Coast', 0.1)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        isCommitted: true,
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
        orders: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, isRevealed: false }],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const page = fixture.componentInstance as unknown as {
      mapForces: () => { action?: { kind: string; status: string } | null }[];
    };
    expect(page.mapForces()[0]?.action).toEqual({ kind: 'Hold', status: 'committed', detail: null });
    expect((fixture.nativeElement as HTMLElement).querySelector('.force-pin.is-mine')?.getAttribute('title')).toContain(
      'Hold (committed)',
    );
    http.verify();
  });

  it('plans a two-territory Move through a unique via and prompts when several vias exist', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [
      squareTerritory('t1', 'Coast', 0.1),
      squareTerritory('t2', 'Vale', 0.4),
      squareTerritory('t3', 'Pass', 0.7),
      squareTerritory('t4', 'Ridge', 1.0),
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
    const base = playState();
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        forces: [
          {
            ...base.forces[0],
            moveTargets: ['t2', 't3', 't4'],
            moveHops: [
              { viaTerritoryId: 't2', targetTerritoryId: 't4' },
              { viaTerritoryId: 't3', targetTerritoryId: 't4' },
            ],
            canMoveTwoTerritories: true,
            movementSpeed: 2,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean; clientX: number; clientY: number }) => void;
      onMapActionKind: (kind: string) => void;
      confirmMapAction: () => Promise<void>;
      mapAction: () => { step: string; kind: string; targetTerritoryId: string; viaTerritoryId: string } | null;
      mapForces: () => { id: string; routeSteps?: readonly string[] }[];
    };

    page.onTerritorySelect({ id: 't1', additive: false, clientX: 40, clientY: 12 });
    page.onMapActionKind('Move');
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('pick-target');
    expect(compiled.textContent).toContain('Pick a territory to move to...');

    page.onTerritorySelect({ id: 't4', additive: false, clientX: 90, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('pick-via');
    expect(compiled.textContent).toContain('Select the territory to move through.');

    page.onTerritorySelect({ id: 't2', additive: false, clientX: 70, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('confirm');
    expect(compiled.textContent).toContain('Move from Coast through Vale to Ridge?');
    expect(page.mapForces().find((force) => force.id === 'force-1')?.routeSteps).toEqual(['t1', 't2', 't4']);
    expect(compiled.querySelectorAll('.order-route')).toHaveLength(2);
    expect(compiled.querySelectorAll('.order-route-head')).toHaveLength(2);

    const pending = page.confirmMapAction();
    const draft = http.expectOne(`/api/campaigns/${campaign.id}/play/draft`);
    expect(draft.request.body as { kind: string; targetTerritoryId: string; viaTerritoryId: string }).toEqual(
      expect.objectContaining({
        kind: 'Move',
        targetTerritoryId: 't4',
        viaTerritoryId: 't2',
      }),
    );
    draft.flush(
      playState({
        revision: 3,
        myDrafts: [
          { forceId: 'force-1', kind: 'Move', targetTerritoryId: 't4', viaTerritoryId: 't2', structureTypeId: null },
        ],
        forces: [
          {
            ...base.forces[0],
            moveTargets: ['t2', 't3', 't4'],
            moveHops: [
              { viaTerritoryId: 't2', targetTerritoryId: 't4' },
              { viaTerritoryId: 't3', targetTerritoryId: 't4' },
            ],
            canMoveTwoTerritories: true,
            movementSpeed: 2,
          },
        ],
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
    expect(visibleText(compiled)).toContain('Draft: Move to Ridge');
    http.verify();
  });

  it('asks for the unique via when a Move must pass through another territory', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [
      squareTerritory('t1', 'Coast', 0.1),
      squareTerritory('t2', 'Vale', 0.4),
      squareTerritory('t3', 'Pass', 0.7),
      squareTerritory('t4', 'Ridge', 1.0),
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
    const base = playState();
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        forces: [
          {
            ...base.forces[0],
            moveTargets: ['t2', 't4'],
            moveHops: [{ viaTerritoryId: 't2', targetTerritoryId: 't4' }],
            canMoveTwoTerritories: true,
            movementSpeed: 2,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean; clientX: number; clientY: number }) => void;
      onMapActionKind: (kind: string) => void;
      mapAction: () => { step: string; viaTerritoryId: string; viaPath: string[] } | null;
      mapForces: () => { id: string; routeSteps?: readonly string[] }[];
    };

    page.onTerritorySelect({ id: 't1', additive: false, clientX: 40, clientY: 12 });
    page.onMapActionKind('Move');
    page.onTerritorySelect({ id: 't4', additive: false, clientX: 90, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('pick-via');
    expect(compiled.textContent).toContain('Select the territory to move through.');

    page.onTerritorySelect({ id: 't2', additive: false, clientX: 70, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()).toEqual(expect.objectContaining({ step: 'confirm', viaTerritoryId: 't2', viaPath: [] }));
    expect(compiled.textContent).toContain('Move from Coast through Vale to Ridge?');
    expect(page.mapForces().find((force) => force.id === 'force-1')?.routeSteps).toEqual(['t1', 't2', 't4']);
    expect(compiled.querySelectorAll('.order-route')).toHaveLength(2);
    http.verify();
  });

  it('picks each hop of a three-territory Move instead of jumping to the destination', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [
      squareTerritory('t1', 'Coast', 0.1),
      squareTerritory('t2', 'Vale', 0.4),
      squareTerritory('t3', 'Pass', 0.7),
      squareTerritory('t4', 'Ridge', 1.0),
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
    const base = playState();
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        forces: [
          {
            ...base.forces[0],
            moveTargets: ['t2', 't3', 't4'],
            moveHops: [
              {
                viaTerritoryId: 't2',
                targetTerritoryId: 't4',
                intermediateTerritoryIds: ['t3'],
              },
            ],
            canMoveTwoTerritories: true,
            movementSpeed: 3,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean; clientX: number; clientY: number }) => void;
      onMapActionKind: (kind: string) => void;
      mapAction: () => { step: string; viaTerritoryId: string; viaPath: string[] } | null;
      mapForces: () => { id: string; routeSteps?: readonly string[] }[];
    };

    page.onTerritorySelect({ id: 't1', additive: false, clientX: 40, clientY: 12 });
    page.onMapActionKind('Move');
    page.onTerritorySelect({ id: 't4', additive: false, clientX: 90, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('pick-via');

    page.onTerritorySelect({ id: 't2', additive: false, clientX: 70, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('pick-via');
    expect(compiled.textContent).toContain('Select the territory to move through.');

    page.onTerritorySelect({ id: 't3', additive: false, clientX: 80, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()).toEqual(
      expect.objectContaining({ step: 'confirm', viaTerritoryId: 't2', viaPath: ['t3'] }),
    );
    expect(compiled.textContent).toContain('Move from Coast through Vale and Pass to Ridge?');
    expect(page.mapForces().find((force) => force.id === 'force-1')?.routeSteps).toEqual(['t1', 't2', 't3', 't4']);
    expect(compiled.querySelectorAll('.order-route')).toHaveLength(3);
    expect(compiled.querySelectorAll('.order-route-head')).toHaveLength(3);
    http.verify();
  });

  it('plans a chosen teleport by picking a non-spawn destination on the map', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [
      squareTerritory('t1', 'Coast', 0.1),
      squareTerritory('t2', 'Vale', 0.4),
      squareTerritory('t3', 'Ridge', 0.7),
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
    const base = playState();
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        forces: [
          {
            ...base.forces[0],
            availableActions: ['Hold', 'Teleport'],
            canChooseTeleportDestination: true,
            teleportTargets: ['t2', 't3'],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onTerritorySelect: (event: { id: string; additive: boolean; clientX: number; clientY: number }) => void;
      onMapActionKind: (kind: string) => void;
      confirmMapAction: () => Promise<void>;
      mapAction: () => { step: string; kind: string; targetTerritoryId: string } | null;
    };

    page.onTerritorySelect({ id: 't1', additive: false, clientX: 40, clientY: 12 });
    page.onMapActionKind('Teleport');
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('pick-target');
    expect(compiled.textContent).toContain('Pick a non-spawn territory to teleport to...');

    page.onTerritorySelect({ id: 't3', additive: false, clientX: 90, clientY: 12 });
    fixture.detectChanges();
    expect(page.mapAction()?.step).toBe('confirm');
    expect(compiled.textContent).toContain('Teleport from Coast to Ridge?');

    const pending = page.confirmMapAction();
    const draft = http.expectOne(`/api/campaigns/${campaign.id}/play/draft`);
    expect(draft.request.body as { kind: string; targetTerritoryId: string }).toEqual(
      expect.objectContaining({
        kind: 'Teleport',
        targetTerritoryId: 't3',
      }),
    );
    draft.flush(
      playState({
        revision: 3,
        myDrafts: [{ forceId: 'force-1', kind: 'Teleport', targetTerritoryId: 't3', structureTypeId: null }],
        forces: [
          {
            ...base.forces[0],
            availableActions: ['Hold', 'Teleport'],
            canChooseTeleportDestination: true,
            teleportTargets: ['t2', 't3'],
          },
        ],
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
    expect(visibleText(compiled)).toContain('Draft: Teleport to Ridge');
    http.verify();
  });

  it('labels specified teleport and lets a force drop an unopened item on Move', async () => {
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
      itemObjectiveTypes: [
        {
          id: 'crown',
          name: 'Crown',
          isHiddenUntilFound: false,
          placement: 'Random',
          allowOnSpawn: false,
          effects: [{ id: 'e1', kind: 'TeleportToChosenNonSpawnOncePerRound', amount: 0 }],
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        hasMap: false,
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
            availableActions: ['Hold', 'Move', 'TeleportRandomly', 'TeleportToSpecificTerritory'],
            canChooseTeleportDestination: true,
            teleportTargets: ['t2'],
            droppableItemObjectiveIds: ['item-1'],
          },
        ],
        itemObjectives: [
          {
            id: 'item-1',
            typeId: 'crown',
            name: 'Crown',
            territoryId: null,
            possessorForceId: 'force-1',
            isRevealed: true,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const options = [...compiled.querySelectorAll('#kind-force-1 option')].map((option) => option.textContent.trim());
    expect(options).toContain('Teleport Randomly');
    expect(options).toContain('Teleport to Specific Territory');

    const page = fixture.componentInstance as unknown as {
      onDraftKind: (forceId: string, kind: string) => void;
      onDraftTarget: (forceId: string, targetTerritoryId: string) => void;
      onDraftDropItem: (forceId: string, itemId: string, selected: boolean) => void;
    };
    page.onDraftKind('force-1', 'Move');
    page.onDraftTarget('force-1', 't2');
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Drop Crown');
    page.onDraftDropItem('force-1', 'item-1', true);
    fixture.detectChanges();
    const saveDraft = compiled.querySelector<HTMLButtonElement>('button[aria-label^="Save draft"]');
    expect(saveDraft).toBeTruthy();
    saveDraft!.click();
    const draft = http.expectOne(`/api/campaigns/${campaign.id}/play/draft`);
    expect(draft.request.body as { kind: string; droppedItemObjectiveIds: string[] }).toEqual(
      expect.objectContaining({
        kind: 'Move',
        droppedItemObjectiveIds: ['item-1'],
      }),
    );
    draft.flush(
      playState({
        hasMap: false,
        revision: 3,
        myDrafts: [
          {
            forceId: 'force-1',
            kind: 'Move',
            targetTerritoryId: 't2',
            structureTypeId: null,
            droppedItemObjectiveIds: ['item-1'],
          },
        ],
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
            availableActions: ['Hold', 'Move', 'TeleportRandomly', 'TeleportToSpecificTerritory'],
            droppableItemObjectiveIds: ['item-1'],
          },
        ],
        itemObjectives: [
          {
            id: 'item-1',
            typeId: 'crown',
            name: 'Crown',
            territoryId: null,
            possessorForceId: 'force-1',
            isRevealed: true,
          },
        ],
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
    expect(compiled.textContent).toContain('Draft: Move');
    http.verify();
  });

  it('highlights a faction’s territories and forces when that faction is clicked', async () => {
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      focusOnMap: (kind: 'player' | 'faction' | 'ally', id: string) => void;
      selectedIds: () => string[];
      focusedForceIds: () => string[];
      mapAction: () => unknown;
    };
    expect(page.mapAction()).toBeNull();
    page.focusOnMap('faction', '1');
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t1']);
    expect(page.focusedForceIds()).toEqual(['force-1']);

    page.focusOnMap('player', 'user-1');
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t1']);
    expect(page.focusedForceIds()).toEqual(['force-1']);

    page.focusOnMap('player', 'user-1');
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual([]);
    expect(page.focusedForceIds()).toEqual([]);
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
    openSection(fixture, 'manage');
    openSection(fixture, 'itemObjectives');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Reveal hidden objectives');
    expect(compiled.textContent).toContain('Apply correction');
    expect(compiled.textContent).toContain('Apply and re-resolve previous');
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
    flushLog(http);
    await pending;
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Crown');
    expect(compiled.textContent).not.toContain('(hidden)');
    http.verify();
  });

  it('lists the viewer private objectives first and hides unclaimed counts', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
      participants: [
        ...campaign.participants,
        {
          userId: 'user-2',
          username: 'southplayer',
          displayName: 'Ada',
          isPlayer: true,
          isGameMaster: false,
          isAdministrator: false,
          factionName: 'South',
          subfaction: null,
          currentSupplyPoints: 1,
          temporarySupplyPoints: 0,
          contributions: [],
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        privateObjectiveUnclaimedCounts: [
          { holderKind: 'Player', holderId: 'user-1', holderName: 'northplayer', count: 1 },
        ],
        privateObjectives: [
          {
            id: 'po-1',
            typeId: 'type-1',
            holderKind: 'Player',
            holderId: 'user-1',
            status: 'Assigned',
            scoringKind: 'Manual',
            name: 'Hold the pass',
            description: 'Control the highland pass.',
            campaignPoints: 3,
            canClaim: true,
            canModerate: false,
          },
          {
            id: 'po-traitor',
            typeId: 'type-traitor',
            holderKind: 'Traitor',
            holderId: 'user-1',
            status: 'Assigned',
            scoringKind: 'Manual',
            name: 'Betray the pact',
            description: 'Strike your former allies.',
            campaignPoints: 4,
            canClaim: true,
            canModerate: false,
          },
          {
            id: 'po-2',
            typeId: 'type-2',
            holderKind: 'Player',
            holderId: 'user-2',
            status: 'Revealed',
            scoringKind: 'Manual',
            name: 'Raid the coast',
            description: 'Strike the shoreline.',
            campaignPoints: 2,
            canClaim: false,
            canModerate: false,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'privateObjectives');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Private objectives');
    expect(compiled.textContent).not.toContain('northplayer: 1');
    expect(compiled.textContent).not.toContain('Unclaimed private objectives (public counts only)');
    expect(compiled.textContent).toContain('Your unclaimed private objectives');
    expect(compiled.textContent).toContain('Hold the pass');
    expect(compiled.textContent).toContain('Control the highland pass.');
    expect(compiled.textContent).toContain('(3 CP)');
    expect(compiled.textContent).toContain('Betray the pact');
    expect(compiled.textContent).toContain('Strike your former allies.');
    expect(compiled.textContent).toContain('(4 CP)');
    const privatePanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Private objectives'),
    );
    const claimed = privatePanel?.querySelector('.claimed-private-objectives') as HTMLDetailsElement | undefined;
    expect(claimed).toBeTruthy();
    expect(claimed?.open).toBe(false);
    expect(claimed?.textContent).toContain('Raid the coast');
    expect(claimed?.textContent).toContain('Ada · South');

    const page = fixture.componentInstance as unknown as { claimPrivateObjective: (id: string) => Promise<void> };
    const pending = page.claimPrivateObjective('po-1');
    const request = http.expectOne(`/api/campaigns/${campaign.id}/play/private-objectives/claim`);
    expect(request.request.body).toEqual({ revision: campaign.revision, assignmentId: 'po-1' });
    request.flush(
      playState({
        privateObjectives: [
          {
            id: 'po-1',
            typeId: 'type-1',
            holderKind: 'Player',
            holderId: 'user-1',
            status: 'Claimed',
            scoringKind: 'Manual',
            name: 'Hold the pass',
            canClaim: false,
            canModerate: true,
          },
        ],
      }),
    );
    await pending;
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Approve points');
    http.verify();
  });

  it('lists the viewer secret rival and revealed rival victories', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
      participants: [
        ...campaign.participants,
        {
          userId: 'user-2',
          username: 'southplayer',
          displayName: 'Ada',
          isPlayer: true,
          isGameMaster: false,
          isAdministrator: false,
          factionName: 'South',
          subfaction: null,
          currentSupplyPoints: 1,
          temporarySupplyPoints: 0,
          contributions: [],
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        rivalObjectives: [
          {
            id: 'rival-1',
            holderUserId: 'user-1',
            status: 'Assigned',
            rivalUserId: 'user-2',
            rivalDisplayName: 'Ada',
            rivalFactionName: 'South',
            rivalSubfaction: 'Corsairs',
            campaignPoints: 5,
          },
          {
            id: 'rival-2',
            holderUserId: 'user-2',
            status: 'Revealed',
            rivalUserId: 'user-1',
            rivalDisplayName: 'northplayer',
            rivalFactionName: 'North',
            rivalSubfaction: 'Riders',
            campaignPoints: 5,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'privateObjectives');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Your unclaimed private objectives');
    expect(compiled.textContent).toContain('Secret rival');
    expect(compiled.textContent).toContain('Defeat Ada (South — Corsairs) in battle or by surrender');
    expect(compiled.textContent).toContain('(5 CP)');
    const claimed = compiled.querySelector('.claimed-private-objectives');
    expect(claimed).toBeTruthy();
    expect(claimed?.textContent).toContain('Defeated northplayer (North — Riders)');
    expect(claimed?.textContent).toContain('Ada');
    http.verify();
  });

  it('shows the rival faction from participants when play omits those fields', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
      participants: [
        ...campaign.participants,
        {
          userId: 'user-2',
          username: 'southplayer',
          displayName: 'Ada',
          isPlayer: true,
          isGameMaster: false,
          isAdministrator: false,
          factionName: 'South',
          subfaction: 'Corsairs',
          currentSupplyPoints: 1,
          temporarySupplyPoints: 0,
          contributions: [],
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        rivalObjectives: [
          {
            id: 'rival-1',
            holderUserId: 'user-1',
            status: 'Assigned',
            rivalUserId: 'user-2',
            rivalDisplayName: 'Ada',
            campaignPoints: 5,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'privateObjectives');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Your unclaimed private objectives');
    expect(compiled.textContent).toContain('Defeat Ada (South — Corsairs) in battle or by surrender');
    http.verify();
  });

  it('shows automatic private objective progress next to the description', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
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
        privateObjectives: [
          {
            id: 'po-auto',
            typeId: 'type-auto',
            holderKind: 'Player',
            holderId: 'user-1',
            status: 'Assigned',
            scoringKind: 'Automatic',
            name: 'Hold five territories',
            description: 'Control five territories.',
            campaignPoints: 4,
            currentCount: 2,
            requiredCount: 5,
            canClaim: false,
            canModerate: false,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'faction');

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Control five territories. (2/5)');
    http.verify();
  });

  it('does not require an action from a force that is already in battle', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [squareTerritory('t1', 'Midland', 0.1)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        isCommitted: true,
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: ['Surrender'],
          },
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't1',
            isMine: false,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        commitments: [
          { userId: 'user-1', username: 'northplayer', isCommitted: true },
          { userId: 'user-2', username: 'southplayer', isCommitted: false },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'AwaitingResults',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: null,
            isDraw: false,
            needsRetreat: false,
            retreatTargets: [],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'orders');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain(
      'Locked in battle at Midland with southplayer · South. This force cannot perform an action until the battle is resolved. Surrender from the map.',
    );
    expect(compiled.textContent).toContain('You have no actions to commit this phase.');
    expect(compiled.textContent).not.toContain('Choose an action for each of your forces');
    expect(compiled.querySelectorAll('.commitment-summary').length).toBe(1);
    expect(compiled.textContent).toContain('1 of 2 players committed. Waiting on southplayer.');
    expect(compiled.querySelector('#kind-force-1')).toBeNull();
    const commit = [...compiled.querySelectorAll('button')].find((button) =>
      button.textContent.trim().startsWith('Commit Actions'),
    );
    expect(commit).toBeUndefined();
    expect(compiled.textContent).not.toContain('Uncommit');
    expect(compiled.querySelector('.campaign-status-bar')?.textContent).toContain('Committed');
    http.verify();
  });

  it('sorts campaign point standings when a column header is clicked', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({ ...campaign, status: 'InProgress' });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const names = (): (string | undefined)[] =>
      [...compiled.querySelectorAll('.standings-table tbody tr')].map((row) =>
        row.querySelector('.map-focus-button')?.textContent.trim(),
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

  it('labels standings columns, shows faction names, and marks the viewer row', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({ ...campaign, status: 'InProgress' });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Standings');
    openSection(fixture, 'links');
    expect(compiled.querySelector('a[href="https://example.test/notes"]')?.textContent).toContain('Notes');
    const headers = [...compiled.querySelectorAll<HTMLTableCellElement>('.standings-table thead th')];
    expect(headers.every((header) => header.getAttribute('scope') === 'col')).toBe(true);
    expect(
      [...compiled.querySelectorAll<HTMLButtonElement>('.standings-table th button')].map((button) =>
        button.textContent.trim(),
      ),
    ).toContain('Territories and structures');
    expect(compiled.textContent).toContain('Battle points');
    expect(compiled.textContent).not.toContain('Battles Won');
    expect(compiled.textContent).not.toContain('Structures captured');

    const viewerRow = compiled.querySelector('.standings-table tbody tr.is-viewer');
    expect(viewerRow).toBeTruthy();
    expect(viewerRow?.querySelector('th')?.getAttribute('scope')).toBe('row');
    expect(viewerRow?.querySelector('.sr-only')?.textContent).toBe('You');
    expect(viewerRow?.querySelector('.standing-faction')?.textContent).toContain('North');
    expect(viewerRow?.querySelector('a.profile-link')).toBeNull();

    const otherRow = [...compiled.querySelectorAll('.standings-table tbody tr')].find(
      (row) => !row.classList.contains('is-viewer'),
    );
    expect(otherRow?.querySelector('a.profile-link')?.textContent.trim()).toBe('southplayer');
    expect(otherRow?.querySelector('.standing-faction')?.textContent).toContain('South');
    expect(compiled.querySelector('.standings-table .standings-total')?.textContent.trim()).toBe('Total');
    const viewerPoints = [...viewerRow!.querySelectorAll('td.standings-points')];
    expect(viewerPoints).toHaveLength(6);
    expect(viewerPoints[0]?.getAttribute('title')).toContain('Town: 4');
    expect(viewerPoints[0]?.getAttribute('title')).toContain('Total: 4');
    expect(viewerPoints[1]?.getAttribute('title')).toContain('Resolved battles: 2');
    expect(viewerPoints[2]?.getAttribute('title')).toContain('Most territories: 1');
    expect(viewerPoints[4]?.getAttribute('title')).toContain('Crown: 3');
    expect(viewerPoints[5]?.getAttribute('title')).toContain('Town: 4');
    expect(viewerPoints[5]?.getAttribute('title')).toContain('Crown: 3');
    expect(viewerPoints[5]?.getAttribute('title')).toContain('Total: 10');
    http.verify();
  });

  it('tints a standing faction logo when that option is enabled', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      factions: [{ ...campaign.factions[0], hasFlagImage: true, tintFlagImage: true }, campaign.factions[1]],
      standings: [{ ...campaign.standings[0], hasFlagImage: true, tintFlagImage: true }, campaign.standings[1]],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.standing-faction app-faction-logo .is-tinted')).toBeTruthy();
    expect(compiled.querySelector('.standing-faction img')).toBeNull();
    http.verify();
  });

  it('uses the parent faction logo when a player subfaction inherits', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      factions: [
        {
          ...campaign.factions[0],
          hasFlagImage: true,
          tintFlagImage: false,
          subfactionAppearances: [{ name: 'Riders', color: null, flagSource: 'inherit', hasFlagImage: false }],
        },
        campaign.factions[1],
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const srcs = [...compiled.querySelectorAll<HTMLImageElement>('app-faction-logo img')].map((image) => image.src);
    expect(srcs.some((src) => src.includes('/factions/1/flag'))).toBe(true);
    expect(srcs.every((src) => !src.includes('/subfactions/'))).toBe(true);
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
    flushLog(http);
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

  it('asks for supply-costing units and offers surrender during an open battle', async () => {
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
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
      terrainTypes: [{ id: 'plains', name: 'Plains', color: '#7CB342', missions: [] }],
      structureTypes: [
        {
          id: 'keep',
          name: 'Keep',
          builtinSymbol: 'Keep',
          hasImage: false,
          hasPillagedImage: false,
          isBuildable: false,
          isPillageable: true,
          isDestructible: true,
          missions: [],
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [
        {
          ...squareTerritory('t1', 'Coast', 0.1),
          terrainTypeId: 'plains',
          structureTypeId: 'keep',
        },
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: ['Surrender'],
          },
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't1',
            isMine: false,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'AwaitingResults',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: null,
            isDraw: false,
            needsRetreat: false,
            canSurrender: true,
            retreatTargets: ['t2'],
            forceSupplies: [
              {
                forceId: 'force-1',
                userId: 'user-1',
                forceAllowancePoints: 3,
                currentSupplyPoints: 4,
                temporarySupplyPoints: 1,
                alliedArmyPoints: 1000,
                freeCharacterCount: 2,
                contributions: [
                  { kind: 'TerritoryTerrain', label: 'Coast terrain (Plains)', points: 1, isAllied: false },
                  { kind: 'Temporary', label: 'Temporary supply', points: 1, isAllied: false },
                ],
              },
              {
                forceId: 'force-2',
                userId: 'user-2',
                forceAllowancePoints: 2,
                currentSupplyPoints: 2,
                temporarySupplyPoints: 0,
                alliedArmyPoints: 1000,
                freeCharacterCount: 1,
                contributions: [
                  { kind: 'TerritoryTerrain', label: 'Ridge terrain (Hills)', points: 2, isAllied: false },
                ],
              },
            ],
            mission: {
              id: 'mission-1',
              name: 'Meeting engagement',
              url: 'https://example.test/missions/meeting',
              hasFile: false,
              fileName: null,
              isAttackerDefender: true,
            },
            attackerForceId: 'force-2',
            defenderForceId: 'force-1',
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'battles');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Supply-costing units');
    expect(compiled.textContent).toContain('Army list (optional text)');
    expect(compiled.textContent).toContain('Other Battle Points');
    expect(compiled.textContent).toContain('Battle summary');
    expect(compiled.querySelector('#army-battle-1-force-1')?.getAttribute('aria-required')).toBe('true');
    expect(compiled.textContent).toContain('Army points this battle');
    expect(compiled.textContent).toContain('1000');
    expect(compiled.textContent).toContain('Standard supply');
    expect(compiled.textContent).toContain('Temporary supply');
    expect(compiled.textContent).toContain('Awaiting results');
    expect(compiled.querySelector('.battle-sides')).toBeTruthy();
    expect(compiled.querySelector('table.battle-supply')).toBeTruthy();
    expect(compiled.textContent).toContain('Surrender');
    expect(compiled.textContent).toContain('Meeting engagement');
    expect(compiled.querySelector('a[href="https://example.test/missions/meeting"]')?.textContent).toContain(
      'Meeting engagement',
    );
    expect(compiled.textContent).toContain('Attacker');
    expect(compiled.textContent).toContain('Defender');
    expect(compiled.textContent).toContain('Attacker/defender mission');
    expect(compiled.textContent).toContain('Terrain: Plains');
    expect(compiled.textContent).toContain('Structure: Keep');
    expect(compiled.textContent).toContain('Attacker army points:');
    expect(compiled.textContent).toContain('Free characters: 2');
    expect(compiled.querySelector('.battle-header')?.textContent).toContain('Attacker supply');
    expect(compiled.querySelector('.battle-header')?.textContent).toContain('Defender supply');
    const attackerSummary = [...compiled.querySelectorAll('.supply-points summary')].find((item) =>
      item.textContent.includes('Attacker supply'),
    );
    expect(attackerSummary?.getAttribute('title')).toContain('Ridge terrain (Hills): +2');
    http.verify();
  });

  it('prefills the latest opponent result and keeps an independent army list', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canManage: false,
      canChooseFaction: false,
      factionId: '1',
      currentRound: 1,
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
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
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle',
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't1',
            isMine: false,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'AwaitingResults',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: {
              submitterUserId: 'user-2',
              winnerForceId: 'force-2',
              isDraw: false,
              submittedUtc: '2026-08-14T12:01:00+00:00',
              reports: [
                {
                  forceId: 'force-1',
                  victoryPoints: 7,
                  armyPoints: 1500,
                  differentialBattlePoints: 2,
                  bonusBattlePoints: 0,
                  supplyCostingUnitCount: 1,
                  answers: [],
                },
                {
                  forceId: 'force-2',
                  victoryPoints: 12,
                  armyPoints: 2000,
                  differentialBattlePoints: 5,
                  bonusBattlePoints: 1,
                  supplyCostingUnitCount: 4,
                  answers: [],
                },
              ],
            },
            armyLists: [
              {
                forceId: 'force-2',
                submitterUserId: 'user-2',
                submittedUtc: '2026-08-14T12:00:00+00:00',
                armyPoints: 2000,
                supplyCostingUnitCount: 4,
                armyListText: 'South list',
                armyListBuilder: 'Other',
                supplyCategories: [],
              },
            ],
            winnerForceId: null,
            isDraw: false,
            needsRetreat: false,
            retreatTargets: [],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'battles');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain("Agree with other player's results");
    expect(compiled.textContent).not.toContain('Results submitted!');
    expect(compiled.textContent).toContain('Submit army list');
    expect(compiled.textContent).toContain('South list');
    expect(compiled.querySelector<HTMLInputElement>('#vp-battle-1-force-2')?.value).toBe('12');
    expect(compiled.querySelector<HTMLInputElement>('#army-battle-1-force-2')?.disabled).toBe(true);
    expect(compiled.querySelector<HTMLTextAreaElement>('#army-list-battle-1-force-1')?.disabled).toBe(false);
    http.verify();
  });

  it('summarizes a pitched battle with combatant supply, mission fallback, and battle-phase commitments', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
      currentRound: 3,
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'Ridge', 0.4)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentRound: 3,
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 3',
        currentPhaseNumber: 3,
        commitments: [{ userId: 'user-1', username: 'northplayer', isCommitted: false }],
        mapTerritories: [
          { id: 't1', ownerFactionId: '1', structureTypeId: null, structureCondition: 'Operational' },
          {
            id: 't2',
            ownerFactionId: '2',
            ownerSubfaction: 'Khorne',
            structureTypeId: null,
            structureCondition: 'Operational',
          },
        ],
        viewerSupply: {
          currentSupplyPoints: 4,
          temporarySupplyPoints: 1,
          mapSupplyPoints: 3,
          roundFreeSupplyPoints: 2,
          splitPenaltyPoints: 0,
          forceAllowancePoints: 5,
          contributions: [{ kind: 'TerritoryTerrain', label: 'Ridge terrain (Hills)', points: 2, isAllied: false }],
        },
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't1',
            isMine: false,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'AwaitingResults',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: null,
            isDraw: false,
            needsRetreat: false,
            canSurrender: false,
            retreatTargets: [],
            forceSupplies: [
              {
                forceId: 'force-1',
                userId: 'user-1',
                forceAllowancePoints: 3,
                currentSupplyPoints: 4,
                temporarySupplyPoints: 1,
                alliedArmyPoints: 1500,
                roundMaxArmyPoints: 1500,
                freeCharacterCount: 1,
                contributions: [
                  { kind: 'TerritoryTerrain', label: 'Ridge terrain (Hills)', points: 2, isAllied: false },
                ],
              },
              {
                forceId: 'force-2',
                userId: 'user-2',
                forceAllowancePoints: 2,
                currentSupplyPoints: 2,
                temporarySupplyPoints: 0,
                alliedArmyPoints: 1500,
                roundMaxArmyPoints: 1500,
                freeCharacterCount: 0,
                contributions: [
                  { kind: 'TerritoryTerrain', label: 'Coast terrain (Plains)', points: 1, isAllied: false },
                ],
              },
            ],
            mission: {
              id: 'mission-1',
              name: 'Meeting engagement',
              url: null,
              hasFile: false,
              fileName: null,
              isAttackerDefender: false,
            },
            attackerForceId: null,
            defenderForceId: null,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'battles');
    openSection(fixture, 'factions');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('0 of 1 committed');
    expect(compiled.textContent).toContain('Pitched battle');
    expect(compiled.textContent).toContain('Meeting engagement');
    expect(compiled.textContent).toContain('See Campaign Manager for Mission details.');
    expect(compiled.querySelector('a[href="https://example.test/missions/meeting"]')).toBeNull();
    expect(compiled.textContent).toContain('Combatant army points:');
    expect(compiled.textContent).toContain('1500');
    expect(compiled.textContent).not.toContain('Attacker/defender mission');
    expect(compiled.querySelector('.battle-header')?.textContent).toContain('Combatant supply');
    const summaryPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Summary'),
    );
    const factionsPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Factions'),
    );
    expect(summaryPanel?.textContent).toContain('Spendable supply');
    expect(factionsPanel?.querySelector('.supply-points')).toBeNull();
    expect(summaryPanel?.querySelector('.supply-points summary')?.getAttribute('title')).toContain(
      'Ridge terrain (Hills): +2',
    );
    const page = fixture.componentInstance as unknown as {
      graph: () => { territories: { id: string; ownerFactionId: string | null; ownerSubfaction?: string | null }[] };
    };
    expect(page.graph().territories.find((territory) => territory.id === 't2')?.ownerFactionId).toBe('2');
    expect(page.graph().territories.find((territory) => territory.id === 't2')?.ownerSubfaction).toBe('Khorne');
    http.verify();
  });

  it('infers owner subfaction from the occupying force when play overlay omits it', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
      factions: [
        campaign.factions[0],
        {
          ...campaign.factions[1],
          name: 'Daemons of Chaos',
          requiresSubfaction: true,
          hasFlagImage: true,
          subfactions: ['Khorne'],
          subfactionAppearances: [{ name: 'Khorne', color: '#B91C1C', flagSource: 'color', hasFlagImage: false }],
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [squareTerritory('t2', 'Ridge', 0.4)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        mapTerritories: [{ id: 't2', ownerFactionId: '2', structureTypeId: null, structureCondition: 'Operational' }],
        forces: [
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't2',
            subfaction: 'Khorne',
            isMine: false,
            inBattle: false,
            moveTargets: [],
            availableActions: [],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      graph: () => { territories: { id: string; ownerFactionId: string | null; ownerSubfaction?: string | null }[] };
      mapForces: () => { subfaction?: string | null }[];
    };
    expect(page.graph().territories.find((territory) => territory.id === 't2')?.ownerSubfaction).toBe('Khorne');
    expect(page.mapForces()[0]?.subfaction).toBe('Khorne');
    http.verify();
  });

  it('requires a retreat destination before submit and accepts a map selection', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'Ridge', 0.4)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'Finalized',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: 'force-2',
            isDraw: false,
            needsRetreat: true,
            awaitingRetreat: true,
            canSurrender: false,
            retreatTargets: ['t2'],
            mission: null,
            attackerForceId: null,
            defenderForceId: null,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'battles');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Awaiting Retreat Order.');
    const submit = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commit Retreat',
    );
    if (!(submit instanceof HTMLButtonElement)) {
      throw new Error('expected a Commit Retreat button');
    }
    expect(submit.disabled).toBe(true);
    openSection(fixture, 'map');
    expect(compiled.textContent).toContain('Pick a territory to retreat to...');
    expect(compiled.querySelector('.map-action-prompt')?.textContent).toContain('Pick a territory to retreat to...');

    const page = fixture.componentInstance as unknown as {
      selectTerritoryOnMap: (id: string) => void;
      retreatTargetId: (id: string) => string;
      selectedIds: () => string[];
    };
    page.selectTerritoryOnMap('t2');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t2']);
    expect(page.retreatTargetId('battle-1')).toBe('t2');
    fixture.detectChanges();
    expect(submit.disabled).toBe(false);
    http.verify();
  });

  it('shows Commit Retreat on the map toolbar during the battle phase', async () => {
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
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'Ridge', 0.4)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'Finalized',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: 'force-2',
            isDraw: false,
            needsRetreat: true,
            awaitingRetreat: true,
            canSurrender: false,
            retreatTargets: ['t2'],
            mission: null,
            attackerForceId: null,
            defenderForceId: null,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const mapCommit = [...compiled.querySelectorAll<HTMLButtonElement>('.map-toolbar button')].find(
      (button) => button.textContent.trim() === 'Commit Retreat',
    );
    expect(mapCommit).toBeTruthy();
    expect(mapCommit?.disabled).toBe(true);
    http.verify();
  });

  it('opens Surrender when the player clicks their force on the map', async () => {
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
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'Ridge', 0.4)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: ['Surrender'],
          },
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't1',
            isMine: false,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'AwaitingResults',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: null,
            isDraw: false,
            needsRetreat: false,
            canSurrender: true,
            retreatTargets: ['t2'],
            mission: null,
            attackerForceId: null,
            defenderForceId: null,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const pin = compiled.querySelector('.force-pin.is-mine');
    expect(pin).toBeInstanceOf(HTMLButtonElement);
    pin?.dispatchEvent(new MouseEvent('click', { bubbles: true, clientX: 40, clientY: 40 }));
    fixture.detectChanges();
    expect(compiled.querySelector('.action-context-menu')?.textContent).toContain('Surrender');
    http.verify();
  });

  it('lets a retreating player uncommit and warns on the last battle-phase commit', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [
        squareTerritory('t1', 'Coast', 0.1),
        squareTerritory('t2', 'Ridge', 0.4),
        squareTerritory('t3', 'South spawn', 0.7),
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        isCommitted: false,
        commitments: [
          { userId: 'user-1', username: 'northplayer', isCommitted: false },
          { userId: 'user-2', username: 'southplayer', isCommitted: true },
        ],
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'Finalized',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: 'force-2',
            isDraw: false,
            needsRetreat: true,
            awaitingRetreat: true,
            isRetreatCommitted: true,
            retreatDraftTargetId: 't2',
            canSurrender: false,
            retreatTargets: ['t2', 't3'],
            mission: null,
            attackerForceId: null,
            defenderForceId: null,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'battles');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Your retreat is committed.');
    const retreatOptions = [...compiled.querySelectorAll<HTMLOptionElement>('#retreat-battle-1 option')]
      .map((option) => option.value)
      .filter((value) => value.length > 0);
    expect(retreatOptions).toEqual(['t2', 't3']);
    const uncommit = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Uncommit',
    );
    expect(uncommit).toBeTruthy();
    uncommit!.click();
    http.expectOne(`/api/campaigns/${campaign.id}/play/retreat/uncommit`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        isCommitted: false,
        commitments: [
          { userId: 'user-1', username: 'northplayer', isCommitted: false },
          { userId: 'user-2', username: 'southplayer', isCommitted: true },
        ],
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'Finalized',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: 'force-2',
            isDraw: false,
            needsRetreat: true,
            awaitingRetreat: true,
            isRetreatCommitted: false,
            retreatDraftTargetId: 't2',
            canSurrender: false,
            retreatTargets: ['t2'],
            mission: null,
            attackerForceId: null,
            defenderForceId: null,
          },
        ],
      }),
    );
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Commit Retreat and close the phase');
    http.verify();
  });

  it('lists who still needs a result or retreat at the top of Battles', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [squareTerritory('t1', 'Coast', 0.1)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        isCommitted: false,
        commitments: [
          { userId: 'user-1', username: 'northplayer', isCommitted: false, needsResult: true, needsRetreat: false },
          { userId: 'user-2', username: 'southplayer', isCommitted: false, needsResult: false, needsRetreat: true },
        ],
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'AwaitingResults',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: null,
            isDraw: false,
            needsRetreat: false,
            retreatTargets: [],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'battles');

    const compiled = fixture.nativeElement as HTMLElement;
    const battles = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Battles'),
    );
    expect(battles?.textContent).toContain('Results and retreats');
    expect(battles?.textContent).toContain(
      '0 of 2 players finished. Waiting on northplayer (needs result), southplayer (needs retreat).',
    );
    expect(battles?.textContent).toContain('Needs result');
    expect(battles?.textContent).toContain('Needs retreat');
    http.verify();
  });

  it('hides Uncommit when a sole retreat destination was auto-committed', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'South spawn', 0.4)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        isCommitted: true,
        commitments: [
          { userId: 'user-1', username: 'northplayer', isCommitted: true },
          { userId: 'user-2', username: 'southplayer', isCommitted: true },
        ],
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'Finalized',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: 'force-2',
            isDraw: false,
            needsRetreat: false,
            awaitingRetreat: false,
            isRetreatCommitted: true,
            retreatDraftTargetId: 't2',
            canSurrender: false,
            retreatTargets: ['t2'],
            mission: null,
            attackerForceId: null,
            defenderForceId: null,
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'battles');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain(
      'This force automatically retreated to South spawn because that was the only legal destination.',
    );
    expect(
      [...compiled.querySelectorAll('button')].find((button) => button.textContent.trim() === 'Uncommit'),
    ).toBeUndefined();
    http.verify();
  });

  it('does not surrender until the second click, and names the territory and opponent', async () => {
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
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [
        {
          id: 't1',
          displayNumber: 1,
          name: 'Windmere',
          description: null,
          polygon: [],
          terrainTypeId: '',
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
          polygon: [],
          terrainTypeId: '',
          structureTypeId: null,
          structureCondition: 'Operational',
          overlayColor: null,
          ownerFactionId: null,
          spawnFactionId: null,
        },
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        forces: [
          {
            id: 'force-1',
            controllerUserId: 'user-1',
            controllerUsername: 'northplayer',
            factionId: '1',
            territoryId: 't1',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: ['Surrender'],
          },
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't1',
            isMine: false,
            inBattle: true,
            moveTargets: [],
            availableActions: [],
          },
        ],
        battles: [
          {
            id: 'battle-1',
            territoryId: 't1',
            status: 'AwaitingResults',
            participantForceIds: ['force-1', 'force-2'],
            reportingForceIds: ['force-1', 'force-2'],
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: null,
            isDraw: false,
            needsRetreat: false,
            canSurrender: true,
            retreatTargets: ['t2'],
            forceSupplies: [
              {
                forceId: 'force-1',
                userId: 'user-1',
                forceAllowancePoints: 3,
                currentSupplyPoints: 4,
                temporarySupplyPoints: 1,
                alliedArmyPoints: 1000,
              },
            ],
            mission: {
              id: 'mission-1',
              name: 'Meeting engagement',
              url: 'https://example.test/missions/meeting',
              hasFile: false,
              fileName: null,
            },
            attackerForceId: 'force-2',
            defenderForceId: 'force-1',
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'battles');

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      onRetreatTarget: (battleId: string, targetTerritoryId: string) => void;
    };
    page.onRetreatTarget('battle-1', 't2');
    fixture.detectChanges();

    const surrender = [...compiled.querySelectorAll('button')].find(
      (element) => element.textContent.trim() === 'Surrender',
    );
    expect(surrender).toBeTruthy();
    surrender!.click();
    fixture.detectChanges();
    expect(compiled.textContent).toContain(
      'Surrender Windmere to South? You can uncommit while this window stays open.',
    );
    http.verify();

    [...compiled.querySelectorAll('button')]
      .find((element) => element.textContent.includes('Surrender Windmere to South'))
      ?.click();
    const request = http.expectOne(`/api/campaigns/${campaign.id}/play/surrender`);
    expect(request.request.body).toEqual({
      revision: campaign.revision,
      battleId: 'battle-1',
      targetTerritoryId: 't2',
    });
    request.flush(playState({ currentPhaseKind: 'Battle', currentPhaseLabel: 'Battle 1' }));
    await fixture.whenStable();
    fixture.detectChanges();
    http.verify();
  });

  it('lets a manager start a ringer battle against an idle enemy force', async () => {
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
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [
        {
          id: 't1',
          displayNumber: 1,
          name: 'North spawn',
          description: null,
          polygon: [],
          terrainTypeId: '',
          structureTypeId: null,
          structureCondition: 'Operational',
          overlayColor: null,
          ownerFactionId: '1',
          spawnFactionId: '1',
        },
        {
          id: 't2',
          displayNumber: 2,
          name: 'Midland',
          description: null,
          polygon: [],
          terrainTypeId: '',
          structureTypeId: null,
          structureCondition: 'Operational',
          overlayColor: null,
          ownerFactionId: null,
          spawnFactionId: null,
        },
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
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
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't2',
            isMine: false,
            inBattle: false,
            moveTargets: [],
            availableActions: ['Hold'],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'manage');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Ringer battle');
    const forceSelect = compiled.querySelector('#ringer-force');
    expect(forceSelect).toBeTruthy();
    expect(forceSelect?.textContent).toContain('southplayer');
    expect(forceSelect?.textContent).not.toContain('northplayer');

    const page = fixture.componentInstance as unknown as { injectRingerBattle: () => Promise<void> };
    const pending = page.injectRingerBattle();
    const request = http.expectOne(`/api/campaigns/${campaign.id}/play/inject-ringer`);
    expect(request.request.body).toEqual({
      revision: campaign.revision,
      targetForceId: 'force-2',
      ringerFactionId: '1',
      missionId: null,
      playerIsDefender: false,
    });
    request.flush(playState({ currentPhaseKind: 'Battle', currentPhaseLabel: 'Battle 1' }));
    await pending;
    await fixture.whenStable();
    http.verify();
  });

  it('lets a manager assign a catalog status to a force', async () => {
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
      forceStatuses: [
        {
          id: 'status-diseased',
          name: 'Diseased',
          effects: 'Disease effects.',
          enableTrigger: 'Disease',
          clearTrigger: 'HoldAtSettlement',
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
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        forceStatuses: [
          {
            id: 'status-diseased',
            name: 'Diseased',
            effects: 'Disease effects.',
            enableTrigger: 'Disease',
            clearTrigger: 'HoldAtSettlement',
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'manage');
    openSection(fixture, 'forceStatus');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Assign force status');
    const page = fixture.componentInstance as unknown as { assignForceStatuses: () => Promise<void> };
    const pending = page.assignForceStatuses();
    const request = http.expectOne(`/api/campaigns/${campaign.id}/play/set-force-statuses`);
    expect(request.request.body).toEqual({
      revision: campaign.revision,
      forceIds: ['force-1'],
      statusName: 'Normal',
    });
    request.flush(playState());
    await pending;
    await fixture.whenStable();
    http.verify();
  });

  it('lists force statuses collapsed by default on an upcoming campaign', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      forceStatuses: [
        {
          id: 'status-diseased',
          name: 'Diseased',
          effects: 'Disease effects.',
          enableTrigger: 'ConsecutiveActions',
          clearTrigger: 'Hold',
          immuneFactionIds: ['2'],
          immuneSubfactions: [{ factionId: '1', subfaction: 'Riders' }],
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [{ ...squareTerritory('t1', 'Coast', 0.1), spawnFactionId: '1', spawnSubfaction: null }],
      adjacencies: [],
    });
    flushPlayUnavailable(http);
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      isOpen: (id: string) => boolean;
      toggleCatalogStatus: (id: string) => void;
    };
    expect(page.isOpen('forceStatusCatalog')).toBe(false);
    expect(compiled.textContent).toContain('Force statuses');
    expect(compiled.textContent).not.toContain('Disease effects.');
    expect(compiled.textContent).not.toContain('North — Riders');

    openSection(fixture, 'forceStatusCatalog');
    expect(compiled.textContent).not.toContain('Disease effects.');

    page.toggleCatalogStatus('status-diseased');
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Disease effects.');
    expect(compiled.textContent).toContain('After consecutive action phases');
    expect(compiled.textContent).toContain('No forces currently have this status.');
    expect(compiled.textContent).toContain('South');
    expect(compiled.textContent).toContain('North — Riders');
  });

  it('lists force statuses collapsed by default with forces, locations, and immunities', async () => {
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
      forceStatuses: [
        {
          id: 'status-diseased',
          name: 'Diseased',
          effects: 'Disease effects.',
          enableTrigger: 'ConsecutiveActions',
          clearTrigger: 'Hold',
          enableConditions: [{ trigger: 'ConsecutiveActions', occurrences: 3, locationKind: 'TerrainTag' }],
          clearConditions: [{ trigger: 'Hold', occurrences: 1, locationKind: 'StructureType' }],
          immuneFactionIds: ['2'],
          immuneSubfactions: [{ factionId: '1', subfaction: 'Riders' }],
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [{ ...squareTerritory('t1', 'Coast', 0.1), spawnFactionId: '1', spawnSubfaction: null }],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
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
            statusName: 'Diseased',
          },
        ],
        forceStatuses: [
          {
            id: 'status-diseased',
            name: 'Diseased',
            effects: 'Disease effects.',
            enableTrigger: 'ConsecutiveActions',
            clearTrigger: 'Hold',
            immuneFactionIds: ['2'],
            immuneSubfactions: [{ factionId: '1', subfaction: 'Riders' }],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      isOpen: (id: string) => boolean;
      toggleCatalogStatus: (id: string) => void;
    };
    expect(page.isOpen('forceStatusCatalog')).toBe(false);
    expect(compiled.textContent).toContain('Force statuses');
    expect(compiled.textContent).not.toContain('Disease effects.');

    openSection(fixture, 'forceStatusCatalog');
    expect(compiled.textContent).not.toContain('Disease effects.');

    page.toggleCatalogStatus('status-diseased');
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Disease effects.');
    expect(compiled.textContent).toContain('After consecutive action phases');
    expect(compiled.textContent).toContain('northplayer');
    expect(compiled.textContent).toContain('North');
    expect(compiled.textContent).toContain('Coast');
    expect(compiled.textContent).toContain('South');
    expect(compiled.textContent).toContain('North — Riders');
  });

  it('offers ringer, player, and draw when reporting a ringer battle', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: true,
      canPlay: true,
      canChooseFaction: false,
      factionId: '2',
      currentRound: 1,
      currentPhaseNumber: 3,
      currentPhaseKind: 'Battle',
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
        currentPhaseKind: 'Battle',
        currentPhaseLabel: 'Battle 1',
        factionId: '2',
        forces: [
          {
            id: 'force-2',
            controllerUserId: 'user-2',
            controllerUsername: 'southplayer',
            factionId: '2',
            territoryId: 't2',
            isMine: true,
            inBattle: true,
            moveTargets: [],
            availableActions: ['Surrender'],
          },
        ],
        battles: [
          {
            id: 'battle-ringer',
            territoryId: 't2',
            status: 'AwaitingResults',
            participantForceIds: ['force-2'],
            reportingForceIds: ['force-2'],
            isRinger: true,
            ringerFactionId: '1',
            isMine: true,
            mySubmission: null,
            opponentSubmission: null,
            winnerForceId: null,
            isDraw: false,
            needsRetreat: false,
            canSurrender: false,
            retreatTargets: [],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'battles');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Winner');
    expect(compiled.textContent).toContain('Ringer');
    expect(compiled.textContent).toContain('Player');
    expect(compiled.textContent).toContain('Draw');
    http.verify();
  });

  it('turns written territory names into map links that select, dim others, and scroll to the map', async () => {
    const scrollIntoView = vi.fn();
    HTMLElement.prototype.scrollIntoView = scrollIntoView;
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const territories = [
      squareTerritory('t1', 'Coast', 0.1),
      squareTerritory('t2', 'Ridge', 0.4),
      squareTerritory('t3', 'Marsh', 0.7),
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
      adjacencies: [
        {
          id: 'ab',
          territoryAId: 't1',
          territoryBId: 't2',
          origin: 'Manual',
          marker: { x: 0.35, y: 0.2 },
        },
      ],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState());
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      selectedIds: () => string[];
      isOpen: (id: string) => boolean;
    };
    const spawnLink = [...compiled.querySelectorAll<HTMLButtonElement>('.territory-link')].find(
      (button) => button.textContent.trim() === 'Coast',
    );
    expect(spawnLink).toBeTruthy();
    spawnLink!.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.selectedIds()).toEqual(['t1']);
    expect(page.isOpen('map')).toBe(true);
    expect(scrollIntoView).toHaveBeenCalled();
    expect(compiled.querySelector('.territory[data-id="t1"]')?.classList.contains('is-selected')).toBe(true);
    expect(compiled.querySelector('.territory[data-id="t2"]')?.classList.contains('is-half-highlighted')).toBe(true);
    expect(compiled.querySelector('.territory[data-id="t3"]')?.classList.contains('is-dimmed')).toBe(true);

    const ridgeLink = [...compiled.querySelectorAll<HTMLButtonElement>('.territory-link')].find(
      (button) => button.textContent.trim() === 'Ridge',
    );
    expect(ridgeLink).toBeTruthy();
    ridgeLink!.click();
    fixture.detectChanges();
    expect(page.selectedIds()).toEqual(['t2']);
    http.verify();
  });

  it('selects a territory from the map directory and announces details', async () => {
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
      territories: [squareTerritory('t1', 'Coast', 0.1), squareTerritory('t2', 'Ridge', 0.4)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState());
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.map-meta')?.getAttribute('aria-live')).toBe('polite');
    expect(compiled.querySelector('.map-main .map-meta')).toBeTruthy();
    expect(compiled.querySelector('.map-guide .map-meta')).toBeNull();
    expect(compiled.textContent).toContain('Select a territory to see its details.');
    const hit = compiled.querySelector('.territory-hit[data-id="t1"]');
    expect(hit?.getAttribute('role')).toBe('button');
    expect(hit?.getAttribute('tabindex')).toBe('0');
    hit?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    fixture.detectChanges();
    expect(compiled.querySelector('.map-meta h3')?.textContent).toContain('Coast');
    expect(compiled.textContent).not.toContain('Select a territory to see its details.');
    http.verify();
  });

  it('keeps the territory details panel height stable when selecting a territory', async () => {
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
      territories: [
        {
          ...squareTerritory('t1', 'Coast', 0.1),
          description:
            'A long contested shoreline with supply caches, watchtowers, and overlapping claims that wrap the details text.',
        },
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState());
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const meta = compiled.querySelector('.map-meta');
    expect(meta).toBeTruthy();
    const emptyStyle = getComputedStyle(meta!);
    expect(emptyStyle.overflow).toBe('auto');
    const emptyHeight = emptyStyle.height;
    expect(compiled.textContent).toContain('Select a territory to see its details.');

    const page = fixture.componentInstance as unknown as { hoveredTerritoryId: { set(id: string | null): void } };
    page.hoveredTerritoryId.set('t1');
    fixture.detectChanges();

    expect(compiled.querySelector('.map-meta h3')?.textContent).toContain('Coast');
    const selectedStyle = getComputedStyle(meta!);
    expect(selectedStyle.height).toBe(emptyHeight);
    expect(selectedStyle.overflow).toBe('auto');

    page.hoveredTerritoryId.set(null);
    fixture.detectChanges();
    compiled.querySelector<HTMLButtonElement>('button[title="Full screen (M)"]')?.click();
    fixture.detectChanges();
    expect(compiled.querySelector('app-campaign-map-view')?.classList.contains('is-fullscreen')).toBe(true);
    expect(compiled.textContent).toContain('Select a territory to see its details.');
    const fullscreenEmpty = getComputedStyle(meta!).height;
    page.hoveredTerritoryId.set('t1');
    fixture.detectChanges();
    expect(compiled.querySelector('.map-meta h3')?.textContent).toContain('Coast');
    expect(getComputedStyle(meta!).height).toBe(fullscreenEmpty);
    http.verify();
  });

  it('shows a top five for each enabled public objective except allied relic control', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    const leader = {
      userId: 'user-1',
      username: 'northplayer',
      displayName: 'northplayer',
      rank: 1,
      metric: 3,
      tieBreakMetric: 0,
      awardsPoints: true,
    };
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      publicObjectiveLeaderboards: [
        { kind: 'MostTerritories', awardPoints: 5, leaders: [leader] },
        { kind: 'LongestTerritoryChain', awardPoints: 3, leaders: [leader] },
        { kind: 'MostBattlesWon', awardPoints: 4, leaders: [{ ...leader, metric: 2, tieBreakMetric: 1 }] },
        { kind: 'MostStructurePoints', awardPoints: 2, leaders: [leader] },
        { kind: 'PointsPerTerritory', awardPoints: 1, leaders: [leader] },
        {
          kind: 'NamedPublicObjective',
          title: 'First to Magritta',
          awardPoints: 8,
          leaders: [{ ...leader, tiedPlayerCount: 4, metric: 2, userId: '00000000-0000-0000-0000-000000000000' }],
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const titles = [...compiled.querySelectorAll('.leaderboard h3')].map((heading) => heading.textContent.trim());
    expect(titles).toEqual([
      'Most territories (5 CP)',
      'Longest territory chain (3 CP)',
      'Most battles won (4 CP)',
      'Most structure points (2 CP)',
      'Campaign points per territory (1 CP)',
      'First to Magritta (8 CP)',
    ]);
    expect(compiled.textContent).toContain('4 players tied with 2');
    expect(compiled.textContent).not.toContain('Allied relic');
    http.verify();
  });

  it('lists ally groups alphabetically with factions, subfactions, and current players', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      factions: [
        {
          id: 'f-zebra',
          name: 'Zebra',
          color: '#111111',
          subfactions: ['Zulu', 'Alpha'],
          allyGroupId: 'g-zeta',
          allyGroupName: 'Zeta Pact',
          requiresSubfaction: false,
          hasFlagImage: false,
        },
        {
          id: 'f-apple',
          name: 'Apple',
          color: '#222222',
          subfactions: [],
          allyGroupId: 'g-zeta',
          allyGroupName: 'Zeta Pact',
          requiresSubfaction: false,
          hasFlagImage: false,
        },
        {
          id: 'f-mid',
          name: 'Midland',
          color: '#333333',
          subfactions: ['East'],
          allyGroupId: 'g-alpha',
          allyGroupName: 'Alpha League',
          requiresSubfaction: false,
          hasFlagImage: false,
        },
        campaign.factions[1],
      ],
      allyGroups: [
        { id: 'g-zeta', name: 'Zeta Pact', color: '#0F172A' },
        { id: 'g-alpha', name: 'Alpha League', color: '#1E3A8A' },
      ],
      participants: [
        {
          userId: 'user-xavier',
          username: 'xavier',
          displayName: 'Xavier',
          isPlayer: true,
          isGameMaster: false,
          isAdministrator: false,
          factionId: 'f-zebra',
          factionName: 'Zebra',
          subfaction: 'Zulu',
        },
        {
          userId: 'user-ada',
          username: 'ada',
          displayName: 'Ada',
          isPlayer: true,
          isGameMaster: false,
          isAdministrator: false,
          factionId: 'f-apple',
          factionName: 'Apple',
          subfaction: null,
        },
        {
          userId: 'user-bob',
          username: 'bob',
          displayName: 'Bob',
          isPlayer: true,
          isGameMaster: false,
          isAdministrator: false,
          factionId: 'f-mid',
          factionName: 'Midland',
          subfaction: 'East',
        },
        {
          userId: 'user-gm',
          username: 'gm',
          displayName: 'Greta',
          isPlayer: false,
          isGameMaster: true,
          isAdministrator: false,
          factionId: 'f-apple',
          factionName: 'Apple',
          subfaction: null,
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const alliesHeading = [...compiled.querySelectorAll('h2')].find((heading) =>
      heading.textContent.includes('Ally groups'),
    );
    const panel = alliesHeading?.closest('.panel');
    expect(panel instanceof HTMLElement).toBe(true);
    if (!(panel instanceof HTMLElement)) {
      return;
    }

    const groups = [...panel.querySelectorAll(':scope > ul.plain-list > li')];
    expect(groups.map((group) => visibleText(group))).toEqual([
      'Alpha League (1 player) - Midland (East) Bob (Midland, East)',
      'Zeta Pact (2 players) - Apple, Zebra (Alpha, Zulu) Ada (Apple) Xavier (Zebra, Zulu)',
    ]);

    const firstPlayers = groups[0]?.querySelector('.ally-group-players');
    expect(firstPlayers instanceof HTMLElement).toBe(true);
    if (!(firstPlayers instanceof HTMLElement)) {
      return;
    }

    expect(visibleText(firstPlayers)).toBe('Bob (Midland, East)');
    expect([...(groups[1]?.querySelectorAll('.ally-group-players li') ?? [])].map((item) => visibleText(item))).toEqual(
      ['Ada (Apple)', 'Xavier (Zebra, Zulu)'],
    );
    http.verify();
  });

  it('keeps Actions, Chat, and Standings open by default while a campaign is running', async () => {
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
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState());
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as { isOpen: (id: string) => boolean };
    expect(page.isOpen('orders')).toBe(true);
    expect(page.isOpen('log')).toBe(true);
    expect(page.isOpen('standings')).toBe(true);
    expect(page.isOpen('map')).toBe(false);
    expect(page.isOpen('manage')).toBe(false);
    expect(page.isOpen('details')).toBe(false);
    expect(compiled.textContent).toContain('Choose an action for each of your forces');
    expect(compiled.querySelectorAll('.commitment-summary').length).toBe(1);
    const statusBar = compiled.querySelector('.campaign-status-bar');
    const actions = [...compiled.querySelectorAll('h2')].find((heading) => heading.textContent.includes('Actions'));
    const log = compiled.querySelector('app-campaign-log');
    expect(statusBar).toBeTruthy();
    expect(actions).toBeTruthy();
    expect(log).toBeTruthy();
    expect(statusBar!.compareDocumentPosition(actions!) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(actions!.compareDocumentPosition(log!) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(compiled.textContent).toContain('Manage campaign');
    expect(compiled.querySelector('.manage-campaign .button-danger')).toBeNull();
    openSection(fixture, 'manage');
    expect(compiled.textContent).toContain('Actions here are attributed to you and notified to the campaign');
    expect(compiled.querySelector('.manage-campaign .button-danger')?.textContent).toContain('End campaign');
    http.verify();
  });

  it('warns when the viewer is the last required commitment', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
        hasMap: false,
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
        commitments: [
          { userId: 'user-1', username: 'northplayer', isCommitted: false },
          { userId: 'user-2', username: 'southplayer', isCommitted: true },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const commit = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Commit Actions and close the phase',
    );
    expect(commit).toBeTruthy();
    expect(commit?.hasAttribute('disabled')).toBe(false);
    commit!.click();
    fixture.detectChanges();
    expect(compiled.querySelector('[role="alertdialog"]')?.textContent).toContain(
      'You are the last player to commit. This closes planning for all players immediately and cannot be undone.',
    );
    http.verify();
  });

  it('asks to close the phase from the map Commit button when you are last', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [squareTerritory('t1', 'Coast', 0.1)],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
        commitments: [
          { userId: 'user-1', username: 'northplayer', isCommitted: false },
          { userId: 'user-2', username: 'southplayer', isCommitted: true },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'map');

    const compiled = fixture.nativeElement as HTMLElement;
    const mapCommit = [...compiled.querySelectorAll<HTMLButtonElement>('.map-toolbar button')].find(
      (button) => button.textContent.trim() === 'Commit Actions and close the phase',
    );
    expect(mapCommit).toBeTruthy();
    expect(mapCommit!.getAttribute('title')).toBe('Commit Actions and close the phase (C)');
    mapCommit!.click();
    fixture.detectChanges();
    expect(compiled.querySelector('[role="alertdialog"]')?.textContent).toContain(
      'You are the last player to commit. This closes planning for all players immediately and cannot be undone.',
    );
    http.verify();
  });

  it('keeps a plain commit label and an uncommit promise while another player is drafting', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
        hasMap: false,
        isCommitted: true,
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
        commitments: [
          { userId: 'user-1', username: 'northplayer', isCommitted: true },
          { userId: 'user-2', username: 'southplayer', isCommitted: false },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Uncommit returns them to draft until this action window closes');
    expect([...compiled.querySelectorAll('button')].some((button) => button.textContent.trim() === 'Uncommit')).toBe(
      true,
    );
    expect(compiled.textContent).not.toContain('Commit Actions and close the phase');
    http.verify();
  });

  it('does not offer Uncommit after the action window has closed', async () => {
    TestBed.inject(AuthService).currentUser.set(viewerProfile('user-1'));
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
        hasMap: false,
        isCommitted: true,
        currentWindowId: null,
        myDrafts: [{ forceId: 'force-1', kind: 'Hold', targetTerritoryId: null, structureTypeId: null }],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('The phase has closed and the orders are resolving.');
    expect([...compiled.querySelectorAll('button')].some((button) => button.textContent.trim() === 'Uncommit')).toBe(
      false,
    );
    http.verify();
  });

  it('renders a hidden-relic notice and each battle reminder once', async () => {
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
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [
        {
          id: 't1',
          displayNumber: 1,
          name: 'Gretios Road',
          description: null,
          polygon: [
            { x: 0.1, y: 0.1 },
            { x: 0.4, y: 0.1 },
            { x: 0.4, y: 0.4 },
            { x: 0.1, y: 0.4 },
          ],
          terrainTypeId: null,
          structureTypeId: null,
          structureCondition: 'Operational',
          overlayColor: null,
          ownerFactionId: '1',
        },
      ],
      adjacencies: [],
    });
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(
      playState({
        hasMap: false,
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
            hiddenRelicNearby: true,
            battleReminders: ['Bring a relic hunter.'],
          },
        ],
      }),
    );
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();
    openSection(fixture, 'faction');

    const compiled = fixture.nativeElement as HTMLElement;
    const relic = 'A hidden Relic is nearby the force at Gretios Road.';
    const reminder = 'Bring a relic hunter.';
    const summaryPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Summary'),
    );
    const notice = summaryPanel?.querySelector<HTMLElement>('.relic-nearby-notice');
    expect(notice).toBeTruthy();
    expect(visibleText(notice!)).toBe(relic);
    expect(notice!.classList.contains('app-glow')).toBe(true);
    expect(notice!.style.getPropertyValue('--glow-color')).toBe('#2563EB');
    expect(notice!.querySelector('strong')?.textContent).toBe(relic);
    expect(compiled.textContent.split(relic).length - 1).toBe(1);
    expect(compiled.textContent.split(reminder).length - 1).toBe(1);
    expect(compiled.textContent).not.toContain('A hidden relic is in an adjacent territory.');
    const ordersPanel = [...compiled.querySelectorAll('.panel')].find((panel) =>
      (panel.querySelector('h2')?.textContent ?? '').includes('Actions'),
    );
    expect(ordersPanel?.textContent).not.toContain(relic);
    http.verify();
  });

  it('shows a notice when the map editor cannot be opened', async () => {
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
    flushLog(http);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as { pageNotice: { set(value: string | null): void } };
    page.pageNotice.set('The map cannot be edited after a campaign has started.');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'The map cannot be edited after a campaign has started.',
    );
    http.verify();
  });

  it('badges a delinquent player and links that badge to the log entry', async () => {
    const fixture = TestBed.createComponent(CampaignDetailPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaign.id}`).flush({
      ...campaign,
      status: 'InProgress',
      hasMap: false,
      canPlay: true,
      canChooseFaction: false,
      factionId: '1',
    });
    http.expectOne(`/api/campaigns/${campaign.id}/map/graph`).flush({
      campaignId: campaign.id,
      revision: campaign.revision,
      canManage: true,
      territories: [],
      adjacencies: [],
    });
    const delinquencyLog = [
      {
        id: 'log-delinquency',
        occurredUtc: '2026-08-14T12:00:00+00:00',
        kind: 'DelinquencyThreshold',
        originator: 'northplayer',
        summary: "northplayer's force reached three missed-order offences and may be kicked.",
        territoryId: null,
        forceId: 'force-1',
        battleId: null,
        isSystemAdjustment: false,
      },
    ];
    http.expectOne(`/api/campaigns/${campaign.id}/play`).flush(playState({ log: delinquencyLog }));
    flushLog(http, delinquencyLog);
    await fixture.whenStable();
    fixture.detectChanges();

    openSection(fixture, 'participants');
    const compiled = fixture.nativeElement as HTMLElement;
    const badge = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'May be kicked',
    );
    expect(badge).toBeTruthy();
    badge!.click();
    fixture.detectChanges();
    expect(compiled.querySelector('#log-entry-log-delinquency')).toBeTruthy();
    http.verify();
  });
});

function visibleText(element: Element): string {
  return element.textContent.replace(/\s+/g, ' ').trim();
}

function squareTerritory(id: string, name: string, x: number): MapTerritory {
  return {
    id,
    displayNumber: Number(id.slice(1)),
    name,
    description: null,
    polygon: [
      { x, y: 0.1 },
      { x: x + 0.2, y: 0.1 },
      { x: x + 0.2, y: 0.4 },
      { x, y: 0.4 },
    ],
    terrainTypeId: 'plains',
    structureTypeId: null,
    structureCondition: 'Operational',
    overlayColor: '#2563EB',
    ownerFactionId: id === 't1' ? '1' : null,
    spawnFactionId: id === 't1' ? '1' : null,
  };
}
