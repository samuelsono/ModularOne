import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from './AuthContext';
import {
  DEFAULT_MODULE_SLUG,
  buildModuleSidebarNavItems,
  filterAppModules,
  getAppModule,
  resolveModuleFromPath,
  type AppModuleDefinition,
  type SidebarNavEntry,
} from '../utils/permissions';
import { getSettingsOverview } from '../services/settingsService';
import {
  consumeUseDefaultModuleOnLogin,
  readStoredCurrentModule,
  writeStoredCurrentModule,
} from '../utils/appModuleStorage';

function resolveCurrentModuleSlug(
  pathname: string,
  storedSlug: string | null,
  defaultSlug: string,
  visibleSlugs: string[],
): string | null {
  const pathModule = pathname === '/' ? null : resolveModuleFromPath(pathname);

  if (pathModule && visibleSlugs.includes(pathModule)) {
    return pathModule;
  }

  if (storedSlug && visibleSlugs.includes(storedSlug)) {
    return storedSlug;
  }

  if (visibleSlugs.includes(defaultSlug)) {
    return defaultSlug;
  }

  return visibleSlugs[0] ?? null;
}

interface ActiveAppContextValue {
  isLoading: boolean;
  defaultModuleSlug: string;
  visibleModules: AppModuleDefinition[];
  currentModuleSlug: string | null;
  currentModule: AppModuleDefinition | null;
  currentNavItems: SidebarNavEntry[];
  isDefaultModule: (slug: string) => boolean;
  selectModule: (slug: string) => void;
  refreshPlatformSettings: () => Promise<void>;
}

const ActiveAppContext = React.createContext<ActiveAppContextValue | undefined>(undefined);

export function ActiveAppProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const hasInitializedRef = React.useRef(false);
  const pendingLauncherSlugRef = React.useRef<string | null>(null);

  const [isLoading, setIsLoading] = React.useState(true);
  const [defaultModuleSlug, setDefaultModuleSlug] = React.useState(DEFAULT_MODULE_SLUG);
  const [currentModuleSlug, setCurrentModuleSlug] = React.useState<string | null>(DEFAULT_MODULE_SLUG);

  const visibleModules = React.useMemo(
    () => filterAppModules(user),
    [user],
  );

  const visibleSlugs = React.useMemo(
    () => visibleModules.map((module) => module.slug),
    [visibleModules],
  );

  const refreshPlatformSettings = React.useCallback(async () => {
    try {
      const overview = await getSettingsOverview();
      const slug = overview.platform?.defaultModuleSlug ?? DEFAULT_MODULE_SLUG;
      setDefaultModuleSlug(slug);
    } catch {
      setDefaultModuleSlug(DEFAULT_MODULE_SLUG);
    }
  }, []);

  React.useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      if (!user) {
        hasInitializedRef.current = false;
        if (!cancelled) {
          setIsLoading(false);
          setCurrentModuleSlug(DEFAULT_MODULE_SLUG);
        }
        return;
      }

      const isFirstLoad = !hasInitializedRef.current;
      if (isFirstLoad) {
        setIsLoading(true);
      }

      const overview = await getSettingsOverview().catch(() => null);
      if (cancelled) {
        return;
      }

      const adminDefault = overview?.platform?.defaultModuleSlug ?? DEFAULT_MODULE_SLUG;
      const useDefaultOnLogin = consumeUseDefaultModuleOnLogin();

      const storedCurrent = readStoredCurrentModule(user.id);
      const resolvedCurrent = useDefaultOnLogin
        ? (visibleSlugs.includes(adminDefault) ? adminDefault : visibleSlugs[0] ?? null)
        : resolveCurrentModuleSlug(location.pathname, storedCurrent, adminDefault, visibleSlugs);

      setDefaultModuleSlug(adminDefault);
      setCurrentModuleSlug(resolvedCurrent);
      setIsLoading(false);
      hasInitializedRef.current = true;
    }

    void bootstrap();

    return () => {
      cancelled = true;
    };
  }, [user, visibleSlugs]);

  React.useEffect(() => {
    if (!user || !currentModuleSlug) {
      return;
    }

    writeStoredCurrentModule(user.id, currentModuleSlug);
  }, [user, currentModuleSlug]);

  React.useEffect(() => {
    if (!user || isLoading) {
      return;
    }

    const pathModule = resolveModuleFromPath(location.pathname);

    if (pendingLauncherSlugRef.current) {
      const pendingModule = getAppModule(pendingLauncherSlugRef.current);
      const reachedHome = pendingModule
        && (location.pathname === pendingModule.homePath
          || (pendingModule.homePath === '/' && location.pathname === '/'));

      if (reachedHome) {
        pendingLauncherSlugRef.current = null;
      }

      return;
    }

    if (pathModule && pathModule !== currentModuleSlug && visibleSlugs.includes(pathModule)) {
      setCurrentModuleSlug(pathModule);
    }
  }, [location.pathname, user, isLoading, currentModuleSlug, visibleSlugs]);

  React.useEffect(() => {
    if (!user || isLoading || !currentModuleSlug) {
      return;
    }

    const module = getAppModule(currentModuleSlug);
    if (!module) {
      return;
    }

    const isAtModuleHome = location.pathname === module.homePath
      || (module.homePath === '/' && location.pathname === '/');

    if (!isAtModuleHome && location.pathname === '/') {
      navigate(module.homePath, { replace: true });
    }
  }, [user, isLoading, location.pathname, currentModuleSlug, navigate]);

  const isDefaultModule = React.useCallback(
    (slug: string) => slug === defaultModuleSlug,
    [defaultModuleSlug],
  );

  const selectModule = React.useCallback((slug: string) => {
    if (!visibleSlugs.includes(slug)) {
      return;
    }

    const module = getAppModule(slug);
    if (!module) {
      return;
    }

    pendingLauncherSlugRef.current = slug;
    setCurrentModuleSlug(slug);

    if (user) {
      writeStoredCurrentModule(user.id, slug);
    }

    navigate(module.homePath);
  }, [navigate, user, visibleSlugs]);

  const currentModule = currentModuleSlug ? getAppModule(currentModuleSlug) ?? null : null;

  const currentNavItems = React.useMemo(
    () => (currentModuleSlug ? buildModuleSidebarNavItems(user, currentModuleSlug) : []),
    [user, currentModuleSlug],
  );

  const value = React.useMemo<ActiveAppContextValue>(() => ({
    isLoading,
    defaultModuleSlug,
    visibleModules,
    currentModuleSlug,
    currentModule,
    currentNavItems,
    isDefaultModule,
    selectModule,
    refreshPlatformSettings,
  }), [
    isLoading,
    defaultModuleSlug,
    visibleModules,
    currentModuleSlug,
    currentModule,
    currentNavItems,
    isDefaultModule,
    selectModule,
    refreshPlatformSettings,
  ]);

  return (
    <ActiveAppContext.Provider value={value}>
      {children}
    </ActiveAppContext.Provider>
  );
}

export function useActiveApp(): ActiveAppContextValue {
  const context = React.useContext(ActiveAppContext);
  if (!context) {
    throw new Error('useActiveApp must be used within an ActiveAppProvider.');
  }
  return context;
}
