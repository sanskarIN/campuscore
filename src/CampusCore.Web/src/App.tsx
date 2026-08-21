import type { ReactNode } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { AppShell } from './components/AppShell';
import { AboutPage } from './pages/AboutPage';
import { AcademicsPage } from './pages/AcademicsPage';
import { AnnouncementsPage } from './pages/AnnouncementsPage';
import { CatalogPage } from './pages/CatalogPage';
import { DashboardPage } from './pages/DashboardPage';
import { LoginPage } from './pages/LoginPage';
import { OperationsPage } from './pages/OperationsPage';
import { SearchPage } from './pages/SearchPage';
import { SettingsPage } from './pages/SettingsPage';
import { StaffPage } from './pages/StaffPage';
import { StudentsPage } from './pages/StudentsPage';

function AdministratorRoute({ children }: { children: ReactNode }) {
  const { hasAnyRole } = useAuth();
  return hasAnyRole('Administrator') ? children : <Navigate to="/" replace />;
}

export default function App() {
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return (
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    );
  }

  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<DashboardPage />} />
        <Route path="search" element={<SearchPage />} />
        <Route path="students" element={<StudentsPage />} />
        <Route path="academics" element={<AcademicsPage />} />
        <Route path="operations" element={<OperationsPage />} />
        <Route path="staff" element={<StaffPage />} />
        <Route path="announcements" element={<AnnouncementsPage />} />
        <Route path="catalog" element={<AdministratorRoute><CatalogPage /></AdministratorRoute>} />
        <Route path="settings" element={<AdministratorRoute><SettingsPage /></AdministratorRoute>} />
        <Route path="about" element={<AboutPage />} />
        <Route path="login" element={<Navigate to="/" replace />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
