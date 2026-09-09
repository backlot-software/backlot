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

const DOTNET_BINARY_NAME =
  process.platform === 'win32' ? 'Backlot.Studio.Desktop.exe' : 'Backlot.Studio.Desktop';

// Candidate locations for a pre-published self-contained bundle: the flat
// `dotnet-bin` written by `build:dotnet`, plus the per-RID subdirectories
// written by `build:dotnet:linux|win|osx`.
function publishedBundleCandidates(root) {
  const rids = ['linux-x64', 'win-x64', 'osx-arm64'];
  return [
    path.join(root, DOTNET_BINARY_NAME),
    ...rids.map((rid) => path.join(root, rid, DOTNET_BINARY_NAME))
  ];
}

// Newest compiled DLL across every build configuration (Debug, Development,
// Release, ...) and target framework, so a Rider build into bin/Development is
// picked up just like a bin/Debug one. Honours BACKLOT_DESKTOP_CONFIGURATION
// when set, which pins the lookup to a single configuration.
function findLocalBuildDll() {
  const binRoot = path.join(__dirname, '..', 'bin');
  if (!fs.existsSync(binRoot)) return null;

  const pinned = process.env.BACKLOT_DESKTOP_CONFIGURATION;
  const configurations = pinned
    ? [pinned]
    : fs.readdirSync(binRoot).filter((entry) => {
        try {
          return fs.statSync(path.join(binRoot, entry)).isDirectory();
        } catch {
          return false;
        }
      });

  const candidates = [];
  for (const configuration of configurations) {
    const configDir = path.join(binRoot, configuration);
    let frameworks;
    try {
      frameworks = fs.readdirSync(configDir);
    } catch {
      continue;
    }

    for (const framework of frameworks) {
      const dll = path.join(configDir, framework, 'Backlot.Studio.Desktop.dll');
      try {
        candidates.push({ dll, mtime: fs.statSync(dll).mtimeMs });
      } catch {
        // Not a build output directory
      }
    }
  }

  if (candidates.length === 0) return null;
  candidates.sort((a, b) => b.mtime - a.mtime);
  return candidates[0].dll;
}

// Locate the .NET executable or fallback to dotnet dll/run.
//
// When packaged, the bundled self-contained binary is the only option. In
// development the freshly compiled output wins over any pre-published bundle in
// `dotnet-bin`, which is gitignored and otherwise silently serves stale code.
function resolveDotnetCommand(args) {
  if (app.isPackaged) {
    const packagedRoot = path.join(process.resourcesPath || '', 'dotnet-bin');
    const packagedPath = publishedBundleCandidates(packagedRoot).find((candidate) =>
      fs.existsSync(candidate)
    );
    if (packagedPath) {
      return { command: packagedPath, args };
    }
  }

  // 1. Local compiled output — the current code.
  const dllPath = findLocalBuildDll();
  if (dllPath) {
    return { command: 'dotnet', args: [dllPath, ...args] };
  }

  // 2. Pre-published local bundle directory.
  const localBinPath = publishedBundleCandidates(path.join(__dirname, 'dotnet-bin')).find(
    (candidate) => fs.existsSync(candidate)
  );
  if (localBinPath) {
    return { command: localBinPath, args };
  }

  // 3. Dotnet project fallback — builds on demand.
  const projectPath = path.join(__dirname, '..', 'Backlot.Studio.Desktop.csproj');
  return { command: 'dotnet', args: ['run', '--project', projectPath, '--', ...args] };
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
