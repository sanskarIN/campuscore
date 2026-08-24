import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import process from 'node:process';

const root = process.cwd();
const read = (path) => readFileSync(resolve(root, path), 'utf8');
const version = read('VERSION').trim();

assert.match(version, /^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/u, 'VERSION must contain a semantic version.');

const directoryProps = read('Directory.Build.props');
const webPackage = JSON.parse(read('src/CampusCore.Web/package.json'));
const extensionPackage = JSON.parse(read('src/CampusCore.Extension/package.json'));
const extensionManifest = JSON.parse(read('src/CampusCore.Extension/manifest.json'));
const webEnvironment = read('src/CampusCore.Web/src/env.ts');
const compose = read('docker-compose.yml');
const envExample = read('.env.example');

const dotnetVersion = directoryProps.match(/<CampusCoreVersion>([^<]+)<\/CampusCoreVersion>/u)?.[1];
const webFallback = webEnvironment.match(/version:\s*import\.meta\.env\.VITE_APP_VERSION\?\.trim\(\)\s*\|\|\s*'([^']+)'/u)?.[1];
const composeVersion = compose.match(/VITE_APP_VERSION:\s*\$\{CAMPUSCORE_VERSION:-([^}]+)\}/u)?.[1];
const exampleVersion = envExample.match(/^CAMPUSCORE_VERSION=(.+)$/mu)?.[1]?.trim();

const components = new Map([
  ['Directory.Build.props CampusCoreVersion', dotnetVersion],
  ['Web package', webPackage.version],
  ['Web runtime fallback', webFallback],
  ['Extension package', extensionPackage.version],
  ['Extension manifest', extensionManifest.version],
  ['Compose default', composeVersion],
  ['.env.example', exampleVersion],
]);

for (const [name, actual] of components) {
  assert.ok(actual, `${name} version could not be resolved.`);
  assert.equal(actual, version, `${name} version ${actual} does not match VERSION ${version}.`);
}

const tagIndex = process.argv.indexOf('--tag');
if (tagIndex >= 0) {
  const tag = process.argv[tagIndex + 1];
  assert.ok(tag, '--tag requires a value.');
  assert.equal(tag, `v${version}`, `Release tag ${tag} must equal v${version}.`);
}

console.log(`CampusCore version ${version} is aligned across all release surfaces.`);
