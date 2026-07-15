import type { ModuleDefinition } from '@platform/module/types';
import AuthLayout from './pages/AuthLayout';
import LoginPage from './pages/Login';
import ForgotPassword from './pages/ForgotPassword';
import ResetPasswordPage, { SetupAccountPage } from './pages/ResetPassword';

export const authModule: ModuleDefinition = {
  id: 'auth',
  routes: [
    {
      path: '/auth',
      element: <AuthLayout />,
      children: [
        { index: true, element: <LoginPage /> },
        { path: 'login', element: <LoginPage /> },
        { path: 'forgot-password', element: <ForgotPassword /> },
        { path: 'reset-password', element: <ResetPasswordPage /> },
        { path: 'setup-account', element: <SetupAccountPage /> },
      ],
    },
  ],
  navItems: [],
};
