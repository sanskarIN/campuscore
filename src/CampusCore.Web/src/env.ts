const stripTrailingSlash = (value: string): string => value.replace(/\/+$/, '');

export const environment = Object.freeze({
  apiBaseUrl: stripTrailingSlash(import.meta.env.VITE_API_BASE_URL?.trim() || 'http://localhost:5080'),
  version: import.meta.env.VITE_APP_VERSION?.trim() || '0.1.0',
});
