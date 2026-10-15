import { expect, test } from '@playwright/test';
import { signIn } from './support';

test('protects authenticated routes and completes sign-in/sign-out', async ({ page }) => {
  await page.goto('/students');
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'CampusCore' })).toBeVisible();

  await signIn(page);
  await expect(page.getByText('248')).toBeVisible();
  await expect(page.getByText('Active students')).toBeVisible();
  await expect(page.getByRole('link', { name: 'Settings & audit' })).toBeVisible();

  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('button', { name: 'Sign in', exact: true })).toBeVisible();
});

test('keeps administrator-only routes unavailable to teachers', async ({ page }) => {
  await signIn(page, ['Teacher']);
  await expect(page.getByRole('link', { name: 'Settings & audit' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Academic catalog' })).toHaveCount(0);

  await page.goto('/settings');
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
});
