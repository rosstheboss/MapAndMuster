import {
  cookieNameFor,
  DEFAULT_STANDINGS_SORT,
  nextStandingsSort,
  readStoredPrefs,
  sortStandings,
  writeStoredPrefs,
} from './campaign-view-prefs.service';
import type { CampaignPointStanding } from './campaign.models';

function standing(overrides: Partial<CampaignPointStanding>): CampaignPointStanding {
  return {
    userId: '1',
    username: 'alpha',
    displayName: 'Alpha',
    factionName: 'North',
    allyGroupName: 'Pact',
    territoryAndStructurePoints: 1,
    battlesWonPoints: 2,
    publicObjectivePoints: 3,
    privateObjectivePoints: 0,
    otherPoints: 4,
    total: 10,
    ...overrides,
  };
}

describe('campaign view prefs', () => {
  beforeEach(() => {
    document.cookie.split(';').forEach((part) => {
      const name = part.split('=')[0]?.trim();
      if (name) {
        document.cookie = `${name}=; Path=/; Max-Age=0`;
      }
    });
  });

  it('round-trips highlight, sections, sort, and chat position in a cookie', () => {
    const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    writeStoredPrefs(campaignId, {
      highlightMode: 'alliance',
      sections: { map: false, standings: true },
      standingsSort: { column: 'battles', direction: 'asc' },
      chatChannelKey: 'Direct:user-2',
      chatScrollTop: 48,
    });

    expect(document.cookie).toContain(cookieNameFor(campaignId));
    expect(readStoredPrefs(campaignId)).toEqual({
      highlightMode: 'alliance',
      sections: { map: false, standings: true },
      standingsSort: { column: 'battles', direction: 'asc' },
      chatChannelKey: 'Direct:user-2',
      chatScrollTop: 48,
    });
  });

  it('sorts standings by total descending by default and toggles columns', () => {
    const rows = [
      standing({ userId: '2', displayName: 'Beta', total: 4, battlesWonPoints: 9 }),
      standing({ userId: '1', displayName: 'Alpha', total: 10, battlesWonPoints: 1 }),
    ];
    expect(sortStandings(rows, DEFAULT_STANDINGS_SORT).map((row) => row.displayName)).toEqual(['Alpha', 'Beta']);
    expect(
      sortStandings(rows, nextStandingsSort(DEFAULT_STANDINGS_SORT, 'displayName')).map((row) => row.displayName),
    ).toEqual(['Alpha', 'Beta']);
    expect(nextStandingsSort({ column: 'displayName', direction: 'asc' }, 'displayName')).toEqual({
      column: 'displayName',
      direction: 'desc',
    });
  });
});
