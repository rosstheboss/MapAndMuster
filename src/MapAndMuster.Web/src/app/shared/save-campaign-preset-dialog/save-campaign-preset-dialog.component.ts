import { Component, effect, inject, input, output, signal, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

import { CampaignService } from '../../core/campaigns/campaign.service';
import type { CampaignPresetListItem } from '../../core/campaigns/campaign.models';
import { campaignPresetSaveNames } from '../../core/campaigns/campaign-presets';
import { AppDialogComponent } from '../dialog/dialog.component';
import { FilterableComboboxComponent } from '../filterable-combobox/filterable-combobox.component';

@Component({
  selector: 'app-save-campaign-preset-dialog',
  imports: [ReactiveFormsModule, FilterableComboboxComponent, AppDialogComponent],
  templateUrl: './save-campaign-preset-dialog.component.html',
  styleUrl: './save-campaign-preset-dialog.component.css',
})
export class SaveCampaignPresetDialogComponent {
  private readonly campaignsApi = inject(CampaignService);
  private readonly formBuilder = inject(FormBuilder);

  readonly open = input(false);
  readonly saving = input(false);

  readonly closed = output<void>();
  readonly confirmed = output<string>();

  protected readonly presetNameControl = this.formBuilder.nonNullable.control('');
  private readonly savedPresets = signal<CampaignPresetListItem[]>([]);
  protected readonly presetNames = computed(() =>
    campaignPresetSaveNames(this.savedPresets().map((preset) => preset.name)),
  );

  constructor() {
    effect(() => {
      if (this.open()) {
        this.presetNameControl.setValue('');
        void this.loadPresets();
      }
    });
  }

  protected close(): void {
    this.closed.emit();
  }

  protected confirm(): void {
    this.confirmed.emit(this.presetNameControl.value.trim());
  }

  private async loadPresets(): Promise<void> {
    try {
      this.savedPresets.set(await this.campaignsApi.listPresets());
    } catch {
      this.savedPresets.set([]);
    }
  }
}
