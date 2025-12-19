import React from 'react';
import type { PaymentMethod } from '../types';
import { Card } from './Card';
import { Button } from './Button';

export interface PaymentMethodCardProps {
  paymentMethod: PaymentMethod;
  isDefault?: boolean;
  onSetDefault?: (paymentMethodId: number) => void;
  onRemove?: (paymentMethodId: number) => void;
  className?: string;
}

const cardIcons: Record<string, string> = {
  visa: '💳',
  mastercard: '💳',
  amex: '💳',
  discover: '💳',
  default: '💳',
};

export const PaymentMethodCard: React.FC<PaymentMethodCardProps> = ({
  paymentMethod,
  isDefault = false,
  onSetDefault,
  onRemove,
  className = '',
}) => {
  const icon = cardIcons[paymentMethod.brand.toLowerCase()] || cardIcons.default;

  return (
    <Card className={`flex items-center justify-between ${className}`} padding="md">
      <div className="flex items-center gap-3">
        <span className="text-2xl">{icon}</span>
        <div>
          <div className="font-medium text-gray-900">
            {paymentMethod.brand} •••• {paymentMethod.last4}
            {isDefault && (
              <span className="ml-2 text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full">
                Default
              </span>
            )}
          </div>
          <div className="text-sm text-gray-500">
            Expires {paymentMethod.expiryMonth}/{paymentMethod.expiryYear}
          </div>
        </div>
      </div>

      <div className="flex gap-2">
        {!isDefault && onSetDefault && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => onSetDefault(paymentMethod.paymentMethodId)}
          >
            Set Default
          </Button>
        )}
        {onRemove && (
          <Button
            variant="danger"
            size="sm"
            onClick={() => onRemove(paymentMethod.paymentMethodId)}
          >
            Remove
          </Button>
        )}
      </div>
    </Card>
  );
};

export interface PaymentMethodListProps {
  paymentMethods: PaymentMethod[];
  defaultPaymentMethodId?: number;
  onSetDefault?: (paymentMethodId: number) => void;
  onRemove?: (paymentMethodId: number) => void;
  emptyMessage?: string;
  className?: string;
}

export const PaymentMethodList: React.FC<PaymentMethodListProps> = ({
  paymentMethods,
  defaultPaymentMethodId,
  onSetDefault,
  onRemove,
  emptyMessage = 'No payment methods saved',
  className = '',
}) => {
  if (paymentMethods.length === 0) {
    return (
      <div className={`text-center py-8 text-gray-500 ${className}`}>
        {emptyMessage}
      </div>
    );
  }

  return (
    <div className={`space-y-3 ${className}`}>
      {paymentMethods.map((pm) => (
        <PaymentMethodCard
          key={pm.paymentMethodId}
          paymentMethod={pm}
          isDefault={pm.paymentMethodId === defaultPaymentMethodId}
          onSetDefault={onSetDefault}
          onRemove={onRemove}
        />
      ))}
    </div>
  );
};
