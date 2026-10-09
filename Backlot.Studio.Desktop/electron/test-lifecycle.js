const assert = require('assert');
const http = require('http');
const { spawn } = require('child_process');
const path = require('path');

async function testReachability() {
  console.log('[Test 1] Testing API reachability logic...');
  const server = http.createServer((req, res) => {
    if (req.url === '/api/status') {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ status: 'Healthy' }));
    } else {
      res.writeHead(404);
      res.end();
    }
  });

  await new Promise(r => server.listen(0, '127.0.0.1', r));
  const port = server.address().port;
  const baseUrl = `http://127.0.0.1:${port}/`;

  // Test online
  const https = require('https');
  function ping(urlStr) {
    return new Promise(resolve => {
      const url = new URL(urlStr);
      const testPath = url.pathname.endsWith('/') ? `${url.pathname}api/status` : `${url.pathname}/api/status`;
      const client = url.protocol === 'https:' ? https : http;
      const req = client.request({
        hostname: url.hostname,
        port: url.port,
        path: testPath,
        method: 'GET',
        timeout: 2000
      }, res => {
        resolve({ success: res.statusCode >= 200 && res.statusCode < 400, statusCode: res.statusCode });
      });
      req.on('error', err => resolve({ success: false, error: err.message }));
      req.end();
    });
  }

  const onlineRes = await ping(baseUrl);
  assert.strictEqual(onlineRes.success, true);
  assert.strictEqual(onlineRes.statusCode, 200);
  console.log(' - Online endpoint reached successfully.');

  // Test offline
  const offlineRes = await ping('http://127.0.0.1:49999/');
  assert.strictEqual(offlineRes.success, false);
  console.log(' - Offline endpoint failed gracefully.');

  server.close();
}

async function testHostSpawning() {
  console.log('[Test 2] Testing Backlot.Studio.Desktop sidecar spawning and port detection...');
  const projectPath = path.join(__dirname, '..', 'Backlot.Studio.Desktop.csproj');
  const child = spawn('dotnet', [
    'run',
    '--project', projectPath,
    '--',
    '--urls=http://127.0.0.1:0',
    '--BacklotStudio:BaseUrl=http://127.0.0.1:5000',
    `--parent-pid=${process.pid}`
  ], {
    stdio: ['pipe', 'pipe', 'pipe']
  });

  let detectedPort = null;
  const readyPromise = new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error('Timeout waiting for readiness token')), 15000);
    child.stdout.on('data', chunk => {
      const text = chunk.toString();
      const match = text.match(/BACKLOT_STUDIO_READY:(\d+)/);
      if (match) {
        clearTimeout(timer);
        detectedPort = parseInt(match[1], 10);
        resolve(detectedPort);
      }
    });
  });

  const port = await readyPromise;
  assert.ok(port > 0, `Expected valid port, got ${port}`);
  console.log(` - Detected ephemeral port: ${port}`);

  // Ping sidecar HTTP endpoint
  const pingSidecar = await new Promise(resolve => {
    http.get(`http://127.0.0.1:${port}/`, res => {
      // Root redirects to /studio (302)
      resolve(res.statusCode);
    }).on('error', err => resolve(null));
  });

  assert.strictEqual(pingSidecar, 302);
  console.log(` - Studio root returned expected 302 redirect to /studio`);

  // Terminate sidecar
  console.log(' - Terminating sidecar...');
  child.stdin.end();
  child.kill('SIGTERM');

  await new Promise(r => child.on('exit', r));
  console.log(' - Sidecar terminated cleanly.');
}

(async () => {
  try {
    await testReachability();
    await testHostSpawning();
    console.log('All tests passed successfully!');
    process.exit(0);
  } catch (err) {
    console.error('Test failed:', err);
    process.exit(1);
  }
})();
