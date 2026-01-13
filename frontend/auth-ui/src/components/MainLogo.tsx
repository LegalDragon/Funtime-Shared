import { useState, useEffect } from 'react';
import { settingsApi } from '../utils/api';

interface MainLogoProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

const sizeClasses = {
  sm: 'w-12 h-12',
  md: 'w-16 h-16',
  lg: 'w-20 h-20',
};

const imageSizeClasses = {
  sm: 'h-12',
  md: 'h-16',
  lg: 'h-20',
};

const fallbackTextSizes = {
  sm: 'text-xl',
  md: 'text-2xl',
  lg: 'text-3xl',
};

export function MainLogo({ size = 'md', className = '' }: MainLogoProps) {
  const [logoUrl, setLogoUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    loadLogo();
  }, []);

  const loadLogo = async () => {
    try {
      const response = await settingsApi.getMainLogo();
      if (response.hasLogo && response.logoUrl) {
        setLogoUrl(settingsApi.getLogoDisplayUrl(response.logoUrl));
      }
    } catch (error) {
      console.error('Failed to load main logo:', error);
    } finally {
      setIsLoading(false);
    }
  };

  // Loading state
  if (isLoading) {
    return (
      <div className={`${sizeClasses[size]} bg-gray-200 rounded-2xl animate-pulse ${className}`} />
    );
  }

  // Show logo if available
  if (logoUrl && !hasError) {
    return (
      <img
        src={logoUrl}
        alt="Logo"
        className={`${imageSizeClasses[size]} w-auto max-w-[200px] object-contain ${className}`}
        onError={() => setHasError(true)}
      />
    );
  }

  // Fallback to "F" icon
  return (
    <div className={`inline-flex items-center justify-center ${sizeClasses[size]} bg-gradient-to-br from-primary-500 to-primary-700 rounded-2xl shadow-soft ${className}`}>
      <span className={`text-white ${fallbackTextSizes[size]} font-bold`}>F</span>
    </div>
  );
}
