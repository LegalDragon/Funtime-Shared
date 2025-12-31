import { useState, useEffect } from 'react';
import { Users, Globe, CreditCard, LogOut, Search, ChevronRight, Edit2, X, Loader2, TrendingUp, Upload, Trash2, Bell, Settings, Image, FileText, Save, CheckCircle, ExternalLink, Radio } from 'lucide-react';
import { adminApi, assetApi, settingsApi } from '../utils/api';
import type { Site, AdminUser, AdminUserDetail, AdminPayment, AdminStats, AssetUploadResponse, AdminPaymentMethod } from '../utils/api';
import { AssetUploadModal } from '../components/AssetUploadModal';
import { NotificationsTab } from '../components/NotificationsTab';
import { PushTestTab } from '../components/PushTestTab';
import { PaymentModal } from '../components/PaymentModal';
import { SiteLogoPreview } from '../components/SiteLogoOverlay';
import { RichTextEditor } from '../components/RichTextEditor';
import { config } from '../utils/config';

// Stripe publishable key from runtime config
const STRIPE_PUBLISHABLE_KEY = config.STRIPE_PUBLISHABLE_KEY;

type Tab = 'overview' | 'sites' | 'users' | 'payments' | 'notifications' | 'push' | 'settings';

export function AdminDashboardPage() {
  const [activeTab, setActiveTab] = useState<Tab>('overview');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Stats
  const [stats, setStats] = useState<AdminStats | null>(null);

  // Sites state
  const [sites, setSites] = useState<Site[]>([]);
  const [editingSite, setEditingSite] = useState<Site | null>(null);
  const [uploadingLogo, setUploadingLogo] = useState(false);
  const [showLogoUpload, setShowLogoUpload] = useState(false);

  // Users state
  const [userSearch, setUserSearch] = useState('');
  const [userSiteFilter, setUserSiteFilter] = useState('');
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [usersTotalCount, setUsersTotalCount] = useState(0);
  const [usersPage, setUsersPage] = useState(1);
  const [selectedUser, setSelectedUser] = useState<AdminUserDetail | null>(null);

  // Payments state
  const [payments, setPayments] = useState<AdminPayment[]>([]);
  const [paymentsTotalCount, setPaymentsTotalCount] = useState(0);
  const [paymentsTotalAmount, setPaymentsTotalAmount] = useState(0);
  const [paymentsPage, setPaymentsPage] = useState(1);
  const [paymentUserSearch, setPaymentUserSearch] = useState('');
  const [paymentSiteFilter, setPaymentSiteFilter] = useState('');

  // Manual charge modal state
  const [showChargeModal, setShowChargeModal] = useState(false);
  const [chargeAmount, setChargeAmount] = useState('');
  const [chargeDescription, setChargeDescription] = useState('');
  const [chargeSiteKey, setChargeSiteKey] = useState('');
  const [isCharging, setIsCharging] = useState(false);

  // Payment flow state
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [paymentClientSecret, setPaymentClientSecret] = useState('');
  const [paymentAmountCents, setPaymentAmountCents] = useState(0);
  const [userPaymentMethods, setUserPaymentMethods] = useState<AdminPaymentMethod[]>([]);

  // User payments state (for user detail modal)
  const [userPayments, setUserPayments] = useState<AdminPayment[]>([]);
  const [userPaymentsLoading, setUserPaymentsLoading] = useState(false);

  // Password reset state
  const [newPassword, setNewPassword] = useState('');
  const [savingPassword, setSavingPassword] = useState(false);

  // Settings state
  const [mainLogoUrl, setMainLogoUrl] = useState<string | null>(null);
  const [mainLogoLoading, setMainLogoLoading] = useState(false);
  const [uploadingMainLogo, setUploadingMainLogo] = useState(false);

  // Legal content state
  const [termsOfService, setTermsOfService] = useState('');
  const [privacyPolicy, setPrivacyPolicy] = useState('');
  const [legalContentLoading, setLegalContentLoading] = useState(false);
  const [savingTerms, setSavingTerms] = useState(false);
  const [savingPrivacy, setSavingPrivacy] = useState(false);
  const [termsSaved, setTermsSaved] = useState(false);
  const [privacySaved, setPrivacySaved] = useState(false);

  const handleLogout = () => {
    localStorage.removeItem('auth_token');
    window.location.href = '/login';
  };

  const handleVisitSite = (siteUrl: string | undefined) => {
    if (!siteUrl) return;
    const token = localStorage.getItem('auth_token');
    if (token) {
      window.location.href = `${siteUrl}/auth/callback?token=${encodeURIComponent(token)}`;
    } else {
      window.location.href = siteUrl;
    }
  };

  // Load stats and sites on mount
  useEffect(() => {
    loadStats();
    loadSites(); // Load sites for filter dropdowns
  }, []);

  // Load data when tab changes
  useEffect(() => {
    if (activeTab === 'payments') loadPayments();
    if (activeTab === 'settings' || activeTab === 'sites') loadMainLogo();
    if (activeTab === 'settings') loadLegalContent();
  }, [activeTab]);

  const loadMainLogo = async () => {
    setMainLogoLoading(true);
    try {
      const response = await settingsApi.getMainLogo();
      if (response.hasLogo && response.logoUrl) {
        setMainLogoUrl(settingsApi.getLogoDisplayUrl(response.logoUrl));
      } else {
        setMainLogoUrl(null);
      }
    } catch (err) {
      console.error('Failed to load main logo:', err);
    } finally {
      setMainLogoLoading(false);
    }
  };

  const loadLegalContent = async () => {
    setLegalContentLoading(true);
    try {
      const [tosResponse, privacyResponse] = await Promise.all([
        settingsApi.getTermsOfService(),
        settingsApi.getPrivacyPolicy(),
      ]);
      setTermsOfService(tosResponse.content || '');
      setPrivacyPolicy(privacyResponse.content || '');
    } catch (err) {
      console.error('Failed to load legal content:', err);
    } finally {
      setLegalContentLoading(false);
    }
  };

  const handleSaveTermsOfService = async () => {
    setSavingTerms(true);
    setTermsSaved(false);
    try {
      await settingsApi.updateTermsOfService(termsOfService);
      setTermsSaved(true);
      setTimeout(() => setTermsSaved(false), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save Terms of Service');
    } finally {
      setSavingTerms(false);
    }
  };

  const handleSavePrivacyPolicy = async () => {
    setSavingPrivacy(true);
    setPrivacySaved(false);
    try {
      await settingsApi.updatePrivacyPolicy(privacyPolicy);
      setPrivacySaved(true);
      setTimeout(() => setPrivacySaved(false), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save Privacy Policy');
    } finally {
      setSavingPrivacy(false);
    }
  };

  const handleMainLogoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploadingMainLogo(true);
    try {
      const response = await settingsApi.uploadMainLogo(file);
      if (response.hasLogo && response.logoUrl) {
        setMainLogoUrl(settingsApi.getLogoDisplayUrl(response.logoUrl));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to upload logo');
    } finally {
      setUploadingMainLogo(false);
    }
  };

  const handleMainLogoDelete = async () => {
    if (!confirm('Are you sure you want to delete the main logo?')) return;

    try {
      await settingsApi.deleteMainLogo();
      setMainLogoUrl(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete logo');
    }
  };

  const loadStats = async () => {
    try {
      const data = await adminApi.getStats();
      setStats(data);
    } catch (err) {
      console.error('Failed to load stats:', err);
    }
  };

  const loadSites = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getSites();
      setSites(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load sites');
    } finally {
      setIsLoading(false);
    }
  };

  const loadUsers = async (search?: string, siteKey?: string, page = 1) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.searchUsers({ search, siteKey, page });
      setUsers(data.users);
      setUsersTotalCount(data.totalCount);
      setUsersPage(page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load users');
    } finally {
      setIsLoading(false);
    }
  };

  const loadUserDetail = async (id: number) => {
    setIsLoading(true);
    setUserPayments([]);
    try {
      const data = await adminApi.getUser(id);
      setSelectedUser(data);
      // Also load full payment history
      handleLoadUserPayments(id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load user');
    } finally {
      setIsLoading(false);
    }
  };

  const loadPayments = async (userSearch?: string, siteKey?: string, page = 1) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getPayments({ userSearch, siteKey, page });
      setPayments(data.payments);
      setPaymentsTotalCount(data.totalCount);
      setPaymentsTotalAmount(data.totalAmountCents);
      setPaymentsPage(page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load payments');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearchUsers = () => {
    loadUsers(userSearch, userSiteFilter);
  };

  const handleSearchPayments = () => {
    loadPayments(paymentUserSearch || undefined, paymentSiteFilter || undefined);
  };

  const handleLoadUserPayments = async (userId: number) => {
    setUserPaymentsLoading(true);
    try {
      const data = await adminApi.getPayments({ userId, pageSize: 50 });
      setUserPayments(data.payments);
    } catch (err) {
      console.error('Failed to load user payments:', err);
    } finally {
      setUserPaymentsLoading(false);
    }
  };

  const handleManualCharge = async () => {
    if (!selectedUser || !chargeAmount || !chargeDescription) return;

    const amountCents = Math.round(parseFloat(chargeAmount) * 100);
    if (isNaN(amountCents) || amountCents <= 0) {
      setError('Please enter a valid amount');
      return;
    }

    setIsCharging(true);
    try {
      // First, get user's saved payment methods
      const methods = await adminApi.getUserPaymentMethods(selectedUser.id);
      setUserPaymentMethods(methods);

      // Create payment intent
      const result = await adminApi.createPaymentIntent({
        userId: selectedUser.id,
        amountCents,
        description: chargeDescription,
        siteKey: chargeSiteKey || undefined,
      });

      if (result.clientSecret) {
        // Show payment modal with Stripe Elements
        setPaymentClientSecret(result.clientSecret);
        setPaymentAmountCents(amountCents);
        setShowChargeModal(false);
        setShowPaymentModal(true);
      } else if (result.status === 'succeeded') {
        // Payment already succeeded (shouldn't happen without payment method, but handle it)
        setShowChargeModal(false);
        setChargeAmount('');
        setChargeDescription('');
        setChargeSiteKey('');
        handleLoadUserPayments(selectedUser.id);
      } else {
        setError('Failed to create payment intent');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create charge');
    } finally {
      setIsCharging(false);
    }
  };

  const handlePaymentSuccess = (paymentIntentId: string) => {
    console.log('Payment succeeded:', paymentIntentId);
    setShowPaymentModal(false);
    setPaymentClientSecret('');
    setChargeAmount('');
    setChargeDescription('');
    setChargeSiteKey('');
    // Reload user payments and stats
    if (selectedUser) {
      handleLoadUserPayments(selectedUser.id);
    }
    loadStats();
  };

  const handlePaymentModalClose = () => {
    setShowPaymentModal(false);
    setPaymentClientSecret('');
  };

  const handleUpdateSite = async (key: string, updates: Partial<Site>) => {
    try {
      await adminApi.updateSite(key, updates);
      setEditingSite(null);
      loadSites();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update site');
    }
  };

  const handleLogoUploadComplete = async (asset: AssetUploadResponse) => {
    if (!editingSite) return;
    setUploadingLogo(true);
    try {
      // Update site with the new asset URL
      const logoUrl = assetApi.getUrl(asset.assetId);
      const updatedSite = await adminApi.updateSite(editingSite.key, { logoUrl });
      setEditingSite(updatedSite);
      setShowLogoUpload(false);
      loadSites();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update site logo');
    } finally {
      setUploadingLogo(false);
    }
  };

  const handleLogoDelete = async () => {
    if (!editingSite) return;
    setUploadingLogo(true);
    try {
      // Clear the logo URL from the site
      const updatedSite = await adminApi.updateSite(editingSite.key, { logoUrl: '' });
      setEditingSite(updatedSite);
      loadSites();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete logo');
    } finally {
      setUploadingLogo(false);
    }
  };

  const handleUpdateUser = async (id: number, updates: Partial<AdminUser>) => {
    try {
      await adminApi.updateUser(id, updates);
      if (selectedUser) {
        loadUserDetail(id);
      }
      loadUsers(userSearch, userSiteFilter, usersPage);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update user');
    }
  };

  const formatCurrency = (cents: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(cents / 100);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-4 flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-gray-900">System Admin</h1>
            <p className="text-sm text-gray-500">Funtime Pickleball Management</p>
          </div>
          <button
            onClick={handleLogout}
            className="inline-flex items-center gap-2 px-4 py-2 text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-lg transition-colors"
          >
            <LogOut className="w-4 h-4" />
            Sign out
          </button>
        </div>
      </header>

      <div className="max-w-7xl mx-auto px-4 py-8">
        {/* Error Message */}
        {error && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
            {error}
            <button onClick={() => setError(null)} className="ml-2 text-red-500 hover:text-red-700">
              <X className="w-4 h-4 inline" />
            </button>
          </div>
        )}

        {/* Tabs */}
        <div className="flex gap-2 mb-8">
          <button
            onClick={() => setActiveTab('overview')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'overview'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <TrendingUp className="w-4 h-4" />
            Overview
          </button>
          <button
            onClick={() => setActiveTab('sites')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'sites'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <Globe className="w-4 h-4" />
            Sites
          </button>
          <button
            onClick={() => setActiveTab('users')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'users'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <Users className="w-4 h-4" />
            Users
          </button>
          <button
            onClick={() => setActiveTab('payments')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'payments'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <CreditCard className="w-4 h-4" />
            Payments
          </button>
          <button
            onClick={() => setActiveTab('notifications')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'notifications'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <Bell className="w-4 h-4" />
            Notifications
          </button>
          <button
            onClick={() => setActiveTab('push')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'push'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <Radio className="w-4 h-4" />
            Push Test
          </button>
          <button
            onClick={() => setActiveTab('settings')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'settings'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <Settings className="w-4 h-4" />
            Settings
          </button>
        </div>

        {/* Overview Tab */}
        {activeTab === 'overview' && stats && (
          <div className="grid md:grid-cols-4 gap-6">
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
              <p className="text-sm text-gray-500 mb-1">Total Users</p>
              <p className="text-3xl font-bold text-gray-900">{stats.totalUsers.toLocaleString()}</p>
              <p className="text-sm text-green-600 mt-2">+{stats.newUsersToday} today</p>
            </div>
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
              <p className="text-sm text-gray-500 mb-1">New This Week</p>
              <p className="text-3xl font-bold text-gray-900">{stats.newUsersThisWeek.toLocaleString()}</p>
              <p className="text-sm text-gray-500 mt-2">{stats.newUsersThisMonth} this month</p>
            </div>
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
              <p className="text-sm text-gray-500 mb-1">Active Subscriptions</p>
              <p className="text-3xl font-bold text-gray-900">{stats.activeSubscriptions.toLocaleString()}</p>
            </div>
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
              <p className="text-sm text-gray-500 mb-1">Revenue (30d)</p>
              <p className="text-3xl font-bold text-gray-900">{formatCurrency(stats.revenueThisMonthCents)}</p>
            </div>
          </div>
        )}

        {/* Sites Tab */}
        {activeTab === 'sites' && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200">
            <div className="p-6 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900">Available Sites</h2>
              <p className="text-sm text-gray-500 mt-1">Configure and manage Funtime Pickleball sites</p>
            </div>
            {isLoading ? (
              <div className="p-8 text-center">
                <Loader2 className="w-8 h-8 animate-spin mx-auto text-gray-400" />
              </div>
            ) : (
              <div className="divide-y divide-gray-200">
                {sites.map((site) => (
                  <div key={site.key} className="p-4 hover:bg-gray-50">
                    <div className="flex items-center gap-4">
                      {/* Site Logo */}
                      <div className="w-12 h-12 flex-shrink-0 rounded-lg overflow-hidden bg-gray-100 flex items-center justify-center">
                        {site.logoUrl ? (
                          <img
                            src={settingsApi.getLogoDisplayUrl(site.logoUrl)}
                            alt={`${site.name} logo`}
                            className="w-full h-full object-cover"
                          />
                        ) : (
                          <Globe className="w-6 h-6 text-gray-400" />
                        )}
                      </div>
                      {/* Logo Overlay Preview (main logo + site logo) */}
                      {(mainLogoUrl || site.logoUrl) && (
                        <div className="flex-shrink-0 border border-gray-200 rounded p-1 bg-white" title="Header Preview">
                          <SiteLogoPreview
                            mainLogoUrl={mainLogoUrl}
                            siteLogoUrl={site.logoUrl ? settingsApi.getLogoDisplayUrl(site.logoUrl) : null}
                            siteName={site.name}
                          />
                        </div>
                      )}
                      <div className="flex-1 min-w-0">
                        <h3 className="font-medium text-gray-900">{site.name}</h3>
                        <p className="text-sm text-gray-500 truncate">{site.key} • {site.url}</p>
                        {site.description && (
                          <p className="text-sm text-gray-400 mt-1 truncate">{site.description}</p>
                        )}
                      </div>
                    <div className="flex items-center gap-3">
                      <span className={`px-2 py-1 text-xs font-medium rounded-full ${
                        site.isActive
                          ? 'bg-green-100 text-green-700'
                          : 'bg-gray-100 text-gray-600'
                      }`}>
                        {site.isActive ? 'Active' : 'Inactive'}
                      </span>
                      {site.requiresSubscription && (
                        <span className="px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-700">
                          Paid
                        </span>
                      )}
                      {site.url ? (
                        <button
                          onClick={() => handleVisitSite(site.url)}
                          className="inline-flex items-center gap-1 px-3 py-1 text-sm bg-primary-500 text-white rounded-lg hover:bg-primary-600"
                        >
                          <ExternalLink className="w-4 h-4" />
                          Visit
                        </button>
                      ) : (
                        <span className="text-xs text-gray-400">No URL</span>
                      )}
                      <button
                        onClick={() => setEditingSite(site)}
                        className="text-gray-400 hover:text-gray-600"
                        title="Edit site"
                      >
                        <Edit2 className="w-5 h-5" />
                      </button>
                    </div>
                    </div>
                  </div>
                ))}
                {sites.length === 0 && (
                  <div className="p-8 text-center text-gray-500">
                    No sites configured yet
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* Users Tab */}
        {activeTab === 'users' && (
          <div className="space-y-6">
            <div className="bg-white rounded-xl shadow-sm border border-gray-200">
              <div className="p-6 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">User Management</h2>
                <p className="text-sm text-gray-500 mt-1">Search and manage user accounts</p>
                <div className="mt-4 flex gap-2">
                  <div className="relative flex-1">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
                    <input
                      type="text"
                      placeholder="Search by email or phone..."
                      value={userSearch}
                      onChange={(e) => setUserSearch(e.target.value)}
                      onKeyDown={(e) => e.key === 'Enter' && handleSearchUsers()}
                      className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                    />
                  </div>
                  <select
                    value={userSiteFilter}
                    onChange={(e) => setUserSiteFilter(e.target.value)}
                    className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  >
                    <option value="">All Sites</option>
                    {sites.map((site) => (
                      <option key={site.key} value={site.key}>
                        {site.name}
                      </option>
                    ))}
                  </select>
                  <button
                    onClick={handleSearchUsers}
                    disabled={isLoading}
                    className="px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 disabled:opacity-50"
                  >
                    {isLoading ? <Loader2 className="w-5 h-5 animate-spin" /> : 'Search'}
                  </button>
                </div>
              </div>
              {users.length > 0 ? (
                <div className="divide-y divide-gray-200">
                  {users.map((user) => (
                    <div key={user.id} className="p-4 flex items-center justify-between hover:bg-gray-50">
                      <div>
                        <p className="font-medium text-gray-900">
                          {user.email || user.phoneNumber || `User #${user.id}`}
                        </p>
                        <p className="text-sm text-gray-500">
                          ID: {user.id} • Joined {formatDate(user.createdAt)}
                          {user.systemRole && <span className="ml-2 text-purple-600">({user.systemRole})</span>}
                        </p>
                      </div>
                      <button
                        onClick={() => loadUserDetail(user.id)}
                        className="text-primary-600 hover:text-primary-700"
                      >
                        <ChevronRight className="w-5 h-5" />
                      </button>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="p-8 text-center text-gray-500">
                  <Users className="w-12 h-12 mx-auto mb-4 text-gray-300" />
                  <p>Enter a search term to find users</p>
                </div>
              )}
              {usersTotalCount > 20 && (
                <div className="p-4 border-t border-gray-200 flex items-center justify-between">
                  <p className="text-sm text-gray-500">
                    Showing {users.length} of {usersTotalCount} users
                  </p>
                  <div className="flex gap-2">
                    <button
                      onClick={() => loadUsers(userSearch, userSiteFilter, usersPage - 1)}
                      disabled={usersPage <= 1}
                      className="px-3 py-1 border rounded disabled:opacity-50"
                    >
                      Previous
                    </button>
                    <button
                      onClick={() => loadUsers(userSearch, userSiteFilter, usersPage + 1)}
                      disabled={users.length < 20}
                      className="px-3 py-1 border rounded disabled:opacity-50"
                    >
                      Next
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* User Detail Modal */}
            {selectedUser && (
              <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
                <div className="bg-white rounded-xl shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto m-4">
                  <div className="p-6 border-b border-gray-200 flex items-center justify-between">
                    <h3 className="text-lg font-semibold">User Details</h3>
                    <button onClick={() => { setSelectedUser(null); setNewPassword(''); }} className="text-gray-400 hover:text-gray-600">
                      <X className="w-5 h-5" />
                    </button>
                  </div>
                  <div className="p-6 space-y-6">
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="text-sm text-gray-500">Email</label>
                        <p className="font-medium">{selectedUser.email || '-'}</p>
                      </div>
                      <div>
                        <label className="text-sm text-gray-500">Phone</label>
                        <p className="font-medium">{selectedUser.phoneNumber || '-'}</p>
                      </div>
                      <div>
                        <label className="text-sm text-gray-500">System Role</label>
                        <p className="font-medium">{selectedUser.systemRole || 'None'}</p>
                      </div>
                      <div>
                        <label className="text-sm text-gray-500">Last Login</label>
                        <p className="font-medium">
                          {selectedUser.lastLoginAt ? formatDate(selectedUser.lastLoginAt) : 'Never'}
                        </p>
                      </div>
                    </div>

                    <div>
                      <h4 className="font-medium text-gray-900 mb-2">Site Roles</h4>
                      <div className="space-y-2">
                        {sites.map((site) => {
                          const userSite = selectedUser.sites.find(s => s.siteKey === site.key);
                          const currentRole = userSite?.role || '';
                          return (
                            <div key={site.key} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                              <span className="font-medium">{site.name}</span>
                              <select
                                value={currentRole}
                                onChange={async (e) => {
                                  const newRole = e.target.value;
                                  try {
                                    await adminApi.updateUserSiteRole(selectedUser.id, site.key, newRole);
                                    loadUserDetail(selectedUser.id);
                                  } catch (err) {
                                    setError(err instanceof Error ? err.message : 'Failed to update role');
                                  }
                                }}
                                className="text-sm px-2 py-1 border border-gray-300 rounded focus:ring-2 focus:ring-primary-500"
                              >
                                <option value="">Not a member</option>
                                <option value="member">Member</option>
                                <option value="moderator">Moderator</option>
                                <option value="admin">Admin</option>
                              </select>
                            </div>
                          );
                        })}
                      </div>
                    </div>

                    {selectedUser.subscriptions.length > 0 && (
                      <div>
                        <h4 className="font-medium text-gray-900 mb-2">Subscriptions</h4>
                        <div className="space-y-2">
                          {selectedUser.subscriptions.map((sub) => (
                            <div key={sub.id} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                              <span>{sub.planName || sub.siteKey}</span>
                              <span className={`px-2 py-1 text-xs rounded-full ${
                                sub.status === 'active' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'
                              }`}>
                                {sub.status}
                              </span>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <h4 className="font-medium text-gray-900">Payment History</h4>
                        <button
                          onClick={() => setShowChargeModal(true)}
                          className="px-3 py-1 text-sm bg-primary-500 text-white rounded-lg hover:bg-primary-600"
                        >
                          + Add Charge
                        </button>
                      </div>
                      {userPaymentsLoading ? (
                        <div className="text-center py-4">
                          <Loader2 className="w-6 h-6 animate-spin mx-auto text-gray-400" />
                        </div>
                      ) : userPayments.length > 0 ? (
                        <div className="space-y-2 max-h-60 overflow-y-auto">
                          {userPayments.map((payment) => (
                            <div key={payment.id} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                              <div>
                                <span className="font-medium">{formatCurrency(payment.amountCents)}</span>
                                <span className="text-sm text-gray-500 ml-2">{formatDate(payment.createdAt)}</span>
                                {payment.description && (
                                  <p className="text-xs text-gray-400 mt-0.5">{payment.description}</p>
                                )}
                              </div>
                              <div className="text-right">
                                <span className={`px-2 py-1 text-xs rounded-full ${
                                  payment.status === 'succeeded' ? 'bg-green-100 text-green-700' :
                                  payment.status === 'requires_payment_method' ? 'bg-yellow-100 text-yellow-700' :
                                  'bg-gray-100 text-gray-600'
                                }`}>
                                  {payment.status}
                                </span>
                                {payment.siteKey && (
                                  <p className="text-xs text-gray-400 mt-0.5">{payment.siteKey}</p>
                                )}
                              </div>
                            </div>
                          ))}
                        </div>
                      ) : (
                        <p className="text-sm text-gray-500 text-center py-4">No payments yet</p>
                      )}
                    </div>

                    {/* Password Reset */}
                    <div>
                      <h4 className="font-medium text-gray-900 mb-2">Reset Password</h4>
                      <div className="flex gap-2">
                        <input
                          type="password"
                          placeholder="New password (min 6 characters)"
                          value={newPassword}
                          onChange={(e) => setNewPassword(e.target.value)}
                          className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500"
                        />
                        <button
                          onClick={async () => {
                            if (!newPassword || newPassword.length < 6) {
                              setError('Password must be at least 6 characters');
                              return;
                            }
                            setSavingPassword(true);
                            try {
                              await adminApi.updateUser(selectedUser.id, { password: newPassword });
                              setNewPassword('');
                              setError(null);
                              alert('Password updated successfully');
                            } catch (err) {
                              setError(err instanceof Error ? err.message : 'Failed to update password');
                            } finally {
                              setSavingPassword(false);
                            }
                          }}
                          disabled={savingPassword || newPassword.length < 6}
                          className="px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          {savingPassword ? 'Saving...' : 'Update'}
                        </button>
                      </div>
                    </div>

                    <div className="pt-4 border-t flex gap-2">
                      <button
                        onClick={() => {
                          const newRole = selectedUser.systemRole === 'SU' ? '' : 'SU';
                          handleUpdateUser(selectedUser.id, { systemRole: newRole });
                        }}
                        className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                      >
                        {selectedUser.systemRole === 'SU' ? 'Remove Admin' : 'Make Admin'}
                      </button>
                    </div>

                    {/* Manual Charge Modal */}
                    {showChargeModal && (
                      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[60]">
                        <div className="bg-white rounded-xl shadow-xl max-w-md w-full m-4">
                          <div className="p-6 border-b border-gray-200 flex items-center justify-between">
                            <h3 className="text-lg font-semibold">Create Manual Charge</h3>
                            <button onClick={() => setShowChargeModal(false)} className="text-gray-400 hover:text-gray-600">
                              <X className="w-5 h-5" />
                            </button>
                          </div>
                          <div className="p-6 space-y-4">
                            <div>
                              <label className="block text-sm font-medium text-gray-700 mb-1">Amount (USD)</label>
                              <input
                                type="number"
                                step="0.01"
                                min="0.01"
                                placeholder="0.00"
                                value={chargeAmount}
                                onChange={(e) => setChargeAmount(e.target.value)}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                              />
                            </div>
                            <div>
                              <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                              <input
                                type="text"
                                placeholder="e.g., Tournament registration fee"
                                value={chargeDescription}
                                onChange={(e) => setChargeDescription(e.target.value)}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                              />
                            </div>
                            <div>
                              <label className="block text-sm font-medium text-gray-700 mb-1">Site (optional)</label>
                              <select
                                value={chargeSiteKey}
                                onChange={(e) => setChargeSiteKey(e.target.value)}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                              >
                                <option value="">No site</option>
                                {sites.map((site) => (
                                  <option key={site.key} value={site.key}>
                                    {site.name}
                                  </option>
                                ))}
                              </select>
                            </div>
                            {!STRIPE_PUBLISHABLE_KEY && (
                              <p className="text-sm text-amber-600 bg-amber-50 p-2 rounded">
                                Stripe key not configured. Set STRIPE_PUBLISHABLE_KEY in config.js to enable payments.
                              </p>
                            )}
                            <p className="text-sm text-gray-500">
                              This will open the payment form where you can use a saved card or enter new payment details.
                            </p>
                            <div className="flex gap-2 pt-2">
                              <button
                                onClick={() => setShowChargeModal(false)}
                                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                              >
                                Cancel
                              </button>
                              <button
                                onClick={handleManualCharge}
                                disabled={isCharging || !chargeAmount || !chargeDescription || !STRIPE_PUBLISHABLE_KEY}
                                className="flex-1 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 disabled:opacity-50"
                              >
                                {isCharging ? <Loader2 className="w-5 h-5 animate-spin mx-auto" /> : `Continue to Payment (${chargeAmount ? formatCurrency(parseFloat(chargeAmount) * 100) : '$0.00'})`}
                              </button>
                            </div>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Payments Tab */}
        {activeTab === 'payments' && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200">
            <div className="p-6 border-b border-gray-200">
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="text-lg font-semibold text-gray-900">Payment History</h2>
                  <p className="text-sm text-gray-500 mt-1">
                    {paymentsTotalCount} payments • {formatCurrency(paymentsTotalAmount)} total
                  </p>
                </div>
              </div>
              <div className="mt-4 flex gap-2">
                <input
                  type="text"
                  placeholder="Search by email or phone..."
                  value={paymentUserSearch}
                  onChange={(e) => setPaymentUserSearch(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearchPayments()}
                  className="w-64 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                />
                <select
                  value={paymentSiteFilter}
                  onChange={(e) => setPaymentSiteFilter(e.target.value)}
                  className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                >
                  <option value="">All Sites</option>
                  {sites.map((site) => (
                    <option key={site.key} value={site.key}>
                      {site.name}
                    </option>
                  ))}
                </select>
                <button
                  onClick={handleSearchPayments}
                  disabled={isLoading}
                  className="px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 disabled:opacity-50"
                >
                  {isLoading ? <Loader2 className="w-5 h-5 animate-spin" /> : 'Filter'}
                </button>
                {(paymentUserSearch || paymentSiteFilter) && (
                  <button
                    onClick={() => {
                      setPaymentUserSearch('');
                      setPaymentSiteFilter('');
                      loadPayments();
                    }}
                    className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                  >
                    Clear
                  </button>
                )}
              </div>
            </div>
            {isLoading ? (
              <div className="p-8 text-center">
                <Loader2 className="w-8 h-8 animate-spin mx-auto text-gray-400" />
              </div>
            ) : payments.length > 0 ? (
              <div className="divide-y divide-gray-200">
                {payments.map((payment) => (
                  <div key={payment.id} className="p-4 flex items-center justify-between">
                    <div>
                      <p className="font-medium text-gray-900">{formatCurrency(payment.amountCents)}</p>
                      <p className="text-sm text-gray-500">
                        {payment.userEmail || `User #${payment.userId}`}
                        {payment.siteKey && <span className="ml-2">• {payment.siteKey}</span>}
                      </p>
                    </div>
                    <div className="text-right">
                      <span className={`px-2 py-1 text-xs font-medium rounded-full ${
                        payment.status === 'succeeded'
                          ? 'bg-green-100 text-green-700'
                          : payment.status === 'pending'
                          ? 'bg-yellow-100 text-yellow-700'
                          : 'bg-red-100 text-red-700'
                      }`}>
                        {payment.status}
                      </span>
                      <p className="text-sm text-gray-500 mt-1">{formatDate(payment.createdAt)}</p>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="p-8 text-center text-gray-500">
                <CreditCard className="w-12 h-12 mx-auto mb-4 text-gray-300" />
                <p>No payments recorded yet</p>
              </div>
            )}
            {paymentsTotalCount > 20 && (
              <div className="p-4 border-t border-gray-200 flex items-center justify-between">
                <p className="text-sm text-gray-500">Page {paymentsPage}</p>
                <div className="flex gap-2">
                  <button
                    onClick={() => loadPayments(paymentUserSearch || undefined, paymentSiteFilter || undefined, paymentsPage - 1)}
                    disabled={paymentsPage <= 1}
                    className="px-3 py-1 border rounded disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <button
                    onClick={() => loadPayments(paymentUserSearch || undefined, paymentSiteFilter || undefined, paymentsPage + 1)}
                    disabled={payments.length < 20}
                    className="px-3 py-1 border rounded disabled:opacity-50"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Notifications Tab */}
        {activeTab === 'notifications' && <NotificationsTab />}

        {/* Push Test Tab */}
        {activeTab === 'push' && <PushTestTab />}

        {/* Settings Tab */}
        {activeTab === 'settings' && (
          <div className="space-y-6">
            {/* Main Logo Section */}
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
              <div className="flex items-center gap-3 mb-6">
                <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
                  <Image className="w-5 h-5 text-primary-600" />
                </div>
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">Main Logo</h3>
                  <p className="text-sm text-gray-500">This logo appears in the header across all pages</p>
                </div>
              </div>

              {mainLogoLoading ? (
                <div className="flex items-center justify-center py-8">
                  <Loader2 className="w-6 h-6 animate-spin text-primary-500" />
                </div>
              ) : (
                <div className="space-y-4">
                  {/* Current Logo Preview */}
                  <div className="border border-gray-200 rounded-lg p-4 bg-gray-50">
                    <p className="text-sm font-medium text-gray-700 mb-3">Current Logo</p>
                    {mainLogoUrl ? (
                      <div className="flex items-center gap-4">
                        <div className="bg-white border border-gray-200 rounded-lg p-3">
                          <img
                            src={mainLogoUrl}
                            alt="Main Logo"
                            className="h-12 w-auto max-w-[200px] object-contain"
                          />
                        </div>
                        <button
                          onClick={handleMainLogoDelete}
                          className="text-red-600 hover:text-red-700 text-sm font-medium flex items-center gap-1"
                        >
                          <Trash2 className="w-4 h-4" />
                          Remove
                        </button>
                      </div>
                    ) : (
                      <div className="text-gray-400 text-sm">No logo uploaded. A default logo will be shown.</div>
                    )}
                  </div>

                  {/* Upload New Logo */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Upload New Logo
                    </label>
                    <div className="flex items-center gap-3">
                      <label className="flex items-center gap-2 px-4 py-2 bg-primary-500 text-white rounded-lg cursor-pointer hover:bg-primary-600 transition-colors">
                        {uploadingMainLogo ? (
                          <Loader2 className="w-4 h-4 animate-spin" />
                        ) : (
                          <Upload className="w-4 h-4" />
                        )}
                        {uploadingMainLogo ? 'Uploading...' : 'Choose File'}
                        <input
                          type="file"
                          accept="image/jpeg,image/png,image/gif,image/webp,image/svg+xml"
                          onChange={handleMainLogoUpload}
                          className="hidden"
                          disabled={uploadingMainLogo}
                        />
                      </label>
                      <span className="text-sm text-gray-500">
                        JPEG, PNG, GIF, WebP, or SVG. Max 2MB.
                      </span>
                    </div>
                    <p className="text-xs text-gray-400 mt-2">
                      Recommended: Use a horizontal logo with transparent background. Max height: 40px when displayed.
                    </p>
                  </div>
                </div>
              )}
            </div>

            {/* Legal Content Section */}
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
              <div className="flex items-center gap-3 mb-6">
                <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
                  <FileText className="w-5 h-5 text-primary-600" />
                </div>
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">Legal Content</h3>
                  <p className="text-sm text-gray-500">Terms of Service and Privacy Policy displayed during registration</p>
                </div>
              </div>

              {legalContentLoading ? (
                <div className="flex items-center justify-center py-8">
                  <Loader2 className="w-6 h-6 animate-spin text-primary-500" />
                </div>
              ) : (
                <div className="space-y-6">
                  {/* Terms of Service */}
                  <div>
                    <div className="flex items-center justify-between mb-2">
                      <label className="block text-sm font-medium text-gray-700">
                        Terms of Service
                      </label>
                      <div className="flex items-center gap-2">
                        {termsSaved && (
                          <span className="flex items-center gap-1 text-sm text-green-600">
                            <CheckCircle className="w-4 h-4" />
                            Saved
                          </span>
                        )}
                        <button
                          onClick={handleSaveTermsOfService}
                          disabled={savingTerms}
                          className="flex items-center gap-1 px-3 py-1 text-sm bg-primary-500 text-white rounded-lg hover:bg-primary-600 disabled:opacity-50"
                        >
                          {savingTerms ? (
                            <Loader2 className="w-4 h-4 animate-spin" />
                          ) : (
                            <Save className="w-4 h-4" />
                          )}
                          Save
                        </button>
                      </div>
                    </div>
                    <RichTextEditor
                      value={termsOfService}
                      onChange={setTermsOfService}
                      placeholder="Enter your Terms of Service content here..."
                      minHeight="200px"
                    />
                  </div>

                  {/* Privacy Policy */}
                  <div>
                    <div className="flex items-center justify-between mb-2">
                      <label className="block text-sm font-medium text-gray-700">
                        Privacy Policy
                      </label>
                      <div className="flex items-center gap-2">
                        {privacySaved && (
                          <span className="flex items-center gap-1 text-sm text-green-600">
                            <CheckCircle className="w-4 h-4" />
                            Saved
                          </span>
                        )}
                        <button
                          onClick={handleSavePrivacyPolicy}
                          disabled={savingPrivacy}
                          className="flex items-center gap-1 px-3 py-1 text-sm bg-primary-500 text-white rounded-lg hover:bg-primary-600 disabled:opacity-50"
                        >
                          {savingPrivacy ? (
                            <Loader2 className="w-4 h-4 animate-spin" />
                          ) : (
                            <Save className="w-4 h-4" />
                          )}
                          Save
                        </button>
                      </div>
                    </div>
                    <RichTextEditor
                      value={privacyPolicy}
                      onChange={setPrivacyPolicy}
                      placeholder="Enter your Privacy Policy content here..."
                      minHeight="200px"
                    />
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Edit Site Modal */}
        {editingSite && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="bg-white rounded-xl shadow-xl max-w-md w-full m-4">
              <div className="p-6 border-b border-gray-200 flex items-center justify-between">
                <h3 className="text-lg font-semibold">Edit Site</h3>
                <button onClick={() => setEditingSite(null)} className="text-gray-400 hover:text-gray-600">
                  <X className="w-5 h-5" />
                </button>
              </div>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  handleUpdateSite(editingSite.key, editingSite);
                }}
                className="p-6 space-y-4"
              >
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
                  <input
                    type="text"
                    value={editingSite.name}
                    onChange={(e) => setEditingSite({ ...editingSite, name: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">URL</label>
                  <input
                    type="text"
                    value={editingSite.url || ''}
                    onChange={(e) => setEditingSite({ ...editingSite, url: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                  <textarea
                    value={editingSite.description || ''}
                    onChange={(e) => setEditingSite({ ...editingSite, description: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                    rows={2}
                  />
                </div>
                {/* Logo Upload Section */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Logo</label>
                  <div className="flex items-center gap-4">
                    <div className="w-16 h-16 rounded-lg overflow-hidden bg-gray-100 flex items-center justify-center">
                      {editingSite.logoUrl ? (
                        <img
                          src={editingSite.logoUrl}
                          alt="Site logo"
                          className="w-full h-full object-cover"
                        />
                      ) : (
                        <Globe className="w-8 h-8 text-gray-400" />
                      )}
                    </div>
                    <div className="flex-1 space-y-2">
                      <button
                        type="button"
                        onClick={() => setShowLogoUpload(true)}
                        disabled={uploadingLogo}
                        className="flex items-center gap-2 px-3 py-1.5 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
                      >
                        {uploadingLogo ? (
                          <Loader2 className="w-4 h-4 animate-spin" />
                        ) : (
                          <Upload className="w-4 h-4" />
                        )}
                        {editingSite.logoUrl ? 'Change Logo' : 'Upload Logo'}
                      </button>
                      {editingSite.logoUrl && (
                        <button
                          type="button"
                          onClick={handleLogoDelete}
                          disabled={uploadingLogo}
                          className="flex items-center gap-2 px-3 py-1.5 text-sm text-red-600 border border-red-200 rounded-lg hover:bg-red-50 disabled:opacity-50"
                        >
                          <Trash2 className="w-4 h-4" />
                          Remove Logo
                        </button>
                      )}
                    </div>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">Max 5MB. JPEG, PNG, GIF, WebP, or SVG.</p>
                </div>
                <div className="flex items-center gap-4">
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={editingSite.isActive}
                      onChange={(e) => setEditingSite({ ...editingSite, isActive: e.target.checked })}
                      className="rounded"
                    />
                    <span className="text-sm">Active</span>
                  </label>
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={editingSite.requiresSubscription}
                      onChange={(e) => setEditingSite({ ...editingSite, requiresSubscription: e.target.checked })}
                      className="rounded"
                    />
                    <span className="text-sm">Requires Subscription</span>
                  </label>
                </div>
                <div className="flex gap-2 pt-4">
                  <button
                    type="button"
                    onClick={() => setEditingSite(null)}
                    className="flex-1 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="flex-1 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600"
                  >
                    Save
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Logo Upload Modal */}
        <AssetUploadModal
          isOpen={showLogoUpload}
          onClose={() => setShowLogoUpload(false)}
          onUploadComplete={handleLogoUploadComplete}
          category="logos"
          acceptedTypes="image/jpeg,image/png,image/gif,image/webp,image/svg+xml"
          maxSizeMB={5}
          title="Upload Site Logo"
        />

        {/* Stripe Payment Modal */}
        {STRIPE_PUBLISHABLE_KEY && (
          <PaymentModal
            isOpen={showPaymentModal}
            onClose={handlePaymentModalClose}
            clientSecret={paymentClientSecret}
            stripePublishableKey={STRIPE_PUBLISHABLE_KEY}
            amountCents={paymentAmountCents}
            currency="usd"
            description={chargeDescription}
            savedPaymentMethods={userPaymentMethods.map(pm => ({
              id: pm.id,
              stripePaymentMethodId: pm.stripePaymentMethodId,
              type: pm.type || 'card',
              cardBrand: pm.cardBrand,
              cardLast4: pm.cardLast4,
              cardExpMonth: pm.cardExpMonth,
              cardExpYear: pm.cardExpYear,
              isDefault: pm.isDefault,
            }))}
            onPaymentSuccess={handlePaymentSuccess}
            onPaymentError={(err) => setError(err)}
          />
        )}
      </div>
    </div>
  );
}
