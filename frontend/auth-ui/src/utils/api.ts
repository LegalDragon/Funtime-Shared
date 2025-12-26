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
  logoUrl?: string;
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

  async uploadSiteLogo(key: string, file: File): Promise<Site> {
    const formData = new FormData();
    formData.append('file', file);

    const token = localStorage.getItem('auth_token');
    const response = await fetch(`${API_BASE_URL}/admin/sites/${key}/logo`, {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: formData,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Upload failed' }));
      throw new Error(error.message || 'Upload failed');
    }

    return response.json();
  },

  async deleteSiteLogo(key: string): Promise<Site> {
    const token = localStorage.getItem('auth_token');
    const response = await fetch(`${API_BASE_URL}/admin/sites/${key}/logo`, {
      method: 'DELETE',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Delete failed' }));
      throw new Error(error.message || 'Delete failed');
    }

    return response.json();
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

// Asset types
export interface AssetUploadResponse {
  assetId: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  storageType: string;
  url: string;
}

export interface AssetInfo {
  id: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  storageType: string;
  category?: string;
  uploadedBy?: number;
  createdAt: string;
  isPublic: boolean;
  url: string;
}

// Asset API methods
export const assetApi = {
  // Upload a file and get asset ID
  async upload(
    file: File,
    options?: {
      category?: string;
      siteKey?: string;
      isPublic?: boolean;
    }
  ): Promise<AssetUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const params = new URLSearchParams();
    if (options?.category) params.set('category', options.category);
    if (options?.siteKey) params.set('siteKey', options.siteKey);
    params.set('isPublic', (options?.isPublic ?? true).toString());

    const token = localStorage.getItem('auth_token');
    const response = await fetch(`${API_BASE_URL}/asset/upload?${params}`, {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: formData,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Upload failed' }));
      throw new Error(error.message || 'Upload failed');
    }

    return response.json();
  },

  // Get asset info by ID
  async getInfo(id: number): Promise<AssetInfo> {
    return request(`/asset/${id}/info`, {
      headers: getAuthHeaders(),
    });
  },

  // Get the URL to access an asset
  getUrl(id: number): string {
    return `${API_BASE_URL}/asset/${id}`;
  },

  // Delete an asset
  async delete(id: number): Promise<void> {
    const token = localStorage.getItem('auth_token');
    const response = await fetch(`${API_BASE_URL}/asset/${id}`, {
      method: 'DELETE',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Delete failed' }));
      throw new Error(error.message || 'Delete failed');
    }
  },
};

// Notification types
export interface MailProfile {
  id: number;
  name: string;
  smtpHost: string;
  smtpPort: number;
  username?: string;
  password?: string;
  fromEmail: string;
  fromName?: string;
  securityMode: string;
  isActive: boolean;
  siteKey?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface NotificationTemplate {
  id: number;
  code: string;
  name: string;
  type: string;
  language: string;
  subject?: string;
  body: string;
  bodyText?: string;
  siteKey?: string;
  isActive: boolean;
  description?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface NotificationTask {
  id: number;
  code: string;
  name: string;
  type: string;
  status: string;
  priority: string;
  mailProfileId?: number;
  mailProfileName?: string;
  templateId?: number;
  templateCode?: string;
  siteKey?: string;
  defaultRecipients?: string;
  ccRecipients?: string;
  bccRecipients?: string;
  testEmail?: string;
  maxRetries: number;
  description?: string;
  createdAt: string;
}

export interface NotificationOutbox {
  id: number;
  taskId?: number;
  type: string;
  toList: string;
  ccList?: string;
  bccList?: string;
  fromEmail?: string;
  fromName?: string;
  subject?: string;
  bodyHtml?: string;
  bodyText?: string;
  status: string;
  priority: string;
  attempts: number;
  maxAttempts: number;
  lastError?: string;
  scheduledAt?: string;
  nextRetryAt?: string;
  siteKey?: string;
  userId?: number;
  createdAt: string;
}

export interface NotificationHistory {
  id: number;
  outboxId?: number;
  taskId?: number;
  type: string;
  toList: string;
  fromEmail?: string;
  subject?: string;
  status: string;
  attempts: number;
  externalId?: string;
  errorMessage?: string;
  siteKey?: string;
  userId?: number;
  sentAt: string;
  deliveredAt?: string;
  createdAt: string;
}

export interface NotificationStats {
  totalProfiles: number;
  activeProfiles: number;
  totalTemplates: number;
  totalTasks: number;
  activeTasks: number;
  pendingMessages: number;
  failedMessages: number;
  sentToday: number;
  sentThisWeek: number;
}

export interface OutboxListResponse {
  items: NotificationOutbox[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface HistoryListResponse {
  items: NotificationHistory[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Notification API methods
export const notificationApi = {
  // Stats
  async getStats(): Promise<NotificationStats> {
    return request('/admin/notifications/stats', { headers: getAuthHeaders() });
  },

  // Mail Profiles
  async getProfiles(siteKey?: string): Promise<MailProfile[]> {
    const params = siteKey ? `?siteKey=${siteKey}` : '';
    return request(`/admin/notifications/profiles${params}`, { headers: getAuthHeaders() });
  },

  async getProfile(id: number): Promise<MailProfile> {
    return request(`/admin/notifications/profiles/${id}`, { headers: getAuthHeaders() });
  },

  async createProfile(profile: Omit<MailProfile, 'id' | 'createdAt' | 'updatedAt'>): Promise<MailProfile> {
    return request('/admin/notifications/profiles', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(profile),
    });
  },

  async updateProfile(id: number, profile: Partial<MailProfile>): Promise<MailProfile> {
    return request(`/admin/notifications/profiles/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(profile),
    });
  },

  async deleteProfile(id: number): Promise<void> {
    return request(`/admin/notifications/profiles/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });
  },

  // Templates
  async getTemplates(siteKey?: string, type?: string): Promise<NotificationTemplate[]> {
    const params = new URLSearchParams();
    if (siteKey) params.set('siteKey', siteKey);
    if (type) params.set('type', type);
    const queryString = params.toString();
    return request(`/admin/notifications/templates${queryString ? `?${queryString}` : ''}`, {
      headers: getAuthHeaders(),
    });
  },

  async getTemplate(id: number): Promise<NotificationTemplate> {
    return request(`/admin/notifications/templates/${id}`, { headers: getAuthHeaders() });
  },

  async createTemplate(template: Omit<NotificationTemplate, 'id' | 'createdAt' | 'updatedAt'>): Promise<NotificationTemplate> {
    return request('/admin/notifications/templates', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(template),
    });
  },

  async updateTemplate(id: number, template: Partial<NotificationTemplate>): Promise<NotificationTemplate> {
    return request(`/admin/notifications/templates/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(template),
    });
  },

  async deleteTemplate(id: number): Promise<void> {
    return request(`/admin/notifications/templates/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });
  },

  // Tasks
  async getTasks(siteKey?: string, status?: string): Promise<NotificationTask[]> {
    const params = new URLSearchParams();
    if (siteKey) params.set('siteKey', siteKey);
    if (status) params.set('status', status);
    const queryString = params.toString();
    return request(`/admin/notifications/tasks${queryString ? `?${queryString}` : ''}`, {
      headers: getAuthHeaders(),
    });
  },

  async getTask(id: number): Promise<NotificationTask> {
    return request(`/admin/notifications/tasks/${id}`, { headers: getAuthHeaders() });
  },

  async createTask(task: Omit<NotificationTask, 'id' | 'createdAt' | 'mailProfileName' | 'templateCode'>): Promise<NotificationTask> {
    return request('/admin/notifications/tasks', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(task),
    });
  },

  async updateTask(id: number, task: Partial<NotificationTask>): Promise<NotificationTask> {
    return request(`/admin/notifications/tasks/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(task),
    });
  },

  async deleteTask(id: number): Promise<void> {
    return request(`/admin/notifications/tasks/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });
  },

  // Outbox
  async getOutbox(options?: {
    status?: string;
    siteKey?: string;
    page?: number;
    pageSize?: number;
  }): Promise<OutboxListResponse> {
    const params = new URLSearchParams();
    if (options?.status) params.set('status', options.status);
    if (options?.siteKey) params.set('siteKey', options.siteKey);
    params.set('page', (options?.page || 1).toString());
    params.set('pageSize', (options?.pageSize || 20).toString());
    return request(`/admin/notifications/outbox?${params}`, { headers: getAuthHeaders() });
  },

  async retryOutbox(id: number): Promise<void> {
    return request(`/admin/notifications/outbox/${id}/retry`, {
      method: 'POST',
      headers: getAuthHeaders(),
    });
  },

  async deleteOutbox(id: number): Promise<void> {
    return request(`/admin/notifications/outbox/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });
  },

  async clearFailedOutbox(): Promise<void> {
    return request('/admin/notifications/outbox/clear-failed', {
      method: 'POST',
      headers: getAuthHeaders(),
    });
  },

  // History
  async getHistory(options?: {
    status?: string;
    siteKey?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }): Promise<HistoryListResponse> {
    const params = new URLSearchParams();
    if (options?.status) params.set('status', options.status);
    if (options?.siteKey) params.set('siteKey', options.siteKey);
    if (options?.fromDate) params.set('fromDate', options.fromDate);
    if (options?.toDate) params.set('toDate', options.toDate);
    params.set('page', (options?.page || 1).toString());
    params.set('pageSize', (options?.pageSize || 20).toString());
    return request(`/admin/notifications/history?${params}`, { headers: getAuthHeaders() });
  },
};
