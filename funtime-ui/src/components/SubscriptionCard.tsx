import React from 'react';
import type { Subscription, SiteKey } from '../types';
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from './Card';
import { Button } from './Button';
import { SiteBadge } from './SiteBadge';

export interface SubscriptionCardProps {
  subscription: Subscription;
  onCancel?: (subscriptionId: number) => void;
  onResume?: (subscriptionId: number) => void;
  className?: string;
}

const statusColors: Record<string, string> = {
  active: 'bg-green-100 text-green-700',
  canceled: 'bg-gray-100 text-gray-700',
  past_due: 'bg-red-100 text-red-700',
  trialing: 'bg-blue-100 text-blue-700',
  incomplete: 'bg-yellow-100 text-yellow-700',
};

export const SubscriptionCard: React.FC<SubscriptionCardProps> = ({
  subscription,
  onCancel,
  onResume,
  className = '',
}) => {
  const formattedAmount = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: subscription.currency.toUpperCase(),
  }).format(subscription.amount / 100);

  const statusColor = statusColors[subscription.status] || 'bg-gray-100 text-gray-700';

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>{subscription.planName}</CardTitle>
          <span className={`text-xs font-medium px-2 py-1 rounded-full ${statusColor}`}>
            {subscription.status.replace('_', ' ')}
          </span>
        </div>
      </CardHeader>

      <CardContent>
        <div className="space-y-3">
          <div className="flex justify-between items-center">
            <span className="text-gray-600">Price</span>
            <span className="font-semibold">
              {formattedAmount}/{subscription.interval}
            </span>
          </div>

          {subscription.siteKey && (
            <div className="flex justify-between items-center">
              <span className="text-gray-600">Site</span>
              <SiteBadge siteKey={subscription.siteKey as SiteKey} size="sm" />
            </div>
          )}

          <div className="flex justify-between items-center">
            <span className="text-gray-600">Started</span>
            <span className="text-sm">{formatDate(subscription.startDate)}</span>
          </div>

          {subscription.currentPeriodEnd && (
            <div className="flex justify-between items-center">
              <span className="text-gray-600">
                {subscription.status === 'canceled' ? 'Access until' : 'Next billing'}
              </span>
              <span className="text-sm">{formatDate(subscription.currentPeriodEnd)}</span>
            </div>
          )}
        </div>
      </CardContent>

      {(onCancel || onResume) && (
        <CardFooter>
          <div className="flex gap-2 justify-end">
            {subscription.status === 'active' && onCancel && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => onCancel(subscription.subscriptionId)}
              >
                Cancel Subscription
              </Button>
            )}
            {subscription.status === 'canceled' && onResume && (
              <Button
                variant="primary"
                size="sm"
                onClick={() => onResume(subscription.subscriptionId)}
              >
                Resume Subscription
              </Button>
            )}
          </div>
        </CardFooter>
      )}
    </Card>
  );
};

export interface SubscriptionListProps {
  subscriptions: Subscription[];
  onCancel?: (subscriptionId: number) => void;
  onResume?: (subscriptionId: number) => void;
  emptyMessage?: string;
  className?: string;
}

export const SubscriptionList: React.FC<SubscriptionListProps> = ({
  subscriptions,
  onCancel,
  onResume,
  emptyMessage = 'No active subscriptions',
  className = '',
}) => {
  if (subscriptions.length === 0) {
    return (
      <div className={`text-center py-8 text-gray-500 ${className}`}>
        {emptyMessage}
      </div>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {subscriptions.map((sub) => (
        <SubscriptionCard
          key={sub.subscriptionId}
          subscription={sub}
          onCancel={onCancel}
          onResume={onResume}
        />
      ))}
    </div>
  );
};

export interface PricingCardProps {
  name: string;
  price: number;
  currency?: string;
  interval: 'month' | 'year';
  features: string[];
  priceId: string;
  isPopular?: boolean;
  onSubscribe: (priceId: string) => void;
  isLoading?: boolean;
  className?: string;
}

export const PricingCard: React.FC<PricingCardProps> = ({
  name,
  price,
  currency = 'usd',
  interval,
  features,
  priceId,
  isPopular = false,
  onSubscribe,
  isLoading = false,
  className = '',
}) => {
  const formattedPrice = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency.toUpperCase(),
    minimumFractionDigits: 0,
  }).format(price / 100);

  return (
    <Card className={`relative ${isPopular ? 'ring-2 ring-green-500' : ''} ${className}`}>
      {isPopular && (
        <div className="absolute -top-3 left-1/2 transform -translate-x-1/2">
          <span className="bg-green-500 text-white text-xs font-bold px-3 py-1 rounded-full">
            Most Popular
          </span>
        </div>
      )}

      <CardHeader>
        <CardTitle>{name}</CardTitle>
      </CardHeader>

      <CardContent>
        <div className="mb-6">
          <span className="text-4xl font-bold">{formattedPrice}</span>
          <span className="text-gray-500">/{interval}</span>
        </div>

        <ul className="space-y-3 mb-6">
          {features.map((feature, index) => (
            <li key={index} className="flex items-start gap-2">
              <span className="text-green-500 mt-0.5">✓</span>
              <span className="text-gray-600">{feature}</span>
            </li>
          ))}
        </ul>

        <Button
          fullWidth
          variant={isPopular ? 'primary' : 'outline'}
          onClick={() => onSubscribe(priceId)}
          isLoading={isLoading}
        >
          Subscribe
        </Button>
      </CardContent>
    </Card>
  );
};
