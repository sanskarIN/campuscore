import { normalizeCampusCoreUrl } from './url.js';

const DEFAULT_CAMPUSCORE_URL = 'http://localhost:5173';
const form = document.getElementById('settings-form');
const input = document.getElementById('campus-url');
const resetButton = document.getElementById('reset-url');
const status = document.getElementById('save-status');

function showStatus(message, isError = false) {
  status.textContent = message;
  status.classList.toggle('status-error', isError);
}

async function loadSettings() {
  const stored = await chrome.storage.sync.get('campusCoreUrl');
  input.value = typeof stored.campusCoreUrl === 'string' && stored.campusCoreUrl.trim()
    ? stored.campusCoreUrl
    : DEFAULT_CAMPUSCORE_URL;
}

form.addEventListener('submit', async (event) => {
  event.preventDefault();

  try {
    const normalized = normalizeCampusCoreUrl(input.value);
    await chrome.storage.sync.set({ campusCoreUrl: normalized });
    input.value = normalized;
    showStatus('Settings saved.');
  } catch (error) {
    showStatus(error instanceof Error ? error.message : 'Unable to save settings.', true);
    input.focus();
  }
});

resetButton.addEventListener('click', async () => {
  await chrome.storage.sync.set({ campusCoreUrl: DEFAULT_CAMPUSCORE_URL });
  input.value = DEFAULT_CAMPUSCORE_URL;
  showStatus('Reset to local development.');
});

void loadSettings().catch(() => showStatus('Unable to load saved settings.', true));
