import { useState, useEffect } from 'react';
import { Key, Plus, Trash2, RefreshCw, Copy, Check, ToggleLeft, ToggleRight, Edit2, X, Loader2 } from 'lucide-react';
import { apiKeysApi } from '../utils/api';
import type { ApiKeyResponse, ApiKeyCreatedResponse, ApiScopeInfo, CreateApiKeyRequest, UpdateApiKeyRequest } from '../utils/api';

export function ApiKeysTab() {
  const [apiKeys, setApiKeys] = useState<ApiKeyResponse[]>([]);
  const [availableScopes, setAvailableScopes] = useState<ApiScopeInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Create modal state
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [creating, setCreating] = useState(false);
  const [newKey, setNewKey] = useState<ApiKeyCreatedResponse | null>(null);

  // Edit modal state
  const [editingKey, setEditingKey] = useState<ApiKeyResponse | null>(null);
  const [updating, setUpdating] = useState(false);

  // Form state
  const [formData, setFormData] = useState<CreateApiKeyRequest>({
    partnerKey: '',
    partnerName: '',
    scopes: [],
    rateLimitPerMinute: 60,
    description: '',
  });

  // Copied state for copy button feedback
  const [copiedId, setCopiedId] = useState<number | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [keys, scopesResponse] = await Promise.all([
        apiKeysApi.getAll(),
        apiKeysApi.getScopes(),
      ]);
      setApiKeys(keys);
      setAvailableScopes(scopesResponse.scopes);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load API keys');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    if (!formData.partnerKey || !formData.partnerName) {
      setError('Partner key and name are required');
      return;
    }

    try {
      setCreating(true);
      setError(null);
      const created = await apiKeysApi.create(formData);
      setNewKey(created);
      setApiKeys(prev => [created, ...prev]);
      // Don't close modal yet - show the new key
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create API key');
    } finally {
      setCreating(false);
    }
  };

  const handleUpdate = async () => {
    if (!editingKey) return;

    try {
      setUpdating(true);
      setError(null);
      const updateData: UpdateApiKeyRequest = {
        partnerName: formData.partnerName,
        scopes: formData.scopes,
        rateLimitPerMinute: formData.rateLimitPerMinute,
        description: formData.description,
      };
      const updated = await apiKeysApi.update(editingKey.id, updateData);
      setApiKeys(prev => prev.map(k => k.id === updated.id ? updated : k));
      setEditingKey(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update API key');
    } finally {
      setUpdating(false);
    }
  };

  const handleToggle = async (id: number) => {
    try {
      const updated = await apiKeysApi.toggle(id);
      setApiKeys(prev => prev.map(k => k.id === updated.id ? updated : k));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to toggle API key');
    }
  };

  const handleRegenerate = async (id: number) => {
    if (!confirm('Are you sure? The old key will stop working immediately.')) return;

    try {
      const regenerated = await apiKeysApi.regenerate(id);
      setNewKey(regenerated);
      setApiKeys(prev => prev.map(k => k.id === regenerated.id ? regenerated : k));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to regenerate API key');
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure you want to delete this API key? This cannot be undone.')) return;

    try {
      await apiKeysApi.delete(id);
      setApiKeys(prev => prev.filter(k => k.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete API key');
    }
  };

  const copyToClipboard = async (text: string, id: number) => {
    await navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const openCreateModal = () => {
    setFormData({
      partnerKey: '',
      partnerName: '',
      scopes: [],
      rateLimitPerMinute: 60,
      description: '',
    });
    setNewKey(null);
    setShowCreateModal(true);
  };

  const openEditModal = (key: ApiKeyResponse) => {
    setFormData({
      partnerKey: key.partnerKey,
      partnerName: key.partnerName,
      scopes: key.scopes,
      rateLimitPerMinute: key.rateLimitPerMinute,
      description: key.description || '',
    });
    setEditingKey(key);
  };

  const closeModals = () => {
    setShowCreateModal(false);
    setEditingKey(null);
    setNewKey(null);
  };

  const toggleScope = (scope: string) => {
    setFormData(prev => ({
      ...prev,
      scopes: prev.scopes.includes(scope)
        ? prev.scopes.filter(s => s !== scope)
        : [...prev.scopes, scope],
    }));
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return 'Never';
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  // Group scopes by category
  const scopesByCategory = availableScopes.reduce((acc, scope) => {
    if (!acc[scope.category]) acc[scope.category] = [];
    acc[scope.category].push(scope);
    return acc;
  }, {} as Record<string, ApiScopeInfo[]>);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-8 h-8 animate-spin text-primary-500" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-gray-900">API Keys</h2>
          <p className="text-sm text-gray-500">Manage API keys for partner applications</p>
        </div>
        <button
          onClick={openCreateModal}
          className="flex items-center gap-2 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600"
        >
          <Plus className="w-4 h-4" />
          Create API Key
        </button>
      </div>

      {/* Error */}
      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          {error}
        </div>
      )}

      {/* New Key Alert */}
      {newKey && (
        <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
          <div className="flex items-start justify-between">
            <div>
              <h3 className="font-medium text-green-800">API Key Created!</h3>
              <p className="text-sm text-green-700 mt-1">
                Copy this key now. You won't be able to see it again.
              </p>
              <div className="mt-3 flex items-center gap-2">
                <code className="px-3 py-2 bg-white border rounded font-mono text-sm">
                  {newKey.apiKey}
                </code>
                <button
                  onClick={() => copyToClipboard(newKey.apiKey, newKey.id)}
                  className="p-2 text-green-600 hover:bg-green-100 rounded"
                >
                  {copiedId === newKey.id ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                </button>
              </div>
            </div>
            <button onClick={() => setNewKey(null)} className="text-green-600 hover:text-green-800">
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>
      )}

      {/* API Keys List */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Partner</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Key</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Scopes</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Usage</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {apiKeys.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-6 py-12 text-center text-gray-500">
                  <Key className="w-12 h-12 mx-auto mb-4 text-gray-300" />
                  <p>No API keys yet</p>
                  <p className="text-sm">Create your first API key to get started</p>
                </td>
              </tr>
            ) : (
              apiKeys.map(key => (
                <tr key={key.id} className={!key.isActive ? 'bg-gray-50' : ''}>
                  <td className="px-6 py-4">
                    <div className="font-medium text-gray-900">{key.partnerName}</div>
                    <div className="text-sm text-gray-500">{key.partnerKey}</div>
                  </td>
                  <td className="px-6 py-4">
                    <code className="text-sm text-gray-600 bg-gray-100 px-2 py-1 rounded">
                      {key.keyMasked}
                    </code>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex flex-wrap gap-1">
                      {key.scopes.slice(0, 3).map(scope => (
                        <span key={scope} className="px-2 py-0.5 text-xs bg-blue-100 text-blue-700 rounded">
                          {scope}
                        </span>
                      ))}
                      {key.scopes.length > 3 && (
                        <span className="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded">
                          +{key.scopes.length - 3}
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="text-sm text-gray-900">{key.usageCount.toLocaleString()} calls</div>
                    <div className="text-xs text-gray-500">
                      Last: {formatDate(key.lastUsedAt)}
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <button
                      onClick={() => handleToggle(key.id)}
                      className={`flex items-center gap-1 px-2 py-1 rounded text-sm ${
                        key.isActive
                          ? 'bg-green-100 text-green-700'
                          : 'bg-gray-100 text-gray-500'
                      }`}
                    >
                      {key.isActive ? (
                        <>
                          <ToggleRight className="w-4 h-4" /> Active
                        </>
                      ) : (
                        <>
                          <ToggleLeft className="w-4 h-4" /> Inactive
                        </>
                      )}
                    </button>
                  </td>
                  <td className="px-6 py-4 text-right">
                    <div className="flex items-center justify-end gap-2">
                      <button
                        onClick={() => openEditModal(key)}
                        className="p-2 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded"
                        title="Edit"
                      >
                        <Edit2 className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => handleRegenerate(key.id)}
                        className="p-2 text-gray-400 hover:text-yellow-600 hover:bg-yellow-50 rounded"
                        title="Regenerate"
                      >
                        <RefreshCw className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => handleDelete(key.id)}
                        className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded"
                        title="Delete"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Create/Edit Modal */}
      {(showCreateModal || editingKey) && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-lg w-full mx-4 max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex items-center justify-between mb-6">
                <h3 className="text-lg font-semibold text-gray-900">
                  {editingKey ? 'Edit API Key' : 'Create API Key'}
                </h3>
                <button onClick={closeModals} className="text-gray-400 hover:text-gray-600">
                  <X className="w-5 h-5" />
                </button>
              </div>

              <div className="space-y-4">
                {/* Partner Key (only for create) */}
                {!editingKey && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Partner Key *
                    </label>
                    <input
                      type="text"
                      value={formData.partnerKey}
                      onChange={e => setFormData(prev => ({ ...prev, partnerKey: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '') }))}
                      placeholder="e.g., community, college, external-app"
                      className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                    />
                    <p className="text-xs text-gray-500 mt-1">Lowercase letters, numbers, and hyphens only</p>
                  </div>
                )}

                {/* Partner Name */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Partner Name *
                  </label>
                  <input
                    type="text"
                    value={formData.partnerName}
                    onChange={e => setFormData(prev => ({ ...prev, partnerName: e.target.value }))}
                    placeholder="e.g., Pickleball Community"
                    className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  />
                </div>

                {/* Scopes */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Permissions (Scopes)
                  </label>
                  <div className="space-y-3">
                    {Object.entries(scopesByCategory).map(([category, scopes]) => (
                      <div key={category}>
                        <div className="text-xs font-medium text-gray-500 mb-1">{category}</div>
                        <div className="flex flex-wrap gap-2">
                          {scopes.map(scope => (
                            <button
                              key={scope.name}
                              type="button"
                              onClick={() => toggleScope(scope.name)}
                              className={`px-3 py-1 text-sm rounded-full border transition-colors ${
                                formData.scopes.includes(scope.name)
                                  ? 'bg-primary-500 text-white border-primary-500'
                                  : 'bg-white text-gray-700 border-gray-300 hover:border-primary-300'
                              }`}
                              title={scope.description}
                            >
                              {scope.name}
                            </button>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Rate Limit */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Rate Limit (requests/minute)
                  </label>
                  <input
                    type="number"
                    value={formData.rateLimitPerMinute}
                    onChange={e => setFormData(prev => ({ ...prev, rateLimitPerMinute: parseInt(e.target.value) || 60 }))}
                    min={1}
                    max={10000}
                    className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  />
                </div>

                {/* Description */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Description
                  </label>
                  <textarea
                    value={formData.description}
                    onChange={e => setFormData(prev => ({ ...prev, description: e.target.value }))}
                    placeholder="Optional description for this API key"
                    rows={2}
                    className="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  />
                </div>
              </div>

              {/* Actions */}
              <div className="flex justify-end gap-3 mt-6 pt-4 border-t">
                <button
                  onClick={closeModals}
                  className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg"
                >
                  Cancel
                </button>
                <button
                  onClick={editingKey ? handleUpdate : handleCreate}
                  disabled={creating || updating}
                  className="flex items-center gap-2 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 disabled:opacity-50"
                >
                  {(creating || updating) && <Loader2 className="w-4 h-4 animate-spin" />}
                  {editingKey ? 'Save Changes' : 'Create API Key'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
