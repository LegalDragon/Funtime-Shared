import { useEffect, useState, useCallback, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { config } from '../utils/config';

export interface Notification {
  type: string;
  payload: unknown;
  timestamp: string;
}

export interface UseNotificationsOptions {
  /** Site key to join for site-wide notifications */
  siteKey?: string;
  /** Called when a notification is received */
  onNotification?: (notification: Notification) => void;
  /** Whether to automatically connect (default: true when token is present) */
  autoConnect?: boolean;
}

export interface UseNotificationsReturn {
  /** Current connection state */
  connectionState: signalR.HubConnectionState;
  /** Whether the connection is active */
  isConnected: boolean;
  /** Recent notifications (last 50) */
  notifications: Notification[];
  /** Manually connect to the notification hub */
  connect: () => Promise<void>;
  /** Manually disconnect from the notification hub */
  disconnect: () => Promise<void>;
  /** Clear notifications list */
  clearNotifications: () => void;
  /** Any connection error */
  error: Error | null;
}

const HUB_URL = `${config.API_URL}/hubs/notifications`;
const MAX_NOTIFICATIONS = 50;

export function useNotifications(options: UseNotificationsOptions = {}): UseNotificationsReturn {
  const { siteKey, onNotification, autoConnect = true } = options;

  const [connectionState, setConnectionState] = useState<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [error, setError] = useState<Error | null>(null);

  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const onNotificationRef = useRef(onNotification);

  // Keep callback ref updated
  useEffect(() => {
    onNotificationRef.current = onNotification;
  }, [onNotification]);

  const handleNotification = useCallback((notification: Notification) => {
    setNotifications((prev) => {
      const updated = [notification, ...prev];
      return updated.slice(0, MAX_NOTIFICATIONS);
    });

    if (onNotificationRef.current) {
      onNotificationRef.current(notification);
    }
  }, []);

  const connect = useCallback(async () => {
    // Get token from localStorage
    const token = localStorage.getItem('auth_token');
    if (!token) {
      setError(new Error('No authentication token available'));
      return;
    }

    // Clean up existing connection
    if (connectionRef.current) {
      await connectionRef.current.stop();
    }

    try {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL, {
          accessTokenFactory: () => token,
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Set up event handlers
      connection.on('ReceiveNotification', handleNotification);

      connection.onclose((err: Error | undefined) => {
        setConnectionState(signalR.HubConnectionState.Disconnected);
        if (err) {
          setError(err);
        }
      });

      connection.onreconnecting((err: Error | undefined) => {
        setConnectionState(signalR.HubConnectionState.Reconnecting);
        if (err) {
          console.warn('SignalR reconnecting:', err);
        }
      });

      connection.onreconnected(() => {
        setConnectionState(signalR.HubConnectionState.Connected);
        setError(null);

        // Rejoin site group after reconnection
        if (siteKey) {
          connection.invoke('JoinSiteGroup', siteKey).catch(console.error);
        }
      });

      // Start connection
      await connection.start();
      connectionRef.current = connection;
      setConnectionState(signalR.HubConnectionState.Connected);
      setError(null);

      // Join site group if specified
      if (siteKey) {
        await connection.invoke('JoinSiteGroup', siteKey);
      }
    } catch (err) {
      setError(err instanceof Error ? err : new Error(String(err)));
      setConnectionState(signalR.HubConnectionState.Disconnected);
    }
  }, [siteKey, handleNotification]);

  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      await connectionRef.current.stop();
      connectionRef.current = null;
    }
    setConnectionState(signalR.HubConnectionState.Disconnected);
  }, []);

  const clearNotifications = useCallback(() => {
    setNotifications([]);
  }, []);

  // Auto-connect on mount if token is available
  useEffect(() => {
    const token = localStorage.getItem('auth_token');
    if (autoConnect && token) {
      connect();
    }

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, [autoConnect, connect]);

  // Handle site key changes
  useEffect(() => {
    const connection = connectionRef.current;
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      if (siteKey) {
        connection.invoke('JoinSiteGroup', siteKey).catch(console.error);
      }
    }
  }, [siteKey]);

  return {
    connectionState,
    isConnected: connectionState === signalR.HubConnectionState.Connected,
    notifications,
    connect,
    disconnect,
    clearNotifications,
    error,
  };
}
