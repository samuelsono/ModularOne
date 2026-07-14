import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { FluentProvider, makeStyles } from '@fluentui/react-components';

import './index.css';
import App from './App.tsx';
import { AuthProvider } from './context/AuthContext';
import { HelpDrawerProvider } from './context/HelpDrawerContext';
import { NotificationProvider } from './context/NotificationContext';
import { theme } from './theme';

export { theme };

export const useStyles = makeStyles({
    content: {
      backgroundColor: theme.colorNeutralBackground3
    }
})
  
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
