import { expect, type Page } from '@playwright/test';

export type TestRole = 'Administrator' | 'Registrar' | 'Teacher';

const dashboard = {
  activeStudents: 248,
  activeStaff: 31,
  sections: 12,
  presentToday: 221,
  absentToday: 9,
  pendingLeaveRequests: 4,
  publishedAnnouncements: 3,
};

export async function mockAuthenticatedApi(
  page: Page,
  roles: TestRole[] = ['Administrator'],
): Promise<void> {
  const authResponse = {
    accessToken: 'campuscore-e2e-token',
    expiresAtUtc: '2099-01-01T00:00:00Z',
    displayName: 'E2E Administrator',
    roles,
  };

  await page.route('**/api/auth/login', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(authResponse) });
  });

  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ id: 'e2e-user', name: 'E2E Administrator', roles }),
    });
  });

  await page.route('**/api/dashboard', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(dashboard) });
  });
}

export async function signIn(
  page: Page,
  roles: TestRole[] = ['Administrator'],
): Promise<void> {
  await mockAuthenticatedApi(page, roles);
  await page.goto('/login');
  await page.getByLabel('Email').fill('admin@example.test');
  await page.getByLabel('Password').fill('StrongPassword!123');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
}
