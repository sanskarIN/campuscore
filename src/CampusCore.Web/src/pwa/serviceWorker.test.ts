import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const workerSource = readFileSync(new URL('../../public/sw.js', import.meta.url), 'utf8');

describe('service worker privacy boundary', () => {
  it('explicitly excludes API requests from caching', () => {
    expect(workerSource).toContain("url.pathname.startsWith('/api/')");
  });

  it('limits its precache to public application shell assets', () => {
    expect(workerSource).toContain("const SHELL = ['/', '/index.html', '/logo.svg', '/manifest.webmanifest']");
    expect(workerSource).not.toMatch(/\/api\/(?:students|auth|admin)/);
  });
});
