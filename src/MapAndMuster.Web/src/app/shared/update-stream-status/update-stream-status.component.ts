import { Component, computed, input } from '@angular/core';

import {
  type UpdateStreamState,
  updateStreamStatusDetail,
  updateStreamStatusLabel,
} from '../../core/campaigns/update-stream';

@Component({
  selector: 'app-update-stream-status',
  templateUrl: './update-stream-status.component.html',
  styleUrl: './update-stream-status.component.css',
})
export class UpdateStreamStatusComponent {
  readonly state = input.required<UpdateStreamState>();

  protected readonly label = computed(() => updateStreamStatusLabel(this.state()));
  protected readonly detail = computed(() => updateStreamStatusDetail(this.state()));
}
