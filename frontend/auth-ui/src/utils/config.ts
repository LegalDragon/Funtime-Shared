// Runtime configuration utility
// Reads from window.__FUNTIME_CONFIG__ (set by /config.js)
// Falls back to Vite env vars for development

interface RuntimeConfig {
  API_URL: string;
  STRIPE_PUBLISHABLE_KEY: string;
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
