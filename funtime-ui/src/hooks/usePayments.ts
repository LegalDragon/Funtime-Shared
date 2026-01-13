import { useState, useCallback, useEffect } from 'react';
import type { PaymentMethod, Payment, Subscription } from '../types';
import { getFuntimeClient } from '../api/client';

export interface CreatePaymentOptions {
  paymentMethodId: string;
  amount: number;
  currency?: string;
  description?: string;
  siteKey?: string;
}

export interface SubscribeOptions {
  priceId: string;
  siteKey?: string;
}

export interface UsePaymentsReturn {
  paymentMethods: PaymentMethod[];
  payments: Payment[];
  subscriptions: Subscription[];
  isLoading: boolean;
  error: string | null;
  // Setup & Payment Methods
  createSetupIntent: () => Promise<string>;
  addPaymentMethod: (stripePaymentMethodId: string, setAsDefault?: boolean) => Promise<PaymentMethod>;
  attachPaymentMethod: (stripePaymentMethodId: string, setAsDefault?: boolean) => Promise<PaymentMethod>;
  removePaymentMethod: (paymentMethodId: number) => Promise<void>;
  deletePaymentMethod: (stripePaymentMethodId: string) => Promise<void>;
  setDefaultPaymentMethod: (stripePaymentMethodId: string) => Promise<void>;
  // Payments
  createPayment: (options: CreatePaymentOptions) => Promise<Payment>;
  createPaymentIntent: (amountCents: number, description?: string, siteKey?: string) => Promise<{ clientSecret: string; paymentIntentId: string }>;
  // Subscriptions
  subscribe: (options: SubscribeOptions) => Promise<Subscription>;
  createSubscription: (stripePriceId: string, siteKey?: string) => Promise<Subscription>;
  cancelSubscription: (subscriptionId: number, cancelAtPeriodEnd?: boolean) => Promise<Subscription>;
  resumeSubscription: (subscriptionId: number) => Promise<Subscription>;
  refresh: () => Promise<void>;
}

export function usePayments(): UsePaymentsReturn {
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [subscriptions, setSubscriptions] = useState<Subscription[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const client = getFuntimeClient();
      const [methods, paymentHistory, subs] = await Promise.all([
        client.getPaymentMethods(),
        client.getPaymentHistory(),
        client.getSubscriptions(),
      ]);
      setPaymentMethods(methods);
      setPayments(paymentHistory);
      setSubscriptions(subs);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load payment data');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const createSetupIntent = useCallback(async (): Promise<string> => {
    const client = getFuntimeClient();
    const response = await client.createSetupIntent();
    return response.clientSecret;
  }, []);

  const attachPaymentMethod = useCallback(async (stripePaymentMethodId: string, setAsDefault = false): Promise<PaymentMethod> => {
    const client = getFuntimeClient();
    const method = await client.attachPaymentMethod(stripePaymentMethodId, setAsDefault);
    setPaymentMethods(prev => {
      if (setAsDefault) {
        return [...prev.map(m => ({ ...m, isDefault: false })), method];
      }
      return [...prev, method];
    });
    return method;
  }, []);

  const deletePaymentMethod = useCallback(async (stripePaymentMethodId: string): Promise<void> => {
    const client = getFuntimeClient();
    await client.deletePaymentMethod(stripePaymentMethodId);
    setPaymentMethods(prev => prev.filter(m => m.stripePaymentMethodId !== stripePaymentMethodId));
  }, []);

  const setDefaultPaymentMethod = useCallback(async (stripePaymentMethodId: string): Promise<void> => {
    const client = getFuntimeClient();
    await client.setDefaultPaymentMethod(stripePaymentMethodId);
    setPaymentMethods(prev => prev.map(m => ({
      ...m,
      isDefault: m.stripePaymentMethodId === stripePaymentMethodId,
    })));
  }, []);

  // Alias for attachPaymentMethod (convenience)
  const addPaymentMethod = attachPaymentMethod;

  // Remove payment method by internal ID
  const removePaymentMethod = useCallback(async (paymentMethodId: number): Promise<void> => {
    const method = paymentMethods.find(m => m.paymentMethodId === paymentMethodId);
    if (method) {
      await deletePaymentMethod(method.stripePaymentMethodId);
    }
  }, [paymentMethods, deletePaymentMethod]);

  // Create payment intent (for custom flows)
  const createPaymentIntent = useCallback(async (amountCents: number, description?: string, siteKey?: string) => {
    const client = getFuntimeClient();
    const response = await client.createPayment(amountCents, description, siteKey);
    return { clientSecret: response.clientSecret, paymentIntentId: response.paymentIntentId };
  }, []);

  // Create payment (simplified API for components)
  const createPayment = useCallback(async (options: CreatePaymentOptions): Promise<Payment> => {
    const client = getFuntimeClient();
    const response = await client.createPaymentWithMethod(
      options.paymentMethodId,
      options.amount,
      options.currency || 'usd',
      options.description,
      options.siteKey
    );
    setPayments(prev => [...prev, response]);
    return response;
  }, []);

  const createSubscription = useCallback(async (stripePriceId: string, siteKey?: string): Promise<Subscription> => {
    const client = getFuntimeClient();
    const sub = await client.createSubscription(stripePriceId, siteKey);
    setSubscriptions(prev => [...prev, sub]);
    return sub;
  }, []);

  // Alias for createSubscription (convenience)
  const subscribe = useCallback(async (options: SubscribeOptions): Promise<Subscription> => {
    return createSubscription(options.priceId, options.siteKey);
  }, [createSubscription]);

  const cancelSubscription = useCallback(async (subscriptionId: number, cancelAtPeriodEnd = true): Promise<Subscription> => {
    const client = getFuntimeClient();
    const sub = await client.cancelSubscription(subscriptionId, cancelAtPeriodEnd);
    setSubscriptions(prev => prev.map(s => s.subscriptionId === subscriptionId ? sub : s));
    return sub;
  }, []);

  const resumeSubscription = useCallback(async (subscriptionId: number): Promise<Subscription> => {
    const client = getFuntimeClient();
    const sub = await client.resumeSubscription(subscriptionId);
    setSubscriptions(prev => prev.map(s => s.subscriptionId === subscriptionId ? sub : s));
    return sub;
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  return {
    paymentMethods,
    payments,
    subscriptions,
    isLoading,
    error,
    createSetupIntent,
    addPaymentMethod,
    attachPaymentMethod,
    removePaymentMethod,
    deletePaymentMethod,
    setDefaultPaymentMethod,
    createPayment,
    createPaymentIntent,
    subscribe,
    createSubscription,
    cancelSubscription,
    resumeSubscription,
    refresh,
  };
}
