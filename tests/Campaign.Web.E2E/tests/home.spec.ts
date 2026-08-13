import { expect, test } from '@playwright/test';

test('unauthenticated visitors are sent to sign in', async ({ page }) => {
  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({ status: 401, contentType: 'application/json', body: '{"code":"auth.unauthorized","message":"Sign in to continue."}' });
  });
  await page.route('**/api/auth/external-providers', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  await page.goto('/');
  await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible();
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
});

test('register page collects profile fields on a phone-sized screen', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route('**/api/auth/external-providers', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  await page.goto('/register');
  await expect(page.getByRole('heading', { level: 1, name: 'Create an account' })).toBeVisible();
  await expect(page.getByRole('textbox', { name: 'Username' })).toBeVisible();
  await expect(page.getByLabel('City')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Create account' })).toBeVisible();
});

test('home shows the signed-in player and logout', async ({ page }) => {
  const profile = {
    id: '11111111-1111-1111-1111-111111111111',
    email: 'ada@example.test',
    username: 'ada',
    firstName: 'Ada',
    middleInitial: null,
    lastName: 'Lovelace',
    city: 'Halifax',
    region: null,
    country: 'Canada',
    displayNameMode: 'Username',
    hasAvatar: false,
    createdUtc: '2026-08-13T00:00:00+00:00',
    updatedUtc: '2026-08-13T00:00:00+00:00',
    profileRevision: 1,
    emailConfirmed: true,
  };

  let authenticated = true;
  await page.route('**/api/auth/me', async (route) => {
    if (authenticated) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(profile) });
      return;
    }

    await route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: '{"code":"auth.unauthorized","message":"Sign in to continue."}',
    });
  });
  await page.route('**/api/auth/logout', async (route) => {
    authenticated = false;
    await route.fulfill({ status: 204, body: '' });
  });
  await page.route('**/api/auth/external-providers', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  await page.goto('/');
  await expect(page.getByRole('heading', { level: 1, name: 'Home' })).toBeVisible();
  await expect(page.getByText('You are signed in as')).toBeVisible();
  await page.getByRole('button', { name: 'Log out' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible();
});
