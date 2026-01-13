import { useState, useEffect } from 'react';
import { ExternalLink, LogOut, Loader2, Shield } from 'lucide-react';
import { authApi, settingsApi, getCurrentUser, type PublicSite } from '../utils/api';

// Fallback gradient colors for sites without logos
const fallbackColors: Record<string, string> = {
  'pickleball.community': 'from-blue-500 to-blue-600',
  'pickleball.college': 'from-green-500 to-green-600',
  'pickleball.date': 'from-pink-500 to-pink-600',
  'pickleball.jobs': 'from-purple-500 to-purple-600',
};

export function SiteSelectionPage() {
  const [sites, setSites] = useState<PublicSite[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const currentUser = getCurrentUser();
  const isSU = currentUser?.role === 'SU';

  useEffect(() => {
    loadSites();
  }, []);

  const loadSites = async () => {
    try {
      const data = await authApi.getSites();
      setSites(data);
    } catch (err) {
      console.error('Failed to load sites:', err);
      setError('Failed to load sites');
    } finally {
      setIsLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('auth_token');
    window.location.href = '/login';
  };

  const handleSiteClick = (siteUrl: string | undefined) => {
    if (!siteUrl) return;
    const token = localStorage.getItem('auth_token');
    if (token) {
      // Redirect to site with token
      window.location.href = `${siteUrl}/auth/callback?token=${encodeURIComponent(token)}`;
    } else {
      window.location.href = siteUrl;
    }
  };

  const getGradient = (siteKey: string) => {
    return fallbackColors[siteKey] || 'from-gray-500 to-gray-600';
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 via-white to-primary-100 px-4 py-12 pt-20">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Welcome to Funtime Pickleball</h1>
          <p className="text-gray-600">Choose a site to continue</p>
          {isSU && (
            <div className="mt-4 inline-flex items-center gap-2 px-4 py-2 bg-purple-100 text-purple-700 rounded-full text-sm font-medium">
              <Shield className="w-4 h-4" />
              System Administrator
            </div>
          )}
        </div>

        {/* Loading */}
        {isLoading && (
          <div className="flex justify-center py-12">
            <Loader2 className="w-8 h-8 animate-spin text-primary-500" />
          </div>
        )}

        {/* Error */}
        {error && (
          <div className="text-center py-12 text-red-600">
            {error}
          </div>
        )}

        {/* Sites Grid */}
        {!isLoading && !error && (
          <div className="grid md:grid-cols-2 gap-6 mb-8">
            {sites.map((site) => (
              <div
                key={site.key}
                className="bg-white rounded-2xl shadow-soft p-6 hover:shadow-lg transition-all"
              >
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-4">
                    {/* Logo or fallback icon */}
                    {site.logoUrl ? (
                      <img
                        src={settingsApi.getLogoDisplayUrl(site.logoUrl)}
                        alt={`${site.name} logo`}
                        className="w-12 h-12 object-contain rounded-lg"
                      />
                    ) : (
                      <div className={`w-12 h-12 rounded-lg bg-gradient-to-r ${getGradient(site.key)} flex items-center justify-center`}>
                        <span className="text-white text-xl font-bold">
                          {site.name.charAt(0).toUpperCase()}
                        </span>
                      </div>
                    )}
                    <div>
                      <h2 className="text-xl font-semibold text-gray-900">
                        {site.name}
                      </h2>
                      {site.description && (
                        <p className="text-gray-600 mt-1 text-sm">{site.description}</p>
                      )}
                      {isSU && site.url && (
                        <p className="text-gray-400 text-xs mt-1 truncate max-w-[200px]">{site.url}</p>
                      )}
                      {isSU && !site.url && (
                        <p className="text-amber-500 text-xs mt-1">URL not configured</p>
                      )}
                    </div>
                  </div>
                </div>
                {/* Action button */}
                <div className="mt-4 flex justify-end">
                  {site.url ? (
                    <button
                      onClick={() => handleSiteClick(site.url)}
                      className={`inline-flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors bg-gradient-to-r ${getGradient(site.key)} text-white hover:opacity-90`}
                    >
                      <ExternalLink className="w-4 h-4" />
                      Visit Site
                    </button>
                  ) : (
                    <span className="inline-flex items-center gap-2 px-4 py-2 rounded-lg font-medium bg-gray-200 text-gray-500 cursor-not-allowed">
                      <ExternalLink className="w-4 h-4" />
                      No URL
                    </span>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}

        {/* No sites message */}
        {!isLoading && !error && sites.length === 0 && (
          <div className="text-center py-12 text-gray-500">
            No sites available
          </div>
        )}

        {/* Logout */}
        <div className="text-center">
          <button
            onClick={handleLogout}
            className="inline-flex items-center gap-2 text-gray-500 hover:text-gray-700 transition-colors"
          >
            <LogOut className="w-4 h-4" />
            Sign out
          </button>
        </div>
      </div>
    </div>
  );
}
