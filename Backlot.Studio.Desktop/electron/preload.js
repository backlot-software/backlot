const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('backlotDesktop', {
  getProfiles: () => ipcRenderer.invoke('get-profiles'),
  saveProfile: (profile) => ipcRenderer.invoke('save-profile', profile),
  deleteProfile: (profileId) => ipcRenderer.invoke('delete-profile', profileId),
  setActiveProfile: (profileId) => ipcRenderer.invoke('set-active-profile', profileId),
  testConnection: (baseUrl) => ipcRenderer.invoke('test-connection', baseUrl),
  connectProfile: (profileId) => ipcRenderer.invoke('connect-profile', profileId),
  disconnectProfile: () => ipcRenderer.invoke('disconnect-profile'),
  onStatusChange: (callback) => {
    const handler = (_event, status) => callback(status);
    ipcRenderer.on('status-change', handler);
    return () => ipcRenderer.removeListener('status-change', handler);
  }
});
