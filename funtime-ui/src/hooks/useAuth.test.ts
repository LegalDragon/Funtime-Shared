import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { useAuth } from './useAuth';

// Mock the API client
vi.mock('../api/client', () => ({
  getFuntimeClient: vi.fn(() => mockClient),
}));

const mockClient = {
  login: vi.fn(),
  register: vi.fn(),
  verifyOtp: vi.fn(),
  sendOtp: vi.fn(),
  getCurrentUser: vi.fn(),
};

describe('useAuth', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('initializes with correct default state', async () => {
    const { result } = renderHook(() => useAuth());

    // After initialization with no token, should not be authenticated
    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBe(null);
    expect(result.current.token).toBe(null);
  });

  it('sets isLoading to false after initialization', async () => {
    const { result } = renderHook(() => useAuth());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });
  });

  it('loads user from stored token', async () => {
    const mockUser = { userId: 1, email: 'test@example.com' };
    mockClient.getCurrentUser.mockResolvedValue(mockUser);
    localStorage.setItem('funtime_token', 'stored-token');

    const { result } = renderHook(() => useAuth());

    await waitFor(() => {
      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.user).toEqual(mockUser);
    });
  });

  it('handles login successfully', async () => {
    const mockUser = { userId: 1, email: 'test@example.com' };
    const mockResponse = { success: true, token: 'test-token', user: mockUser };
    mockClient.login.mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useAuth());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    let response;
    await act(async () => {
      response = await result.current.login('test@example.com', 'password');
    });

    expect(mockClient.login).toHaveBeenCalledWith('test@example.com', 'password');
    expect(response).toEqual(mockResponse);
    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user).toEqual(mockUser);
    expect(localStorage.getItem('funtime_token')).toBe('test-token');
  });

  it('handles login failure', async () => {
    const mockResponse = { success: false, message: 'Invalid credentials' };
    mockClient.login.mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useAuth());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    let response;
    await act(async () => {
      response = await result.current.login('test@example.com', 'wrong');
    });

    expect(response).toEqual(mockResponse);
    expect(result.current.isAuthenticated).toBe(false);
  });

  it('handles register successfully', async () => {
    const mockUser = { userId: 1, email: 'new@example.com' };
    const mockResponse = { success: true, token: 'new-token', user: mockUser };
    mockClient.register.mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useAuth());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.register('new@example.com', 'password');
    });

    expect(mockClient.register).toHaveBeenCalledWith('new@example.com', 'password');
    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user).toEqual(mockUser);
  });

  it('handles OTP login', async () => {
    const mockUser = { userId: 1, phoneNumber: '+1234567890' };
    const mockResponse = { success: true, token: 'otp-token', user: mockUser };
    mockClient.verifyOtp.mockResolvedValue(mockResponse);

    const { result } = renderHook(() => useAuth());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.loginWithOtp('+1234567890', '123456');
    });

    expect(mockClient.verifyOtp).toHaveBeenCalledWith('+1234567890', '123456');
    expect(result.current.isAuthenticated).toBe(true);
  });

  it('sends OTP', async () => {
    mockClient.sendOtp.mockResolvedValue(undefined);

    const { result } = renderHook(() => useAuth());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.sendOtp('+1234567890');
    });

    expect(mockClient.sendOtp).toHaveBeenCalledWith('+1234567890');
  });

  it('handles logout', async () => {
    const mockUser = { userId: 1, email: 'test@example.com' };
    mockClient.getCurrentUser.mockResolvedValue(mockUser);
    localStorage.setItem('funtime_token', 'stored-token');

    const { result } = renderHook(() => useAuth());

    await waitFor(() => {
      expect(result.current.isAuthenticated).toBe(true);
    });

    act(() => {
      result.current.logout();
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBe(null);
    expect(result.current.token).toBe(null);
    expect(localStorage.getItem('funtime_token')).toBe(null);
  });

  it('refreshes user data', async () => {
    const mockUser = { userId: 1, email: 'test@example.com' };
    mockClient.getCurrentUser.mockResolvedValue(mockUser);
    localStorage.setItem('funtime_token', 'stored-token');

    const { result } = renderHook(() => useAuth());

    await waitFor(() => {
      expect(result.current.isAuthenticated).toBe(true);
    });

    const updatedUser = { ...mockUser, email: 'updated@example.com' };
    mockClient.getCurrentUser.mockResolvedValue(updatedUser);

    await act(async () => {
      await result.current.refreshUser();
    });

    expect(result.current.user).toEqual(updatedUser);
  });
});
