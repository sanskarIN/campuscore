import { expect, test } from '@playwright/test';

interface StoredSession {
  accessToken: string;
}

interface IdResponse {
  id: string;
}

interface StudentSearchResponse {
  items: Array<{ id: string; admissionNumber: string; displayName: string }>;
}

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
const today = new Date().toISOString().slice(0, 10);
const calendarYear = today.slice(0, 4);

let accessToken = '';
let origin = '';
let studentId = '';
let academicYearId = '';
let sectionId = '';
let subjectId = '';

test('runs critical administrator and academic workflows against the real stack', async ({ page }) => {
  const authHeaders = () => ({ Authorization: `Bearer ${accessToken}` });
  const apiUrl = (path: string) => `${origin}${path}`;

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

    origin = new URL(page.url()).origin;
    const session = await page.evaluate(() => {
      const raw = window.sessionStorage.getItem('campuscore.auth');
      if (!raw) throw new Error('CampusCore session was not stored after bootstrap.');
      return JSON.parse(raw) as StoredSession;
    });
    accessToken = session.accessToken;
    expect(accessToken.length).toBeGreaterThan(20);
  });

  await test.step('create a real student and primary guardian', async () => {
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

    const searchResponse = await page.request.get(
      apiUrl(`/api/students/?q=${encodeURIComponent(student.admissionNumber)}&active=true&page=1&pageSize=25`),
      { headers: authHeaders() },
    );
    expect(searchResponse.ok()).toBeTruthy();
    const search = await searchResponse.json() as StudentSearchResponse;
    expect(search.items).toHaveLength(1);
    studentId = search.items[0]?.id ?? '';
    expect(studentId).not.toBe('');

    const guardianResponse = await page.request.post(apiUrl(`/api/students/${studentId}/guardians`), {
      headers: authHeaders(),
      data: {
        name: 'Guardian Example',
        relationship: 'Parent',
        email: 'guardian@example.test',
        phone: null,
        isPrimary: true,
      },
    });
    expect(guardianResponse.status()).toBe(201);

    await row.getByRole('button', { name: 'View' }).click();
    await expect(page.getByRole('heading', { name: `${student.firstName} ${student.lastName}` })).toBeVisible();
    await expect(page.getByText(student.email)).toBeVisible();
    await expect(page.getByText('Guardian Example')).toBeVisible();
    await expect(page.getByText(/Parent · Primary/u)).toBeVisible();
  });

  await test.step('create the academic catalog needed for an enrollment and report card', async () => {
    const create = async (path: string, data: unknown): Promise<string> => {
      const response = await page.request.post(apiUrl(path), { headers: authHeaders(), data });
      expect(response.status(), `${path} should return Created`).toBe(201);
      return ((await response.json()) as IdResponse).id;
    };

    academicYearId = await create('/api/catalog/academic-years', {
      name: `E2E ${calendarYear}`,
      startsOn: `${calendarYear}-01-01`,
      endsOn: `${calendarYear}-12-31`,
      isActive: true,
    });
    const classId = await create('/api/catalog/classes', { name: 'Grade 8 E2E', sortOrder: 8 });
    sectionId = await create('/api/catalog/sections', { schoolClassId: classId, name: 'A', capacity: 40 });
    subjectId = await create('/api/catalog/subjects', { code: 'E2E-MATH', name: 'Release Mathematics', maximumMarks: 100 });
  });

  await test.step('enroll the student through the real operations UI', async () => {
    await page.getByRole('link', { name: 'Operations', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Enrollment, leave & timetable' })).toBeVisible();

    const enrollmentForm = page.getByRole('heading', { name: 'Create enrollment' }).locator('..');
    await enrollmentForm.getByLabel('Student search').fill(student.admissionNumber);
    await enrollmentForm.getByRole('button', { name: 'Find' }).click();
    await enrollmentForm.getByRole('option').filter({ hasText: student.admissionNumber }).click();
    await enrollmentForm.getByLabel('Academic year').selectOption(academicYearId);
    await enrollmentForm.getByLabel('Section').selectOption(sectionId);
    await enrollmentForm.getByLabel('Enrollment date').fill(today);
    await enrollmentForm.getByLabel('Roll number').fill('20');
    await enrollmentForm.getByRole('button', { name: 'Create enrollment' }).click();

    await expect(page.getByText('Enrollment created.', { exact: true })).toBeVisible();
  });

  await test.step('record attendance and marks through the real academics UI', async () => {
    await page.getByRole('link', { name: 'Academics', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Academics' })).toBeVisible();

    const attendanceForm = page.getByRole('heading', { name: 'Record attendance' }).locator('..');
    await attendanceForm.getByLabel('Student ID').fill(studentId);
    await attendanceForm.getByLabel('Date').fill(today);
    await attendanceForm.getByLabel('Status').selectOption({ label: 'Present' });
    await attendanceForm.getByLabel('Note').fill('Full-stack release smoke attendance');
    await attendanceForm.getByRole('button', { name: 'Save attendance' }).click();
    await expect(page.getByText('Attendance saved.', { exact: true })).toBeVisible();

    const markForm = page.getByRole('heading', { name: 'Record mark' }).locator('..');
    await markForm.getByLabel('Student ID').fill(studentId);
    await markForm.getByLabel('Academic year').selectOption(academicYearId);
    await markForm.getByLabel('Subject').selectOption(subjectId);
    await markForm.getByLabel('Assessment').fill('Release smoke assessment');
    await markForm.getByLabel('Score').fill('88');
    await markForm.getByLabel('Maximum').fill('100');
    await markForm.getByRole('button', { name: 'Record mark' }).click();
    await expect(page.getByText('Mark recorded.', { exact: true })).toBeVisible();
  });

  await test.step('render the persisted report card through the real operations UI', async () => {
    await page.getByRole('link', { name: 'Operations', exact: true }).click();
    const reportSection = page.getByRole('heading', { name: 'Report card' }).locator('..').locator('..');

    await reportSection.getByLabel('Student search').fill(student.admissionNumber);
    await reportSection.getByRole('button', { name: 'Find' }).click();
    await reportSection.getByRole('option').filter({ hasText: student.admissionNumber }).click();
    await reportSection.getByLabel('Academic year').selectOption(academicYearId);
    await reportSection.getByRole('button', { name: 'Generate report' }).click();

    await expect(reportSection.getByRole('heading', { name: `${student.firstName} ${student.lastName}` })).toBeVisible();
    const subjectRow = reportSection.getByRole('row').filter({ hasText: 'E2E-MATH' });
    await expect(subjectRow).toContainText('Release Mathematics');
    await expect(subjectRow).toContainText('88%');
    await expect(subjectRow).toContainText('A');
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
