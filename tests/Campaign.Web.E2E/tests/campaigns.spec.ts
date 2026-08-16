import { expect, test } from '@playwright/test';

const profile = {
  id: '11111111-1111-1111-1111-111111111111',
  email: 'ada@example.test',
  username: 'ada',
  firstName: 'Ada',
  middleInitial: null,
  lastName: 'Lovelace',
  suffix: null,
  city: 'Halifax',
  region: null,
  country: 'Canada',
  displayNameMode: 'Username',
  timeZoneId: null,
  hasAvatar: false,
  createdUtc: '2026-08-13T00:00:00+00:00',
  updatedUtc: '2026-08-13T00:00:00+00:00',
  profileRevision: 1,
  emailConfirmed: true,
};

test('signed-in players can open their campaigns and start setup', async ({ page }) => {
  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
  });
  await page.route('**/api/campaigns', async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      return;
    }

    await route.fallback();
  });

  await page.goto('/campaigns');
  await expect(page.getByRole('heading', { level: 1, name: 'Your campaigns' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Create campaign' })).toBeVisible();
  await page.getByRole('link', { name: 'Create campaign' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Create campaign' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Back to campaigns' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Expand All' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Collapse All' })).toBeVisible();
  await expect(page.getByLabel('Campaign name')).toBeVisible();
  await expect(page.getByLabel('City (optional)')).toBeVisible();
  await expect(page.getByRole('checkbox', { name: 'Publicly viewable' })).toBeChecked();
  await expect(page.getByLabel('Start date and time')).toBeVisible();
  await expect(page.getByLabel('Number of rounds')).toBeVisible();
  await expect(page.getByLabel('Faction preset')).toBeVisible();
  await page.getByLabel('Faction preset').selectOption('Warhammer: The Old World');
  await page.getByRole('button', { name: 'Add preset' }).click();
  await expect(page.getByLabel('Faction 1 name')).toHaveValue('Beastmen Brayherds');
  await expect(page.getByLabel('Faction 3 name')).toHaveValue('Daemons of Chaos');
  await expect(page.getByLabel('Faction 3 subfaction 1')).toHaveValue('Khorne');
  await expect(page.getByLabel('Faction 3 subfaction 2')).toHaveValue('Nurgle');
  await expect(page.getByLabel('Faction 3 subfaction 3')).toHaveValue('Slaanesh');
  await expect(page.getByLabel('Faction 3 subfaction 4')).toHaveValue('Tzeentch');
  await expect(page.getByRole('checkbox', { name: 'Players who choose this faction must pick a subfaction' }).nth(2)).toBeChecked();
  await expect(page.getByLabel('Terrain 1 name')).toHaveValue('Beach');
  await page.getByRole('button', { name: 'Create campaign' }).click();
  await expect(page.getByRole('alert')).toContainText('Campaign name is not filled in.');
  await expect(page.getByRole('alert')).toContainText('Start date and time is not filled in.');
});

test('signed-in players can browse all campaigns', async ({ page }) => {
  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
  });
  await page.route('**/api/campaigns/all', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  await page.goto('/campaigns/all');
  await expect(page.getByRole('heading', { level: 1, name: 'All campaigns' })).toBeVisible();
  await expect(page.getByText('No campaigns are available to join or view right now.')).toBeVisible();
});

test('managers can open the map editor after setup', async ({ page }) => {
  const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
  });
  await page.route(`**/api/campaigns/${campaignId}/map/graph`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        campaignId,
        revision: 2,
        canManage: true,
        territories: [],
        adjacencies: [],
      }),
    });
  });
  await page.route(`**/api/campaigns/${campaignId}`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: campaignId,
        name: 'Border War',
        description: null,
        playerSlotCount: 8,
        occupiedPlayerSlots: 1,
        isPrivate: false,
        isPubliclyViewable: true,
        creatorIsParticipant: true,
        city: null,
        region: null,
        country: null,
        hasMap: true,
        canManage: true,
        isParticipant: true,
        revision: 2,
        createdUtc: '2026-08-13T00:00:00+00:00',
        updatedUtc: '2026-08-13T00:00:00+00:00',
        factions: [
          { id: '1', name: 'North', color: '#2563EB', subfactions: [], allyGroupName: null, requiresSubfaction: false, hasFlagImage: false },
          { id: '2', name: 'South', color: '#DC2626', subfactions: [], allyGroupName: null, requiresSubfaction: false, hasFlagImage: false },
        ],
        allyGroups: [],
        links: [],
        terrainTypes: [],
        structureTypes: [],
        timeZoneId: 'UTC',
        startsAtLocal: '2099-01-05T12:00',
        startsUtc: '2099-01-05T12:00:00+00:00',
        endsUtc: '2099-03-02T12:00:00+00:00',
        roundCount: 8,
        roundLengthAmount: 1,
        roundLengthUnit: 'Weeks',
        phases: [],
        status: 'Scheduled',
        currentRound: null,
        currentPhaseNumber: null,
        currentPhaseKind: null,
        currentPhaseStartsUtc: null,
        currentPhaseEndsUtc: null,
      }),
    });
  });
  await page.route(`**/api/campaigns/${campaignId}/map**`, async (route) => {
    if (route.request().url().includes('/map/graph')) {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: Buffer.from(
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
        'base64',
      ),
    });
  });

  await page.goto(`/campaigns/${campaignId}/map`);
  await expect(page.getByRole('heading', { level: 1, name: 'Map editor' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Auto Generate Connections' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Clear Connections' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Download map' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Download SVG data' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Upload SVG' })).toBeVisible();
  await expect(page.getByRole('radio', { name: 'Draw' })).toBeVisible();
});

test('players can duplicate a campaign from Your campaigns', async ({ page }) => {
  const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const copyId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
  });
  await page.route('**/api/campaigns', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: campaignId,
          name: 'Border War',
          description: null,
          playerSlotCount: 8,
          occupiedPlayerSlots: 1,
          isPrivate: false,
          isPubliclyViewable: true,
          canManage: true,
          isParticipant: true,
          canView: true,
          canJoin: false,
          canLeave: false,
          city: null,
          region: null,
          country: null,
          currentRound: null,
          currentPhaseLabel: null,
          currentPhaseEndsUtc: null,
          canPlay: false,
          status: 'Scheduled',
          startsUtc: '2099-01-05T12:00:00+00:00',
          endsUtc: '2099-03-02T12:00:00+00:00',
        },
      ]),
    });
  });
  await page.route(`**/api/campaigns/${campaignId}/duplicate`, async (route) => {
    await route.fulfill({
      status: 201,
      contentType: 'application/json',
      body: JSON.stringify({
        id: copyId,
        name: 'Border War',
        description: null,
        playerSlotCount: 8,
        occupiedPlayerSlots: 1,
        isPrivate: false,
        isPubliclyViewable: true,
        creatorIsParticipant: true,
        city: null,
        region: null,
        country: null,
        hasMap: true,
        canManage: true,
        isParticipant: true,
        revision: 1,
        createdUtc: '2026-08-14T00:00:00+00:00',
        updatedUtc: '2026-08-14T00:00:00+00:00',
        factions: [],
        allyGroups: [],
        links: [],
        terrainTypes: [],
        structureTypes: [],
        timeZoneId: 'UTC',
        startsAtLocal: '2026-08-21T00:00',
        startsUtc: '2026-08-21T00:00:00+00:00',
        endsUtc: '2026-10-16T00:00:00+00:00',
        roundCount: 8,
        roundLengthAmount: 1,
        roundLengthUnit: 'Weeks',
        phases: [],
        status: 'Scheduled',
        currentRound: null,
        currentPhaseNumber: null,
        currentPhaseKind: null,
        currentPhaseStartsUtc: null,
        currentPhaseEndsUtc: null,
      }),
    });
  });
  await page.route(`**/api/campaigns/${copyId}`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: copyId,
        name: 'Border War',
        description: null,
        playerSlotCount: 8,
        occupiedPlayerSlots: 1,
        isPrivate: false,
        isPubliclyViewable: true,
        creatorIsParticipant: true,
        city: null,
        region: null,
        country: null,
        hasMap: true,
        canManage: true,
        isParticipant: true,
        revision: 1,
        createdUtc: '2026-08-14T00:00:00+00:00',
        updatedUtc: '2026-08-14T00:00:00+00:00',
        factions: [],
        allyGroups: [],
        links: [],
        terrainTypes: [],
        structureTypes: [],
        timeZoneId: 'UTC',
        startsAtLocal: '2026-08-21T00:00',
        startsUtc: '2026-08-21T00:00:00+00:00',
        endsUtc: '2026-10-16T00:00:00+00:00',
        roundCount: 8,
        roundLengthAmount: 1,
        roundLengthUnit: 'Weeks',
        phases: [],
        status: 'Scheduled',
        currentRound: null,
        currentPhaseNumber: null,
        currentPhaseKind: null,
        currentPhaseStartsUtc: null,
        currentPhaseEndsUtc: null,
      }),
    });
  });

  await page.goto('/campaigns');
  await page.getByRole('button', { name: 'Border War' }).click();
  await page.getByRole('button', { name: 'Duplicate campaign' }).click();
  await expect(page).toHaveURL(`/campaigns/${copyId}/edit`);
});

test('players can read and chat in the campaign log', async ({ page }) => {
  const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const campaign = {
    id: campaignId,
    name: 'Border War',
    description: 'A contested frontier.',
    playerSlotCount: 8,
    occupiedPlayerSlots: 1,
    isPrivate: false,
    isPubliclyViewable: true,
    creatorIsParticipant: true,
    city: null,
    region: null,
    country: null,
    hasMap: false,
    canManage: true,
    isParticipant: true,
    revision: 2,
    createdUtc: '2026-08-13T00:00:00+00:00',
    updatedUtc: '2026-08-13T00:00:00+00:00',
    factions: [
      {
        id: 'north',
        name: 'North',
        color: '#2563EB',
        subfactions: [],
        allyGroupName: null,
        requiresSubfaction: false,
        hasFlagImage: false,
      },
    ],
    allyGroups: [],
    links: [],
    terrainTypes: [],
    structureTypes: [],
    timeZoneId: 'UTC',
    startsAtLocal: '2026-08-14T12:00',
    startsUtc: '2026-08-14T12:00:00+00:00',
    endsUtc: '2026-08-16T12:00:00+00:00',
    roundCount: 3,
    roundLengthAmount: 1,
    roundLengthUnit: 'Days',
    phases: [],
    status: 'InProgress',
    currentRound: 1,
    currentPhaseNumber: 1,
    currentPhaseKind: 'Action',
    currentPhaseStartsUtc: '2026-08-14T12:00:00+00:00',
    currentPhaseEndsUtc: '2026-08-14T12:06:00+00:00',
    factionId: 'north',
    subfaction: null,
    canPlay: true,
    canChooseFaction: false,
    canChat: true,
    mentionableMembers: [{ userId: profile.id, username: 'ada', displayName: 'ada' }],
    log: [
      {
        id: 'log-1',
        occurredUtc: '2026-08-15T20:45:23-04:00',
        kind: 'ResolvedAction',
        originator: 'Campaign',
        summary: 'North held in Coast.',
        territoryId: 't1',
        forceId: 'force-1',
        battleId: null,
        isSystemAdjustment: false,
      },
    ],
  };

  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ ...profile, timeZoneId: 'America/New_York' }),
    });
  });
  await page.route(`**/api/campaigns/${campaignId}/play`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: campaignId,
        name: 'Border War',
        revision: 2,
        canManage: true,
        isParticipant: true,
        canChat: true,
        mentionableMembers: campaign.mentionableMembers,
        status: 'InProgress',
        currentRound: 1,
        currentPhaseNumber: 1,
        currentPhaseKind: 'Action',
        currentPhaseLabel: 'Action 1',
        currentPhaseStartsUtc: '2026-08-14T12:00:00+00:00',
        currentPhaseEndsUtc: '2026-08-14T12:06:00+00:00',
        currentWindowId: 'window-1',
        hasMap: false,
        factionId: 'north',
        canChooseFaction: false,
        isCommitted: false,
        roundCount: 3,
        minRoundCount: 1,
        remainingWindows: [],
        factions: campaign.factions,
        structureTypes: [],
        forces: [],
        myDrafts: [],
        orders: [],
        commitments: [],
        battles: [],
        log: campaign.log,
        playersMissingFaction: [],
      }),
    });
  });
  await page.route(`**/api/campaigns/${campaignId}/chat`, async (route) => {
    const body = route.request().postDataJSON() as { message: string };
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        ...campaign,
        revision: 3,
        log: [
          ...campaign.log,
          {
            id: 'log-2',
            occurredUtc: '2026-08-15T20:46:23-04:00',
            kind: 'PlayerChat',
            originator: 'ada',
            summary: body.message,
            territoryId: null,
            forceId: null,
            battleId: null,
            isSystemAdjustment: false,
          },
        ],
      }),
    });
  });
  await page.route(`**/api/campaigns/${campaignId}/map/graph`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        campaignId,
        revision: 2,
        canManage: true,
        territories: [],
        adjacencies: [],
      }),
    });
  });
  await page.route(`**/api/campaigns/${campaignId}`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(campaign),
    });
  });

  await page.goto(`/campaigns/${campaignId}`);
  await expect(page.getByRole('heading', { level: 1, name: 'Border War' })).toBeVisible();
  await expect(page.getByText('Campaign log')).toBeVisible();
  await expect(page.getByText('(2026-08-15 08:45:23 PM EDT)')).toBeVisible();
  await expect(page.getByText('Campaign:')).toBeVisible();
  await expect(page.getByText('North held in Coast.')).toBeVisible();
  await expect(page.getByText('Phase ends in')).toBeVisible();
  await expect(page.getByRole('link', { name: 'Play' })).toBeVisible();
  await expect(page.getByText('Commit orders')).toHaveCount(0);
  await page.getByLabel('Message').fill('Hey, everybody! This is a message to all of you.');
  await page.getByRole('button', { name: 'Send' }).click();
  await expect(page.getByText('ada:')).toBeVisible();
  await expect(page.getByText('Hey, everybody! This is a message to all of you.')).toBeVisible();
  await expect(page.getByText('Successfully saved changes.')).toHaveCount(0);
  await expect(page.getByText('Saving')).toHaveCount(0);
});
