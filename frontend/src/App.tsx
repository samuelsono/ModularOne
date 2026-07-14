import Navigation from './components/Navigation';
import SideNavigation from './components/SideNavigation';
import { BrowserRouter, Outlet, Route, Routes } from 'react-router-dom';
import Dashboard from './page/Dashboard';
import VehicleList from './page/VehicleList';
import DriversList from './page/DriversList';
import LiveTracking from './page/LiveTracking';
import ReportsPage from './page/ReportsPage';
import SettingsPage from './page/SettingsPage';
import ModulePlaceholder from './page/ModulePlaceholder';
import { useStyles } from './main';
import AuthLayout from './page/auth/AuthLayout';
import LoginPage from './page/auth/Login';
import ForgotPassword from './page/auth/ForgotPassword';
import ResetPasswordPage, { SetupAccountPage } from './page/auth/ResetPassword';
import { GuestRoute, ProtectedRoute } from './components/ProtectedRoute';
import { PageSearchProvider } from './context/PageSearchContext';
import { ActiveAppProvider } from './context/ActiveAppContext';
import EmployeesList from './page/EmployeesList';
import LeaveLayout from './page/leave/LeaveLayout';
import LeaveRequestsPage from './page/leave/LeaveRequestsPage';
import LeaveApprovalsPage from './page/leave/LeaveApprovalsPage';
import LeavePoliciesPage from './page/leave/LeavePoliciesPage';
import LeaveBalancesPage from './page/leave/LeaveBalancesPage';
import LeaveCalendarPage from './page/leave/LeaveCalendarPage';
import LeaveReportsPage from './page/leave/LeaveReportsPage';
import ExpenseLayout from './page/expense/ExpenseLayout';
import ExpenseClaimsPage from './page/expense/ExpenseClaimsPage';
import ExpenseApprovalsPage from './page/expense/ExpenseApprovalsPage';
import ExpenseCategoriesPage from './page/expense/ExpenseCategoriesPage';
import ExpenseReportsPage from './page/expense/ExpenseReportsPage';
import ExpenseBalancesPage from './page/expense/ExpenseBalancesPage';
import CoreLayout from './page/core/CoreLayout';
import CompaniesPage from './page/core/CompaniesPage';
import DepartmentsPage from './page/core/DepartmentsPage';
import PositionsPage from './page/core/PositionsPage';

function Layout() {
  const styles = useStyles();
  return (
    <ActiveAppProvider>
      <PageSearchProvider>
        <div className="flex flex-col pt-[52px] h-[100vh] overflow-y-hidden">
          <Navigation />
          <div className="flex flex-row max-w-[100vw] h-full overflow-hidden">
            <SideNavigation />
            <div className={`${styles.content} h-full w-full min-w-0 overflow-hidden`}>
              <Outlet />
            </div>
          </div>
        </div>
      </PageSearchProvider>
    </ActiveAppProvider>
  );
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<GuestRoute />}>
          <Route path="/auth" element={<AuthLayout />}>
            <Route index element={<LoginPage />} />
            <Route path="login" element={<LoginPage />} />
            <Route path="forgot-password" element={<ForgotPassword />} />
            <Route path="reset-password" element={<ResetPasswordPage />} />
            <Route path="setup-account" element={<SetupAccountPage />} />
          </Route>
        </Route>

        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<Layout />}>
            <Route index element={<Dashboard />} />
            <Route path="vehicle-list" element={<VehicleList />} />
            <Route path="live-tracking" element={<LiveTracking />} />
            <Route path="drivers" element={<DriversList />} />
            <Route path="employees" element={<EmployeesList />} />
            <Route path="reports" element={<ReportsPage />} />
            <Route path="settings" element={<SettingsPage />} />
            <Route path="accounting/*" element={<ModulePlaceholder />} />
            <Route path="leave" element={<LeaveLayout />}>
              <Route index element={<LeaveReportsPage />} />
              <Route path="requests" element={<LeaveRequestsPage />} />
              <Route path="approvals" element={<LeaveApprovalsPage />} />
              <Route path="calendar" element={<LeaveCalendarPage />} />
              <Route path="policies" element={<LeavePoliciesPage />} />
              <Route path="balances" element={<LeaveBalancesPage />} />
            </Route>
            <Route path="expense" element={<ExpenseLayout />}>
              <Route index element={<ExpenseClaimsPage />} />
              <Route path="approvals" element={<ExpenseApprovalsPage />} />
              <Route path="categories" element={<ExpenseCategoriesPage />} />
              <Route path="reports" element={<ExpenseReportsPage />} />
              <Route path="balances" element={<ExpenseBalancesPage />} />
            </Route>
            <Route path="core" element={<CoreLayout />}>
              <Route path="companies" element={<CompaniesPage />} />
              <Route path="departments" element={<DepartmentsPage />} />
              <Route path="positions" element={<PositionsPage />} />
              <Route path="employees" element={<EmployeesList />} />
            </Route>
            
            <Route path="payroll/*" element={<ModulePlaceholder />} />
            <Route path="performance/*" element={<ModulePlaceholder />} />
            <Route path="recruitment/*" element={<ModulePlaceholder />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
