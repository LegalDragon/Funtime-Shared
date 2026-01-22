import { useState, useEffect } from 'react';
import { Plus, Edit2, Trash2, X, Loader2, Check, FileType, ToggleLeft, ToggleRight, AlertCircle } from 'lucide-react';
import { fileTypesApi, type AssetFileType } from '../utils/api';

const CATEGORIES = [
  { value: 'image', label: 'Image' },
  { value: 'video', label: 'Video' },
  { value: 'audio', label: 'Audio' },
  { value: 'document', label: 'Document' },
];

export function FileTypesTab() {
  const [fileTypes, setFileTypes] = useState<AssetFileType[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingType, setEditingType] = useState<AssetFileType | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [saving, setSaving] = useState(false);
  const [togglingId, setTogglingId] = useState<number | null>(null);

  // Form state
  const [formData, setFormData] = useState({
    mimeType: '',
    extensions: '',
    category: 'image',
    maxSizeMB: 10,
    isEnabled: true,
    displayName: '',
  });

  useEffect(() => {
    loadFileTypes();
  }, []);

  const loadFileTypes = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await fileTypesApi.getAll();
      setFileTypes(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load file types');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setFormData({
      mimeType: '',
      extensions: '',
      category: 'image',
      maxSizeMB: 10,
      isEnabled: true,
      displayName: '',
    });
    setIsCreating(true);
    setEditingType(null);
  };

  const handleEdit = (fileType: AssetFileType) => {
    setFormData({
      mimeType: fileType.mimeType,
      extensions: fileType.extensions,
      category: fileType.category,
      maxSizeMB: fileType.maxSizeMB,
      isEnabled: fileType.isEnabled,
      displayName: fileType.displayName || '',
    });
    setEditingType(fileType);
    setIsCreating(false);
  };

  const handleSave = async () => {
    if (!formData.mimeType || !formData.extensions || !formData.category) {
      setError('Please fill in all required fields');
      return;
    }

    setSaving(true);
    setError(null);
    try {
      if (editingType) {
        await fileTypesApi.update(editingType.id, {
          mimeType: formData.mimeType,
          extensions: formData.extensions,
          category: formData.category,
          maxSizeMB: formData.maxSizeMB,
          isEnabled: formData.isEnabled,
          displayName: formData.displayName || undefined,
        });
      } else {
        await fileTypesApi.create({
          mimeType: formData.mimeType,
          extensions: formData.extensions,
          category: formData.category,
          maxSizeMB: formData.maxSizeMB,
          isEnabled: formData.isEnabled,
          displayName: formData.displayName || undefined,
        });
      }
      setEditingType(null);
      setIsCreating(false);
      loadFileTypes();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save file type');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure you want to delete this file type?')) return;

    try {
      await fileTypesApi.delete(id);
      loadFileTypes();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete file type');
    }
  };

  const handleToggle = async (id: number) => {
    setTogglingId(id);
    try {
      const result = await fileTypesApi.toggle(id);
      setFileTypes(prev =>
        prev.map(ft => (ft.id === id ? { ...ft, isEnabled: result.isEnabled } : ft))
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to toggle file type');
    } finally {
      setTogglingId(null);
    }
  };

  const handleCancel = () => {
    setEditingType(null);
    setIsCreating(false);
    setError(null);
  };

  // Group file types by category
  const groupedTypes = fileTypes.reduce((acc, ft) => {
    if (!acc[ft.category]) acc[ft.category] = [];
    acc[ft.category].push(ft);
    return acc;
  }, {} as Record<string, AssetFileType[]>);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
              <FileType className="w-5 h-5 text-primary-600" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900">Asset File Types</h2>
              <p className="text-sm text-gray-500">Configure allowed file types for asset uploads</p>
            </div>
          </div>
          <button
            onClick={handleCreate}
            className="flex items-center gap-2 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 transition-colors"
          >
            <Plus className="w-4 h-4" />
            Add File Type
          </button>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-center gap-2 text-red-700">
          <AlertCircle className="w-5 h-5 flex-shrink-0" />
          <span>{error}</span>
          <button onClick={() => setError(null)} className="ml-auto text-red-500 hover:text-red-700">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Create/Edit Form */}
      {(isCreating || editingType) && (
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold mb-4">
            {editingType ? 'Edit File Type' : 'Add New File Type'}
          </h3>
          <div className="grid md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                MIME Type <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.mimeType}
                onChange={(e) => setFormData({ ...formData, mimeType: e.target.value })}
                placeholder="e.g., image/jpeg"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Extensions <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.extensions}
                onChange={(e) => setFormData({ ...formData, extensions: e.target.value })}
                placeholder="e.g., .jpg,.jpeg"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
              <p className="text-xs text-gray-500 mt-1">Comma-separated, include the dot</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Category <span className="text-red-500">*</span>
              </label>
              <select
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                {CATEGORIES.map((cat) => (
                  <option key={cat.value} value={cat.value}>
                    {cat.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Max Size (MB)
              </label>
              <input
                type="number"
                min="1"
                max="500"
                value={formData.maxSizeMB}
                onChange={(e) => setFormData({ ...formData, maxSizeMB: parseInt(e.target.value) || 10 })}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Display Name
              </label>
              <input
                type="text"
                value={formData.displayName}
                onChange={(e) => setFormData({ ...formData, displayName: e.target.value })}
                placeholder="e.g., JPEG Image"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div className="flex items-center">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={formData.isEnabled}
                  onChange={(e) => setFormData({ ...formData, isEnabled: e.target.checked })}
                  className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                />
                <span className="text-sm font-medium text-gray-700">Enabled</span>
              </label>
            </div>
          </div>
          <div className="flex justify-end gap-2 mt-6">
            <button
              onClick={handleCancel}
              className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleSave}
              disabled={saving}
              className="flex items-center gap-2 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 transition-colors disabled:opacity-50"
            >
              {saving ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  Save
                </>
              )}
            </button>
          </div>
        </div>
      )}

      {/* File Types List */}
      {loading ? (
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-8 flex justify-center">
          <Loader2 className="w-8 h-8 animate-spin text-primary-500" />
        </div>
      ) : (
        <div className="space-y-4">
          {CATEGORIES.map((category) => {
            const types = groupedTypes[category.value] || [];
            if (types.length === 0) return null;

            return (
              <div key={category.value} className="bg-white rounded-xl shadow-sm border border-gray-200">
                <div className="p-4 border-b border-gray-200 bg-gray-50">
                  <h3 className="font-medium text-gray-900">{category.label}s</h3>
                  <p className="text-sm text-gray-500">{types.length} type{types.length !== 1 ? 's' : ''}</p>
                </div>
                <div className="divide-y divide-gray-100">
                  {types.map((fileType) => (
                    <div
                      key={fileType.id}
                      className={`p-4 flex items-center justify-between ${
                        !fileType.isEnabled ? 'bg-gray-50 opacity-60' : ''
                      }`}
                    >
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2">
                          <p className="font-medium text-gray-900">
                            {fileType.displayName || fileType.mimeType}
                          </p>
                          {!fileType.isEnabled && (
                            <span className="px-2 py-0.5 text-xs bg-gray-200 text-gray-600 rounded-full">
                              Disabled
                            </span>
                          )}
                        </div>
                        <p className="text-sm text-gray-500">
                          {fileType.mimeType} • {fileType.extensions} • Max {fileType.maxSizeMB}MB
                        </p>
                      </div>
                      <div className="flex items-center gap-2 ml-4">
                        <button
                          onClick={() => handleToggle(fileType.id)}
                          disabled={togglingId === fileType.id}
                          className={`p-2 rounded-lg transition-colors ${
                            fileType.isEnabled
                              ? 'text-green-600 hover:bg-green-50'
                              : 'text-gray-400 hover:bg-gray-100'
                          }`}
                          title={fileType.isEnabled ? 'Disable' : 'Enable'}
                        >
                          {togglingId === fileType.id ? (
                            <Loader2 className="w-5 h-5 animate-spin" />
                          ) : fileType.isEnabled ? (
                            <ToggleRight className="w-5 h-5" />
                          ) : (
                            <ToggleLeft className="w-5 h-5" />
                          )}
                        </button>
                        <button
                          onClick={() => handleEdit(fileType)}
                          className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
                          title="Edit"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleDelete(fileType.id)}
                          className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                          title="Delete"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}

          {fileTypes.length === 0 && (
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-8 text-center text-gray-500">
              <FileType className="w-12 h-12 mx-auto mb-4 text-gray-300" />
              <p>No file types configured yet</p>
              <p className="text-sm mt-1">Click "Add File Type" to create your first allowed file type</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
