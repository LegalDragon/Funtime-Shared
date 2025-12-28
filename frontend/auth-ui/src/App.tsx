import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { LoginPage } from './pages/Login';
import { RegisterPage } from './pages/Register';
import { ForgotPasswordPage } from './pages/ForgotPassword';
import { SiteSelectionPage } from './pages/SiteSelection';
import { AdminDashboardPage } from './pages/AdminDashboard';
import { Header } from './components/Header';

function App() {
  return (
    <BrowserRouter>
      <Header />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/sites" element={<SiteSelectionPage />} />
        <Route path="/admin" element={<AdminDashboardPage />} />
        {/* Default redirect to login */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
