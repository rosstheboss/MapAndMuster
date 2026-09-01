import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const campaignId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const player = {
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
  isAdministrator: false,
  inAppNotificationsEnabled: true,
  emailNotificationsEnabled: true,
  preferredChatLanguage: 'English',
};

const admin = { ...player, isAdministrator: true };

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
  mentionableMembers: [{ userId: player.id, username: 'ada', displayName: 'ada' }],
  log: [],
  standings: [
    {
      userId: player.id,
      username: 'ada',
      displayName: 'ada',
      factionId: 'north',
      factionName: 'North',
      factionColor: '#2563EB',
      hasFlagImage: false,
      allyGroupName: null,
      territoryAndStructurePoints: 4,
      battlesWonPoints: 2,
      publicObjectivePoints: 1,
      privateObjectivePoints: 0,
      otherPoints: 3,
      total: 10,
      heldItems: [],
    },
  ],
};

const png = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
  'base64',
);

const coast = {
  id: 't1',
  displayNumber: 1,
  name: 'Coast',
  description: null,
  polygon: [
    { x: 0.1, y: 0.1 },
    { x: 0.4, y: 0.1 },
    { x: 0.4, y: 0.4 },
    { x: 0.1, y: 0.4 },
  ],
  terrainTypeId: null,
  structureTypeId: null,
  structureCondition: 'Operational',
  overlayColor: '#2563EB',
  ownerFactionId: 'north',
  spawnFactionId: null,
};

function formatViolations(violations: { id: string; help: string; nodes: { html: string }[] }[]): string {
  return violations
    .map((violation) => `${violation.id}: ${violation.help}\n${violation.nodes.map((node) => node.html).join('\n')}`)
    .join('\n\n');
}

async function expectNoAxeViolations(page: Page, options?: { exclude?: string[] }): Promise<void> {
  let builder = new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'best-practice']);
  for (const selector of options?.exclude ?? []) {
    builder = builder.exclude(selector);
  }

  const results = await builder.analyze();
  expect(results.violations, formatViolations(results.violations)).toEqual([]);
}

async function mockSession(page: Page, profile: typeof player): Promise<void> {
  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
  });
  await page.route('**/api/auth/external-providers', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });
}

async function mockMapImage(page: Page): Promise<void> {
  await page.route(`**/api/campaigns/${campaignId}/map**`, async (route) => {
    if (route.request().url().includes('/map/graph')) {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: png,
    });
  });
}

test('login has no axe violations', async ({ page }) => {
  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: '{"code":"auth.unauthorized","message":"Sign in to continue."}',
    });
  });
  await page.route('**/api/auth/external-providers', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  await page.goto('/');
  await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Sign in' })).toHaveAttribute('aria-current', 'page');
  await expectNoAxeViolations(page);
});

test('campaign list has no axe violations', async ({ page }) => {
  await mockSession(page, player);
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
          currentRound: 1,
          currentPhaseLabel: 'Action 1',
          currentPhaseKind: 'Action',
          currentPhaseEndsUtc: '2026-08-14T12:06:00+00:00',
          canPlay: true,
          canChooseFaction: false,
          isCommitted: false,
          status: 'InProgress',
          startsUtc: '2026-08-14T12:00:00+00:00',
          endsUtc: '2026-08-16T12:00:00+00:00',
        },
      ]),
    });
  });

  await page.goto('/campaigns');
  await expect(page.getByRole('heading', { level: 1, name: 'Your campaigns' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Your campaigns' })).toHaveAttribute('aria-current', 'page');
  await expectNoAxeViolations(page);
});

test('campaign detail has no axe violations', async ({ page }) => {
  await mockSession(page, player);
  await page.route(`**/api/campaigns/${campaignId}/log`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: campaignId,
        revision: campaign.revision,
        canChat: true,
        mentionableMembers: campaign.mentionableMembers,
        chatChannels: [{ kind: 'Public', label: 'Everyone' }],
        log: [],
      }),
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
        debugDrafts: [],
        canDebug: true,
        isDebugActive: false,
        debugActorUserId: null,
        commitments: [],
        battles: [],
        log: [],
        standings: campaign.standings,
        playersMissingFaction: [],
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
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(campaign) });
  });

  await page.goto(`/campaigns/${campaignId}`);
  await expect(page.getByRole('heading', { level: 1, name: 'Border War' })).toBeVisible();
  await expectNoAxeViolations(page);
});

test('campaign map territories are keyboard selectable', async ({ page }) => {
  await mockSession(page, player);
  await page.route(`**/api/campaigns/${campaignId}/log`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: campaignId,
        revision: campaign.revision,
        canChat: true,
        mentionableMembers: campaign.mentionableMembers,
        chatChannels: [{ kind: 'Public', label: 'Everyone' }],
        log: [],
      }),
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
        hasMap: true,
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
        debugDrafts: [],
        canDebug: true,
        isDebugActive: false,
        debugActorUserId: null,
        commitments: [],
        battles: [],
        log: [],
        standings: campaign.standings,
        playersMissingFaction: [],
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
        territories: [
          coast,
          ...Array.from({ length: 24 }, (_, index) => ({
            ...coast,
            id: `extra-${index}`,
            displayNumber: index + 2,
            name: `Ridge ${index + 1}`,
            ownerFactionId: null,
            polygon: [
              { x: 0.55, y: 0.05 + index * 0.02 },
              { x: 0.7, y: 0.05 + index * 0.02 },
              { x: 0.7, y: 0.07 + index * 0.02 },
              { x: 0.55, y: 0.07 + index * 0.02 },
            ],
          })),
        ],
        adjacencies: [],
      }),
    });
  });
  await mockMapImage(page);
  await page.route(`**/api/campaigns/${campaignId}`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ ...campaign, hasMap: true }),
    });
  });

  await page.goto(`/campaigns/${campaignId}`);
  await expect(page.getByRole('heading', { level: 1, name: 'Border War' })).toBeVisible();
  await page.getByRole('button', { name: 'Expand All' }).click();
  const details = page.locator('.map-meta');
  await expect(details).toContainText('Select a territory to see its details.');
  await page.locator('.map-section').scrollIntoViewIfNeeded();
  const scroller = page.locator('.territory-directory-body');
  await expect
    .poll(async () =>
      scroller.evaluate((element) => ({
        overflowY: getComputedStyle(element).overflowY,
        canScroll: element.scrollHeight > element.clientHeight + 1,
      })),
    )
    .toEqual({ overflowY: 'auto', canScroll: true });
  const mapBody = page.locator('.map-body');
  const mapBodyEmpty = await mapBody.boundingBox();
  expect(mapBodyEmpty).toBeTruthy();

  await page.locator('.map-legend').evaluate((element) => {
    (element as HTMLDetailsElement).open = true;
  });
  await expect(page.getByText('Ownership tint')).toBeVisible();
  const mapBodyWithLegend = await mapBody.boundingBox();
  expect(mapBodyWithLegend?.height).toBeCloseTo(mapBodyEmpty!.height, 1);

  await page.locator('.territory-directory').evaluate((element) => {
    (element as HTMLDetailsElement).open = false;
  });
  const mapBodyDirectoryClosed = await mapBody.boundingBox();
  expect(mapBodyDirectoryClosed?.height).toBeCloseTo(mapBodyEmpty!.height, 1);
  await page.locator('.territory-directory').evaluate((element) => {
    (element as HTMLDetailsElement).open = true;
  });
  const mapBodyDirectoryOpen = await mapBody.boundingBox();
  expect(mapBodyDirectoryOpen?.height).toBeCloseTo(mapBodyEmpty!.height, 1);

  const emptyBox = await details.boundingBox();
  expect(emptyBox).toBeTruthy();

  await page.getByRole('button', { name: 'Full screen' }).click();
  await expect(page.getByRole('button', { name: 'Exit full screen' })).toBeVisible();
  const fullscreenEmpty = await details.boundingBox();
  const fullscreenEmptyViewport = await page.locator('.map-viewport').boundingBox();

  const hit = page.locator('.territory-hit[data-id="t1"]');
  await expect(hit).toHaveAttribute('role', 'button');
  await hit.focus();
  await expect(hit).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(details).toContainText('Coast');
  await expect(details).not.toContainText('Select a territory to see its details.');
  const fullscreenSelected = await details.boundingBox();
  const fullscreenSelectedViewport = await page.locator('.map-viewport').boundingBox();
  expect(fullscreenSelected?.height).toBeCloseTo(fullscreenEmpty!.height, 1);
  expect(fullscreenSelectedViewport?.height).toBeCloseTo(fullscreenEmptyViewport!.height, 1);

  await page.keyboard.press('Escape');
  await expect(page.getByRole('button', { name: 'Full screen' })).toBeVisible();
  await expect(page.locator('app-campaign-map-view')).not.toHaveClass(/is-fullscreen/);
  await expect
    .poll(async () => Math.round((await details.boundingBox())?.height ?? 0))
    .toBe(Math.round(emptyBox!.height));
});

test('campaign setup has no axe violations', async ({ page }) => {
  test.setTimeout(90_000);
  await mockSession(page, admin);

  await page.goto('/campaigns/new');
  await expect(page.getByRole('heading', { level: 1, name: 'Create campaign' })).toBeVisible();
  await expect(page.getByLabel('Upload campaign preset')).toBeAttached();
  await expectNoAxeViolations(page);
});

test('map editor has no axe violations', async ({ page }) => {
  await mockSession(page, player);
  await page.route(`**/api/campaigns/${campaignId}/map/graph`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        campaignId,
        revision: 2,
        canManage: true,
        territories: [coast],
        adjacencies: [],
      }),
    });
  });
  await page.route(`**/api/campaigns/${campaignId}`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        ...campaign,
        hasMap: true,
        status: 'Scheduled',
        currentRound: null,
        currentPhaseNumber: null,
        currentPhaseKind: null,
        currentPhaseStartsUtc: null,
        currentPhaseEndsUtc: null,
      }),
    });
  });
  await mockMapImage(page);

  await page.goto(`/campaigns/${campaignId}/map`);
  await expect(page.getByRole('heading', { level: 1, name: 'Map editor' })).toBeVisible();
  await expect(page.locator('.territory-hit[data-id="t1"]')).toHaveAttribute('role', 'button');
  await expectNoAxeViolations(page);
});
