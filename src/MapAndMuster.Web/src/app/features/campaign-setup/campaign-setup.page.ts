import { Component, computed, DestroyRef, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, type FormArray, type FormControl, type FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService, readApiErrorMessages, readApiFieldErrors } from '../../core/auth/auth.service';
import { FilterableComboboxComponent } from '../../shared/filterable-combobox/filterable-combobox.component';
import { SaveCampaignPresetDialogComponent } from '../../shared/save-campaign-preset-dialog/save-campaign-preset-dialog.component';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type {
  CampaignDetail,
  CampaignMission,
  CampaignItemObjectiveType,
  CampaignPresetListItem,
  CampaignPrivateObjectiveType,
  CampaignPublicObjectiveType,
  CampaignStructureType,
  CampaignTerrainType,
  ItemObjectiveChoice,
  ItemObjectiveChoiceResult,
  SaveCampaignPayload,
  SaveMissionPayload,
} from '../../core/campaigns/campaign.models';
import { defaultStructureCatalog, defaultTerrainCatalog } from '../../core/campaigns/catalog-defaults';
import { campaignFromPreset, campaignPresetApplyOptions } from '../../core/campaigns/campaign-presets';
import { defaultArmyEscalations } from '../../core/campaigns/army-escalation-defaults';
import {
  HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS,
  HUNT_IN_ESTALIA_SPLIT_FORCE_SUPPLY_PENALTY_IS_PERCENT,
  HUNT_IN_ESTALIA_SPLIT_FORCE_SUPPLY_PENALTY_VALUE,
  huntInEstaliaArmyEscalations,
} from '../../core/campaigns/hunt-in-estalia-defaults';
import {
  FORCE_STATUS_CLEAR_OPTIONS,
  FORCE_STATUS_ENABLE_OPTIONS,
  forceStatusesFromStandardPreset,
  STANDARD_FORCE_STATUSES,
} from '../../core/campaigns/force-status-presets';
import { OLD_WORLD_SPECIAL_RULES, type SpecialRulePreset } from '../../core/campaigns/special-rule-presets';
import { FORM_SAVE_SUCCESS_MESSAGE } from '../../core/forms/form-messages';
import { FormSubmitOverlayService } from '../../core/forms/form-submit-overlay.service';
import { syncDirtyFromBaseline } from '../../core/forms/sync-form-dirty';
import {
  DURATION_UNITS,
  PHASE_KINDS,
  durationRangeMessage,
  maxAmountForUnit,
} from '../../core/campaigns/campaign-schedule';
import {
  compareNames,
  FACTION_COLOR_PALETTE,
  FACTION_PRESETS,
  factionsFromPreset,
  nextUnusedFactionColor,
} from '../../core/campaigns/faction-presets';
import {
  defaultItemObjective,
  ITEM_OBJECTIVE_DEFAULT_COLOR,
  ITEM_OBJECTIVE_SYMBOLS,
  type ItemObjectivePlacement,
  type ItemObjectivePresetItem,
} from '../../core/campaigns/item-objective-presets';
import { STRUCTURE_PRESETS, structureTypesFromPreset } from '../../core/campaigns/structure-presets';
import { TERRAIN_PRESETS, terrainTypesFromPreset } from '../../core/campaigns/terrain-presets';
import { listCountries, listTimeZones, regionsForCountry } from '../../core/location/location';
import { CampaignMapPreviewComponent } from '../../shared/campaign-map-preview/campaign-map-preview.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { MapSymbolComponent } from '../../shared/map-symbol/map-symbol.component';
import { PasswordInputComponent } from '../../shared/password-input/password-input.component';
import { InstantDatePipe } from '../../shared/time/instant-date.pipe';
import { downloadBlob } from '../../core/maps/map-export';
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
type AllyGroupForm = FormGroup<{ id: FormControl<string>; name: FormControl<string>; color: FormControl<string> }>;
type LinkGroup = FormGroup<{ label: FormControl<string>; url: FormControl<string> }>;
type MissionQuestionGroup = FormGroup<{
  id: FormControl<string>;
  prompt: FormControl<string>;
  kind: FormControl<string>;
  battlePoints: FormControl<number>;
  campaignPoints: FormControl<number>;
}>;
type MissionGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  url: FormControl<string>;
  clearFile: FormControl<boolean>;
  resultQuestions: FormArray<MissionQuestionGroup>;
  isAttackerDefender: FormControl<boolean>;
  hasArmyPointsAdvantage: FormControl<boolean>;
  armyPointsAdvantageSide: FormControl<string>;
  armyPointsAdvantageIsPercent: FormControl<boolean>;
  armyPointsAdvantageAmount: FormControl<number>;
  hasSupplyPointsAdvantage: FormControl<boolean>;
  supplyPointsAdvantageSide: FormControl<string>;
  supplyPointsAdvantageAmount: FormControl<number>;
}>;
type FactionGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  color: FormControl<string>;
  requiresSubfaction: FormControl<boolean>;
  allyGroupId: FormControl<string>;
  flagSource: FormControl<'color' | 'image'>;
  clearFlagImage: FormControl<boolean>;
  subfactions: FormArray<NamedGroup>;
  specialRuleIds: FormControl<string[]>;
  subfactionSpecialRuleIds: FormControl<Record<string, string[]>>;
}>;
type TerrainGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  color: FormControl<string>;
  campaignPoints: FormControl<number>;
  isWaterFeature: FormControl<boolean>;
  supplyPoints: FormControl<number>;
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
  campaignPoints: FormControl<number>;
  supplyPoints: FormControl<number>;
  pillageSupplyPoints: FormControl<number>;
  destroySupplyPoints: FormControl<number>;
  missions: FormArray<MissionGroup>;
}>;
type ItemObjectiveGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  isHiddenUntilFound: FormControl<boolean>;
  placement: FormControl<ItemObjectivePlacement>;
  allowOnSpawn: FormControl<boolean>;
  builtinSymbol: FormControl<string>;
  color: FormControl<string>;
  iconSource: FormControl<'symbol' | 'image'>;
  clearImage: FormControl<boolean>;
  campaignPoints: FormControl<number>;
  flavorText: FormControl<string>;
  specialRuleIds: FormControl<string[]>;
  choices: FormArray<ItemChoiceGroup>;
}>;
type ItemChoiceGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  results: FormArray<ItemChoiceResultGroup>;
}>;
type ItemChoiceResultGroup = FormGroup<{
  id: FormControl<string>;
  flavorText: FormControl<string>;
  newStateKey: FormControl<string>;
  destroyItem: FormControl<boolean>;
  replacementItemTypeId: FormControl<string>;
  grantedPrivateObjectiveTypeId: FormControl<string>;
}>;
type PublicObjectiveGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  description: FormControl<string>;
  campaignPoints: FormControl<number>;
}>;
type SpecialRuleGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  text: FormControl<string>;
  effectKey: FormControl<string>;
}>;
type ForceStatusGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  effects: FormControl<string>;
  enableTrigger: FormControl<string>;
  clearTrigger: FormControl<string>;
}>;
type PrivateObjectiveGroup = FormGroup<{
  id: FormControl<string>;
  name: FormControl<string>;
  description: FormControl<string>;
  campaignPoints: FormControl<number>;
  allowPlayer: FormControl<boolean>;
  allowFaction: FormControl<boolean>;
  allowAllyGroup: FormControl<boolean>;
  scoringKind: FormControl<string>;
  automaticKind: FormControl<string>;
  requiredCount: FormControl<number>;
  structureTypeId: FormControl<string>;
  territoryIds: FormControl<string>;
}>;
type PhaseGroup = FormGroup<{
  kind: FormControl<string>;
  durationAmount: FormControl<number>;
  durationUnit: FormControl<string>;
  endPhaseEarlyIfAble: FormControl<boolean>;
}>;
type RoundEscalationGroup = FormGroup<{
  roundNumber: FormControl<number>;
  maxArmyPoints: FormControl<number>;
  freeSupplyPoints: FormControl<number>;
  freeCharacterCount: FormControl<number>;
}>;

const TOP_LEVEL_SECTION_IDS = [
  'details',
  'schedule',
  'visibility',
  'specialRules',
  'forceStatuses',
  'publicObjectives',
  'privateObjectives',
  'allies',
  'factions',
  'missions',
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
    SaveCampaignPresetDialogComponent,
    IconComponent,
    InstantDatePipe,
    MapSymbolComponent,
    CampaignMapPreviewComponent,
    PasswordInputComponent,
  ],
  templateUrl: './campaign-setup.page.html',
  styleUrl: './campaign-setup.page.css',
})
export class CampaignSetupPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly auth = inject(AuthService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formAlert = viewChild<ElementRef<HTMLElement>>('formAlert');
  private readonly presetUpload = viewChild<ElementRef<HTMLInputElement>>('presetUpload');

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessages = signal<string[]>([]);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly lastSavedAtUtc = signal<string | null>(null);
  protected readonly saveStatus = signal<'success' | 'failure' | null>(null);
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
  private readonly itemObjectiveImages = new Map<string, File>();
  private readonly flagImages = new Map<string, File>();
  private readonly missionFiles = new Map<string, File>();
  private readonly storedStructureImages = signal<ReadonlySet<string>>(new Set());
  private readonly storedPillagedImages = signal<ReadonlySet<string>>(new Set());
  private readonly storedItemObjectiveImages = signal<ReadonlySet<string>>(new Set());
  private readonly storedFlagImages = signal<ReadonlySet<string>>(new Set());
  private readonly storedMissionFiles = signal<ReadonlySet<string>>(new Set());
  private pendingPresetMapId: string | null = null;
  private presetsLoaded = false;
  private hydrating = false;
  private loadedDetail: CampaignDetail | null = null;
  private savedFormValue: unknown = null;
  private readonly formTick = signal(0);
  private readonly pendingUploadsTick = signal(0);
  protected readonly hasUnsavedChanges = computed(() => {
    this.formTick();
    this.pendingUploadsTick();
    this.catalogTick();
    return this.form.dirty || this.hasPendingUploads();
  });

  protected readonly timeZones = listTimeZones();
  protected readonly durationUnits = DURATION_UNITS;
  protected readonly phaseKinds = PHASE_KINDS;
  protected readonly factionPresets = FACTION_PRESETS;
  protected readonly terrainPresets = TERRAIN_PRESETS;
  protected readonly structurePresets = STRUCTURE_PRESETS;
  protected readonly savedPresets = signal<CampaignPresetListItem[]>([]);
  protected readonly savePresetOpen = signal(false);
  protected readonly isAdministrator = computed(() => this.auth.currentUser()?.isAdministrator === true);
  protected readonly allCampaignPresets = computed(() => campaignPresetApplyOptions(this.savedPresets()));
  protected readonly forceStatusEnableOptions = FORCE_STATUS_ENABLE_OPTIONS;
  protected readonly forceStatusClearOptions = FORCE_STATUS_CLEAR_OPTIONS;
  protected readonly structureSymbols = STRUCTURE_TYPES;
  protected readonly itemObjectiveSymbols = ITEM_OBJECTIVE_SYMBOLS;
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
  protected readonly specialRulePresetPick = this.formBuilder.nonNullable.control('');
  protected readonly forceStatusPresetPick = this.formBuilder.nonNullable.control('');
  private readonly catalogTick = signal(0);
  private readonly assignmentPicks = new Map<string, FormControl<string>>();

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
    allyGroups: this.formBuilder.array<AllyGroupForm>([]),
    links: this.formBuilder.array<LinkGroup>([]),
    terrainTypes: this.formBuilder.array<TerrainGroup>(this.createDefaultTerrainGroups()),
    structureTypes: this.formBuilder.array<StructureGroup>(this.createDefaultStructureGroups()),
    itemObjectiveTypes: this.formBuilder.array<ItemObjectiveGroup>([]),
    specialRules: this.formBuilder.array<SpecialRuleGroup>([]),
    missions: this.formBuilder.array<MissionGroup>([]),
    forceStatuses: this.formBuilder.array<ForceStatusGroup>([]),
    privateObjectiveTypes: this.formBuilder.array<PrivateObjectiveGroup>([]),
    publicObjectiveTypes: this.formBuilder.array<PublicObjectiveGroup>([]),
    pointsPerBattleWon: [2, [minValue(0), maxValue(999)]],
    pointsPerBattleDraw: [1, [minValue(0), maxValue(999)]],
    useDifferentialBattleScoring: [true],
    differentialMultiplier: [1, [minValue(0.01), maxValue(999)]],
    differentialMinimum: [0, [minValue(-999), maxValue(999)]],
    differentialMaximum: [10, [minValue(-999), maxValue(999)]],
    allowNegativeDifferential: [false],
    mostTerritoriesCampaignPoints: [0, [minValue(0), maxValue(999)]],
    longestTerritoryChainCampaignPoints: [0, [minValue(0), maxValue(999)]],
    mostBattlesWonCampaignPoints: [0, [minValue(0), maxValue(999)]],
    mostStructurePointsCampaignPoints: [0, [minValue(0), maxValue(999)]],
    pointsPerTerritoryCampaignPoints: [0, [minValue(0), maxValue(999)]],
    alliedRelicControlCampaignPoints: [0, [minValue(0), maxValue(999)]],
    splitForceSupplyPenaltyPercent: [HUNT_IN_ESTALIA_SPLIT_FORCE_SUPPLY_PENALTY_VALUE, [minValue(0), maxValue(100)]],
    splitForceSupplyPenaltyIsPercent: [HUNT_IN_ESTALIA_SPLIT_FORCE_SUPPLY_PENALTY_IS_PERCENT],
    alwaysAskGeneralKill: [true],
    alwaysAskSupplyLineDestroyed: [true],
    generalKillCampaignPoints: [1, [minValue(0), maxValue(999)]],
    supplyLineDestroyedCampaignPoints: [1, [minValue(0), maxValue(999)]],
    roundEscalations: this.formBuilder.array<RoundEscalationGroup>(
      defaultArmyEscalations(8).map((row) => this.createRoundEscalationGroup(row)),
    ),
    phases: this.formBuilder.array<PhaseGroup>([
      this.createPhaseGroup('Action', 3, 'Days'),
      this.createPhaseGroup('Action', 3, 'Days'),
      this.createPhaseGroup('Battle', 1, 'Days'),
    ]),
  });
  protected readonly isPrivate = toSignal(this.form.controls.isPrivate.valueChanges, {
    initialValue: this.form.controls.isPrivate.value,
  });
  protected readonly useDifferential = toSignal(this.form.controls.useDifferentialBattleScoring.valueChanges, {
    initialValue: this.form.controls.useDifferentialBattleScoring.value,
  });
  protected readonly splitForcePenaltyIsPercent = toSignal(
    this.form.controls.splitForceSupplyPenaltyIsPercent.valueChanges,
    {
      initialValue: this.form.controls.splitForceSupplyPenaltyIsPercent.value,
    },
  );
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

    this.form.controls.roundCount.valueChanges.pipe(takeUntilDestroyed()).subscribe((count) => {
      this.syncRoundEscalations(Number(count) || 0);
    });
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      if (this.hydrating) {
        return;
      }

      this.syncFormDirty();
      this.formTick.update((value) => value + 1);
    });

    this.destroyRef.onDestroy(() => this.revokeMapObjectUrl());
  }

  protected get factions(): FormArray<FactionGroup> {
    return this.form.controls.factions;
  }

  protected get allyGroups(): FormArray<AllyGroupForm> {
    return this.form.controls.allyGroups;
  }

  protected get links(): FormArray<LinkGroup> {
    return this.form.controls.links;
  }

  protected get phases(): FormArray<PhaseGroup> {
    return this.form.controls.phases;
  }

  protected get roundEscalations(): FormArray<RoundEscalationGroup> {
    return this.form.controls.roundEscalations;
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

  protected get specialRules(): FormArray<SpecialRuleGroup> {
    return this.form.controls.specialRules;
  }

  protected get missions(): FormArray<MissionGroup> {
    return this.form.controls.missions;
  }

  protected get forceStatuses(): FormArray<ForceStatusGroup> {
    return this.form.controls.forceStatuses;
  }

  protected get privateObjectiveTypes(): FormArray<PrivateObjectiveGroup> {
    return this.form.controls.privateObjectiveTypes;
  }

  protected get publicObjectiveTypes(): FormArray<PublicObjectiveGroup> {
    return this.form.controls.publicObjectiveTypes;
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

  protected questionsOf(mission: MissionGroup): FormArray<MissionQuestionGroup> {
    return mission.controls.resultQuestions;
  }

  protected addMissionQuestion(mission: MissionGroup): void {
    if (mission.controls.resultQuestions.length >= 20) {
      return;
    }

    mission.controls.resultQuestions.push(this.createMissionQuestionGroup());
  }

  protected removeMissionQuestion(mission: MissionGroup, index: number): void {
    mission.controls.resultQuestions.removeAt(index);
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

  protected discardUnsavedChanges(): void {
    if (!this.isEdit() || !this.loadedDetail || this.saving() || this.loading()) {
      return;
    }

    this.clearPendingUploads();
    this.hydrateFromDetail(this.loadedDetail);
    this.captureBaseline();
    this.successMessage.set(null);
    this.errorMessages.set([]);
    this.saveStatus.set(null);
  }

  protected sectionHeaderDirty(id: string): boolean {
    const dirty = this.sectionHasDirty(id);
    if (id === 'map') {
      return dirty;
    }

    return !this.isOpen(id) && dirty;
  }

  protected isMapDirty(): boolean {
    this.pendingUploadsTick();
    return this.mapFile !== null || this.pendingPresetMapId !== null;
  }

  protected hasPendingFlag(factionId: string): boolean {
    this.pendingUploadsTick();
    return this.flagImages.has(factionId);
  }

  protected hasPendingStructureImage(structureId: string): boolean {
    this.pendingUploadsTick();
    return this.structureImages.has(structureId);
  }

  protected hasPendingPillagedImage(structureId: string): boolean {
    this.pendingUploadsTick();
    return this.structurePillagedImages.has(structureId);
  }

  protected hasPendingItemImage(itemId: string): boolean {
    this.pendingUploadsTick();
    return this.itemObjectiveImages.has(itemId);
  }

  protected hasPendingMissionFile(missionId: string): boolean {
    this.pendingUploadsTick();
    return this.missionFiles.has(missionId);
  }

  protected expandAllSections(): void {
    this.setAllSections(true);
  }

  protected collapseAllSections(): void {
    this.setAllSections(false);
  }

  protected allyMembers(groupId: string): string {
    return this.factions.controls
      .filter((faction) => faction.controls.allyGroupId.value === groupId && faction.controls.name.value.trim())
      .map((faction) => faction.controls.name.value.trim())
      .sort(compareNames)
      .join(', ');
  }

  protected unalignedFactions(): string {
    return this.factions.controls
      .filter((faction) => !faction.controls.allyGroupId.value.trim() && faction.controls.name.value.trim())
      .map((faction) => faction.controls.name.value.trim())
      .sort(compareNames)
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

    this.ensureSpecialRulesFromNames([
      ...factions.flatMap((faction) => [...(faction.specialRuleNames ?? [])]),
      ...factions.flatMap((faction) =>
        Object.values(faction.subfactionSpecialRules ?? {}).flatMap((names) => [...names]),
      ),
    ]);
    const ruleIds = this.specialRuleIdsByName();
    this.replaceArray(
      this.factions,
      factions.map((faction) =>
        this.createFactionGroup(faction.name, '', faction.subfactions, {
          color: faction.color,
          requiresSubfaction: faction.requiresSubfaction,
          specialRuleIds: (faction.specialRuleNames ?? [])
            .map((name) => ruleIds.get(name))
            .filter((id): id is string => !!id),
          subfactionSpecialRuleIds: this.subfactionRuleIdsFromNames(faction.subfactionSpecialRules, ruleIds),
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
      types.map((entry) => this.createTerrainGroup(undefined, entry.name, entry.color, '', 0, entry.isWaterFeature)),
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
    const selected = this.campaignPresetId.value;
    if (selected.startsWith('saved:')) {
      void this.applySavedCampaignPreset(selected.slice('saved:'.length));
      return;
    }

    const copy = campaignFromPreset(selected);
    if (!copy) {
      this.revealErrors(['Select a campaign preset before adding it.']);
      return;
    }

    if (!this.form.controls.name.value.trim()) {
      this.form.controls.name.setValue(copy.name);
    }

    this.replaceArray(
      this.specialRules,
      copy.specialRules.map((rule) =>
        this.createSpecialRuleGroup(undefined, rule.name, rule.description, rule.effectKey),
      ),
    );
    this.replaceArray(
      this.forceStatuses,
      copy.forceStatuses.map((status) =>
        this.createForceStatusGroup(undefined, status.name, status.effects, status.enableTrigger, status.clearTrigger),
      ),
    );
    this.bumpCatalog();
    const ruleIds = this.specialRuleIdsByName();
    this.replaceArray(
      this.factions,
      copy.factions.map((faction) =>
        this.createFactionGroup(faction.name, '', faction.subfactions, {
          color: faction.color,
          requiresSubfaction: faction.requiresSubfaction,
          specialRuleIds: (faction.specialRuleNames ?? [])
            .map((name) => ruleIds.get(name))
            .filter((id): id is string => !!id),
          subfactionSpecialRuleIds: this.subfactionRuleIdsFromNames(faction.subfactionSpecialRules, ruleIds),
        }),
      ),
    );
    this.replaceArray(
      this.terrainTypes,
      copy.terrainTypes.map((entry) =>
        this.createTerrainGroup(undefined, entry.name, entry.color, '', 0, entry.isWaterFeature),
      ),
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
    this.applyBattleScoringDefaults();
  }

  private async applySavedCampaignPreset(presetId: string): Promise<void> {
    this.errorMessages.set([]);
    try {
      const preset = await this.campaignsApi.getPreset(presetId);
      if (!this.form.controls.name.value.trim()) {
        this.form.controls.name.setValue(preset.name);
      }

      this.applyCatalogFromDetail(preset);
      if (this.isEdit() && this.campaignId() && preset.hasMap) {
        const detail = await this.campaignsApi.applyPresetMap(this.campaignId()!, presetId, this.revision);
        this.revision = detail.revision;
        this.hasExistingMap.set(detail.hasMap);
        this.setStoredMapPreview(detail.id, detail.revision, detail.hasMap);
      } else if (preset.hasMap) {
        this.pendingPresetMapId = presetId;
        this.bumpPendingUploads();
      }
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Unable to apply this campaign preset.'));
    }
  }

  private applyCatalogFromDetail(campaign: CampaignDetail): void {
    this.replaceArray(
      this.specialRules,
      (campaign.specialRules ?? []).map((rule) =>
        this.createSpecialRuleGroup(rule.id, rule.name, rule.text, rule.effectKey ?? undefined),
      ),
    );
    this.replaceArray(
      this.forceStatuses,
      (campaign.forceStatuses ?? []).map((status) =>
        this.createForceStatusGroup(status.id, status.name, status.effects, status.enableTrigger, status.clearTrigger),
      ),
    );
    this.bumpCatalog();
    this.replaceArray(
      this.factions,
      campaign.factions.map((faction) =>
        this.createFactionGroup(faction.name, this.allyGroupIdFor(campaign, faction), faction.subfactions, {
          id: faction.id,
          color: faction.color,
          requiresSubfaction: faction.requiresSubfaction,
          hasFlagImage: faction.hasFlagImage,
          specialRuleIds: faction.specialRuleIds ?? [],
          subfactionSpecialRuleIds: this.subfactionRuleIdsFromDetail(faction.subfactionSpecialRules),
        }),
      ),
    );
    this.replaceArray(
      this.allyGroups,
      campaign.allyGroups.map((group) => this.createAllyGroup(group.id, group.name, group.color)),
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
      (campaign.itemObjectiveTypes ?? []).map((item) => this.createItemObjectiveGroupFromDetail(item)),
    );
    this.replaceArray(
      this.publicObjectiveTypes,
      (campaign.publicObjectiveTypes ?? []).map((item) => this.createPublicObjectiveGroup(item, item.id)),
    );
    this.replaceArray(
      this.privateObjectiveTypes,
      (campaign.privateObjectiveTypes ?? []).map((item) => this.createPrivateObjectiveGroup(item, item.id)),
    );
    this.replaceArray(
      this.missions,
      this.catalogMissionsFrom(campaign).map((mission) => this.createMissionGroupFromDetail(mission)),
    );
    this.form.controls.playerCount.setValue(campaign.playerSlotCount);
    this.form.controls.roundCount.setValue(campaign.roundCount);
    this.form.controls.roundLengthAmount.setValue(campaign.roundLengthAmount);
    this.form.controls.roundLengthUnit.setValue(campaign.roundLengthUnit);
    this.form.controls.pointsPerBattleWon.setValue(campaign.pointsPerBattleWon ?? 2);
    this.form.controls.pointsPerBattleDraw.setValue(campaign.pointsPerBattleDraw ?? 1);
    this.form.controls.useDifferentialBattleScoring.setValue(campaign.useDifferentialBattleScoring ?? true);
    this.form.controls.differentialMultiplier.setValue(campaign.differentialMultiplier ?? 1);
    this.form.controls.differentialMinimum.setValue(campaign.differentialMinimum ?? 0);
    this.form.controls.differentialMaximum.setValue(campaign.differentialMaximum ?? 10);
    this.form.controls.allowNegativeDifferential.setValue(campaign.allowNegativeDifferential ?? false);
    this.form.controls.mostTerritoriesCampaignPoints.setValue(campaign.mostTerritoriesCampaignPoints ?? 0);
    this.form.controls.longestTerritoryChainCampaignPoints.setValue(campaign.longestTerritoryChainCampaignPoints ?? 0);
    this.form.controls.mostBattlesWonCampaignPoints.setValue(campaign.mostBattlesWonCampaignPoints ?? 0);
    this.form.controls.mostStructurePointsCampaignPoints.setValue(campaign.mostStructurePointsCampaignPoints ?? 0);
    this.form.controls.pointsPerTerritoryCampaignPoints.setValue(campaign.pointsPerTerritoryCampaignPoints ?? 0);
    this.form.controls.alliedRelicControlCampaignPoints.setValue(campaign.alliedRelicControlCampaignPoints ?? 0);
    this.applySplitForcePenalty(campaign);
    this.form.controls.alwaysAskGeneralKill.setValue(campaign.alwaysAskGeneralKill !== false);
    this.form.controls.alwaysAskSupplyLineDestroyed.setValue(campaign.alwaysAskSupplyLineDestroyed !== false);
    this.form.controls.generalKillCampaignPoints.setValue(campaign.generalKillCampaignPoints ?? 1);
    this.form.controls.supplyLineDestroyedCampaignPoints.setValue(campaign.supplyLineDestroyedCampaignPoints ?? 1);
    this.applyRoundEscalations(campaign.roundEscalations, campaign.roundCount);
    this.replaceArray(
      this.phases,
      campaign.phases.map((phase) =>
        this.createPhaseGroup(
          phase.kind,
          phase.durationAmount,
          phase.durationUnit,
          phase.endPhaseEarlyIfAble !== false,
        ),
      ),
    );
    this.refreshPhases();
  }

  private applyBattleScoringDefaults(): void {
    this.form.controls.pointsPerBattleWon.setValue(2);
    this.form.controls.pointsPerBattleDraw.setValue(1);
    this.form.controls.useDifferentialBattleScoring.setValue(true);
    this.form.controls.differentialMultiplier.setValue(1);
    this.form.controls.differentialMinimum.setValue(0);
    this.form.controls.differentialMaximum.setValue(10);
    this.form.controls.allowNegativeDifferential.setValue(false);
    this.form.controls.splitForceSupplyPenaltyPercent.setValue(HUNT_IN_ESTALIA_SPLIT_FORCE_SUPPLY_PENALTY_VALUE);
    this.form.controls.splitForceSupplyPenaltyIsPercent.setValue(HUNT_IN_ESTALIA_SPLIT_FORCE_SUPPLY_PENALTY_IS_PERCENT);
    this.form.controls.alwaysAskGeneralKill.setValue(true);
    this.form.controls.alwaysAskSupplyLineDestroyed.setValue(true);
    this.form.controls.generalKillCampaignPoints.setValue(1);
    this.form.controls.supplyLineDestroyedCampaignPoints.setValue(1);
    this.replaceArray(
      this.roundEscalations,
      huntInEstaliaArmyEscalations(Number(this.form.controls.roundCount.value) || 8).map((row) =>
        this.createRoundEscalationGroup(row),
      ),
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
      faction.controls.allyGroupId.setValue('');
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
    this.allyGroups.push(this.createAllyGroup());
  }

  protected removeAllyGroup(index: number): void {
    const id = this.allyGroups.at(index).controls.id.value;
    this.allyGroups.removeAt(index);
    for (const faction of this.factions.controls) {
      if (faction.controls.allyGroupId.value === id) {
        faction.controls.allyGroupId.setValue('');
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
    this.bumpPendingUploads();
  }

  protected addItemObjective(): void {
    if (this.itemObjectiveTypes.length >= 50) {
      return;
    }

    this.itemObjectiveTypes.push(this.createItemObjectiveGroup());
  }

  protected removeItemObjective(index: number): void {
    const id = this.itemObjectiveTypes.at(index).controls.id.value;
    this.itemObjectiveImages.delete(id);
    this.itemObjectiveTypes.removeAt(index);
    this.bumpPendingUploads();
  }

  protected addPublicObjective(): void {
    if (this.publicObjectiveTypes.length >= 50) {
      return;
    }

    this.publicObjectiveTypes.push(this.createPublicObjectiveGroup());
  }

  protected removePublicObjective(index: number): void {
    this.publicObjectiveTypes.removeAt(index);
  }

  protected addSpecialRule(): void {
    if (this.specialRules.length >= 80) {
      return;
    }

    this.specialRules.push(this.createSpecialRuleGroup());
    this.bumpCatalog();
  }

  protected addPickedSpecialRule(): void {
    const name = this.specialRulePresetPick.value.trim();
    if (!name || this.specialRules.length >= 80) {
      return;
    }

    if (this.findSpecialRuleByName(name)) {
      this.specialRulePresetPick.setValue('');
      return;
    }

    const preset = this.presetSpecialRule(name);
    this.specialRules.push(
      this.createSpecialRuleGroup(undefined, preset?.name ?? name, preset?.description ?? '', preset?.effectKey),
    );
    this.specialRulePresetPick.setValue('');
    this.bumpCatalog();
  }

  protected specialRuleNameOptions(index: number): string[] {
    this.catalogTick();
    const current = this.specialRules.at(index).controls.name.value.trim().toLowerCase();
    const used = new Set(
      this.specialRules.controls
        .filter((_, ruleIndex) => ruleIndex !== index)
        .map((rule) => rule.controls.name.value.trim().toLowerCase())
        .filter((name) => name.length > 0),
    );
    return OLD_WORLD_SPECIAL_RULES.filter(
      (rule) => !used.has(rule.name.toLowerCase()) || rule.name.toLowerCase() === current,
    ).map((rule) => rule.name);
  }

  protected availableSpecialRulePresetNames(): string[] {
    this.catalogTick();
    const used = new Set(
      this.specialRules.controls.map((rule) => rule.controls.name.value.trim().toLowerCase()).filter((name) => name),
    );
    return OLD_WORLD_SPECIAL_RULES.filter((rule) => !used.has(rule.name.toLowerCase())).map((rule) => rule.name);
  }

  protected pickControl(ownerId: string): FormControl<string> {
    const existing = this.assignmentPicks.get(ownerId);
    if (existing) {
      return existing;
    }

    const control = this.formBuilder.nonNullable.control('');
    this.assignmentPicks.set(ownerId, control);
    return control;
  }

  protected assignedSpecialRules(control: FormControl<string[]>): { id: string; name: string }[] {
    this.catalogTick();
    const names = new Map(
      this.specialRules.controls.map((rule) => [rule.controls.id.value, rule.controls.name.value.trim()]),
    );
    return control.value.map((id) => ({ id, name: names.get(id) ?? '' })).filter((rule) => rule.name.length > 0);
  }

  protected assignableSpecialRuleNames(control: FormControl<string[]>): string[] {
    this.catalogTick();
    const assigned = new Set(this.assignedSpecialRules(control).map((rule) => rule.name.toLowerCase()));
    const catalog = this.specialRules.controls
      .map((rule) => rule.controls.name.value.trim())
      .filter((name) => name.length > 0 && !assigned.has(name.toLowerCase()));
    const presets = OLD_WORLD_SPECIAL_RULES.map((rule) => rule.name).filter(
      (name) =>
        !assigned.has(name.toLowerCase()) && !catalog.some((entry) => entry.toLowerCase() === name.toLowerCase()),
    );
    return [...catalog, ...presets];
  }

  protected assignSpecialRuleByName(control: FormControl<string[]>, ownerId: string): void {
    const name = this.pickControl(ownerId).value.trim();
    if (!name) {
      return;
    }

    let rule = this.findSpecialRuleByName(name);
    if (!rule) {
      if (this.specialRules.length >= 80) {
        return;
      }

      const preset = this.presetSpecialRule(name);
      rule = this.createSpecialRuleGroup(undefined, preset?.name ?? name, preset?.description ?? '', preset?.effectKey);
      this.specialRules.push(rule);
    }

    const id = rule.controls.id.value;
    if (!control.value.includes(id)) {
      control.setValue([...control.value, id]);
    }

    this.pickControl(ownerId).setValue('');
    this.bumpCatalog();
  }

  protected removeAssignedSpecialRule(control: FormControl<string[]>, ruleId: string): void {
    control.setValue(control.value.filter((id) => id !== ruleId));
    this.bumpCatalog();
  }

  protected removeSpecialRule(index: number): void {
    const id = this.specialRules.at(index).controls.id.value;
    this.specialRules.removeAt(index);
    this.dropAssignedSpecialRule(id);
    this.bumpCatalog();
  }

  protected applyStandardForceStatuses(): void {
    this.replaceArray(
      this.forceStatuses,
      forceStatusesFromStandardPreset().map((status) =>
        this.createForceStatusGroup(undefined, status.name, status.effects, status.enableTrigger, status.clearTrigger),
      ),
    );
  }

  protected addForceStatus(): void {
    if (this.forceStatuses.length >= 20) {
      return;
    }

    this.forceStatuses.push(this.createForceStatusGroup());
  }

  protected addPickedForceStatus(): void {
    const name = this.forceStatusPresetPick.value.trim();
    if (!name || this.forceStatuses.length >= 20) {
      return;
    }

    if (
      this.forceStatuses.controls.some(
        (status) => status.controls.name.value.trim().toLowerCase() === name.toLowerCase(),
      )
    ) {
      this.forceStatusPresetPick.setValue('');
      return;
    }

    const preset = STANDARD_FORCE_STATUSES.find((status) => status.name.toLowerCase() === name.toLowerCase());
    this.forceStatuses.push(
      this.createForceStatusGroup(
        undefined,
        preset?.name ?? name,
        preset?.effects ?? '',
        preset?.enableTrigger ?? 'Hold',
        preset?.clearTrigger ?? 'Hold',
      ),
    );
    this.forceStatusPresetPick.setValue('');
  }

  protected availableForceStatusPresetNames(): string[] {
    const used = new Set(
      this.forceStatuses.controls
        .map((status) => status.controls.name.value.trim().toLowerCase())
        .filter((name) => name),
    );
    return STANDARD_FORCE_STATUSES.filter((status) => !used.has(status.name.toLowerCase())).map(
      (status) => status.name,
    );
  }

  protected removeForceStatus(index: number): void {
    this.forceStatuses.removeAt(index);
  }

  protected addPrivateObjective(): void {
    if (this.privateObjectiveTypes.length >= 50) {
      return;
    }

    this.privateObjectiveTypes.push(this.createPrivateObjectiveGroup());
  }

  protected removePrivateObjective(index: number): void {
    this.privateObjectiveTypes.removeAt(index);
  }

  protected addItemChoice(item: ItemObjectiveGroup): void {
    if (item.controls.choices.length >= 10) {
      return;
    }

    item.controls.choices.push(this.createItemChoiceGroup());
  }

  protected removeItemChoice(item: ItemObjectiveGroup, index: number): void {
    item.controls.choices.removeAt(index);
  }

  protected addItemChoiceResult(choice: ItemChoiceGroup): void {
    if (choice.controls.results.length >= 12) {
      return;
    }

    choice.controls.results.push(this.createItemChoiceResultGroup());
  }

  protected removeItemChoiceResult(choice: ItemChoiceGroup, index: number): void {
    choice.controls.results.removeAt(index);
  }

  protected addMission(group: TerrainGroup | StructureGroup): void {
    if (group.controls.missions.length >= 20) {
      return;
    }

    group.controls.missions.push(this.createMissionGroup());
  }

  protected addCatalogMission(): void {
    if (this.missions.length >= 80) {
      return;
    }

    this.missions.push(this.createMissionGroup());
  }

  protected removeCatalogMission(index: number): void {
    const missionId = this.missions.at(index).controls.id.value;
    this.missionFiles.delete(missionId);
    this.missions.removeAt(index);
    this.bumpPendingUploads();
    for (const owner of [...this.terrainTypes.controls, ...this.structureTypes.controls]) {
      const attached = owner.controls.missions.controls.findIndex((mission) => mission.controls.id.value === missionId);
      if (attached >= 0) {
        owner.controls.missions.removeAt(attached);
      }
    }
  }

  protected catalogMissionNames(): string[] {
    return this.missions.controls
      .map((mission) => mission.controls.name.value.trim())
      .filter((name) => name.length > 0)
      .sort((left, right) => left.localeCompare(right));
  }

  protected isCatalogMission(mission: MissionGroup): boolean {
    const id = mission.controls.id.value;
    return this.missions.controls.some((item) => item.controls.id.value === id);
  }

  protected onAttachedMissionName(group: TerrainGroup | StructureGroup, missionIndex: number, name: string): void {
    const mission = group.controls.missions.at(missionIndex);
    const trimmed = name.trim();
    mission.controls.name.setValue(name);
    const match = this.missions.controls.find(
      (item) => item.controls.name.value.trim().toLowerCase() === trimmed.toLowerCase(),
    );
    if (match) {
      mission.controls.id.setValue(match.controls.id.value);
      mission.controls.url.setValue(match.controls.url.value);
      return;
    }

    const catalogIds = new Set(this.missions.controls.map((item) => item.controls.id.value));
    if (trimmed.length > 0 && catalogIds.has(mission.controls.id.value)) {
      mission.controls.id.setValue(this.newId());
      mission.controls.url.setValue('');
      this.replaceArray(mission.controls.resultQuestions, []);
    }
  }

  protected setPillagedIconSource(structure: StructureGroup, source: 'symbol' | 'image'): void {
    structure.controls.pillagedIconSource.setValue(source);
    if (source === 'symbol') {
      this.structurePillagedImages.delete(structure.controls.id.value);
      this.bumpPendingUploads();
      structure.controls.clearPillagedImage.setValue(true);
    } else {
      structure.controls.clearPillagedImage.setValue(false);
    }
  }

  protected setIconSource(structure: StructureGroup, source: 'symbol' | 'image'): void {
    structure.controls.iconSource.setValue(source);
    if (source === 'symbol') {
      this.structureImages.delete(structure.controls.id.value);
      this.bumpPendingUploads();
      structure.controls.clearImage.setValue(true);
      if (!structure.controls.builtinSymbol.value) {
        structure.controls.builtinSymbol.setValue(this.structureSymbols[0].id);
      }
    } else {
      structure.controls.clearImage.setValue(false);
    }
  }

  protected setItemIconSource(item: ItemObjectiveGroup, source: 'symbol' | 'image'): void {
    item.controls.iconSource.setValue(source);
    if (source === 'symbol') {
      this.itemObjectiveImages.delete(item.controls.id.value);
      this.bumpPendingUploads();
      item.controls.clearImage.setValue(true);
      if (!item.controls.builtinSymbol.value) {
        item.controls.builtinSymbol.setValue('Crown');
      }
    } else {
      item.controls.clearImage.setValue(false);
    }
  }

  protected setFlagSource(faction: FactionGroup, source: 'color' | 'image'): void {
    faction.controls.flagSource.setValue(source);
    if (source === 'color') {
      this.flagImages.delete(faction.controls.id.value);
      this.bumpPendingUploads();
      faction.controls.clearFlagImage.setValue(true);
    } else {
      faction.controls.clearFlagImage.setValue(false);
    }
  }

  protected removeMission(group: TerrainGroup | StructureGroup, index: number): void {
    const missionId = group.controls.missions.at(index).controls.id.value;
    group.controls.missions.removeAt(index);
    const stillUsed =
      this.missions.controls.some((mission) => mission.controls.id.value === missionId) ||
      [...this.terrainTypes.controls, ...this.structureTypes.controls].some((owner) =>
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
    this.bumpPendingUploads();
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
      this.bumpPendingUploads();
      const group = this.structureTypes.controls.find((item) => item.controls.id.value === structureId);
      group?.controls.clearImage.setValue(false);
    }
  }

  protected onItemObjectiveImageSelected(itemId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file) {
      this.itemObjectiveImages.set(itemId, file);
      this.bumpPendingUploads();
      const group = this.itemObjectiveTypes.controls.find((item) => item.controls.id.value === itemId);
      group?.controls.clearImage.setValue(false);
    }
  }

  protected onStructurePillagedImageSelected(structureId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file) {
      this.structurePillagedImages.set(structureId, file);
      this.bumpPendingUploads();
      const group = this.structureTypes.controls.find((item) => item.controls.id.value === structureId);
      group?.controls.clearPillagedImage.setValue(false);
    }
  }

  protected onFlagImageSelected(factionId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (file) {
      this.flagImages.set(factionId, file);
      this.bumpPendingUploads();
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
      this.bumpPendingUploads();
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

  protected hasStoredItemObjectiveImage(itemId: string): boolean {
    return this.storedItemObjectiveImages().has(itemId);
  }

  protected itemObjectiveImageUrl(itemId: string): string | null {
    const campaignId = this.campaignId();
    if (!campaignId || !this.hasStoredItemObjectiveImage(itemId)) {
      return null;
    }

    return this.campaignsApi.itemObjectiveImageUrl(campaignId, itemId, this.revision);
  }

  protected pendingItemObjectiveImageName(itemId: string): string | null {
    return this.itemObjectiveImages.get(itemId)?.name ?? null;
  }

  protected async ensureSavedPresets(): Promise<void> {
    if (this.presetsLoaded) {
      return;
    }

    this.presetsLoaded = true;
    try {
      this.savedPresets.set(await this.campaignsApi.listPresets());
    } catch {
      this.presetsLoaded = false;
    }
  }

  protected openSavePresetDialog(): void {
    if (!this.isAdministrator()) {
      return;
    }

    this.savePresetOpen.set(true);
  }

  protected closeSavePresetDialog(): void {
    this.savePresetOpen.set(false);
  }

  protected openPresetUpload(): void {
    if (!this.isAdministrator()) {
      return;
    }

    this.presetUpload()?.nativeElement.click();
  }

  protected async downloadPreset(): Promise<void> {
    if (!this.isAdministrator() || !this.isEdit()) {
      return;
    }

    this.saving.set(true);
    this.errorMessages.set([]);
    try {
      const downloaded = await this.overlay.run(async () => {
        const persisted = await this.persistCampaignCore();
        if (!persisted) {
          return null;
        }

        this.applyPersistedCampaign(persisted);
        const file = await this.campaignsApi.downloadCampaignPresetPackage(persisted.detail.id);
        downloadBlob(file.blob, file.filename);
        return file;
      });
      if (!downloaded) {
        return;
      }

      this.revealSuccess('Downloaded campaign preset.');
    } catch (error: unknown) {
      this.serverFields.set(new Set(readApiFieldErrors(error)));
      this.revealErrors(readApiErrorMessages(error, 'Unable to download the campaign preset.'));
    } finally {
      this.saving.set(false);
    }
  }

  protected async uploadPreset(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!this.isAdministrator() || !file) {
      return;
    }

    this.saving.set(true);
    this.errorMessages.set([]);
    try {
      const imported = await this.overlay.run(async () => this.campaignsApi.importPresetPackage(file));
      this.presetsLoaded = false;
      await this.ensureSavedPresets();
      this.revealSuccess(`Imported preset ${imported.name}. Apply it with Add preset.`);
    } catch (error: unknown) {
      this.serverFields.set(new Set(readApiFieldErrors(error)));
      this.revealErrors(readApiErrorMessages(error, 'Unable to upload the campaign preset.'));
    } finally {
      this.saving.set(false);
    }
  }

  protected async confirmSavePreset(name: string): Promise<void> {
    if (name.length < 3) {
      this.revealErrors(['Preset name must be at least 3 characters.']);
      return;
    }

    this.saving.set(true);
    this.errorMessages.set([]);
    try {
      const created = await this.overlay.run(async () => {
        const persisted = await this.persistCampaignCore();
        if (!persisted) {
          return null;
        }

        await this.campaignsApi.saveAsPreset(persisted.detail.id, name);
        this.presetsLoaded = false;
        await this.ensureSavedPresets();
        return persisted;
      });
      if (!created) {
        return;
      }

      this.closeSavePresetDialog();
      this.applyPersistedCampaign(created);
      if (created.isNew) {
        await this.router.navigate(['/campaigns', created.detail.id, 'map']);
        return;
      }

      this.revealSuccess();
    } catch (error: unknown) {
      this.serverFields.set(new Set(readApiFieldErrors(error)));
      this.revealErrors(readApiErrorMessages(error, 'Unable to save the campaign preset.'));
    } finally {
      this.saving.set(false);
    }
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    this.errorMessages.set([]);
    try {
      const created = await this.overlay.run(async () => this.persistCampaignCore());
      if (!created) {
        this.saveStatus.set('failure');
        return;
      }

      this.applyPersistedCampaign(created);
      if (created.isNew) {
        await this.router.navigate(['/campaigns', created.detail.id, 'map']);
        return;
      }

      this.lastSavedAtUtc.set(new Date().toISOString());
      this.saveStatus.set('success');
      this.revealSuccess();
    } catch (error: unknown) {
      this.saveStatus.set('failure');
      this.serverFields.set(new Set(readApiFieldErrors(error)));
      this.revealErrors(readApiErrorMessages(error, 'Unable to save the campaign.'));
    } finally {
      this.saving.set(false);
    }
  }

  private async persistCampaignCore(): Promise<{ detail: CampaignDetail; isNew: boolean } | null> {
    this.form.markAllAsTouched();
    this.serverFields.set(new Set());
    this.successMessage.set(null);
    const collected = this.collectFailures();
    if (collected.messages.length > 0) {
      this.expandSections(collected.sections);
      this.revealErrors(collected.messages);
      return null;
    }

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

    if (this.pendingPresetMapId && !this.mapFile) {
      detail = await this.campaignsApi.applyPresetMap(detail.id, this.pendingPresetMapId, detail.revision);
      this.pendingPresetMapId = null;
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

    for (const [itemId, file] of this.itemObjectiveImages) {
      const item = this.itemObjectiveTypes.controls.find((entry) => entry.controls.id.value === itemId);
      if (item?.controls.iconSource.value !== 'image') {
        continue;
      }

      detail = await this.campaignsApi.uploadItemObjectiveImage(detail.id, itemId, file, detail.revision);
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
  }

  private applyPersistedCampaign(created: { detail: CampaignDetail; isNew: boolean }): void {
    this.campaignId.set(created.detail.id);
    this.loadedDetail = created.detail;
    this.clearPendingUploads();
    this.hydrateFromDetail(created.detail);
    this.captureBaseline();
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

      this.loadedDetail = campaign;
      this.hydrateFromDetail(campaign);
      this.captureBaseline();
    } catch (error: unknown) {
      this.revealErrors(readApiErrorMessages(error, 'Unable to load this campaign.'));
    } finally {
      this.loading.set(false);
    }
  }

  private hydrateFromDetail(campaign: CampaignDetail): void {
    this.hydrating = true;
    try {
      this.revision = campaign.revision;
      this.hasExistingMap.set(campaign.hasMap);
      this.setStoredMapPreview(campaign.id, campaign.revision, campaign.hasMap);
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
          this.createFactionGroup(faction.name, this.allyGroupIdFor(campaign, faction), faction.subfactions, {
            id: faction.id,
            color: faction.color,
            requiresSubfaction: faction.requiresSubfaction,
            hasFlagImage: faction.hasFlagImage,
            specialRuleIds: faction.specialRuleIds ?? [],
            subfactionSpecialRuleIds: this.subfactionRuleIdsFromDetail(faction.subfactionSpecialRules),
          }),
        ),
      );
      this.replaceArray(
        this.allyGroups,
        campaign.allyGroups.map((group) => this.createAllyGroup(group.id, group.name, group.color)),
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
        this.specialRules,
        (campaign.specialRules ?? []).map((rule) =>
          this.createSpecialRuleGroup(rule.id, rule.name, rule.text, rule.effectKey ?? undefined),
        ),
      );
      this.replaceArray(
        this.missions,
        this.catalogMissionsFrom(campaign).map((mission) => this.createMissionGroupFromDetail(mission)),
      );
      this.replaceArray(
        this.forceStatuses,
        (campaign.forceStatuses ?? []).map((status) =>
          this.createForceStatusGroup(
            status.id,
            status.name,
            status.effects,
            status.enableTrigger,
            status.clearTrigger,
          ),
        ),
      );
      this.bumpCatalog();
      this.replaceArray(
        this.privateObjectiveTypes,
        (campaign.privateObjectiveTypes ?? []).map((type) => this.createPrivateObjectiveGroup(type, type.id)),
      );
      this.replaceArray(
        this.publicObjectiveTypes,
        (campaign.publicObjectiveTypes ?? []).map((type) => this.createPublicObjectiveGroup(type, type.id)),
      );
      this.form.controls.pointsPerBattleWon.setValue(campaign.pointsPerBattleWon ?? 2);
      this.form.controls.pointsPerBattleDraw.setValue(campaign.pointsPerBattleDraw ?? 1);
      this.form.controls.useDifferentialBattleScoring.setValue(campaign.useDifferentialBattleScoring ?? true);
      this.form.controls.differentialMultiplier.setValue(campaign.differentialMultiplier ?? 1);
      this.form.controls.differentialMinimum.setValue(campaign.differentialMinimum ?? 0);
      this.form.controls.differentialMaximum.setValue(campaign.differentialMaximum ?? 10);
      this.form.controls.allowNegativeDifferential.setValue(campaign.allowNegativeDifferential ?? false);
      this.form.controls.mostTerritoriesCampaignPoints.setValue(campaign.mostTerritoriesCampaignPoints ?? 0);
      this.form.controls.longestTerritoryChainCampaignPoints.setValue(
        campaign.longestTerritoryChainCampaignPoints ?? 0,
      );
      this.form.controls.mostBattlesWonCampaignPoints.setValue(campaign.mostBattlesWonCampaignPoints ?? 0);
      this.form.controls.mostStructurePointsCampaignPoints.setValue(campaign.mostStructurePointsCampaignPoints ?? 0);
      this.form.controls.pointsPerTerritoryCampaignPoints.setValue(campaign.pointsPerTerritoryCampaignPoints ?? 0);
      this.form.controls.alliedRelicControlCampaignPoints.setValue(campaign.alliedRelicControlCampaignPoints ?? 0);
      this.applySplitForcePenalty(campaign);
      this.form.controls.alwaysAskGeneralKill.setValue(campaign.alwaysAskGeneralKill !== false);
      this.form.controls.alwaysAskSupplyLineDestroyed.setValue(campaign.alwaysAskSupplyLineDestroyed !== false);
      this.form.controls.generalKillCampaignPoints.setValue(campaign.generalKillCampaignPoints ?? 1);
      this.form.controls.supplyLineDestroyedCampaignPoints.setValue(campaign.supplyLineDestroyedCampaignPoints ?? 1);
      this.applyRoundEscalations(campaign.roundEscalations, campaign.roundCount);
      this.replaceArray(
        this.phases,
        campaign.phases.map((phase) =>
          this.createPhaseGroup(
            phase.kind,
            phase.durationAmount,
            phase.durationUnit,
            phase.endPhaseEarlyIfAble !== false,
          ),
        ),
      );
      this.refreshPhases();
      if (this.factions.length < 2) {
        while (this.factions.length < 2) {
          this.factions.push(this.createFactionGroup());
        }
      }
    } finally {
      this.hydrating = false;
    }
  }

  private createFactionGroup(
    name = '',
    allyGroupId = '',
    subfactions: readonly string[] = [''],
    options?: {
      id?: string;
      color?: string;
      requiresSubfaction?: boolean;
      hasFlagImage?: boolean;
      specialRuleIds?: readonly string[];
      subfactionSpecialRuleIds?: Record<string, string[]>;
    },
  ): FactionGroup {
    const names = subfactions.length > 0 ? subfactions : [''];
    return this.formBuilder.nonNullable.group({
      id: [options?.id ?? crypto.randomUUID()],
      name: [name, [required, maxLength(60)]],
      color: [options?.color ?? '#2563EB', required],
      requiresSubfaction: [options?.requiresSubfaction === true],
      allyGroupId: [allyGroupId],
      flagSource: this.formBuilder.nonNullable.control<'color' | 'image'>(options?.hasFlagImage ? 'image' : 'color'),
      clearFlagImage: [false],
      subfactions: this.formBuilder.array<NamedGroup>(names.map((value) => this.createNamedGroup(value))),
      specialRuleIds: [options?.specialRuleIds ? [...options.specialRuleIds] : []],
      subfactionSpecialRuleIds: this.formBuilder.nonNullable.control<Record<string, string[]>>(
        options?.subfactionSpecialRuleIds ? { ...options.subfactionSpecialRuleIds } : {},
      ),
    });
  }

  private createNamedGroup(name = ''): NamedGroup {
    return this.formBuilder.nonNullable.group({
      name: [name, maxLength(60)],
    });
  }

  private createAllyGroup(id?: string, name = '', color?: string): AllyGroupForm {
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [name, maxLength(60)],
      color: [color ?? this.nextAllyColor(), required],
    });
  }

  private allyGroupIdFor(
    campaign: Pick<CampaignDetail, 'allyGroups'>,
    faction: { allyGroupId?: string | null; allyGroupName?: string | null },
  ): string {
    const id = faction.allyGroupId?.trim();
    if (id && campaign.allyGroups.some((group) => group.id === id)) {
      return id;
    }

    const name = faction.allyGroupName?.trim();
    if (!name) {
      return '';
    }

    return campaign.allyGroups.find((group) => group.name.toLowerCase() === name.toLowerCase())?.id ?? '';
  }

  private applySplitForcePenalty(campaign: {
    splitForceSupplyPenaltyPercent?: number;
    splitForceSupplyPenaltyIsPercent?: boolean;
  }): void {
    this.form.controls.splitForceSupplyPenaltyPercent.setValue(
      campaign.splitForceSupplyPenaltyPercent ?? HUNT_IN_ESTALIA_SPLIT_FORCE_SUPPLY_PENALTY_VALUE,
    );
    this.form.controls.splitForceSupplyPenaltyIsPercent.setValue(
      campaign.splitForceSupplyPenaltyIsPercent ?? HUNT_IN_ESTALIA_SPLIT_FORCE_SUPPLY_PENALTY_IS_PERCENT,
    );
  }

  private nextAllyColor(): string {
    return nextUnusedFactionColor(this.allyGroups.controls.map((group) => group.controls.color.value));
  }

  private newId(): string {
    return crypto.randomUUID();
  }

  private createMissionGroup(
    id?: string,
    name = '',
    url = '',
    clearFile = false,
    extra?: Partial<CampaignMission>,
  ): MissionGroup {
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [name, maxLength(60)],
      url: [url, [maxLength(2048), httpUrl]],
      clearFile: [clearFile],
      resultQuestions: this.formBuilder.array<MissionQuestionGroup>([]),
      isAttackerDefender: [extra?.isAttackerDefender ?? false],
      hasArmyPointsAdvantage: [extra?.hasArmyPointsAdvantage ?? false],
      armyPointsAdvantageSide: [extra?.armyPointsAdvantageSide ?? 'Defender'],
      armyPointsAdvantageIsPercent: [extra?.armyPointsAdvantageIsPercent ?? false],
      armyPointsAdvantageAmount: [extra?.armyPointsAdvantageAmount ?? 0],
      hasSupplyPointsAdvantage: [extra?.hasSupplyPointsAdvantage ?? false],
      supplyPointsAdvantageSide: [extra?.supplyPointsAdvantageSide ?? 'Defender'],
      supplyPointsAdvantageAmount: [extra?.supplyPointsAdvantageAmount ?? 0],
    });
  }

  private createMissionQuestionGroup(
    id?: string,
    prompt = '',
    kind = 'Boolean',
    battlePoints = 0,
    campaignPoints = 0,
  ): MissionQuestionGroup {
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      prompt: [prompt, maxLength(240)],
      kind: [kind],
      battlePoints: [battlePoints, [minValue(0), maxValue(999)]],
      campaignPoints: [campaignPoints, [minValue(0), maxValue(999)]],
    });
  }

  private applyRoundEscalations(
    rows:
      | readonly {
          roundNumber: number;
          maxArmyPoints: number;
          freeSupplyPoints: number;
          freeCharacterCount: number;
        }[]
      | undefined,
    roundCount: number,
  ): void {
    this.replaceArray(
      this.roundEscalations,
      (rows ?? []).map((row) => this.createRoundEscalationGroup(row)),
    );
    this.syncRoundEscalations(roundCount);
  }

  private createRoundEscalationGroup(row: {
    roundNumber: number;
    maxArmyPoints: number;
    freeSupplyPoints: number;
    freeCharacterCount: number;
  }): RoundEscalationGroup {
    return this.formBuilder.nonNullable.group({
      roundNumber: [row.roundNumber],
      maxArmyPoints: [row.maxArmyPoints, [minValue(10), maxValue(100000)]],
      freeSupplyPoints: [row.freeSupplyPoints, [minValue(0), maxValue(999)]],
      freeCharacterCount: [row.freeCharacterCount, [minValue(0), maxValue(99)]],
    });
  }

  private syncRoundEscalations(roundCount: number): void {
    const count = Math.max(3, Math.min(52, Math.floor(roundCount)));
    if (!Number.isFinite(count)) {
      return;
    }

    const current = this.roundEscalations.getRawValue();
    if (current.length === count && current.every((row, index) => row.roundNumber === index + 1)) {
      return;
    }

    const defaults = defaultArmyEscalations(count);
    this.replaceArray(
      this.roundEscalations,
      defaults.map((row) => {
        const existing = current.find((item) => item.roundNumber === row.roundNumber);
        return this.createRoundEscalationGroup(existing ?? row);
      }),
    );
  }

  private createTerrainGroup(
    id?: string,
    name = '',
    color = '#7CB342',
    missionName = '',
    campaignPoints = 0,
    isWaterFeature = false,
  ): TerrainGroup {
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [name, [required, maxLength(60)]],
      color: [color, required],
      campaignPoints: [campaignPoints, [minValue(0), maxValue(999)]],
      isWaterFeature: [isWaterFeature],
      supplyPoints: [HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS, [minValue(0), maxValue(999)]],
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
      campaignPoints: [type.campaignPoints ?? 0, [minValue(0), maxValue(999)]],
      isWaterFeature: [type.isWaterFeature === true],
      supplyPoints: [type.supplyPoints ?? HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS, [minValue(0), maxValue(999)]],
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
    campaignPoints = 0,
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
      campaignPoints: [campaignPoints, [minValue(0), maxValue(999)]],
      supplyPoints: [HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS, [minValue(0), maxValue(999)]],
      pillageSupplyPoints: [HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS, [minValue(0), maxValue(999)]],
      destroySupplyPoints: [HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS, [minValue(0), maxValue(999)]],
      missions: this.formBuilder.array<MissionGroup>(missions ?? []),
    });
  }

  private createStructureGroupFromDetail(type: CampaignStructureType): StructureGroup {
    const missions =
      type.missions.length > 0 ? type.missions.map((mission) => this.createMissionGroupFromDetail(mission)) : [];
    const group = this.createStructureGroup(
      type.id,
      type.name,
      type.builtinSymbol ?? '',
      missions,
      type.hasImage ? 'image' : 'symbol',
      type.hasPillagedImage ? 'image' : 'symbol',
      type.isBuildable,
      type.isPillageable,
      type.isDestructible,
      type.campaignPoints ?? 0,
    );
    group.patchValue({
      supplyPoints: type.supplyPoints ?? HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS,
      pillageSupplyPoints: type.pillageSupplyPoints ?? HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS,
      destroySupplyPoints: type.destroySupplyPoints ?? HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS,
    });
    return group;
  }

  private createMissionGroupFromDetail(mission: CampaignMission): MissionGroup {
    const group = this.createMissionGroup(mission.id, mission.name, mission.url ?? '', false, mission);
    const questions = mission.resultQuestions ?? [];
    this.replaceArray(
      group.controls.resultQuestions,
      questions.map((question) =>
        this.createMissionQuestionGroup(
          question.id,
          question.prompt,
          question.kind,
          question.battlePoints,
          question.campaignPoints,
        ),
      ),
    );
    return group;
  }

  private createItemObjectiveGroup(
    item?: ItemObjectivePresetItem,
    id?: string,
    extra?: Partial<CampaignItemObjectiveType>,
  ): ItemObjectiveGroup {
    const defaults = defaultItemObjective();
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [item?.name ?? '', [maxLength(60)]],
      isHiddenUntilFound: [item?.isHiddenUntilFound ?? defaults.isHiddenUntilFound],
      placement: this.formBuilder.nonNullable.control<ItemObjectivePlacement>(item?.placement ?? defaults.placement),
      allowOnSpawn: [item?.allowOnSpawn ?? defaults.allowOnSpawn],
      builtinSymbol: [extra?.builtinSymbol ?? 'Crown'],
      color: [extra?.color ?? ITEM_OBJECTIVE_DEFAULT_COLOR, required],
      iconSource: this.formBuilder.nonNullable.control<'symbol' | 'image'>(extra?.hasImage ? 'image' : 'symbol'),
      clearImage: [false],
      campaignPoints: [extra?.campaignPoints ?? 0, [minValue(0), maxValue(999)]],
      flavorText: [extra?.flavorText ?? '', maxLength(2000)],
      specialRuleIds: [extra?.specialRuleIds ? [...extra.specialRuleIds] : []],
      choices: this.formBuilder.array<ItemChoiceGroup>(
        (extra?.choices ?? []).map((choice) => this.createItemChoiceGroup(choice)),
      ),
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
      type,
    );
  }

  private createPublicObjectiveGroup(type?: Partial<CampaignPublicObjectiveType>, id?: string): PublicObjectiveGroup {
    return this.formBuilder.nonNullable.group({
      id: [id ?? type?.id ?? this.newId()],
      name: [type?.name ?? '', [maxLength(60)]],
      description: [type?.description ?? '', maxLength(240)],
      campaignPoints: [type?.campaignPoints ?? 0, [minValue(0), maxValue(999)]],
    });
  }

  private createForceStatusGroup(
    id?: string,
    name = '',
    effects = '',
    enableTrigger = 'Hold',
    clearTrigger = 'Hold',
  ): ForceStatusGroup {
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [name, [maxLength(60)]],
      effects: [effects, maxLength(2000)],
      enableTrigger: [enableTrigger, required],
      clearTrigger: [clearTrigger, required],
    });
  }

  private createSpecialRuleGroup(id?: string, name = '', text = '', effectKey?: string): SpecialRuleGroup {
    const group = this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [name, [maxLength(60)]],
      text: [text, maxLength(2000)],
      effectKey: [effectKey ?? ''],
    });
    group.controls.name.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((value) => {
      this.fillSpecialRuleText(group, value);
      this.bumpCatalog();
    });
    return group;
  }

  private fillSpecialRuleText(group: SpecialRuleGroup, name: string): void {
    const preset = this.presetSpecialRule(name);
    if (!preset) {
      group.controls.effectKey.setValue('');
      return;
    }

    const current = group.controls.text.value;
    if (!current.trim() || OLD_WORLD_SPECIAL_RULES.some((rule) => rule.description === current)) {
      group.controls.text.setValue(preset.description);
    }

    group.controls.effectKey.setValue(preset.effectKey ?? '');
  }

  private createPrivateObjectiveGroup(
    type?: Partial<CampaignPrivateObjectiveType>,
    id?: string,
  ): PrivateObjectiveGroup {
    const kinds = new Set(type?.allowedHolderKinds ?? ['Player', 'Faction', 'AllyGroup']);
    return this.formBuilder.nonNullable.group({
      id: [id ?? type?.id ?? this.newId()],
      name: [type?.name ?? '', [maxLength(60)]],
      description: [type?.description ?? '', maxLength(2000)],
      campaignPoints: [type?.campaignPoints ?? 0, [minValue(0), maxValue(999)]],
      allowPlayer: [kinds.has('Player')],
      allowFaction: [kinds.has('Faction')],
      allowAllyGroup: [kinds.has('AllyGroup')],
      scoringKind: [type?.scoringKind ?? 'Manual'],
      automaticKind: [type?.automaticKind ?? 'None'],
      requiredCount: [type?.requiredCount ?? 1, [minValue(1), maxValue(999)]],
      structureTypeId: [type?.structureTypeId ?? ''],
      territoryIds: [(type?.territoryIds ?? []).join(', ')],
    });
  }

  private createItemChoiceGroup(choice?: ItemObjectiveChoice): ItemChoiceGroup {
    const results = choice?.results ?? [];
    return this.formBuilder.nonNullable.group({
      id: [choice?.id ?? this.newId()],
      name: [choice?.name ?? '', maxLength(60)],
      results: this.formBuilder.array<ItemChoiceResultGroup>(
        results.length > 0
          ? results.map((result) => this.createItemChoiceResultGroup(result))
          : [this.createItemChoiceResultGroup()],
      ),
    });
  }

  private createItemChoiceResultGroup(result?: ItemObjectiveChoiceResult): ItemChoiceResultGroup {
    return this.formBuilder.nonNullable.group({
      id: [result?.id ?? this.newId()],
      flavorText: [result?.flavorText ?? '', maxLength(2000)],
      newStateKey: [result?.newStateKey ?? '', maxLength(60)],
      destroyItem: [result?.destroyItem === true],
      replacementItemTypeId: [result?.replacementItemTypeId ?? ''],
      grantedPrivateObjectiveTypeId: [result?.grantedPrivateObjectiveTypeId ?? ''],
    });
  }

  private specialRuleIdsByName(): Map<string, string> {
    return new Map(this.specialRules.controls.map((rule) => [rule.controls.name.value, rule.controls.id.value]));
  }

  private subfactionRuleIdsFromNames(
    mapping: Readonly<Record<string, readonly string[]>> | undefined,
    ruleIds: Map<string, string>,
  ): Record<string, string[]> {
    const next: Record<string, string[]> = {};
    for (const [name, names] of Object.entries(mapping ?? {})) {
      next[name] = names.map((ruleName) => ruleIds.get(ruleName)).filter((id): id is string => !!id);
    }

    return next;
  }

  private subfactionRuleIdsFromDetail(
    assignments: readonly { name: string; specialRuleIds: readonly string[] }[] | undefined,
  ): Record<string, string[]> {
    const next: Record<string, string[]> = {};
    for (const assignment of assignments ?? []) {
      next[assignment.name] = [...assignment.specialRuleIds];
    }

    return next;
  }

  protected namedSubfactions(faction: FactionGroup): string[] {
    return faction.controls.subfactions.controls
      .map((item) => item.controls.name.value.trim())
      .filter((name) => name.length > 0);
  }

  protected subfactionSpecialRuleControl(faction: FactionGroup, subfaction: string): FormControl<string[]> {
    const current = faction.controls.subfactionSpecialRuleIds.value[subfaction] ?? [];
    return this.formBuilder.nonNullable.control([...current]);
  }

  protected assignedSubfactionSpecialRules(faction: FactionGroup, subfaction: string): { id: string; name: string }[] {
    const ids = faction.controls.subfactionSpecialRuleIds.value[subfaction] ?? [];
    return this.assignedSpecialRules(this.formBuilder.nonNullable.control(ids));
  }

  protected assignableSubfactionSpecialRuleNames(faction: FactionGroup, subfaction: string): string[] {
    const ids = faction.controls.subfactionSpecialRuleIds.value[subfaction] ?? [];
    return this.assignableSpecialRuleNames(this.formBuilder.nonNullable.control(ids));
  }

  protected assignSubfactionSpecialRuleByName(faction: FactionGroup, subfaction: string): void {
    const ownerId = `${faction.controls.id.value}:${subfaction}`;
    const control = this.formBuilder.nonNullable.control([
      ...(faction.controls.subfactionSpecialRuleIds.value[subfaction] ?? []),
    ]);
    this.assignSpecialRuleByName(control, ownerId);
    faction.controls.subfactionSpecialRuleIds.setValue({
      ...faction.controls.subfactionSpecialRuleIds.value,
      [subfaction]: control.value,
    });
  }

  protected removeSubfactionSpecialRule(faction: FactionGroup, subfaction: string, ruleId: string): void {
    const current = faction.controls.subfactionSpecialRuleIds.value[subfaction] ?? [];
    faction.controls.subfactionSpecialRuleIds.setValue({
      ...faction.controls.subfactionSpecialRuleIds.value,
      [subfaction]: current.filter((id) => id !== ruleId),
    });
  }

  private findSpecialRuleByName(name: string): SpecialRuleGroup | undefined {
    const needle = name.trim().toLowerCase();
    return this.specialRules.controls.find((rule) => rule.controls.name.value.trim().toLowerCase() === needle);
  }

  private presetSpecialRule(name: string): SpecialRulePreset | undefined {
    const needle = name.trim().toLowerCase();
    return OLD_WORLD_SPECIAL_RULES.find((rule) => rule.name.toLowerCase() === needle);
  }

  private ensureSpecialRulesFromNames(names: readonly string[]): void {
    const seen = new Set(
      this.specialRules.controls.map((rule) => rule.controls.name.value.trim().toLowerCase()).filter((name) => name),
    );
    for (const name of names) {
      const trimmed = name.trim();
      if (!trimmed || seen.has(trimmed.toLowerCase()) || this.specialRules.length >= 80) {
        continue;
      }

      const preset = this.presetSpecialRule(trimmed);
      this.specialRules.push(
        this.createSpecialRuleGroup(undefined, preset?.name ?? trimmed, preset?.description ?? '', preset?.effectKey),
      );
      seen.add((preset?.name ?? trimmed).toLowerCase());
    }

    this.bumpCatalog();
  }

  private bumpCatalog(): void {
    this.catalogTick.update((value) => value + 1);
  }

  private hasPendingUploads(): boolean {
    return (
      this.mapFile !== null ||
      this.pendingPresetMapId !== null ||
      this.structureImages.size > 0 ||
      this.structurePillagedImages.size > 0 ||
      this.itemObjectiveImages.size > 0 ||
      this.flagImages.size > 0 ||
      this.missionFiles.size > 0
    );
  }

  private bumpPendingUploads(): void {
    this.pendingUploadsTick.update((value) => value + 1);
  }

  private clearPendingUploads(): void {
    this.mapFile = null;
    this.mapFileName.set(null);
    this.pendingPresetMapId = null;
    this.structureImages.clear();
    this.structurePillagedImages.clear();
    this.itemObjectiveImages.clear();
    this.flagImages.clear();
    this.missionFiles.clear();
    this.bumpPendingUploads();
  }

  private captureBaseline(): void {
    this.savedFormValue = structuredClone(this.form.getRawValue());
    this.form.markAsPristine();
    this.syncFormDirty();
    this.formTick.update((value) => value + 1);
  }

  private syncFormDirty(): void {
    if (this.savedFormValue === null || this.savedFormValue === undefined) {
      return;
    }

    syncDirtyFromBaseline(this.form, this.savedFormValue);
  }

  private anyDirty(controls: readonly { dirty: boolean }[]): boolean {
    return controls.some((control) => control.dirty);
  }

  private missionGroupHasPending(group: MissionGroup): boolean {
    return this.hasPendingMissionFile(group.controls.id.value);
  }

  private sectionHasDirty(id: string): boolean {
    this.formTick();
    this.pendingUploadsTick();
    this.catalogTick();
    const indexed = /^(.*)-(\d+)$/.exec(id);
    if (indexed) {
      return this.indexedSectionDirty(indexed[1], Number(indexed[2]));
    }

    switch (id) {
      case 'details':
        return this.anyDirty([
          this.form.controls.name,
          this.form.controls.description,
          this.form.controls.playerCount,
          this.form.controls.city,
          this.form.controls.region,
          this.form.controls.country,
        ]);
      case 'schedule':
        return this.anyDirty([
          this.form.controls.timeZoneId,
          this.form.controls.startsAtLocal,
          this.form.controls.roundCount,
          this.form.controls.roundLengthAmount,
          this.form.controls.roundLengthUnit,
          this.phases,
        ]);
      case 'round-army':
        return this.roundEscalations.dirty;
      case 'visibility':
        return this.anyDirty([
          this.form.controls.isPrivate,
          this.form.controls.isPubliclyViewable,
          this.form.controls.joinPassword,
          this.form.controls.creatorRole,
        ]);
      case 'specialRules':
        return this.specialRules.dirty;
      case 'forceStatuses':
        return this.forceStatuses.dirty;
      case 'publicObjectives':
        return (
          this.publicObjectiveTypes.dirty ||
          this.anyDirty([
            this.form.controls.pointsPerBattleWon,
            this.form.controls.pointsPerBattleDraw,
            this.form.controls.useDifferentialBattleScoring,
            this.form.controls.differentialMultiplier,
            this.form.controls.differentialMinimum,
            this.form.controls.differentialMaximum,
            this.form.controls.allowNegativeDifferential,
            this.form.controls.mostTerritoriesCampaignPoints,
            this.form.controls.longestTerritoryChainCampaignPoints,
            this.form.controls.mostBattlesWonCampaignPoints,
            this.form.controls.mostStructurePointsCampaignPoints,
            this.form.controls.pointsPerTerritoryCampaignPoints,
            this.form.controls.alliedRelicControlCampaignPoints,
            this.form.controls.alwaysAskGeneralKill,
            this.form.controls.alwaysAskSupplyLineDestroyed,
            this.form.controls.generalKillCampaignPoints,
            this.form.controls.supplyLineDestroyedCampaignPoints,
          ])
        );
      case 'privateObjectives':
        return this.privateObjectiveTypes.dirty;
      case 'allies':
        return this.allyGroups.dirty;
      case 'factions':
        return this.factions.dirty || this.flagImages.size > 0;
      case 'missions':
        return this.missions.dirty || this.missions.controls.some((mission) => this.missionGroupHasPending(mission));
      case 'terrain':
        return (
          this.terrainTypes.dirty ||
          this.terrainTypes.controls.some((terrain) =>
            terrain.controls.missions.controls.some((mission) => this.missionGroupHasPending(mission)),
          )
        );
      case 'structures':
        return (
          this.structureTypes.dirty ||
          this.structureImages.size > 0 ||
          this.structurePillagedImages.size > 0 ||
          this.structureTypes.controls.some((structure) =>
            structure.controls.missions.controls.some((mission) => this.missionGroupHasPending(mission)),
          )
        );
      case 'itemObjectives':
        return this.itemObjectiveTypes.dirty || this.itemObjectiveImages.size > 0;
      case 'links':
        return this.links.dirty;
      case 'map':
        return this.isMapDirty();
      default:
        return false;
    }
  }

  private indexedSectionDirty(prefix: string, index: number): boolean {
    switch (prefix) {
      case 'special-rule':
        return this.arrayControlDirty(this.specialRules, index);
      case 'force-status':
        return this.arrayControlDirty(this.forceStatuses, index);
      case 'public-objective':
        return this.arrayControlDirty(this.publicObjectiveTypes, index);
      case 'private-objective':
        return this.arrayControlDirty(this.privateObjectiveTypes, index);
      case 'faction-item': {
        if (!this.hasArrayIndex(this.factions, index)) {
          return false;
        }

        const faction = this.factions.at(index);
        return faction.dirty || this.hasPendingFlag(faction.controls.id.value);
      }
      case 'faction-sub':
        return this.hasArrayIndex(this.factions, index) && this.factions.at(index).controls.subfactions.dirty;
      case 'mission-item': {
        if (!this.hasArrayIndex(this.missions, index)) {
          return false;
        }

        const mission = this.missions.at(index);
        return mission.dirty || this.missionGroupHasPending(mission);
      }
      case 'terrain-item': {
        if (!this.hasArrayIndex(this.terrainTypes, index)) {
          return false;
        }

        const terrain = this.terrainTypes.at(index);
        return (
          terrain.dirty || terrain.controls.missions.controls.some((mission) => this.missionGroupHasPending(mission))
        );
      }
      case 'terrain-missions': {
        if (!this.hasArrayIndex(this.terrainTypes, index)) {
          return false;
        }

        const terrain = this.terrainTypes.at(index);
        return (
          terrain.controls.missions.dirty ||
          terrain.controls.missions.controls.some((mission) => this.missionGroupHasPending(mission))
        );
      }
      case 'structure-item': {
        if (!this.hasArrayIndex(this.structureTypes, index)) {
          return false;
        }

        const structure = this.structureTypes.at(index);
        const id = structure.controls.id.value;
        return (
          structure.dirty ||
          this.hasPendingStructureImage(id) ||
          this.hasPendingPillagedImage(id) ||
          structure.controls.missions.controls.some((mission) => this.missionGroupHasPending(mission))
        );
      }
      case 'structure-missions': {
        if (!this.hasArrayIndex(this.structureTypes, index)) {
          return false;
        }

        const structure = this.structureTypes.at(index);
        return (
          structure.controls.missions.dirty ||
          structure.controls.missions.controls.some((mission) => this.missionGroupHasPending(mission))
        );
      }
      case 'item-objective': {
        if (!this.hasArrayIndex(this.itemObjectiveTypes, index)) {
          return false;
        }

        const item = this.itemObjectiveTypes.at(index);
        return item.dirty || this.hasPendingItemImage(item.controls.id.value);
      }
      default:
        return false;
    }
  }

  private hasArrayIndex(items: { length: number }, index: number): boolean {
    return index >= 0 && index < items.length;
  }

  private arrayControlDirty(items: { length: number; at(index: number): { dirty: boolean } }, index: number): boolean {
    return this.hasArrayIndex(items, index) && items.at(index).dirty;
  }

  private dropAssignedSpecialRule(ruleId: string): void {
    for (const faction of this.factions.controls) {
      faction.controls.specialRuleIds.setValue(faction.controls.specialRuleIds.value.filter((id) => id !== ruleId));
      const next: Record<string, string[]> = {};
      for (const [name, ids] of Object.entries(faction.controls.subfactionSpecialRuleIds.value)) {
        next[name] = ids.filter((id) => id !== ruleId);
      }

      faction.controls.subfactionSpecialRuleIds.setValue(next);
    }

    for (const item of this.itemObjectiveTypes.controls) {
      item.controls.specialRuleIds.setValue(item.controls.specialRuleIds.value.filter((id) => id !== ruleId));
    }
  }

  private createDefaultTerrainGroups(): TerrainGroup[] {
    return defaultTerrainCatalog().map((entry) =>
      this.createTerrainGroup(undefined, entry.name, entry.color, '', 0, entry.isWaterFeature === true),
    );
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

  private createPhaseGroup(
    kind: string,
    durationAmount: number,
    durationUnit: string,
    endPhaseEarlyIfAble = true,
  ): PhaseGroup {
    return this.formBuilder.nonNullable.group({
      kind: [kind, required],
      durationAmount: [durationAmount, [required, minValue(1), maxValue(60)]],
      durationUnit: [durationUnit, required],
      endPhaseEarlyIfAble: [endPhaseEarlyIfAble],
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
      .filter((group) => group.name.trim().length > 0)
      .map((group) => ({ id: group.id, name: group.name.trim(), color: group.color }));
    const factions = value.factions.map((faction) => {
      const group = value.allyGroups.find((item) => item.id === faction.allyGroupId);
      return {
        id: faction.id,
        name: faction.name.trim(),
        color: faction.color,
        requiresSubfaction: faction.requiresSubfaction,
        allyGroupId: faction.allyGroupId.trim().length > 0 ? faction.allyGroupId : null,
        allyGroupName: group?.name.trim() ? group.name.trim() : null,
        subfactions: faction.subfactions.map((item) => item.name.trim()).filter((name) => name.length > 0),
        clearFlagImage: faction.flagSource === 'color' || faction.clearFlagImage,
        specialRuleIds: faction.specialRuleIds,
        subfactionSpecialRules: Object.entries(faction.subfactionSpecialRuleIds)
          .filter(([name]) => faction.subfactions.some((item) => item.name.trim() === name))
          .map(([name, specialRuleIds]) => ({ name, specialRuleIds })),
      };
    });
    const links = value.links
      .filter((link) => link.label.trim().length > 0 || link.url.trim().length > 0)
      .map((link) => ({ label: link.label.trim(), url: link.url.trim() }));
    const terrainTypes = value.terrainTypes.map((type) => ({
      id: type.id,
      name: type.name.trim(),
      color: type.color,
      campaignPoints: 0,
      isWaterFeature: type.isWaterFeature,
      supplyPoints: Number(type.supplyPoints) || HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS,
      missions: type.missions
        .filter((mission) => mission.name.trim().length > 0 || mission.url.trim().length > 0)
        .map((mission) => this.toAttachedMissionPayload(mission)),
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
      campaignPoints: Number(type.campaignPoints) || 0,
      supplyPoints: Number(type.supplyPoints) || HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS,
      pillageSupplyPoints: Number(type.pillageSupplyPoints) || HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS,
      destroySupplyPoints: Number(type.destroySupplyPoints) || HUNT_IN_ESTALIA_DEFAULT_SUPPLY_POINTS,
      missions: type.missions
        .filter((mission) => mission.name.trim().length > 0 || mission.url.trim().length > 0)
        .map((mission) => this.toAttachedMissionPayload(mission)),
    }));
    const itemObjectiveTypes = value.itemObjectiveTypes
      .filter((type) => type.name.trim().length > 0)
      .map((type) => ({
        id: type.id,
        name: type.name.trim(),
        isHiddenUntilFound: type.isHiddenUntilFound,
        placement: type.placement,
        allowOnSpawn: type.allowOnSpawn,
        builtinSymbol: type.builtinSymbol,
        color: type.color,
        clearImage: type.iconSource === 'symbol' || type.clearImage,
        campaignPoints: Number(type.campaignPoints) || 0,
        flavorText: type.flavorText.trim() || null,
        specialRuleIds: type.specialRuleIds,
        choices: type.choices
          .filter((choice) => choice.name.trim().length > 0)
          .map((choice) => ({
            id: choice.id,
            name: choice.name.trim(),
            results: choice.results.map((result) => ({
              id: result.id,
              flavorText: result.flavorText.trim() || null,
              newStateKey: result.newStateKey.trim() || null,
              destroyItem: result.destroyItem,
              replacementItemTypeId: result.replacementItemTypeId.trim() || null,
              grantedPrivateObjectiveTypeId: result.grantedPrivateObjectiveTypeId.trim() || null,
            })),
          })),
      }));
    const specialRules = value.specialRules
      .filter((rule) => rule.name.trim().length > 0)
      .map((rule) => ({
        id: rule.id,
        name: rule.name.trim(),
        text: rule.text.trim() || null,
        effectKey: rule.effectKey.trim() || null,
      }));
    const forceStatuses = value.forceStatuses
      .filter((status) => status.name.trim().length > 0)
      .map((status) => ({
        id: status.id,
        name: status.name.trim(),
        effects: status.effects.trim() || null,
        enableTrigger: status.enableTrigger,
        clearTrigger: status.clearTrigger,
      }));
    const privateObjectiveTypes = value.privateObjectiveTypes
      .filter((type) => type.name.trim().length > 0)
      .map((type) => ({
        id: type.id,
        name: type.name.trim(),
        description: type.description.trim() || null,
        campaignPoints: Number(type.campaignPoints) || 0,
        allowedHolderKinds: [
          type.allowPlayer ? 'Player' : null,
          type.allowFaction ? 'Faction' : null,
          type.allowAllyGroup ? 'AllyGroup' : null,
        ].filter((kind): kind is string => kind !== null),
        scoringKind: type.scoringKind,
        automaticKind: type.scoringKind === 'Automatic' ? type.automaticKind : 'None',
        requiredCount: Number(type.requiredCount) || 1,
        structureTypeId: type.structureTypeId.trim() || null,
        territoryIds: type.territoryIds
          .split(/[\s,]+/)
          .map((value) => value.trim())
          .filter((value) => value.length > 0),
      }));
    const publicObjectiveTypes = value.publicObjectiveTypes
      .filter((type) => type.name.trim().length > 0)
      .map((type) => ({
        id: type.id,
        name: type.name.trim(),
        description: type.description.trim() || null,
        campaignPoints: Number(type.campaignPoints) || 0,
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
      specialRules,
      missions: this.mergedMissionPayloads(value),
      forceStatuses,
      privateObjectiveTypes,
      publicObjectiveTypes,
      pointsPerBattleWon: Number(value.pointsPerBattleWon) || 0,
      pointsPerBattleDraw: Number(value.pointsPerBattleDraw) || 0,
      useDifferentialBattleScoring: Boolean(value.useDifferentialBattleScoring),
      differentialMultiplier: Number(value.differentialMultiplier) || 1,
      differentialMinimum: Number(value.differentialMinimum) || 0,
      differentialMaximum: Number(value.differentialMaximum) || 0,
      allowNegativeDifferential: Boolean(value.allowNegativeDifferential),
      mostTerritoriesCampaignPoints: Number(value.mostTerritoriesCampaignPoints) || 0,
      longestTerritoryChainCampaignPoints: Number(value.longestTerritoryChainCampaignPoints) || 0,
      mostBattlesWonCampaignPoints: Number(value.mostBattlesWonCampaignPoints) || 0,
      mostStructurePointsCampaignPoints: Number(value.mostStructurePointsCampaignPoints) || 0,
      pointsPerTerritoryCampaignPoints: Number(value.pointsPerTerritoryCampaignPoints) || 0,
      alliedRelicControlCampaignPoints: Number(value.alliedRelicControlCampaignPoints) || 0,
      splitForceSupplyPenaltyPercent: Number(value.splitForceSupplyPenaltyPercent) || 0,
      splitForceSupplyPenaltyIsPercent: Boolean(value.splitForceSupplyPenaltyIsPercent),
      alwaysAskGeneralKill: Boolean(value.alwaysAskGeneralKill),
      alwaysAskSupplyLineDestroyed: Boolean(value.alwaysAskSupplyLineDestroyed),
      generalKillCampaignPoints: Number(value.generalKillCampaignPoints) || 0,
      supplyLineDestroyedCampaignPoints: Number(value.supplyLineDestroyedCampaignPoints) || 0,
      roundEscalations: value.roundEscalations.map((row) => ({
        roundNumber: Number(row.roundNumber),
        maxArmyPoints: Number(row.maxArmyPoints) || 0,
        freeSupplyPoints: Number(row.freeSupplyPoints) || 0,
        freeCharacterCount: Number(row.freeCharacterCount) || 0,
      })),
      timeZoneId: value.timeZoneId || 'UTC',
      startsAtLocal: value.startsAtLocal,
      roundCount: Number(value.roundCount),
      roundLengthAmount: Number(value.roundLengthAmount),
      roundLengthUnit: value.roundLengthUnit,
      phases: value.phases.map((phase) => ({
        kind: phase.kind,
        durationAmount: Number(phase.durationAmount),
        durationUnit: phase.durationUnit,
        endPhaseEarlyIfAble: phase.endPhaseEarlyIfAble,
      })),
    };
  }

  private toMissionPayload(mission: {
    id: string;
    name: string;
    url: string;
    clearFile: boolean;
    resultQuestions: { id: string; prompt: string; kind: string; battlePoints: number; campaignPoints: number }[];
    isAttackerDefender?: boolean;
    hasArmyPointsAdvantage?: boolean;
    armyPointsAdvantageSide?: string;
    armyPointsAdvantageIsPercent?: boolean;
    armyPointsAdvantageAmount?: number;
    hasSupplyPointsAdvantage?: boolean;
    supplyPointsAdvantageSide?: string;
    supplyPointsAdvantageAmount?: number;
  }): SaveMissionPayload {
    const hasPendingFile = this.missionFiles.has(mission.id);
    return {
      id: mission.id,
      name: mission.name.trim(),
      url: hasPendingFile ? null : mission.url.trim() || null,
      clearFile: hasPendingFile ? false : mission.clearFile,
      resultQuestions: mission.resultQuestions
        .filter((question) => question.prompt.trim().length > 0)
        .map((question) => ({
          id: question.id,
          prompt: question.prompt.trim(),
          kind: question.kind,
          battlePoints: Number(question.battlePoints) || 0,
          campaignPoints: Number(question.campaignPoints) || 0,
        })),
      isAttackerDefender: Boolean(mission.isAttackerDefender),
      hasArmyPointsAdvantage: Boolean(mission.hasArmyPointsAdvantage),
      armyPointsAdvantageSide: mission.armyPointsAdvantageSide ?? 'Defender',
      armyPointsAdvantageIsPercent: Boolean(mission.armyPointsAdvantageIsPercent),
      armyPointsAdvantageAmount: Number(mission.armyPointsAdvantageAmount) || 0,
      hasSupplyPointsAdvantage: Boolean(mission.hasSupplyPointsAdvantage),
      supplyPointsAdvantageSide: mission.supplyPointsAdvantageSide ?? 'Defender',
      supplyPointsAdvantageAmount: Number(mission.supplyPointsAdvantageAmount) || 0,
    };
  }

  private toAttachedMissionPayload(mission: {
    id: string;
    name: string;
    url: string;
    clearFile: boolean;
    resultQuestions: { id: string; prompt: string; kind: string; battlePoints: number; campaignPoints: number }[];
  }): SaveMissionPayload {
    const catalogById = this.missions.controls.find((item) => item.controls.id.value === mission.id);
    if (catalogById) {
      return this.toMissionPayload(catalogById.getRawValue());
    }

    const catalogByName = this.missions.controls.find(
      (item) => item.controls.name.value.trim().toLowerCase() === mission.name.trim().toLowerCase(),
    );
    if (catalogByName) {
      return this.toMissionPayload(catalogByName.getRawValue());
    }

    return this.toMissionPayload(mission);
  }

  private mergedMissionPayloads(value: ReturnType<typeof this.form.getRawValue>): SaveMissionPayload[] {
    const catalog = value.missions
      .filter((mission) => mission.name.trim().length > 0)
      .map((mission) => this.toMissionPayload(mission));
    const seen = new Set(catalog.map((mission) => mission.id));
    const extras = [...value.terrainTypes, ...value.structureTypes].flatMap((type) =>
      type.missions
        .filter((mission) => mission.name.trim().length > 0 && !seen.has(mission.id))
        .map((mission) => {
          seen.add(mission.id);
          return this.toMissionPayload(mission);
        }),
    );
    return [...catalog, ...extras];
  }

  private catalogMissionsFrom(campaign: CampaignDetail): CampaignMission[] {
    if ((campaign.missions ?? []).length > 0) {
      return campaign.missions ?? [];
    }

    const seen = new Map<string, CampaignMission>();
    for (const mission of [...campaign.terrainTypes, ...campaign.structureTypes].flatMap((type) => type.missions)) {
      seen.set(mission.id, mission);
    }

    return [...seen.values()];
  }

  private collectFailures(): { messages: string[]; sections: string[] } {
    const failures: string[] = [];
    const sections = new Set<string>();
    const labels: Record<string, string> = {
      name: 'Campaign name',
      description: 'Description',
      playerCount: 'Max Number of Players',
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

      const flavorMessage = describeControlError(item.controls.flavorText, `Item objective ${index + 1} flavor text`);
      if (flavorMessage) {
        failures.push(flavorMessage);
        sections.add('itemObjectives');
        sections.add(`item-objective-${index}`);
      }

      if (item.controls.iconSource.value === 'image') {
        const id = item.controls.id.value;
        if (!this.itemObjectiveImages.has(id) && !this.hasStoredItemObjectiveImage(id)) {
          failures.push(`Item objective ${index + 1} needs a logo image or a built-in icon.`);
          sections.add('itemObjectives');
          sections.add(`item-objective-${index}`);
        }
      }
    });

    const usedPublicNames = new Set<string>();
    this.publicObjectiveTypes.controls.forEach((item, index) => {
      const name = item.controls.name.value.trim();
      if (!name) {
        return;
      }

      const nameMessage = describeControlError(item.controls.name, `Public objective ${index + 1} name`);
      if (nameMessage) {
        failures.push(nameMessage);
        sections.add('publicObjectives');
        sections.add(`public-objective-${index}`);
      }

      const key = name.toLowerCase();
      if (usedPublicNames.has(key)) {
        failures.push(`Public objective ${index + 1} name must be unique.`);
        sections.add('publicObjectives');
        sections.add(`public-objective-${index}`);
      }

      usedPublicNames.add(key);
    });

    const usedSpecialRuleNames = new Set<string>();
    this.specialRules.controls.forEach((rule, index) => {
      const name = rule.controls.name.value.trim();
      if (!name) {
        return;
      }

      const nameMessage = describeControlError(rule.controls.name, `Faction special rule ${index + 1} name`);
      if (nameMessage) {
        failures.push(nameMessage);
        sections.add('specialRules');
        sections.add(`special-rule-${index}`);
      }

      const key = name.toLowerCase();
      if (usedSpecialRuleNames.has(key)) {
        failures.push(`Faction special rule ${index + 1} name must be unique.`);
        sections.add('specialRules');
        sections.add(`special-rule-${index}`);
      }

      usedSpecialRuleNames.add(key);
    });

    const usedForceStatusNames = new Set<string>();
    this.forceStatuses.controls.forEach((status, index) => {
      const name = status.controls.name.value.trim();
      if (!name) {
        return;
      }

      const nameMessage = describeControlError(status.controls.name, `Force status ${index + 1} name`);
      if (nameMessage) {
        failures.push(nameMessage);
        sections.add('forceStatuses');
        sections.add(`force-status-${index}`);
      }

      if (name.toLowerCase() === 'normal') {
        failures.push('Normal is the absence of a status and cannot be configured.');
        sections.add('forceStatuses');
        sections.add(`force-status-${index}`);
      }

      const key = name.toLowerCase();
      if (usedForceStatusNames.has(key)) {
        failures.push(`Force status ${index + 1} name must be unique.`);
        sections.add('forceStatuses');
        sections.add(`force-status-${index}`);
      }

      usedForceStatusNames.add(key);

      if (!status.controls.enableTrigger.value) {
        failures.push(`Force status ${index + 1} needs an enable condition.`);
        sections.add('forceStatuses');
        sections.add(`force-status-${index}`);
      }

      if (!status.controls.clearTrigger.value) {
        failures.push(`Force status ${index + 1} needs a clear condition.`);
        sections.add('forceStatuses');
        sections.add(`force-status-${index}`);
      }
    });

    const usedPrivateNames = new Set<string>();
    this.privateObjectiveTypes.controls.forEach((item, index) => {
      const name = item.controls.name.value.trim();
      if (!name) {
        return;
      }

      const nameMessage = describeControlError(item.controls.name, `Private objective ${index + 1} name`);
      if (nameMessage) {
        failures.push(nameMessage);
        sections.add('privateObjectives');
        sections.add(`private-objective-${index}`);
      }

      const key = name.toLowerCase();
      if (usedPrivateNames.has(key)) {
        failures.push(`Private objective ${index + 1} name must be unique.`);
        sections.add('privateObjectives');
        sections.add(`private-objective-${index}`);
      }

      usedPrivateNames.add(key);
    });

    this.roundEscalations.controls.forEach((row) => {
      const roundNumber = row.controls.roundNumber.value;
      const message = describeControlError(row.controls.maxArmyPoints, `Round ${roundNumber} max army points`);
      if (message) {
        failures.push(message);
        sections.add('schedule');
      }
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
    this.missions.controls.forEach((mission, index) => {
      const name = mission.controls.name.value.trim();
      if (!name) {
        return;
      }

      const key = name.toLowerCase();
      const id = mission.controls.id.value;
      const existing = missionNames.get(key);
      if (existing && existing !== id) {
        sections.add('missions');
        sections.add(`mission-item-${index}`);
        if (!failures.includes('Mission names must be unique.')) {
          failures.push('Mission names must be unique.');
        }
      } else {
        missionNames.set(key, id);
      }
    });
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
    const catalog = this.missions.controls.find((mission) => mission.controls.id.value === missionId);
    if (catalog) {
      return catalog;
    }

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
    const ids: string[] = [...TOP_LEVEL_SECTION_IDS, 'round-army'];
    this.missions.controls.forEach((_, index) => {
      ids.push(`mission-item-${index}`);
    });
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
    this.storedItemObjectiveImages.set(
      new Set((campaign.itemObjectiveTypes ?? []).filter((type) => type.hasImage).map((type) => type.id)),
    );
    this.storedMissionFiles.set(
      new Set(
        [
          ...(campaign.missions ?? []),
          ...campaign.terrainTypes.flatMap((type) => type.missions),
          ...campaign.structureTypes.flatMap((type) => type.missions),
        ]
          .filter((mission) => mission.hasFile)
          .map((mission) => mission.id),
      ),
    );
  }

  private removeMissionsFiles(group: TerrainGroup | StructureGroup): void {
    for (const mission of group.controls.missions.controls) {
      this.missionFiles.delete(mission.controls.id.value);
    }

    this.bumpPendingUploads();
  }

  private revealErrors(messages: readonly string[]): void {
    this.successMessage.set(null);
    this.errorMessages.set([...messages]);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }

  private revealSuccess(message = FORM_SAVE_SUCCESS_MESSAGE): void {
    this.errorMessages.set([]);
    this.successMessage.set(message);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }
}
