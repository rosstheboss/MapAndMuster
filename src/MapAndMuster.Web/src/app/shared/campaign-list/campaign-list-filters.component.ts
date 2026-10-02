import { Component, computed, effect, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { preferencesAllowed } from '../../core/cookies/cookie-consent';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import { IconComponent } from '../icon/icon.component';
import { campaignMatchesFilters, emptyCampaignListFilters, type CampaignListFilters } from './campaign-list.filters';

const FILTER_COOKIE = 'campaign_list_filters';
const FILTER_MAX_AGE_SECONDS = 60 * 60 * 24 * 365;

@Component({
  selector: 'app-campaign-list-filters',
  imports: [FormsModule, IconComponent],
  templateUrl: './campaign-list-filters.component.html',
  styleUrl: './campaign-list-filters.component.css',
})
export class CampaignListFiltersComponent {
  readonly campaigns = input.required<readonly CampaignListItem[]>();
  readonly filtered = output<readonly CampaignListItem[]>();

  protected readonly open = signal(true);
  protected readonly draft = signal<CampaignListFilters>(readStoredFilters());
  protected readonly applied = signal<CampaignListFilters>(readStoredFilters());
  protected readonly managerQuery = signal('');
  protected readonly participantQuery = signal('');

  protected readonly knownUsernames = computed(() => {
    const names = new Set<string>();
    for (const campaign of this.campaigns()) {
      if (campaign.managerUsername) {
        names.add(campaign.managerUsername);
      }

      for (const name of campaign.publicParticipantUsernames ?? []) {
        names.add(name);
      }
    }

    return [...names].sort((left, right) => left.localeCompare(right));
  });

  constructor() {
    effect(() => {
      const filters = this.applied();
      this.filtered.emit(this.campaigns().filter((campaign) => campaignMatchesFilters(campaign, filters)));
    });
  }

  protected patch(partial: Partial<CampaignListFilters>): void {
    this.draft.update((current) => ({ ...current, ...partial }));
  }

  protected apply(): void {
    const next = cloneFilters(this.draft());
    this.applied.set(next);
    writeStoredFilters(next);
  }

  protected clear(): void {
    const next = emptyCampaignListFilters();
    this.draft.set(next);
    this.applied.set(cloneFilters(next));
    this.managerQuery.set('');
    this.participantQuery.set('');
    writeStoredFilters(next);
  }

  protected suggestions(query: string, selected: readonly string[]): string[] {
    const needle = query.trim().toLowerCase();
    if (needle.length < 1) {
      return [];
    }

    return this.knownUsernames()
      .filter(
        (name) =>
          name.toLowerCase().includes(needle) && !selected.some((item) => item.toLowerCase() === name.toLowerCase()),
      )
      .slice(0, 8);
  }

  protected addName(kind: 'managers' | 'participants', raw: string): void {
    const name = raw.trim();
    if (!name) {
      return;
    }

    this.draft.update((current) => {
      const selected = current[kind];
      if (selected.some((item) => item.toLowerCase() === name.toLowerCase())) {
        return current;
      }

      return { ...current, [kind]: [...selected, name] };
    });
    if (kind === 'managers') {
      this.managerQuery.set('');
    } else {
      this.participantQuery.set('');
    }
  }

  protected removeName(kind: 'managers' | 'participants', name: string): void {
    this.draft.update((current) => ({
      ...current,
      [kind]: current[kind].filter((item) => item.toLowerCase() !== name.toLowerCase()),
    }));
  }

  protected onNameKey(kind: 'managers' | 'participants', event: KeyboardEvent): void {
    if (event.key !== 'Enter') {
      return;
    }

    event.preventDefault();
    const query = kind === 'managers' ? this.managerQuery() : this.participantQuery();
    const match = this.suggestions(query, this.draft()[kind]).at(0);
    this.addName(kind, match ?? query);
  }

  protected roundCounts(): string[] {
    return this.distinct(this.campaigns().map((item) => (item.roundCount ? `${item.roundCount}` : null)));
  }

  protected roundLengths(): string[] {
    return this.distinct(
      this.campaigns().map((item) =>
        item.roundLengthAmount && item.roundLengthUnit ? `${item.roundLengthAmount} ${item.roundLengthUnit}` : null,
      ),
    );
  }

  protected distinct(values: (string | null | undefined)[]): string[] {
    return [...new Set(values.map((value) => value?.trim()).filter((value): value is string => !!value))].sort((a, b) =>
      a.localeCompare(b),
    );
  }
}

function cloneFilters(filters: CampaignListFilters): CampaignListFilters {
  return { ...filters, participants: [...filters.participants], managers: [...filters.managers] };
}

function readStoredFilters(): CampaignListFilters {
  const empty = emptyCampaignListFilters();
  if (!preferencesAllowed()) {
    return empty;
  }

  const match = new RegExp(`(?:^|; )${FILTER_COOKIE}=([^;]*)`).exec(document.cookie);
  if (!match?.[1]) {
    return empty;
  }

  try {
    const parsed = JSON.parse(decodeURIComponent(match[1])) as Partial<CampaignListFilters>;
    return {
      ...empty,
      ...parsed,
      participants: Array.isArray(parsed.participants) ? parsed.participants : [],
      managers: Array.isArray(parsed.managers) ? parsed.managers : [],
    };
  } catch {
    return empty;
  }
}

function writeStoredFilters(filters: CampaignListFilters): void {
  if (!preferencesAllowed()) {
    return;
  }

  document.cookie = `${FILTER_COOKIE}=${encodeURIComponent(JSON.stringify(filters))}; Path=/; Max-Age=${FILTER_MAX_AGE_SECONDS}; SameSite=Lax`;
}
