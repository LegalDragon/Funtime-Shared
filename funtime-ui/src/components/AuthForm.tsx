import React, { useState } from 'react';
import { Button } from './Button';
import { Input } from './Input';
import { Card, CardHeader, CardTitle, CardContent } from './Card';

export interface AuthFormProps {
  mode: 'login' | 'register' | 'otp';
  onSubmit: (data: AuthFormData) => Promise<void>;
  onModeChange?: (mode: 'login' | 'register' | 'otp') => void;
  isLoading?: boolean;
  error?: string;
}

export interface AuthFormData {
  email?: string;
  password?: string;
  phoneNumber?: string;
  otpCode?: string;
}

export const AuthForm: React.FC<AuthFormProps> = ({
  mode,
  onSubmit,
  onModeChange,
  isLoading = false,
  error,
}) => {
  const [formData, setFormData] = useState<AuthFormData>({});
  const [otpSent, setOtpSent] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit(formData);
  };

  const renderEmailForm = () => (
    <>
      <Input
        label="Email"
        name="email"
        type="email"
        placeholder="you@example.com"
        value={formData.email || ''}
        onChange={handleChange}
        required
      />
      <Input
        label="Password"
        name="password"
        type="password"
        placeholder="Enter your password"
        value={formData.password || ''}
        onChange={handleChange}
        required
        minLength={8}
      />
    </>
  );

  const renderOtpForm = () => (
    <>
      <Input
        label="Phone Number"
        name="phoneNumber"
        type="tel"
        placeholder="+1234567890"
        value={formData.phoneNumber || ''}
        onChange={handleChange}
        required
        disabled={otpSent}
      />
      {otpSent && (
        <Input
          label="Verification Code"
          name="otpCode"
          type="text"
          placeholder="123456"
          value={formData.otpCode || ''}
          onChange={handleChange}
          required
          maxLength={6}
        />
      )}
    </>
  );

  const getTitle = () => {
    switch (mode) {
      case 'login':
        return 'Sign In';
      case 'register':
        return 'Create Account';
      case 'otp':
        return 'Phone Login';
    }
  };

  const getButtonText = () => {
    if (mode === 'otp') {
      return otpSent ? 'Verify Code' : 'Send Code';
    }
    return mode === 'login' ? 'Sign In' : 'Create Account';
  };

  return (
    <Card className="w-full max-w-md mx-auto">
      <CardHeader>
        <CardTitle>{getTitle()}</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          {mode === 'otp' ? renderOtpForm() : renderEmailForm()}

          {error && (
            <p className="text-sm text-red-600">{error}</p>
          )}

          <Button
            type="submit"
            fullWidth
            isLoading={isLoading}
          >
            {getButtonText()}
          </Button>
        </form>

        {onModeChange && (
          <div className="mt-4 text-center text-sm text-gray-600">
            {mode === 'login' && (
              <>
                Don't have an account?{' '}
                <button
                  type="button"
                  onClick={() => onModeChange('register')}
                  className="text-green-600 hover:underline"
                >
                  Sign up
                </button>
              </>
            )}
            {mode === 'register' && (
              <>
                Already have an account?{' '}
                <button
                  type="button"
                  onClick={() => onModeChange('login')}
                  className="text-green-600 hover:underline"
                >
                  Sign in
                </button>
              </>
            )}
            <div className="mt-2">
              <button
                type="button"
                onClick={() => {
                  setOtpSent(false);
                  onModeChange(mode === 'otp' ? 'login' : 'otp');
                }}
                className="text-green-600 hover:underline"
              >
                {mode === 'otp' ? 'Use email instead' : 'Use phone number instead'}
              </button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
};
