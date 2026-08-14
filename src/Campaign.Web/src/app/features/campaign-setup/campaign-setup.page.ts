import { Component, inject, signal, viewChild, type ElementRef } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, type FormArray, type FormControl, type FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { readApiErrorMessages, readApiFieldErrors } from '../../core/auth/auth.service';
import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignDetail, SaveCampaignPayload } from '../../core/campaigns/campaign.models';
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
type FactionGroup = FormGroup<{
  name: FormControl<string>;
  allyGroupName: FormControl<string>;
  subfactions: FormArray<NamedGroup>;
}>;

@Component({
  selector: 'app-campaign-setup-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './campaign-setup.page.html',
  styleUrl: './campaign-setup.page.css',
})
export class CampaignSetupPage {
  private readonly campaignsApi = inject(CampaignService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formAlert = viewChild<ElementRef<HTMLElement>>('formAlert');

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessages = signal<string[]>([]);
  protected readonly serverFields = signal<ReadonlySet<string>>(new Set());
  protected readonly campaignId = signal<string | null>(null);
  protected readonly hasExistingMap = signal(false);
  protected readonly mapFileName = signal<string | null>(null);
  private mapFile: File | null = null;
  private revision = 0;

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [required, minLength(3), maxLength(80)]],
    description: ['', maxLength(500)],
    playerCount: [8, [required, minValue(2), maxValue(100)]],
    isPrivate: [false],
    joinPassword: [''],
    creatorRole: this.formBuilder.nonNullable.control<'manager' | 'both'>('both'),
    factions: this.formBuilder.array<FactionGroup>([this.createFactionGroup(), this.createFactionGroup()]),
    allyGroups: this.formBuilder.array<NamedGroup>([]),
    links: this.formBuilder.array<LinkGroup>([]),
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

  protected isEdit(): boolean {
    return this.campaignId() !== null;
  }

  protected descriptionLength(): number {
    return this.form.controls.description.value.length;
  }

  protected subfactionsOf(faction: FactionGroup): FormArray<NamedGroup> {
    return faction.controls.subfactions;
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

  protected addFaction(): void {
    this.factions.push(this.createFactionGroup());
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

  protected onMapSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.mapFile = file;
    this.mapFileName.set(file?.name ?? null);
  }

  protected async save(): Promise<void> {
    this.form.markAllAsTouched();
    this.serverFields.set(new Set());
    const failures = this.collectFailures();
    if (failures.length > 0) {
      this.revealErrors(failures);
      return;
    }

    this.saving.set(true);
    this.errorMessages.set([]);
    try {
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

      await this.router.navigate(['/campaigns', detail.id]);
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
      this.form.patchValue({
        name: campaign.name,
        description: campaign.description ?? '',
        playerCount: campaign.playerSlotCount,
        isPrivate: campaign.isPrivate,
        creatorRole: campaign.creatorIsParticipant ? 'both' : 'manager',
      });
      this.replaceArray(
        this.factions,
        campaign.factions.map((faction) =>
          this.createFactionGroup(faction.name, faction.allyGroupName ?? '', faction.subfactions),
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

  private createFactionGroup(name = '', allyGroupName = '', subfactions: readonly string[] = ['']): FactionGroup {
    const names = subfactions.length > 0 ? subfactions : [''];
    return this.formBuilder.nonNullable.group({
      name: [name, [required, maxLength(60)]],
      allyGroupName: [allyGroupName],
      subfactions: this.formBuilder.array<NamedGroup>(names.map((value) => this.createNamedGroup(value))),
    });
  }

  private createNamedGroup(name = ''): NamedGroup {
    return this.formBuilder.nonNullable.group({
      name: [name, maxLength(60)],
    });
  }

  private createLinkGroup(label = '', url = ''): LinkGroup {
    return this.formBuilder.nonNullable.group({
      label: [label, maxLength(80)],
      url: [url, [maxLength(2048), httpUrl]],
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
      name: faction.name.trim(),
      allyGroupName: faction.allyGroupName.trim() || null,
      subfactions: faction.subfactions.map((item) => item.name.trim()).filter((name) => name.length > 0),
    }));
    const links = value.links
      .filter((link) => link.label.trim().length > 0 || link.url.trim().length > 0)
      .map((link) => ({ label: link.label.trim(), url: link.url.trim() }));

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
    };
  }

  private collectFailures(): string[] {
    const failures: string[] = [];
    const labels: Record<string, string> = {
      name: 'Campaign name',
      description: 'Description',
      playerCount: 'Number of players',
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
    });

    if (this.factions.length < 2) {
      failures.push('At least 2 factions are required.');
    }

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

  private revealErrors(messages: readonly string[]): void {
    this.errorMessages.set([...messages]);
    queueMicrotask(() => scrollAlertIntoView(this.formAlert()?.nativeElement));
  }
}
