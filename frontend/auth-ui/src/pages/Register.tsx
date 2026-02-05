import { useState, useEffect } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Eye, EyeOff, UserPlus, Mail, Phone, Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { authApi, settingsApi } from '../utils/api';
import type { OAuthProvider } from '../utils/api';
import { redirectWithToken, getSiteDisplayName, getSiteKey, getReturnTo, getRedirectUrl } from '../utils/redirect';
import { SiteLogoOverlay } from '../components/SiteLogoOverlay';

type AuthMode = 'email' | 'phone';

// SVG icons for OAuth providers
const GoogleIcon = () => (
  <svg className="w-5 h-5" viewBox="0 0 24 24">
    <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
    <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
    <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
    <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
  </svg>
);

const AppleIcon = () => (
  <svg className="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
    <path d="M12.152 6.896c-.948 0-2.415-1.078-3.96-1.04-2.04.027-3.91 1.183-4.961 3.014-2.117 3.675-.546 9.103 1.519 12.09 1.013 1.454 2.208 3.09 3.792 3.039 1.52-.065 2.09-.987 3.935-.987 1.831 0 2.35.987 3.96.948 1.637-.026 2.676-1.48 3.676-2.948 1.156-1.688 1.636-3.325 1.662-3.415-.039-.013-3.182-1.221-3.22-4.857-.026-3.04 2.48-4.494 2.597-4.559-1.429-2.09-3.623-2.324-4.39-2.376-2-.156-3.675 1.09-4.61 1.09zM15.53 3.83c.843-1.012 1.4-2.427 1.245-3.83-1.207.052-2.662.805-3.532 1.818-.78.896-1.454 2.338-1.273 3.714 1.338.104 2.715-.688 3.559-1.701"/>
  </svg>
);

const FacebookIcon = () => (
  <svg className="w-5 h-5" viewBox="0 0 24 24" fill="#1877F2">
    <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
  </svg>
);

const MicrosoftIcon = () => (
  <svg className="w-5 h-5" viewBox="0 0 24 24">
    <path fill="#F25022" d="M1 1h10v10H1z"/>
    <path fill="#00A4EF" d="M1 13h10v10H1z"/>
    <path fill="#7FBA00" d="M13 1h10v10H13z"/>
    <path fill="#FFB900" d="M13 13h10v10H13z"/>
  </svg>
);

const getProviderIcon = (provider: string) => {
  switch (provider.toLowerCase()) {
    case 'google': return <GoogleIcon />;
    case 'apple': return <AppleIcon />;
    case 'facebook': return <FacebookIcon />;
    case 'microsoft': return <MicrosoftIcon />;
    default: return null;
  }
};

export function RegisterPage() {
  const { t, i18n } = useTranslation();
  const [searchParams] = useSearchParams();
  const [mode, setMode] = useState<AuthMode>('email');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  // Email registration state
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');

  // Phone registration state
  const [phoneNumber, setPhoneNumber] = useState('');
  const [otpCode, setOtpCode] = useState('');
  const [otpSent, setOtpSent] = useState(false);

  // OAuth providers state
  const [oauthProviders, setOauthProviders] = useState<OAuthProvider[]>([]);

  const siteKey = getSiteKey();
  const siteName = getSiteDisplayName(siteKey);
  const returnTo = getReturnTo();
  const redirectUrl = getRedirectUrl();

  // Logo state
  const [mainLogoUrl, setMainLogoUrl] = useState<string | null>(null);
  const [siteLogoUrl, setSiteLogoUrl] = useState<string | null>(null);
  const [foundSiteName, setFoundSiteName] = useState<string | null>(null);

  useEffect(() => {
    loadLogos();
    loadOAuthProviders();

    // Check for OAuth error in URL
    const oauthError = searchParams.get('error');
    if (oauthError) {
      setError(oauthError);
    }
  }, [siteKey, searchParams]);

  const loadOAuthProviders = async () => {
    try {
      const providers = await authApi.getOAuthProviders();
      setOauthProviders(providers);
    } catch (error) {
      // OAuth providers are optional - don't show error
      console.log('OAuth providers not available');
    }
  };

  const handleOAuthRegister = (provider: string) => {
    // Redirect to OAuth start URL with current language
    const currentLang = i18n.language;
    const url = authApi.getOAuthStartUrl(provider, redirectUrl || undefined, siteKey || undefined, currentLang);
    window.location.href = url;
  };

  const loadLogos = async () => {
    try {
      // Load main logo
      const mainLogoResponse = await settingsApi.getMainLogo();
      if (mainLogoResponse.hasLogo && mainLogoResponse.logoUrl) {
        setMainLogoUrl(settingsApi.getLogoDisplayUrl(mainLogoResponse.logoUrl));
      }

      // Load site logo if site key is present (case-insensitive)
      if (siteKey) {
        const sites = await authApi.getSites();
        const site = sites.find(s => s.key.toLowerCase() === siteKey.toLowerCase());
        if (site) {
          setFoundSiteName(site.name);
          if (site.logoUrl) {
            setSiteLogoUrl(settingsApi.getLogoDisplayUrl(site.logoUrl));
          }
        }
      }
    } catch (error) {
      console.error('Failed to load logos:', error);
    }
  };

  // Get site display title (e.g., "Pickleball.Community" or "Funtime Pickleball")
  const getSiteTitle = () => {
    if (foundSiteName) {
      // Site found - use "Pickleball.SiteName" format
      return `Pickleball.${foundSiteName}`;
    }
    return 'Funtime Pickleball';
  };

  const handleEmailRegister = async (e: React.FormEvent) => {
    e.preventDefault();

    if (password !== confirmPassword) {
      setError(t('register.passwordsDoNotMatch'));
      return;
    }

    if (password.length < 8) {
      setError(t('validation.passwordTooShort', { min: 8 }));
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.register(email, password, firstName, lastName);
      if (response.success && response.token) {
        redirectWithToken(response.token);
      } else {
        setError(response.message || 'Registration failed');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSendOtp = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.sendOtp(phoneNumber);
      if (response.success) {
        setOtpSent(true);
      } else {
        setError(response.message || 'Failed to send code');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send code');
    } finally {
      setIsLoading(false);
    }
  };

  const handlePhoneRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.verifyOtp(phoneNumber, otpCode);
      if (response.success && response.token) {
        redirectWithToken(response.token);
      } else {
        setError(response.message || 'Invalid code');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Verification failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary-50 via-white to-primary-100 px-4 py-12">
      <div className="max-w-md w-full animate-fade-in">
        {/* Logo and Site Info */}
        <div className="flex items-center gap-4 mb-8">
          <SiteLogoOverlay
            mainLogoUrl={mainLogoUrl}
            siteLogoUrl={siteLogoUrl}
            siteName={siteName}
            size="xl"
          />
          <div className="flex-1 text-center">
            <h1 className="text-2xl font-bold text-gray-900">{getSiteTitle()}</h1>
            <h2 className="text-lg text-gray-600">{t('register.title')}</h2>
            {returnTo && (
              <p className="text-sm text-gray-500">
                {t('auth.continueTo', { destination: returnTo })}
              </p>
            )}
          </div>
        </div>

        {/* Auth Card */}
        <div className="bg-white rounded-2xl shadow-soft p-8">
          {/* Mode Toggle */}
          <div className="flex border-b border-gray-200 mb-6">
            <button
              onClick={() => setMode('email')}
              className={`flex-1 py-3 text-sm font-medium border-b-2 transition-colors flex items-center justify-center gap-2 ${
                mode === 'email'
                  ? 'border-primary-500 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              <Mail className="w-4 h-4" />
              {t('auth.email')}
            </button>
            <button
              onClick={() => setMode('phone')}
              className={`flex-1 py-3 text-sm font-medium border-b-2 transition-colors flex items-center justify-center gap-2 ${
                mode === 'phone'
                  ? 'border-primary-500 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              <Phone className="w-4 h-4" />
              {t('auth.phone')}
            </button>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {error}
            </div>
          )}

          {/* OAuth Buttons */}
          {oauthProviders.length > 0 && (
            <>
              <div className="space-y-3 mb-6">
                {oauthProviders.map((provider) => (
                  <button
                    key={provider.name}
                    type="button"
                    onClick={() => handleOAuthRegister(provider.name)}
                    className="w-full flex items-center justify-center gap-3 py-2.5 px-4 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors text-gray-700 font-medium"
                  >
                    {getProviderIcon(provider.name)}
                    {t('auth.continueWith', { provider: provider.displayName })}
                  </button>
                ))}
              </div>

              <div className="relative mb-6">
                <div className="absolute inset-0 flex items-center">
                  <div className="w-full border-t border-gray-300" />
                </div>
                <div className="relative flex justify-center text-sm">
                  <span className="px-4 bg-white text-gray-500">{t('auth.orContinueWith')}</span>
                </div>
              </div>
            </>
          )}

          {/* Email Registration Form */}
          {mode === 'email' && (
            <form onSubmit={handleEmailRegister} className="space-y-5">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
                    {t('register.firstName')}
                  </label>
                  <input
                    type="text"
                    id="firstName"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    required
                    className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 transition-colors"
                    placeholder={t('register.firstName')}
                  />
                </div>
                <div>
                  <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
                    {t('register.lastName')}
                  </label>
                  <input
                    type="text"
                    id="lastName"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    required
                    className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 transition-colors"
                    placeholder={t('register.lastName')}
                  />
                </div>
              </div>

              <div>
                <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth.emailAddress')}
                </label>
                <input
                  type="email"
                  id="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 transition-colors"
                  placeholder={t('auth.emailPlaceholder')}
                />
              </div>

              <div>
                <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth.password')}
                </label>
                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    id="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    minLength={8}
                    className="appearance-none block w-full px-3 py-2 pr-10 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 transition-colors"
                    placeholder={t('register.createPassword')}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute inset-y-0 right-0 flex items-center pr-3 text-gray-400 hover:text-gray-600"
                  >
                    {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
                  </button>
                </div>
                <p className="mt-1 text-xs text-gray-500">{t('register.passwordRequirement')}</p>
              </div>

              <div>
                <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth.confirmPassword')}
                </label>
                <div className="relative">
                  <input
                    type={showConfirmPassword ? 'text' : 'password'}
                    id="confirmPassword"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                    className="appearance-none block w-full px-3 py-2 pr-10 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 transition-colors"
                    placeholder={t('register.confirmPasswordPlaceholder')}
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
                    {t('register.creating')}
                  </>
                ) : (
                  <>
                    <UserPlus className="w-5 h-5" />
                    {t('register.createAccount')}
                  </>
                )}
              </button>
            </form>
          )}

          {/* Phone Registration Form */}
          {mode === 'phone' && (
            <form onSubmit={handlePhoneRegister} className="space-y-5">
              <div>
                <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth.phoneNumber')}
                </label>
                <div className="flex gap-2">
                  <input
                    type="tel"
                    id="phone"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    required
                    disabled={otpSent}
                    className="flex-1 appearance-none block px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100 transition-colors"
                    placeholder={t('auth.phonePlaceholder')}
                  />
                  {!otpSent && (
                    <button
                      type="button"
                      onClick={handleSendOtp}
                      disabled={isLoading || !phoneNumber}
                      className="px-4 py-2 bg-gray-100 text-gray-700 font-medium rounded-lg hover:bg-gray-200 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {isLoading ? <Loader2 className="w-5 h-5 animate-spin" /> : t('register.sendCode')}
                    </button>
                  )}
                </div>
              </div>

              {otpSent && (
                <>
                  <div>
                    <label htmlFor="otp" className="block text-sm font-medium text-gray-700 mb-1">
                      {t('phoneAuth.verificationCode')}
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
                      onClick={() => {
                        setOtpSent(false);
                        setOtpCode('');
                      }}
                      className="text-gray-500 hover:text-gray-700"
                    >
                      {t('phoneAuth.changeNumber')}
                    </button>
                    <button
                      type="button"
                      onClick={handleSendOtp}
                      disabled={isLoading}
                      className="text-primary-600 hover:text-primary-700 font-medium"
                    >
                      {t('phoneAuth.resendCode')}
                    </button>
                  </div>

                  <button
                    type="submit"
                    disabled={isLoading || otpCode.length < 6}
                    className="w-full flex justify-center items-center gap-2 py-2.5 px-4 bg-gradient-to-r from-primary-500 to-primary-600 text-white font-medium rounded-lg hover:from-primary-600 hover:to-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-sm"
                  >
                    {isLoading ? (
                      <>
                        <Loader2 className="w-5 h-5 animate-spin" />
                        {t('phoneAuth.verifying')}
                      </>
                    ) : (
                      <>
                        <UserPlus className="w-5 h-5" />
                        {t('register.createAccount')}
                      </>
                    )}
                  </button>
                </>
              )}
            </form>
          )}

          {/* Terms */}
          <p className="mt-5 text-xs text-gray-500 text-center">
            {t('register.agreeToTerms')}{' '}
            <a
              href={`/terms-of-service${siteKey ? `?site=${siteKey}` : ''}`}
              target="_blank"
              rel="noopener noreferrer"
              className="text-primary-600 hover:underline"
            >
              {t('register.termsOfService')}
            </a>
            {' '}{t('register.and')}{' '}
            <a
              href={`/privacy-policy${siteKey ? `?site=${siteKey}` : ''}`}
              target="_blank"
              rel="noopener noreferrer"
              className="text-primary-600 hover:underline"
            >
              {t('register.privacyPolicy')}
            </a>
          </p>
        </div>

        {/* Login Link */}
        <p className="mt-6 text-center text-sm text-gray-600">
          {t('auth.haveAccount')}{' '}
          <Link
            to={`/login${window.location.search}`}
            className="text-primary-600 hover:text-primary-700 font-medium"
          >
            {t('auth.signIn')}
          </Link>
        </p>
      </div>
    </div>
  );
}
