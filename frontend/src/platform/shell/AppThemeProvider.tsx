import type { ReactNode } from 'react';
import { useMemo } from 'react';
import { FluentProvider } from '@fluentui/react-components';
import { useActiveApp } from '@platform/shell/ActiveAppContext';
import { useColorMode } from '@platform/shell/ColorModeContext';
import { resolveEffectiveThemeName, resolveThemeByName } from '../../theme';

interface AppThemeProviderProps {
  children: ReactNode;
}

export function AppThemeProvider({ children }: AppThemeProviderProps) {
  const { currentThemeName } = useActiveApp();
  const { colorMode, prefersDark } = useColorMode();

  const theme = useMemo(() => {
    const effectiveName = resolveEffectiveThemeName(
      currentThemeName,
      colorMode,
      prefersDark,
    );
    return resolveThemeByName(effectiveName);
  }, [currentThemeName, colorMode, prefersDark]);

  return (
    <FluentProvider theme={theme}>
      {children}
    </FluentProvider>
  );
}
