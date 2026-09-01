import type { CampaignListItem } from '../../core/campaigns/campaign.models';
import {
  campaignAttentionItems,
  campaignCanOpen,
  campaignCommitLabel,
  campaignPlayerCountText,
  campaignRemainingSetupLabel,
  campaignRoundPhaseText,
} from './campaign-list.summary';

function item(
  overrides: Partial<CampaignListItem> & Pick<CampaignListItem, 'id' | 'name' | 'status' | 'startsUtc' | 'endsUtc'>,
): CampaignListItem {
  return {
    description: null,
    playerSlotCount: 8,
    occupiedPlayerSlots: 4,
    isPrivate: false,
    isPubliclyViewable: true,
    canManage: false,
    isParticipant: true,
    canView: true,
    canJoin: false,
    canLeave: false,
    city: null,
    region: null,
    country: null,
    currentRound: null,
    currentPhaseLabel: null,
    currentPhaseKind: null,
    currentPhaseEndsUtc: null,
    canPlay: false,
    canChooseFaction: false,
    isCommitted: false,
    ...overrides,
  };
}

describe('campaign list summary helpers', () => {
  it('shows round and phase only for in-progress campaigns', () => {
    expect(
      campaignRoundPhaseText(
        item({
          id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          name: 'Current War',
          status: 'InProgress',
          startsUtc: '2098-01-01T12:00:00+00:00',
          endsUtc: '2099-06-01T12:00:00+00:00',
          currentRound: 3,
          currentPhaseLabel: 'Action 1',
        }),
      ),
    ).toBe('Round 3 · Action 1');
    expect(
      campaignRoundPhaseText(
        item({
          id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          name: 'Border War',
          status: 'Scheduled',
          startsUtc: '2099-01-05T12:00:00+00:00',
          endsUtc: '2099-03-02T12:00:00+00:00',
        }),
      ),
    ).toBeNull();
  });

  it('labels remaining faction setup from the server flag', () => {
    expect(
      campaignRemainingSetupLabel(
        item({
          id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          name: 'Border War',
          status: 'Scheduled',
          startsUtc: '2099-01-05T12:00:00+00:00',
          endsUtc: '2099-03-02T12:00:00+00:00',
          canChooseFaction: true,
        }),
      ),
    ).toBe('Choose your faction');
    expect(
      campaignRemainingSetupLabel(
        item({
          id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          name: 'Current War',
          status: 'InProgress',
          startsUtc: '2098-01-01T12:00:00+00:00',
          endsUtc: '2099-06-01T12:00:00+00:00',
        }),
      ),
    ).toBeNull();
  });

  it('shows a commit label only for action-phase participants who already have a faction', () => {
    const active = item({
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      name: 'Current War',
      status: 'InProgress',
      startsUtc: '2098-01-01T12:00:00+00:00',
      endsUtc: '2099-06-01T12:00:00+00:00',
      currentRound: 2,
      currentPhaseLabel: 'Action 1',
      currentPhaseKind: 'Action',
      isParticipant: true,
    });
    expect(campaignCommitLabel(active)).toBe('Not committed');
    expect(campaignCommitLabel({ ...active, isCommitted: true })).toBe('Committed');
    expect(campaignCommitLabel({ ...active, canChooseFaction: true })).toBeNull();
    expect(campaignCommitLabel({ ...active, currentPhaseKind: 'Battle' })).toBeNull();
    expect(campaignCommitLabel({ ...active, isParticipant: false })).toBeNull();
  });

  it('opens a campaign the viewer can view or play', () => {
    const listed = item({
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      name: 'Open War',
      status: 'Scheduled',
      startsUtc: '2099-02-01T12:00:00+00:00',
      endsUtc: '2099-04-01T12:00:00+00:00',
      canView: false,
      canPlay: false,
      canJoin: true,
    });
    expect(campaignCanOpen(listed)).toBe(false);
    expect(campaignCanOpen({ ...listed, canView: true })).toBe(true);
    expect(campaignCanOpen({ ...listed, canPlay: true })).toBe(true);
    expect(campaignPlayerCountText(listed)).toBe('4 of 8 players');
  });

  it('lists in-progress campaigns soonest-ending first for Home', () => {
    const later = item({
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      name: 'Later War',
      status: 'InProgress',
      startsUtc: '2098-01-01T12:00:00+00:00',
      endsUtc: '2099-06-01T12:00:00+00:00',
    });
    const sooner = item({
      id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      name: 'Sooner War',
      status: 'InProgress',
      startsUtc: '2098-01-01T12:00:00+00:00',
      endsUtc: '2099-05-01T12:00:00+00:00',
    });
    const upcoming = item({
      id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
      name: 'Border War',
      status: 'Scheduled',
      startsUtc: '2099-01-05T12:00:00+00:00',
      endsUtc: '2099-03-02T12:00:00+00:00',
    });
    expect(campaignAttentionItems([later, upcoming, sooner]).map((campaign) => campaign.name)).toEqual([
      'Sooner War',
      'Later War',
    ]);
  });
});
