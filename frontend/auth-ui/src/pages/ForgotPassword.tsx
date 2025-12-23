import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Eye, EyeOff, KeyRound, ArrowLeft, CheckCircle2, Loader2, Mail, Phone } from 'lucide-react';
import { authApi } from '../utils/api';
import { getSiteDisplayName, getSiteKey } from '../utils/redirect';

type Step = 'input' | 'code' | 'password' | 'success';
type RecoveryMode = 'email' | 'phone';

export function ForgotPasswordPage() {
  const [mode, setMode] = useState<RecoveryMode>('email');
  const [step, setStep] = useState<Step>('input');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [otpCode, setOtpCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const siteKey = getSiteKey();
  const siteName = getSiteDisplayName(siteKey);

  const recoveryValue = mode === 'email' ? email : phoneNumber;

  const handleSendCode = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.requestPasswordReset(recoveryValue);
      if (response.success) {
        setStep('code');
      } else {
        setError(response.message || 'Failed to send code');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send code');
    } finally {
      setIsLoading(false);
    }
  };

  const handleVerifyCode = async (e: React.FormEvent) => {
    e.preventDefault();
    // For now, just move to password step
    // In production, you might want to verify the code first
    setStep('password');
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    if (newPassword.length < 8) {
      setError('Password must be at least 8 characters');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.resetPassword(recoveryValue, otpCode, newPassword);
      if (response.success) {
        setStep('success');
      } else {
        setError(response.message || 'Failed to reset password');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reset password');
    } finally {
      setIsLoading(false);
    }
  };

  const handleModeChange = (newMode: RecoveryMode) => {
    setMode(newMode);
    setError(null);
  };

  const handleBackToInput = () => {
    setStep('input');
    setOtpCode('');
    setError(null);
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary-50 via-white to-primary-100 px-4 py-12">
      <div className="max-w-md w-full animate-fade-in">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-br from-primary-500 to-primary-700 rounded-2xl mb-4 shadow-soft">
            {step === 'success' ? (
              <CheckCircle2 className="w-8 h-8 text-white" />
            ) : (
              <KeyRound className="w-8 h-8 text-white" />
            )}
          </div>
          <h1 className="text-2xl font-bold text-gray-900">
            {step === 'success' ? 'Password Reset!' : 'Reset your password'}
          </h1>
          {step !== 'success' && (
            <p className="text-sm text-gray-500 mt-1">{siteName}</p>
          )}
        </div>

        {/* Card */}
        <div className="bg-white rounded-2xl shadow-soft p-8">
          {/* Mode Toggle - only show on input step */}
          {step === 'input' && (
            <div className="flex border-b border-gray-200 mb-6">
              <button
                onClick={() => handleModeChange('email')}
                className={`flex-1 py-3 text-sm font-medium border-b-2 transition-colors flex items-center justify-center gap-2 ${
                  mode === 'email'
                    ? 'border-primary-500 text-primary-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                <Mail className="w-4 h-4" />
                Email
              </button>
              <button
                onClick={() => handleModeChange('phone')}
                className={`flex-1 py-3 text-sm font-medium border-b-2 transition-colors flex items-center justify-center gap-2 ${
                  mode === 'phone'
                    ? 'border-primary-500 text-primary-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                <Phone className="w-4 h-4" />
                Phone
              </button>
            </div>
          )}

          {/* Error Message */}
          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {error}
            </div>
          )}

          {/* Step 1: Enter Email or Phone */}
          {step === 'input' && (
            <form onSubmit={handleSendCode} className="space-y-5">
              <p className="text-sm text-gray-600 mb-4">
                Enter your {mode === 'email' ? 'email address' : 'phone number'} and we'll send you a verification code to reset your password.
              </p>

              {mode === 'email' ? (
                <div>
                  <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                    Email address
                  </label>
                  <input
                    type="email"
                    id="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 transition-colors"
                    placeholder="you@example.com"
                  />
                </div>
              ) : (
                <div>
                  <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
                    Phone Number
                  </label>
                  <input
                    type="tel"
                    id="phone"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    required
                    className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 transition-colors"
                    placeholder="+1 (555) 123-4567"
                  />
                </div>
              )}

              <button
                type="submit"
                disabled={isLoading || !recoveryValue}
                className="w-full flex justify-center items-center gap-2 py-2.5 px-4 bg-gradient-to-r from-primary-500 to-primary-600 text-white font-medium rounded-lg hover:from-primary-600 hover:to-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-sm"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="w-5 h-5 animate-spin" />
                    Sending...
                  </>
                ) : (
                  'Send Code'
                )}
              </button>
            </form>
          )}

          {/* Step 2: Enter Verification Code */}
          {step === 'code' && (
            <form onSubmit={handleVerifyCode} className="space-y-5">
              <p className="text-sm text-gray-600 mb-4">
                We sent a code to <strong>{recoveryValue}</strong>. Enter it below.
              </p>

              <div>
                <label htmlFor="otp" className="block text-sm font-medium text-gray-700 mb-1">
                  Verification Code
                </label>
                <input
                  type="text"
                  id="otp"
                  value={otpCode}
                  onChange={(e) => setOtpCode(e.target.value)}
                  required
                  maxLength={6}
                  className="w-full appearance-none block px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 text-center text-lg tracking-widest transition-colors"
                  placeholder="000000"
                />
              </div>

              <div className="flex items-center justify-between text-sm">
                <button
                  type="button"
                  onClick={handleBackToInput}
                  className="text-gray-500 hover:text-gray-700 flex items-center gap-1"
                >
                  <ArrowLeft className="w-4 h-4" />
                  Change {mode === 'email' ? 'email' : 'number'}
                </button>
                <button
                  type="button"
                  onClick={handleSendCode}
                  disabled={isLoading}
                  className="text-primary-600 hover:text-primary-700 font-medium"
                >
                  Resend code
                </button>
              </div>

              <button
                type="submit"
                disabled={otpCode.length < 6}
                className="w-full flex justify-center items-center gap-2 py-2.5 px-4 bg-gradient-to-r from-primary-500 to-primary-600 text-white font-medium rounded-lg hover:from-primary-600 hover:to-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-sm"
              >
                Verify Code
              </button>
            </form>
          )}

          {/* Step 3: Enter New Password */}
          {step === 'password' && (
            <form onSubmit={handleResetPassword} className="space-y-5">
              <p className="text-sm text-gray-600 mb-4">
                Enter your new password below.
              </p>

              <div>
                <label htmlFor="newPassword" className="block text-sm font-medium text-gray-700 mb-1">
                  New Password
                </label>
                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    id="newPassword"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    required
                    minLength={8}
                    className="appearance-none block w-full px-3 py-2 pr-10 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 transition-colors"
                    placeholder="Enter new password"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute inset-y-0 right-0 flex items-center pr-3 text-gray-400 hover:text-gray-600"
                  >
                    {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
                  </button>
                </div>
                <p className="mt-1 text-xs text-gray-500">Must be at least 8 characters</p>
              </div>

              <div>
                <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1">
                  Confirm Password
                </label>
                <div className="relative">
                  <input
                    type={showConfirmPassword ? 'text' : 'password'}
                    id="confirmPassword"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                    className="appearance-none block w-full px-3 py-2 pr-10 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 transition-colors"
                    placeholder="Confirm new password"
                  />
                  <button
                    type="button"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    className="absolute inset-y-0 right-0 flex items-center pr-3 text-gray-400 hover:text-gray-600"
                  >
                    {showConfirmPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
                  </button>
                </div>
              </div>

              <button
                type="submit"
                disabled={isLoading}
                className="w-full flex justify-center items-center gap-2 py-2.5 px-4 bg-gradient-to-r from-primary-500 to-primary-600 text-white font-medium rounded-lg hover:from-primary-600 hover:to-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-sm"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="w-5 h-5 animate-spin" />
                    Resetting...
                  </>
                ) : (
                  'Reset Password'
                )}
              </button>
            </form>
          )}

          {/* Step 4: Success */}
          {step === 'success' && (
            <div className="text-center">
              <div className="inline-flex items-center justify-center w-16 h-16 bg-primary-100 rounded-full mb-4">
                <CheckCircle2 className="w-8 h-8 text-primary-600" />
              </div>
              <p className="text-gray-600 mb-6">
                Your password has been reset successfully. You can now sign in with your new password.
              </p>
              <Link
                to={`/login${window.location.search}`}
                className="inline-flex justify-center items-center gap-2 w-full py-2.5 px-4 bg-gradient-to-r from-primary-500 to-primary-600 text-white font-medium rounded-lg hover:from-primary-600 hover:to-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 transition-all shadow-sm"
              >
                Sign In
              </Link>
            </div>
          )}
        </div>

        {/* Back to Login Link */}
        {step !== 'success' && (
          <p className="mt-6 text-center text-sm text-gray-600">
            Remember your password?{' '}
            <Link
              to={`/login${window.location.search}`}
              className="text-primary-600 hover:text-primary-700 font-medium"
            >
              Sign in
            </Link>
          </p>
        )}
      </div>
    </div>
  );
}
