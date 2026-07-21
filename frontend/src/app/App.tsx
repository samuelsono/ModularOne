import { BrowserRouter, Outlet, useRoutes } from 'react-router-dom';
import type { RouteObject } from 'react-router-dom';
import Navigation from './Navigation';
import SideNavigation from '@platform/shell/SideNavigation';
import { GuestRoute, ProtectedRoute } from '@platform/auth/ProtectedRoute';
import { PageSearchProvider } from '@platform/shell/PageSearchContext';
import { ActiveAppProvider } from '@platform/shell/ActiveAppContext';
import { AppThemeProvider } from '@platform/shell/AppThemeProvider';
import { ColorModeProvider } from '@platform/shell/ColorModeContext';
import { useStyles } from '../main';
import { assembleAuthRoutes, assembleProtectedChildRoutes } from './modules';

function Layout() {
  const styles = useStyles();
  return (
    <ActiveAppProvider>
      <ColorModeProvider>
        <AppThemeProvider>
          <PageSearchProvider>
            <div className={`${styles.content} flex flex-col pt-[52px] h-[100vh] overflow-y-hidden`}>
              <Navigation />
              <div className="flex flex-row max-w-[100vw] h-full overflow-hidden">
                <SideNavigation />
                <div className="h-full w-full min-w-0 overflow-hidden">
                  <Outlet />
                </div>
              </div>
            </div>
          </PageSearchProvider>
        </AppThemeProvider>
      </ColorModeProvider>
    </ActiveAppProvider>
  );
}

function AppRoutes() {
  const routes: RouteObject[] = [
    {
      element: <GuestRoute />,
      children: assembleAuthRoutes(),
    },
    {
      element: <ProtectedRoute />,
      children: [
        {
          path: '/',
          element: <Layout />,
          children: assembleProtectedChildRoutes(),
        },
      ],
    },
  ];

  return useRoutes(routes);
}

function App() {
  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  );
}

export default App;
