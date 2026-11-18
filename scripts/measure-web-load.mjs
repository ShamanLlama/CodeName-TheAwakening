#!/usr/bin/env node
// Measures how long a web build takes to become playable on an emulated phone.
//
//   node scripts/measure-web-load.mjs <build-dir> <godot|unity> [--mbps=12,4] [--runs=2] [--serve=itch|precompressed]
//
// The build is served locally the way itch.io serves it: plain files gzipped on the fly
// (`itch`), or Unity's pre-compressed files with Content-Encoding set (`precompressed`).
// Each run uses a fresh browser context, so it is a first visit with an empty cache.
// Ready means: Godot's loading overlay (#status) is removed; Unity logs AWAKENING_READY.
// Prints one JSON object per run and a summary. Emulation, not a physical phone.
import { createServer } from 'node:http';
import { createReadStream, statSync, existsSync } from 'node:fs';
import { extname, join, normalize, resolve } from 'node:path';
import { createGzip } from 'node:zlib';
import { chromium } from 'playwright';

const [dir, kind, ...rest] = process.argv.slice(2);
if (!dir || !['godot', 'unity'].includes(kind)) {
  console.error('usage: measure-web-load.mjs <build-dir> <godot|unity> [--mbps=12,4] [--runs=2] [--serve=itch|precompressed]');
  process.exit(2);
}
const option = (name, fallback) => rest.find((a) => a.startsWith(`--${name}=`))?.split('=')[1] ?? fallback;
const speeds = option('mbps', '12,4').split(',').map(Number);
const runs = Number(option('runs', '2'));
const serve = option('serve', 'itch');
const root = resolve(dir);

const types = {
  '.html': 'text/html', '.js': 'application/javascript', '.wasm': 'application/wasm', '.pck': 'application/octet-stream',
  '.data': 'application/octet-stream', '.png': 'image/png', '.json': 'application/json', '.css': 'text/css',
};

let served = 0;
const counted = (stream) => stream.on('data', (chunk) => { served += chunk.length; });
const server = createServer((req, res) => {
  const path = normalize(decodeURIComponent(new URL(req.url, 'http://x').pathname)).replace(/^\/+/, '') || 'index.html';
  const file = join(root, path);
  if (!file.startsWith(root) || !existsSync(file) || !statSync(file).isFile()) {
    res.writeHead(404).end();
    return;
  }
  let name = path;
  const headers = { 'Cache-Control': 'no-store', 'Cross-Origin-Opener-Policy': 'same-origin' };
  const precompressed = name.endsWith('.gz') || name.endsWith('.br') || name.endsWith('.unityweb');
  if (name.endsWith('.gz') || name.endsWith('.br')) name = name.slice(0, -3);
  headers['Content-Type'] = types[extname(name)] ?? 'application/octet-stream';
  if (precompressed && serve === 'precompressed') {
    headers['Content-Encoding'] = path.endsWith('.br') ? 'br' : 'gzip';
    res.writeHead(200, headers);
    counted(createReadStream(file)).pipe(res);
  } else if (!precompressed && !path.endsWith('.png')) {
    headers['Content-Encoding'] = 'gzip';
    res.writeHead(200, headers);
    counted(createReadStream(file).pipe(createGzip({ level: 6 }))).pipe(res);
  } else {
    res.writeHead(200, headers);
    counted(createReadStream(file)).pipe(res);
  }
});
await new Promise((ok) => server.listen(0, '127.0.0.1', ok));
const url = `http://127.0.0.1:${server.address().port}/index.html`;

// PLAYWRIGHT_CHROMIUM_EXECUTABLE picks an installed Chromium when Playwright's own is missing.
const browser = await chromium.launch({
  executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE || undefined,
  args: ['--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'],
});
const results = [];
for (const mbps of speeds) {
  for (let run = 0; run < runs; run++) {
    const context = await browser.newContext({ viewport: { width: 844, height: 390 }, deviceScaleFactor: 3, isMobile: true, hasTouch: true });
    const page = await context.newPage();
    const cdp = await context.newCDPSession(page);
    await cdp.send('Network.enable');
    await cdp.send('Network.setCacheDisabled', { cacheDisabled: true });
    await cdp.send('Network.emulateNetworkConditions', {
      offline: false, latency: 40, downloadThroughput: (mbps * 1e6) / 8, uploadThroughput: (mbps * 1e6) / 8,
    });
    await cdp.send('Emulation.setCPUThrottlingRate', { rate: 4 });
    served = 0;
    const errors = [];
    page.on('pageerror', (e) => errors.push(String(e)));
    let unityReady;
    const unitySignal = new Promise((ok) => { unityReady = ok; });
    page.on('console', (m) => {
      if (m.text().includes('AWAKENING_READY')) unityReady();
      if (m.type() === 'error') errors.push(m.text());
    });
    const started = Date.now();
    await page.goto(url, { waitUntil: 'commit' });
    const timeout = 240000;
    if (kind === 'godot') {
      await page.waitForFunction(() => document.readyState === 'complete' && !document.getElementById('status'), null, { timeout, polling: 100 });
    } else {
      await Promise.race([unitySignal, new Promise((_, no) => setTimeout(() => no(new Error('timeout')), timeout))]);
    }
    const seconds = (Date.now() - started) / 1000;
    await page.waitForTimeout(500);
    const result = { kind, serve, mbps, run: run + 1, seconds: Number(seconds.toFixed(2)), megabytes: Number((served / 1e6).toFixed(2)), errors: errors.slice(0, 5) };
    console.log(JSON.stringify(result));
    results.push(result);
    await context.close();
  }
}
await browser.close();
server.close();
const summary = {};
for (const r of results) {
  const key = `${r.mbps} Mbps`;
  summary[key] ??= { seconds: [], megabytes: r.megabytes };
  summary[key].seconds.push(r.seconds);
}
console.log(JSON.stringify({ summary }));
