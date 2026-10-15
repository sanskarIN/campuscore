import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { buildCampusCoreRoute, normalizeCampusCoreUrl } from './url.js';

const root = process.cwd();
const manifest = JSON.parse(readFileSync(resolve(root, 'manifest.json'), 'utf8'));
const packageMetadata = JSON.parse(readFileSync(resolve(root, 'package.json'), 'utf8'));

assert.equal(manifest.manifest_version, 3, 'Extension must use Manifest V3.');
assert.equal(manifest.version, packageMetadata.version, 'Extension manifest and package versions must match.');
assert.match(manifest.version, /^\d+(?:\.\d+){0,3}$/u, 'Extension version must use Chrome numeric version syntax.');
assert.deepEqual(manifest.permissions ?? [], ['storage'], 'Extension permissions must remain storage-only.');
assert.equal('host_permissions' in manifest, false, 'Host permissions are intentionally forbidden.');
assert.equal('content_scripts' in manifest, false, 'Content scripts are intentionally forbidden.');

const referencedFiles = [
  manifest.action?.default_popup,
  manifest.background?.service_worker,
  manifest.options_page,
].filter(Boolean);

for (const file of referencedFiles) {
  assert.ok(existsSync(resolve(root, file)), `Manifest references missing file: ${file}`);
}

assert.equal(normalizeCampusCoreUrl('https://campus.example.edu/'), 'https://campus.example.edu');
assert.equal(normalizeCampusCoreUrl('http://localhost:5173/'), 'http://localhost:5173');
assert.equal(buildCampusCoreRoute('https://campus.example.edu', '/students'), 'https://campus.example.edu/students');
assert.throws(() => normalizeCampusCoreUrl('http://campus.example.edu'), /Use HTTPS/u);
assert.throws(() => normalizeCampusCoreUrl('https://user:pass@campus.example.edu'), /credentials/u);
assert.throws(() => normalizeCampusCoreUrl('https://campus.example.edu?token=secret'), /query/u);

console.log(`CampusCore Companion ${manifest.version} manifest and URL policy validated.`);
