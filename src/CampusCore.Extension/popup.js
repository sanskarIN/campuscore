import { buildCampusCoreRoute, normalizeCampusCoreUrl } from './url.js';

const status = document.getElementById('connection-status');
const settingsButton = document.getElementById('open-settings');
const routeButtons = [...document.querySelectorAll('[data-route]')];

let campusCoreUrl = '';

function setUnavailable(message) {
  campusCoreUrl = '';
  status.textContent = message;
  status.classList.add('status-error');
  for (const button of routeButtons) button.disabled = true;
}

async function loadConfiguration() {
  try {
    const stored = await chrome.storage.sync.get('campusCoreUrl');
    campusCoreUrl = normalizeCampusCoreUrl(stored.campusCoreUrl);
    status.textContent = campusCoreUrl;
    status.classList.remove('status-error');
    for (const button of routeButtons) button.disabled = false;
  } catch (error) {
    setUnavailable(error instanceof Error ? error.message : 'Configure the CampusCore URL first.');
  }
}

for (const button of routeButtons) {
  button.addEventListener('click', async () => {
    if (!campusCoreUrl) return;
    const route = button.dataset.route ?? '';
    await chrome.tabs.create({ url: buildCampusCoreRoute(campusCoreUrl, route) });
    window.close();
  });
}

settingsButton.addEventListener('click', async () => {
  await chrome.runtime.openOptionsPage();
  window.close();
});

void loadConfiguration();
