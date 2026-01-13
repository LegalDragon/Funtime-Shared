import { useState, useEffect } from 'react';
import { loadStripe } from '@stripe/stripe-js';
import type { Stripe } from '@stripe/stripe-js';
import {
  Elements,
  PaymentElement,
  useStripe,
  useElements,
} from '@stripe/react-stripe-js';
import { X, CreditCard, Loader2, Check, AlertCircle } from 'lucide-react';

// Stripe promise - will be initialized when needed
let stripePromise: Promise<Stripe | null> | null = null;

const getStripe = (publishableKey: string) => {
  if (!stripePromise) {
    stripePromise = loadStripe(publishableKey);
  }
  return stripePromise;
};

export interface PaymentMethod {
  id: number;
  stripePaymentMethodId: string;
  type: string;
  cardBrand?: string;
  cardLast4?: string;
  cardExpMonth?: number;
  cardExpYear?: number;
  isDefault: boolean;
}

export interface PaymentModalProps {
  isOpen: boolean;
  onClose: () => void;
  clientSecret: string;
  stripePublishableKey: string;
  amountCents: number;
  currency?: string;
  description?: string;
  savedPaymentMethods?: PaymentMethod[];
  onPaymentSuccess: (paymentIntentId: string) => void;
  onPaymentError?: (error: string) => void;
}

function PaymentForm({
  clientSecret,
  amountCents,
  currency = 'usd',
  description,
  savedPaymentMethods,
  onPaymentSuccess,
  onPaymentError,
  onClose,
}: Omit<PaymentModalProps, 'isOpen' | 'stripePublishableKey'>) {
  const stripe = useStripe();
  const elements = useElements();
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [selectedSavedMethod, setSelectedSavedMethod] = useState<string | null>(null);
  const [useNewCard, setUseNewCard] = useState(!savedPaymentMethods?.length);

  const formatAmount = (cents: number, curr: string) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: curr.toUpperCase(),
    }).format(cents / 100);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!stripe) {
      setError('Stripe not loaded');
      return;
    }

    setIsProcessing(true);
    setError(null);

    try {
      let result;

      if (useNewCard) {
        if (!elements) {
          setError('Payment form not loaded');
          setIsProcessing(false);
          return;
        }

        result = await stripe.confirmPayment({
          elements,
          confirmParams: {
            return_url: window.location.href,
          },
          redirect: 'if_required',
        });
      } else if (selectedSavedMethod) {
        result = await stripe.confirmCardPayment(clientSecret, {
          payment_method: selectedSavedMethod,
        });
      } else {
        setError('Please select a payment method');
        setIsProcessing(false);
        return;
      }

      if (result.error) {
        setError(result.error.message || 'Payment failed');
        onPaymentError?.(result.error.message || 'Payment failed');
      } else if (result.paymentIntent?.status === 'succeeded') {
        setSuccess(true);
        onPaymentSuccess(result.paymentIntent.id);
      } else if (result.paymentIntent?.status === 'processing') {
        setSuccess(true);
        onPaymentSuccess(result.paymentIntent.id);
      } else {
        setError(`Unexpected status: ${result.paymentIntent?.status}`);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Payment failed';
      setError(message);
      onPaymentError?.(message);
    } finally {
      setIsProcessing(false);
    }
  };

  if (success) {
    return (
      <div className="text-center py-8">
        <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <Check className="w-8 h-8 text-green-600" />
        </div>
        <h3 className="text-xl font-semibold text-gray-900 mb-2">Payment Successful!</h3>
        <p className="text-gray-600 mb-6">{formatAmount(amountCents, currency)} has been charged.</p>
        <button
          onClick={onClose}
          className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          Close
        </button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Amount Display */}
      <div className="bg-gray-50 rounded-lg p-4 text-center">
        <p className="text-sm text-gray-500">Amount to charge</p>
        <p className="text-3xl font-bold text-gray-900">{formatAmount(amountCents, currency)}</p>
        {description && <p className="text-sm text-gray-600 mt-1">{description}</p>}
      </div>

      {/* Saved Payment Methods */}
      {savedPaymentMethods && savedPaymentMethods.length > 0 && (
        <div className="space-y-3">
          <label className="block text-sm font-medium text-gray-700">Saved Payment Methods</label>
          <div className="space-y-2">
            {savedPaymentMethods.map((pm) => (
              <label
                key={pm.stripePaymentMethodId}
                className={`flex items-center p-3 border rounded-lg cursor-pointer transition-colors ${
                  !useNewCard && selectedSavedMethod === pm.stripePaymentMethodId
                    ? 'border-blue-500 bg-blue-50'
                    : 'border-gray-200 hover:border-gray-300'
                }`}
              >
                <input
                  type="radio"
                  name="paymentMethod"
                  checked={!useNewCard && selectedSavedMethod === pm.stripePaymentMethodId}
                  onChange={() => {
                    setUseNewCard(false);
                    setSelectedSavedMethod(pm.stripePaymentMethodId);
                  }}
                  className="h-4 w-4 text-blue-600"
                />
                <CreditCard className="w-5 h-5 ml-3 text-gray-400" />
                <span className="ml-3 flex-1">
                  <span className="font-medium capitalize">{pm.cardBrand}</span>
                  <span className="text-gray-500"> •••• {pm.cardLast4}</span>
                </span>
                <span className="text-sm text-gray-500">
                  {pm.cardExpMonth}/{pm.cardExpYear}
                </span>
                {pm.isDefault && (
                  <span className="ml-2 px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded">
                    Default
                  </span>
                )}
              </label>
            ))}
          </div>

          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-200" />
            </div>
            <div className="relative flex justify-center text-sm">
              <span className="px-2 bg-white text-gray-500">or</span>
            </div>
          </div>

          <label
            className={`flex items-center p-3 border rounded-lg cursor-pointer transition-colors ${
              useNewCard ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-gray-300'
            }`}
          >
            <input
              type="radio"
              name="paymentMethod"
              checked={useNewCard}
              onChange={() => setUseNewCard(true)}
              className="h-4 w-4 text-blue-600"
            />
            <CreditCard className="w-5 h-5 ml-3 text-gray-400" />
            <span className="ml-3 font-medium">Use a new card</span>
          </label>
        </div>
      )}

      {/* New Card Form */}
      {useNewCard && (
        <div className="space-y-3">
          <label className="block text-sm font-medium text-gray-700">Card Details</label>
          <div className="border border-gray-200 rounded-lg p-4">
            <PaymentElement
              options={{
                layout: 'tabs',
              }}
            />
          </div>
        </div>
      )}

      {/* Error Display */}
      {error && (
        <div className="flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700">
          <AlertCircle className="w-5 h-5 flex-shrink-0" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      {/* Submit Button */}
      <button
        type="submit"
        disabled={isProcessing || !stripe}
        className="w-full py-3 px-4 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
      >
        {isProcessing ? (
          <>
            <Loader2 className="w-5 h-5 animate-spin" />
            Processing...
          </>
        ) : (
          <>
            <CreditCard className="w-5 h-5" />
            Pay {formatAmount(amountCents, currency)}
          </>
        )}
      </button>
    </form>
  );
}

export function PaymentModal({
  isOpen,
  onClose,
  clientSecret,
  stripePublishableKey,
  amountCents,
  currency = 'usd',
  description,
  savedPaymentMethods,
  onPaymentSuccess,
  onPaymentError,
}: PaymentModalProps) {
  const [stripeLoaded, setStripeLoaded] = useState(false);

  useEffect(() => {
    if (isOpen && stripePublishableKey) {
      getStripe(stripePublishableKey).then(() => setStripeLoaded(true));
    }
  }, [isOpen, stripePublishableKey]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-xl max-w-md w-full max-h-[90vh] overflow-y-auto">
        <div className="p-4 border-b border-gray-200 flex items-center justify-between">
          <h3 className="text-lg font-semibold text-gray-900">Complete Payment</h3>
          <button
            onClick={onClose}
            className="p-1 hover:bg-gray-100 rounded-lg transition-colors"
          >
            <X className="w-5 h-5 text-gray-500" />
          </button>
        </div>

        <div className="p-6">
          {stripeLoaded && clientSecret ? (
            <Elements
              stripe={getStripe(stripePublishableKey)}
              options={{
                clientSecret,
                appearance: {
                  theme: 'stripe',
                  variables: {
                    colorPrimary: '#2563eb',
                    borderRadius: '8px',
                  },
                },
              }}
            >
              <PaymentForm
                clientSecret={clientSecret}
                amountCents={amountCents}
                currency={currency}
                description={description}
                savedPaymentMethods={savedPaymentMethods}
                onPaymentSuccess={onPaymentSuccess}
                onPaymentError={onPaymentError}
                onClose={onClose}
              />
            </Elements>
          ) : (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="w-8 h-8 animate-spin text-gray-400" />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default PaymentModal;
