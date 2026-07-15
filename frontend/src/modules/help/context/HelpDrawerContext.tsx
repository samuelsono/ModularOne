import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';

interface HelpDrawerContextValue {
  open: boolean;
  setOpen: (open: boolean) => void;
}

const HelpDrawerContext = createContext<HelpDrawerContextValue | null>(null);

export function HelpDrawerProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);

  const value = useMemo(
    () => ({ open, setOpen }),
    [open],
  );

  return (
    <HelpDrawerContext.Provider value={value}>
      {children}
    </HelpDrawerContext.Provider>
  );
}

export function useHelpDrawer() {
  const context = useContext(HelpDrawerContext);
  if (!context) {
    throw new Error('useHelpDrawer must be used within HelpDrawerProvider');
  }
  return context;
}
