// frontend/auth-ui/src/pages/ChangeCredential.tsx
import { useSearchParams, useNavigate } from 'react-router-dom'
import { SharedChangeCredential } from '../components/SharedChangeCredential'

export function ChangeCredentialPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const type = searchParams.get('type') as 'email' | 'phone' || 'email'
  const token = localStorage.getItem('token') || ''

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 p-4">
      <div className="bg-white rounded-xl shadow-lg p-6 w-full max-w-md">
        <SharedChangeCredential
          authApiUrl={import.meta.env.VITE_API_URL}
          type={type}
          authToken={token}
          onSuccess={() => navigate('/profile')}
          onCancel={() => navigate(-1)}
        />
      </div>
    </div>
  )
}
