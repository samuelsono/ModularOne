import type { SearchProvider } from '@platform/module/types';

let providers: SearchProvider[] = [];

export function registerSearchProviders(next: SearchProvider[]): void {
  providers = next;
}

export function getRegisteredSearchProviders(): SearchProvider[] {
  return providers;
}

export function getPageSearchPlaceholder(pathname: string): string {
  const match = providers.find((provider) => provider.matchesPath(pathname));
  return match?.placeholder ?? 'Search current page';
}

export function isPageSearchEnabled(pathname: string): boolean {
  return providers.some(
    (provider) => provider.matchesPath(pathname) && provider.enabled !== false,
  );
}
