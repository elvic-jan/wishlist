// Service worker: makes the app shell render instantly on return visits, even
// while the backend is cold-starting from zero replicas (~20 s). The page then
// shows its own "waking up" state until the API answers.
//
// - Navigations: serve the cached index.html at once and refresh it in the
//   background (stale-while-revalidate). First visit falls through to network.
// - /assets/*: Vite content-hashes these, so cache-first is safe. Each fresh
//   index.html prunes assets it no longer references.
// - /api/*, /healthz and anything cross-origin: never intercepted.

const CACHE = 'wishlist-shell-v1';
const SHELL = '/index.html';

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
      await cache.put(SHELL, response);
      const assets = [...new Set(html.match(/\/assets\/[^"'\s)]+/g) ?? [])];
      await cache.addAll(assets);
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
  const cached = await cache.match(SHELL);
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
    // The page being served right now may still be the previous shell, so keep
    // its assets too; only shells older than that lose theirs.
    const previous = await cache.match(SHELL);
    const keep = (await response.clone().text()) + (previous ? await previous.text() : '');
    await cache.put(SHELL, response.clone());
    await pruneAssets(cache, keep);
  }
  return response;
}

async function cacheFirst(request) {
  const cache = await caches.open(CACHE);
  const cached = await cache.match(request);
  if (cached) return cached;

  const response = await fetch(request);
  if (response.ok) await cache.put(request, response.clone());
  return response;
}

// Drop cached /assets/* files the current index.html no longer references, so
// old deploys don't pile up. A lazily imported chunk (none today) would just be
// refetched on demand.
async function pruneAssets(cache, html) {
  const referenced = new Set(html.match(/\/assets\/[^"'\s)]+/g) ?? []);
  for (const request of await cache.keys()) {
    const path = new URL(request.url).pathname;
    if (path.startsWith('/assets/') && !referenced.has(path)) {
      await cache.delete(request);
    }
  }
}
