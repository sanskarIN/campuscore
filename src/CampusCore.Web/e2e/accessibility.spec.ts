import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';
import { signIn } from './support';

async function expectNoSeriousAccessibilityViolations(page: Page): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();

  const serious = results.violations.filter(
    (violation) => violation.impact === 'serious' || violation.impact === 'critical',
  );
  const details = serious
    .map((violation) => `${violation.id}: ${violation.help} (${violation.nodes.length} node(s))`)
    .join('\n');

  expect(serious, details).toEqual([]);
}

test('login surface has no serious WCAG A/AA violations', async ({ page }) => {
  await page.goto('/login');
  await expect(page.getByRole('heading', { name: 'CampusCore' })).toBeVisible();
  await expectNoSeriousAccessibilityViolations(page);
});

test('authenticated dashboard has no serious WCAG A/AA violations', async ({ page }) => {
  await signIn(page);
  await expectNoSeriousAccessibilityViolations(page);
});
