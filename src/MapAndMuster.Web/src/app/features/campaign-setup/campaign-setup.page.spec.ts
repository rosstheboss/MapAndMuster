import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import type { OwnProfile } from '../../core/auth/auth.models';
import { FACTION_PRESETS, WARHAMMER_OLD_WORLD_PRESET_ID } from '../../core/campaigns/faction-presets';
import { HUNT_IN_ESTALIA_CAMPAIGN_PRESET_ID } from '../../core/campaigns/campaign-presets';
import { STANDARD_STRUCTURES_PRESET_ID } from '../../core/campaigns/structure-presets';
import { STANDARD_TERRAIN_PRESET_ID, TERRAIN_PRESETS } from '../../core/campaigns/terrain-presets';
import type { CampaignDetail } from '../../core/campaigns/campaign.models';
import { CampaignSetupPage } from './campaign-setup.page';

function factionNames(compiled: HTMLElement): string[] {
  return [...compiled.querySelectorAll<HTMLInputElement>('input[id^="faction-name-"]')].map((input) => input.value);
}

function clickNamedButton(compiled: HTMLElement, name: string): void {
  const button = [...compiled.querySelectorAll('button')].find((item) => item.textContent.trim() === name);
  expect(button).toBeTruthy();
  button!.click();
}

function stubBlobDownload(): void {
  Object.defineProperty(URL, 'createObjectURL', {
    configurable: true,
    writable: true,
    value: () => 'blob:preset',
  });
  Object.defineProperty(URL, 'revokeObjectURL', {
    configurable: true,
    writable: true,
    value: () => undefined,
  });
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
    expect(compiled.querySelector('#add-catalog-mission')).toBeTruthy();
    expect(compiled.querySelector('#name')).toBeTruthy();
    expect(compiled.querySelector('label[for="playerCount"]')?.textContent).toContain('Max Number of Players');
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
    expect(compiled.textContent).toContain('A campaign map image is required.');
    expect(compiled.textContent).toContain('up to 20 MB');
    const factionsToggle = [...compiled.querySelectorAll<HTMLButtonElement>('button.section-toggle')].find((button) =>
      button.textContent.trim().startsWith('Factions'),
    );
    expect(factionsToggle?.getAttribute('aria-expanded')).toBe('true');
    const visibilityToggle = [...compiled.querySelectorAll<HTMLButtonElement>('button.section-toggle')].find((button) =>
      button.textContent.trim().startsWith('Visibility'),
    );
    expect(visibilityToggle?.getAttribute('aria-expanded')).toBe('false');
    expect(compiled.querySelector('.setup-index')).toBeTruthy();
    expect(compiled.querySelector('.setup-index')?.textContent).toContain('Campaign details');
    expect(compiled.textContent).toMatch(/required sections remaining/);
    expect(
      [...compiled.querySelectorAll('button.section-toggle')].some((button) =>
        button.textContent.trim().startsWith('Missions for '),
      ),
    ).toBe(true);
    TestBed.inject(HttpTestingController).verify();
  });

  it('lets an administrator look up The Hunt in Estalia when saving a preset', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    TestBed.inject(AuthService).currentUser.set(administratorProfile());
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.setup-toolbar')?.textContent).toContain('Save as Preset');
    expect(compiled.querySelector('.setup-toolbar')?.textContent).toContain('Upload Preset');
    expect(compiled.querySelector('.setup-toolbar')?.textContent).not.toContain('Download Preset');
    expect(compiled.querySelector('input.sr-only[type="file"]')?.getAttribute('aria-label')).toBe(
      'Upload campaign preset',
    );
    expect(compiled.querySelector('h3')).toBeNull();
    expect(compiled.querySelector('h4')).toBeNull();
    expect([...compiled.querySelectorAll('.subsection-heading')].map((node) => node.textContent.trim())).toEqual(
      expect.arrayContaining([
        'Ranking public objectives',
        'Running public objectives',
        'Battle campaign points',
        'Supply and battle reports',
      ]),
    );
    clickNamedButton(compiled, 'Save as Preset');
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/campaign-presets').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    const input = compiled.querySelector<HTMLInputElement>('#savePresetName')!;
    expect(input).toBeTruthy();
    input.dispatchEvent(new Event('focus'));
    input.value = 'hunt';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const options = [...compiled.querySelectorAll('[role="option"]')].map((item) => item.textContent.trim());
    expect(options).toEqual(['The Hunt in Estalia']);
    http.verify();
  });

  it('lists a saved Hunt preset once in the apply dropdown', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const select = compiled.querySelector<HTMLSelectElement>('#campaignPreset')!;
    select.dispatchEvent(new Event('focus'));
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('/api/campaign-presets')
      .flush([{ id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: ' The Hunt in Estalia ', hasMap: true }]);
    await fixture.whenStable();
    fixture.detectChanges();

    const huntOptions = [...select.options].filter(
      (option) => option.textContent.trim().toLowerCase() === 'the hunt in estalia',
    );
    expect(huntOptions).toHaveLength(1);
    expect(huntOptions[0]?.value).toBe('saved:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
    http.verify();
  });

  it('shows one army-size row per round with generic defaults', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector<HTMLInputElement>('#roundCount')?.value).toBe('8');
    expect(compiled.querySelector<HTMLInputElement>('#roundLengthAmount')?.value).toBe('1');
    expect(compiled.querySelector<HTMLSelectElement>('#roundLengthUnit')?.value).toBe('Weeks');
    expect(compiled.textContent).not.toContain('Between 3 and 52.');
    expect(compiled.textContent).not.toContain('Between 10 and 100000.');
    expect(compiled.textContent).toContain('Each round can raise the army points cap');
    expect(compiled.textContent).toContain(
      'Changing the number of rounds keeps values you already entered for overlapping rounds.',
    );
    expect(compiled.textContent).not.toContain('Defaults follow The Hunt in Estalia.');
    const armyToggle = [...compiled.querySelectorAll<HTMLButtonElement>('button.section-toggle')].find((button) =>
      button.textContent.trim().startsWith('Round army size and free supply'),
    );
    expect(armyToggle?.getAttribute('aria-expanded')).toBe('false');
    armyToggle?.click();
    fixture.detectChanges();
    expect(armyToggle?.getAttribute('aria-expanded')).toBe('true');
    expect(compiled.querySelector('#round-escalation-points-7')).toBeTruthy();
    expect(compiled.querySelector('#round-escalation-points-8')).toBeNull();
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-0')?.value).toBe('1000');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-supply-0')?.value).toBe('1');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-characters-0')?.value).toBe('1');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-7')?.value).toBe('1000');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-supply-7')?.value).toBe('1');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-characters-7')?.value).toBe('1');
    TestBed.inject(HttpTestingController).verify();
  });

  it('keeps overlapping army-size values when the round count changes', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      form: { controls: { roundCount: { setValue: (value: number) => void } } };
      roundEscalations: {
        length: number;
        at: (index: number) => {
          controls: {
            maxArmyPoints: { value: number; setValue: (value: number) => void };
            freeSupplyPoints: { value: number };
            freeCharacterCount: { value: number };
          };
        };
      };
    };
    page.roundEscalations.at(0).controls.maxArmyPoints.setValue(777);
    page.form.controls.roundCount.setValue(4);
    fixture.detectChanges();
    expect(page.roundEscalations.length).toBe(4);
    expect(page.roundEscalations.at(0).controls.maxArmyPoints.value).toBe(777);

    page.form.controls.roundCount.setValue(8);
    fixture.detectChanges();
    expect(page.roundEscalations.length).toBe(8);
    expect(page.roundEscalations.at(0).controls.maxArmyPoints.value).toBe(777);
    expect(page.roundEscalations.at(7).controls.maxArmyPoints.value).toBe(1000);
    expect(page.roundEscalations.at(7).controls.freeSupplyPoints.value).toBe(1);
    expect(page.roundEscalations.at(7).controls.freeCharacterCount.value).toBe(1);
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

    page.specialRulePresetPick.setValue('Crusaders');
    page.addPickedSpecialRule();
    fixture.detectChanges();
    expect(page.specialRules.at(0).controls.name.value).toBe('Crusaders');
    expect(page.specialRules.at(0).controls.text.value).toContain('two adjacent territories');
    expect((fixture.nativeElement as HTMLElement).querySelector('#specialRulePreset')).toBeTruthy();
    expect((fixture.nativeElement as HTMLElement).querySelector('#faction-special-rule-0')).toBeTruthy();
    expect(
      (fixture.nativeElement as HTMLElement)
        .querySelector('label[for="special-rule-description-0"]')
        ?.textContent.trim(),
    ).toBe('Description');

    const factionId = page.factions.at(0).controls.id.value;
    page.pickControl(factionId).setValue('Crusaders');
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
    expect(names).toHaveLength(18);
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
    expect(compiled.querySelector<HTMLInputElement>(`#subfaction-color-${daemonsIndex}-0`)?.value.toUpperCase()).toBe(
      '#B91C1C',
    );
    expect(compiled.querySelector<HTMLInputElement>(`#subfaction-color-${daemonsIndex}-1`)?.value.toUpperCase()).toBe(
      '#3F6212',
    );
    expect(compiled.querySelector<HTMLInputElement>(`#subfaction-color-${daemonsIndex}-2`)?.value.toUpperCase()).toBe(
      '#F472B6',
    );
    expect(compiled.querySelector<HTMLInputElement>(`#subfaction-color-${daemonsIndex}-3`)?.value.toUpperCase()).toBe(
      '#0E7490',
    );
    const daemonsGroup = page.factions.at(daemonsIndex) as unknown as {
      controls: {
        requiresSubfaction: { value: boolean };
        subfactionSpecialRuleIds: { value: Record<string, string[]> };
      };
    };
    expect(daemonsGroup.controls.requiresSubfaction.value).toBe(true);
    expect(daemonsGroup.controls.subfactionSpecialRuleIds.value['Khorne'].length).toBeGreaterThan(0);
    expect(daemonsGroup.controls.subfactionSpecialRuleIds.value['Nurgle'].length).toBeGreaterThan(0);
    expect(daemonsGroup.controls.subfactionSpecialRuleIds.value['Slaanesh'].length).toBeGreaterThan(0);
    expect(daemonsGroup.controls.subfactionSpecialRuleIds.value['Tzeentch'].length).toBeGreaterThan(0);
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
    const imageOption = [...compiled.querySelectorAll<HTMLInputElement>('input[type="radio"]')].find(
      (input) =>
        input.name.startsWith('faction-flag-') &&
        (input.closest('label')?.textContent ?? '').includes('Uploaded image'),
    );
    expect(imageOption).toBeTruthy();
    imageOption!.click();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('Maximum size 50px × 50px');
    expect(compiled.textContent).toContain('Tint logo with faction color');
    expect(compiled.textContent).not.toContain('Uploaded flags are not recolored');

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

  it('shows subfaction color and flag controls when a faction is expanded after collapse all', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      presetId: { setValue: (value: string) => void };
      applySelectedPreset: () => void;
      collapseAllSections: () => void;
    };

    page.presetId.setValue(WARHAMMER_OLD_WORLD_PRESET_ID);
    page.applySelectedPreset();
    fixture.detectChanges();
    page.collapseAllSections();
    fixture.detectChanges();

    const names = factionNames(compiled);
    const daemonsIndex = names.indexOf('Daemons of Chaos');
    expect(daemonsIndex).toBeGreaterThan(-1);

    const factionsToggle = [...compiled.querySelectorAll<HTMLButtonElement>('button.section-toggle')].find((button) =>
      button.textContent.trim().startsWith('Factions'),
    );
    factionsToggle?.click();
    fixture.detectChanges();
    const daemonsToggle = [...compiled.querySelectorAll<HTMLButtonElement>('button.section-toggle')].find((button) =>
      button.textContent.includes('Daemons of Chaos'),
    );
    daemonsToggle?.click();
    fixture.detectChanges();

    const khorneColor = compiled.querySelector<HTMLInputElement>(`#subfaction-color-${daemonsIndex}-0`);
    expect(khorneColor).toBeTruthy();
    expect(khorneColor!.closest('[hidden]')).toBeNull();
    expect(khorneColor!.value.toUpperCase()).toBe('#B91C1C');
    const khorneFlagRadios = [
      ...compiled.querySelectorAll<HTMLInputElement>(`input[name="subfaction-flag-${daemonsIndex}-0"]`),
    ];
    expect(khorneFlagRadios.map((input) => input.closest('label')?.textContent?.trim())).toEqual([
      'Color flag',
      'Uploaded image',
    ]);
    expect(compiled.textContent).toContain('Inherit faction color');

    const upload = khorneFlagRadios.find((input) =>
      (input.closest('label')?.textContent ?? '').includes('Uploaded image'),
    );
    upload?.click();
    fixture.detectChanges();
    expect(compiled.querySelector(`#subfaction-flag-image-${daemonsIndex}-0`)).toBeTruthy();
    expect(compiled.textContent).toContain('Tint logo with subfaction color');
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
    expect(compiled.querySelector<HTMLInputElement>('#special-rule-name-0')?.value).toBe('Expert Ambushers');
    expect(compiled.querySelector<HTMLTextAreaElement>('#special-rule-description-0')?.value).toContain(
      'Ambushing rolls',
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
    expect(compiled.querySelector('#mostStructurePointsCampaignPoints')).toBeTruthy();
    expect(compiled.querySelector('#pointsPerTerritoryCampaignPoints')).toBeTruthy();
    expect(compiled.querySelector('#alliedRelicControlCampaignPoints')).toBeTruthy();
    expect(compiled.querySelector<HTMLInputElement>('#splitForceSupplyPenaltyPercent')?.value).toBe('1');
    expect(
      compiled.querySelector<HTMLInputElement>('input[formControlName="splitForceSupplyPenaltyIsPercent"]')?.checked,
    ).toBe(false);
    expect(compiled.querySelector('#round-escalation-points-7')).toBeTruthy();
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-0')?.value).toBe('500');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-0')?.min).toBe('10');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-0')?.max).toBe('100000');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-supply-0')?.value).toBe('1');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-characters-0')?.value).toBe('1');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-6')?.value).toBe('2500');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-supply-6')?.value).toBe('3');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-characters-6')?.value).toBe('2');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-7')?.value).toBe('3000');
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

  it('keeps factions in an ally group after the group is renamed', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      addAllyGroup: () => void;
      allyMembers: (groupId: string) => string;
      allyGroups: {
        at: (index: number) => {
          controls: { id: { value: string }; name: { setValue: (value: string) => void } };
        };
      };
      factions: {
        at: (index: number) => {
          controls: {
            name: { setValue: (value: string) => void };
            allyGroupId: { setValue: (value: string) => void; value: string };
          };
        };
      };
    };

    page.addAllyGroup();
    const groupId = page.allyGroups.at(0).controls.id.value;
    page.allyGroups.at(0).controls.name.setValue('Pact');
    page.factions.at(0).controls.name.setValue('North');
    page.factions.at(0).controls.allyGroupId.setValue(groupId);
    fixture.detectChanges();

    page.allyGroups.at(0).controls.name.setValue('Northern League');
    fixture.detectChanges();

    expect(page.factions.at(0).controls.allyGroupId.value).toBe(groupId);
    expect(page.allyMembers(groupId)).toContain('North');
    TestBed.inject(HttpTestingController).verify();
  });

  it('assigns a faction to an ally group from the group dropdown and updates the faction field', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      addAllyGroup: () => void;
      allyMembers: (groupId: string) => string;
      onAllyGroupFactionPicked: (groupId: string, event: Event) => void;
      allyGroups: {
        at: (index: number) => {
          controls: { id: { value: string }; name: { setValue: (value: string) => void } };
        };
      };
      factions: {
        at: (index: number) => {
          controls: {
            id: { value: string };
            name: { setValue: (value: string) => void };
            allyGroupId: { value: string };
          };
        };
      };
    };

    page.addAllyGroup();
    const groupId = page.allyGroups.at(0).controls.id.value;
    page.allyGroups.at(0).controls.name.setValue('Pact');
    page.factions.at(0).controls.name.setValue('North');
    page.factions.at(1).controls.name.setValue('South');
    fixture.detectChanges();

    const select = { value: page.factions.at(0).controls.id.value };
    page.onAllyGroupFactionPicked(groupId, { target: select } as unknown as Event);
    fixture.detectChanges();

    expect(page.factions.at(0).controls.allyGroupId.value).toBe(groupId);
    expect(page.allyMembers(groupId)).toContain('North');
    expect(select.value).toBe('');
    const compiled = fixture.nativeElement as HTMLElement;
    const factionSelect = compiled.querySelector<HTMLSelectElement>('#faction-ally-0');
    expect(factionSelect?.value).toBe(groupId);
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

  it('rejects round army points outside 10 to 100000', async () => {
    HTMLElement.prototype.scrollIntoView = () => undefined;
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      roundEscalations: {
        at: (index: number) => { controls: { maxArmyPoints: { setValue: (value: number) => void } } };
      };
      save: () => Promise<void>;
    };
    page.roundEscalations.at(0).controls.maxArmyPoints.setValue(9);
    await page.save();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect([...compiled.querySelectorAll('.error-banner p')].map((node) => node.textContent.trim())).toContain(
      'Round 1 max army points must be at least 10.',
    );

    page.roundEscalations.at(0).controls.maxArmyPoints.setValue(100001);
    await page.save();
    fixture.detectChanges();
    expect([...compiled.querySelectorAll('.error-banner p')].map((node) => node.textContent.trim())).toContain(
      'Round 1 max army points must be at most 100000.',
    );
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

  it('adds catalog attacker/defender missions and keeps terrain pickers as name lists', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const addCatalog = compiled.querySelector<HTMLButtonElement>('#add-catalog-mission');
    expect(addCatalog).toBeTruthy();
    addCatalog!.click();
    fixture.detectChanges();

    expect(compiled.querySelector('#catalog-mission-name-0')).toBeTruthy();
    const attackerDefender = compiled.querySelector<HTMLInputElement>('#catalog-mission-ad-0');
    expect(attackerDefender).toBeTruthy();
    attackerDefender!.click();
    fixture.detectChanges();

    const armyAdvantage = compiled.querySelector<HTMLInputElement>('#catalog-mission-ap-adv-0');
    expect(armyAdvantage).toBeTruthy();
    armyAdvantage!.click();
    fixture.detectChanges();

    const supplyAdvantage = compiled.querySelector<HTMLInputElement>('#catalog-mission-sp-adv-0');
    expect(supplyAdvantage).toBeTruthy();
    supplyAdvantage!.click();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      missions: {
        getRawValue: () => { name: string; isAttackerDefender: boolean }[];
        at: (index: number) => {
          controls: {
            name: { setValue: (value: string) => void };
          };
        };
      };
      catalogMissionNames: () => string[];
    };
    page.missions.at(0).controls.name.setValue('Meeting engagement');
    fixture.detectChanges();
    expect(compiled.querySelector('#catalog-mission-ap-side-0')).toBeTruthy();
    expect(compiled.querySelector('#catalog-mission-sp-side-0')).toBeTruthy();
    expect(compiled.querySelector('#catalog-mission-ap-amount-0')).toBeTruthy();
    expect(compiled.querySelector('#terrain-mission-url-0-0')).toBeNull();
    expect(compiled.querySelector('#terrain-question-prompt-0-0-0')).toBeNull();
    expect(compiled.textContent).not.toContain('Use uploaded mission');
    expect(page.missions.getRawValue()[0]).toMatchObject({
      name: 'Meeting engagement',
      isAttackerDefender: true,
    });
    expect(page.catalogMissionNames()).toContain('Meeting engagement');
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
    TestBed.inject(AuthService).currentUser.set(administratorProfile());
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
    expect(toolbar?.textContent).toContain('Save as Preset');
    expect(toolbar?.textContent).toContain('Download Preset');
    expect(toolbar?.textContent).toContain('Upload Preset');
    expect(toolbar?.textContent).toContain('Clear Unsaved Changes');
    expect(toolbar?.textContent).toContain('End campaign');
    const save = [...(toolbar?.querySelectorAll('button') ?? [])].find(
      (button) => button.textContent.trim() === 'Save campaign',
    );
    const discard = [...(toolbar?.querySelectorAll('button') ?? [])].find(
      (button) => button.textContent.trim() === 'Clear Unsaved Changes',
    );
    expect(save?.disabled).toBe(true);
    expect(discard?.disabled).toBe(true);
    expect(toolbar ? getComputedStyle(toolbar).position : '').toBe('sticky');
    expect(compiled.querySelector('app-campaign-map-preview img')?.getAttribute('src')).toContain(
      `/api/campaigns/${campaignId}/map?v=2`,
    );
    http.verify();
  });

  it('ends the campaign from Edit campaign and returns to Your Campaigns', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(scheduledEditCampaign(campaignId));
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    clickNamedButton(compiled, 'End campaign');
    fixture.detectChanges();
    expect(compiled.querySelector('[role="alertdialog"]')?.textContent).toContain('End this campaign?');
    const confirm = [...compiled.querySelectorAll<HTMLButtonElement>('[role="alertdialog"] button')].find(
      (element) => element.textContent.trim() === 'End campaign',
    );
    expect(confirm).toBeTruthy();
    confirm!.click();
    const end = http.expectOne(`/api/campaigns/${campaignId}/end`);
    expect(end.request.method).toBe('POST');
    expect(end.request.body).toEqual({ revision: 2 });
    end.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    expect(navigate).toHaveBeenCalledWith('/campaigns');
    fixture.detectChanges();
    expect(document.querySelector('[role="alertdialog"]')).toBeNull();
    fixture.destroy();
    expect(document.querySelector('.app-dialog-backdrop')).toBeNull();
    http.verify();
  });

  it('shows the logo tint toggle for a faction that uses an uploaded image', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    const http = TestBed.inject(HttpTestingController);
    const campaign = scheduledEditCampaign(campaignId);
    http.expectOne(`/api/campaigns/${campaignId}`).flush({
      ...campaign,
      factions: [{ ...campaign.factions[0], hasFlagImage: true, tintFlagImage: true }, campaign.factions[1]],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const tint = [...compiled.querySelectorAll<HTMLInputElement>('input[type="checkbox"]')].find((input) =>
      (input.closest('label')?.textContent ?? '').includes('Tint logo with faction color'),
    );
    expect(tint).toBeTruthy();
    expect(tint?.checked).toBe(true);
    expect(compiled.querySelector('app-faction-logo .is-tinted')).toBeTruthy();
    http.verify();
  });

  it('saves logo tint when the toggle is checked or cleared', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    const http = TestBed.inject(HttpTestingController);
    const campaign = scheduledEditCampaign(campaignId);
    const withFlag = {
      ...campaign,
      factions: [{ ...campaign.factions[0], hasFlagImage: true, tintFlagImage: false }, campaign.factions[1]],
    };
    http.expectOne(`/api/campaigns/${campaignId}`).flush(withFlag);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as { save: () => Promise<void> };
    const tintCheckbox = (): HTMLInputElement | undefined =>
      [...compiled.querySelectorAll<HTMLInputElement>('input[type="checkbox"]')].find((input) =>
        (input.closest('label')?.textContent ?? '').includes('Tint logo with faction color'),
      );

    const tint = tintCheckbox();
    expect(tint).toBeTruthy();
    expect(tint?.checked).toBe(false);
    tint!.click();
    fixture.detectChanges();
    expect(tintCheckbox()?.checked).toBe(true);
    expect(compiled.querySelector('app-faction-logo .is-tinted')).toBeTruthy();

    const enabling = page.save();
    const enablePut = http.expectOne(`/api/campaigns/${campaignId}`);
    expect(enablePut.request.method).toBe('PUT');
    expect((enablePut.request.body as { factions: { tintFlagImage?: boolean }[] }).factions[0].tintFlagImage).toBe(
      true,
    );
    const enabled = {
      ...withFlag,
      revision: 3,
      factions: [{ ...withFlag.factions[0], hasFlagImage: true, tintFlagImage: true }, withFlag.factions[1]],
    };
    enablePut.flush(enabled);
    await enabling;
    fixture.detectChanges();
    expect(tintCheckbox()?.checked).toBe(true);
    expect(compiled.querySelector('app-faction-logo .is-tinted')).toBeTruthy();

    tintCheckbox()!.click();
    fixture.detectChanges();
    expect(tintCheckbox()?.checked).toBe(false);
    expect(compiled.querySelector('app-faction-logo .is-tinted')).toBeNull();

    const disabling = page.save();
    const disablePut = http.expectOne(`/api/campaigns/${campaignId}`);
    expect((disablePut.request.body as { factions: { tintFlagImage?: boolean }[] }).factions[0].tintFlagImage).toBe(
      false,
    );
    disablePut.flush({
      ...enabled,
      revision: 4,
      factions: [{ ...enabled.factions[0], hasFlagImage: true, tintFlagImage: false }, enabled.factions[1]],
    });
    await disabling;
    fixture.detectChanges();
    expect(tintCheckbox()?.checked).toBe(false);
    expect(compiled.querySelector('app-faction-logo .is-tinted')).toBeNull();
    http.verify();
  });

  it('downloads a campaign preset package after saving current settings', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    TestBed.inject(AuthService).currentUser.set(administratorProfile());
    const http = TestBed.inject(HttpTestingController);
    const campaign = scheduledEditCampaign(campaignId);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    await fixture.whenStable();
    fixture.detectChanges();

    stubBlobDownload();
    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as { downloadPreset: () => Promise<void> };
    const downloading = page.downloadPreset();
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    await fixture.whenStable();
    http.expectOne(`/api/campaigns/${campaignId}/preset-package`).flush(new Blob(['zip']), {
      headers: { 'Content-Disposition': 'attachment; filename="border-war-preset.mapandmuster-preset"' },
    });
    await downloading;
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Downloaded campaign preset.');
    http.verify();
  });

  it('uploads a campaign preset package into the named-preset library', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    TestBed.inject(AuthService).currentUser.set(administratorProfile());
    const http = TestBed.inject(HttpTestingController);
    const campaign = scheduledEditCampaign(campaignId);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as { uploadPreset: (event: Event) => Promise<void> };
    const input = document.createElement('input');
    const file = new File(['zip'], 'border-war-preset.mapandmuster-preset', { type: 'application/zip' });
    Object.defineProperty(input, 'files', { configurable: true, value: [file] });
    const uploading = page.uploadPreset({ target: input } as unknown as Event);
    const posted = http.expectOne('/api/campaign-presets/package');
    expect(posted.request.method).toBe('POST');
    expect(posted.request.body).toBeInstanceOf(FormData);
    posted.flush({ id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: 'Border War', hasMap: true });
    await fixture.whenStable();
    http
      .expectOne('/api/campaign-presets')
      .flush([{ id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: 'Border War', hasMap: true }]);
    await uploading;
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Imported preset Border War. Apply it with Add preset.');
    http.verify();
  });

  it('applies a saved preset including logos onto matching catalog names', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    TestBed.inject(AuthService).currentUser.set(administratorProfile());
    const http = TestBed.inject(HttpTestingController);
    const campaign = scheduledEditCampaign(campaignId);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      campaignPresetId: { setValue: (value: string) => void };
      applySelectedCampaignPreset: () => void;
      factions: { at: (index: number) => { controls: { id: { value: string } } } };
      hasStoredFlagImage: (factionId: string) => boolean;
    };
    page.campaignPresetId.setValue('saved:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
    page.applySelectedCampaignPreset();

    http.expectOne('/api/campaign-presets/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb').flush({
      ...campaign,
      id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      name: 'Logo War',
      factions: [
        { ...campaign.factions[0], id: 'preset-north', hasFlagImage: true },
        { ...campaign.factions[1], id: 'preset-south' },
      ],
    });
    await fixture.whenStable();
    const applied = http.expectOne(`/api/campaigns/${campaignId}/apply-preset`);
    expect(applied.request.method).toBe('POST');
    applied.flush({
      ...campaign,
      revision: 3,
      factions: [{ ...campaign.factions[0], hasFlagImage: true }, campaign.factions[1]],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.factions.at(0).controls.id.value).toBe('1');
    expect(page.hasStoredFlagImage('1')).toBe(true);
    http.verify();
  });

  it('remaps saved-preset catalog ids and previews logos before create', async () => {
    const fixture = TestBed.createComponent(CampaignSetupPage);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as unknown as {
      campaignPresetId: { setValue: (value: string) => void };
      applySelectedCampaignPreset: () => void;
      allyGroups: {
        at: (index: number) => { controls: { id: { value: string }; name: { value: string } } };
        length: number;
      };
      structureTypes: {
        controls: readonly { controls: { id: { value: string }; name: { value: string } } }[];
      };
      privateObjectiveTypes: {
        at: (index: number) => { controls: { structureTypeId: { value: string } } };
      };
      factions: {
        at: (index: number) => { controls: { id: { value: string } } };
      };
      hasStoredFlagImage: (factionId: string) => boolean;
      factionFlagUrl: (factionId: string) => string | null;
    };
    const townId = page.structureTypes.controls.find((item) => item.controls.name.value === 'Town')?.controls.id.value;
    expect(townId).toBeTruthy();

    page.campaignPresetId.setValue('saved:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
    page.applySelectedCampaignPreset();

    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/campaign-presets/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb').flush({
      ...scheduledEditCampaign('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),
      name: 'The Hunt in Estalia',
      factions: [
        {
          id: 'preset-north',
          name: 'North',
          color: '#2563EB',
          subfactions: [],
          allyGroupName: 'Pact',
          requiresSubfaction: false,
          hasFlagImage: true,
        },
        {
          id: 'preset-south',
          name: 'South',
          color: '#DC2626',
          subfactions: [],
          allyGroupName: 'Pact',
          requiresSubfaction: false,
          hasFlagImage: false,
        },
      ],
      allyGroups: [{ id: 'preset-pact', name: 'Pact', color: '#4B5563' }],
      structureTypes: [
        {
          id: 'preset-town',
          name: 'Town',
          builtinSymbol: 'Town',
          hasImage: true,
          hasPillagedImage: false,
          isBuildable: true,
          isPillageable: true,
          isDestructible: true,
          missions: [],
        },
      ],
      privateObjectiveTypes: [
        {
          id: 'preset-po',
          name: 'Hold two towns',
          campaignPoints: 3,
          allowedHolderKinds: ['Player'],
          scoringKind: 'Automatic',
          automaticKind: 'ControlStructureType',
          requiredCount: 2,
          structureTypeId: 'preset-town',
        },
      ],
    });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.allyGroups.length).toBe(1);
    expect(page.allyGroups.at(0).controls.name.value).toBe('Pact');
    expect(page.allyGroups.at(0).controls.id.value).not.toBe('preset-pact');
    expect(page.structureTypes.controls.find((item) => item.controls.name.value === 'Town')?.controls.id.value).toBe(
      townId,
    );
    expect(page.privateObjectiveTypes.at(0).controls.structureTypeId.value).toBe(townId);
    const northId = page.factions.at(0).controls.id.value;
    expect(northId).not.toBe('preset-north');
    expect(page.hasStoredFlagImage(northId)).toBe(true);
    expect(page.factionFlagUrl(northId)).toBe(
      '/api/campaign-presets/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/factions/preset-north/flag',
    );
    http.verify();
  });

  it('enables save after an edit and can discard unsaved campaign changes', async () => {
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
    const page = fixture.componentInstance as unknown as {
      form: { controls: { name: { setValue(value: string): void; value: string } } };
      discardUnsavedChanges: () => void;
      hasUnsavedChanges: () => boolean;
      collapseAllSections: () => void;
    };
    const name = compiled.querySelector<HTMLInputElement>('#name');
    expect(name?.classList.contains('ng-dirty')).toBe(false);

    page.form.controls.name.setValue('Frontier War');
    fixture.detectChanges();
    expect(page.hasUnsavedChanges()).toBe(true);
    expect(name?.classList.contains('ng-dirty')).toBe(true);

    const save = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Save campaign',
    );
    const discard = [...compiled.querySelectorAll('button')].find(
      (button) => button.textContent.trim() === 'Clear Unsaved Changes',
    );
    expect(save?.disabled).toBe(false);
    expect(discard?.disabled).toBe(false);

    page.collapseAllSections();
    fixture.detectChanges();
    const detailsToggle = [...compiled.querySelectorAll<HTMLButtonElement>('button.section-toggle')].find((button) =>
      button.textContent.includes('Campaign details'),
    );
    expect(detailsToggle?.classList.contains('has-dirty')).toBe(true);

    page.discardUnsavedChanges();
    fixture.detectChanges();
    expect(page.hasUnsavedChanges()).toBe(false);
    expect(page.form.controls.name.value).toBe('Border War');
    expect(save?.disabled).toBe(true);
    expect(detailsToggle?.classList.contains('has-dirty')).toBe(false);
    http.verify();
  });

  it('shows save status next to save campaign after success or failure', async () => {
    HTMLElement.prototype.scrollIntoView = () => undefined;
    const fixture = TestBed.createComponent(CampaignSetupPage);
    const http = TestBed.inject(HttpTestingController);
    const campaign = scheduledEditCampaign(campaignId);
    http.expectOne(`/api/campaigns/${campaignId}`).flush(campaign);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance as unknown as {
      form: { controls: { name: { setValue(value: string): void } } };
      discardUnsavedChanges: () => void;
      lastSavedAtUtc: () => string | null;
      save: () => Promise<void>;
    };

    expect(compiled.querySelector('.save-status')).toBeNull();
    expect(compiled.textContent).not.toContain('Last saved');

    page.form.controls.name.setValue('Frontier War');
    fixture.detectChanges();
    const saving = page.save();
    http.expectOne(`/api/campaigns/${campaignId}`).flush({ ...campaign, name: 'Frontier War', revision: 3 });
    await saving;
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Successfully saved changes.');
    expect(compiled.textContent).toContain('Last saved');
    expect(page.lastSavedAtUtc()).toBeTruthy();
    expect(compiled.querySelector('.save-status.is-success')).toBeTruthy();
    expect(compiled.querySelector('[aria-label="Campaign saved"]')).toBeTruthy();

    page.form.controls.name.setValue('Western War');
    fixture.detectChanges();
    page.discardUnsavedChanges();
    fixture.detectChanges();
    expect(compiled.querySelector('.save-status')).toBeNull();
    expect(compiled.textContent).toContain('Last saved');

    page.form.controls.name.setValue('Broken War');
    fixture.detectChanges();
    const failing = page.save();
    http
      .expectOne(`/api/campaigns/${campaignId}`)
      .flush({ title: 'Unable to save the campaign.' }, { status: 400, statusText: 'Bad Request' });
    await failing;
    fixture.detectChanges();
    expect(compiled.querySelector('.save-status.is-failure')).toBeTruthy();
    expect(compiled.querySelector('[aria-label="Campaign save failed"]')).toBeTruthy();
    expect(compiled.textContent).toContain('Last saved');
    http.verify();
  });

  it('pads stored army-size rows to the campaign round count', async () => {
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
      roundEscalations: [
        { roundNumber: 1, maxArmyPoints: 500, freeSupplyPoints: 2, freeCharacterCount: 3 },
        { roundNumber: 2, maxArmyPoints: 750, freeSupplyPoints: 2, freeCharacterCount: 3 },
        { roundNumber: 3, maxArmyPoints: 900, freeSupplyPoints: 2, freeCharacterCount: 3 },
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
    expect(compiled.querySelector('#round-escalation-points-7')).toBeTruthy();
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-0')?.value).toBe('500');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-supply-0')?.value).toBe('2');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-characters-0')?.value).toBe('3');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-2')?.value).toBe('900');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-3')?.value).toBe('1000');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-supply-3')?.value).toBe('1');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-characters-3')?.value).toBe('1');
    expect(compiled.querySelector<HTMLInputElement>('#round-escalation-points-7')?.value).toBe('1000');
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
      form: {
        getRawValue: () => {
          phases: { kind: string; durationAmount: number; durationUnit: string; endPhaseEarlyIfAble: boolean }[];
        };
      };
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
      { kind: 'Action', durationAmount: 2, durationUnit: 'Minutes', endPhaseEarlyIfAble: true },
      { kind: 'Action', durationAmount: 2, durationUnit: 'Minutes', endPhaseEarlyIfAble: true },
      { kind: 'Action', durationAmount: 2, durationUnit: 'Minutes', endPhaseEarlyIfAble: true },
      { kind: 'Battle', durationAmount: 54, durationUnit: 'Minutes', endPhaseEarlyIfAble: true },
    ]);
    http.verify();
  });
});

function scheduledEditCampaign(campaignId: string): CampaignDetail {
  return {
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
    terrainTypes: [
      {
        id: 'terrain-1',
        name: 'Plains',
        color: '#7CB342',
        missions: [
          {
            id: 'mission-1',
            name: 'Meeting engagement',
            url: null,
            hasFile: false,
            fileName: null,
          },
        ],
      },
    ],
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
  };
}

function administratorProfile(): OwnProfile {
  return {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    email: 'admin@example.test',
    username: 'admin',
    firstName: 'Ada',
    middleInitial: null,
    lastName: 'Admin',
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
    isAdministrator: true,
    inAppNotificationsEnabled: true,
    emailNotificationsEnabled: true,
    preferredChatLanguage: 'English',
  };
}
