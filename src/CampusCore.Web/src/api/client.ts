import { clearSession, getAccessToken } from '../auth/session';
import { environment } from '../env';

export class ApiError extends Error {
  readonly status: number;
  readonly details: unknown;

  constructor(message: string, status: number, details: unknown = null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.details = details;
  }
}

function resolveUrl(path: string): string {
  const normalized = path.startsWith('/') ? path : `/${path}`;
  return `${environment.apiBaseUrl}${normalized}`;
}

async function parseFailure(response: Response): Promise<unknown> {
  const contentType = response.headers.get('content-type') ?? '';
  if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
    try {
      return await response.json();
    } catch {
      return null;
    }
  }

  try {
    return await response.text();
  } catch {
    return null;
  }
}

function messageFromFailure(details: unknown, fallback: string): string {
  if (typeof details === 'string' && details.trim()) return details;
  if (details && typeof details === 'object') {
    const candidate = details as Record<string, unknown>;
    for (const key of ['detail', 'title', 'message']) {
      if (typeof candidate[key] === 'string' && candidate[key].trim()) return candidate[key];
    }
  }
  return fallback;
}

function handleUnauthorized(status: number, hadToken: boolean): void {
  if (status !== 401 || !hadToken) return;
  clearSession();
  if (typeof window !== 'undefined') window.dispatchEvent(new Event('campuscore:unauthorized'));
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  const token = getAccessToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }
  headers.set('Accept', 'application/json');

  const response = await fetch(resolveUrl(path), {
    ...init,
    headers,
    credentials: 'omit',
  });

  handleUnauthorized(response.status, Boolean(token));

  if (!response.ok) {
    const details = await parseFailure(response);
    throw new ApiError(messageFromFailure(details, `Request failed with status ${response.status}.`), response.status, details);
  }

  if (response.status === 204) return undefined as T;
  const contentType = response.headers.get('content-type') ?? '';
  if (contentType.includes('application/json')) return (await response.json()) as T;
  return (await response.text()) as T;
}

export function apiJson<T>(path: string, method: 'POST' | 'PUT' | 'PATCH', body: unknown): Promise<T> {
  return apiRequest<T>(path, { method, body: JSON.stringify(body) });
}

export async function apiDownload(path: string): Promise<Blob> {
  const token = getAccessToken();
  const headers = new Headers();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  const response = await fetch(resolveUrl(path), { headers, credentials: 'omit' });
  handleUnauthorized(response.status, Boolean(token));
  if (!response.ok) {
    const details = await parseFailure(response);
    throw new ApiError(messageFromFailure(details, 'Download failed.'), response.status, details);
  }
  return response.blob();
}
