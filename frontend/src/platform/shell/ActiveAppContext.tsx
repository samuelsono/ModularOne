import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '@platform/auth/AuthContext';
import {
  CORE_HOST_MODULE_SLUGS,
  DEFAULT_MODULE_SLUG,
  buildModuleSidebarNavItems,
  filterAppModules,
  filterAppModulesByInstalled,
  getAppModule,
  isCoreRoute,
  resolveCoreHostModuleSlug,
  resolveModuleFromPath,
  type AppModuleDefinition,
  type SidebarNavEntry,
} from '@platform/permissions/apps';
import { getSettingsOverview } from '@platform/api/platformSettingsApi';
import {
  consumeUseDefaultModuleOnLogin,
  readStoredCurrentModule,
  writeStoredCurrentModule,
} from '@platform/utils/appModuleStorage';

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

  // Bare `/` is Fleet home — only treat it as fleet when that app is visible.
  if (pathname === '/' && visibleSlugs.includes('fleet')) {
    return 'fleet';
  }

  // /core/* does not map to its own launcher app — keep Leave/Expense host on refresh.
  if (isCoreRoute(pathname)) {
    const coreHost = resolveCoreHostModuleSlug(storedSlug, visibleSlugs);
    if (coreHost) {
      return coreHost;
    }
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
  const userIdRef = React.useRef<string | null>(null);

  const [isLoading, setIsLoading] = React.useState(true);
  const [defaultModuleSlug, setDefaultModuleSlug] = React.useState(DEFAULT_MODULE_SLUG);
  const [installedAppSlugs, setInstalledAppSlugs] = React.useState<string[]>([]);
  // null until bootstrap — avoids persisting DEFAULT_MODULE_SLUG (fleet) on first paint
  // and wiping the real active module before /core/* refresh resolution runs.
  const [currentModuleSlug, setCurrentModuleSlug] = React.useState<string | null>(null);

  const visibleModules = React.useMemo(
    () => {
      const permissionFiltered = filterAppModules(user);
      // If installed apps are set, filter by them; otherwise show all permission-accessible apps
      if (installedAppSlugs.length > 0) {
        return filterAppModulesByInstalled(permissionFiltered, installedAppSlugs);
      }
      return permissionFiltered;
    },
    [user, installedAppSlugs],
  );

  const visibleSlugs = React.useMemo(
    () => visibleModules.map((module) => module.slug),
    [visibleModules],
  );

  const refreshPlatformSettings = React.useCallback(async () => {
    try {
      const overview = await getSettingsOverview();
      const slug = overview.platform?.defaultModuleSlug ?? DEFAULT_MODULE_SLUG;
      const installed = overview.platform?.installedAppSlugs ?? [];
      setDefaultModuleSlug(slug);
      setInstalledAppSlugs(installed);
    } catch {
      setDefaultModuleSlug(DEFAULT_MODULE_SLUG);
    }
  }, []);

  // Bootstrap once per authenticated user. Do not re-resolve the active module when
  // installed-app filters change — that race was undoing Fleet launcher selections.
  React.useEffect(() => {
    let cancelled = false;
    const userId = user?.id ?? null;

    async function bootstrap() {
      if (!user || !userId) {
        hasInitializedRef.current = false;
        userIdRef.current = null;
        pendingLauncherSlugRef.current = null;
        if (!cancelled) {
          setIsLoading(false);
          setCurrentModuleSlug(DEFAULT_MODULE_SLUG);
        }
        return;
      }

      const userChanged = userIdRef.current !== userId;
      userIdRef.current = userId;

      if (userChanged || !hasInitializedRef.current) {
        setIsLoading(true);
      }

      const overview = await getSettingsOverview().catch(() => null);
      if (cancelled) {
        return;
      }

      const adminDefault = overview?.platform?.defaultModuleSlug ?? DEFAULT_MODULE_SLUG;
      const installed = overview?.platform?.installedAppSlugs ?? [];
      const useDefaultOnLogin = consumeUseDefaultModuleOnLogin();

      setInstalledAppSlugs(installed);
      setDefaultModuleSlug(adminDefault);

      // Only pick the active module on first load / user change — never while switching apps.
      if (userChanged || !hasInitializedRef.current) {
        const permissionSlugs = filterAppModules(user).map((module) => module.slug);
        const installedSet = new Set(installed.map((slug) => slug.toLowerCase()));
        const slugsForResolve = installed.length > 0
          ? permissionSlugs.filter((slug) => installedSet.has(slug.toLowerCase()))
          : permissionSlugs;

        const storedCurrent = readStoredCurrentModule(user.id);
        const resolvedCurrent = useDefaultOnLogin
          ? (slugsForResolve.includes(adminDefault) ? adminDefault : slugsForResolve[0] ?? null)
          : resolveCurrentModuleSlug(location.pathname, storedCurrent, adminDefault, slugsForResolve);

        setCurrentModuleSlug(resolvedCurrent);
        hasInitializedRef.current = true;
      }

      if (!cancelled) {
        setIsLoading(false);
      }
    }

    void bootstrap();

    return () => {
      cancelled = true;
    };
    // Intentionally only re-bootstrap when the user identity changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- location used only on first resolve
  }, [user?.id]);

  React.useEffect(() => {
    if (!user || !currentModuleSlug || !hasInitializedRef.current) {
      return;
    }

    writeStoredCurrentModule(user.id, currentModuleSlug);
  }, [user, currentModuleSlug]);

  React.useEffect(() => {
    if (!user || isLoading) {
      return;
    }

    // While a launcher navigation is in flight, do not let the URL rewrite the active app.
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

    const pathModule = resolveModuleFromPath(location.pathname);

    if (pathModule && pathModule !== currentModuleSlug && visibleSlugs.includes(pathModule)) {
      setCurrentModuleSlug(pathModule);
      return;
    }

    // Bare `/` means Fleet home when fleet is available.
    if (
      location.pathname === '/'
      && currentModuleSlug !== 'fleet'
      && visibleSlugs.includes('fleet')
    ) {
      setCurrentModuleSlug('fleet');
      return;
    }

    // Refresh / deep-link on /core/*: restore Leave/Expense host shell.
    const isCoreHost =
      !!currentModuleSlug
      && (CORE_HOST_MODULE_SLUGS as readonly string[]).includes(currentModuleSlug);
    if (isCoreRoute(location.pathname) && !isCoreHost) {
      const coreHost = resolveCoreHostModuleSlug(
        currentModuleSlug ?? readStoredCurrentModule(user.id),
        visibleSlugs,
      );
      if (coreHost && coreHost !== currentModuleSlug) {
        setCurrentModuleSlug(coreHost);
      }
    }
  }, [location.pathname, user, isLoading, currentModuleSlug, visibleSlugs]);

  React.useEffect(() => {
    if (!user || isLoading || !currentModuleSlug) {
      return;
    }

    // Do not bounce away from `/` while a launcher switch (especially to Fleet) is pending.
    if (pendingLauncherSlugRef.current) {
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
