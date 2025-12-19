import type {
  AuthResponse,
  ValidateTokenResponse,
  User,
  UserProfile,
  UserFull,
  UserSite,
  PaymentMethod,
  Payment,
  Subscription,
  ApiResponse,
  SetupIntentResponse,
  PaymentIntentResponse,
} from '../types';

export interface FuntimeClientConfig {
  baseUrl: string;
  stripePublishableKey?: string;
  getToken?: () => string | null;
  onUnauthorized?: () => void;
}

// Store config globally for components to access
let globalConfig: FuntimeClientConfig | null = null;

export function getConfig(): FuntimeClientConfig | null {
  return globalConfig;
}

export class FuntimeClient {
  private baseUrl: string;
  private getToken: () => string | null;
  private onUnauthorized: () => void;

  constructor(config: FuntimeClientConfig) {
    this.baseUrl = config.baseUrl.replace(/\/$/, '');
    this.getToken = config.getToken || (() => null);
    this.onUnauthorized = config.onUnauthorized || (() => {});
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const token = this.getToken();
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string>),
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      ...options,
      headers,
    });

    if (response.status === 401) {
      this.onUnauthorized();
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Request failed' }));
      throw new Error(error.message || `HTTP ${response.status}`);
    }

    return response.json();
  }

  // Auth endpoints
  async register(email: string, password: string): Promise<AuthResponse> {
    return this.request('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  }

  async login(email: string, password: string): Promise<AuthResponse> {
    return this.request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  }

  async sendOtp(phoneNumber: string): Promise<ApiResponse> {
    return this.request('/auth/otp/send', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber }),
    });
  }

  async verifyOtp(phoneNumber: string, code: string): Promise<AuthResponse> {
    return this.request('/auth/otp/verify', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber, code }),
    });
  }

  async validateToken(token: string): Promise<ValidateTokenResponse> {
    return this.request('/auth/validate', {
      method: 'POST',
      body: JSON.stringify({ token }),
    });
  }

  async getCurrentUser(): Promise<User> {
    return this.request('/auth/me');
  }

  async linkPhone(phoneNumber: string, code: string): Promise<AuthResponse> {
    return this.request('/auth/link-phone', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber, code }),
    });
  }

  async linkEmail(email: string, password: string): Promise<AuthResponse> {
    return this.request('/auth/link-email', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  }

  async changePassword(currentPassword: string, newPassword: string): Promise<ApiResponse> {
    return this.request('/auth/change-password', {
      method: 'POST',
      body: JSON.stringify({ currentPassword, newPassword }),
    });
  }

  async resetPassword(phoneNumber: string, code: string, newPassword: string): Promise<ApiResponse> {
    return this.request('/auth/reset-password', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber, code, newPassword }),
    });
  }

  // Profile endpoints
  async getProfile(): Promise<UserProfile> {
    return this.request('/profile');
  }

  async updateProfile(profile: Partial<Omit<UserProfile, 'id' | 'userId' | 'createdAt' | 'updatedAt'>>): Promise<UserProfile> {
    return this.request('/profile', {
      method: 'PUT',
      body: JSON.stringify(profile),
    });
  }

  async getFullProfile(): Promise<UserFull> {
    return this.request('/profile/full');
  }

  // Sites endpoints
  async getMySites(): Promise<UserSite[]> {
    return this.request('/sites');
  }

  async joinSite(siteKey: string): Promise<UserSite> {
    return this.request('/sites/join', {
      method: 'POST',
      body: JSON.stringify({ siteKey }),
    });
  }

  async leaveSite(siteKey: string): Promise<ApiResponse> {
    return this.request('/sites/leave', {
      method: 'POST',
      body: JSON.stringify({ siteKey }),
    });
  }

  async checkSiteMembership(siteKey: string): Promise<UserSite | null> {
    return this.request(`/sites/check/${siteKey}`);
  }

  // Payment endpoints
  async createSetupIntent(): Promise<SetupIntentResponse> {
    return this.request('/payments/setup-intent', { method: 'POST' });
  }

  async getPaymentMethods(): Promise<PaymentMethod[]> {
    return this.request('/payments/payment-methods');
  }

  async attachPaymentMethod(stripePaymentMethodId: string, setAsDefault = false): Promise<PaymentMethod> {
    return this.request('/payments/payment-methods', {
      method: 'POST',
      body: JSON.stringify({ stripePaymentMethodId, setAsDefault }),
    });
  }

  async setDefaultPaymentMethod(stripePaymentMethodId: string): Promise<ApiResponse> {
    return this.request('/payments/payment-methods/default', {
      method: 'POST',
      body: JSON.stringify({ stripePaymentMethodId }),
    });
  }

  async deletePaymentMethod(stripePaymentMethodId: string): Promise<ApiResponse> {
    return this.request(`/payments/payment-methods/${stripePaymentMethodId}`, {
      method: 'DELETE',
    });
  }

  async createPayment(amountCents: number, description?: string, siteKey?: string): Promise<PaymentIntentResponse> {
    return this.request('/payments/create-payment', {
      method: 'POST',
      body: JSON.stringify({ amountCents, description, siteKey }),
    });
  }

  async getPaymentHistory(limit = 20): Promise<Payment[]> {
    return this.request(`/payments/history?limit=${limit}`);
  }

  async getSubscriptions(): Promise<Subscription[]> {
    return this.request('/payments/subscriptions');
  }

  async createSubscription(stripePriceId: string, siteKey?: string): Promise<Subscription> {
    return this.request('/payments/subscriptions', {
      method: 'POST',
      body: JSON.stringify({ stripePriceId, siteKey }),
    });
  }

  async cancelSubscription(subscriptionId: number, cancelAtPeriodEnd = true): Promise<Subscription> {
    return this.request('/payments/subscriptions/cancel', {
      method: 'POST',
      body: JSON.stringify({ subscriptionId, cancelAtPeriodEnd }),
    });
  }

  async resumeSubscription(subscriptionId: number): Promise<Subscription> {
    return this.request('/payments/subscriptions/resume', {
      method: 'POST',
      body: JSON.stringify({ subscriptionId }),
    });
  }

  async createPaymentWithMethod(
    paymentMethodId: string,
    amountCents: number,
    currency: string,
    description?: string,
    siteKey?: string
  ): Promise<Payment> {
    return this.request('/payments/charge', {
      method: 'POST',
      body: JSON.stringify({ paymentMethodId, amountCents, currency, description, siteKey }),
    });
  }
}

// Default singleton for simple usage
let defaultClient: FuntimeClient | null = null;

export function initFuntimeClient(config: FuntimeClientConfig): FuntimeClient {
  globalConfig = config;
  defaultClient = new FuntimeClient(config);
  return defaultClient;
}

// Alias for initFuntimeClient (convenience)
export const setFuntimeClient = initFuntimeClient;

export function getFuntimeClient(): FuntimeClient {
  if (!defaultClient) {
    throw new Error('FuntimeClient not initialized. Call initFuntimeClient or setFuntimeClient first.');
  }
  return defaultClient;
}
