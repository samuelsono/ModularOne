export function toDateOnlyString(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function startOfToday(reference = new Date()): Date {
  return new Date(reference.getFullYear(), reference.getMonth(), reference.getDate());
}

export function resolveUpcomingHolidaysEndDate(untilYear: number, reference = new Date()): Date {
  const today = startOfToday(reference);
  const currentYear = today.getFullYear();

  if (untilYear < currentYear) {
    return today;
  }

  const endOfUntilYear = new Date(untilYear, 11, 31);

  if (untilYear > currentYear) {
    return endOfUntilYear;
  }

  const plusThreeMonths = new Date(today);
  plusThreeMonths.setMonth(plusThreeMonths.getMonth() + 3);

  return plusThreeMonths > endOfUntilYear ? plusThreeMonths : endOfUntilYear;
}

export function formatHolidayListDate(value: string): string {
  const parsed = new Date(`${value}T00:00:00`);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleDateString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

export function formatHolidayRangeEndDate(date: Date): string {
  return date.toLocaleDateString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}
