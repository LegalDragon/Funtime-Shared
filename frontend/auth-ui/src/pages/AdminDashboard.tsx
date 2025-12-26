import { useState, useEffect } from 'react';
import { Users, Globe, CreditCard, LogOut, Search, ChevronRight, Edit2, X, Loader2, TrendingUp } from 'lucide-react';
import { adminApi } from '../utils/api';
import type { Site, AdminUser, AdminUserDetail, AdminPayment, AdminStats } from '../utils/api';

type Tab = 'overview' | 'sites' | 'users' | 'payments';

export function AdminDashboardPage() {
  const [activeTab, setActiveTab] = useState<Tab>('overview');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Stats
  const [stats, setStats] = useState<AdminStats | null>(null);

  // Sites state
  const [sites, setSites] = useState<Site[]>([]);
  const [editingSite, setEditingSite] = useState<Site | null>(null);

  // Users state
  const [userSearch, setUserSearch] = useState('');
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [usersTotalCount, setUsersTotalCount] = useState(0);
  const [usersPage, setUsersPage] = useState(1);
  const [selectedUser, setSelectedUser] = useState<AdminUserDetail | null>(null);

  // Payments state
  const [payments, setPayments] = useState<AdminPayment[]>([]);
  const [paymentsTotalCount, setPaymentsTotalCount] = useState(0);
  const [paymentsTotalAmount, setPaymentsTotalAmount] = useState(0);
  const [paymentsPage, setPaymentsPage] = useState(1);

  const handleLogout = () => {
    localStorage.removeItem('auth_token');
    window.location.href = '/login';
  };

  // Load stats on mount
  useEffect(() => {
    loadStats();
  }, []);

  // Load data when tab changes
  useEffect(() => {
    if (activeTab === 'sites') loadSites();
    if (activeTab === 'payments') loadPayments();
  }, [activeTab]);

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

  const loadUsers = async (search?: string, page = 1) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.searchUsers(search, page);
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
    try {
      const data = await adminApi.getUser(id);
      setSelectedUser(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load user');
    } finally {
      setIsLoading(false);
    }
  };

  const loadPayments = async (page = 1) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getPayments({ page });
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
    loadUsers(userSearch);
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

  const handleUpdateUser = async (id: number, updates: Partial<AdminUser>) => {
    try {
      await adminApi.updateUser(id, updates);
      if (selectedUser) {
        loadUserDetail(id);
      }
      loadUsers(userSearch, usersPage);
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
                  <div key={site.key} className="p-4 flex items-center justify-between hover:bg-gray-50">
                    <div className="flex-1">
                      <h3 className="font-medium text-gray-900">{site.name}</h3>
                      <p className="text-sm text-gray-500">{site.key} • {site.url}</p>
                      {site.description && (
                        <p className="text-sm text-gray-400 mt-1">{site.description}</p>
                      )}
                    </div>
                    <div className="flex items-center gap-4">
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
                      <button
                        onClick={() => setEditingSite(site)}
                        className="text-gray-400 hover:text-gray-600"
                      >
                        <Edit2 className="w-5 h-5" />
                      </button>
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
                      onClick={() => loadUsers(userSearch, usersPage - 1)}
                      disabled={usersPage <= 1}
                      className="px-3 py-1 border rounded disabled:opacity-50"
                    >
                      Previous
                    </button>
                    <button
                      onClick={() => loadUsers(userSearch, usersPage + 1)}
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
                    <button onClick={() => setSelectedUser(null)} className="text-gray-400 hover:text-gray-600">
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

                    {selectedUser.sites.length > 0 && (
                      <div>
                        <h4 className="font-medium text-gray-900 mb-2">Sites</h4>
                        <div className="space-y-2">
                          {selectedUser.sites.map((site) => (
                            <div key={site.siteKey} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                              <span>{site.siteKey}</span>
                              <span className="text-sm text-gray-500">{site.role}</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

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

                    {selectedUser.recentPayments.length > 0 && (
                      <div>
                        <h4 className="font-medium text-gray-900 mb-2">Recent Payments</h4>
                        <div className="space-y-2">
                          {selectedUser.recentPayments.map((payment) => (
                            <div key={payment.id} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                              <div>
                                <span className="font-medium">{formatCurrency(payment.amountCents)}</span>
                                <span className="text-sm text-gray-500 ml-2">{formatDate(payment.createdAt)}</span>
                              </div>
                              <span className={`px-2 py-1 text-xs rounded-full ${
                                payment.status === 'succeeded' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'
                              }`}>
                                {payment.status}
                              </span>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

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
                    onClick={() => loadPayments(paymentsPage - 1)}
                    disabled={paymentsPage <= 1}
                    className="px-3 py-1 border rounded disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <button
                    onClick={() => loadPayments(paymentsPage + 1)}
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
      </div>
    </div>
  );
}
