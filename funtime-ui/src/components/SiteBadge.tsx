import React from 'react';
import type { SiteKey } from '../types';

export interface SiteBadgeProps {
  siteKey: SiteKey;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

const siteInfo: Record<SiteKey, { name: string; color: string; icon: string }> = {
  community: { name: 'Community', color: 'bg-blue-500', icon: 'users' },
  college: { name: 'College', color: 'bg-orange-500', icon: 'graduation-cap' },
  date: { name: 'Date', color: 'bg-pink-500', icon: 'heart' },
  jobs: { name: 'Jobs', color: 'bg-green-500', icon: 'briefcase' },
};

export const SiteBadge: React.FC<SiteBadgeProps> = ({
  siteKey,
  size = 'md',
  className = '',
}) => {
  const info = siteInfo[siteKey];

  const sizeStyles = {
    sm: 'text-xs px-2 py-0.5',
    md: 'text-sm px-3 py-1',
    lg: 'text-base px-4 py-1.5',
  };

  return (
    <span
      className={`inline-flex items-center rounded-full text-white font-medium ${info.color} ${sizeStyles[size]} ${className}`}
    >
      pickleball.{siteKey}
    </span>
  );
};

export interface SiteListProps {
  sites: SiteKey[];
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

export const SiteList: React.FC<SiteListProps> = ({
  sites,
  size = 'sm',
  className = '',
}) => (
  <div className={`flex flex-wrap gap-1 ${className}`}>
    {sites.map(siteKey => (
      <SiteBadge key={siteKey} siteKey={siteKey} size={size} />
    ))}
  </div>
);
