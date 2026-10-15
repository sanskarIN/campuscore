const LOCAL_DEVELOPMENT_HOSTS = new Set(['localhost', '127.0.0.1']);

export function normalizeCampusCoreUrl(rawValue) {
  const value = String(rawValue ?? '').trim();
  if (!value) throw new Error('Enter the CampusCore URL.');

  let url;
  try {
    url = new URL(value);
  } catch {
    throw new Error('Enter a valid absolute URL, for example https://campus.example.edu.');
  }

  const isLocalHttp = url.protocol === 'http:' && LOCAL_DEVELOPMENT_HOSTS.has(url.hostname);
  if (url.protocol !== 'https:' && !isLocalHttp) {
    throw new Error('Use HTTPS. HTTP is accepted only for localhost development.');
  }

  if (url.username || url.password || url.search || url.hash) {
    throw new Error('Do not include credentials, query parameters, or a fragment.');
  }

  url.pathname = url.pathname.replace(/\/+$/u, '') || '/';
  return url.toString().replace(/\/$/u, '');
}

export function buildCampusCoreRoute(baseUrl, route) {
  const normalizedBase = `${normalizeCampusCoreUrl(baseUrl)}/`;
  const normalizedRoute = String(route ?? '').replace(/^\/+/, '');
  return new URL(normalizedRoute, normalizedBase).toString();
}
