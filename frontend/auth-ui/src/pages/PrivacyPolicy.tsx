import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Loader2, ArrowLeft } from 'lucide-react';
import { settingsApi, authApi, rewriteAssetUrls, type PublicSite } from '../utils/api';
import { SiteLogoOverlay } from '../components/SiteLogoOverlay';

export function PrivacyPolicyPage() {
  const [searchParams] = useSearchParams();
  const rawSiteKey = searchParams.get('site') || '';
  // Strip "pickleball." prefix if present
  const siteKey = rawSiteKey.startsWith('pickleball.') ? rawSiteKey.substring(11) : rawSiteKey;

  const [content, setContent] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [mainLogoUrl, setMainLogoUrl] = useState<string | null>(null);
  const [siteLogoUrl, setSiteLogoUrl] = useState<string | null>(null);
  const [foundSiteName, setFoundSiteName] = useState<string | null>(null);

  useEffect(() => {
    loadContent();
    loadSiteInfo();
  }, []);

  const loadContent = async () => {
    try {
      const response = await settingsApi.getPrivacyPolicy();
      setContent(response.content || '');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load Privacy Policy');
    } finally {
      setIsLoading(false);
    }
  };

  const loadSiteInfo = async () => {
    try {
      // Get main logo
      const mainLogoResponse = await settingsApi.getMainLogo();
      if (mainLogoResponse.hasLogo && mainLogoResponse.logoUrl) {
        setMainLogoUrl(settingsApi.getLogoDisplayUrl(mainLogoResponse.logoUrl));
      }

      // Get site-specific logo if site key provided
      if (siteKey) {
        const sites = await authApi.getSites();
        const site = sites.find((s: PublicSite) => s.key.toLowerCase() === siteKey.toLowerCase());
        if (site) {
          setFoundSiteName(site.name);
          if (site.logoUrl) {
            setSiteLogoUrl(settingsApi.getLogoDisplayUrl(site.logoUrl));
          }
        }
      }
    } catch (err) {
      console.error('Failed to load site info:', err);
    }
  };

  const getSiteTitle = () => {
    if (foundSiteName) {
      return `Pickleball.${foundSiteName}`;
    }
    return 'Funtime Pickleball';
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-4xl mx-auto px-4 py-8">
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-8">
          {/* Header with logo and title */}
          <div className="flex items-center gap-4 mb-6 pb-6 border-b border-gray-200">
            <button
              onClick={() => window.close()}
              className="text-gray-500 hover:text-gray-700"
              title="Close"
            >
              <ArrowLeft className="w-5 h-5" />
            </button>
            <SiteLogoOverlay
              mainLogoUrl={mainLogoUrl}
              siteLogoUrl={siteLogoUrl}
              siteName={foundSiteName || 'Site'}
              size="lg"
              showFallback={true}
            />
            <div className="flex-1">
              <h1 className="text-xl font-bold text-gray-900">{getSiteTitle()}</h1>
              <h2 className="text-lg text-gray-600">Privacy Policy</h2>
            </div>
          </div>

          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="w-8 h-8 animate-spin text-primary-500" />
            </div>
          ) : error ? (
            <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
              {error}
            </div>
          ) : content ? (
            <div
              className="prose prose-gray max-w-none"
              dangerouslySetInnerHTML={{ __html: rewriteAssetUrls(content) }}
            />
          ) : (
            <div className="text-gray-500 text-center py-8">
              No Privacy Policy has been configured yet.
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
