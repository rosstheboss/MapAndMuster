import { Component, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, type FormArray, type FormControl, type FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { readApiErrorMessages, readApiFieldErrors } from '../../core/auth/auth.service';
import { FilterableComboboxComponent } from '../../shared/filterable-combobox/filterable-combobox.component';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type {
  CampaignDetail,
  CampaignMission,
  CampaignStructureType,
  CampaignTerrainType,
  SaveCampaignPayload,
} from '../../core/campaigns/campaign.models';
import { defaultStructureCatalog, defaultTerrainCatalog } from '../../core/campaigns/catalog-defaults';
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
import { listTimeZones } from '../../core/location/location';
import { MapSymbolComponent } from '../../shared/map-symbol/map-symbol.component';
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
  clearImage: FormControl<boolean>;
  missions: FormArray<MissionGroup>;
}>;
type PhaseGroup = FormGroup<{
  kind: FormControl<string>;
  durationAmount: FormControl<number>;
  durationUnit: FormControl<string>;
}>;

@Component({
  selector: 'app-campaign-setup-page',
  imports: [ReactiveFormsModule, RouterLink, FilterableComboboxComponent, MapSymbolComponent],
  templateUrl: './campaign-setup.page.html',
  styleUrl: './campaign-setup.page.css',
})
export class CampaignSetupPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly overlay = inject(FormSubmitOverlayService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formAlert = viewChild<ElementRef<HTMLElement>>('formAlert');

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessages = signal<string[]>([]);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly serverFields = signal<ReadonlySet<string>>(new Set());
  protected readonly campaignId = signal<string | null>(null);
  protected readonly hasExistingMap = signal(false);
  protected readonly mapFileName = signal<string | null>(null);
  private mapFile: File | null = null;
  private revision = 0;
  private readonly structureImages = new Map<string, File>();
  private readonly missionFiles = new Map<string, File>();
  private readonly storedStructureImages = signal<ReadonlySet<string>>(new Set());
  private readonly storedMissionFiles = signal<ReadonlySet<string>>(new Set());

  protected readonly timeZones = listTimeZones();
  protected readonly durationUnits = DURATION_UNITS;
  protected readonly phaseKinds = PHASE_KINDS;
  protected readonly factionPresets = FACTION_PRESETS;
  protected readonly presetId = this.formBuilder.nonNullable.control('');
  protected readonly selectedPresetId = toSignal(this.presetId.valueChanges, {
    initialValue: this.presetId.value,
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [required, minLength(3), maxLength(80)]],
    description: ['', maxLength(500)],
    playerCount: [8, [required, minValue(2), maxValue(100)]],
    isPrivate: [false],
    joinPassword: [''],
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
    phases: this.formBuilder.array<PhaseGroup>([
      this.createPhaseGroup('Action', 3, 'Days'),
      this.createPhaseGroup('Action', 3, 'Days'),
      this.createPhaseGroup('Battle', 1, 'Days'),
    ]),
  });
  protected readonly isPrivate = toSignal(this.form.controls.isPrivate.valueChanges, {
    initialValue: this.form.controls.isPrivate.value,
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    this.campaignId.set(id);
    if (id) {
      void this.loadCampaign(id);
    } else {
      this.loading.set(false);
    }
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
    this.removeMissionsFiles(this.structureTypes.at(index));
    this.structureTypes.removeAt(index);
  }

  protected addMission(group: TerrainGroup | StructureGroup): void {
    if (group.controls.missions.length >= 20) {
      return;
    }

    group.controls.missions.push(this.createMissionGroup());
  }

  protected removeMission(group: TerrainGroup | StructureGroup, index: number): void {
    const missionId = group.controls.missions.at(index).controls.id.value;
    this.missionFiles.delete(missionId);
    group.controls.missions.removeAt(index);
  }

  protected addPhase(kind: string): void {
    if (this.phases.length >= 16) {
      return;
    }

    const unit = this.form.controls.roundLengthUnit.value;
    this.phases.push(this.createPhaseGroup(kind, 1, unit));
  }

  protected removePhase(index: number): void {
    if (this.phases.length <= 2) {
      return;
    }

    this.phases.removeAt(index);
  }

  protected movePhase(index: number, offset: number): void {
    const target = index + offset;
    if (target < 0 || target >= this.phases.length) {
      return;
    }

    const current = this.phases.at(index);
    this.phases.removeAt(index);
    this.phases.insert(target, current);
  }

  protected onMapSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.mapFile = file;
    this.mapFileName.set(file?.name ?? null);
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

  protected missionFileName(missionId: string): string | null {
    return this.missionFiles.get(missionId)?.name ?? null;
  }

  protected hasStoredStructureImage(structureId: string): boolean {
    return this.storedStructureImages().has(structureId);
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

  protected async save(): Promise<void> {
    this.form.markAllAsTouched();
    this.serverFields.set(new Set());
    this.successMessage.set(null);
    const failures = this.collectFailures();
    if (failures.length > 0) {
      this.revealErrors(failures);
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
          detail = await this.campaignsApi.uploadStructureImage(detail.id, structureId, file, detail.revision);
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
      this.structureImages.clear();
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

      this.revision = campaign.revision;
      this.hasExistingMap.set(campaign.hasMap);
      this.rememberStoredFiles(campaign);
      this.form.patchValue({
        name: campaign.name,
        description: campaign.description ?? '',
        playerCount: campaign.playerSlotCount,
        isPrivate: campaign.isPrivate,
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
        this.phases,
        campaign.phases.map((phase) => this.createPhaseGroup(phase.kind, phase.durationAmount, phase.durationUnit)),
      );
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
    options?: { id?: string; color?: string; requiresSubfaction?: boolean },
  ): FactionGroup {
    const names = subfactions.length > 0 ? subfactions : [''];
    return this.formBuilder.nonNullable.group({
      id: [options?.id ?? crypto.randomUUID()],
      name: [name, [required, maxLength(60)]],
      color: [options?.color ?? '#2563EB', required],
      requiresSubfaction: [options?.requiresSubfaction === true],
      allyGroupName: [allyGroupName],
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

  private createStructureGroup(id?: string, name = '', builtinSymbol = '', missions?: MissionGroup[]): StructureGroup {
    return this.formBuilder.nonNullable.group({
      id: [id ?? this.newId()],
      name: [name, [required, maxLength(60)]],
      builtinSymbol: [builtinSymbol],
      clearImage: [false],
      missions: this.formBuilder.array<MissionGroup>(missions ?? [this.createMissionGroup()]),
    });
  }

  private createStructureGroupFromDetail(type: CampaignStructureType): StructureGroup {
    const missions =
      type.missions.length > 0
        ? type.missions.map((mission) => this.createMissionGroupFromDetail(mission))
        : [this.createMissionGroup()];
    return this.createStructureGroup(type.id, type.name, type.builtinSymbol ?? '', missions);
  }

  private createMissionGroupFromDetail(mission: CampaignMission): MissionGroup {
    return this.createMissionGroup(mission.id, mission.name, mission.url ?? '', false);
  }

  private createDefaultTerrainGroups(): TerrainGroup[] {
    return defaultTerrainCatalog().map((entry) =>
      this.createTerrainGroup(undefined, entry.name, entry.color, entry.missionName),
    );
  }

  private createDefaultStructureGroups(): StructureGroup[] {
    return defaultStructureCatalog().map((entry) =>
      this.createStructureGroup(undefined, entry.name, entry.builtinSymbol),
    );
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
      clearImage: type.clearImage,
      missions: type.missions
        .filter((mission) => mission.name.trim().length > 0 || mission.url.trim().length > 0)
        .map((mission) => this.toMissionPayload(mission)),
    }));

    return {
      name: value.name.trim(),
      description: value.description.trim() || null,
      playerCount: Number(value.playerCount),
      isPrivate: value.isPrivate,
      joinPassword: value.isPrivate && value.joinPassword.trim().length > 0 ? value.joinPassword : null,
      creatorIsParticipant: value.creatorRole === 'both',
      factions,
      allyGroups,
      links,
      terrainTypes,
      structureTypes,
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

  private collectFailures(): string[] {
    const failures: string[] = [];
    const labels: Record<string, string> = {
      name: 'Campaign name',
      description: 'Description',
      playerCount: 'Number of players',
      startsAtLocal: 'Start date and time',
      timeZoneId: 'Campaign time zone',
      roundCount: 'Number of rounds',
      roundLengthAmount: 'Round length',
    };
    for (const [name, label] of Object.entries(labels)) {
      const message = describeControlError(this.form.get(name), label);
      if (message) {
        failures.push(message);
      }
    }

    if (this.form.controls.isPrivate.value) {
      const password = this.form.controls.joinPassword.value;
      if (!this.isEdit() && password.trim().length === 0) {
        failures.push('Private campaigns require a join password.');
      } else if (password.length > 0 && password.length < 8) {
        failures.push('Join password is too short (minimum 8 characters).');
      }
    }

    this.factions.controls.forEach((faction, index) => {
      const message = describeControlError(faction.controls.name, `Faction ${index + 1} name`);
      if (message) {
        failures.push(message);
      }

      const namedSubfactions = faction.controls.subfactions.controls.filter(
        (item) => item.controls.name.value.trim().length > 0,
      );
      if (faction.controls.requiresSubfaction.value && namedSubfactions.length === 0) {
        failures.push(`Faction ${index + 1} requires at least one subfaction.`);
      }
    });

    if (this.factions.length < 2) {
      failures.push('At least 2 factions are required.');
    }

    const usedFactionColors = new Set<string>();
    this.factions.controls.forEach((faction, index) => {
      const color = faction.controls.color.value.toUpperCase();
      if (usedFactionColors.has(color)) {
        failures.push(`Faction ${index + 1} color must be unique.`);
      }

      usedFactionColors.add(color);
    });

    if (this.terrainTypes.length < 1) {
      failures.push('At least 1 terrain type is required.');
    }

    const usedTerrainColors = new Set<string>();
    this.terrainTypes.controls.forEach((terrain, index) => {
      const nameMessage = describeControlError(terrain.controls.name, `Terrain type ${index + 1} name`);
      if (nameMessage) {
        failures.push(nameMessage);
      }

      const color = terrain.controls.color.value.toUpperCase();
      if (usedTerrainColors.has(color)) {
        failures.push(`Terrain type ${index + 1} color must be unique.`);
      }

      usedTerrainColors.add(color);

      const namedMissions = terrain.controls.missions.controls.filter(
        (mission) => mission.controls.name.value.trim().length > 0,
      );
      if (namedMissions.length === 0) {
        failures.push(`Terrain type ${index + 1} requires at least one mission.`);
      }
    });

    this.structureTypes.controls.forEach((structure, index) => {
      const nameMessage = describeControlError(structure.controls.name, `Structure ${index + 1} name`);
      if (nameMessage) {
        failures.push(nameMessage);
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
      }
    }

    const actionCount = this.actionCount();
    const battleCount = this.phases.controls.filter((phase) => phase.controls.kind.value === 'Battle').length;
    if (actionCount < 1 || battleCount < 1) {
      failures.push('A round must include at least one action and one battle phase.');
    }

    this.phases.controls.forEach((phase, index) => {
      const amountMessage = describeControlError(phase.controls.durationAmount, `Round step ${index + 1} length`);
      if (amountMessage) {
        failures.push(amountMessage);
        return;
      }

      const rangeMessage = durationRangeMessage(
        `Round step ${index + 1} length`,
        Number(phase.controls.durationAmount.value),
        phase.controls.durationUnit.value,
      );
      if (rangeMessage) {
        failures.push(rangeMessage);
      }
    });

    this.allyGroups.controls.forEach((group, index) => {
      if (group.controls.name.value.trim().length === 0) {
        failures.push(`Ally group ${index + 1} name is not filled in.`);
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
      } else if (labelMessage) {
        failures.push(labelMessage);
      }

      const urlMessage = describeControlError(link.controls.url, `Link ${index + 1} URL`);
      if (!link.controls.url.value.trim()) {
        failures.push(`Link ${index + 1} URL is not filled in.`);
      } else if (urlMessage) {
        failures.push(urlMessage);
      }
    });

    return failures;
  }

  private rememberStoredFiles(campaign: CampaignDetail): void {
    this.storedStructureImages.set(
      new Set(campaign.structureTypes.filter((type) => type.hasImage).map((type) => type.id)),
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
