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
    systemRole?: string;
  };
}

interface PasswordResetVerifyResponse {
  success: boolean;
  message?: string;
  accountExists: boolean;
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

  // Verify password reset code (returns whether account exists)
  async verifyPasswordResetCode(identifier: string, mode: 'email' | 'phone', code: string): Promise<PasswordResetVerifyResponse> {
    const body = mode === 'email'
      ? { email: identifier, code }
      : { phoneNumber: identifier, code };

    return request('/auth/password-reset/verify', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  // Complete password reset with verification code
  async resetPassword(identifier: string, mode: 'email' | 'phone', code: string, newPassword: string): Promise<ApiResponse> {
    const body = mode === 'email'
      ? { email: identifier, code, newPassword }
      : { phoneNumber: identifier, code, newPassword };

    return request('/auth/password-reset/complete', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  // Quick register (create account with verified OTP)
  async quickRegister(identifier: string, mode: 'email' | 'phone', code: string, password: string): Promise<AuthResponse> {
    const body = mode === 'email'
      ? { email: identifier, code, password }
      : { phoneNumber: identifier, code, password };

    return request('/auth/password-reset/register', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },
};

// Admin types
export interface Site {
  key: string;
  name: string;
  description?: string;
  url?: string;
  isActive: boolean;
  requiresSubscription: boolean;
  monthlyPriceCents?: number;
  yearlyPriceCents?: number;
  displayOrder: number;
  createdAt: string;
  updatedAt?: string;
}

export interface AdminUser {
  id: number;
  email?: string;
  phoneNumber?: string;
  systemRole?: string;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export interface AdminUserList {
  users: AdminUser[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserSiteInfo {
  siteKey: string;
  role: string;
  isActive: boolean;
  joinedAt: string;
}

export interface SubscriptionInfo {
  id: number;
  siteKey?: string;
  planName?: string;
  status: string;
  amountCents?: number;
  interval?: string;
  currentPeriodEnd?: string;
}

export interface PaymentInfo {
  id: number;
  amountCents: number;
  currency: string;
  status: string;
  description?: string;
  siteKey?: string;
  createdAt: string;
}

export interface AdminUserDetail extends AdminUser {
  updatedAt?: string;
  sites: UserSiteInfo[];
  subscriptions: SubscriptionInfo[];
  recentPayments: PaymentInfo[];
}

export interface AdminPayment {
  id: number;
  userId: number;
  userEmail?: string;
  amountCents: number;
  currency: string;
  status: string;
  description?: string;
  siteKey?: string;
  createdAt: string;
}

export interface AdminPaymentList {
  payments: AdminPayment[];
  totalCount: number;
  totalAmountCents: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminStats {
  totalUsers: number;
  newUsersToday: number;
  newUsersThisWeek: number;
  newUsersThisMonth: number;
  activeSubscriptions: number;
  revenueThisMonthCents: number;
  totalSites: number;
  activeSites: number;
}

// Helper to get auth header
function getAuthHeaders(): HeadersInit {
  const token = localStorage.getItem('auth_token');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

// Admin API methods
export const adminApi = {
  // Stats
  async getStats(): Promise<AdminStats> {
    return request('/admin/stats', {
      headers: getAuthHeaders(),
    });
  },

  // Sites
  async getSites(): Promise<Site[]> {
    return request('/admin/sites', {
      headers: getAuthHeaders(),
    });
  },

  async createSite(site: Omit<Site, 'createdAt' | 'updatedAt'>): Promise<Site> {
    return request('/admin/sites', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(site),
    });
  },

  async updateSite(key: string, updates: Partial<Site>): Promise<Site> {
    return request(`/admin/sites/${key}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(updates),
    });
  },

  // Users
  async searchUsers(search?: string, page = 1, pageSize = 20): Promise<AdminUserList> {
    const params = new URLSearchParams();
    if (search) params.set('search', search);
    params.set('page', page.toString());
    params.set('pageSize', pageSize.toString());

    return request(`/admin/users?${params}`, {
      headers: getAuthHeaders(),
    });
  },

  async getUser(id: number): Promise<AdminUserDetail> {
    return request(`/admin/users/${id}`, {
      headers: getAuthHeaders(),
    });
  },

  async updateUser(id: number, updates: Partial<AdminUser>): Promise<AdminUser> {
    return request(`/admin/users/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(updates),
    });
  },

  // Payments
  async getPayments(filters?: {
    userId?: number;
    siteKey?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }): Promise<AdminPaymentList> {
    const params = new URLSearchParams();
    if (filters?.userId) params.set('userId', filters.userId.toString());
    if (filters?.siteKey) params.set('siteKey', filters.siteKey);
    if (filters?.status) params.set('status', filters.status);
    if (filters?.fromDate) params.set('fromDate', filters.fromDate);
    if (filters?.toDate) params.set('toDate', filters.toDate);
    params.set('page', (filters?.page || 1).toString());
    params.set('pageSize', (filters?.pageSize || 20).toString());

    return request(`/admin/payments?${params}`, {
      headers: getAuthHeaders(),
    });
  },
};
