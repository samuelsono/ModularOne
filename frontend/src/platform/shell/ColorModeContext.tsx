import React from 'react';
import {
  DEFAULT_COLOR_MODE,
  nextColorMode,
  type ColorMode,
} from '../../theme';

const COLOR_MODE_STORAGE_KEY = 'cartrack.colorMode';

function readStoredColorMode(): ColorMode {
  try {
    const raw = localStorage.getItem(COLOR_MODE_STORAGE_KEY);
    if (raw === 'light' || raw === 'dark' || raw === 'system') {
      return raw;
    }
  } catch {
    // Ignore storage access errors (private mode, etc.).
  }

  return DEFAULT_COLOR_MODE;
}

function writeStoredColorMode(mode: ColorMode) {
  try {
    localStorage.setItem(COLOR_MODE_STORAGE_KEY, mode);
  } catch {
    // Ignore storage access errors.
  }
}

function getPrefersDark(): boolean {
  if (typeof window === 'undefined' || !window.matchMedia) {
    return false;
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

interface ColorModeContextValue {
  colorMode: ColorMode;
  prefersDark: boolean;
  resolvedMode: 'light' | 'dark';
  setColorMode: (mode: ColorMode) => void;
  cycleColorMode: () => void;
}

const ColorModeContext = React.createContext<ColorModeContextValue | undefined>(undefined);

export function ColorModeProvider({ children }: { children: React.ReactNode }) {
  const [colorMode, setColorModeState] = React.useState<ColorMode>(() => readStoredColorMode());
  const [prefersDark, setPrefersDark] = React.useState(() => getPrefersDark());

  React.useEffect(() => {
    if (typeof window === 'undefined' || !window.matchMedia) {
      return;
    }

    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const onChange = () => setPrefersDark(media.matches);

    onChange();
    media.addEventListener('change', onChange);
    return () => media.removeEventListener('change', onChange);
  }, []);

  const setColorMode = React.useCallback((mode: ColorMode) => {
    setColorModeState(mode);
    writeStoredColorMode(mode);
  }, []);

  const cycleColorMode = React.useCallback(() => {
    setColorModeState((current) => {
      const next = nextColorMode(current);
      writeStoredColorMode(next);
      return next;
    });
  }, []);

  const resolvedMode: 'light' | 'dark' =
    colorMode === 'system' ? (prefersDark ? 'dark' : 'light') : colorMode;

  React.useEffect(() => {
    document.documentElement.style.colorScheme = resolvedMode;
  }, [resolvedMode]);

  const value = React.useMemo<ColorModeContextValue>(() => ({
    colorMode,
    prefersDark,
    resolvedMode,
    setColorMode,
    cycleColorMode,
  }), [colorMode, prefersDark, resolvedMode, setColorMode, cycleColorMode]);

  return (
    <ColorModeContext.Provider value={value}>
      {children}
    </ColorModeContext.Provider>
  );
}

export function useColorMode(): ColorModeContextValue {
  const context = React.useContext(ColorModeContext);
  if (!context) {
    throw new Error('useColorMode must be used within a ColorModeProvider.');
  }
  return context;
}
