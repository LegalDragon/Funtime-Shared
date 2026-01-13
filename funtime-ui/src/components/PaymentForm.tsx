import React, { useState } from 'react';
import { CardElement, useStripe, useElements } from '@stripe/react-stripe-js';
import { Button } from './Button';
import { usePayments } from '../hooks/usePayments';

export interface PaymentFormProps {
  onSuccess?: (paymentMethodId: string) => void;
  onError?: (error: string) => void;
  buttonText?: string;
  className?: string;
}

const cardElementOptions = {
  style: {
    base: {
      fontSize: '16px',
      color: '#374151',
      fontFamily: 'system-ui, -apple-system, sans-serif',
      '::placeholder': {
        color: '#9CA3AF',
      },
    },
    invalid: {
      color: '#EF4444',
      iconColor: '#EF4444',
    },
  },
};

export const PaymentForm: React.FC<PaymentFormProps> = ({
  onSuccess,
  onError,
  buttonText = 'Save Card',
  className = '',
}) => {
  const stripe = useStripe();
  const elements = useElements();
  const { addPaymentMethod } = usePayments();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!stripe || !elements) {
      return;
    }

    setIsLoading(true);
    setError(null);

    const cardElement = elements.getElement(CardElement);
    if (!cardElement) {
      setError('Card element not found');
      setIsLoading(false);
      return;
    }

    try {
      const { error: stripeError, paymentMethod } = await stripe.createPaymentMethod({
        type: 'card',
        card: cardElement,
      });

      if (stripeError) {
        setError(stripeError.message || 'An error occurred');
        onError?.(stripeError.message || 'An error occurred');
        setIsLoading(false);
        return;
      }

      if (paymentMethod) {
        await addPaymentMethod(paymentMethod.id);
        cardElement.clear();
        onSuccess?.(paymentMethod.id);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to add payment method';
      setError(message);
      onError?.(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={className}>
      <div className="mb-4">
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Card Details
        </label>
        <div className="border border-gray-300 rounded-lg p-3 focus-within:ring-2 focus-within:ring-green-500 focus-within:border-green-500">
          <CardElement options={cardElementOptions} />
        </div>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {error}
        </div>
      )}

      <Button
        type="submit"
        disabled={!stripe || isLoading}
        isLoading={isLoading}
        fullWidth
      >
        {buttonText}
      </Button>
    </form>
  );
};

export interface QuickPaymentFormProps {
  amount: number;
  currency?: string;
  description?: string;
  onSuccess?: (paymentIntentId: string) => void;
  onError?: (error: string) => void;
  className?: string;
}

export const QuickPaymentForm: React.FC<QuickPaymentFormProps> = ({
  amount,
  currency = 'usd',
  description,
  onSuccess,
  onError,
  className = '',
}) => {
  const stripe = useStripe();
  const elements = useElements();
  const { createPayment } = usePayments();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!stripe || !elements) {
      return;
    }

    setIsLoading(true);
    setError(null);

    const cardElement = elements.getElement(CardElement);
    if (!cardElement) {
      setError('Card element not found');
      setIsLoading(false);
      return;
    }

    try {
      const { error: stripeError, paymentMethod } = await stripe.createPaymentMethod({
        type: 'card',
        card: cardElement,
      });

      if (stripeError) {
        setError(stripeError.message || 'An error occurred');
        onError?.(stripeError.message || 'An error occurred');
        setIsLoading(false);
        return;
      }

      if (paymentMethod) {
        const payment = await createPayment({
          paymentMethodId: paymentMethod.id,
          amount,
          currency,
          description,
        });
        cardElement.clear();
        onSuccess?.(payment.stripePaymentIntentId);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Payment failed';
      setError(message);
      onError?.(message);
    } finally {
      setIsLoading(false);
    }
  };

  const formattedAmount = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency.toUpperCase(),
  }).format(amount / 100);

  return (
    <form onSubmit={handleSubmit} className={className}>
      <div className="mb-4">
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Card Details
        </label>
        <div className="border border-gray-300 rounded-lg p-3 focus-within:ring-2 focus-within:ring-green-500 focus-within:border-green-500">
          <CardElement options={cardElementOptions} />
        </div>
      </div>

      {description && (
        <p className="mb-4 text-sm text-gray-600">{description}</p>
      )}

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {error}
        </div>
      )}

      <Button
        type="submit"
        disabled={!stripe || isLoading}
        isLoading={isLoading}
        fullWidth
      >
        Pay {formattedAmount}
      </Button>
    </form>
  );
};
