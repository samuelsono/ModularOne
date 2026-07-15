export function normalizeSearchQuery(query: string): string {
  return query.trim().toLowerCase();
}

export function matchesSearchQuery(query: string, values: Array<string | number | null | undefined>): boolean {
  const normalized = normalizeSearchQuery(query);
  if (!normalized) {
    return true;
  }

  const haystack = values
    .filter((value) => value != null && String(value).trim() !== '')
    .join(' ')
    .toLowerCase();

  return haystack.includes(normalized);
}
