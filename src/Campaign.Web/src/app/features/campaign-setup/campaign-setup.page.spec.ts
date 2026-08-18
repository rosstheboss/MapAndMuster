import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';

import { FACTION_PRESETS, WARHAMMER_OLD_WORLD_PRESET_ID } from '../../core/campaigns/faction-presets';
import { HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID } from '../../core/campaigns/campaign-presets';
import { STANDARD_STRUCTURES_PRESET_ID } from '../../core/campaigns/structure-presets';
import { STANDARD_TERRAIN_PRESET_ID, TERRAIN_PRESETS } from '../../core/campaigns/terrain-presets';
import { CampaignSetupPage } from './campaign-setup.page';

function factionNames(compiled: HTMLElement): string[] {
  return [...compiled.querySelectorAll<HTMLInputElement>('input[id^="faction-name-"]')].map((input) => input.value);
}

function clickNamedButton(compiled: HTMLElement, name: string): void {
  const button = [...compiled.querySelectorAll('button')].find((item) => item.textContent.trim() === name);
  expect(button).toBeTruthy();
  button!.click();
}

function setSelectValue(element: HTMLElement | null, value: string): void {
  expect(element).toBeTruthy();
  const select = element as HTMLSelectElement;
  select.value = value;
  select.dispatchEvent(new Event('change'));
}

function setInputValue(element: HTMLElement | null, value: string): void {
  expect(element).toBeTruthy();
  const input = element as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input'));
}

function phaseKinds(compiled: HTMLElement): string[] {
  return [...compiled.querySelectorAll<HTMLSelectElement>('select[id^="phase-kind-"]')].map((select) => select.value);
}

function phaseAmounts(compiled: HTMLElement): string[] {
  return [...compiled.querySelectorAll<HTMLInputElement>('input[id^="phase-amount-"]')].map((input) => input.value);
}

describe('CampaignSetupPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignSetupPage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('requires a name and two factions before creating', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Create campaign');
    expect(compiled.querySelector('.setup-toolbar')?.textContent).toContain('Back to campaigns');
    expect(compiled.querySelector('.setup-toolbar')?.textContent).toContain('Expand All');
    expect(compiled.querySelector('.setup-toolbar')?.textContent).toContain('Collapse All');
    expect(compiled.querySelector('.setup-toolbar button.button')?.textContent).toContain('Create campaign');
    expect(compiled.querySelector('.setup-toolbar')?.textContent).not.toContain('Save as Preset');
    expect(compiled.querySelector('a[href$="/map"]')).toBeNull();
    expect(compiled.querySelector('#name')).toBeTruthy();
    expect(compiled.querySelector('#playerCount')).toBeTruthy();
    expect(compiled.querySelector('#city')).toBeTruthy();
    expect(compiled.querySelector('#country')).toBeTruthy();
    expect(compiled.querySelector('#startsAtLocal')).toBeTruthy();
    expect(compiled.querySelector('#roundCount')).toBeTruthy();
    expect(compiled.querySelector('#phase-kind-0')).toBeTruthy();
    expect(compiled.querySelector('#faction-name-0')).toBeTruthy();
    expect(compiled.querySelector('#faction-name-1')).toBeTruthy();
    const publicView = [...compiled.querySelectorAll<HTMLInputElement>('input[type="checkbox"]')].find((input) =>
      (input.closest('label')?.textContent ?? '').includes('Publicly viewable'),
    );
    expect(publicView?.checked).toBe(true);

    const page = fixture.componentInstance as unknown as { save: () => Promise<void> };
    await page.save();
    fixture.detectChanges();
    const lines = [...compiled.querySelectorAll('.error-banner p')].map((node) => node.textContent.trim());
    expect(lines).toContain('Campaign name is not filled in.');
    expect(lines).toContain('Start date and time is not filled in.');
    expect(lines).toContain('Faction 1 name is not filled in.');
    expect(lines).toContain('Faction 2 name is not filled in.');
    expect(lines).toContain('A campaign map image is required.');
    expect(compiled.textContent).toContain('up to 20 MB');
    const factionsToggle = [...compiled.querySelectorAll<HTMLButtonElement>('button.section-toggle')].find((button) =>
      button.textContent.trim().startsWith('Factions'),
    );
    expect(factionsToggle?.getAttribute('aria-expanded')).toBe('true');
    TestBed.inject(HttpTestingController).verify();
  });

  it('lists faction special rules, public, and private objectives before allies and factions', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const titles = [
      ...(fixture.nativeElement as HTMLElement).querySelectorAll('.setup-section > legend > .section-toggle'),
    ].map((button) => button.textContent.replace(/\s+/g, ' ').trim());
    const special = titles.findIndex((title) => title.startsWith('Faction special rules'));
    const publicObjectives = titles.findIndex((title) => title.startsWith('Public objectives'));
    const privateObjectives = titles.findIndex((title) => title.startsWith('Private objectives'));
    const allies = titles.findIndex((title) => title.startsWith('Ally groups'));
    const factions = titles.findIndex((title) => title.startsWith('Factions'));
    expect(special).toBeGreaterThan(-1);
    expect(special).toBeLessThan(publicObjectives);
    expect(publicObjectives).toBeLessThan(privateObjectives);
    expect(privateObjectives).toBeLessThan(allies);
    expect(allies).toBeLessThan(factions);
  });

  it('adds a pre-configured faction special rule from the autocomplete list', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      specialRulePresetPick: { setValue: (value: string) => void };
      addPickedSpecialRule: () => void;
      pickControl: (ownerId: string) => { setValue: (value: string) => void };
      assignSpecialRuleByName: (control: { value: string[] }, ownerId: string) => void;
      specialRules: {
        at: (index: number) => {
          controls: { id: { value: string }; name: { value: string }; text: { value: string } };
        };
      };
      factions: {
        at: (index: number) => {
          controls: { id: { value: string }; specialRuleIds: { value: string[] } };
        };
      };
    };

    page.specialRulePresetPick.setValue('Forced March');
    page.addPickedSpecialRule();
    fixture.detectChanges();
    expect(page.specialRules.at(0).controls.name.value).toBe('Forced March');
    expect(page.specialRules.at(0).controls.text.value).toContain('one extra adjacent territory');
    expect((fixture.nativeElement as HTMLElement).querySelector('#specialRulePreset')).toBeTruthy();
    expect((fixture.nativeElement as HTMLElement).querySelector('#faction-special-rule-0')).toBeTruthy();
    expect(
      (fixture.nativeElement as HTMLElement)
        .querySelector('label[for="special-rule-description-0"]')
        ?.textContent.trim(),
    ).toBe('Description');

    const factionId = page.factions.at(0).controls.id.value;
    page.pickControl(factionId).setValue('Forced March');
    page.assignSpecialRuleByName(page.factions.at(0).controls.specialRuleIds, factionId);
    expect(page.factions.at(0).controls.specialRuleIds.value).toEqual([page.specialRules.at(0).controls.id.value]);
  });

  it('leaves the description blank for a custom faction special rule', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      addSpecialRule: () => void;
      specialRules: {
        at: (index: number) => {
          controls: { name: { value: string }; text: { value: string } };
        };
      };
    };

    page.addSpecialRule();
    fixture.detectChanges();
    expect(page.specialRules.at(0).controls.name.value).toBe('');
    expect(page.specialRules.at(0).controls.text.value).toBe('');
    expect((fixture.nativeElement as HTMLElement).querySelector('#special-rule-description-0')).toBeTruthy();
  });

  it('defaults structure flags for catalog structures and new custom structures', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Buildable');
    expect(compiled.textContent).toContain('Pillageable');
    expect(compiled.textContent).toContain('Destructible');

    const page = fixture.componentInstance as unknown as {
      structureTypes: {
        getRawValue: () => {
          name: string;
          isBuildable: boolean;
          isPillageable: boolean;
          isDestructible: boolean;
        }[];
      };
      addStructureType: () => void;
    };
    const byName = (
      name: string,
    ): { name: string; isBuildable: boolean; isPillageable: boolean; isDestructible: boolean } | undefined =>
      page.structureTypes.getRawValue().find((type) => type.name === name);
    expect(byName('Capital City')).toMatchObject({
      isBuildable: false,
      isPillageable: false,
      isDestructible: false,
    });
    expect(byName('Castle')).toMatchObject({ isBuildable: false, isPillageable: true, isDestructible: false });
    expect(byName('City')).toMatchObject({ isBuildable: false, isPillageable: true, isDestructible: false });
    expect(byName('Town')).toMatchObject({ isBuildable: false, isPillageable: true, isDestructible: true });
    expect(byName('Supply Depot')).toMatchObject({ isBuildable: true, isPillageable: true, isDestructible: true });
    expect(byName('Fortification')).toMatchObject({ isBuildable: true, isPillageable: true, isDestructible: true });

    page.addStructureType();
    const added = page.structureTypes.getRawValue().at(-1);
    expect(added?.isBuildable).toBe(true);
    expect(added?.isPillageable).toBe(true);
    expect(added?.isDestructible).toBe(true);
    TestBed.inject(HttpTestingController).verify();
  });

  it('replaces factions from a preset copy and can clear factions and allies', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      presetId: { setValue: (value: string) => void };
      applySelectedPreset: () => void;
      factions: {
        at: (index: number) => {
          controls: { name: { setValue: (value: string) => void }; requiresSubfaction: { value: boolean } };
        };
      };
    };

    page.presetId.setValue(WARHAMMER_OLD_WORLD_PRESET_ID);
    page.applySelectedPreset();
    fixture.detectChanges();

    const names = factionNames(compiled);
    expect(names).toHaveLength(17);
    expect(names[0]).toBe('Beastmen Brayherds');
    expect(names[names.length - 1]).toBe('Wood Elf Realms');
    expect(compiled.querySelector<HTMLInputElement>('#subfaction-0-0')?.value).toBe('Minotaur Blood Herd');
    expect(compiled.querySelector<HTMLInputElement>('#subfaction-0-1')?.value).toBe('Wild Herd');
    const daemonsIndex = names.indexOf('Daemons of Chaos');
    expect(daemonsIndex).toBeGreaterThan(-1);
    expect(compiled.querySelector<HTMLInputElement>(`#subfaction-${daemonsIndex}-0`)?.value).toBe('Khorne');
    expect(compiled.querySelector<HTMLInputElement>(`#subfaction-${daemonsIndex}-1`)?.value).toBe('Nurgle');
    expect(compiled.querySelector<HTMLInputElement>(`#subfaction-${daemonsIndex}-2`)?.value).toBe('Slaanesh');
    expect(compiled.querySelector<HTMLInputElement>(`#subfaction-${daemonsIndex}-3`)?.value).toBe('Tzeentch');
    const daemonsGroup = page.factions.at(daemonsIndex) as unknown as {
      controls: { requiresSubfaction: { value: boolean } };
    };
    expect(daemonsGroup.controls.requiresSubfaction.value).toBe(true);
    expect(compiled.querySelector('#terrain-name-0')).toBeTruthy();
    expect(compiled.querySelector<HTMLInputElement>('#terrain-name-0')?.value).toBe('Beach');
    const terrainNames = [...compiled.querySelectorAll<HTMLInputElement>('input[id^="terrain-name-"]')].map(
      (input) => input.value,
    );
    expect(terrainNames).toContain('Cave');
    expect(terrainNames).toContain('Forest');
    expect(terrainNames).toContain('Jungle');
    expect(compiled.querySelector('#structure-name-0')).toBeTruthy();
    expect(compiled.querySelector('#structure-symbol-0')).toBeTruthy();
    expect(compiled.querySelector('#terrain-mission-name-0-0')).toBeTruthy();
    expect(compiled.querySelector<HTMLInputElement>('#terrain-mission-name-0-0')?.value).toBe('');
    expect(compiled.querySelector('#structure-mission-name-0-0')).toBeNull();
    expect(compiled.textContent).toContain('Color flag');
    const imageOption = [...compiled.querySelectorAll<HTMLInputElement>('input[type="radio"]')].find((input) =>
      (input.closest('label')?.textContent ?? '').includes('Uploaded image'),
    );
    expect(imageOption).toBeTruthy();
    imageOption!.click();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Maximum size 50px × 50px');

    page.factions.at(0).controls.name.setValue('Renamed Herd');
    fixture.detectChanges();
    expect(FACTION_PRESETS[0]?.factions.some((faction) => faction.name === 'Beastmen Brayherds')).toBe(true);
    expect(FACTION_PRESETS[0]?.factions.some((faction) => faction.name === 'Renamed Herd')).toBe(false);

    clickNamedButton(compiled, 'Add ally group');
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.querySelector('#ally-group-0')).toBeTruthy();

    clickNamedButton(compiled, 'Clear allies');
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.querySelector('#ally-group-0')).toBeNull();

    clickNamedButton(compiled, 'Clear factions');
    await fixture.whenStable();
    fixture.detectChanges();
    expect(factionNames(compiled)).toEqual(['', '']);

    const factionsToggle = [...compiled.querySelectorAll<HTMLButtonElement>('button.section-toggle')].find((button) =>
      button.textContent.trim().startsWith('Factions'),
    );
    factionsToggle?.click();
    fixture.detectChanges();
    expect(factionsToggle?.getAttribute('aria-expanded')).toBe('false');
    const pageSave = fixture.componentInstance as unknown as { save: () => Promise<void> };
    await pageSave.save();
    fixture.detectChanges();
    expect(factionsToggle?.getAttribute('aria-expanded')).toBe('true');
    TestBed.inject(HttpTestingController).verify();
  });

  it('replaces terrain, structures, and catalogs from presets without mutating the source', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      terrainPresetId: { setValue: (value: string) => void };
      applySelectedTerrainPreset: () => void;
      structurePresetId: { setValue: (value: string) => void };
      applySelectedStructurePreset: () => void;
      campaignPresetId: { setValue: (value: string) => void };
      applySelectedCampaignPreset: () => void;
      terrainTypes: { at: (index: number) => { controls: { name: { setValue: (value: string) => void } } } };
    };

    compiled.querySelector<HTMLInputElement>('#terrain-name-0')!.value = 'Renamed terrain';
    compiled.querySelector<HTMLInputElement>('#terrain-name-0')!.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    page.terrainPresetId.setValue(STANDARD_TERRAIN_PRESET_ID);
    page.applySelectedTerrainPreset();
    fixture.detectChanges();
    expect(compiled.querySelector<HTMLInputElement>('#terrain-name-0')?.value).toBe('Beach');
    expect(TERRAIN_PRESETS[0]?.terrainTypes[0]?.name).toBe('Beach');
    expect(
      (
        fixture.componentInstance as unknown as {
          terrainTypes: { at: (index: number) => { controls: { isWaterFeature: { value: boolean } } } };
        }
      ).terrainTypes.at(0).controls.isWaterFeature.value,
    ).toBe(true);

    page.structurePresetId.setValue(STANDARD_STRUCTURES_PRESET_ID);
    page.applySelectedStructurePreset();
    fixture.detectChanges();
    expect(compiled.querySelector<HTMLInputElement>('#structure-name-0')?.value).toBe('Capital City');

    page.campaignPresetId.setValue(HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID);
    page.applySelectedCampaignPreset();
    fixture.detectChanges();
    expect(compiled.querySelector<HTMLInputElement>('#name')?.value).toBe('The Hunt in Estalia');
    expect(factionNames(compiled)[0]).toBe('Beastmen Brayherds');
    expect(compiled.querySelector('#item-objective-name-0')).toBeNull();
    expect(compiled.querySelector<HTMLInputElement>('#special-rule-name-0')?.value).toBe('Forced March');
    expect(compiled.querySelector<HTMLTextAreaElement>('#special-rule-description-0')?.value).toContain(
      'one extra adjacent territory',
    );
    expect(compiled.querySelector<HTMLInputElement>('#force-status-name-0')?.value).toBe('Diseased');
    expect(compiled.querySelector('#forceStatusPreset')).toBeTruthy();
    expect(compiled.querySelector('#specialRulePreset')).toBeTruthy();
    expect(
      (
        fixture.componentInstance as unknown as {
          factions: { at: (index: number) => { controls: { specialRuleIds: { value: string[] } } } };
        }
      ).factions.at(0).controls.specialRuleIds.value.length,
    ).toBeGreaterThan(0);

    clickNamedButton(compiled, 'Add item objective');
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.querySelector('#item-objective-name-0')).toBeTruthy();
    expect(compiled.querySelector<HTMLSelectElement>('#item-objective-placement-0')?.value).toBe('Random');
    const itemPage = fixture.componentInstance as unknown as {
      itemObjectiveTypes: {
        at: (index: number) => {
          controls: { isHiddenUntilFound: { value: boolean }; allowOnSpawn: { value: boolean } };
        };
      };
    };
    expect(itemPage.itemObjectiveTypes.at(0).controls.isHiddenUntilFound.value).toBe(true);
    expect(itemPage.itemObjectiveTypes.at(0).controls.allowOnSpawn.value).toBe(false);
    expect(compiled.querySelector('#item-objective-points-0')).toBeTruthy();
    expect(compiled.querySelector('#item-objective-symbol-0')).toBeTruthy();
    expect(compiled.querySelector('#item-objective-flavor-0')).toBeTruthy();
    expect(compiled.querySelector('#pointsPerBattleWon')).toBeTruthy();
    expect(compiled.querySelector('#pointsPerBattleDraw')).toBeTruthy();
    expect(compiled.querySelector('#mostTerritoriesCampaignPoints')).toBeTruthy();
    expect(compiled.querySelector<HTMLInputElement>('#splitForceSupplyPenaltyPercent')?.value).toBe('25');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-0')?.value).toBe('1000');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-supply-2')?.value).toBe('1');
    expect(compiled.querySelector<HTMLInputElement>('#terrain-supply-0')?.value).toBe('1');
    expect(compiled.querySelector('#structure-supply-0')).toBeTruthy();
    clickNamedButton(compiled, 'Add public objective');
    await fixture.whenStable();
    fixture.detectChanges();
    expect(compiled.querySelector('#public-objective-name-0')).toBeTruthy();
    expect(compiled.querySelector('#public-objective-points-0')).toBeTruthy();
    clickNamedButton(compiled, 'Add ally group');
    fixture.detectChanges();
    expect(compiled.querySelector('#ally-group-color-0')).toBeTruthy();
    TestBed.inject(HttpTestingController).verify();
  });

  it('rejects campaign maps larger than 20 MB before upload', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      onMapSelected: (event: Event) => void;
    };
    const input = { files: [{ name: 'huge.png', size: 20 * 1024 * 1024 + 1 }], value: 'huge.png' };
    page.onMapSelected({ target: input } as unknown as Event);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect([...compiled.querySelectorAll('.error-banner p')].map((node) => node.textContent.trim())).toContain(
      'Campaign maps must be 20 MB or smaller.',
    );
    expect(input.value).toBe('');
    TestBed.inject(HttpTestingController).verify();
  });

  it('requires a state when a city is provided', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      form: { controls: { city: { setValue: (value: string) => void } } };
      save: () => Promise<void>;
    };
    page.form.controls.city.setValue('Halifax');
    await page.save();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const lines = [...compiled.querySelectorAll('.error-banner p')].map((node) => node.textContent.trim());
    expect(lines).toContain('State or province is required when a city is provided.');
    TestBed.inject(HttpTestingController).verify();
  });

  it('expands and collapses every setup section from the toolbar', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const detailsToggle = [...compiled.querySelectorAll<HTMLButtonElement>('button.section-toggle')].find((button) =>
      button.textContent.includes('Campaign details'),
    );
    expect(detailsToggle?.getAttribute('aria-expanded')).toBe('true');

    clickNamedButton(compiled, 'Collapse All');
    fixture.detectChanges();
    expect(detailsToggle?.getAttribute('aria-expanded')).toBe('false');
    expect(compiled.querySelector('#name')?.closest('[hidden]')).toBeTruthy();

    clickNamedButton(compiled, 'Expand All');
    fixture.detectChanges();
    expect(detailsToggle?.getAttribute('aria-expanded')).toBe('true');
    expect(compiled.querySelector('#name')?.closest('[hidden]')).toBeNull();
    expect(compiled.textContent).toContain('Pillaged icon');
    TestBed.inject(HttpTestingController).verify();
  });
});

describe('CampaignSetupPage edit', () => {
  const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampaignSetupPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => campaignId } } },
        },
      ],
    }).compileComponents();
  });

  it('keeps edit-map and save actions in a sticky toolbar without saving', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush({
      id: campaignId,
      name: 'Border War',
      description: 'A contested frontier.',
      playerSlotCount: 8,
      occupiedPlayerSlots: 1,
      isPrivate: false,
      isPubliclyViewable: true,
      creatorIsParticipant: true,
      city: null,
      region: null,
      country: null,
      hasMap: true,
      canManage: true,
      isParticipant: true,
      revision: 2,
      createdUtc: '2026-08-13T00:00:00+00:00',
      updatedUtc: '2026-08-13T00:00:00+00:00',
      factions: [
        {
          id: '1',
          name: 'North',
          color: '#2563EB',
          subfactions: [],
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
      links: [],
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
      canChooseFaction: false,
      canChat: true,
      mentionableMembers: [],
      log: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const toolbar = compiled.querySelector<HTMLElement>('.setup-toolbar');
    expect(toolbar).toBeTruthy();
    expect(compiled.querySelector('h1')?.textContent).toContain('Edit campaign');
    expect(toolbar?.textContent).toContain('Back to campaigns');
    expect(toolbar?.querySelector(`a[href="/campaigns/${campaignId}/map"]`)?.textContent).toContain('Edit map');
    expect(toolbar?.textContent).toContain('Expand All');
    expect(toolbar?.textContent).toContain('Collapse All');
    expect(toolbar?.querySelector('button.button')?.textContent).toContain('Save campaign');
    expect(toolbar ? getComputedStyle(toolbar).position : '').toBe('sticky');
    expect(compiled.querySelector('app-campaign-map-preview img')?.getAttribute('src')).toContain(
      `/api/campaigns/${campaignId}/map?v=2`,
    );
    http.verify();
  });

  it('keeps phase kinds, lengths, and order in sync after add, remove, move, and kind changes', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`/api/campaigns/${campaignId}`).flush({
      id: campaignId,
      name: 'Border War',
      description: 'A contested frontier.',
      playerSlotCount: 8,
      occupiedPlayerSlots: 1,
      isPrivate: false,
      isPubliclyViewable: true,
      creatorIsParticipant: true,
      city: null,
      region: null,
      country: null,
      hasMap: true,
      canManage: true,
      isParticipant: true,
      revision: 2,
      createdUtc: '2026-08-13T00:00:00+00:00',
      updatedUtc: '2026-08-13T00:00:00+00:00',
      factions: [
        {
          id: '1',
          name: 'North',
          color: '#2563EB',
          subfactions: [],
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
      links: [],
      terrainTypes: [],
      structureTypes: [],
      timeZoneId: 'UTC',
      startsAtLocal: '2099-01-05T12:00',
      startsUtc: '2099-01-05T12:00:00+00:00',
      endsUtc: '2099-03-02T12:00:00+00:00',
      roundCount: 8,
      roundLengthAmount: 1,
      roundLengthUnit: 'Hours',
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
      canChooseFaction: false,
      canChat: true,
      mentionableMembers: [],
      log: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      form: { getRawValue: () => { phases: { kind: string; durationAmount: number; durationUnit: string }[] } };
    };

    clickNamedButton(compiled, 'Add action');
    fixture.detectChanges();
    const firstRemove = compiled.querySelector('#phase-kind-0')?.closest('.nested-card')?.querySelectorAll('button');
    const remove = [...(firstRemove ?? [])].find((button) => button.textContent.trim() === 'Remove');
    expect(remove).toBeTruthy();
    remove!.click();
    fixture.detectChanges();
    clickNamedButton(compiled, 'Add action');
    fixture.detectChanges();

    setSelectValue(compiled.querySelector('#phase-kind-1'), 'Action');
    fixture.detectChanges();
    setSelectValue(compiled.querySelector('#phase-kind-2'), 'Battle');
    fixture.detectChanges();

    setSelectValue(compiled.querySelector('#roundLengthUnit'), 'Hours');
    fixture.detectChanges();
    for (const index of [0, 1, 2, 3]) {
      setSelectValue(compiled.querySelector(`#phase-unit-${index}`), 'Minutes');
      fixture.detectChanges();
    }

    setInputValue(compiled.querySelector('#phase-amount-0'), '2');
    setInputValue(compiled.querySelector('#phase-amount-1'), '2');
    setInputValue(compiled.querySelector('#phase-amount-2'), '54');
    setInputValue(compiled.querySelector('#phase-amount-3'), '2');
    fixture.detectChanges();

    const battleRowButtons = compiled
      .querySelector('#phase-kind-2')
      ?.closest('.nested-card')
      ?.querySelectorAll('button');
    const moveDown = [...(battleRowButtons ?? [])].find((button) => button.textContent.trim() === 'Move down');
    expect(moveDown).toBeTruthy();
    moveDown!.click();
    fixture.detectChanges();

    expect(phaseKinds(compiled)).toEqual(['Action', 'Action', 'Action', 'Battle']);
    expect(phaseAmounts(compiled)).toEqual(['2', '2', '2', '54']);
    expect(page.form.getRawValue().phases).toEqual([
      { kind: 'Action', durationAmount: 2, durationUnit: 'Minutes' },
      { kind: 'Action', durationAmount: 2, durationUnit: 'Minutes' },
      { kind: 'Action', durationAmount: 2, durationUnit: 'Minutes' },
      { kind: 'Battle', durationAmount: 54, durationUnit: 'Minutes' },
    ]);
    http.verify();
  });
});
