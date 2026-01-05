import { config } from './config';

// API base URL from runtime config or same origin
const API_BASE_URL = config.API_URL;

// Rewrite asset URLs in HTML content to use current API base
// This handles content saved with development URLs like http://localhost:5000/asset/7
export function rewriteAssetUrls(html: string): string {
  if (!html) return html;

  // Match URLs like http://localhost:5000/asset/123 or https://old-server.com/asset/123
  // and replace with current API_BASE_URL
  return html.replace(
    /https?:\/\/[^/\s"']+\/asset\/(\d+)/g,
    `${API_BASE_URL}/asset/$1`
  );
}

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
    siteRole?: string;
    isSiteAdmin?: boolean;
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

// Public site info
export interface PublicSite {
  key: string;
  name: string;
  description?: string;
  url?: string;
  logoUrl?: string;
}

// Auth API methods
export const authApi = {
  // Get public sites (no auth required)
  async getSites(): Promise<PublicSite[]> {
    return request('/auth/sites', {});
  },

  // Login with email and password
  async login(email: string, password: string, siteKey?: string): Promise<AuthResponse> {
    return request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, siteKey }),
    });
  },

  // Login with phone number and password
  async loginWithPhone(phoneNumber: string, password: string, siteKey?: string): Promise<AuthResponse> {
    return request('/auth/login/phone', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber, password, siteKey }),
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

  // Get available OAuth providers
  async getOAuthProviders(): Promise<OAuthProvider[]> {
    return request('/auth/oauth/providers', {});
  },

  // Get OAuth start URL (for redirect)
  getOAuthStartUrl(provider: string, returnUrl?: string, siteKey?: string): string {
    const params = new URLSearchParams();
    if (returnUrl) params.set('returnUrl', returnUrl);
    if (siteKey) params.set('site', siteKey);
    const queryString = params.toString();
    return `${API_BASE_URL}/auth/oauth/${provider}/start${queryString ? '?' + queryString : ''}`;
  },
};

// OAuth provider info
export interface OAuthProvider {
  name: string;
  displayName: string;
  isConfigured: boolean;
}

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
  // Profile fields
  firstName?: string;
  lastName?: string;
  displayName?: string;
  city?: string;
  state?: string;
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

// JWT token payload structure (using .NET claim names)
export interface TokenPayload {
  nameid: string; // User ID (ClaimTypes.NameIdentifier)
  email?: string; // ClaimTypes.Email
  // ClaimTypes.Role maps to this long URL in the JWT
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
  role?: string; // Fallback short name
  sites?: string; // JSON array of site keys
  exp: number;
}

// Decode JWT token (without verification - that's done server-side)
export function decodeToken(token: string): TokenPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const payload = parts[1];
    // Handle base64url encoding
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

// Get current user info from stored token
export function getCurrentUser(): { id: number; email?: string; role?: string; sites: string[] } | null {
  const token = localStorage.getItem('auth_token');
  if (!token) return null;

  const payload = decodeToken(token);
  if (!payload) return null;

  // Check if token is expired
  if (payload.exp * 1000 < Date.now()) {
    localStorage.removeItem('auth_token');
    return null;
  }

  // .NET ClaimTypes.Role uses the full URL as the claim name
  const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role;

  return {
    id: parseInt(payload.nameid),
    email: payload.email,
    role,
    sites: payload.sites ? JSON.parse(payload.sites) : [],
  };
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
  async searchUsers(filters?: {
    search?: string;
    siteKey?: string;
    page?: number;
    pageSize?: number;
  }): Promise<AdminUserList> {
    const params = new URLSearchParams();
    if (filters?.search) params.set('search', filters.search);
    if (filters?.siteKey) params.set('siteKey', filters.siteKey);
    params.set('page', (filters?.page || 1).toString());
    params.set('pageSize', (filters?.pageSize || 20).toString());

    return request(`/admin/users?${params}`, {
      headers: getAuthHeaders(),
    });
  },

  async getUser(id: number): Promise<AdminUserDetail> {
    return request(`/admin/users/${id}`, {
      headers: getAuthHeaders(),
    });
  },

  async updateUser(id: number, updates: Partial<AdminUser> & { password?: string }): Promise<AdminUser> {
    return request(`/admin/users/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(updates),
    });
  },

  async updateUserSiteRole(userId: number, siteKey: string, role: string): Promise<UserSiteInfo> {
    return request(`/admin/users/${userId}/site-role`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify({ siteKey, role }),
    });
  },

  // Payments
  async getPayments(filters?: {
    userId?: number;
    userSearch?: string;
    siteKey?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }): Promise<AdminPaymentList> {
    const params = new URLSearchParams();
    if (filters?.userId) params.set('userId', filters.userId.toString());
    if (filters?.userSearch) params.set('userSearch', filters.userSearch);
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

  // Get user's payment methods
  async getUserPaymentMethods(userId: number): Promise<AdminPaymentMethod[]> {
    return request(`/admin/users/${userId}/payment-methods`, {
      headers: getAuthHeaders(),
    });
  },

  // Create payment intent (returns clientSecret for Stripe Elements)
  async createPaymentIntent(chargeData: {
    userId: number;
    amountCents: number;
    currency?: string;
    description: string;
    siteKey?: string;
  }): Promise<{
    paymentId: number;
    stripePaymentIntentId?: string;
    status: string;
    amountCents: number;
    currency: string;
    clientSecret?: string;
  }> {
    return request(`/admin/payments/charge`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({
        userId: chargeData.userId,
        amountCents: chargeData.amountCents,
        currency: chargeData.currency || 'usd',
        description: chargeData.description,
        siteKey: chargeData.siteKey,
      }),
    });
  },

  // Charge using saved payment method (immediate charge)
  async chargeWithPaymentMethod(chargeData: {
    userId: number;
    amountCents: number;
    currency?: string;
    description: string;
    siteKey?: string;
    paymentMethodId: string;
  }): Promise<{
    paymentId: number;
    stripePaymentIntentId?: string;
    status: string;
    amountCents: number;
    currency: string;
  }> {
    return request(`/admin/payments/charge`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({
        userId: chargeData.userId,
        amountCents: chargeData.amountCents,
        currency: chargeData.currency || 'usd',
        description: chargeData.description,
        siteKey: chargeData.siteKey,
        paymentMethodId: chargeData.paymentMethodId,
      }),
    });
  },

  // Send verification OTP to user's email or phone
  async sendVerification(userId: number, type: 'email' | 'phone'): Promise<{ success: boolean; message: string }> {
    return request(`/admin/users/${userId}/send-verification`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ type }),
    });
  },

  // Send test email to user
  async sendTestEmail(userId: number, subject?: string, message?: string): Promise<{ success: boolean; message: string }> {
    return request(`/admin/users/${userId}/send-test-email`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ subject, message }),
    });
  },

  // Send test SMS to user
  async sendTestSms(userId: number, message?: string): Promise<{ success: boolean; message: string }> {
    return request(`/admin/users/${userId}/send-test-sms`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ message }),
    });
  },
};

// Verification API - for users to verify their own email/phone
export interface VerifyRequestResponse {
  success: boolean;
  message: string;
  maskedIdentifier?: string;
  expiresInSeconds: number;
}

export interface VerifyConfirmResponse {
  success: boolean;
  message: string;
  verified: boolean;
}

export interface VerifyStatusResponse {
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
  email?: string;
  phone?: string;
}

export const verifyApi = {
  // Request a verification code to be sent
  async requestCode(type: 'email' | 'phone'): Promise<VerifyRequestResponse> {
    return request('/api/verify/request', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ type }),
    });
  },

  // Confirm verification with the received code
  async confirmCode(type: 'email' | 'phone', code: string): Promise<VerifyConfirmResponse> {
    return request('/api/verify/confirm', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ type, code }),
    });
  },

  // Get current verification status
  async getStatus(): Promise<VerifyStatusResponse> {
    return request('/api/verify/status', {
      headers: getAuthHeaders(),
    });
  },
};

// Credential change types
export interface CredentialChangeRequestResponse {
  success: boolean;
  message: string;
}

export interface CredentialChangeVerifyResponse {
  success: boolean;
  message: string;
  token?: string;
  user?: {
    id: number;
    email?: string;
    phone?: string;
  };
}

// Credential change API - for changing email/phone with OTP verification
export const credentialChangeApi = {
  // Request email change - sends OTP to new email
  async requestEmailChange(newEmail: string): Promise<CredentialChangeRequestResponse> {
    return request('/auth/change-email/request', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ newEmail }),
    });
  },

  // Verify email change with OTP code
  async verifyEmailChange(newEmail: string, code: string): Promise<CredentialChangeVerifyResponse> {
    return request('/auth/change-email/verify', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ newEmail, code }),
    });
  },

  // Request phone change - sends OTP via SMS
  async requestPhoneChange(newPhoneNumber: string): Promise<CredentialChangeRequestResponse> {
    return request('/auth/change-phone/request', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ newPhoneNumber }),
    });
  },

  // Verify phone change with OTP code
  async verifyPhoneChange(newPhoneNumber: string, code: string): Promise<CredentialChangeVerifyResponse> {
    return request('/auth/change-phone/verify', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ newPhoneNumber, code }),
    });
  },
};

// Admin payment method type
export interface AdminPaymentMethod {
  id: number;
  stripePaymentMethodId: string;
  type?: string;
  cardBrand?: string;
  cardLast4?: string;
  cardExpMonth?: number;
  cardExpYear?: number;
  isDefault: boolean;
  createdAt: string;
}

// Asset types
export type AssetType = 'image' | 'video' | 'document' | 'audio' | 'link';

export interface AssetUploadResponse {
  assetId: number;
  assetType: AssetType;
  fileName: string;
  contentType: string;
  fileSize: number;
  url: string;
  externalUrl?: string;
  thumbnailUrl?: string;
}

export interface AssetInfo {
  id: number;
  assetType: AssetType;
  fileName: string;
  contentType: string;
  fileSize: number;
  category?: string;
  externalUrl?: string;
  thumbnailUrl?: string;
  uploadedBy?: number;
  createdAt: string;
  isPublic: boolean;
  url: string;
}

export interface RegisterLinkRequest {
  url: string;
  title?: string;
  assetType?: AssetType;
  thumbnailUrl?: string;
  category?: string;
  siteKey?: string;
  isPublic?: boolean;
}

// Asset API methods
export const assetApi = {
  // Upload a file and get asset ID
  async upload(
    file: File,
    options?: {
      assetType?: AssetType;
      category?: string;
      siteKey?: string;
      isPublic?: boolean;
    }
  ): Promise<AssetUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const params = new URLSearchParams();
    if (options?.assetType) params.set('assetType', options.assetType);
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

  // Register an external link as an asset (YouTube, etc.)
  async registerLink(linkData: RegisterLinkRequest): Promise<AssetUploadResponse> {
    const token = localStorage.getItem('auth_token');
    const response = await fetch(`${API_BASE_URL}/asset/link`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(linkData),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to register link' }));
      throw new Error(error.message || 'Failed to register link');
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

// Notification types - matching FXNotification database schema

export interface MailProfile {
  profileId: number;
  profileCode?: string;
  app_ID?: number;
  fromName?: string;
  fromEmail?: string;
  smtpHost?: string;
  smtpPort: number;
  authUser?: string;
  authSecretRef?: string;
  securityMode?: string;
  isActive: boolean;
}

export interface AppRow {
  app_ID: number;
  app_Code?: string;
  descr?: string;
  profileID?: number;
}

export interface EmailTemplate {
  eT_ID: number;
  eT_Code?: string;
  lang_Code?: string;
  subject?: string;
  body?: string;
  app_Code?: string;
}

export interface TaskRow {
  task_ID: number;
  taskCode?: string;
  taskType?: string;
  app_ID?: number;
  profileID?: number;
  templateID?: number;
  status?: string;
  mailPriority?: string;
  testMailTo?: string;
  langCode?: string;
  mailFromName?: string;
  mailFrom?: string;
  mailTo?: string;
  mailCC?: string;
  mailBCC?: string;
  attachmentProcName?: string;
}

export interface OutboxRow {
  id: number;
  taskId?: number;
  taskCode?: string;
  taskStatus?: string;
  templateCode?: string;
  langCode?: string;
  emailFrom?: string;
  emailFromName?: string;
  mailPriority?: number;
  objectId?: string;
  toList?: string;
  ccList?: string;
  bccList?: string;
  subject?: string;
  bodyHtml?: string;
  bodyJson?: string;
  detailJson?: string;
  attempts: number;
  status?: string;
  createdAt?: string;
}

export interface HistoryRow {
  iD: number;
  taskId?: number;
  taskCode?: string;
  toList?: string;
  ccList?: string;
  bccList?: string;
  subject?: string;
  status?: string;
  attempts: number;
  errorMessage?: string;
  sentAt?: string;
  createdAt?: string;
}

export interface LookupItem {
  value?: string;
  text?: string;
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
  items: OutboxRow[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface HistoryListResponse {
  items: HistoryRow[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Notification API methods - matching FXNotification stored procedures
export const notificationApi = {
  // Stats
  async getStats(): Promise<NotificationStats> {
    return request('/admin/notifications/stats', { headers: getAuthHeaders() });
  },

  // Mail Profiles
  async getProfiles(): Promise<MailProfile[]> {
    return request('/admin/notifications/profiles', { headers: getAuthHeaders() });
  },

  async createProfile(profile: Partial<MailProfile>): Promise<MailProfile> {
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

  // Applications
  async getApps(): Promise<AppRow[]> {
    return request('/admin/notifications/apps', { headers: getAuthHeaders() });
  },

  async createApp(app: Partial<AppRow>): Promise<AppRow> {
    return request('/admin/notifications/apps', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(app),
    });
  },

  async updateApp(id: number, app: Partial<AppRow>): Promise<AppRow> {
    return request(`/admin/notifications/apps/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(app),
    });
  },

  // Templates
  async getTemplates(appId?: number): Promise<EmailTemplate[]> {
    const params = appId ? `?appId=${appId}` : '';
    return request(`/admin/notifications/templates${params}`, { headers: getAuthHeaders() });
  },

  async createTemplate(template: Partial<EmailTemplate>): Promise<EmailTemplate> {
    return request('/admin/notifications/templates', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(template),
    });
  },

  async updateTemplate(id: number, template: Partial<EmailTemplate>): Promise<EmailTemplate> {
    return request(`/admin/notifications/templates/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(template),
    });
  },

  // Tasks
  async getTasks(appId?: number): Promise<TaskRow[]> {
    const params = appId ? `?appId=${appId}` : '';
    return request(`/admin/notifications/tasks${params}`, { headers: getAuthHeaders() });
  },

  async createTask(task: Partial<TaskRow>): Promise<TaskRow> {
    return request('/admin/notifications/tasks', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(task),
    });
  },

  async updateTask(id: number, task: Partial<TaskRow>): Promise<TaskRow> {
    return request(`/admin/notifications/tasks/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(task),
    });
  },

  // Outbox
  async getOutbox(options?: { page?: number; pageSize?: number }): Promise<OutboxListResponse> {
    const params = new URLSearchParams();
    params.set('page', (options?.page || 1).toString());
    params.set('pageSize', (options?.pageSize || 20).toString());
    return request(`/admin/notifications/outbox?${params}`, { headers: getAuthHeaders() });
  },

  async deleteOutbox(id: number): Promise<void> {
    return request(`/admin/notifications/outbox/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });
  },

  // History
  async getHistory(options?: { page?: number; pageSize?: number }): Promise<HistoryListResponse> {
    const params = new URLSearchParams();
    params.set('page', (options?.page || 1).toString());
    params.set('pageSize', (options?.pageSize || 20).toString());
    return request(`/admin/notifications/history?${params}`, { headers: getAuthHeaders() });
  },

  async retryHistory(id: number): Promise<void> {
    return request(`/admin/notifications/history/${id}/retry`, {
      method: 'POST',
      headers: getAuthHeaders(),
    });
  },

  // Lookups
  async getSecurityModes(): Promise<LookupItem[]> {
    return request('/admin/notifications/lookup/security-modes', { headers: getAuthHeaders() });
  },

  async getTaskStatuses(): Promise<LookupItem[]> {
    return request('/admin/notifications/lookup/task-status', { headers: getAuthHeaders() });
  },

  async getTaskTypes(): Promise<LookupItem[]> {
    return request('/admin/notifications/lookup/task-types', { headers: getAuthHeaders() });
  },

  async getTaskPriorities(): Promise<LookupItem[]> {
    return request('/admin/notifications/lookup/task-priorities', { headers: getAuthHeaders() });
  },
};

// Push Notification API - for sending real-time notifications
export interface PushNotificationPayload {
  type: string;
  payload?: unknown;
}

export interface UserConnectionStatus {
  userId: number;
  isConnected: boolean;
}

export interface BatchNotificationResult {
  userId: number;
  isConnected: boolean;
}

export const pushApi = {
  // Send notification to a specific user
  async sendToUser(userId: number, type: string, payload?: unknown): Promise<{ success: boolean; isUserConnected: boolean }> {
    return request(`/api/push/user/${userId}`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ type, payload }),
    });
  },

  // Send notification to all users on a site
  async sendToSite(siteKey: string, type: string, payload?: unknown): Promise<{ success: boolean }> {
    return request(`/api/push/site/${siteKey}`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ type, payload }),
    });
  },

  // Broadcast to all connected users (admin only)
  async broadcast(type: string, payload?: unknown): Promise<{ success: boolean }> {
    return request('/api/push/broadcast', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ type, payload }),
    });
  },

  // Check if a user is connected
  async getUserStatus(userId: number): Promise<UserConnectionStatus> {
    return request(`/api/push/user/${userId}/status`, {
      headers: getAuthHeaders(),
    });
  },

  // Send to multiple users at once
  async sendToUsers(
    userIds: number[],
    type: string,
    payload?: unknown
  ): Promise<{ success: boolean; results: BatchNotificationResult[] }> {
    return request('/api/push/users/batch', {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ userIds, type, payload }),
    });
  },
};

// Settings API
export interface MainLogoResponse {
  hasLogo: boolean;
  logoUrl?: string;
  fileName?: string;
}

export interface LegalContentResponse {
  content: string;
  updatedAt?: string;
}

export const settingsApi = {
  // Get main logo (public)
  async getMainLogo(): Promise<MainLogoResponse> {
    return request('/settings/logo');
  },

  // Upload main logo (admin only)
  async uploadMainLogo(file: File): Promise<MainLogoResponse> {
    const token = localStorage.getItem('auth_token');
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch(`${API_BASE_URL}/settings/logo`, {
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

  // Delete main logo (admin only)
  async deleteMainLogo(): Promise<void> {
    const token = localStorage.getItem('auth_token');
    const response = await fetch(`${API_BASE_URL}/settings/logo`, {
      method: 'DELETE',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Delete failed' }));
      throw new Error(error.message || 'Delete failed');
    }
  },

  // Get logo URL for display (handles API path prefix)
  getLogoDisplayUrl(logoUrl: string): string {
    if (!logoUrl) return '';

    // Extract the path from the URL (remove any host prefix)
    let path = logoUrl;

    // If it's a full URL, extract just the path
    if (logoUrl.startsWith('http://') || logoUrl.startsWith('https://')) {
      try {
        const url = new URL(logoUrl);
        path = url.pathname;
      } catch {
        // If URL parsing fails, try to extract path after the host
        const match = logoUrl.match(/https?:\/\/[^/]+(\/.*)/);
        if (match) {
          path = match[1];
        }
      }
    }

    // Now prepend the correct API base URL
    return `${API_BASE_URL}${path}`;
  },

  // Get Terms of Service (public)
  async getTermsOfService(): Promise<LegalContentResponse> {
    return request('/settings/terms-of-service');
  },

  // Update Terms of Service (admin only)
  async updateTermsOfService(content: string): Promise<LegalContentResponse> {
    return request('/settings/terms-of-service', {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify({ content }),
    });
  },

  // Get Privacy Policy (public)
  async getPrivacyPolicy(): Promise<LegalContentResponse> {
    return request('/settings/privacy-policy');
  },

  // Update Privacy Policy (admin only)
  async updatePrivacyPolicy(content: string): Promise<LegalContentResponse> {
    return request('/settings/privacy-policy', {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify({ content }),
    });
  },
};
