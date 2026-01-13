import React, { useState } from 'react'
import { Mail, Phone, ArrowLeft, CheckCircle } from 'lucide-react'

/**
 * SharedChangeCredential - Funtime Auth Email/Phone Change Component
 *
 * A reusable component for changing email or phone credentials with OTP verification.
 * Use across all Funtime sites for consistent credential change experience.
 *
 * ============================================================================
 * INSTALLATION
 * ============================================================================
 *
 * Dependencies:
 *   - React 18+
 *   - lucide-react (for icons)
 *   - Tailwind CSS (for styling)
 *
 * ============================================================================
 * BACKEND REQUIREMENTS
 * ============================================================================
 *
 * This component requires 4 API endpoints on your auth server:
 *
 *   POST /api/auth/change-email/request
 *     Body: { newEmail: string }
 *     Response: { success: boolean, message: string }
 *
 *   POST /api/auth/change-email/verify
 *     Body: { newEmail: string, code: string }
 *     Response: { success: boolean, message: string, token?: string, user?: object }
 *
 *   POST /api/auth/change-phone/request
 *     Body: { newPhoneNumber: string }
 *     Response: { success: boolean, message: string }
 *
 *   POST /api/auth/change-phone/verify
 *     Body: { newPhoneNumber: string, code: string }
 *     Response: { success: boolean, message: string, token?: string, user?: object }
 *
 * All endpoints require Authorization: Bearer {token} header.
 *
 * ============================================================================
 * USAGE EXAMPLES
 * ============================================================================
 *
 * Example 1: Basic Usage (Standalone)
 * ------------------------------------
 *
 *   import { SharedChangeCredential } from '@funtime/ui'
 *   // or: import SharedChangeCredential from './SharedChangeCredential'
 *
 *   function ChangeEmailPage() {
 *     const token = localStorage.getItem('jwtToken')
 *
 *     return (
 *       <SharedChangeCredential
 *         authApiUrl="https://auth.funtime.com/api"
 *         type="email"
 *         currentValue="user@example.com"
 *         authToken={token}
 *         onSuccess={({ newValue, token }) => {
 *           // Update stored token
 *           if (token) localStorage.setItem('jwtToken', token)
 *           // Navigate or show success message
 *           console.log('Email changed to:', newValue)
 *         }}
 *         onError={(message) => console.error(message)}
 *         onCancel={() => navigate('/profile')}
 *       />
 *     )
 *   }
 *
 * Example 2: Inside a Modal
 * -------------------------
 *
 *   import { SharedChangeCredential } from '@funtime/ui'
 *
 *   function ChangeCredentialModal({ isOpen, onClose, type, currentValue }) {
 *     if (!isOpen) return null
 *
 *     return (
 *       <div className="fixed inset-0 z-50 flex items-center justify-center">
 *         <div className="fixed inset-0 bg-black/50" onClick={onClose} />
 *         <div className="relative bg-white rounded-xl p-6 max-w-md w-full">
 *           <SharedChangeCredential
 *             authApiUrl={process.env.REACT_APP_AUTH_URL}
 *             type={type}
 *             currentValue={currentValue}
 *             authToken={localStorage.getItem('jwtToken')}
 *             onSuccess={({ newValue, token }) => {
 *               if (token) localStorage.setItem('jwtToken', token)
 *               onClose()
 *             }}
 *             onCancel={onClose}
 *             siteName="My App"
 *             primaryColor="blue"
 *           />
 *         </div>
 *       </div>
 *     )
 *   }
 *
 * Example 3: With React Context
 * -----------------------------
 *
 *   import { SharedChangeCredential } from '@funtime/ui'
 *   import { useAuth } from './contexts/AuthContext'
 *
 *   function ProfileSettings() {
 *     const { user, token, updateUser, setToken } = useAuth()
 *     const [modalType, setModalType] = useState(null) // 'email' | 'phone' | null
 *
 *     const handleSuccess = async ({ newValue, token: newToken, user: userData }) => {
 *       // Update token if returned
 *       if (newToken) setToken(newToken)
 *
 *       // Update user in context
 *       updateUser({
 *         ...user,
 *         [modalType]: newValue
 *       })
 *
 *       // Sync with local backend if needed
 *       await fetch('/api/users/profile', {
 *         method: 'PUT',
 *         headers: { 'Content-Type': 'application/json' },
 *         body: JSON.stringify({ [modalType]: newValue })
 *       })
 *
 *       setModalType(null)
 *     }
 *
 *     return (
 *       <>
 *         <button onClick={() => setModalType('email')}>Change Email</button>
 *         <button onClick={() => setModalType('phone')}>Change Phone</button>
 *
 *         {modalType && (
 *           <Modal onClose={() => setModalType(null)}>
 *             <SharedChangeCredential
 *               authApiUrl="https://auth.funtime.com/api"
 *               type={modalType}
 *               currentValue={modalType === 'email' ? user.email : user.phone}
 *               authToken={token}
 *               onSuccess={handleSuccess}
 *               onCancel={() => setModalType(null)}
 *             />
 *           </Modal>
 *         )}
 *       </>
 *     )
 *   }
 *
 * ============================================================================
 * PROPS REFERENCE
 * ============================================================================
 *
 *   authApiUrl (required)
 *     Type: string
 *     The base URL for the auth API (e.g., "https://auth.funtime.com/api")
 *
 *   type (required)
 *     Type: 'email' | 'phone'
 *     Which credential to change
 *
 *   authToken (required)
 *     Type: string
 *     The current JWT token for authenticated requests
 *
 *   currentValue (optional)
 *     Type: string
 *     The current email or phone to display to the user
 *
 *   onSuccess (optional)
 *     Type: ({ newValue, token?, user? }) => void
 *     Called after successful verification. `token` is the new JWT if returned.
 *
 *   onError (optional)
 *     Type: (message: string) => void
 *     Called when an error occurs
 *
 *   onCancel (optional)
 *     Type: () => void
 *     Called when user clicks Cancel. If provided, shows a Cancel button.
 *
 *   siteName (optional)
 *     Type: string
 *     Default: "Funtime"
 *     Your site name for branding (currently unused, reserved for future)
 *
 *   primaryColor (optional)
 *     Type: string
 *     Default: "blue"
 *     Tailwind color name for buttons and accents (e.g., "blue", "indigo", "green")
 *
 * ============================================================================
 * FLOW DIAGRAM
 * ============================================================================
 *
 *   ┌─────────────────┐
 *   │   Step: input   │  User enters new email/phone
 *   │                 │  Clicks "Send Verification Code"
 *   └────────┬────────┘
 *            │ POST /auth/change-{type}/request
 *            ▼
 *   ┌─────────────────┐
 *   │  Step: verify   │  User receives OTP via email/SMS
 *   │                 │  Enters 6-digit code
 *   └────────┬────────┘
 *            │ POST /auth/change-{type}/verify
 *            ▼
 *   ┌─────────────────┐
 *   │  Step: success  │  Shows success message
 *   │                 │  Calls onSuccess with new token
 *   └─────────────────┘
 *
 * ============================================================================
 */

export interface SharedChangeCredentialProps {
  /** Base URL for auth API (e.g., https://auth.funtime.com/api) */
  authApiUrl: string;
  /** Type of credential to change */
  type: 'email' | 'phone';
  /** Current email or phone value */
  currentValue?: string;
  /** Callback with { newValue, token, user } on successful change */
  onSuccess?: (result: { newValue: string; token?: string; user?: { id: number; email?: string; phone?: string } }) => void;
  /** Callback with error message on failure */
  onError?: (message: string) => void;
  /** Callback when user cancels */
  onCancel?: () => void;
  /** Name of the site (e.g., "Pickleball Community") */
  siteName?: string;
  /** Primary brand color (default: blue) */
  primaryColor?: string;
  /** Current JWT token for authenticated requests */
  authToken: string;
}

export const SharedChangeCredential: React.FC<SharedChangeCredentialProps> = ({
  authApiUrl,
  type,
  currentValue,
  onSuccess,
  onError,
  onCancel,
  primaryColor = 'blue',
  authToken
}) => {
  const [step, setStep] = useState<'input' | 'verify' | 'success'>('input')
  const [newValue, setNewValue] = useState('')
  const [otp, setOtp] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const isEmail = type === 'email'
  const Icon = isEmail ? Mail : Phone
  const label = isEmail ? 'Email' : 'Phone Number'
  const placeholder = isEmail ? 'you@example.com' : '+1234567890'

  const getAuthHeaders = (): HeadersInit => ({
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${authToken}`
  })

  const handleSendOtp = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!newValue.trim()) {
      setError(`Please enter a new ${label.toLowerCase()}`)
      return
    }

    // Basic validation
    if (isEmail && !newValue.includes('@')) {
      setError('Please enter a valid email address')
      return
    }

    if (!isEmail && newValue.length < 10) {
      setError('Please enter a valid phone number')
      return
    }

    setLoading(true)
    setError('')

    try {
      const endpoint = isEmail
        ? `${authApiUrl}/auth/change-email/request`
        : `${authApiUrl}/auth/change-phone/request`

      const body = isEmail
        ? { newEmail: newValue }
        : { newPhoneNumber: newValue }

      const response = await fetch(endpoint, {
        method: 'POST',
        headers: getAuthHeaders(),
        body: JSON.stringify(body)
      })

      const data = await response.json()

      if (!response.ok) {
        throw new Error(data.message || 'Failed to send verification code')
      }

      setStep('verify')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to send verification code'
      setError(message)
      onError?.(message)
    } finally {
      setLoading(false)
    }
  }

  const handleVerifyOtp = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!otp.trim() || otp.length < 4) {
      setError('Please enter the verification code')
      return
    }

    setLoading(true)
    setError('')

    try {
      const endpoint = isEmail
        ? `${authApiUrl}/auth/change-email/verify`
        : `${authApiUrl}/auth/change-phone/verify`

      const body = isEmail
        ? { newEmail: newValue, code: otp }
        : { newPhoneNumber: newValue, code: otp }

      const response = await fetch(endpoint, {
        method: 'POST',
        headers: getAuthHeaders(),
        body: JSON.stringify(body)
      })

      const data = await response.json()

      if (!response.ok) {
        throw new Error(data.message || 'Invalid verification code')
      }

      setStep('success')

      // Call success callback with new value and token
      setTimeout(() => {
        onSuccess?.({
          newValue,
          token: data.token,
          user: data.user
        })
      }, 1500)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Verification failed'
      setError(message)
      onError?.(message)
    } finally {
      setLoading(false)
    }
  }

  const handleResendOtp = async () => {
    setLoading(true)
    setError('')

    try {
      const endpoint = isEmail
        ? `${authApiUrl}/auth/change-email/request`
        : `${authApiUrl}/auth/change-phone/request`

      const body = isEmail
        ? { newEmail: newValue }
        : { newPhoneNumber: newValue }

      const response = await fetch(endpoint, {
        method: 'POST',
        headers: getAuthHeaders(),
        body: JSON.stringify(body)
      })

      const data = await response.json()

      if (!response.ok) {
        throw new Error(data.message || 'Failed to resend code')
      }

      setError('')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to resend code'
      setError(message)
      onError?.(message)
    } finally {
      setLoading(false)
    }
  }

  const buttonClass = `w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-${primaryColor}-600 hover:bg-${primaryColor}-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-${primaryColor}-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200`

  const inputClass = (hasError = false) => `w-full px-3 py-2.5 border rounded-lg placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-${primaryColor}-500 sm:text-sm transition ${
    hasError ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : 'border-gray-300 focus:border-blue-500'
  }`

  // Success Step
  if (step === 'success') {
    return (
      <div className="w-full max-w-md mx-auto text-center">
        <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <CheckCircle className="w-8 h-8 text-green-600" />
        </div>
        <h2 className="text-2xl font-bold text-gray-900 mb-2">{label} Updated!</h2>
        <p className="text-gray-600 mb-2">
          Your {label.toLowerCase()} has been successfully changed to:
        </p>
        <p className="font-medium text-gray-900 mb-6">{newValue}</p>
      </div>
    )
  }

  return (
    <div className="w-full max-w-md mx-auto">
      {/* Header */}
      <div className="text-center mb-6">
        <div className={`w-16 h-16 bg-gradient-to-br from-${primaryColor}-500 to-purple-600 rounded-2xl flex items-center justify-center shadow-lg mx-auto`}>
          <Icon className="w-8 h-8 text-white" />
        </div>
        <h2 className="mt-4 text-2xl font-bold text-gray-900">
          {step === 'input' && `Change ${label}`}
          {step === 'verify' && 'Verify Your Identity'}
        </h2>
        <p className="mt-1 text-sm text-gray-600">
          {step === 'input' && `Enter your new ${label.toLowerCase()} to receive a verification code`}
          {step === 'verify' && `Enter the code sent to ${newValue}`}
        </p>
      </div>

      {/* Back Button */}
      {step === 'verify' && (
        <button
          onClick={() => {
            setStep('input')
            setOtp('')
            setError('')
          }}
          className="flex items-center text-sm text-gray-500 hover:text-gray-700 mb-4"
        >
          <ArrowLeft className="w-4 h-4 mr-1" />
          Back
        </button>
      )}

      {error && (
        <div className="rounded-lg bg-red-50 p-4 border border-red-200 mb-4">
          <div className="text-sm text-red-700 font-medium">{error}</div>
        </div>
      )}

      {/* Input Step - Enter new email/phone */}
      {step === 'input' && (
        <form onSubmit={handleSendOtp} className="space-y-4">
          {currentValue && (
            <div className="p-3 bg-gray-50 rounded-lg mb-4">
              <p className="text-sm text-gray-600">
                Current {label.toLowerCase()}: <strong>{currentValue}</strong>
              </p>
            </div>
          )}
          <div>
            <label htmlFor="newValue" className="block text-sm font-medium text-gray-700 mb-1">
              New {label}
            </label>
            <input
              id="newValue"
              type={isEmail ? 'email' : 'tel'}
              value={newValue}
              onChange={(e) => setNewValue(e.target.value)}
              required
              className={inputClass()}
              placeholder={placeholder}
            />
          </div>
          <p className="text-xs text-gray-500">
            We'll send a verification code to verify you own this {label.toLowerCase()}.
          </p>
          <button type="submit" disabled={loading} className={buttonClass}>
            {loading ? 'Sending...' : 'Send Verification Code'}
          </button>
        </form>
      )}

      {/* Verify Step - Enter OTP */}
      {step === 'verify' && (
        <form onSubmit={handleVerifyOtp} className="space-y-4">
          <div>
            <label htmlFor="otp" className="block text-sm font-medium text-gray-700 mb-1">
              Verification Code
            </label>
            <input
              id="otp"
              type="text"
              value={otp}
              onChange={(e) => setOtp(e.target.value.replace(/\D/g, ''))}
              maxLength={6}
              required
              className={`${inputClass()} text-center text-lg tracking-widest`}
              placeholder="123456"
            />
          </div>
          <div className="flex justify-end">
            <button
              type="button"
              onClick={handleResendOtp}
              disabled={loading}
              className={`text-sm text-${primaryColor}-600 hover:text-${primaryColor}-500 font-medium disabled:opacity-50`}
            >
              Resend code
            </button>
          </div>
          <button type="submit" disabled={loading || otp.length < 4} className={buttonClass}>
            {loading ? 'Verifying...' : 'Verify & Update'}
          </button>
        </form>
      )}

      {/* Cancel Button */}
      {onCancel && (
        <div className="mt-6 text-center">
          <button
            type="button"
            onClick={onCancel}
            className="text-sm font-medium text-gray-500 hover:text-gray-700"
          >
            Cancel
          </button>
        </div>
      )}
    </div>
  )
}

export default SharedChangeCredential
