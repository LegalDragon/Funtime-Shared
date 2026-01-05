import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { LoginPage } from './pages/Login';
import { RegisterPage } from './pages/Register';
import { ForgotPasswordPage } from './pages/ForgotPassword';
import { SiteSelectionPage } from './pages/SiteSelection';
import { AdminDashboardPage } from './pages/AdminDashboard';
import { TermsOfServicePage } from './pages/TermsOfService';
import { PrivacyPolicyPage } from './pages/PrivacyPolicy';
import { OAuthCallbackPage } from './pages/OAuthCallback';
import { VerifyPage } from './pages/VerifyPage';
import { ChangeCredentialPage } from './pages/ChangeCredential';


function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/sites" element={<SiteSelectionPage />} />
        <Route path="/admin" element={<AdminDashboardPage />} />
        <Route path="/terms-of-service" element={<TermsOfServicePage />} />
        <Route path="/privacy-policy" element={<PrivacyPolicyPage />} />
        <Route path="/oauth-callback" element={<OAuthCallbackPage />} />
        <Route path="/verify" element={<VerifyPage />} />
        {/* Default redirect to login */}
        <Route path="*" element={<Navigate to="/login" replace />} />
        <Route path="/change-credential" element={<ChangeCredentialPage />} />

      </Routes>
    </BrowserRouter>
  );
}

export default App;
