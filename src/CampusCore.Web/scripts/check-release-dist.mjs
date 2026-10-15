import assert from 'node:assert/strict';
import { readdir, readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

const distDirectory = path.resolve('dist');
const version = (await readFile(path.resolve('../../VERSION'), 'utf8')).trim();
const forbiddenMarkers = [
  'http://localhost:5080',
  'campuscore-development-only',
  'campuscore-local-only',
  'replace-with-local-password',
];
const inspectExtensions = new Set(['.html', '.js', '.css', '.json', '.webmanifest', '.svg']);

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

assert.ok((await stat(distDirectory)).isDirectory(), 'dist must exist before release verification.');
const files = await collectFiles(distDirectory);
assert.ok(files.length > 0, 'dist must contain release assets.');
assert.ok(files.some((file) => path.basename(file) === 'index.html'), 'dist/index.html is required.');

let versionFound = false;
for (const file of files) {
  if (file.endsWith('.map') || !inspectExtensions.has(path.extname(file))) continue;
  const content = await readFile(file, 'utf8');
  if (content.includes(version)) versionFound = true;

  for (const marker of forbiddenMarkers) {
    assert.equal(content.includes(marker), false, `Release asset ${path.relative(distDirectory, file)} contains forbidden marker: ${marker}`);
  }
}

assert.ok(versionFound, `Release assets must contain the prepared application version ${version}.`);
console.log(`CampusCore Web ${version} release assets contain no local/development deployment markers.`);
