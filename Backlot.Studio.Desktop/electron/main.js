const { app, BrowserWindow, ipcMain, Menu } = require('electron');
const path = require('path');
const fs = require('fs');
const { spawn } = require('child_process');
const http = require('http');
const https = require('https');

let mainWindow = null;
let sidecarProcess = null;
let currentPort = null;
let isQuitting = false;

// Profile persistence in userData directory
function getProfilesFilePath() {
  return path.join(app.getPath('userData'), 'connections.json');
}

function loadProfilesData() {
  const filePath = getProfilesFilePath();
  try {
    if (fs.existsSync(filePath)) {
      const data = fs.readFileSync(filePath, 'utf-8');
      return JSON.parse(data);
    }
  } catch (err) {
    console.error('Error loading profiles from disk:', err);
  }

  // Default seed configuration
  return {
    activeProfileId: 'local-dev',
    profiles: [
      {
        id: 'local-dev',
        name: 'Local Development',
        baseUrl: 'https://localhost:7221/'
      }
    ]
  };
}

function saveProfilesData(data) {
  const filePath = getProfilesFilePath();
  try {
    const dir = path.dirname(filePath);
    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
    }
    fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf-8');
  } catch (err) {
    console.error('Error saving profiles to disk:', err);
  }
}

// Locate the .NET executable or fallback to dotnet dll/run
function resolveDotnetCommand(args) {
  const isWindows = process.platform === 'win32';
  const binaryName = isWindows ? 'Backlot.Studio.Desktop.exe' : 'Backlot.Studio.Desktop';

  // 1. Packaged resources path
  const packagedPath = path.join(process.resourcesPath || '', 'dotnet-bin', binaryName);
  if (fs.existsSync(packagedPath)) {
    return { command: packagedPath, args };
  }

  // 2. Pre-published local bundle directory
  const localBinPath = path.join(__dirname, 'dotnet-bin', binaryName);
  if (fs.existsSync(localBinPath)) {
    return { command: localBinPath, args };
  }

  // 3. Local compiled DLL
  const dllPath = path.join(__dirname, '..', 'bin', 'Debug', 'net10.0', 'Backlot.Studio.Desktop.dll');
  if (fs.existsSync(dllPath)) {
    return { command: 'dotnet', args: [dllPath, ...args] };
  }

  // 4. Dotnet project fallback
  const projectPath = path.join(__dirname, '..', 'Backlot.Studio.Desktop.csproj');
  return { command: 'dotnet', args: ['run', '--project', projectPath, '--no-build', '--', ...args] };
}

// Start the .NET Kestrel sidecar process
async function startSidecar(profile) {
  await stopSidecar();

  return new Promise((resolve, reject) => {
    const dotnetArgs = [
      '--urls=http://127.0.0.1:0',
      `--BacklotStudio:BaseUrl=${profile.baseUrl}`,
      `--parent-pid=${process.pid}`
    ];

    const { command, args } = resolveDotnetCommand(dotnetArgs);
    console.log(`[Main] Spawning sidecar: ${command} ${args.join(' ')}`);

    const child = spawn(command, args, {
      stdio: ['pipe', 'pipe', 'pipe'],
      env: { ...process.env }
    });

    sidecarProcess = child;
    let isReady = false;
    let stderrOutput = '';

    const readyTimeout = setTimeout(() => {
      if (!isReady) {
        stopSidecar();
        reject(new Error('Timed out waiting for Backlot Studio sidecar to bind to port.'));
      }
    }, 20000);

    child.stdout.on('data', (chunk) => {
      const text = chunk.toString();
      console.log(`[Sidecar] ${text.trim()}`);
      const match = text.match(/BACKLOT_STUDIO_READY:(\d+)/);
      if (match && !isReady) {
        isReady = true;
        clearTimeout(readyTimeout);
        currentPort = match[1];
        resolve({ port: currentPort });
      }
    });

    child.stderr.on('data', (chunk) => {
      const text = chunk.toString();
      console.error(`[Sidecar Stderr] ${text.trim()}`);
      stderrOutput += text;
    });

    child.on('exit', (code, signal) => {
      console.log(`[Sidecar] Process exited with code: ${code}, signal: ${signal}`);
      sidecarProcess = null;
      currentPort = null;

      if (!isReady) {
        clearTimeout(readyTimeout);
        reject(new Error(`Sidecar failed to start (exit code ${code}): ${stderrOutput}`));
      } else if (!isQuitting && mainWindow && !mainWindow.isDestroyed()) {
        showConnectionsView('The Studio backend process exited unexpectedly.');
      }
    });

    child.on('error', (err) => {
      console.error('[Sidecar] Failed to start process:', err);
      if (!isReady) {
        clearTimeout(readyTimeout);
        reject(err);
      }
    });
  });
}

// Stop the .NET Kestrel sidecar gracefully
async function stopSidecar() {
  if (!sidecarProcess) {
    currentPort = null;
    return;
  }

  const proc = sidecarProcess;
  sidecarProcess = null;
  currentPort = null;

  return new Promise((resolve) => {
    let resolved = false;
    const finish = () => {
      if (!resolved) {
        resolved = true;
        resolve();
      }
    };

    proc.once('exit', finish);

    try {
      if (proc.stdin && !proc.stdin.destroyed) {
        proc.stdin.end();
      }
      proc.kill('SIGTERM');
    } catch {
      // Ignore errors when terminating
    }

    setTimeout(() => {
      try {
        proc.kill('SIGKILL');
      } catch {
        // Ignore
      }
      finish();
    }, 2000);
  });
}

// HTTP ping to verify remote Backlot API connectivity
function testApiReachability(baseUrl) {
  return new Promise((resolve) => {
    try {
      const url = new URL(baseUrl);
      const testPath = url.pathname.endsWith('/') ? `${url.pathname}api/status` : `${url.pathname}/api/status`;
      const isHttps = url.protocol === 'https:';
      const client = isHttps ? https : http;

      const req = client.request(
        {
          hostname: url.hostname,
          port: url.port || (isHttps ? 443 : 80),
          path: testPath,
          method: 'GET',
          timeout: 5000,
          rejectUnauthorized: false // Allow local/self-signed certs for testing
        },
        (res) => {
          resolve({
            success: res.statusCode >= 200 && res.statusCode < 400,
            statusCode: res.statusCode,
            message: `Received HTTP ${res.statusCode}`
          });
        }
      );

      req.on('timeout', () => {
        req.destroy();
        resolve({ success: false, message: 'Connection timed out after 5 seconds' });
      });

      req.on('error', (err) => {
        resolve({ success: false, message: err.message });
      });

      req.end();
    } catch (err) {
      resolve({ success: false, message: err.message });
    }
  });
}

function showConnectionsView(initialError = null) {
  if (!mainWindow || mainWindow.isDestroyed()) return;

  mainWindow.loadFile(path.join(__dirname, 'src', 'connections.html')).then(() => {
    if (initialError) {
      mainWindow.webContents.send('status-change', { type: 'error', message: initialError });
    }
  });
}

function buildMenu() {
  const template = [
    ...(process.platform === 'darwin'
      ? [
          {
            label: app.name,
            submenu: [
              { role: 'about' },
              { type: 'separator' },
              { role: 'services' },
              { type: 'separator' },
              { role: 'hide' },
              { role: 'hideOthers' },
              { role: 'unhide' },
              { type: 'separator' },
              { role: 'quit' }
            ]
          }
        ]
      : []),
    {
      label: 'Studio',
      submenu: [
        {
          label: 'Switch Connection Profile',
          accelerator: 'CmdOrCtrl+Shift+C',
          click: async () => {
            await stopSidecar();
            showConnectionsView();
          }
        },
        {
          label: 'Reload Studio',
          accelerator: 'CmdOrCtrl+R',
          click: () => {
            if (mainWindow && !mainWindow.isDestroyed()) {
              mainWindow.reload();
            }
          }
        },
        { type: 'separator' },
        {
          label: 'Exit',
          accelerator: process.platform === 'darwin' ? 'Cmd+Q' : 'Ctrl+Q',
          click: () => {
            app.quit();
          }
        }
      ]
    },
    {
      label: 'Edit',
      submenu: [
        { role: 'undo' },
        { role: 'redo' },
        { type: 'separator' },
        { role: 'cut' },
        { role: 'copy' },
        { role: 'paste' },
        { role: 'selectAll' }
      ]
    },
    {
      label: 'View',
      submenu: [
        { role: 'resetZoom' },
        { role: 'zoomIn' },
        { role: 'zoomOut' },
        { type: 'separator' },
        { role: 'togglefullscreen' },
        { role: 'toggleDevTools' }
      ]
    }
  ];

  Menu.setApplicationMenu(Menu.buildFromTemplate(template));
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 860,
    minWidth: 960,
    minHeight: 640,
    icon: path.join(__dirname, 'assets', 'icon.png'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  buildMenu();
  showConnectionsView();
}

// IPC Handlers
ipcMain.handle('get-profiles', () => {
  return loadProfilesData();
});

ipcMain.handle('save-profile', (_event, profile) => {
  const data = loadProfilesData();
  const existingIdx = data.profiles.findIndex((p) => p.id === profile.id);
  if (existingIdx >= 0) {
    data.profiles[existingIdx] = profile;
  } else {
    data.profiles.push(profile);
  }
  saveProfilesData(data);
  return data;
});

ipcMain.handle('delete-profile', (_event, profileId) => {
  const data = loadProfilesData();
  data.profiles = data.profiles.filter((p) => p.id !== profileId);
  if (data.activeProfileId === profileId) {
    data.activeProfileId = data.profiles.length > 0 ? data.profiles[0].id : null;
  }
  saveProfilesData(data);
  return data;
});

ipcMain.handle('set-active-profile', (_event, profileId) => {
  const data = loadProfilesData();
  data.activeProfileId = profileId;
  saveProfilesData(data);
  return data;
});

ipcMain.handle('test-connection', async (_event, baseUrl) => {
  return await testApiReachability(baseUrl);
});

ipcMain.handle('connect-profile', async (_event, profileId) => {
  const data = loadProfilesData();
  const profile = data.profiles.find((p) => p.id === profileId);
  if (!profile) {
    throw new Error(`Profile with ID '${profileId}' not found.`);
  }

  data.activeProfileId = profileId;
  saveProfilesData(data);

  try {
    const { port } = await startSidecar(profile);
    const studioUrl = `http://127.0.0.1:${port}/studio`;
    if (mainWindow && !mainWindow.isDestroyed()) {
      await mainWindow.loadURL(studioUrl);
    }
    return { success: true, port, url: studioUrl };
  } catch (err) {
    console.error('Failed to connect to profile:', err);
    return { success: false, error: err.message };
  }
});

ipcMain.handle('disconnect-profile', async () => {
  await stopSidecar();
  showConnectionsView();
  return { success: true };
});

// App Lifecycle
app.whenReady().then(() => {
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('before-quit', async (event) => {
  if (sidecarProcess && !isQuitting) {
    event.preventDefault();
    isQuitting = true;
    await stopSidecar();
    app.quit();
  }
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});
