// Parse query parameters from URL
export function getQueryParams(): URLSearchParams {
  return new URLSearchParams(window.location.search);
}

// Get redirect URL from query params
// Supports both 'redirect' and 'returnUrl' parameter names
export function getRedirectUrl(): string | null {
  const params = getQueryParams();
  return params.get('redirect') || params.get('returnUrl');
}

// Get site identifier from query params
// Strips "pickleball." prefix if present (e.g., "pickleball.community" -> "community")
export function getSiteKey(): string | null {
  const params = getQueryParams();
  const siteKey = params.get('site');
  if (siteKey && siteKey.startsWith('pickleball.')) {
    return siteKey.substring('pickleball.'.length);
  }
  return siteKey;
}

// Get returnTo path from query params
export function getReturnTo(): string | null {
  const params = getQueryParams();
  return params.get('returnTo');
}

// Get language code from query params (case-insensitive)
export function getLangcode(): string | null {
  const params = getQueryParams();
  return params.get('Langcode') || params.get('langcode') || params.get('LangCode');
}

// Redirect back to the original site with token and optional site role info
export function redirectWithToken(
  token: string,
  options?: { siteRole?: string; isSiteAdmin?: boolean }
): void {
  const redirectUrl = getRedirectUrl();
  const returnTo = getReturnTo();
  const langcode = getLangcode();

  if (!redirectUrl) {
    // No redirect URL, just show success or go to a default page
    console.warn('No redirect URL provided');
    return;
  }

  // Build the callback URL with token
  const url = new URL(redirectUrl);
  url.searchParams.set('token', token);

  if (returnTo) {
    url.searchParams.set('returnTo', returnTo);
  }

  // Preserve language code for the destination site
  if (langcode) {
    url.searchParams.set('Langcode', langcode);
  }

  // Include site role information if provided
  if (options?.siteRole) {
    url.searchParams.set('siteRole', options.siteRole);
  }
  if (options?.isSiteAdmin !== undefined) {
    url.searchParams.set('isSiteAdmin', options.isSiteAdmin.toString());
  }

  // Redirect to the callback URL
  window.location.href = url.toString();
}

// Validate that redirect URL is from an allowed domain
export function isAllowedRedirect(redirectUrl: string): boolean {
  const allowedDomains = [
    'pickleball.community',
    'pickleball.college',
    'pickleball.date',
    'pickleball.jobs',
    'localhost',
    '127.0.0.1',
  ];

  try {
    const url = new URL(redirectUrl);
    return allowedDomains.some(domain =>
      url.hostname === domain || url.hostname.endsWith(`.${domain}`)
    );
  } catch {
    return false;
  }
}

// Get display name for a site
export function getSiteDisplayName(siteKey: string | null): string {
  const siteNames: Record<string, string> = {
    'pickleball.community': 'Pickleball Community',
    'pickleball.college': 'Pickleball College',
    'pickleball.date': 'Pickleball Date',
    'pickleball.jobs': 'Pickleball Jobs',
    'community': 'Pickleball Community',
    'college': 'Pickleball College',
    'date': 'Pickleball Date',
    'jobs': 'Pickleball Jobs',
  };

  return siteKey ? siteNames[siteKey] || siteKey : 'Funtime Pickleball';
}
