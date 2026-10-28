// Service worker: makes the app shell render instantly on return visits, even
// while the backend is cold-starting from zero replicas (~20 s). The page then
// shows its own "waking up" state until the API answers.
//
// - Navigations: serve the cached index.html at once and refresh it in the
//   background (stale-while-revalidate). First visit falls through to network.
// - /assets/*: Vite content-hashes these, so cache-first is safe. A shell's
//   assets are the files index.html names plus the page chunks its scripts
//   load later; both are cached up front, and each fresh index.html prunes
//   assets no longer needed.
// - /api/*, /healthz and anything cross-origin: never intercepted.

const CACHE = 'wishlist-shell-v1';
const SHELL = '/index.html';
// Cached files are the same for every request, so a Vary header (some servers
// send Vary: Origin) must not make a cached copy miss.
const MATCH = { ignoreVary: true };

// The first page load happens before this worker controls anything, so fetch
// the shell and the assets it names up front; otherwise the second visit would
// still find an empty cache.
self.addEventListener('install', (event) => {
  event.waitUntil(
    (async () => {
      const cache = await caches.open(CACHE);
      const response = await fetch(SHELL, { cache: 'no-cache' });
      if (!response.ok) return;
      const html = await response.clone().text();
      // Assets first, so the cached shell never names files that aren't cached.
      // Fetching them all again also repairs caches left inconsistent by earlier versions.
      await cacheShellAssets(cache, html, { refetch: true });
      await cache.put(SHELL, response);
      await self.skipWaiting();
    })(),
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      for (const key of await caches.keys()) {
        if (key !== CACHE) await caches.delete(key);
      }
      await self.clients.claim();
    })(),
  );
});

// The app found a newer version and wants it cached before it reloads, so the
// reload doesn't get the old shell again. Always answers, even on failure.
self.addEventListener('message', (event) => {
  if (event.data?.type !== 'refresh-shell') return;
  const port = event.ports[0];
  event.waitUntil(
    (async () => {
      try {
        await refreshShell(await caches.open(CACHE));
      } catch {
        // Offline or server down: the app reloads anyway.
      }
      port?.postMessage('done');
    })(),
  );
});

self.addEventListener('fetch', (event) => {
  const { request } = event;
  if (request.method !== 'GET') return;

  const url = new URL(request.url);
  if (url.origin !== self.location.origin) return;
  if (url.pathname.startsWith('/api/') || url.pathname === '/healthz') return;

  if (request.mode === 'navigate') {
    event.respondWith(serveShell(event));
  } else if (url.pathname.startsWith('/assets/')) {
    event.respondWith(cacheFirst(request));
  }
});

async function serveShell(event) {
  const cache = await caches.open(CACHE);
  const cached = await cache.match(SHELL, MATCH);
  const refresh = refreshShell(cache);

  if (cached) {
    event.waitUntil(refresh.catch(() => {}));
    return cached;
  }
  return refresh;
}

async function refreshShell(cache) {
  const response = await fetch(SHELL, { cache: 'no-cache' });
  if (response.ok) {
    const html = await response.clone().text();
    // Cache the new shell's assets before the shell itself. A cached shell
    // without them would come up blank once a later deploy removes them from
    // the server. If they can't be fetched, keep the old shell for now.
    let keep;
    try {
      keep = await cacheShellAssets(cache, html);
    } catch {
      return response;
    }
    // The page being served right now may still be the previous shell, so keep
    // its assets too; only shells older than that lose theirs.
    const previous = await cache.match(SHELL, MATCH);
    if (previous) {
      for (const path of await cachedAssetsOf(cache, await previous.text())) keep.add(path);
    }
    await cache.put(SHELL, response.clone());
    await pruneAssets(cache, keep);
  }
  return response;
}

// "/assets/x" in index.html and CSS; "assets/x" in Vite's list of the chunks
// a script imports later.
function assetsIn(text) {
  const found = text.match(/(?<![\w.-])\/?assets\/[\w.-]+\.\w+/g) ?? [];
  return [...new Set(found.map((path) => (path.startsWith('/') ? path : '/' + path)))];
}

// Caches everything a shell needs: the assets index.html names, then the
// chunks those scripts name, and so on. Returns the set of paths.
async function cacheShellAssets(cache, html, { refetch = false } = {}) {
  const all = new Set();
  let next = assetsIn(html);
  while (next.length > 0) {
    if (refetch) await cache.addAll(next);
    else await cacheMissing(cache, next);
    next.forEach((path) => all.add(path));
    next = (await referencedByScripts(cache, next)).filter((path) => !all.has(path));
  }
  return all;
}

// The same set for a shell whose assets are already cached; fetches nothing.
async function cachedAssetsOf(cache, html) {
  const all = new Set();
  let next = assetsIn(html);
  while (next.length > 0) {
    next.forEach((path) => all.add(path));
    next = (await referencedByScripts(cache, next)).filter((path) => !all.has(path));
  }
  return all;
}

async function referencedByScripts(cache, paths) {
  const found = new Set();
  for (const path of paths) {
    if (!path.endsWith('.js')) continue;
    const cached = await cache.match(path, MATCH);
    if (cached) for (const ref of scriptRefs(await cached.text())) found.add(ref);
  }
  return [...found];
}

// A script names other chunks as "assets/x.js" (Vite's dependency lists) or,
// for chunks without dependencies, relative to itself: import("./x.js").
// Every chunk lives in /assets/, so both resolve there.
function scriptRefs(text) {
  const relative = [...text.matchAll(/["'`]\.\/([\w.-]+\.js)["'`]/g)].map((m) => '/assets/' + m[1]);
  return [...new Set([...assetsIn(text), ...relative])];
}

async function cacheMissing(cache, paths) {
  const missing = [];
  for (const path of paths) {
    if (!(await cache.match(path, MATCH))) missing.push(path);
  }
  await cache.addAll(missing);
}

async function cacheFirst(request) {
  const cache = await caches.open(CACHE);
  const cached = await cache.match(request, MATCH);
  if (cached) return cached;

  const response = await fetch(request);
  // Never keep an HTML page (a server's fallback for a missing file) as an asset.
  const isHtml = response.headers.get('content-type')?.includes('text/html');
  if (response.ok && !isHtml) await cache.put(request, response.clone());
  return response;
}

// Drop cached /assets/* files the kept shells don't need, so old deploys
// don't pile up.
async function pruneAssets(cache, referenced) {
  for (const request of await cache.keys()) {
    const path = new URL(request.url).pathname;
    if (path.startsWith('/assets/') && !referenced.has(path)) {
      await cache.delete(request);
    }
  }
}
