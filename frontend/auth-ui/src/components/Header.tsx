import { useState, useEffect } from 'react';
import { Link, useLocation, useSearchParams } from 'react-router-dom';
import { authApi, settingsApi } from '../utils/api';
import { SiteLogoOverlay } from './SiteLogoOverlay';

export function Header() {
  const [mainLogoUrl, setMainLogoUrl] = useState<string | null>(null);
  const [siteLogoUrl, setSiteLogoUrl] = useState<string | null>(null);
  const [siteName, setSiteName] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);
  const location = useLocation();
  const [searchParams] = useSearchParams();

  const siteKey = searchParams.get('site');

  useEffect(() => {
    loadLogos();
  }, [siteKey]);

  const loadLogos = async () => {
    setIsLoading(true);
    try {
      // Load main logo
      const mainLogoResponse = await settingsApi.getMainLogo();
      if (mainLogoResponse.hasLogo && mainLogoResponse.logoUrl) {
        setMainLogoUrl(settingsApi.getLogoDisplayUrl(mainLogoResponse.logoUrl));
      }

      // Load site logo if site key is present
      if (siteKey) {
        const sites = await authApi.getSites();
        const site = sites.find(s => s.key === siteKey);
        if (site) {
          setSiteName(site.name);
          if (site.logoUrl) {
            setSiteLogoUrl(settingsApi.getLogoDisplayUrl(site.logoUrl));
          }
        }
      }
    } catch (error) {
      console.error('Failed to load logos:', error);
    } finally {
      setIsLoading(false);
    }
  };

  // Don't show header on admin page (it has its own navigation)
  if (location.pathname.startsWith('/admin')) {
    return null;
  }

  return (
    <header className="fixed top-0 left-0 right-0 z-50 bg-white/80 backdrop-blur-sm border-b border-gray-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <Link to="/" className="flex items-center">
            {isLoading ? (
              <div className="w-10 h-10 bg-gray-200 rounded-lg animate-pulse" />
            ) : (
              <SiteLogoOverlay
                mainLogoUrl={mainLogoUrl}
                siteLogoUrl={siteLogoUrl}
                siteName={siteName}
                size="md"
                showFallback={true}
              />
            )}
          </Link>

          {/* Navigation links */}
          <nav className="flex items-center space-x-4">
            {location.pathname !== '/login' && (
              <Link
                to={`/login${location.search}`}
                className="text-sm font-medium text-gray-600 hover:text-gray-900 transition-colors"
              >
                Sign In
              </Link>
            )}
            {location.pathname !== '/register' && (
              <Link
                to={`/register${location.search}`}
                className="text-sm font-medium text-white bg-green-600 hover:bg-green-700 px-4 py-2 rounded-lg transition-colors"
              >
                Sign Up
              </Link>
            )}
          </nav>
        </div>
      </div>
    </header>
  );
}
