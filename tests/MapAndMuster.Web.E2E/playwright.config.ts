import { defineConfig, devices } from '@playwright/test';

const consentCookie = {
  name: 'cookie_consent',
  value: encodeURIComponent(JSON.stringify({ version: 1, preferences: true })),
  domain: '127.0.0.1',
  path: '/',
  expires: Math.floor(Date.now() / 1000) + 60 * 60 * 24 * 180,
  httpOnly: false,
  secure: false,
  sameSite: 'Lax' as const,
};

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  reporter: process.env['CI'] ? 'github' : 'html',
  use: {
    baseURL: 'http://127.0.0.1:4200',
    trace: 'on-first-retry',
    storageState: {
      cookies: [consentCookie],
      origins: [],
    },
  },
  webServer: {
    command: 'npm start -- --host 127.0.0.1 --port 4200',
    cwd: '../../src/MapAndMuster.Web',
    url: 'http://127.0.0.1:4200',
    reuseExistingServer: !process.env['CI'],
    timeout: 120_000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
