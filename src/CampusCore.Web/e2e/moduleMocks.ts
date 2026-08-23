import type { Page, Route } from '@playwright/test';

async function fulfillJson(route: Route, body: unknown): Promise<void> {
  await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
}

export async function mockModuleApi(page: Page): Promise<void> {
  await page.route('**/api/students/**', async (route) => fulfillJson(route, { items: [], page: 1, pageSize: 25, total: 0 }));
  await page.route('**/api/catalog/academic-years', async (route) => fulfillJson(route, []));
  await page.route('**/api/catalog/classes', async (route) => fulfillJson(route, []));
  await page.route('**/api/catalog/subjects', async (route) => fulfillJson(route, []));
  await page.route('**/api/catalog/grade-scales', async (route) => fulfillJson(route, []));
  await page.route('**/api/operations/staff', async (route) => fulfillJson(route, []));
  await page.route('**/api/announcements/**', async (route) => fulfillJson(route, []));
  await page.route('**/api/admin/settings', async (route) => fulfillJson(route, {
    id: 'settings',
    institutionName: 'CampusCore E2E',
    address: null,
    timeZoneId: 'Asia/Kolkata',
    locale: 'en-IN',
    dateFormat: 'dd/MM/yyyy',
    defaultPageSize: 25,
    allowGuardianPortal: false,
  }));
  await page.route('**/api/admin/audit*', async (route) => fulfillJson(route, { items: [], page: 1, pageSize: 50, total: 0 }));
  await page.route('**/api/admin/users/**', async (route) => fulfillJson(route, []));
}
