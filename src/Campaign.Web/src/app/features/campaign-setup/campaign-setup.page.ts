import { Component, computed, DestroyRef, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, type FormArray, type FormControl, type FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { readApiErrorMessages, readApiFieldErrors } from '../../core/auth/auth.service';
import { FilterableComboboxComponent } from '../../shared/filterable-combobox/filterable-combobox.component';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type {
  CampaignDetail,
  CampaignMission,
  CampaignItemObjectiveType,
  CampaignStructureType,
  CampaignTerrainType,
  SaveCampaignPayload,
} from '../../core/campaigns/campaign.models';
import { defaultStructureCatalog, defaultTerrainCatalog } from '../../core/campaigns/catalog-defaults';
import { CAMPAIGN_PRESETS, campaignFromPreset } from '../../core/campaigns/campaign-presets';
import { FORM_SAVE_SUCCESS_MESSAGE } from '../../core/forms/form-messages';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import {
  DURATION_UNITS,
  PHASE_KINDS,
  durationRangeMessage,
  maxAmountForUnit,
} from '../../core/campaigns/campaign-schedule';
import {
  FACTION_COLOR_PALETTE,
  FACTION_PRESETS,
  factionsFromPreset,
  nextUnusedFactionColor,
} from '../../core/campaigns/faction-presets';
import {
  defaultItemObjective,
  type ItemObjectivePlacement,
  type ItemObjectivePresetItem,
} from '../../core/campaigns/item-objective-presets';
import { STRUCTURE_PRESETS, structureTypesFromPreset } from '../../core/campaigns/structure-presets';
import { TERRAIN_PRESETS, terrainTypesFromPreset } from '../../core/campaigns/terrain-presets';
import { listCountries, listTimeZones, regionsForCountry } from '../../core/location/location';
import { MapSymbolComponent } from '../../shared/map-symbol/map-symbol.component';
import { CampaignMapPreviewComponent } from '../../shared/campaign-map-preview/campaign-map-preview.component';
import { STRUCTURE_TYPES } from '../../core/maps/structures';
import {
  describeControlError,
  httpUrl,
  maxLength,
  maxValue,
  minLength,
  minValue,
  required,
  scrollAlertIntoView,
} from '../../core/forms/validators';

type NamedGroup = FormGroup<{ name: FormControl<string> }>;
type LinkGroup = FormGroup<{ label: FormControl<string>; url: FormControl<string> }>;
type MissionGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  url: FormControl<string>;
  clearFile: FormControl<boolean>;
}>;
type FactionGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  color: FormControl<string>;
  requiresSubfaction: FormControl<boolean>;
  allyGroupName: FormControl<string>;
  flagSource: FormControl<'color' | 'image'>;
  clearFlagImage: FormControl<boolean>;
  subfactions: FormArray<NamedGroup>;
}>;
type TerrainGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  color: FormControl<string>;
  missions: FormArray<MissionGroup>;
}>;
type StructureGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  builtinSymbol: FormControl<string>;
  iconSource: FormControl<'symbol' | 'image'>;
  clearImage: FormControl<boolean>;
  pillagedIconSource: FormControl<'symbol' | 'image'>;
  clearPillagedImage: FormControl<boolean>;
  isBuildable: FormControl<boolean>;
  isPillageable: FormControl<boolean>;
  isDestructible: FormControl<boolean>;
  missions: FormArray<MissionGroup>;
}>;
type ItemObjectiveGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  isHiddenUntilFound: FormControl<boolean>;
  placement: FormControl<ItemObjectivePlacement>;
  allowOnSpawn: FormControl<boolean>;
}>;
type PhaseGroup = FormGroup<{
  kind: FormControl<string>;
  durationAmount: FormControl<number>;
  durationUnit: FormControl<string>;
}>;

const TOP_LEVEL_SECTION_IDS = [
  'details',
  'schedule',
  'visibility',
  'allies',
  'factions',
  'terrain',
  'structures',
  'itemObjectives',
  'links',
  'map',
] as const;

@Component({
  selector: 'app-campaign-setup-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    FilterableComboboxComponent,
    MapSymbolComponent,
    CampaignMapPreviewComponent,
  ],
  templateUrl: './campaign-setup.page.html',
  styleUrl: './campaign-setup.page.css',
})
export class CampaignSetupPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formAlert = viewChild<ElementRef<HTMLElement>>('formAlert');

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessages = signal<string[]>([]);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly serverFields = signal<ReadonlySet<string>>(new Set());
  protected readonly campaignId = signal<string | null>(null);
  protected readonly hasExistingMap = signal(false);
  protected readonly mapFileName = signal<string | null>(null);
  protected readonly mapPreviewUrl = signal<string | null>(null);
  private readonly sectionOpen = signal<Record<string, boolean>>({});
  private readonly phasesTick = signal(0);
  protected readonly phaseGroups = computed(() => {
    this.phasesTick();
    return this.phases.controls;
  });
  private mapFile: File | null = null;
  private mapObjectUrl: string | null = null;
  private revision = 0;
  private readonly structureImages = new Map<string, File>();
  private readonly structurePillagedImages = new Map<string, File>();
  private readonly flagImages = new Map<string, File>();
  private readonly missionFiles = new Map<string, File>();
  private readonly storedStructureImages = signal<ReadonlySet<string>>(new Set());
  private readonly storedPillagedImages = signal<ReadonlySet<string>>(new Set());
  private readonly storedFlagImages = signal<ReadonlySet<string>>(new Set());
  private readonly storedMissionFiles = signal<ReadonlySet<string>>(new Set());

  protected readonly timeZones = listTimeZones();
  protected readonly durationUnits = DURATION_UNITS;
  protected readonly phaseKinds = PHASE_KINDS;
  protected readonly factionPresets = FACTION_PRESETS;
  protected readonly terrainPresets = TERRAIN_PRESETS;
  protected readonly structurePresets = STRUCTURE_PRESETS;
  protected readonly campaignPresets = CAMPAIGN_PRESETS;
  protected readonly structureSymbols = STRUCTURE_TYPES;
  protected readonly structureImageMaxPx = 50;
  protected readonly flagImageMaxPx = 50;
  protected readonly mapMaxBytes = 20 * 1024 * 1024;
  protected readonly presetId = this.formBuilder.nonNullable.control('');
  protected readonly selectedPresetId = toSignal(this.presetId.valueChanges, {
    initialValue: this.presetId.value,
  });
  protected readonly terrainPresetId = this.formBuilder.nonNullable.control('');
  protected readonly selectedTerrainPresetId = toSignal(this.terrainPresetId.valueChanges, {
    initialValue: this.terrainPresetId.value,
  });
  protected readonly structurePresetId = this.formBuilder.nonNullable.control('');
  protected readonly selectedStructurePresetId = toSignal(this.structurePresetId.valueChanges, {
    initialValue: this.structurePresetId.value,
  });
  protected readonly campaignPresetId = this.formBuilder.nonNullable.control('');
  protected readonly selectedCampaignPresetId = toSignal(this.campaignPresetId.valueChanges, {
    initialValue: this.campaignPresetId.value,
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [required, minLength(3), maxLength(80)]],
    description: ['', maxLength(500)],
    playerCount: [8, [required, minValue(2), maxValue(100)]],
    isPrivate: [false],
    isPubliclyViewable: [true],
    joinPassword: [''],
    city: ['', maxLength(100)],
    region: [''],
    country: [''],
    creatorRole: this.formBuilder.nonNullable.control<'manager' | 'both'>('both'),
    timeZoneId: ['UTC', required],
    startsAtLocal: ['', required],
    roundCount: [8, [required, minValue(3), maxValue(52)]],
    roundLengthAmount: [1, [required, minValue(1), maxValue(60)]],
    roundLengthUnit: ['Weeks', required],
    factions: this.formBuilder.array<FactionGroup>([
      this.createFactionGroup('', '', [''], { color: FACTION_COLOR_PALETTE[0] ?? '#2563EB' }),
      this.createFactionGroup('', '', [''], { color: FACTION_COLOR_PALETTE[1] ?? '#DC2626' }),
    ]),
    allyGroups: this.formBuilder.array<NamedGroup>([]),
    links: this.formBuilder.array<LinkGroup>([]),
    terrainTypes: this.formBuilder.array<TerrainGroup>(this.createDefaultTerrainGroups()),
    structureTypes: this.formBuilder.array<StructureGroup>(this.createDefaultStructureGroups()),
    itemObjectiveTypes: this.formBuilder.array<ItemObjectiveGroup>([]),
    phases: this.formBuilder.array<PhaseGroup>([
      this.createPhaseGroup('Action', 3, 'Days'),
      this.createPhaseGroup('Action', 3, 'Days'),
      this.createPhaseGroup('Battle', 1, 'Days'),
    ]),
  });
  protected readonly isPrivate = toSignal(this.form.controls.isPrivate.valueChanges, {
    initialValue: this.form.controls.isPrivate.value,
  });
  protected readonly countries = listCountries();
  protected readonly countryValue = toSignal(this.form.controls.country.valueChanges, {
    initialValue: this.form.controls.country.value,
  });
  protected readonly regionOptions = computed(() => regionsForCountry(this.countryValue()));

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    this.campaignId.set(id);
    if (id) {
      void this.loadCampaign(id);
    } else {
      this.loading.set(false);
    }

    this.destroyRef.onDestroy(() => this.revokeMapObjectUrl());
  }

  protected get factions(): FormArray<FactionGroup> {
    return this.form.controls.factions;
  }

  protected get allyGroups(): FormArray<NamedGroup> {
    return this.form.controls.allyGroups;
  }

  protected get links(): FormArray<LinkGroup> {
    return this.form.controls.links;
  }

  protected get phases(): FormArray<PhaseGroup> {
    return this.form.controls.phases;
  }

  protected get terrainTypes(): FormArray<TerrainGroup> {
    return this.form.controls.terrainTypes;
  }

  protected get structureTypes(): FormArray<StructureGroup> {
    return this.form.controls.structureTypes;
  }

  protected get itemObjectiveTypes(): FormArray<ItemObjectiveGroup> {
    return this.form.controls.itemObjectiveTypes;
  }

  protected actionCount(): number {
    return this.phases.controls.filter((phase) => phase.controls.kind.value === 'Action').length;
  }

  protected maxAmountFor(unit: string): number {
    return maxAmountForUnit(unit);
  }

  protected isEdit(): boolean {
    return this.campaignId() !== null;
  }

  protected descriptionLength(): number {
    return this.form.controls.description.value.length;
  }

  protected subfactionsOf(faction: FactionGroup): FormArray<NamedGroup> {
    return faction.controls.subfactions;
  }

  protected missionsOf(group: TerrainGroup | StructureGroup): FormArray<MissionGroup> {
    return group.controls.missions;
  }

  protected isInvalid(name: string): boolean {
    if (this.serverFields().has(name)) {
      return true;
    }

    const control = this.form.get(name);
    return !!control && control.touched && control.invalid;
  }

  protected isGroupInvalid(group: FormGroup, name: string, field: string): boolean {
    if (this.serverFields().has(field)) {
      return true;
    }

    const control = group.get(name);
    return !!control && control.touched && control.invalid;
  }

  protected isOpen(id: string): boolean {
    return this.sectionOpen()[id] !== false;
  }

  protected toggleSection(id: string): void {
    this.sectionOpen.update((current) => ({ ...current, [id]: current[id] === false }));
  }

  protected expandAllSections(): void {
    this.setAllSections(true);
  }

  protected collapseAllSections(): void {
    this.setAllSections(false);
  }

  protected allyMembers(groupName: string): string {
    return this.factions.controls
      .filter((faction) => faction.controls.allyGroupName.value === groupName && faction.controls.name.value.trim())
      .map((faction) => faction.controls.name.value.trim())
      .join(', ');
  }

  protected unalignedFactions(): string {
    return this.factions.controls
      .filter((faction) => !faction.controls.allyGroupName.value.trim() && faction.controls.name.value.trim())
      .map((faction) => faction.controls.name.value.trim())
      .join(', ');
  }

  protected itemLabel(name: string, fallback: string): string {
    const trimmed = name.trim();
    return trimmed.length > 0 ? trimmed : fallback;
  }

  protected applySelectedPreset(): void {
    const factions = factionsFromPreset(this.presetId.value);
    if (!factions) {
      this.revealErrors(['Select a faction preset before adding it.']);
      return;
    }

    this.replaceArray(
      this.factions,
      factions.map((faction) =>
        this.createFactionGroup(faction.name, '', faction.subfactions, {
          color: faction.color,
          requiresSubfaction: faction.requiresSubfaction,
        }),
      ),
    );
  }

  protected applySelectedTerrainPreset(): void {
    const types = terrainTypesFromPreset(this.terrainPresetId.value);
    if (!types) {
      this.revealErrors(['Select a terrain preset before adding it.']);
      return;
    }

    this.replaceArray(
      this.terrainTypes,
      types.map((entry) => this.createTerrainGroup(undefined, entry.name, entry.color)),
    );
  }

  protected applySelectedStructurePreset(): void {
    const types = structureTypesFromPreset(this.structurePresetId.value);
    if (!types) {
      this.revealErrors(['Select a structure preset before adding it.']);
      return;
    }

    this.replaceArray(
      this.structureTypes,
      types.map((entry) =>
        this.createStructureGroup(
          undefined,
          entry.name,
          entry.builtinSymbol,
          undefined,
          'symbol',
          'symbol',
          entry.isBuildable,
          entry.isPillageable,
          entry.isDestructible,
        ),
      ),
    );
  }

  protected applySelectedCampaignPreset(): void {
    const copy = campaignFromPreset(this.campaignPresetId.value);
    if (!copy) {
      this.revealErrors(['Select a campaign preset before adding it.']);
      return;
    }

    if (!this.form.controls.name.value.trim()) {
      this.form.controls.name.setValue(copy.name);
    }

    this.replaceArray(
      this.factions,
      copy.factions.map((faction) =>
        this.createFactionGroup(faction.name, '', faction.subfactions, {
          color: faction.color,
          requiresSubfaction: faction.requiresSubfaction,
        }),
      ),
    );
    this.replaceArray(
      this.terrainTypes,
      copy.terrainTypes.map((entry) => this.createTerrainGroup(undefined, entry.name, entry.color)),
    );
    this.replaceArray(
      this.structureTypes,
      copy.structureTypes.map((entry) =>
        this.createStructureGroup(
          undefined,
          entry.name,
          entry.builtinSymbol,
          undefined,
          'symbol',
          'symbol',
          entry.isBuildable,
          entry.isPillageable,
          entry.isDestructible,
        ),
      ),
    );
    this.replaceArray(
      this.itemObjectiveTypes,
      copy.itemObjectives.map((item) => this.createItemObjectiveGroup(item)),
    );
  }

  protected clearFactions(): void {
    this.replaceArray(this.factions, [
      this.createFactionGroup('', '', [''], { color: FACTION_COLOR_PALETTE[0] ?? '#2563EB' }),
      this.createFactionGroup('', '', [''], { color: FACTION_COLOR_PALETTE[1] ?? '#DC2626' }),
    ]);
  }

  protected clearAllyGroups(): void {
    this.replaceArray(this.allyGroups, []);
    for (const faction of this.factions.controls) {
      faction.controls.allyGroupName.setValue('');
    }
  }

  protected addFaction(): void {
    this.factions.push(
      this.createFactionGroup('', '', [''], {
        color: nextUnusedFactionColor(this.factions.controls.map((faction) => faction.controls.color.value)),
      }),
    );
  }

  protected removeFaction(index: number): void {
    if (this.factions.length <= 2) {
      return;
    }

    this.factions.removeAt(index);
  }

  protected addSubfaction(faction: FactionGroup): void {
    faction.controls.subfactions.push(this.createNamedGroup());
  }

  protected removeSubfaction(faction: FactionGroup, index: number): void {
    faction.controls.subfactions.removeAt(index);
  }

  protected addAllyGroup(): void {
    this.allyGroups.push(this.createNamedGroup());
  }

  protected removeAllyGroup(index: number): void {
    const name = this.allyGroups.at(index).controls.name.value;
    this.allyGroups.removeAt(index);
    for (const faction of this.factions.controls) {
      if (faction.controls.allyGroupName.value === name) {
        faction.controls.allyGroupName.setValue('');
      }
    }
  }

  protected addLink(): void {
    if (this.links.length >= 20) {
      return;
    }

    this.links.push(this.createLinkGroup());
  }

  protected removeLink(index: number): void {
    this.links.removeAt(index);
  }

  protected addTerrainType(): void {
    if (this.terrainTypes.length >= 50) {
      return;
    }

    this.terrainTypes.push(this.createTerrainGroup());
  }

  protected removeTerrainType(index: number): void {
    if (this.terrainTypes.length <= 1) {
      return;
    }

    this.removeMissionsFiles(this.terrainTypes.at(index));
    this.terrainTypes.removeAt(index);
  }

  protected addStructureType(): void {
    if (this.structureTypes.length >= 50) {
      return;
    }

    this.structureTypes.push(this.createStructureGroup());
  }

  protected removeStructureType(index: number): void {
    const id = this.structureTypes.at(index).controls.id.value;
    this.structureImages.delete(id);
    this.structurePillagedImages.delete(id);
    this.removeMissionsFiles(this.structureTypes.at(index));
    this.structureTypes.removeAt(index);
  }

  protected addItemObjective(): void {
    if (this.itemObjectiveTypes.length >= 50) {
      return;
    }

    this.itemObjectiveTypes.push(this.createItemObjectiveGroup());
  }

  protected removeItemObjective(index: number): void {
    this.itemObjectiveTypes.removeAt(index);
  }

  protected addMission(group: TerrainGroup | StructureGroup): void {
    if (group.controls.missions.length >= 20) {
      return;
    }

    group.controls.missions.push(this.createMissionGroup());
  }

  protected addReusedMission(group: TerrainGroup | StructureGroup, missionId: string): void {
    if (!missionId || group.controls.missions.length >= 20) {
      return;
    }

    if (group.controls.missions.controls.some((mission) => mission.controls.id.value === missionId)) {
      return;
    }

    const source = this.findMission(missionId);
    if (!source) {
      return;
    }

    group.controls.missions.push(
      this.createMissionGroup(
        source.controls.id.value,
        source.controls.name.value,
        source.controls.url.value,
        source.controls.clearFile.value,
      ),
    );
  }

  protected onReuseMissionSelected(group: TerrainGroup | StructureGroup, event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.addReusedMission(group, select.value);
    select.value = '';
  }

  protected reusableMissions(group: TerrainGroup | StructureGroup): { id: string; name: string }[] {
    const used = new Set(group.controls.missions.controls.map((mission) => mission.controls.id.value));
    const seen = new Map<string, { id: string; name: string }>();
    for (const owner of [...this.terrainTypes.controls, ...this.structureTypes.controls]) {
      for (const mission of owner.controls.missions.controls) {
        const name = mission.controls.name.value.trim();
        const id = mission.controls.id.value;
        if (!name || used.has(id) || seen.has(id)) {
          continue;
        }

        seen.set(id, { id, name });
      }
    }

    return [...seen.values()].sort((left, right) => left.name.localeCompare(right.name));
  }

  protected setPillagedIconSource(structure: StructureGroup, source: 'symbol' | 'image'): void {
    structure.controls.pillagedIconSource.setValue(source);
    if (source === 'symbol') {
      this.structurePillagedImages.delete(structure.controls.id.value);
      structure.controls.clearPillagedImage.setValue(true);
    } else {
      structure.controls.clearPillagedImage.setValue(false);
    }
  }

  protected setIconSource(structure: StructureGroup, source: 'symbol' | 'image'): void {
    structure.controls.iconSource.setValue(source);
    if (source === 'symbol') {
      this.structureImages.delete(structure.controls.id.value);
      structure.controls.clearImage.setValue(true);
      if (!structure.controls.builtinSymbol.value) {
        structure.controls.builtinSymbol.setValue(this.structureSymbols[0].id);
      }
    } else {
      structure.controls.clearImage.setValue(false);
    }
  }

  protected setFlagSource(faction: FactionGroup, source: 'color' | 'image'): void {
    faction.controls.flagSource.setValue(source);
    if (source === 'color') {
      this.flagImages.delete(faction.controls.id.value);
      faction.controls.clearFlagImage.setValue(true);
    } else {
      faction.controls.clearFlagImage.setValue(false);
    }
  }

  protected removeMission(group: TerrainGroup | StructureGroup, index: number): void {
    const missionId = group.controls.missions.at(index).controls.id.value;
    group.controls.missions.removeAt(index);
    const stillUsed = [...this.terrainTypes.controls, ...this.structureTypes.controls].some((owner) =>
      owner.controls.missions.controls.some((mission) => mission.controls.id.value === missionId),
    );
    if (!stillUsed) {
      this.missionFiles.delete(missionId);
    }
  }

  protected addPhase(kind: string): void {
    if (this.phases.length >= 16) {
      return;
    }

    const unit = this.form.controls.roundLengthUnit.value;
    this.phases.push(this.createPhaseGroup(kind, 1, unit));
    this.refreshPhases();
  }

  protected removePhase(index: number): void {
    if (this.phases.length <= 2) {
      return;
    }

    this.phases.removeAt(index);
    this.refreshPhases();
  }

  protected movePhase(index: number, offset: number): void {
    const target = index + offset;
    if (target < 0 || target >= this.phases.length) {
      return;
    }

    const current = this.phases.at(index);
    this.phases.removeAt(index);
    this.phases.insert(target, current);
    this.refreshPhases();
  }

  protected onMapSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file && file.size > this.mapMaxBytes) {
      input.value = '';
      this.mapFile = null;
      this.mapFileName.set(null);
      this.revokeMapObjectUrl();
      const id = this.campaignId();
      this.mapPreviewUrl.set(this.hasExistingMap() && id ? this.campaignsApi.mapUrl(id, this.revision) : null);
      this.successMessage.set(null);
      this.errorMessages.set(['Campaign maps must be 20 MB or smaller.']);
      return;
    }

    this.mapFile = file;
    this.mapFileName.set(file?.name ?? null);
    this.revokeMapObjectUrl();
    if (file) {
      this.mapObjectUrl = URL.createObjectURL(file);
      this.mapPreviewUrl.set(this.mapObjectUrl);
      return;
    }

    const id = this.campaignId();
    this.mapPreviewUrl.set(this.hasExistingMap() && id ? this.campaignsApi.mapUrl(id, this.revision) : null);
  }

  private revokeMapObjectUrl(): void {
    if (this.mapObjectUrl) {
      URL.revokeObjectURL(this.mapObjectUrl);
      this.mapObjectUrl = null;
    }
  }

  private setStoredMapPreview(campaignId: string, revision: number, hasMap: boolean): void {
    this.revokeMapObjectUrl();
    this.mapPreviewUrl.set(hasMap ? this.campaignsApi.mapUrl(campaignId, revision) : null);
  }

  protected onStructureImageSelected(structureId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file) {
      this.structureImages.set(structureId, file);
      const group = this.structureTypes.controls.find((item) => item.controls.id.value === structureId);
      group?.controls.clearImage.setValue(false);
    }
  }

  protected onStructurePillagedImageSelected(structureId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file) {
      this.structurePillagedImages.set(structureId, file);
      const group = this.structureTypes.controls.find((item) => item.controls.id.value === structureId);
      group?.controls.clearPillagedImage.setValue(false);
    }
  }

  protected onFlagImageSelected(factionId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file) {
      this.flagImages.set(factionId, file);
      const group = this.factions.controls.find((item) => item.controls.id.value === factionId);
      group?.controls.clearFlagImage.setValue(false);
    }
  }

  protected flagImageName(factionId: string): string | null {
    return this.flagImages.get(factionId)?.name ?? null;
  }

  protected hasStoredFlagImage(factionId: string): boolean {
    return this.storedFlagImages().has(factionId);
  }

  protected factionFlagUrl(factionId: string): string | null {
    const campaignId = this.campaignId();
    if (!campaignId || !this.hasStoredFlagImage(factionId)) {
      return null;
    }

    return this.campaignsApi.flagImageUrl(campaignId, factionId, this.revision);
  }

  protected onMissionFileSelected(missionId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file) {
      this.missionFiles.set(missionId, file);
      for (const owner of [...this.terrainTypes.controls, ...this.structureTypes.controls]) {
        const mission = owner.controls.missions.controls.find((item) => item.controls.id.value === missionId);
        if (mission) {
          mission.controls.clearFile.setValue(false);
          mission.controls.url.setValue('');
        }
      }
    }
  }

  protected structureImageName(structureId: string): string | null {
    return this.structureImages.get(structureId)?.name ?? null;
  }

  protected structurePillagedImageName(structureId: string): string | null {
    return this.structurePillagedImages.get(structureId)?.name ?? null;
  }

  protected missionFileName(missionId: string): string | null {
    return this.missionFiles.get(missionId)?.name ?? null;
  }

  protected hasStoredStructureImage(structureId: string): boolean {
    return this.storedStructureImages().has(structureId);
  }

  protected hasStoredPillagedImage(structureId: string): boolean {
    return this.storedPillagedImages().has(structureId);
  }

  protected hasStoredMissionFile(missionId: string): boolean {
    return this.storedMissionFiles().has(missionId);
  }

  protected structureImageUrl(structureId: string): string | null {
    const campaignId = this.campaignId();
    if (!campaignId || !this.hasStoredStructureImage(structureId)) {
      return null;
    }

    return this.campaignsApi.structureImageUrl(campaignId, structureId, this.revision);
  }

  protected structurePillagedImageUrl(structureId: string): string | null {
    const campaignId = this.campaignId();
    if (!campaignId || !this.hasStoredPillagedImage(structureId)) {
      return null;
    }

    return this.campaignsApi.structureImageUrl(campaignId, structureId, this.revision, true);
  }

  protected async save(): Promise<void> {
    this.form.markAllAsTouched();
    this.serverFields.set(new Set());
    this.successMessage.set(null);
    const collected = this.collectFailures();
    if (collected.messages.length > 0) {
      this.expandSections(collected.sections);
      this.revealErrors(collected.messages);
      return;
    }

    this.saving.set(true);
    this.errorMessages.set([]);
    try {
      const created = await this.overlay.run(async () => {
        const payload = this.toPayload();
        const campaignId = this.campaignId();
        let detail: CampaignDetail;
        if (campaignId) {
          detail = await this.campaignsApi.update(campaignId, { ...payload, revision: this.revision });
        } else {
          detail = await this.campaignsApi.create(payload);
        }

        if (this.mapFile) {
          detail = await this.campaignsApi.uploadMap(detail.id, this.mapFile, detail.revision);
        }

        for (const [structureId, file] of this.structureImages) {
          const structure = this.structureTypes.controls.find((item) => item.controls.id.value === structureId);
          if (structure?.controls.iconSource.value !== 'image') {
            continue;
          }

          detail = await this.campaignsApi.uploadStructureImage(detail.id, structureId, file, detail.revision);
        }

        for (const [structureId, file] of this.structurePillagedImages) {
          const structure = this.structureTypes.controls.find((item) => item.controls.id.value === structureId);
          if (structure?.controls.pillagedIconSource.value !== 'image') {
            continue;
          }

          detail = await this.campaignsApi.uploadStructureImage(detail.id, structureId, file, detail.revision, true);
        }

        for (const [factionId, file] of this.flagImages) {
          const faction = this.factions.controls.find((item) => item.controls.id.value === factionId);
          if (faction?.controls.flagSource.value !== 'image') {
            continue;
          }

          detail = await this.campaignsApi.uploadFlagImage(detail.id, factionId, file, detail.revision);
        }

        for (const [missionId, file] of this.missionFiles) {
          detail = await this.campaignsApi.uploadMissionFile(detail.id, missionId, file, detail.revision);
        }

        return { detail, isNew: campaignId === null };
      });

      this.revision = created.detail.revision;
      this.hasExistingMap.set(created.detail.hasMap);
      this.mapFile = null;
      this.mapFileName.set(null);
      this.setStoredMapPreview(created.detail.id, created.detail.revision, created.detail.hasMap);
      this.structureImages.clear();
      this.structurePillagedImages.clear();
      this.flagImages.clear();
      this.missionFiles.clear();
      this.rememberStoredFiles(created.detail);
      if (created.isNew) {
        await this.router.navigate(['/campaigns', created.detail.id, 'map']);
        return;
      }

      this.revealSuccess();
    } catch (error: unknown) {
      this.serverFields.set(new Set(readApiFieldErrors(error)));
      this.revealErrors(readApiErrorMessages(error, 'Unable to save the campaign.'));
    } finally {
      this.saving.set(false);
    }
  }

  private async loadCampaign(id: string): Promise<void> {
    try {
      const campaign = await this.campaignsApi.get(id);
      if (!campaign.canManage) {
        this.revealErrors(['Only a campaign manager can edit this campaign.']);
        this.loading.set(false);
        return;
      }

      if (campaign.status !== 'Scheduled') {
        await this.router.navigate(['/campaigns', id, 'play']);
        return;
      }

      this.revision = campaign.revision;
      this.hasExistingMap.set(campaign.hasMap);
      this.setStoredMapPreview(id, campaign.revision, campaign.hasMap);
      this.rememberStoredFiles(campaign);
      this.form.patchValue({
        name: campaign.name,
        description: campaign.description ?? '',
        playerCount: campaign.playerSlotCount,
        isPrivate: campaign.isPrivate,
        isPubliclyViewable: campaign.isPubliclyViewable,
        city: campaign.city ?? '',
        region: campaign.region ?? '',
        country: campaign.country ?? '',
        creatorRole: campaign.creatorIsParticipant ? 'both' : 'manager',
        timeZoneId: campaign.timeZoneId,
        startsAtLocal: campaign.startsAtLocal,
        roundCount: campaign.roundCount,
        roundLengthAmount: campaign.roundLengthAmount,
        roundLengthUnit: campaign.roundLengthUnit,
      });
      this.replaceArray(
        this.factions,
        campaign.factions.map((faction) =>
          this.createFactionGroup(faction.name, faction.allyGroupName ?? '', faction.subfactions, {
            id: faction.id,
            color: faction.color,
            requiresSubfaction: faction.requiresSubfaction,
            hasFlagImage: faction.hasFlagImage,
          }),
        ),
      );
      this.replaceArray(
        this.allyGroups,
        campaign.allyGroups.map((group) => this.createNamedGroup(group.name)),
      );
      this.replaceArray(
        this.links,
        campaign.links.map((link) => this.createLinkGroup(link.label, link.url)),
      );
      this.replaceArray(
        this.terrainTypes,
        campaign.terrainTypes.length > 0
          ? campaign.terrainTypes.map((type) => this.createTerrainGroupFromDetail(type))
          : this.createDefaultTerrainGroups(),
      );
      this.replaceArray(
        this.structureTypes,
        campaign.structureTypes.map((type) => this.createStructureGroupFromDetail(type)),
      );
      this.replaceArray(
        this.itemObjectiveTypes,
        (campaign.itemObjectiveTypes ?? []).map((type) => this.createItemObjectiveGroupFromDetail(type)),
      );
      this.replaceArray(
        this.phases,
        campaign.phases.map((phase) => this.createPhaseGroup(phase.kind, phase.durationAmount, phase.durationUnit)),
      );
      this.refreshPhases();
      if (this.factions.length < 2) {
        while (this.factions.length < 2) {
          this.factions.push(this.createFactionGroup());
        }
      }
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Unable to load this campaign.'));
    } finally {
      this.loading.set(false);
    }
  }

  private createFactionGroup(
    name = '',
    allyGroupName = '',
    subfactions: readonly string[] = [''],
    options?: {
      id?: string;
      color?: string;
      requiresSubfaction?: boolean;
      hasFlagImage?: boolean;
    },
  ): FactionGroup {
    const names = subfactions.length > 0 ? subfactions : [''];
    return this.formBuilder.nonNullable.group({
      id: [options?.id ?? crypto.randomUUID()],
      name: [name, [required, maxLength(60)]],
      color: [options?.color ?? '#2563EB', required],
      requiresSubfaction: [options?.requiresSubfaction === true],
      allyGroupName: [allyGroupName],
      flagSource: this.formBuilder.nonNullable.control<'color' | 'image'>(options?.hasFlagImage ? 'image' : 'color'),
      clearFlagImage: [false],
      subfactions: this.formBuilder.array<NamedGroup>(names.map((value) => this.createNamedGroup(value))),
    });
  }

  private createNamedGroup(name = ''): NamedGroup {
    return this.formBuilder.nonNullable.group({
      name: [name, maxLength(60)],
    });
  }

  private newId(): string {
    return crypto.randomUUID();
  }

  private createMissionGroup(id?: string, name = '', url = '', clearFile = false): MissionGroup {
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [name, maxLength(60)],
      url: [url, [maxLength(2048), httpUrl]],
      clearFile: [clearFile],
    });
  }

  private createTerrainGroup(id?: string, name = '', color = '#7CB342', missionName = ''): TerrainGroup {
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [name, [required, maxLength(60)]],
      color: [color, required],
      missions: this.formBuilder.array<MissionGroup>([this.createMissionGroup(undefined, missionName)]),
    });
  }

  private createTerrainGroupFromDetail(type: CampaignTerrainType): TerrainGroup {
    const missions =
      type.missions.length > 0
        ? type.missions.map((mission) => this.createMissionGroupFromDetail(mission))
        : [this.createMissionGroup()];
    return this.formBuilder.nonNullable.group({
      id: [type.id],
      name: [type.name, [required, maxLength(60)]],
      color: [type.color, required],
      missions: this.formBuilder.array<MissionGroup>(missions),
    });
  }

  private createStructureGroup(
    id?: string,
    name = '',
    builtinSymbol = '',
    missions?: MissionGroup[],
    iconSource: 'symbol' | 'image' = 'symbol',
    pillagedIconSource: 'symbol' | 'image' = 'symbol',
    isBuildable = true,
    isPillageable = true,
    isDestructible = true,
  ): StructureGroup {
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [name, [required, maxLength(60)]],
      builtinSymbol: [builtinSymbol],
      iconSource: this.formBuilder.nonNullable.control<'symbol' | 'image'>(iconSource),
      clearImage: [false],
      pillagedIconSource: this.formBuilder.nonNullable.control<'symbol' | 'image'>(pillagedIconSource),
      clearPillagedImage: [false],
      isBuildable: [isBuildable],
      isPillageable: [isPillageable],
      isDestructible: [isDestructible],
      missions: this.formBuilder.array<MissionGroup>(missions ?? []),
    });
  }

  private createStructureGroupFromDetail(type: CampaignStructureType): StructureGroup {
    const missions =
      type.missions.length > 0 ? type.missions.map((mission) => this.createMissionGroupFromDetail(mission)) : [];
    return this.createStructureGroup(
      type.id,
      type.name,
      type.builtinSymbol ?? '',
      missions,
      type.hasImage ? 'image' : 'symbol',
      type.hasPillagedImage ? 'image' : 'symbol',
      type.isBuildable,
      type.isPillageable,
      type.isDestructible,
    );
  }

  private createMissionGroupFromDetail(mission: CampaignMission): MissionGroup {
    return this.createMissionGroup(mission.id, mission.name, mission.url ?? '', false);
  }

  private createItemObjectiveGroup(item?: ItemObjectivePresetItem, id?: string): ItemObjectiveGroup {
    const defaults = defaultItemObjective();
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [item?.name ?? '', [maxLength(60)]],
      isHiddenUntilFound: [item?.isHiddenUntilFound ?? defaults.isHiddenUntilFound],
      placement: this.formBuilder.nonNullable.control<ItemObjectivePlacement>(item?.placement ?? defaults.placement),
      allowOnSpawn: [item?.allowOnSpawn ?? defaults.allowOnSpawn],
    });
  }

  private createItemObjectiveGroupFromDetail(type: CampaignItemObjectiveType): ItemObjectiveGroup {
    return this.createItemObjectiveGroup(
      {
        name: type.name,
        isHiddenUntilFound: type.isHiddenUntilFound,
        placement: type.placement === 'Placed' ? 'Placed' : 'Random',
        allowOnSpawn: type.allowOnSpawn,
      },
      type.id,
    );
  }

  private createDefaultTerrainGroups(): TerrainGroup[] {
    return defaultTerrainCatalog().map((entry) => this.createTerrainGroup(undefined, entry.name, entry.color));
  }

  private createDefaultStructureGroups(): StructureGroup[] {
    return defaultStructureCatalog().map((entry) =>
      this.createStructureGroup(
        undefined,
        entry.name,
        entry.builtinSymbol,
        undefined,
        'symbol',
        'symbol',
        entry.isBuildable,
        entry.isPillageable,
        entry.isDestructible,
      ),
    );
  }

  private refreshPhases(): void {
    this.phasesTick.update((value) => value + 1);
  }

  private createLinkGroup(label = '', url = ''): LinkGroup {
    return this.formBuilder.nonNullable.group({
      label: [label, maxLength(80)],
      url: [url, [maxLength(2048), httpUrl]],
    });
  }

  private createPhaseGroup(kind: string, durationAmount: number, durationUnit: string): PhaseGroup {
    return this.formBuilder.nonNullable.group({
      kind: [kind, required],
      durationAmount: [durationAmount, [required, minValue(1), maxValue(60)]],
      durationUnit: [durationUnit, required],
    });
  }

  private replaceArray<T extends FormGroup>(array: FormArray<T>, groups: T[]): void {
    array.clear();
    for (const group of groups) {
      array.push(group);
    }
  }

  private toPayload(): SaveCampaignPayload {
    const value = this.form.getRawValue();
    const allyGroups = value.allyGroups
      .map((group) => group.name.trim())
      .filter((name) => name.length > 0)
      .map((name) => ({ name }));
    const factions = value.factions.map((faction) => ({
      id: faction.id,
      name: faction.name.trim(),
      color: faction.color,
      requiresSubfaction: faction.requiresSubfaction,
      allyGroupName: faction.allyGroupName.trim() || null,
      subfactions: faction.subfactions.map((item) => item.name.trim()).filter((name) => name.length > 0),
      clearFlagImage: faction.flagSource === 'color' || faction.clearFlagImage,
    }));
    const links = value.links
      .filter((link) => link.label.trim().length > 0 || link.url.trim().length > 0)
      .map((link) => ({ label: link.label.trim(), url: link.url.trim() }));
    const terrainTypes = value.terrainTypes.map((type) => ({
      id: type.id,
      name: type.name.trim(),
      color: type.color,
      missions: type.missions
        .filter((mission) => mission.name.trim().length > 0 || mission.url.trim().length > 0)
        .map((mission) => this.toMissionPayload(mission)),
    }));
    const structureTypes = value.structureTypes.map((type) => ({
      id: type.id,
      name: type.name.trim(),
      builtinSymbol: type.builtinSymbol.trim() || null,
      clearImage: type.iconSource === 'symbol' || type.clearImage,
      clearPillagedImage: type.pillagedIconSource === 'symbol' || type.clearPillagedImage,
      isBuildable: type.isBuildable,
      isPillageable: type.isPillageable,
      isDestructible: type.isDestructible,
      missions: type.missions
        .filter((mission) => mission.name.trim().length > 0 || mission.url.trim().length > 0)
        .map((mission) => this.toMissionPayload(mission)),
    }));
    const itemObjectiveTypes = value.itemObjectiveTypes
      .filter((type) => type.name.trim().length > 0)
      .map((type) => ({
        id: type.id,
        name: type.name.trim(),
        isHiddenUntilFound: type.isHiddenUntilFound,
        placement: type.placement,
        allowOnSpawn: type.allowOnSpawn,
      }));

    return {
      name: value.name.trim(),
      description: value.description.trim() || null,
      playerCount: Number(value.playerCount),
      isPrivate: value.isPrivate,
      isPubliclyViewable: value.isPubliclyViewable,
      joinPassword: value.isPrivate && value.joinPassword.trim().length > 0 ? value.joinPassword : null,
      creatorIsParticipant: value.creatorRole === 'both',
      city: value.city.trim() || null,
      region: value.region.trim() || null,
      country: value.country.trim() || null,
      factions,
      allyGroups,
      links,
      terrainTypes,
      structureTypes,
      itemObjectiveTypes,
      timeZoneId: value.timeZoneId || 'UTC',
      startsAtLocal: value.startsAtLocal,
      roundCount: Number(value.roundCount),
      roundLengthAmount: Number(value.roundLengthAmount),
      roundLengthUnit: value.roundLengthUnit,
      phases: value.phases.map((phase) => ({
        kind: phase.kind,
        durationAmount: Number(phase.durationAmount),
        durationUnit: phase.durationUnit,
      })),
    };
  }

  private toMissionPayload(mission: { id: string; name: string; url: string; clearFile: boolean }): {
    id: string;
    name: string;
    url: string | null;
    clearFile: boolean;
  } {
    const hasPendingFile = this.missionFiles.has(mission.id);
    return {
      id: mission.id,
      name: mission.name.trim(),
      url: hasPendingFile ? null : mission.url.trim() || null,
      clearFile: hasPendingFile ? false : mission.clearFile,
    };
  }

  private collectFailures(): { messages: string[]; sections: string[] } {
    const failures: string[] = [];
    const sections = new Set<string>();
    const labels: Record<string, string> = {
      name: 'Campaign name',
      description: 'Description',
      playerCount: 'Number of players',
      city: 'City',
      startsAtLocal: 'Start date and time',
      timeZoneId: 'Campaign time zone',
      roundCount: 'Number of rounds',
      roundLengthAmount: 'Round length',
    };
    const labelSections: Record<string, string> = {
      name: 'details',
      description: 'details',
      playerCount: 'details',
      city: 'details',
      startsAtLocal: 'schedule',
      timeZoneId: 'schedule',
      roundCount: 'schedule',
      roundLengthAmount: 'schedule',
    };
    for (const [name, label] of Object.entries(labels)) {
      const message = describeControlError(this.form.get(name), label);
      if (message) {
        failures.push(message);
        sections.add(labelSections[name] ?? 'details');
      }
    }

    const city = this.form.controls.city.value.trim();
    const region = this.form.controls.region.value.trim();
    const country = this.form.controls.country.value.trim();
    if (city && !region) {
      failures.push('State or province is required when a city is provided.');
      sections.add('details');
    }

    if (region && !country) {
      failures.push('Country is required when a state or province is provided.');
      sections.add('details');
    }

    if (this.form.controls.isPrivate.value) {
      const password = this.form.controls.joinPassword.value;
      if (!this.isEdit() && password.trim().length === 0) {
        failures.push('Private campaigns require a join password.');
        sections.add('visibility');
      } else if (password.length > 0 && password.length < 8) {
        failures.push('Join password is too short (minimum 8 characters).');
        sections.add('visibility');
      }
    }

    this.factions.controls.forEach((faction, index) => {
      const message = describeControlError(faction.controls.name, `Faction ${index + 1} name`);
      if (message) {
        failures.push(message);
        sections.add('factions');
        sections.add(`faction-item-${index}`);
      }

      const namedSubfactions = faction.controls.subfactions.controls.filter(
        (item) => item.controls.name.value.trim().length > 0,
      );
      if (faction.controls.requiresSubfaction.value && namedSubfactions.length === 0) {
        failures.push(`Faction ${index + 1} requires at least one subfaction.`);
        sections.add('factions');
        sections.add(`faction-item-${index}`);
        sections.add(`faction-sub-${index}`);
      }
    });

    if (this.factions.length < 2) {
      failures.push('At least 2 factions are required.');
      sections.add('factions');
    }

    const usedFactionColors = new Set<string>();
    this.factions.controls.forEach((faction, index) => {
      const color = faction.controls.color.value.toUpperCase();
      if (usedFactionColors.has(color)) {
        failures.push(`Faction ${index + 1} color must be unique.`);
        sections.add('factions');
        sections.add(`faction-item-${index}`);
      }

      usedFactionColors.add(color);
    });

    this.factions.controls.forEach((faction, index) => {
      if (faction.controls.flagSource.value !== 'image') {
        return;
      }

      const id = faction.controls.id.value;
      if (!this.flagImages.has(id) && !this.hasStoredFlagImage(id)) {
        failures.push(`Faction ${index + 1} needs a flag image or the color flag.`);
        sections.add('factions');
        sections.add(`faction-item-${index}`);
      }
    });

    if (this.terrainTypes.length < 1) {
      failures.push('At least 1 terrain type is required.');
      sections.add('terrain');
    }

    const usedTerrainColors = new Set<string>();
    this.terrainTypes.controls.forEach((terrain, index) => {
      const nameMessage = describeControlError(terrain.controls.name, `Terrain type ${index + 1} name`);
      if (nameMessage) {
        failures.push(nameMessage);
        sections.add('terrain');
        sections.add(`terrain-item-${index}`);
      }

      const color = terrain.controls.color.value.toUpperCase();
      if (usedTerrainColors.has(color)) {
        failures.push(`Terrain type ${index + 1} color must be unique.`);
        sections.add('terrain');
        sections.add(`terrain-item-${index}`);
      }

      usedTerrainColors.add(color);

      const namedMissions = terrain.controls.missions.controls.filter(
        (mission) => mission.controls.name.value.trim().length > 0,
      );
      if (namedMissions.length === 0) {
        failures.push(`Terrain type ${index + 1} requires at least one mission.`);
        sections.add('terrain');
        sections.add(`terrain-item-${index}`);
        sections.add(`terrain-missions-${index}`);
      }
    });

    this.structureTypes.controls.forEach((structure, index) => {
      const nameMessage = describeControlError(structure.controls.name, `Structure ${index + 1} name`);
      if (nameMessage) {
        failures.push(nameMessage);
        sections.add('structures');
        sections.add(`structure-item-${index}`);
      }

      if (structure.controls.iconSource.value === 'image') {
        const id = structure.controls.id.value;
        if (!this.structureImages.has(id) && !this.hasStoredStructureImage(id)) {
          failures.push(`Structure ${index + 1} needs a logo image or a built-in icon.`);
          sections.add('structures');
          sections.add(`structure-item-${index}`);
        }
      }

      if (structure.controls.pillagedIconSource.value === 'image') {
        const id = structure.controls.id.value;
        if (!this.structurePillagedImages.has(id) && !this.hasStoredPillagedImage(id)) {
          failures.push(`Structure ${index + 1} needs a pillaged logo image or the built-in pillaged icon.`);
          sections.add('structures');
          sections.add(`structure-item-${index}`);
        }
      }
    });

    const usedItemNames = new Set<string>();
    this.itemObjectiveTypes.controls.forEach((item, index) => {
      const name = item.controls.name.value.trim();
      if (!name) {
        return;
      }

      const nameMessage = describeControlError(item.controls.name, `Item objective ${index + 1} name`);
      if (nameMessage) {
        failures.push(nameMessage);
        sections.add('itemObjectives');
        sections.add(`item-objective-${index}`);
      }

      const key = name.toLowerCase();
      if (usedItemNames.has(key)) {
        failures.push(`Item objective ${index + 1} name must be unique.`);
        sections.add('itemObjectives');
        sections.add(`item-objective-${index}`);
      }

      usedItemNames.add(key);
    });

    const roundLengthError = describeControlError(this.form.controls.roundLengthAmount, 'Round length');
    if (!roundLengthError) {
      const roundLengthMessage = durationRangeMessage(
        'Round length',
        Number(this.form.controls.roundLengthAmount.value),
        this.form.controls.roundLengthUnit.value,
      );
      if (roundLengthMessage) {
        failures.push(roundLengthMessage);
        sections.add('schedule');
      }
    }

    const actionCount = this.actionCount();
    const battleCount = this.phases.controls.filter((phase) => phase.controls.kind.value === 'Battle').length;
    if (actionCount < 1 || battleCount < 1) {
      failures.push('A round must include at least one action and one battle phase.');
      sections.add('schedule');
    }

    this.phases.controls.forEach((phase, index) => {
      const amountMessage = describeControlError(phase.controls.durationAmount, `Round step ${index + 1} length`);
      if (amountMessage) {
        failures.push(amountMessage);
        sections.add('schedule');
        return;
      }

      const rangeMessage = durationRangeMessage(
        `Round step ${index + 1} length`,
        Number(phase.controls.durationAmount.value),
        phase.controls.durationUnit.value,
      );
      if (rangeMessage) {
        failures.push(rangeMessage);
        sections.add('schedule');
      }
    });

    this.allyGroups.controls.forEach((group, index) => {
      if (group.controls.name.value.trim().length === 0) {
        failures.push(`Ally group ${index + 1} name is not filled in.`);
        sections.add('allies');
      }
    });

    this.links.controls.forEach((link, index) => {
      const hasAny = link.controls.label.value.trim().length > 0 || link.controls.url.value.trim().length > 0;
      if (!hasAny) {
        return;
      }

      const labelMessage = describeControlError(link.controls.label, `Link ${index + 1} label`);
      if (!link.controls.label.value.trim()) {
        failures.push(`Link ${index + 1} label is not filled in.`);
        sections.add('links');
      } else if (labelMessage) {
        failures.push(labelMessage);
        sections.add('links');
      }

      const urlMessage = describeControlError(link.controls.url, `Link ${index + 1} URL`);
      if (!link.controls.url.value.trim()) {
        failures.push(`Link ${index + 1} URL is not filled in.`);
        sections.add('links');
      } else if (urlMessage) {
        failures.push(urlMessage);
        sections.add('links');
      }
    });

    const missionNames = new Map<string, string>();
    const owners: { group: TerrainGroup | StructureGroup; section: string; nested: string }[] = [
      ...this.terrainTypes.controls.map((group, index) => ({
        group,
        section: 'terrain',
        nested: `terrain-missions-${index}`,
      })),
      ...this.structureTypes.controls.map((group, index) => ({
        group,
        section: 'structures',
        nested: `structure-missions-${index}`,
      })),
    ];
    for (const owner of owners) {
      for (const mission of owner.group.controls.missions.controls) {
        const name = mission.controls.name.value.trim();
        if (!name) {
          continue;
        }

        const key = name.toLowerCase();
        const id = mission.controls.id.value;
        const existing = missionNames.get(key);
        if (existing && existing !== id) {
          sections.add(owner.section);
          sections.add(owner.nested);
          if (!failures.includes('Mission names must be unique.')) {
            failures.push('Mission names must be unique.');
          }
        } else {
          missionNames.set(key, id);
        }
      }
    }

    if (!this.isEdit() && !this.mapFile) {
      failures.push('A campaign map image is required.');
      sections.add('map');
    }

    return { messages: failures, sections: [...sections] };
  }

  private findMission(missionId: string): MissionGroup | null {
    for (const owner of [...this.terrainTypes.controls, ...this.structureTypes.controls]) {
      const match = owner.controls.missions.controls.find((mission) => mission.controls.id.value === missionId);
      if (match) {
        return match;
      }
    }

    return null;
  }

  private setAllSections(open: boolean): void {
    this.sectionOpen.set(Object.fromEntries(this.allSectionIds().map((id) => [id, open])));
  }

  private allSectionIds(): string[] {
    const ids: string[] = [...TOP_LEVEL_SECTION_IDS];
    this.factions.controls.forEach((_, index) => {
      ids.push(`faction-item-${index}`, `faction-sub-${index}`);
    });
    this.terrainTypes.controls.forEach((_, index) => {
      ids.push(`terrain-item-${index}`, `terrain-missions-${index}`);
    });
    this.structureTypes.controls.forEach((_, index) => {
      ids.push(`structure-item-${index}`, `structure-missions-${index}`);
    });
    this.itemObjectiveTypes.controls.forEach((_, index) => {
      ids.push(`item-objective-${index}`);
    });
    return ids;
  }

  private expandSections(ids: readonly string[]): void {
    this.sectionOpen.update((current) => {
      const next = { ...current };
      for (const id of ids) {
        next[id] = true;
      }

      return next;
    });
  }

  private rememberStoredFiles(campaign: CampaignDetail): void {
    this.storedStructureImages.set(
      new Set(campaign.structureTypes.filter((type) => type.hasImage).map((type) => type.id)),
    );
    this.storedPillagedImages.set(
      new Set(campaign.structureTypes.filter((type) => type.hasPillagedImage).map((type) => type.id)),
    );
    this.storedFlagImages.set(
      new Set(campaign.factions.filter((faction) => faction.hasFlagImage).map((faction) => faction.id)),
    );
    this.storedMissionFiles.set(
      new Set(
        [...campaign.terrainTypes, ...campaign.structureTypes]
          .flatMap((type) => type.missions)
          .filter((mission) => mission.hasFile)
          .map((mission) => mission.id),
      ),
    );
  }

  private removeMissionsFiles(group: TerrainGroup | StructureGroup): void {
    for (const mission of group.controls.missions.controls) {
      this.missionFiles.delete(mission.controls.id.value);
    }
  }

  private revealErrors(messages: readonly string[]): void {
    this.successMessage.set(null);
    this.errorMessages.set([...messages]);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }

  private revealSuccess(): void {
    this.errorMessages.set([]);
    this.successMessage.set(FORM_SAVE_SUCCESS_MESSAGE);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }
}
