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
  await expect(page.getByLabel('Campaign name')).toBeVisible();
  await expect(page.getByLabel('Start date and time')).toBeVisible();
  await expect(page.getByLabel('Number of rounds')).toBeVisible();
  await expect(page.getByLabel('Faction preset')).toBeVisible();
  await page.getByLabel('Faction preset').selectOption('Warhammer: The Old World');
  await page.getByRole('button', { name: 'Add preset' }).click();
  await expect(page.getByLabel('Faction 1 name')).toHaveValue('Beastmen Brayherds');
  await page.getByRole('button', { name: 'Create campaign' }).click();
  await expect(page.getByRole('alert')).toContainText('Campaign name is not filled in.');
  await expect(page.getByRole('alert')).toContainText('Start date and time is not filled in.');
});
