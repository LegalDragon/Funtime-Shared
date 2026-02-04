import { useState, useEffect } from 'react';
import { Mail, FileText, ListTodo, Inbox, History, Plus, Edit2, Trash2, RefreshCw, X, Loader2, AlertCircle, Building2, Eye, Key, Copy, Check, ToggleLeft, ToggleRight } from 'lucide-react';
import { notificationApi } from '../utils/api';
import type {
  MailProfile,
  AppRow,
  EmailTemplate,
  TaskRow,
  OutboxRow,
  HistoryRow,
  NotificationStats,
  LookupItem,
} from '../utils/api';

type SubTab = 'profiles' | 'apps' | 'templates' | 'tasks' | 'outbox' | 'history';

export function NotificationsTab() {
  const [subTab, setSubTab] = useState<SubTab>('profiles');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [stats, setStats] = useState<NotificationStats | null>(null);

  // Profiles state
  const [profiles, setProfiles] = useState<MailProfile[]>([]);
  const [editingProfile, setEditingProfile] = useState<Partial<MailProfile> | null>(null);

  // Apps state
  const [apps, setApps] = useState<AppRow[]>([]);
  const [editingApp, setEditingApp] = useState<Partial<AppRow> | null>(null);
  const [selectedAppId, setSelectedAppId] = useState<number | undefined>();

  // Templates state
  const [templates, setTemplates] = useState<EmailTemplate[]>([]);
  const [editingTemplate, setEditingTemplate] = useState<Partial<EmailTemplate> | null>(null);

  // Tasks state
  const [tasks, setTasks] = useState<TaskRow[]>([]);
  const [editingTask, setEditingTask] = useState<Partial<TaskRow> | null>(null);

  // Lookups
  const [securityModes, setSecurityModes] = useState<LookupItem[]>([]);
  const [taskStatuses, setTaskStatuses] = useState<LookupItem[]>([]);
  const [taskTypes, setTaskTypes] = useState<LookupItem[]>([]);

  // Outbox state
  const [outboxItems, setOutboxItems] = useState<OutboxRow[]>([]);
  const [outboxPage, setOutboxPage] = useState(1);
  const [outboxTotal, setOutboxTotal] = useState(0);

  // History state
  const [historyItems, setHistoryItems] = useState<HistoryRow[]>([]);
  const [historyPage, setHistoryPage] = useState(1);
  const [historyTotal, setHistoryTotal] = useState(0);

  // Body view modal state
  const [viewingBody, setViewingBody] = useState<{ title: string; bodyHtml?: string; bodyJson?: string; detailJson?: string; rawData?: string } | null>(null);

  // App-Profile association state
  const [linkedProfileIds, setLinkedProfileIds] = useState<number[]>([]);
  const [loadingLinkedProfiles, setLoadingLinkedProfiles] = useState(false);

  // API key management state
  const [revealedKey, setRevealedKey] = useState<{ appId: number; key: string } | null>(null);
  const [copiedKeyId, setCopiedKeyId] = useState<number | null>(null);

  useEffect(() => {
    loadStats();
    loadLookups();
  }, []);

  useEffect(() => {
    if (subTab === 'profiles') loadProfiles();
    if (subTab === 'apps') loadApps();
    if (subTab === 'templates') loadTemplates();
    if (subTab === 'tasks') loadTasks();
    if (subTab === 'outbox') loadOutbox();
    if (subTab === 'history') loadHistory();
  }, [subTab, selectedAppId]);

  const loadStats = async () => {
    try {
      const data = await notificationApi.getStats();
      setStats(data);
    } catch (err) {
      console.error('Failed to load notification stats:', err);
    }
  };

  const loadLookups = async () => {
    try {
      const [modes, statuses, types] = await Promise.all([
        notificationApi.getSecurityModes().catch(() => []),
        notificationApi.getTaskStatuses().catch(() => []),
        notificationApi.getTaskTypes().catch(() => []),
      ]);
      setSecurityModes(modes);
      setTaskStatuses(statuses);
      setTaskTypes(types);
    } catch (err) {
      console.error('Failed to load lookups:', err);
    }
  };

  const loadProfiles = async () => {
    setIsLoading(true);
    try {
      const data = await notificationApi.getProfiles();
      setProfiles(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load profiles');
    } finally {
      setIsLoading(false);
    }
  };

  const loadApps = async () => {
    setIsLoading(true);
    try {
      const data = await notificationApi.getApps();
      setApps(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load apps');
    } finally {
      setIsLoading(false);
    }
  };

  const loadTemplates = async () => {
    setIsLoading(true);
    try {
      const data = await notificationApi.getTemplates(selectedAppId);
      setTemplates(data);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load templates');
    } finally {
      setIsLoading(false);
    }
  };

  const loadTasks = async () => {
    setIsLoading(true);
    try {
      const data = await notificationApi.getTasks(selectedAppId);
      setTasks(data);
      setError(null);
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
      setError(null);
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
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load history');
    } finally {
      setIsLoading(false);
    }
  };

  // Profile handlers
  const handleSaveProfile = async () => {
    if (!editingProfile) return;
    setIsLoading(true);
    try {
      if (editingProfile.profileId) {
        await notificationApi.updateProfile(editingProfile.profileId, editingProfile);
      } else {
        await notificationApi.createProfile(editingProfile);
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

  // Load linked profiles when editing an app
  const loadAppProfiles = async (appId: number) => {
    setLoadingLinkedProfiles(true);
    try {
      const ids = await notificationApi.getAppProfiles(appId);
      setLinkedProfileIds(ids);
    } catch (err) {
      console.error('Failed to load app profiles:', err);
      setLinkedProfileIds([]);
    } finally {
      setLoadingLinkedProfiles(false);
    }
  };

  const openAppEditor = (app: Partial<AppRow>) => {
    setEditingApp(app);
    if (app.app_ID) {
      loadAppProfiles(app.app_ID);
    } else {
      setLinkedProfileIds([]);
    }
    // Ensure profiles are loaded for the checkboxes
    if (profiles.length === 0) {
      notificationApi.getProfiles().then(setProfiles).catch(() => {});
    }
  };

  // App handlers
  const handleSaveApp = async () => {
    if (!editingApp) return;
    setIsLoading(true);
    try {
      let appId: number;
      if (editingApp.app_ID) {
        await notificationApi.updateApp(editingApp.app_ID, editingApp);
        appId = editingApp.app_ID;
      } else {
        const created = await notificationApi.createApp(editingApp);
        appId = created.app_ID;
        // Show the generated API key
        if (created.fullKey) {
          setRevealedKey({ appId: created.app_ID, key: created.fullKey });
        }
      }
      // Save linked profiles
      await notificationApi.updateAppProfiles(appId, linkedProfileIds);
      setEditingApp(null);
      loadApps();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save app');
    } finally {
      setIsLoading(false);
    }
  };

  // Template handlers
  const handleSaveTemplate = async () => {
    if (!editingTemplate) return;
    setIsLoading(true);
    try {
      if (editingTemplate.eT_ID) {
        await notificationApi.updateTemplate(editingTemplate.eT_ID, editingTemplate);
      } else {
        await notificationApi.createTemplate(editingTemplate);
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

  // Task handlers
  const handleSaveTask = async () => {
    if (!editingTask) return;
    setIsLoading(true);
    try {
      if (editingTask.task_ID) {
        await notificationApi.updateTask(editingTask.task_ID, editingTask);
      } else {
        await notificationApi.createTask(editingTask);
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

  const handleRetryHistory = async (id: number) => {
    try {
      await notificationApi.retryHistory(id);
      loadHistory(historyPage);
      loadOutbox(outboxPage);
      loadStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retry');
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

  // App API key handlers
  const handleToggleApp = async (id: number) => {
    try {
      const updated = await notificationApi.toggleApp(id);
      setApps(prev => prev.map(a => a.app_ID === id ? updated : a));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to toggle app');
    }
  };

  const handleRegenerateKey = async (id: number) => {
    if (!confirm('Regenerate API key? The old key will stop working immediately.')) return;
    try {
      const updated = await notificationApi.regenerateAppKey(id);
      setApps(prev => prev.map(a => a.app_ID === id ? updated : a));
      if (updated.fullKey) {
        setRevealedKey({ appId: id, key: updated.fullKey });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to regenerate key');
    }
  };

  const handleRevokeKey = async (id: number) => {
    if (!confirm('Revoke this API key? The app will lose API access.')) return;
    try {
      await notificationApi.revokeAppKey(id);
      setApps(prev => prev.map(a => a.app_ID === id ? { ...a, maskedKey: undefined } : a));
      if (revealedKey?.appId === id) setRevealedKey(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to revoke key');
    }
  };

  const copyKey = async (key: string, appId: number) => {
    await navigator.clipboard.writeText(key);
    setCopiedKeyId(appId);
    setTimeout(() => setCopiedKeyId(null), 2000);
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '-';
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
          { key: 'profiles', label: 'Profiles', icon: Mail },
          { key: 'apps', label: 'Applications', icon: Building2 },
          { key: 'templates', label: 'Templates', icon: FileText },
          { key: 'tasks', label: 'Tasks', icon: ListTodo },
          { key: 'outbox', label: 'Outbox', icon: Inbox },
          { key: 'history', label: 'Sent', icon: History },
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

      {/* App Selector for Templates/Tasks */}
      {(subTab === 'templates' || subTab === 'tasks') && apps.length > 0 && (
        <div className="flex items-center gap-2">
          <label className="text-sm text-gray-600">Filter by App:</label>
          <select
            value={selectedAppId || ''}
            onChange={(e) => setSelectedAppId(e.target.value ? parseInt(e.target.value) : undefined)}
            className="px-3 py-1.5 border rounded-lg text-sm"
          >
            <option value="">All Applications</option>
            {apps.map((app) => (
              <option key={app.app_ID} value={app.app_ID}>{app.app_Code} - {app.descr}</option>
            ))}
          </select>
        </div>
      )}

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
                onClick={() => {
                  // Find the value for StartTlsWhenAvailable from loaded modes, or default to '2'
                  const defaultMode = securityModes.find(m => m.text?.includes('when available'))?.value || '2';
                  setEditingProfile({ smtpPort: 587, securityMode: defaultMode, isActive: true });
                }}
                className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"
              >
                <Plus className="w-4 h-4" /> Add Profile
              </button>
            </div>
            <div className="divide-y divide-gray-200">
              {profiles.map((profile) => (
                <div key={profile.profileId} className="p-4 flex items-center justify-between">
                  <div>
                    <p className="font-medium">{profile.profileCode || `Profile ${profile.profileId}`}</p>
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
                  </div>
                </div>
              ))}
              {profiles.length === 0 && (
                <div className="p-8 text-center text-gray-500">No mail profiles configured</div>
              )}
            </div>
          </>
        )}

        {/* Applications Tab */}
        {!isLoading && subTab === 'apps' && (
          <>
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
              <h3 className="font-semibold">Applications & API Keys</h3>
              <button
                onClick={() => openAppEditor({})}
                className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"
              >
                <Plus className="w-4 h-4" /> Add Application
              </button>
            </div>

            {/* Revealed key banner */}
            {revealedKey && (
              <div className="mx-4 mt-4 p-4 bg-green-50 border border-green-200 rounded-lg">
                <div className="flex items-start justify-between">
                  <div>
                    <h4 className="font-medium text-green-800 flex items-center gap-2">
                      <Key className="w-4 h-4" /> API Key Generated
                    </h4>
                    <p className="text-sm text-green-700 mt-1">Copy this key now — it won't be shown again.</p>
                    <div className="mt-2 flex items-center gap-2">
                      <code className="px-3 py-2 bg-white border border-green-300 rounded font-mono text-sm select-all">
                        {revealedKey.key}
                      </code>
                      <button
                        onClick={() => copyKey(revealedKey.key, revealedKey.appId)}
                        className="p-2 text-green-600 hover:bg-green-100 rounded"
                        title="Copy to clipboard"
                      >
                        {copiedKeyId === revealedKey.appId ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                      </button>
                    </div>
                  </div>
                  <button onClick={() => setRevealedKey(null)} className="text-green-600 hover:text-green-800">
                    <X className="w-5 h-5" />
                  </button>
                </div>
              </div>
            )}

            <div className="divide-y divide-gray-200">
              {apps.map((app) => (
                <div key={app.app_ID} className="p-4">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <p className="font-medium">{app.app_Code}</p>
                        <span className={`px-2 py-0.5 text-xs rounded-full ${
                          app.isActive !== false ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'
                        }`}>
                          {app.isActive !== false ? 'Active' : 'Inactive'}
                        </span>
                      </div>
                      <p className="text-sm text-gray-500">{app.descr}</p>
                      {app.profileID && (
                        <p className="text-sm text-gray-400">
                          Profile: {profiles.find(p => p.profileId === app.profileID)?.profileCode || app.profileID}
                        </p>
                      )}

                      {/* API Key info */}
                      <div className="mt-2 flex items-center gap-3 text-sm">
                        {app.maskedKey ? (
                          <>
                            <span className="flex items-center gap-1 text-gray-500">
                              <Key className="w-3.5 h-3.5" />
                              <code className="bg-gray-100 px-2 py-0.5 rounded text-xs">{app.maskedKey}</code>
                            </span>
                            {app.requestCount != null && app.requestCount > 0 && (
                              <span className="text-gray-400">{app.requestCount.toLocaleString()} requests</span>
                            )}
                            {app.lastUsedAt && (
                              <span className="text-gray-400">Last used: {formatDate(app.lastUsedAt)}</span>
                            )}
                          </>
                        ) : (
                          <span className="text-gray-400 italic text-xs">No API key</span>
                        )}
                      </div>
                    </div>

                    {/* Action buttons */}
                    <div className="flex items-center gap-1">
                      <button
                        onClick={() => handleToggleApp(app.app_ID)}
                        className="p-1.5 hover:bg-gray-100 rounded"
                        title={app.isActive !== false ? 'Deactivate' : 'Activate'}
                      >
                        {app.isActive !== false
                          ? <ToggleRight className="w-4 h-4 text-green-500" />
                          : <ToggleLeft className="w-4 h-4 text-gray-400" />
                        }
                      </button>
                      <button
                        onClick={() => handleRegenerateKey(app.app_ID)}
                        className="p-1.5 hover:bg-gray-100 rounded"
                        title={app.maskedKey ? 'Regenerate API Key' : 'Generate API Key'}
                      >
                        <Key className="w-4 h-4 text-yellow-600" />
                      </button>
                      {app.maskedKey && (
                        <button
                          onClick={() => handleRevokeKey(app.app_ID)}
                          className="p-1.5 hover:bg-gray-100 rounded"
                          title="Revoke API Key"
                        >
                          <Trash2 className="w-4 h-4 text-red-400" />
                        </button>
                      )}
                      <button onClick={() => openAppEditor(app)} className="p-1.5 hover:bg-gray-100 rounded" title="Edit">
                        <Edit2 className="w-4 h-4 text-gray-500" />
                      </button>
                    </div>
                  </div>
                </div>
              ))}
              {apps.length === 0 && (
                <div className="p-8 text-center text-gray-500">No applications configured</div>
              )}
            </div>
          </>
        )}

        {/* Templates Tab */}
        {!isLoading && subTab === 'templates' && (
          <>
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
              <h3 className="font-semibold">Email Templates</h3>
              <button
                onClick={() => setEditingTemplate({ lang_Code: 'en' })}
                className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"
              >
                <Plus className="w-4 h-4" /> Add Template
              </button>
            </div>
            <div className="divide-y divide-gray-200">
              {templates.map((template) => (
                <div key={template.eT_ID} className="p-4 flex items-center justify-between">
                  <div>
                    <p className="font-medium">{template.eT_Code}</p>
                    <p className="text-sm text-gray-500">
                      {template.subject} ({template.lang_Code})
                    </p>
                    {template.app_Code && (
                      <p className="text-sm text-gray-400">App: {template.app_Code}</p>
                    )}
                  </div>
                  <button onClick={() => setEditingTemplate(template)} className="p-1 hover:bg-gray-100 rounded">
                    <Edit2 className="w-4 h-4 text-gray-500" />
                  </button>
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
              <h3 className="font-semibold">Tasks</h3>
              <button
                onClick={() => setEditingTask({ taskType: 'Email', status: 'Active', langCode: 'en' })}
                className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"
              >
                <Plus className="w-4 h-4" /> Add Task
              </button>
            </div>
            <div className="divide-y divide-gray-200">
              {tasks.map((task) => (
                <div key={task.task_ID} className="p-4 flex items-center justify-between">
                  <div>
                    <p className="font-medium"><span className="text-gray-400 text-sm mr-2">#{task.task_ID}</span>{task.taskCode}</p>
                    <p className="text-sm text-gray-500">
                      {task.taskType} - {task.mailTo || task.mailFrom || '(no recipients)'}
                    </p>
                    {task.testMailTo && (
                      <p className="text-sm text-gray-400">Test: {task.testMailTo}</p>
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
              <h3 className="font-semibold">Outbox ({outboxTotal})</h3>
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
                      {item.subject || item.taskCode || '(no subject)'} - Attempts: {item.attempts}
                    </p>
                    <p className="text-xs text-gray-400">{formatDate(item.createdAt)}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-1 text-xs rounded-full ${
                      item.status === 'Pending' ? 'bg-blue-100 text-blue-700' :
                      item.status === 'Failed' ? 'bg-red-100 text-red-700' :
                      'bg-gray-100 text-gray-600'
                    }`}>
                      {item.status}
                    </span>
                    <button
                      onClick={() => setViewingBody({
                        title: `Outbox #${item.id} — ${item.toList || 'Unknown'}`,
                        bodyHtml: item.bodyHtml,
                        bodyJson: item.bodyJson,
                        detailJson: item.detailJson,
                        rawData: JSON.stringify({
                          id: item.id, taskCode: item.taskCode, taskStatus: item.taskStatus,
                          templateCode: item.templateCode, toList: item.toList,
                          ccList: item.ccList, bccList: item.bccList, subject: item.subject,
                          emailFrom: item.emailFrom, emailFromName: item.emailFromName,
                          objectId: item.objectId, attempts: item.attempts,
                          status: item.status, createdAt: item.createdAt,
                          bodyJson: item.bodyJson, detailJson: item.detailJson,
                        }, null, 2),
                      })}
                      className="p-1 hover:bg-gray-100 rounded"
                      title="View Details"
                    >
                      <Eye className="w-4 h-4 text-gray-500" />
                    </button>
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

        {/* History/Sent Tab */}
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
                    <p className="text-sm text-gray-500">{item.subject || item.taskCode || '(no subject)'}</p>
                    <p className="text-xs text-gray-400">Sent: {formatDate(item.sentAt)}</p>
                    {item.errorMessage && (
                      <p className="text-xs text-red-500 truncate max-w-md">{item.errorMessage}</p>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-1 text-xs rounded-full ${
                      item.status === 'Sent' ? 'bg-green-100 text-green-700' :
                      item.status === 'Failed' ? 'bg-red-100 text-red-700' :
                      'bg-gray-100 text-gray-600'
                    }`}>
                      {item.status}
                    </span>
                    <button
                      onClick={() => setViewingBody({
                        title: `Sent #${item.id} — ${item.toList || 'Unknown'}`,
                        bodyHtml: item.bodyHtml,
                        bodyJson: item.bodyJson,
                        detailJson: item.detailJson,
                        rawData: JSON.stringify({
                          id: item.id, taskCode: item.taskCode, taskId: item.taskId,
                          toList: item.toList, ccList: item.ccList, bccList: item.bccList,
                          subject: item.subject, status: item.status, attempts: item.attempts,
                          errorMessage: item.errorMessage,
                          sentAt: item.sentAt, createdAt: item.createdAt,
                          bodyJson: item.bodyJson, detailJson: item.detailJson,
                        }, null, 2),
                      })}
                      className="p-1 hover:bg-gray-100 rounded"
                      title="View Details"
                    >
                      <Eye className="w-4 h-4 text-gray-500" />
                    </button>
                    <button onClick={() => handleRetryHistory(item.id)} className="p-1 hover:bg-gray-100 rounded" title="Retry">
                      <RefreshCw className="w-4 h-4 text-blue-500" />
                    </button>
                  </div>
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
              <h3 className="font-semibold">{editingProfile.profileId ? 'Edit' : 'Add'} Mail Profile</h3>
              <button onClick={() => setEditingProfile(null)}><X className="w-5 h-5" /></button>
            </div>
            <div className="p-4 space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">Profile Code</label>
                <input
                  type="text"
                  value={editingProfile.profileCode || ''}
                  onChange={(e) => setEditingProfile({ ...editingProfile, profileCode: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                  placeholder="PRIMARY_SMTP"
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
                  <label className="block text-sm font-medium mb-1">Auth User</label>
                  <input
                    type="text"
                    value={editingProfile.authUser || ''}
                    onChange={(e) => setEditingProfile({ ...editingProfile, authUser: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Auth Secret Ref</label>
                  <input
                    type="text"
                    value={editingProfile.authSecretRef || ''}
                    onChange={(e) => setEditingProfile({ ...editingProfile, authSecretRef: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    placeholder="Key vault reference"
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
                  value={editingProfile.securityMode || '2'}
                  onChange={(e) => setEditingProfile({ ...editingProfile, securityMode: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                >
                  {securityModes.length > 0 ? (
                    securityModes.map((m) => (
                      <option key={m.value} value={m.value}>{m.text}</option>
                    ))
                  ) : (
                    <>
                      <option key="0" value="0">None</option>
                      <option key="3" value="3">SSL on Connect</option>
                      <option key="1" value="1">StartTLS (required)</option>
                      <option key="2" value="2">StartTLS (when available)</option>
                    </>
                  )}
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

      {/* App Edit Modal */}
      {editingApp && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-lg w-full m-4 max-h-[90vh] overflow-y-auto">
            <div className="p-4 border-b flex items-center justify-between">
              <h3 className="font-semibold">{editingApp.app_ID ? 'Edit' : 'Add'} Application</h3>
              <button onClick={() => setEditingApp(null)}><X className="w-5 h-5" /></button>
            </div>
            <div className="p-4 space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">App Code</label>
                <input
                  type="text"
                  value={editingApp.app_Code || ''}
                  onChange={(e) => setEditingApp({ ...editingApp, app_Code: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                  placeholder="PICKLEBALL_COMMUNITY"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Description</label>
                <input
                  type="text"
                  value={editingApp.descr || ''}
                  onChange={(e) => setEditingApp({ ...editingApp, descr: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Default Profile</label>
                <select
                  value={editingApp.profileID || ''}
                  onChange={(e) => setEditingApp({ ...editingApp, profileID: e.target.value ? parseInt(e.target.value) : undefined })}
                  className="w-full px-3 py-2 border rounded-lg"
                >
                  <option value="">None</option>
                  {profiles.map((p) => (
                    <option key={p.profileId} value={p.profileId}>{p.profileCode}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Notes</label>
                <textarea
                  value={(editingApp as any).notes || ''}
                  onChange={(e) => setEditingApp({ ...editingApp, notes: e.target.value } as any)}
                  className="w-full px-3 py-2 border rounded-lg"
                  rows={2}
                  placeholder="Optional notes about this application"
                />
              </div>
              {/* Linked Profiles */}
              <div>
                <label className="block text-sm font-medium mb-2">Linked Profiles</label>
                {loadingLinkedProfiles ? (
                  <div className="flex items-center gap-2 text-sm text-gray-500">
                    <Loader2 className="w-4 h-4 animate-spin" /> Loading profiles...
                  </div>
                ) : profiles.length > 0 ? (
                  <div className="border rounded-lg p-3 max-h-48 overflow-y-auto space-y-2">
                    {profiles.map((p) => (
                      <label key={p.profileId} className="flex items-center gap-2 cursor-pointer hover:bg-gray-50 rounded px-1 py-0.5">
                        <input
                          type="checkbox"
                          checked={linkedProfileIds.includes(p.profileId)}
                          onChange={(e) => {
                            if (e.target.checked) {
                              setLinkedProfileIds([...linkedProfileIds, p.profileId]);
                            } else {
                              setLinkedProfileIds(linkedProfileIds.filter(id => id !== p.profileId));
                            }
                          }}
                          className="rounded border-gray-300"
                        />
                        <span className="text-sm">
                          {p.profileCode || `Profile ${p.profileId}`}
                          {p.fromEmail && <span className="text-gray-400 ml-1">({p.fromEmail})</span>}
                        </span>
                        {!p.isActive && <span className="text-xs text-gray-400 italic">inactive</span>}
                      </label>
                    ))}
                  </div>
                ) : (
                  <p className="text-sm text-gray-400 italic">No profiles available</p>
                )}
                <p className="text-xs text-gray-400 mt-1">Select which mail profiles this app can use for sending.</p>
              </div>

              {!editingApp.app_ID && (
                <p className="text-sm text-blue-600 flex items-center gap-1">
                  <Key className="w-4 h-4" />
                  An API key will be generated automatically on create.
                </p>
              )}
            </div>
            <div className="p-4 border-t flex justify-end gap-2">
              <button onClick={() => setEditingApp(null)} className="px-4 py-2 border rounded-lg">Cancel</button>
              <button onClick={handleSaveApp} disabled={isLoading} className="px-4 py-2 bg-blue-600 text-white rounded-lg disabled:opacity-50 flex items-center gap-2">
                {isLoading && <Loader2 className="w-4 h-4 animate-spin" />}
                Save
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Template Edit Modal */}
      {editingTemplate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-2xl w-full m-4 max-h-[90vh] overflow-y-auto">
            <div className="p-4 border-b flex items-center justify-between">
              <h3 className="font-semibold">{editingTemplate.eT_ID ? 'Edit' : 'Add'} Template</h3>
              <button onClick={() => setEditingTemplate(null)}><X className="w-5 h-5" /></button>
            </div>
            <div className="p-4 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Template Code</label>
                  <input
                    type="text"
                    value={editingTemplate.eT_Code || ''}
                    onChange={(e) => setEditingTemplate({ ...editingTemplate, eT_Code: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    placeholder="WELCOME_EMAIL"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Language</label>
                  <input
                    type="text"
                    value={editingTemplate.lang_Code || 'en'}
                    onChange={(e) => setEditingTemplate({ ...editingTemplate, lang_Code: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">App Code</label>
                <select
                  value={editingTemplate.app_Code || ''}
                  onChange={(e) => setEditingTemplate({ ...editingTemplate, app_Code: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                >
                  <option value="">(Global)</option>
                  {apps.map((a) => (
                    <option key={a.app_ID} value={a.app_Code}>{a.app_Code}</option>
                  ))}
                </select>
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
                <label className="block text-sm font-medium mb-1">Body (HTML with Scriban)</label>
                <textarea
                  value={editingTemplate.body || ''}
                  onChange={(e) => setEditingTemplate({ ...editingTemplate, body: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg font-mono text-sm"
                  rows={12}
                />
              </div>
            </div>
            <div className="p-4 border-t flex justify-end gap-2">
              <button onClick={() => setEditingTemplate(null)} className="px-4 py-2 border rounded-lg">Cancel</button>
              <button onClick={handleSaveTemplate} className="px-4 py-2 bg-blue-600 text-white rounded-lg">Save</button>
            </div>
          </div>
        </div>
      )}

      {/* Body View Modal */}
      {viewingBody && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={() => setViewingBody(null)}>
          <div className="bg-white rounded-xl shadow-xl max-w-3xl w-full m-4 max-h-[90vh] flex flex-col" onClick={(e) => e.stopPropagation()}>
            <div className="p-4 border-b flex items-center justify-between shrink-0">
              <h3 className="font-semibold truncate">{viewingBody.title}</h3>
              <button onClick={() => setViewingBody(null)} className="p-1 hover:bg-gray-100 rounded">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="p-4 overflow-y-auto space-y-4">
              {viewingBody.bodyJson && (
                <div>
                  <h4 className="text-sm font-semibold text-gray-600 mb-2">Body JSON</h4>
                  <pre className="bg-gray-50 border border-gray-200 rounded-lg p-3 text-sm font-mono whitespace-pre-wrap break-words max-h-64 overflow-y-auto">
                    {(() => {
                      try { return JSON.stringify(JSON.parse(viewingBody.bodyJson!), null, 2); }
                      catch { return viewingBody.bodyJson; }
                    })()}
                  </pre>
                </div>
              )}
              {viewingBody.detailJson && (
                <div>
                  <h4 className="text-sm font-semibold text-gray-600 mb-2">Detail JSON</h4>
                  <pre className="bg-gray-50 border border-gray-200 rounded-lg p-3 text-sm font-mono whitespace-pre-wrap break-words max-h-64 overflow-y-auto">
                    {(() => {
                      try { return JSON.stringify(JSON.parse(viewingBody.detailJson!), null, 2); }
                      catch { return viewingBody.detailJson; }
                    })()}
                  </pre>
                </div>
              )}
              {viewingBody.bodyHtml && (
                <div>
                  <h4 className="text-sm font-semibold text-gray-600 mb-2">Body HTML</h4>
                  <div className="border border-gray-200 rounded-lg overflow-hidden">
                    <div className="bg-gray-50 px-3 py-2 border-b flex items-center justify-between">
                      <span className="text-xs text-gray-500">Preview</span>
                    </div>
                    <div
                      className="p-3 bg-white max-h-80 overflow-y-auto"
                      dangerouslySetInnerHTML={{ __html: viewingBody.bodyHtml }}
                    />
                  </div>
                </div>
              )}
              {viewingBody.rawData && (
                <div>
                  <h4 className="text-sm font-semibold text-gray-600 mb-2">
                    {!viewingBody.bodyJson && !viewingBody.detailJson && !viewingBody.bodyHtml ? 'Record Data' : 'All Fields'}
                  </h4>
                  <pre className="bg-gray-50 border border-gray-200 rounded-lg p-3 text-sm font-mono whitespace-pre-wrap break-words max-h-64 overflow-y-auto">
                    {viewingBody.rawData}
                  </pre>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Task Edit Modal */}
      {editingTask && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-lg w-full m-4 max-h-[90vh] overflow-y-auto">
            <div className="p-4 border-b flex items-center justify-between">
              <h3 className="font-semibold">{editingTask.task_ID ? 'Edit' : 'Add'} Task</h3>
              <button onClick={() => setEditingTask(null)}><X className="w-5 h-5" /></button>
            </div>
            <div className="p-4 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Task Code</label>
                  <input
                    type="text"
                    value={editingTask.taskCode || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, taskCode: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Type</label>
                  <select
                    value={editingTask.taskType || 'Email'}
                    onChange={(e) => setEditingTask({ ...editingTask, taskType: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    {taskTypes.length > 0 ? (
                      taskTypes.map((t) => (
                        <option key={t.value} value={t.value}>{t.text}</option>
                      ))
                    ) : (
                      <>
                        <option key="Email" value="Email">Email</option>
                        <option key="SMS" value="SMS">SMS</option>
                      </>
                    )}
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Status</label>
                  <select
                    value={editingTask.status || 'Active'}
                    onChange={(e) => setEditingTask({ ...editingTask, status: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    {taskStatuses.length > 0 ? (
                      taskStatuses.map((s) => (
                        <option key={s.value} value={s.value}>{s.text}</option>
                      ))
                    ) : (
                      <>
                        <option key="Active" value="Active">Active</option>
                        <option key="Testing" value="Testing">Testing</option>
                        <option key="Inactive" value="Inactive">Inactive</option>
                      </>
                    )}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Language</label>
                  <input
                    type="text"
                    value={editingTask.langCode || 'en'}
                    onChange={(e) => setEditingTask({ ...editingTask, langCode: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Profile</label>
                  <select
                    value={editingTask.profileID || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, profileID: e.target.value ? parseInt(e.target.value) : undefined })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    <option value="">Select...</option>
                    {profiles.map((p) => (
                      <option key={p.profileId} value={p.profileId}>{p.profileCode}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Template</label>
                  <select
                    value={editingTask.templateID || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, templateID: e.target.value ? parseInt(e.target.value) : undefined })}
                    className="w-full px-3 py-2 border rounded-lg"
                  >
                    <option value="">Select...</option>
                    {templates.map((t) => (
                      <option key={t.eT_ID} value={t.eT_ID}>{t.eT_Code}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Mail From</label>
                  <input
                    type="email"
                    value={editingTask.mailFrom || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, mailFrom: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Mail From Name</label>
                  <input
                    type="text"
                    value={editingTask.mailFromName || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, mailFromName: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Mail To</label>
                <input
                  type="text"
                  value={editingTask.mailTo || ''}
                  onChange={(e) => setEditingTask({ ...editingTask, mailTo: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                  placeholder="Comma-separated or use + prefix to prepend"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">CC</label>
                  <input
                    type="text"
                    value={editingTask.mailCC || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, mailCC: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">BCC</label>
                  <input
                    type="text"
                    value={editingTask.mailBCC || ''}
                    onChange={(e) => setEditingTask({ ...editingTask, mailBCC: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Test Mail To (used when Status = Testing)</label>
                <input
                  type="email"
                  value={editingTask.testMailTo || ''}
                  onChange={(e) => setEditingTask({ ...editingTask, testMailTo: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Attachment Proc Name</label>
                <input
                  type="text"
                  value={editingTask.attachmentProcName || ''}
                  onChange={(e) => setEditingTask({ ...editingTask, attachmentProcName: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg"
                  placeholder="Optional stored procedure for attachments"
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
