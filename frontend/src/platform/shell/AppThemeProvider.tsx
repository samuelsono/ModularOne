import type { ReactNode } from 'react';
import { FluentProvider } from '@fluentui/react-components';
import { useActiveApp } from '@platform/shell/ActiveAppContext';

interface AppThemeProviderProps {
  children: ReactNode;
}

export function AppThemeProvider({ children }: AppThemeProviderProps) {
  const { currentTheme } = useActiveApp();

  return (
    <FluentProvider theme={currentTheme}>
      {children}
    </FluentProvider>
  );
}
