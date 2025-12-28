import { useState, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { settingsApi } from '../utils/api';

export function Header() {
  const [logoUrl, setLogoUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const location = useLocation();

  useEffect(() => {
    loadLogo();
  }, []);

  const loadLogo = async () => {
    try {
      const response = await settingsApi.getMainLogo();
      if (response.hasLogo && response.logoUrl) {
        setLogoUrl(settingsApi.getLogoDisplayUrl(response.logoUrl));
      }
    } catch (error) {
      console.error('Failed to load main logo:', error);
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
          <Link to="/" className="flex items-center space-x-3">
            {isLoading ? (
              <div className="w-10 h-10 bg-gray-200 rounded-lg animate-pulse" />
            ) : logoUrl ? (
              <img
                src={logoUrl}
                alt="Logo"
                className="h-10 w-auto max-w-[180px] object-contain"
                onError={(e) => {
                  // Hide broken image
                  (e.target as HTMLImageElement).style.display = 'none';
                }}
              />
            ) : (
              // Default logo/text when no logo is uploaded
              <div className="flex items-center space-x-2">
                <div className="w-10 h-10 bg-gradient-to-br from-green-500 to-emerald-600 rounded-lg flex items-center justify-center">
                  <span className="text-white font-bold text-lg">F</span>
                </div>
                <span className="text-xl font-semibold text-gray-900">Funtime</span>
              </div>
            )}
          </Link>

          {/* Navigation links */}
          <nav className="flex items-center space-x-4">
            {location.pathname !== '/login' && (
              <Link
                to="/login"
                className="text-sm font-medium text-gray-600 hover:text-gray-900 transition-colors"
              >
                Sign In
              </Link>
            )}
            {location.pathname !== '/register' && (
              <Link
                to="/register"
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
