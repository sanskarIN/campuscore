import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const envFiles = ['.env', '.env.local', '.env.android', '.env.android.local'];
const values = { ...process.env };

for (const file of envFiles) {
  const path = resolve(process.cwd(), file);
  if (!existsSync(path)) continue;

  for (const rawLine of readFileSync(path, 'utf8').split(/\r?\n/u)) {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) continue;

    const separator = line.indexOf('=');
    if (separator <= 0) continue;

    const key = line.slice(0, separator).trim();
    if (values[key]) continue;

    let value = line.slice(separator + 1).trim();
    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }

    values[key] = value;
  }
}

const apiBaseUrl = values.VITE_API_BASE_URL?.trim();
if (!apiBaseUrl) {
  console.error('Android builds require VITE_API_BASE_URL. Copy .env.android.example to .env.android and point it at the deployed API.');
  process.exit(1);
}

let url;
try {
  url = new URL(apiBaseUrl);
} catch {
  console.error('VITE_API_BASE_URL must be an absolute URL for Android builds.');
  process.exit(1);
}

const localDevelopmentHosts = new Set(['localhost', '127.0.0.1', '10.0.2.2']);
const allowLocalHttp = values.CAMPUSCORE_ANDROID_ALLOW_HTTP === '1' && localDevelopmentHosts.has(url.hostname);

if (url.protocol !== 'https:' && !allowLocalHttp) {
  console.error('Android release builds require an HTTPS API URL. For emulator-only local HTTP, set CAMPUSCORE_ANDROID_ALLOW_HTTP=1 and use localhost, 127.0.0.1, or 10.0.2.2.');
  process.exit(1);
}

if (url.pathname !== '/' || url.search || url.hash) {
  console.error('VITE_API_BASE_URL must contain only scheme, host, and optional port; do not include a path, query, or fragment.');
  process.exit(1);
}

console.log(`Android API target validated: ${url.origin}`);
