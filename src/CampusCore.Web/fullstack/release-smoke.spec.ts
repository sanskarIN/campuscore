import { expect, test } from '@playwright/test';

const administrator = {
  displayName: 'CampusCore Release Administrator',
  email: 'release-admin@example.test',
  password: 'CampusCore!Release2026',
};

const student = {
  admissionNumber: 'E2E-0200',
  firstName: 'Release',
  lastName: 'Candidate',
  dateOfBirth: '2012-04-12',
  email: 'release-candidate@example.test',
};

const bootstrapKey = process.env.CAMPUSCORE_E2E_BOOTSTRAP_KEY ?? 'campuscore-fullstack-bootstrap-key-2026';

test('runs critical administrator workflows against the real stack', async ({ page }) => {
  await test.step('bootstrap the first administrator through the real authentication UI', async () => {
    await page.goto('/login');
    await page.getByRole('button', { name: 'First-run setup' }).click();
    await page.getByLabel('Display name').fill(administrator.displayName);
    await page.getByLabel('Email').fill(administrator.email);
    await page.getByLabel('Password').fill(administrator.password);
    await page.getByLabel('Bootstrap key').fill(bootstrapKey);
    await page.getByRole('button', { name: 'Create first administrator' }).click();

    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByText(administrator.displayName)).toBeVisible();
  });

  await test.step('create and persist a real student record', async () => {
    await page.getByRole('link', { name: 'Students', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Students' })).toBeVisible();
    await page.getByRole('button', { name: 'Add student' }).click();

    await page.getByLabel('Admission number').fill(student.admissionNumber);
    await page.getByLabel('Date of birth').fill(student.dateOfBirth);
    await page.getByLabel('First name').fill(student.firstName);
    await page.getByLabel('Last name').fill(student.lastName);
    await page.getByLabel('Email').fill(student.email);
    await page.getByRole('button', { name: 'Create student' }).click();

    const row = page.getByRole('row').filter({ hasText: student.admissionNumber });
    await expect(row).toContainText(`${student.firstName} ${student.lastName}`);
    await row.getByRole('button', { name: 'View' }).click();
    await expect(page.getByRole('heading', { name: `${student.firstName} ${student.lastName}` })).toBeVisible();
    await expect(page.getByText(student.email)).toBeVisible();
  });

  await test.step('publish a real announcement', async () => {
    await page.getByRole('link', { name: 'Announcements', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Announcements' })).toBeVisible();
    await page.getByRole('button', { name: 'New announcement' }).click();

    await page.getByLabel('Title').fill('v0.2.0 release-candidate smoke notice');
    await page.getByLabel('Message').fill('This fictional notice verifies the real announcement persistence workflow.');
    await page.getByRole('button', { name: 'Publish', exact: true }).click();

    await expect(page.getByRole('status')).toContainText('Announcement published.');
    await expect(page.getByRole('heading', { name: 'v0.2.0 release-candidate smoke notice' })).toBeVisible();
  });

  await test.step('persist institution settings and verify its audit event', async () => {
    await page.getByRole('link', { name: 'Settings & audit' }).click();
    await expect(page.getByRole('heading', { name: 'Settings & audit' })).toBeVisible();

    const institutionName = page.getByLabel('Name');
    await institutionName.fill('CampusCore v0.2.0 E2E Institution');
    await page.getByRole('button', { name: 'Save settings' }).click();
    await expect(page.getByRole('status')).toContainText('Institution settings saved.');

    await page.reload();
    await expect(page.getByRole('heading', { name: 'Settings & audit' })).toBeVisible();
    await expect(page.getByLabel('Name')).toHaveValue('CampusCore v0.2.0 E2E Institution');
    await expect(page.getByText('settings.updated')).toBeVisible();
  });

  await test.step('sign out and restore the protected-route boundary', async () => {
    await page.getByRole('button', { name: 'Sign out' }).click();
    await expect(page).toHaveURL(/\/login$/);

    await page.goto('/settings');
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole('button', { name: 'Sign in', exact: true })).toBeVisible();
  });
});
