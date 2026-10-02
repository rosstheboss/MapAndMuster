import { campaignMatchesFilters, emptyCampaignListFilters } from './campaign-list.filters';
import type { CampaignListItem } from '../../core/campaigns/campaign.models';

const campaign = (overrides: Partial<CampaignListItem> = {}): CampaignListItem => ({
  id: 'c1',
  name: 'Border War',
  description: null,
  playerSlotCount: 8,
  occupiedPlayerSlots: 2,
  isPrivate: false,
  isPubliclyViewable: true,
  canManage: false,
  isParticipant: false,
  canView: true,
  canJoin: true,
  canLeave: false,
  city: 'Estalia',
  region: 'Coast',
  country: 'Fictional',
  status: 'Scheduled',
  startsUtc: '2026-10-01T00:00:00Z',
  endsUtc: '2026-12-01T00:00:00Z',
  currentRound: null,
  currentPhaseLabel: null,
  currentPhaseKind: null,
  currentPhaseEndsUtc: null,
  canPlay: false,
  canChooseFaction: false,
  isCommitted: false,
  isFreeForAll: true,
  gameSystem: 'Bolt Action',
  managerUsername: 'ross',
  publicParticipantUsernames: ['ada', 'bea'],
  ...overrides,
});

describe('campaign list filters', () => {
  it('matches a partial title and free-for-all', () => {
    const filters = { ...emptyCampaignListFilters(), title: 'border', alliance: 'free-for-all' as const };
    expect(campaignMatchesFilters(campaign(), filters)).toBe(true);
    expect(campaignMatchesFilters(campaign({ isFreeForAll: false }), filters)).toBe(false);
  });

  it('matches any selected player username', () => {
    const filters = { ...emptyCampaignListFilters(), participants: ['bea', 'missing'] };
    expect(campaignMatchesFilters(campaign(), filters)).toBe(true);
    expect(campaignMatchesFilters(campaign({ publicParticipantUsernames: ['ada'] }), filters)).toBe(false);
  });

  it('treats unspecified location as empty', () => {
    const filters = { ...emptyCampaignListFilters(), city: 'unspecified' };
    expect(campaignMatchesFilters(campaign({ city: null }), filters)).toBe(true);
    expect(campaignMatchesFilters(campaign(), filters)).toBe(false);
  });
});
