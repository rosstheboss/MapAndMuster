import { Component, input } from '@angular/core';

@Component({
  selector: 'app-campaign-map-preview',
  templateUrl: './campaign-map-preview.component.html',
  styleUrl: './campaign-map-preview.component.css',
})
export class CampaignMapPreviewComponent {
  readonly imageUrl = input.required<string>();
}
