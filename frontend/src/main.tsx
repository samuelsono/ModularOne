import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { FluentProvider, makeStyles, tokens } from '@fluentui/react-components';

import './index.css';
import App from './app/App';
import { AuthProvider } from '@platform/auth/AuthContext';
import { HelpDrawerProvider } from '@modules/help/context/HelpDrawerContext';
import { NotificationProvider } from '@modules/notifications/context/NotificationContext';
import { resolveThemeByName } from './theme';

export const theme = resolveThemeByName();

export const useStyles = makeStyles({
  content: {
    backgroundColor: tokens.colorNeutralBackground3,
  },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <FluentProvider theme={theme}>
      <AuthProvider>
        <NotificationProvider>
          <HelpDrawerProvider>
            <App />
          </HelpDrawerProvider>
        </NotificationProvider>
      </AuthProvider>
    </FluentProvider>
  </StrictMode>,
);
