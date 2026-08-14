import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { FACTION_PRESETS, WARHAMMER_OLD_WORLD_PRESET_ID } from '../../core/campaigns/faction-presets';
import { CampaignSetupPage } from './campaign-setup.page';

function factionNames(compiled: HTMLElement): string[] {
  return [...compiled.querySelectorAll<HTMLInputElement>('input[id^="faction-name-"]')].map((input) => input.value);
}

function clickNamedButton(compiled: HTMLElement, name: string): void {
  const button = [...compiled.querySelectorAll('button')].find((item) => item.textContent.trim() === name);
  expect(button).toBeTruthy();
  button!.click();
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
    expect(compiled.querySelector('#name')).toBeTruthy();
    expect(compiled.querySelector('#playerCount')).toBeTruthy();
    expect(compiled.querySelector('#startsAtLocal')).toBeTruthy();
    expect(compiled.querySelector('#roundCount')).toBeTruthy();
    expect(compiled.querySelector('#phase-kind-0')).toBeTruthy();
    expect(compiled.querySelector('#faction-name-0')).toBeTruthy();
    expect(compiled.querySelector('#faction-name-1')).toBeTruthy();

    const page = fixture.componentInstance as unknown as { save: () => Promise<void> };
    await page.save();
    fixture.detectChanges();
    const lines = [...compiled.querySelectorAll('.error-banner p')].map((node) => node.textContent.trim());
    expect(lines).toContain('Campaign name is not filled in.');
    expect(lines).toContain('Start date and time is not filled in.');
    expect(lines).toContain('Faction 1 name is not filled in.');
    expect(lines).toContain('Faction 2 name is not filled in.');
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
        at: (index: number) => { controls: { name: { setValue: (value: string) => void } } };
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
    TestBed.inject(HttpTestingController).verify();
  });
});
