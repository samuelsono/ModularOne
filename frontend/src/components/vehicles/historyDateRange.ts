import {
  formatDateOnlyForApi,
  parseDateOnly,
} from '../../utils/dateOnly';

export function toDateInputValue(date: Date): string {
  return formatDateOnlyForApi(date);
}

export function getTodayDateInputValue(): string {
  return toDateInputValue(new Date());
}

export function parseDateInputValue(value: string): Date | undefined {
  return parseDateOnly(value);
}

export function buildRangeFromDateInputs(startDate: string, endDate: string): { start: Date; end: Date } | null {
  if (!startDate || !endDate) {
    return null;
  }

  const start = parseDateOnly(startDate);
  const end = parseDateOnly(endDate);

  if (!start || !end) {
    return null;
  }

  const endOfDay = new Date(end);
  endOfDay.setHours(23, 59, 59, 999);

  if (endOfDay < start) {
    return null;
  }

  const now = new Date();
  if (endDate === toDateInputValue(now) && endOfDay > now) {
    return { start, end: now };
  }

  return { start, end: endOfDay };
}

export function formatDateRangeLabel(startDate: string, endDate: string): string {
  if (!startDate || !endDate) {
    return 'Select a date range';
  }

  if (startDate === endDate) {
    return startDate === getTodayDateInputValue() ? "Today's data" : startDate;
  }

  return `${startDate} to ${endDate}`;
}
