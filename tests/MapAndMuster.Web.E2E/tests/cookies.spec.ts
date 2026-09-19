import { expect, test } from '@playwright/test';

test.use({ storageState: { cookies: [], origins: [] } });

test('cookie banner lets a visitor accept or reject cookies', async ({ page }) => {
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

  await page.goto('/login');
  await expect(page.getByRole('heading', { level: 2, name: 'Cookies' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'cookie notice' })).toBeVisible();
  await page.getByRole('button', { name: 'Reject non-essential' }).click();
  await expect(page.getByRole('heading', { level: 2, name: 'Cookies' })).toHaveCount(0);
  await expect(page.getByRole('contentinfo').getByRole('link', { name: 'Cookies' })).toBeVisible();
});
