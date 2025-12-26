import { useState, useEffect } from 'react';
import { Mail, FileText, ListTodo, Inbox, History, Plus, Edit2, Trash2, RefreshCw, X, Loader2, AlertCircle } from 'lucide-react';
import { notificationApi } from '../utils/api';
import type {
  MailProfile,
  NotificationTemplate,
  NotificationTask,
  NotificationOutbox,
  NotificationHistory,
  NotificationStats,
} from '../utils/api';

type SubTab = 'profiles' | 'templates' | 'tasks' | 'outbox' | 'history';

export function NotificationsTab() {
  const [subTab, setSubTab] = useState<SubTab>('profiles');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [stats, setStats] = useState<NotificationStats | null>(null);

  // Profiles state
  const [profiles, setProfiles] = useState<MailProfile[]>([]);
  const [editingProfile, setEditingProfile] = useState<Partial<MailProfile> | null>(null);

  // Templates state
  const [templates, setTemplates] = useState<NotificationTemplate[]>([]);
  const [editingTemplate, setEditingTemplate] = useState<Partial<NotificationTemplate> | null>(null);

  // Tasks state
  const [tasks, setTasks] = useState<NotificationTask[]>([]);
  const [editingTask, setEditingTask] = useState<Partial<NotificationTask> | null>(null);

  // Outbox state
  const [outboxItems, setOutboxItems] = useState<NotificationOutbox[]>([]);
  const [outboxPage, setOutboxPage] = useState(1);
  const [outboxTotal, setOutboxTotal] = useState(0);

  // History state
  const [historyItems, setHistoryItems] = useState<NotificationHistory[]>([]);
  const [historyPage, setHistoryPage] = useState(1);
  const [historyTotal, setHistoryTotal] = useState(0);

  useEffect(() => {
    loadStats();
  }, []);

  useEffect(() => {
    if (subTab === 'profiles') loadProfiles();
    if (subTab === 'templates') loadTemplates();
    if (subTab === 'tasks') loadTasks();
    if (subTab === 'outbox') loadOutbox();
    if (subTab === 'history') loadHistory();
  }, [subTab]);

  const loadStats = async () => {
    try {
      const data = await notificationApi.getStats();
      setStats(data);
    } catch (err) {
      console.error('Failed to load notification stats:', err);
    }
  };

  const loadProfiles = async () => {
    setIsLoading(true);
    try {
      const data = await notificationApi.getProfiles();
      setProfiles(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load profiles');
    } finally {
      setIsLoading(false);
    }
  };

  const loadTemplates = async () => {
    setIsLoading(true);
    try {
      const data = await notificationApi.getTemplates();
      setTemplates(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load templates');
    } finally {
      setIsLoading(false);
    }
  };

  const loadTasks = async () => {
    setIsLoading(true);
    try {
      const data = await notificationApi.getTasks();
      setTasks(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load tasks');
    } finally {
      setIsLoading(false);
    }
  };

  const loadOutbox = async (page = 1) => {
    setIsLoading(true);
    try {
      const data = await notificationApi.getOutbox({ page });
      setOutboxItems(data.items);
      setOutboxTotal(data.totalCount);
      setOutboxPage(page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load outbox');
    } finally {
      setIsLoading(false);
    }
  };

  const loadHistory = async (page = 1) => {
    setIsLoading(true);
    try {
      const data = await notificationApi.getHistory({ page });
      setHistoryItems(data.items);
      setHistoryTotal(data.totalCount);
      setHistoryPage(page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load history');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSaveProfile = async () => {
    if (!editingProfile) return;
    setIsLoading(true);
    try {
      if (editingProfile.id) {
        await notificationApi.updateProfile(editingProfile.id, editingProfile);
      } else {
        await notificationApi.createProfile(editingProfile as Omit<MailProfile, 'id' | 'createdAt' | 'updatedAt'>);
      }
      setEditingProfile(null);
      loadProfiles();
      loadStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save profile');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteProfile = async (id: number) => {
    if (!confirm('Delete this mail profile?')) return;
    try {
      await notificationApi.deleteProfile(id);
      loadProfiles();
      loadStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete profile');
    }
  };

  const handleSaveTemplate = async () => {
    if (!editingTemplate) return;
    setIsLoading(true);
    try {
      if (editingTemplate.id) {
        await notificationApi.updateTemplate(editingTemplate.id, editingTemplate);
      } else {
        await notificationApi.createTemplate(editingTemplate as Omit<NotificationTemplate, 'id' | 'createdAt' | 'updatedAt'>);
      }
      setEditingTemplate(null);
      loadTemplates();
      loadStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save template');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteTemplate = async (id: number) => {
    if (!confirm('Delete this template?')) return;
    try {
      await notificationApi.deleteTemplate(id);
      loadTemplates();
      loadStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete template');
    }
  };

  const handleSaveTask = async () => {
    if (!editingTask) return;
    setIsLoading(true);
    try {
      if (editingTask.id) {
        await notificationApi.updateTask(editingTask.id, editingTask);
      } else {
        await notificationApi.createTask(editingTask as Omit<NotificationTask, 'id' | 'createdAt' | 'mailProfileName' | 'templateCode'>);
      }
      setEditingTask(null);
      loadTasks();
      loadStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save task');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteTask = async (id: number) => {
    if (!confirm('Delete this task?')) return;
    try {
      await notificationApi.deleteTask(id);
      loadTasks();
      loadStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete task');
    }
  };

  const handleRetryOutbox = async (id: number) => {
    try {
      await notificationApi.retryOutbox(id);
      loadOutbox(outboxPage);
      loadStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retry');
    }
  };

  const handleDeleteOutbox = async (id: number) => {
    if (!confirm('Delete this message?')) return;
    try {
      await notificationApi.deleteOutbox(id);
      loadOutbox(outboxPage);
      loadStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete');
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <div className="space-y-6">
      {/* Stats Cards */}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white rounded-lg p-4 border border-gray-200">
            <p className="text-sm text-gray-500">Mail Profiles</p>
            <p className="text-2xl font-bold">{stats.activeProfiles}/{stats.totalProfiles}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border border-gray-200">
            <p className="text-sm text-gray-500">Templates</p>
            <p className="text-2xl font-bold">{stats.totalTemplates}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border border-gray-200">
            <p className="text-sm text-gray-500">Active Tasks</p>
            <p className="text-2xl font-bold">{stats.activeTasks}/{stats.totalTasks}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border border-gray-200">
            <p className="text-sm text-gray-500">Pending / Failed</p>
            <p className="text-2xl font-bold">
              {stats.pendingMessages}
              {stats.failedMessages > 0 && (
                <span className="text-red-500 ml-1">/ {stats.failedMessages}</span>
              )}
            </p>
          </div>
        </div>
      )}

      {/* Error Message */}
      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertCircle className="w-5 h-5" />
            {error}
          </div>
          <button onClick={() => setError(null)}>
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Sub-tabs */}
      <div className="flex gap-2 flex-wrap">
        {[
          { key: 'profiles', label: 'Mail Profiles', icon: Mail },
          { key: 'templates', label: 'Templates', icon: FileText },
          { key: 'tasks', label: 'Tasks', icon: ListTodo },
          { key: 'outbox', label: 'Outbox', icon: Inbox },
          { key: 'history', label: 'History', icon: History },
        ].map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            onClick={() => setSubTab(key as SubTab)}
            className={`flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
              subTab === key
                ? 'bg-blue-100 text-blue-700'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            <Icon className="w-4 h-4" />
            {label}
          </button>
        ))}
      </div>

      {/* Content */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200">
        {isLoading && (
          <div className="p-8 text-center">
            <Loader2 className="w-8 h-8 animate-spin mx-auto text-gray-400" />
          </div>
        )}

        {/* Profiles Tab */}
        {!isLoading && subTab === 'profiles' && (
          <>
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
              <h3 className="font-semibold">Mail Profiles</h3>
              <button
                onClick={() => setEditingProfile({ smtpPort: 587, securityMode: 'StartTlsWhenAvailable', isActive: true })}
                className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"
              >
                <Plus className="w-4 h-4" /> Add Profile
              </button>
            </div>
            <div className="divide-y divide-gray-200">
              {profiles.map((profile) => (
                <div key={profile.id} className="p-4 flex items-center justify-between">
                  <div>
                    <p className="font-medium">{profile.name}</p>
                    <p className="text-sm text-gray-500">
                      {profile.smtpHost}:{profile.smtpPort} ({profile.securityMode})
                    </p>
                    <p className="text-sm text-gray-400">{profile.fromEmail}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-1 text-xs rounded-full ${
                      profile.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'
                    }`}>
                      {profile.isActive ? 'Active' : 'Inactive'}
                    </span>
                    <button onClick={() => setEditingProfile(profile)} className="p-1 hover:bg-gray-100 rounded">
                      <Edit2 className="w-4 h-4 text-gray-500" />
                    </button>
                    <button onClick={() => handleDeleteProfile(profile.id)} className="p-1 hover:bg-gray-100 rounded">
                      <Trash2 className="w-4 h-4 text-red-500" />
                    </button>
                  </div>
                </div>
              ))}
              {profiles.length === 0 && (
                <div className="p-8 text-center text-gray-500">No mail profiles configured</div>
              )}
            </div>
          </>
        )}

        {/* Templates Tab */}
        {!isLoading && subTab === 'templates' && (
          <>
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
              <h3 className="font-semibold">Notification Templates</h3>
              <button
                onClick={() => setEditingTemplate({ type: 'Email', language: 'en', isActive: true, body: '' })}
                className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"
              >
                <Plus className="w-4 h-4" /> Add Template
              </button>
            </div>
            <div className="divide-y divide-gray-200">
              {templates.map((template) => (
                <div key={template.id} className="p-4 flex items-center justify-between">
                  <div>
                    <p className="font-medium">{template.name}</p>
                    <p className="text-sm text-gray-500">
                      {template.code} ({template.type}, {template.language})
                    </p>
                    {template.subject && (
                      <p className="text-sm text-gray-400 truncate max-w-md">{template.subject}</p>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-1 text-xs rounded-full ${
                      template.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'
                    }`}>
                      {template.isActive ? 'Active' : 'Inactive'}
                    </span>
                    <button onClick={() => setEditingTemplate(template)} className="p-1 hover:bg-gray-100 rounded">
                      <Edit2 className="w-4 h-4 text-gray-500" />
                    </button>
                    <button onClick={() => handleDeleteTemplate(template.id)} className="p-1 hover:bg-gray-100 rounded">
                      <Trash2 className="w-4 h-4 text-red-500" />
                    </button>
                  </div>
                </div>
              ))}
              {templates.length === 0 && (
                <div className="p-8 text-center text-gray-500">No templates configured</div>
              )}
            </div>
          </>
        )}

        {/* Tasks Tab */}
        {!isLoading && subTab === 'tasks' && (
          <>
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
              <h3 className="font-semibold">Notification Tasks</h3>
              <button
                onClick={() => setEditingTask({ type: 'Email', status: 'Active', priority: 'Normal', maxRetries: 3 })}
                className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"
              >
                <Plus className="w-4 h-4" /> Add Task
              </button>
            </div>
            <div className="divide-y divide-gray-200">
              {tasks.map((task) => (
                <div key={task.id} className="p-4 flex items-center justify-between">
                  <div>
                    <p className="font-medium">{task.name}</p>
                    <p className="text-sm text-gray-500">
                      {task.code} ({task.type})
                      {task.templateCode && <span className="ml-2">Template: {task.templateCode}</span>}
                    </p>
                    {task.mailProfileName && (
                      <p className="text-sm text-gray-400">Profile: {task.mailProfileName}</p>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-1 text-xs rounded-full ${
                      task.status === 'Active' ? 'bg-green-100 text-green-700' :
                      task.status === 'Testing' ? 'bg-yellow-100 text-yellow-700' :
                      'bg-gray-100 text-gray-600'
                    }`}>
                      {task.status}
                    </span>
                    <button onClick={() => setEditingTask(task)} className="p-1 hover:bg-gray-100 rounded">
                      <Edit2 className="w-4 h-4 text-gray-500" />
                    </button>
                    <button onClick={() => handleDeleteTask(task.id)} className="p-1 hover:bg-gray-100 rounded">
                      <Trash2 className="w-4 h-4 text-red-500" />
                    </button>
                  </div>
                </div>
              ))}
              {tasks.length === 0 && (
                <div className="p-8 text-center text-gray-500">No tasks configured</div>
              )}
            </div>
          </>
        )}

        {/* Outbox Tab */}
        {!isLoading && subTab === 'outbox' && (
          <>
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
              <h3 className="font-semibold">Pending Messages ({outboxTotal})</h3>
              <button
                onClick={() => loadOutbox(outboxPage)}
                className="flex items-center gap-2 px-3 py-1.5 text-gray-600 hover:bg-gray-100 rounded-lg text-sm"
              >
                <RefreshCw className="w-4 h-4" /> Refresh
              </button>
            </div>
            <div className="divide-y divide-gray-200">
              {outboxItems.map((item) => (
                <div key={item.id} className="p-4 flex items-center justify-between">
                  <div>
                    <p className="font-medium">{item.toList}</p>
                    <p className="text-sm text-gray-500">
                      {item.subject || `(${item.type})`} - Attempts: {item.attempts}/{item.maxAttempts}
                    </p>
                    {item.lastError && (
                      <p className="text-sm text-red-500 truncate max-w-md">{item.lastError}</p>
                    )}
                    <p className="text-xs text-gray-400">{formatDate(item.createdAt)}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-1 text-xs rounded-full ${
                      item.status === 'Pending' ? 'bg-blue-100 text-blue-700' :
                      item.status === 'Processing' ? 'bg-yellow-100 text-yellow-700' :
                      item.status === 'Failed' ? 'bg-red-100 text-red-700' :
                      'bg-gray-100 text-gray-600'
                    }`}>
                      {item.status}
                    </span>
                    <button onClick={() => handleRetryOutbox(item.id)} className="p-1 hover:bg-gray-100 rounded" title="Retry">
                      <RefreshCw className="w-4 h-4 text-blue-500" />
                    </button>
                    <button onClick={() => handleDeleteOutbox(item.id)} className="p-1 hover:bg-gray-100 rounded" title="Delete">
                      <Trash2 className="w-4 h-4 text-red-500" />
                    </button>
                  </div>
                </div>
              ))}
              {outboxItems.length === 0 && (
                <div className="p-8 text-center text-gray-500">No pending messages</div>
              )}
            </div>
            {outboxTotal > 20 && (
              <div className="p-4 border-t flex justify-between">
                <button
                  onClick={() => loadOutbox(outboxPage - 1)}
                  disabled={outboxPage <= 1}
                  className="px-3 py-1 border rounded disabled:opacity-50"
                >
                  Previous
                </button>
                <span className="text-sm text-gray-500">Page {outboxPage}</span>
                <button
                  onClick={() => loadOutbox(outboxPage + 1)}
                  disabled={outboxItems.length < 20}
                  className="px-3 py-1 border rounded disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            )}
          </>
        )}

        {/* History Tab */}
        {!isLoading && subTab === 'history' && (
          <>
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
              <h3 className="font-semibold">Sent Messages ({historyTotal})</h3>
              <button
                onClick={() => loadHistory(historyPage)}
                className="flex items-center gap-2 px-3 py-1.5 text-gray-600 hover:bg-gray-100 rounded-lg text-sm"
              >
                <RefreshCw className="w-4 h-4" /> Refresh
              </button>
            </div>
            <div className="divide-y divide-gray-200">
              {historyItems.map((item) => (
                <div key={item.id} className="p-4 flex items-center justify-between">
                  <div>
                    <p className="font-medium">{item.toList}</p>
                    <p className="text-sm text-gray-500">{item.subject || `(${item.type})`}</p>
                    <p className="text-xs text-gray-400">Sent: {formatDate(item.sentAt)}</p>
                  </div>
                  <span className={`px-2 py-1 text-xs rounded-full ${
                    item.status === 'Sent' ? 'bg-green-100 text-green-700' :
                    item.status === 'Delivered' ? 'bg-blue-100 text-blue-700' :
                    item.status === 'Bounced' ? 'bg-yellow-100 text-yellow-700' :
                    'bg-red-100 text-red-700'
                  }`}>
                    {item.status}
                  </span>
                </div>
              ))}
              {historyItems.length === 0 && (
                <div className="p-8 text-center text-gray-500">No sent messages</div>
              )}
            </div>
            {historyTotal > 20 && (
              <div className="p-4 border-t flex justify-between">
                <button
                  onClick={() => loadHistory(historyPage - 1)}
                  disabled={historyPage <= 1}
                  className="px-3 py-1 border rounded disabled:opacity-50"
                >
                  Previous
                </button>
                <span className="text-sm text-gray-500">Page {historyPage}</span>
                <button
                  onClick={() => loadHistory(historyPage + 1)}
                  disabled={historyItems.length < 20}
                  className="px-3 py-1 border rounded disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            )}
          </>
        )}
      </div>

      {/* Profile Edit Modal */}
      {editingProfile && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-lg w-full m-4 max-h-[90vh] overflow-y-auto">
            <div className="p-4 border-b flex items-center justify-between">
              <h3 className="font-semibold">{editingProfile.id ? 'Edit' : 'Add'} Mail Profile</h3>
              <button onClick={() => setEditingProfile(null)}><X className="w-5 h-5" /></button>
            </div>
            <div className="p-4 space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">Name</label>
                <input
                  type="text"
                  value={editingProfile.name || ''}
                  onChange={(e) => setEditingProfile({ ...editingProfile, name: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                  placeholder="Primary SMTP"
                />
              </div>
              <div className="grid grid-cols-3 gap-4">
                <div className="col-span-2">
                  <label className="block text-sm font-medium mb-1">SMTP Host</label>
                  <input
                    type="text"
                    value={editingProfile.smtpHost || ''}
                    onChange={(e) => setEditingProfile({ ...editingProfile, smtpHost: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    placeholder="smtp.example.com"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Port</label>
                  <input
                    type="number"
                    value={editingProfile.smtpPort || 587}
                    onChange={(e) => setEditingProfile({ ...editingProfile, smtpPort: parseInt(e.target.value) })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Username</label>
                  <input
                    type="text"
                    value={editingProfile.username || ''}
                    onChange={(e) => setEditingProfile({ ...editingProfile, username: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Password</label>
                  <input
                    type="password"
                    value={editingProfile.password || ''}
                    onChange={(e) => setEditingProfile({ ...editingProfile, password: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    placeholder={editingProfile.id ? '(unchanged)' : ''}
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">From Email</label>
                  <input
                    type="email"
                    value={editingProfile.fromEmail || ''}
                    onChange={(e) => setEditingProfile({ ...editingProfile, fromEmail: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">From Name</label>
                  <input
                    type="text"
                    value={editingProfile.fromName || ''}
                    onChange={(e) => setEditingProfile({ ...editingProfile, fromName: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Security Mode</label>
                <select
                  value={editingProfile.securityMode || 'StartTlsWhenAvailable'}
                  onChange={(e) => setEditingProfile({ ...editingProfile, securityMode: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                >
                  <option value="None">None</option>
                  <option value="SslOnConnect">SSL on Connect</option>
                  <option value="StartTls">StartTLS (required)</option>
                  <option value="StartTlsWhenAvailable">StartTLS (when available)</option>
                </select>
              </div>
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={editingProfile.isActive ?? true}
                  onChange={(e) => setEditingProfile({ ...editingProfile, isActive: e.target.checked })}
                />
                <span className="text-sm">Active</span>
              </label>
            </div>
            <div className="p-4 border-t flex justify-end gap-2">
              <button onClick={() => setEditingProfile(null)} className="px-4 py-2 border rounded-lg">Cancel</button>
              <button onClick={handleSaveProfile} className="px-4 py-2 bg-blue-600 text-white rounded-lg">Save</button>
            </div>
          </div>
        </div>
      )}

      {/* Template Edit Modal */}
      {editingTemplate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-2xl w-full m-4 max-h-[90vh] overflow-y-auto">
            <div className="p-4 border-b flex items-center justify-between">
              <h3 className="font-semibold">{editingTemplate.id ? 'Edit' : 'Add'} Template</h3>
              <button onClick={() => setEditingTemplate(null)}><X className="w-5 h-5" /></button>
            </div>
            <div className="p-4 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Code</label>
                  <input
                    type="text"
                    value={editingTemplate.code || ''}
                    onChange={(e) => setEditingTemplate({ ...editingTemplate, code: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    placeholder="welcome_email"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Name</label>
                  <input
                    type="text"
                    value={editingTemplate.name || ''}
                    onChange={(e) => setEditingTemplate({ ...editingTemplate, name: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
              </div>
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Type</label>
                  <select
                    value={editingTemplate.type || 'Email'}
                    onChange={(e) => setEditingTemplate({ ...editingTemplate, type: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    <option value="Email">Email</option>
                    <option value="SMS">SMS</option>
                    <option value="Push">Push</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Language</label>
                  <input
                    type="text"
                    value={editingTemplate.language || 'en'}
                    onChange={(e) => setEditingTemplate({ ...editingTemplate, language: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Site</label>
                  <input
                    type="text"
                    value={editingTemplate.siteKey || ''}
                    onChange={(e) => setEditingTemplate({ ...editingTemplate, siteKey: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    placeholder="(all sites)"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Subject</label>
                <input
                  type="text"
                  value={editingTemplate.subject || ''}
                  onChange={(e) => setEditingTemplate({ ...editingTemplate, subject: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Body (HTML)</label>
                <textarea
                  value={editingTemplate.body || ''}
                  onChange={(e) => setEditingTemplate({ ...editingTemplate, body: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg font-mono text-sm"
                  rows={10}
                />
              </div>
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={editingTemplate.isActive ?? true}
                  onChange={(e) => setEditingTemplate({ ...editingTemplate, isActive: e.target.checked })}
                />
                <span className="text-sm">Active</span>
              </label>
            </div>
            <div className="p-4 border-t flex justify-end gap-2">
              <button onClick={() => setEditingTemplate(null)} className="px-4 py-2 border rounded-lg">Cancel</button>
              <button onClick={handleSaveTemplate} className="px-4 py-2 bg-blue-600 text-white rounded-lg">Save</button>
            </div>
          </div>
        </div>
      )}

      {/* Task Edit Modal */}
      {editingTask && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-lg w-full m-4 max-h-[90vh] overflow-y-auto">
            <div className="p-4 border-b flex items-center justify-between">
              <h3 className="font-semibold">{editingTask.id ? 'Edit' : 'Add'} Task</h3>
              <button onClick={() => setEditingTask(null)}><X className="w-5 h-5" /></button>
            </div>
            <div className="p-4 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Code</label>
                  <input
                    type="text"
                    value={editingTask.code || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, code: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Name</label>
                  <input
                    type="text"
                    value={editingTask.name || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, name: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
              </div>
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Type</label>
                  <select
                    value={editingTask.type || 'Email'}
                    onChange={(e) => setEditingTask({ ...editingTask, type: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    <option value="Email">Email</option>
                    <option value="SMS">SMS</option>
                    <option value="Push">Push</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Status</label>
                  <select
                    value={editingTask.status || 'Active'}
                    onChange={(e) => setEditingTask({ ...editingTask, status: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    <option value="Active">Active</option>
                    <option value="Testing">Testing</option>
                    <option value="Inactive">Inactive</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Priority</label>
                  <select
                    value={editingTask.priority || 'Normal'}
                    onChange={(e) => setEditingTask({ ...editingTask, priority: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    <option value="Low">Low</option>
                    <option value="Normal">Normal</option>
                    <option value="High">High</option>
                    <option value="Critical">Critical</option>
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Mail Profile</label>
                  <select
                    value={editingTask.mailProfileId || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, mailProfileId: e.target.value ? parseInt(e.target.value) : undefined })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    <option value="">Select...</option>
                    {profiles.map((p) => (
                      <option key={p.id} value={p.id}>{p.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Template</label>
                  <select
                    value={editingTask.templateId || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, templateId: e.target.value ? parseInt(e.target.value) : undefined })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    <option value="">Select...</option>
                    {templates.map((t) => (
                      <option key={t.id} value={t.id}>{t.code} - {t.name}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Default Recipients</label>
                <input
                  type="text"
                  value={editingTask.defaultRecipients || ''}
                  onChange={(e) => setEditingTask({ ...editingTask, defaultRecipients: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                  placeholder="Comma-separated emails"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Test Email (used when Status = Testing)</label>
                <input
                  type="email"
                  value={editingTask.testEmail || ''}
                  onChange={(e) => setEditingTask({ ...editingTask, testEmail: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                />
              </div>
            </div>
            <div className="p-4 border-t flex justify-end gap-2">
              <button onClick={() => setEditingTask(null)} className="px-4 py-2 border rounded-lg">Cancel</button>
              <button onClick={handleSaveTask} className="px-4 py-2 bg-blue-600 text-white rounded-lg">Save</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
