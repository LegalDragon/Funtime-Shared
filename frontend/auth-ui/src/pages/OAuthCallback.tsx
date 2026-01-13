import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Loader2, AlertCircle, CheckCircle } from 'lucide-react';
import { redirectWithToken, getRedirectUrl } from '../utils/redirect';

export function OAuthCallbackPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [message, setMessage] = useState('Processing authentication...');

  useEffect(() => {
    const token = searchParams.get('token');
    const error = searchParams.get('error');
    const siteRole = searchParams.get('siteRole') || undefined;
    const isSiteAdmin = searchParams.get('isSiteAdmin') === 'true';

    if (error) {
      setStatus('error');
      setMessage(error);
      return;
    }

    if (!token) {
      setStatus('error');
      setMessage('No authentication token received');
      return;
    }

    // Store the token
    localStorage.setItem('auth_token', token);
    setStatus('success');
    setMessage('Authentication successful! Redirecting...');

    // Check if there's a redirect URL to go back to
    const redirectUrl = getRedirectUrl();

    if (redirectUrl) {
      // Redirect to the original site with token and role info
      setTimeout(() => {
        redirectWithToken(token, { siteRole, isSiteAdmin });
      }, 1000);
    } else {
      // No redirect URL, go to site selection
      setTimeout(() => {
        navigate('/sites');
      }, 1000);
    }
  }, [searchParams, navigate]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary-50 via-white to-primary-100 px-4">
      <div className="max-w-md w-full bg-white rounded-2xl shadow-soft p-8 text-center">
        {status === 'loading' && (
          <>
            <Loader2 className="w-12 h-12 text-primary-500 animate-spin mx-auto mb-4" />
            <h1 className="text-xl font-semibold text-gray-900 mb-2">
              Authenticating
            </h1>
            <p className="text-gray-600">{message}</p>
          </>
        )}

        {status === 'success' && (
          <>
            <CheckCircle className="w-12 h-12 text-green-500 mx-auto mb-4" />
            <h1 className="text-xl font-semibold text-gray-900 mb-2">
              Success!
            </h1>
            <p className="text-gray-600">{message}</p>
          </>
        )}

        {status === 'error' && (
          <>
            <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
            <h1 className="text-xl font-semibold text-gray-900 mb-2">
              Authentication Failed
            </h1>
            <p className="text-gray-600 mb-6">{message}</p>
            <button
              onClick={() => navigate('/login')}
              className="px-6 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 transition-colors"
            >
              Back to Login
            </button>
          </>
        )}
      </div>
    </div>
  );
}
