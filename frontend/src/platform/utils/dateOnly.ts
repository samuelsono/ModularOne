const DATE_ONLY_PATTERN = /^(\d{4})-(\d{2})-(\d{2})/;

/**
 * Parses an API date-only string (yyyy-MM-dd) as local calendar date.
 * Avoids UTC interpretation that shifts the day for GMT+2 users.
 */
export function parseDateOnly(value: string | null | undefined): Date | undefined {
  if (!value) {
    return undefined;
  }

  const match = DATE_ONLY_PATTERN.exec(value.trim());
  if (!match) {
    return undefined;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (month < 1 || month > 12 || day < 1 || day > 31) {
    return undefined;
  }

  const date = new Date(year, month - 1, day);
  if (
    date.getFullYear() !== year
    || date.getMonth() !== month - 1
    || date.getDate() !== day
  ) {
    return undefined;
  }

  return date;
}

/**
 * Formats a local Date to yyyy-MM-dd without UTC conversion.
 */
export function formatDateOnlyForApi(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function formatDateOnlyDisplay(
  value: string | null | undefined,
  locale = 'en-ZA',
  fallback = '—',
): string {
  const date = parseDateOnly(value);
  if (!date) {
    return fallback;
  }

  return date.toLocaleDateString(locale, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

export function isDateOnlyExpired(value: string | null | undefined): boolean {
  const date = parseDateOnly(value);
  if (!date) {
    return false;
  }

  const today = startOfLocalDay(new Date());
  return date < today;
}

export function isDateOnlyExpiringSoon(
  value: string | null | undefined,
  withinDays = 90,
): boolean {
  const date = parseDateOnly(value);
  if (!date) {
    return false;
  }

  const today = startOfLocalDay(new Date());
  const limit = new Date(today);
  limit.setDate(limit.getDate() + withinDays);

  return date >= today && date <= limit;
}

function startOfLocalDay(date: Date): Date {
  const normalized = new Date(date);
  normalized.setHours(0, 0, 0, 0);
  return normalized;
}
