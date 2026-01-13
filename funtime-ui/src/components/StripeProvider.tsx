import React, { createContext, useContext, useMemo } from 'react';
import { loadStripe, Stripe } from '@stripe/stripe-js';
import { Elements } from '@stripe/react-stripe-js';

interface StripeContextValue {
  publishableKey: string;
}

const StripeContext = createContext<StripeContextValue | null>(null);

export interface StripeProviderProps {
  publishableKey: string;
  children: React.ReactNode;
}

export const StripeProvider: React.FC<StripeProviderProps> = ({
  publishableKey,
  children,
}) => {
  const stripePromise = useMemo(() => loadStripe(publishableKey), [publishableKey]);

  return (
    <StripeContext.Provider value={{ publishableKey }}>
      <Elements stripe={stripePromise}>
        {children}
      </Elements>
    </StripeContext.Provider>
  );
};

export const useStripeContext = () => {
  const context = useContext(StripeContext);
  if (!context) {
    throw new Error('useStripeContext must be used within a StripeProvider');
  }
  return context;
};
