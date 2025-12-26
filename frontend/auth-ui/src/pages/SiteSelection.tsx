import { ExternalLink, LogOut } from 'lucide-react';

// Available Funtime Pickleball sites
const sites = [
  {
    key: 'pickleball.community',
    name: 'Pickleball Community',
    description: 'Connect with players in your area',
    url: 'https://pickleball.community',
    color: 'from-blue-500 to-blue-600',
  },
  {
    key: 'pickleball.college',
    name: 'Pickleball College',
    description: 'Learn and improve your skills',
    url: 'https://pickleball.college',
    color: 'from-green-500 to-green-600',
  },
  {
    key: 'pickleball.date',
    name: 'Pickleball Date',
    description: 'Find your perfect playing partner',
    url: 'https://pickleball.date',
    color: 'from-pink-500 to-pink-600',
  },
  {
    key: 'pickleball.jobs',
    name: 'Pickleball Jobs',
    description: 'Career opportunities in pickleball',
    url: 'https://pickleball.jobs',
    color: 'from-purple-500 to-purple-600',
  },
];

export function SiteSelectionPage() {
  const handleLogout = () => {
    localStorage.removeItem('auth_token');
    window.location.href = '/login';
  };

  const handleSiteClick = (siteUrl: string) => {
    const token = localStorage.getItem('auth_token');
    if (token) {
      // Redirect to site with token
      window.location.href = `${siteUrl}/auth/callback?token=${encodeURIComponent(token)}`;
    } else {
      window.location.href = siteUrl;
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 via-white to-primary-100 px-4 py-12">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Welcome to Funtime Pickleball</h1>
          <p className="text-gray-600">Choose a site to continue</p>
        </div>

        {/* Sites Grid */}
        <div className="grid md:grid-cols-2 gap-6 mb-8">
          {sites.map((site) => (
            <button
              key={site.key}
              onClick={() => handleSiteClick(site.url)}
              className="bg-white rounded-2xl shadow-soft p-6 text-left hover:shadow-lg transition-all group"
            >
              <div className="flex items-start justify-between">
                <div>
                  <h2 className="text-xl font-semibold text-gray-900 group-hover:text-primary-600 transition-colors">
                    {site.name}
                  </h2>
                  <p className="text-gray-600 mt-1">{site.description}</p>
                </div>
                <div className={`p-2 rounded-lg bg-gradient-to-r ${site.color} text-white`}>
                  <ExternalLink className="w-5 h-5" />
                </div>
              </div>
            </button>
          ))}
        </div>

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
