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
};

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
    expect(compiled.querySelector('a.button')?.textContent).toContain('Edit campaign');
    expect(compiled.textContent).toContain('Edit map');

    const deleteButton = [...compiled.querySelectorAll('button')].find((button) =>
      button.textContent.includes('Delete campaign'),
    );
    expect(deleteButton).toBeTruthy();
    deleteButton!.click();
    fixture.detectChanges();
    expect(compiled.querySelector('[role="alertdialog"]')?.textContent).toContain('Delete this campaign?');
    http.verify();
  });
});
