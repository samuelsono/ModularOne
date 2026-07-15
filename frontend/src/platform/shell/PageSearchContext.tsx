import * as React from 'react';
import { useLocation } from 'react-router-dom';

import { getPageSearchPlaceholder, isPageSearchEnabled } from '@platform/search/registry';

interface PageSearchContextValue {
  query: string;
  setQuery: (query: string) => void;
  placeholder: string;
  enabled: boolean;
}

const PageSearchContext = React.createContext<PageSearchContextValue | null>(null);

export function PageSearchProvider({ children }: { children: React.ReactNode }) {
  const location = useLocation();
  const [query, setQuery] = React.useState('');
  const placeholder = getPageSearchPlaceholder(location.pathname);
  const enabled = isPageSearchEnabled(location.pathname);

  React.useEffect(() => {
    setQuery('');
  }, [location.pathname]);

  const value = React.useMemo(
    () => ({ query, setQuery, placeholder, enabled }),
    [enabled, placeholder, query],
  );

  return (
    <PageSearchContext.Provider value={value}>
      {children}
    </PageSearchContext.Provider>
  );
}

export function usePageSearch(): PageSearchContextValue {
  const context = React.useContext(PageSearchContext);
  if (!context) {
    throw new Error('usePageSearch must be used within PageSearchProvider');
  }

  return context;
}

export function usePageSearchQuery(): string {
  return usePageSearch().query;
}
