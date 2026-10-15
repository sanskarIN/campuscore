const DEFAULT_CAMPUSCORE_URL = 'http://localhost:5173';

chrome.runtime.onInstalled.addListener(async ({ reason }) => {
  if (reason !== 'install') return;

  const stored = await chrome.storage.sync.get('campusCoreUrl');
  if (typeof stored.campusCoreUrl === 'string' && stored.campusCoreUrl.trim()) return;

  await chrome.storage.sync.set({ campusCoreUrl: DEFAULT_CAMPUSCORE_URL });
});
