import { expect, test } from '@playwright/test';
import { signIn } from './support';

test('surfaces the private-data offline state without hiding the app shell', async ({ page, context }) => {
  await signIn(page);

  await context.setOffline(true);
  await page.evaluate(() => window.dispatchEvent(new Event('offline')));

  await expect(page.getByText('Offline shell', { exact: true })).toBeVisible();
  await expect(page.getByRole('status')).toContainText('private API data is never cached');
  await expect(page.getByRole('navigation', { name: 'Primary navigation' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
});

test('supports keyboard navigation through the authenticated shell', async ({ page }) => {
  await signIn(page);

  await page.getByRole('link', { name: 'Search', exact: true }).focus();
  await page.keyboard.press('Enter');

  await expect(page).toHaveURL(/\/search$/);
  await expect(page.getByRole('heading', { name: 'Search' })).toBeVisible();
});
