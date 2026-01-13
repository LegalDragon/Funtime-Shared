import { useState } from 'react';
import { CheckCircle, AlertCircle } from 'lucide-react';
import { VerificationModal } from './VerificationModal';

interface VerificationBadgeProps {
  type: 'email' | 'phone';
  isVerified: boolean;
  identifier?: string;
  onVerified?: () => void;
  /** If true, badge is not clickable */
  readOnly?: boolean;
  /** Size variant */
  size?: 'sm' | 'md';
}

export function VerificationBadge({
  type,
  isVerified,
  identifier,
  onVerified,
  readOnly = false,
  size = 'sm',
}: VerificationBadgeProps) {
  const [showModal, setShowModal] = useState(false);

  const handleClick = () => {
    if (!isVerified && !readOnly) {
      setShowModal(true);
    }
  };

  const handleVerified = () => {
    onVerified?.();
  };

  const sizeClasses = size === 'sm'
    ? 'text-xs px-2 py-0.5'
    : 'text-sm px-3 py-1';

  const iconSize = size === 'sm' ? 'w-3 h-3' : 'w-4 h-4';

  if (isVerified) {
    return (
      <span className={`inline-flex items-center gap-1 ${sizeClasses} text-green-600 bg-green-100 rounded-full`}>
        <CheckCircle className={iconSize} />
        Verified
      </span>
    );
  }

  return (
    <>
      <button
        onClick={handleClick}
        disabled={readOnly}
        className={`inline-flex items-center gap-1 ${sizeClasses} text-amber-600 bg-amber-100 rounded-full transition-colors ${
          !readOnly ? 'hover:bg-amber-200 cursor-pointer' : 'cursor-default'
        }`}
        title={readOnly ? 'Not verified' : 'Click to verify'}
      >
        <AlertCircle className={iconSize} />
        Not verified
      </button>

      <VerificationModal
        type={type}
        identifier={identifier}
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        onVerified={handleVerified}
      />
    </>
  );
}
