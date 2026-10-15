import { expect, test } from '@playwright/test';
import { mockModuleApi } from './moduleMocks';
import { signIn } from './support';

const routes = [
  ['/search', 'Global search'],
  ['/students', 'Students'],
  ['/academics', 'Academics'],
  ['/operations', 'Enrollment, leave & timetable'],
  ['/staff', 'Staff directory'],
  ['/announcements', 'Announcements'],
  ['/catalog', 'Academic catalog'],
  ['/settings', 'Settings & audit'],
  ['/about', 'CampusCore'],
] as const;

test('renders every primary application module for an administrator', async ({ page }) => {
  await signIn(page);
  await mockModuleApi(page);

  for (const [path, heading] of routes) {
    await page.goto(path);
    await expect(page).toHaveURL(new RegExp(`${path.replace('/', '\\/')}$`));
    await expect(page.getByRole('heading', { name: heading, exact: true }).first()).toBeVisible();
    await expect(page.getByRole('alert')).toHaveCount(0);
  }
});

test('About page exposes current public links and no retired contact', async ({ page }) => {
  await signIn(page);
  await page.goto('/about');
  await expect(page.getByRole('link', { name: 'Author website' })).toHaveAttribute('href', 'https://sanskarin.github.io');
  await expect(page.getByRole('link', { name: 'Buy Me a Coffee' })).toHaveAttribute('href', 'https://buymeacoffee.com/SanskarCodes');
  await expect(page.locator('a[href^="mailto:"]')).toHaveCount(2);
});
