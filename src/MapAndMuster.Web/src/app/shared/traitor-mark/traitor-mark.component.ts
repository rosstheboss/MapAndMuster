import { Component, computed, input } from '@angular/core';

export interface TraitorVictim {
  userId?: string | null;
  username?: string | null;
  displayName?: string | null;
  factionName: string;
  subfaction?: string | null;
}

@Component({
  selector: 'app-traitor-mark',
  templateUrl: './traitor-mark.component.html',
  styleUrl: './traitor-mark.component.css',
})
export class TraitorMarkComponent {
  readonly victims = input<readonly TraitorVictim[]>([]);

  protected readonly label = computed(() => {
    const listed = this.victims();
    if (listed.length === 0) {
      return 'Traitor';
    }

    const names = listed.map((victim) => victimLabel(victim)).join('; ');
    return `Betrayed ${names}`;
  });
}

function victimLabel(victim: TraitorVictim): string {
  const displayName = victim.displayName?.trim();
  const username = victim.username?.trim();
  const who = displayName && displayName.length > 0 ? displayName : username;
  const subfaction = victim.subfaction?.trim();
  const faction = subfaction && subfaction.length > 0 ? `${victim.factionName} (${subfaction})` : victim.factionName;
  if (who && who.length > 0) {
    return `${who} · ${faction}`;
  }

  return faction;
}
