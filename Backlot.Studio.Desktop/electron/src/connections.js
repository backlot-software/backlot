document.addEventListener('DOMContentLoaded', async () => {
  let state = {
    activeProfileId: null,
    selectedProfileId: null,
    profiles: []
  };

  const profilesListEl = document.getElementById('profiles-list');
  const formHeadingEl = document.getElementById('form-heading');
  const profileIdEl = document.getElementById('profile-id');
  const profileNameEl = document.getElementById('profile-name');
  const profileUrlEl = document.getElementById('profile-url');
  const deleteBtn = document.getElementById('delete-btn');
  const saveBtn = document.getElementById('save-btn');
  const connectBtn = document.getElementById('connect-btn');
  const testBtn = document.getElementById('test-connection-btn');
  const testResultEl = document.getElementById('test-result');
  const newProfileBtn = document.getElementById('new-profile-btn');
  const alertBanner = document.getElementById('alert-banner');
  const alertMessage = document.getElementById('alert-message');
  const alertCloseBtn = document.getElementById('alert-close-btn');
  const loadingOverlay = document.getElementById('loading-overlay');
  const loadingMessage = document.getElementById('loading-message');

  function showAlert(msg) {
    alertMessage.textContent = msg;
    alertBanner.classList.remove('hidden');
  }

  function hideAlert() {
    alertBanner.classList.add('hidden');
    alertMessage.textContent = '';
  }

  alertCloseBtn.addEventListener('click', hideAlert);

  function showLoading(msg) {
    loadingMessage.textContent = msg || 'Connecting to Backlot Studio sidecar...';
    loadingOverlay.classList.remove('hidden');
  }

  function hideLoading() {
    loadingOverlay.classList.add('hidden');
  }

  function setTestResult(type, message) {
    testResultEl.classList.remove('hidden', 'bg-green-50', 'text-green-800', 'border-green-200', 'bg-red-50', 'text-red-800', 'border-red-200');
    if (type === 'success') {
      testResultEl.classList.add('bg-green-50', 'text-green-800', 'border-green-200');
      testResultEl.innerHTML = `<span class="font-semibold">Reachable:</span> ${escapeHtml(message)}`;
    } else {
      testResultEl.classList.add('bg-red-50', 'text-red-800', 'border-red-200');
      testResultEl.innerHTML = `<span class="font-semibold">Unreachable:</span> ${escapeHtml(message)}`;
    }
  }

  function clearTestResult() {
    testResultEl.classList.add('hidden');
    testResultEl.innerHTML = '';
  }

  function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text || '';
    return div.innerHTML;
  }

  function renderProfiles() {
    profilesListEl.innerHTML = '';

    if (state.profiles.length === 0) {
      profilesListEl.innerHTML = `
        <div class="text-center py-8 px-4 bg-white rounded-lg border border-dashed border-slate-300 text-slate-500 text-sm">
          No connection profiles found. Click <strong>+ Add Profile</strong> to create one.
        </div>
      `;
      return;
    }

    state.profiles.forEach((profile) => {
      const isSelected = profile.id === state.selectedProfileId;
      const isActive = profile.id === state.activeProfileId;

      const card = document.createElement('div');
      card.className = `profile-card p-3.5 rounded-lg border cursor-pointer bg-white shadow-sm flex items-center justify-between ${
        isSelected ? 'active border-primary bg-indigo-50/30 ring-1 ring-primary' : 'border-slate-200 hover:border-slate-300'
      }`;

      card.innerHTML = `
        <div class="flex-1 min-w-0 pr-2">
          <div class="flex items-center space-x-2">
            <span class="font-semibold text-sm text-slate-900 truncate">${escapeHtml(profile.name)}</span>
            ${isActive ? '<span class="inline-flex items-center px-1.5 py-0.2 rounded text-[10px] font-medium bg-green-100 text-green-800">Active</span>' : ''}
          </div>
          <div class="text-xs text-slate-500 truncate mt-0.5 font-mono">${escapeHtml(profile.baseUrl)}</div>
        </div>
        <button type="button" data-connect-id="${profile.id}"
                class="quick-connect-btn ml-2 px-2.5 py-1 text-xs font-medium text-primary hover:text-white hover:bg-primary border border-primary/40 rounded transition-colors flex-shrink-0">
          Connect
        </button>
      `;

      card.addEventListener('click', (e) => {
        if (e.target.closest('.quick-connect-btn')) return;
        selectProfile(profile.id);
      });

      card.querySelector('.quick-connect-btn').addEventListener('click', async (e) => {
        e.stopPropagation();
        await triggerConnect(profile.id);
      });

      profilesListEl.appendChild(card);
    });
  }

  function selectProfile(profileId) {
    state.selectedProfileId = profileId;
    const profile = state.profiles.find((p) => p.id === profileId);
    clearTestResult();
    hideAlert();

    if (profile) {
      formHeadingEl.textContent = 'Edit Profile';
      profileIdEl.value = profile.id;
      profileNameEl.value = profile.name;
      profileUrlEl.value = profile.baseUrl;
      deleteBtn.classList.remove('hidden');
    } else {
      newProfile();
    }

    renderProfiles();
  }

  function newProfile() {
    state.selectedProfileId = null;
    clearTestResult();
    hideAlert();

    formHeadingEl.textContent = 'Add New Profile';
    profileIdEl.value = 'profile-' + Date.now();
    profileNameEl.value = '';
    profileUrlEl.value = '';
    deleteBtn.classList.add('hidden');
    profileNameEl.focus();

    renderProfiles();
  }

  async function triggerConnect(profileId) {
    hideAlert();
    showLoading('Starting Backlot Studio Kestrel host...');

    try {
      const res = await window.backlotDesktop.connectProfile(profileId);
      if (!res.success) {
        hideLoading();
        showAlert(res.error || 'Failed to start Studio sidecar.');
      }
    } catch (err) {
      hideLoading();
      showAlert(err.message || 'Error occurred while connecting.');
    }
  }

  // Event Listeners
  newProfileBtn.addEventListener('click', newProfile);

  saveBtn.addEventListener('click', async () => {
    const name = profileNameEl.value.trim();
    const url = profileUrlEl.value.trim();
    const id = profileIdEl.value.trim() || ('profile-' + Date.now());

    if (!name) {
      showAlert('Please enter a profile name.');
      profileNameEl.focus();
      return;
    }

    if (!url) {
      showAlert('Please enter the target Base URL.');
      profileUrlEl.focus();
      return;
    }

    try {
      new URL(url);
    } catch {
      showAlert('Invalid URL format. Please include http:// or https://');
      profileUrlEl.focus();
      return;
    }

    hideAlert();
    const profile = { id, name, baseUrl: url };
    const data = await window.backlotDesktop.saveProfile(profile);
    state.profiles = data.profiles;
    state.activeProfileId = data.activeProfileId;
    state.selectedProfileId = id;
    renderProfiles();
    setTestResult('success', 'Profile saved successfully.');
  });

  deleteBtn.addEventListener('click', async () => {
    const id = profileIdEl.value;
    if (!id) return;

    if (confirm('Are you sure you want to delete this profile?')) {
      const data = await window.backlotDesktop.deleteProfile(id);
      state.profiles = data.profiles;
      state.activeProfileId = data.activeProfileId;
      if (state.profiles.length > 0) {
        selectProfile(state.profiles[0].id);
      } else {
        newProfile();
      }
    }
  });

  testBtn.addEventListener('click', async () => {
    const url = profileUrlEl.value.trim();
    if (!url) {
      showAlert('Please enter a Base URL to test.');
      profileUrlEl.focus();
      return;
    }

    try {
      new URL(url);
    } catch {
      showAlert('Invalid URL format. Must start with http:// or https://');
      profileUrlEl.focus();
      return;
    }

    hideAlert();
    testBtn.disabled = true;
    testBtn.innerHTML = '<span class="spinner mr-1"></span> Testing...';

    try {
      const res = await window.backlotDesktop.testConnection(url);
      if (res.success) {
        setTestResult('success', `${res.message} - Remote endpoint is reachable!`);
      } else {
        setTestResult('error', `${res.message || 'Remote endpoint could not be reached'}`);
      }
    } catch (err) {
      setTestResult('error', err.message);
    } finally {
      testBtn.disabled = false;
      testBtn.textContent = 'Test Ping';
    }
  });

  connectBtn.addEventListener('click', async () => {
    const name = profileNameEl.value.trim();
    const url = profileUrlEl.value.trim();
    let id = profileIdEl.value.trim();

    if (!name || !url) {
      showAlert('Please enter both a profile name and Base URL.');
      return;
    }

    if (!id || !state.profiles.some((p) => p.id === id)) {
      id = 'profile-' + Date.now();
      const profile = { id, name, baseUrl: url };
      const data = await window.backlotDesktop.saveProfile(profile);
      state.profiles = data.profiles;
      state.activeProfileId = id;
    }

    await triggerConnect(id);
  });

  // Listen for main process notifications
  if (window.backlotDesktop.onStatusChange) {
    window.backlotDesktop.onStatusChange((status) => {
      if (status && status.type === 'error') {
        hideLoading();
        showAlert(status.message);
      }
    });
  }

  // Initial Load
  try {
    const data = await window.backlotDesktop.getProfiles();
    state.profiles = data.profiles || [];
    state.activeProfileId = data.activeProfileId || (state.profiles[0] ? state.profiles[0].id : null);
    state.selectedProfileId = state.activeProfileId;

    if (state.selectedProfileId) {
      selectProfile(state.selectedProfileId);
    } else {
      newProfile();
    }
  } catch (err) {
    showAlert('Failed to load connection profiles: ' + err.message);
  }
});
