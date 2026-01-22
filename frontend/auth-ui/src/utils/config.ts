// Runtime configuration utility
// Reads from window.__FUNTIME_CONFIG__ (set by /config.js)
// Falls back to Vite env vars for development

interface RuntimeConfig {
  API_URL: string;
  STRIPE_PUBLISHABLE_KEY: string;
  DEV_SITE_URLS?: Record<string, string>; // Map site keys to localhost URLs for development
}

declare global {
  interface Window {
    __FUNTIME_CONFIG__?: RuntimeConfig;
  }
}

function getConfig(): RuntimeConfig {
  // Runtime config takes priority (from config.js)
  if (window.__FUNTIME_CONFIG__) {
    return window.__FUNTIME_CONFIG__;
  }

  // Fallback to Vite env vars (for local development)
  return {
    API_URL: import.meta.env.VITE_API_URL || '',
    STRIPE_PUBLISHABLE_KEY: import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY || ''
  };
}

export const config = getConfig();

// Check if running on localhost
export function isLocalhost(): boolean {
  return window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
}

// Get the appropriate site URL (dev or prod)
export function getSiteUrl(siteKey: string, prodUrl: string): string {
  if (isLocalhost() && config.DEV_SITE_URLS?.[siteKey]) {
    return config.DEV_SITE_URLS[siteKey];
  }
  return prodUrl;
}
