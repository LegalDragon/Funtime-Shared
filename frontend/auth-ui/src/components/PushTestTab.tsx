import { useState, useEffect } from 'react';
import { Radio, Send, CheckCircle, Users, Globe, Loader2, AlertCircle } from 'lucide-react';
import { pushApi, adminApi } from '../utils/api';
import type { Site } from '../utils/api';

export function PushTestTab() {
  const [sites, setSites] = useState<Site[]>([]);
  const [pushTargetType, setPushTargetType] = useState<'user' | 'site' | 'broadcast'>('user');
  const [pushUserId, setPushUserId] = useState('');
  const [pushSiteKey, setPushSiteKey] = useState('');
  const [pushType, setPushType] = useState('test');
  const [pushTitle, setPushTitle] = useState('');
  const [pushMessage, setPushMessage] = useState('');
  const [pushSending, setPushSending] = useState(false);
  const [pushResult, setPushResult] = useState<{ success: boolean; message: string } | null>(null);

  useEffect(() => {
    loadSites();
  }, []);

  const loadSites = async () => {
    try {
      const data = await adminApi.getSites();
      setSites(data);
    } catch (err) {
      console.error('Failed to load sites:', err);
    }
  };

  const handleSendPush = async () => {
    setPushSending(true);
    setPushResult(null);

    const payload = {
      title: pushTitle || undefined,
      message: pushMessage || undefined,
      sentAt: new Date().toISOString(),
    };

    try {
      let result;
      if (pushTargetType === 'user') {
        const userId = parseInt(pushUserId);
        if (isNaN(userId) || userId <= 0) {
          throw new Error('Please enter a valid user ID');
        }
        result = await pushApi.sendToUser(userId, pushType, payload);
        setPushResult({
          success: true,
          message: result.isUserConnected
            ? `Notification sent to user ${userId} (connected)`
            : `Notification sent to user ${userId} (not currently connected - will receive on next connection)`,
        });
      } else if (pushTargetType === 'site') {
        if (!pushSiteKey) {
          throw new Error('Please select a site');
        }
        await pushApi.sendToSite(pushSiteKey, pushType, payload);
        setPushResult({
          success: true,
          message: `Notification sent to all users on site "${pushSiteKey}"`,
        });
      } else {
        await pushApi.broadcast(pushType, payload);
        setPushResult({
          success: true,
          message: 'Broadcast sent to all connected users',
        });
      }
    } catch (err) {
      setPushResult({
        success: false,
        message: err instanceof Error ? err.message : 'Failed to send notification',
      });
    } finally {
      setPushSending(false);
    }
  };

  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <div className="max-w-xl">
        <h3 className="font-semibold text-lg mb-2">Test Push Notifications</h3>
        <p className="text-sm text-gray-500 mb-6">
          Send real-time push notifications via SignalR. Users must be connected to receive notifications.
        </p>

        {/* Target Type Selection */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-700 mb-2">Send To</label>
          <div className="flex gap-2 flex-wrap">
            <button
              type="button"
              onClick={() => setPushTargetType('user')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg border ${
                pushTargetType === 'user'
                  ? 'bg-blue-50 border-blue-500 text-blue-700'
                  : 'border-gray-300 text-gray-600 hover:bg-gray-50'
              }`}
            >
              <Users className="w-4 h-4" />
              Specific User
            </button>
            <button
              type="button"
              onClick={() => setPushTargetType('site')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg border ${
                pushTargetType === 'site'
                  ? 'bg-blue-50 border-blue-500 text-blue-700'
                  : 'border-gray-300 text-gray-600 hover:bg-gray-50'
              }`}
            >
              <Globe className="w-4 h-4" />
              Site Users
            </button>
            <button
              type="button"
              onClick={() => setPushTargetType('broadcast')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg border ${
                pushTargetType === 'broadcast'
                  ? 'bg-blue-50 border-blue-500 text-blue-700'
                  : 'border-gray-300 text-gray-600 hover:bg-gray-50'
              }`}
            >
              <Radio className="w-4 h-4" />
              Broadcast All
            </button>
          </div>
        </div>

        {/* Target Input */}
        {pushTargetType === 'user' && (
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">User ID</label>
            <input
              type="number"
              value={pushUserId}
              onChange={(e) => setPushUserId(e.target.value)}
              placeholder="Enter user ID (e.g., 123)"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        )}

        {pushTargetType === 'site' && (
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Site</label>
            <select
              value={pushSiteKey}
              onChange={(e) => setPushSiteKey(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">Select a site...</option>
              {sites.map((site) => (
                <option key={site.key} value={site.key}>
                  {site.name} ({site.key})
                </option>
              ))}
            </select>
          </div>
        )}

        {pushTargetType === 'broadcast' && (
          <div className="mb-4 p-3 bg-amber-50 border border-amber-200 rounded-lg">
            <p className="text-sm text-amber-700">
              This will send a notification to <strong>all connected users</strong> across all sites.
            </p>
          </div>
        )}

        {/* Notification Content */}
        <div className="space-y-4 mb-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Notification Type</label>
            <select
              value={pushType}
              onChange={(e) => setPushType(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="test">test</option>
              <option value="alert">alert</option>
              <option value="message">message</option>
              <option value="update">update</option>
              <option value="announcement">announcement</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Title (optional)</label>
            <input
              type="text"
              value={pushTitle}
              onChange={(e) => setPushTitle(e.target.value)}
              placeholder="Notification title"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Message (optional)</label>
            <textarea
              value={pushMessage}
              onChange={(e) => setPushMessage(e.target.value)}
              placeholder="Notification message content"
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>

        {/* Result Message */}
        {pushResult && (
          <div className={`mb-4 p-3 rounded-lg flex items-start gap-2 ${
            pushResult.success
              ? 'bg-green-50 border border-green-200 text-green-700'
              : 'bg-red-50 border border-red-200 text-red-700'
          }`}>
            {pushResult.success ? (
              <CheckCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
            ) : (
              <AlertCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
            )}
            <span className="text-sm">{pushResult.message}</span>
          </div>
        )}

        {/* Send Button */}
        <button
          onClick={handleSendPush}
          disabled={pushSending}
          className="flex items-center justify-center gap-2 w-full px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {pushSending ? (
            <Loader2 className="w-5 h-5 animate-spin" />
          ) : (
            <Send className="w-5 h-5" />
          )}
          {pushSending ? 'Sending...' : 'Send Notification'}
        </button>

        {/* Help Text */}
        <div className="mt-6 p-4 bg-gray-50 rounded-lg">
          <h4 className="font-medium text-gray-900 mb-2">How it works</h4>
          <ul className="text-sm text-gray-600 space-y-1">
            <li>• Notifications are sent via SignalR WebSocket connection</li>
            <li>• Users must be connected to receive notifications in real-time</li>
            <li>• Use the <code className="bg-gray-200 px-1 rounded">useNotifications</code> hook in React to receive</li>
            <li>• External sites can call <code className="bg-gray-200 px-1 rounded">POST /api/push/user/:id</code></li>
          </ul>
        </div>
      </div>
    </div>
  );
}
