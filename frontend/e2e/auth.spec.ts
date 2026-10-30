import { expect, test } from '@playwright/test';

test.describe('authentication shell (no backend required)', () => {
  test('an anonymous visit to a protected route redirects to login with a returnUrl', async ({ page }) => {
    await page.goto('/account');

    await expect(page).toHaveURL(/\/login\?returnUrl=%2Faccount/);
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
  });

  test('the login form renders and enables the submit button once filled', async ({ page }) => {
    await page.goto('/login');

    const email = page.locator('#email');
    const password = page.locator('#password');
    const submit = page.getByRole('button', { name: 'Iniciar sesión' });

    await expect(email).toBeVisible();
    await expect(password).toBeVisible();
    await expect(submit).toBeDisabled();

    await email.fill('user@catrashop.test');
    await password.fill('a-password');

    await expect(submit).toBeEnabled();
  });

  test('the register link navigates to the registration page', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('link', { name: 'Crea una' }).click();

    await expect(page).toHaveURL(/\/register$/);
  });
});
