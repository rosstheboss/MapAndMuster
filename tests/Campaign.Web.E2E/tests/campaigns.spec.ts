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
  await expect(page.getByRole('button', { name: 'Generate Connections' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Clear connections' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Download map' })).toBeVisible();
  await expect(page.getByRole('radio', { name: 'Draw' })).toBeVisible();
});
