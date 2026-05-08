const CACHE_NAME = 'reminder-v2';
const urlsToCache = [
  '/',
  '/manifest.json',
  '/icon-192.svg',
  '/icon-512.svg',
  '/css/admin.css'
];

// Install event - cache resources
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => {
        // Filter out URLs that might fail
        return Promise.allSettled(
          urlsToCache.map(url => cache.add(url).catch(() => null))
        );
      })
  );
  self.skipWaiting();
});

// Activate event - clean old caches
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames.map((cacheName) => {
          if (cacheName !== CACHE_NAME) {
            return caches.delete(cacheName);
          }
        })
      );
    })
  );
  self.clients.claim();
});

// Fetch event - serve from cache, fallback to network
self.addEventListener('fetch', (event) => {
  // Skip cross-origin requests (like CDN)
  if (!event.request.url.startsWith(self.location.origin)) {
    return;
  }

  // Skip API requests - let them go directly to network
  if (event.request.url.includes('/api/') || event.request.url.includes('/events') || event.request.url.includes('/workouts') || event.request.url.includes('/shopping') || event.request.url.includes('/digests')) {
    return;
  }

  event.respondWith(
    caches.match(event.request)
      .then((response) => {
        if (response) {
          return response;
        }
        return fetch(event.request).then((response) => {
          // Don't cache non-successful responses or opaque responses
          if (!response || response.status !== 200) {
            return response;
          }
          const responseToCache = response.clone();
          caches.open(CACHE_NAME)
            .then((cache) => {
              cache.put(event.request, responseToCache);
            });
          return response;
        }).catch(() => {
          // Return nothing on network error - prevents console errors
          return new Response('', { status: 503, statusText: 'Service Unavailable' });
        });
      })
  );
});