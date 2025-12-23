// API base URL from environment or same origin
const API_BASE_URL = import.meta.env.VITE_API_URL || '';

interface ApiResponse<T = unknown> {
  success: boolean;
  message?: string;
  data?: T;
}

interface AuthResponse {
  success: boolean;
  token?: string;
  message?: string;
  user?: {
    id: number;
    email?: string;
    phoneNumber?: string;
  };
}

async function request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || `Request failed with status ${response.status}`);
  }

  return data;
}

// Auth API methods
export const authApi = {
  // Login with email and password
  async login(email: string, password: string): Promise<AuthResponse> {
    return request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  },

  // Register new user
  async register(email: string, password: string): Promise<AuthResponse> {
    return request('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  },

  // Send OTP to phone number
  async sendOtp(phoneNumber: string): Promise<ApiResponse> {
    return request('/auth/otp/send', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber }),
    });
  },

  // Verify OTP code
  async verifyOtp(phoneNumber: string, code: string): Promise<AuthResponse> {
    return request('/auth/otp/verify', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber, code }),
    });
  },

  // Request password reset (sends code via email or phone)
  async requestPasswordReset(identifier: string, mode: 'email' | 'phone'): Promise<ApiResponse> {
    const body = mode === 'email'
      ? { email: identifier }
      : { phoneNumber: identifier };

    return request('/auth/password-reset/send', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  // Reset password with verification code
  async resetPassword(identifier: string, mode: 'email' | 'phone', code: string, newPassword: string): Promise<ApiResponse> {
    const body = mode === 'email'
      ? { email: identifier, code, newPassword }
      : { phoneNumber: identifier, code, newPassword };

    return request('/auth/password-reset/verify', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },
};
