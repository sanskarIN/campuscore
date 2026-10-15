import { readdir, stat } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

const kib = 1024;
const mib = 1024 * kib;
const budgets = {
  totalJavaScript: 700 * kib,
  largestJavaScript: 450 * kib,
  totalCss: 200 * kib,
  totalDist: 3 * mib,
};

async function collectFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) files.push(...await collectFiles(fullPath));
    else if (entry.isFile()) files.push(fullPath);
  }

  return files;
}

function formatBytes(bytes) {
  return `${(bytes / kib).toFixed(1)} KiB`;
}

const distDirectory = path.resolve('dist');
let files;
try {
  files = await collectFiles(distDirectory);
} catch (error) {
  console.error('Web bundle budget requires an existing dist directory. Run npm run build first.');
  console.error(error instanceof Error ? error.message : String(error));
  process.exit(1);
}

const sizes = await Promise.all(files.map(async (file) => ({ file, size: (await stat(file)).size })));
const javascript = sizes.filter(({ file }) => file.endsWith('.js'));
const css = sizes.filter(({ file }) => file.endsWith('.css'));

const totals = {
  totalJavaScript: javascript.reduce((sum, item) => sum + item.size, 0),
  largestJavaScript: javascript.reduce((largest, item) => Math.max(largest, item.size), 0),
  totalCss: css.reduce((sum, item) => sum + item.size, 0),
  totalDist: sizes.reduce((sum, item) => sum + item.size, 0),
};

const failures = Object.entries(budgets)
  .filter(([name, budget]) => totals[name] > budget)
  .map(([name, budget]) => `${name}: ${formatBytes(totals[name])} exceeds ${formatBytes(budget)}`);

console.log(`JavaScript total: ${formatBytes(totals.totalJavaScript)}`);
console.log(`Largest JavaScript file: ${formatBytes(totals.largestJavaScript)}`);
console.log(`CSS total: ${formatBytes(totals.totalCss)}`);
console.log(`Distribution total: ${formatBytes(totals.totalDist)}`);

if (failures.length > 0) {
  console.error('Bundle budget failed:');
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}

console.log('Bundle budget passed.');
