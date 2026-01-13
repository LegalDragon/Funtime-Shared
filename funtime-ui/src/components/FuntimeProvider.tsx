import React, { useEffect, useState } from 'react';
import { loadStripe, Stripe } from '@stripe/stripe-js';
import { Elements } from '@stripe/react-stripe-js';
import { initFuntimeClient, getConfig, type FuntimeClientConfig } from '../api/client';

export interface FuntimeProviderProps extends FuntimeClientConfig {
  children: React.ReactNode;
}

export const FuntimeProvider: React.FC<FuntimeProviderProps> = ({
  children,
  baseUrl,
  stripePublishableKey,
  getToken,
  onUnauthorized,
}) => {
  const [stripePromise, setStripePromise] = useState<Promise<Stripe | null> | null>(null);

  // Initialize the API client
  useEffect(() => {
    initFuntimeClient({
      baseUrl,
      stripePublishableKey,
      getToken,
      onUnauthorized,
    });
  }, [baseUrl, stripePublishableKey, getToken, onUnauthorized]);

  // Initialize Stripe if key is provided
  useEffect(() => {
    if (stripePublishableKey) {
      setStripePromise(loadStripe(stripePublishableKey));
    }
  }, [stripePublishableKey]);

  // If Stripe key is provided, wrap with Elements
  if (stripePublishableKey && stripePromise) {
    return (
      <Elements stripe={stripePromise}>
        {children}
      </Elements>
    );
  }

  // No Stripe, just render children
  return <>{children}</>;
};

// Hook to get Stripe key from config (for components that need it)
export function useStripeKey(): string | undefined {
  const config = getConfig();
  return config?.stripePublishableKey;
}
