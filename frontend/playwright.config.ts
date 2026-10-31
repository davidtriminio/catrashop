import { defineConfig, devices } from '@playwright/test';

/**
 * End-to-end tests for the customer web app, driven by a real browser against a live `ng serve`.
 *
 * The specs here are deliberately infrastructure-free: they cover client-side routing, guard redirects,
 * and form rendering — behaviour that holds without the backend running (a fresh browser has no session,
 * so protected routes redirect to /login purely client-side). Full journeys that need the API and a
 * database are part of the Docker-up verification pass.
 */
const PORT = 4300;

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? 'line' : [['list']],
  use: {
    baseURL: `http://localhost:${PORT}`,
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: {
    command: `pnpm exec ng serve catrashop-web --port ${PORT}`,
    url: `http://localhost:${PORT}`,
    reuseExistingServer: !process.env.CI,
    timeout: 180_000,
    stdout: 'ignore',
    stderr: 'pipe',
  },
});
