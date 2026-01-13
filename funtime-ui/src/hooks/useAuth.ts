import { useState, useCallback, useEffect } from 'react';
import type { User, UserFull, AuthResponse } from '../types';
import { getFuntimeClient } from '../api/client';

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  token: string | null;
}

export interface UseAuthReturn extends AuthState {
  login: (email: string, password: string) => Promise<AuthResponse>;
  register: (email: string, password: string) => Promise<AuthResponse>;
  loginWithOtp: (phoneNumber: string, code: string) => Promise<AuthResponse>;
  sendOtp: (phoneNumber: string) => Promise<void>;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const TOKEN_KEY = 'funtime_token';

export function useAuth(): UseAuthReturn {
  const [state, setState] = useState<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: true,
    token: null,
  });

  const setToken = useCallback((token: string | null) => {
    if (token) {
      localStorage.setItem(TOKEN_KEY, token);
    } else {
      localStorage.removeItem(TOKEN_KEY);
    }
    setState(prev => ({ ...prev, token }));
  }, []);

  const refreshUser = useCallback(async () => {
    try {
      const client = getFuntimeClient();
      const user = await client.getCurrentUser();
      setState(prev => ({
        ...prev,
        user,
        isAuthenticated: true,
        isLoading: false,
      }));
    } catch {
      setState(prev => ({
        ...prev,
        user: null,
        isAuthenticated: false,
        isLoading: false,
      }));
    }
  }, []);

  const login = useCallback(async (email: string, password: string): Promise<AuthResponse> => {
    const client = getFuntimeClient();
    const response = await client.login(email, password);
    if (response.success && response.token) {
      setToken(response.token);
      if (response.user) {
        setState(prev => ({
          ...prev,
          user: response.user!,
          isAuthenticated: true,
          isLoading: false,
        }));
      }
    }
    return response;
  }, [setToken]);

  const register = useCallback(async (email: string, password: string): Promise<AuthResponse> => {
    const client = getFuntimeClient();
    const response = await client.register(email, password);
    if (response.success && response.token) {
      setToken(response.token);
      if (response.user) {
        setState(prev => ({
          ...prev,
          user: response.user!,
          isAuthenticated: true,
          isLoading: false,
        }));
      }
    }
    return response;
  }, [setToken]);

  const loginWithOtp = useCallback(async (phoneNumber: string, code: string): Promise<AuthResponse> => {
    const client = getFuntimeClient();
    const response = await client.verifyOtp(phoneNumber, code);
    if (response.success && response.token) {
      setToken(response.token);
      if (response.user) {
        setState(prev => ({
          ...prev,
          user: response.user!,
          isAuthenticated: true,
          isLoading: false,
        }));
      }
    }
    return response;
  }, [setToken]);

  const sendOtp = useCallback(async (phoneNumber: string): Promise<void> => {
    const client = getFuntimeClient();
    await client.sendOtp(phoneNumber);
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      token: null,
    });
  }, [setToken]);

  // Initialize from stored token
  useEffect(() => {
    const storedToken = localStorage.getItem(TOKEN_KEY);
    if (storedToken) {
      setState(prev => ({ ...prev, token: storedToken }));
      refreshUser();
    } else {
      setState(prev => ({ ...prev, isLoading: false }));
    }
  }, [refreshUser]);

  return {
    ...state,
    login,
    register,
    loginWithOtp,
    sendOtp,
    logout,
    refreshUser,
  };
}
