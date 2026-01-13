import { useState, useEffect, useRef } from 'react';
import { X, Loader2, CheckCircle, AlertCircle, Mail, Phone, RefreshCw } from 'lucide-react';
import { verifyApi } from '../utils/api';

interface VerificationModalProps {
  type: 'email' | 'phone';
  identifier?: string; // Optional: display the email/phone being verified
  isOpen: boolean;
  onClose: () => void;
  onVerified: () => void;
}

export function VerificationModal({ type, identifier, isOpen, onClose, onVerified }: VerificationModalProps) {
  const [code, setCode] = useState(['', '', '', '', '', '']);
  const [isLoading, setIsLoading] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [maskedIdentifier, setMaskedIdentifier] = useState<string | null>(null);
  const [resendCooldown, setResendCooldown] = useState(0);
  const [codeSent, setCodeSent] = useState(false);

  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  // Send verification code when modal opens
  useEffect(() => {
    if (isOpen && !codeSent) {
      sendCode();
    }
  }, [isOpen]);

  // Reset state when modal closes
  useEffect(() => {
    if (!isOpen) {
      setCode(['', '', '', '', '', '']);
      setError(null);
      setSuccess(false);
      setCodeSent(false);
      setMaskedIdentifier(null);
    }
  }, [isOpen]);

  // Resend cooldown timer
  useEffect(() => {
    if (resendCooldown > 0) {
      const timer = setTimeout(() => setResendCooldown(resendCooldown - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [resendCooldown]);

  const sendCode = async () => {
    setIsSending(true);
    setError(null);
    try {
      const response = await verifyApi.requestCode(type);
      if (response.success) {
        setMaskedIdentifier(response.maskedIdentifier || null);
        setCodeSent(true);
        setResendCooldown(60);
        // Focus first input
        setTimeout(() => inputRefs.current[0]?.focus(), 100);
      } else {
        setError(response.message);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send verification code');
    } finally {
      setIsSending(false);
    }
  };

  const handleCodeChange = (index: number, value: string) => {
    // Only allow digits
    const digit = value.replace(/\D/g, '').slice(-1);

    const newCode = [...code];
    newCode[index] = digit;
    setCode(newCode);
    setError(null);

    // Auto-advance to next input
    if (digit && index < 5) {
      inputRefs.current[index + 1]?.focus();
    }

    // Auto-submit when all digits entered
    if (digit && index === 5) {
      const fullCode = newCode.join('');
      if (fullCode.length === 6) {
        verifyCode(fullCode);
      }
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent) => {
    if (e.key === 'Backspace' && !code[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handlePaste = (e: React.ClipboardEvent) => {
    e.preventDefault();
    const pasted = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
    if (pasted.length === 6) {
      const newCode = pasted.split('');
      setCode(newCode);
      verifyCode(pasted);
    }
  };

  const verifyCode = async (codeString: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await verifyApi.confirmCode(type, codeString);
      if (response.verified) {
        setSuccess(true);
        // Brief delay to show success state
        setTimeout(() => {
          onVerified();
          onClose();
        }, 1500);
      } else {
        setError(response.message || 'Invalid code');
        setCode(['', '', '', '', '', '']);
        inputRefs.current[0]?.focus();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Verification failed');
      setCode(['', '', '', '', '', '']);
      inputRefs.current[0]?.focus();
    } finally {
      setIsLoading(false);
    }
  };

  if (!isOpen) return null;

  const Icon = type === 'email' ? Mail : Phone;
  const typeLabel = type === 'email' ? 'Email' : 'Phone';

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl shadow-xl max-w-md w-full mx-4 overflow-hidden">
        {/* Header */}
        <div className="p-6 border-b border-gray-200 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary-100 rounded-lg">
              <Icon className="w-5 h-5 text-primary-600" />
            </div>
            <h3 className="text-lg font-semibold text-gray-900">Verify Your {typeLabel}</h3>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6">
          {success ? (
            // Success state
            <div className="text-center py-8">
              <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <CheckCircle className="w-8 h-8 text-green-600" />
              </div>
              <h4 className="text-xl font-semibold text-gray-900 mb-2">{typeLabel} Verified!</h4>
              <p className="text-gray-600">Your {type} has been successfully verified.</p>
            </div>
          ) : isSending ? (
            // Sending code state
            <div className="text-center py-8">
              <Loader2 className="w-8 h-8 text-primary-600 animate-spin mx-auto mb-4" />
              <p className="text-gray-600">Sending verification code...</p>
            </div>
          ) : (
            // Code entry state
            <>
              <p className="text-center text-gray-600 mb-6">
                We sent a 6-digit code to{' '}
                <span className="font-medium text-gray-900">
                  {maskedIdentifier || identifier || `your ${type}`}
                </span>
              </p>

              {/* Code input */}
              <div className="flex justify-center gap-2 mb-6">
                {code.map((digit, index) => (
                  <input
                    key={index}
                    ref={(el) => { inputRefs.current[index] = el; }}
                    type="text"
                    inputMode="numeric"
                    maxLength={1}
                    value={digit}
                    onChange={(e) => handleCodeChange(index, e.target.value)}
                    onKeyDown={(e) => handleKeyDown(index, e)}
                    onPaste={index === 0 ? handlePaste : undefined}
                    disabled={isLoading}
                    className={`w-12 h-14 text-center text-2xl font-semibold border-2 rounded-lg focus:outline-none focus:border-primary-500 transition-colors ${
                      error ? 'border-red-300 bg-red-50' : 'border-gray-300'
                    } disabled:opacity-50`}
                  />
                ))}
              </div>

              {/* Error message */}
              {error && (
                <div className="flex items-center gap-2 text-red-600 text-sm mb-4 justify-center">
                  <AlertCircle className="w-4 h-4" />
                  {error}
                </div>
              )}

              {/* Loading indicator */}
              {isLoading && (
                <div className="flex items-center justify-center gap-2 text-primary-600 mb-4">
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Verifying...</span>
                </div>
              )}

              {/* Resend button */}
              <div className="text-center">
                <p className="text-sm text-gray-500 mb-2">Didn't receive the code?</p>
                <button
                  onClick={sendCode}
                  disabled={resendCooldown > 0 || isSending}
                  className="inline-flex items-center gap-2 text-primary-600 hover:text-primary-700 text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <RefreshCw className={`w-4 h-4 ${isSending ? 'animate-spin' : ''}`} />
                  {resendCooldown > 0 ? `Resend in ${resendCooldown}s` : 'Resend Code'}
                </button>
              </div>

              {type === 'email' && (
                <p className="text-xs text-gray-400 text-center mt-4">
                  Check your spam folder if you don't see the email
                </p>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
