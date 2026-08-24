import { isNativeRuntime } from './platform/runtime';

const stripTrailingSlash = (value: string): string => value.replace(/\/+$/, '');

const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim();
if (isNativeRuntime() && !configuredApiBaseUrl) {
  throw new Error('VITE_API_BASE_URL is required when CampusCore runs inside a native shell.');
}

export const environment = Object.freeze({
  apiBaseUrl: stripTrailingSlash(configuredApiBaseUrl || 'http://localhost:5080'),
  version: import.meta.env.VITE_APP_VERSION?.trim() || '0.1.0',
});
