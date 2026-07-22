import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Badge,
  Card,
  Field,
  MessageBar,
  MessageBarBody,
  SpinButton,
  Spinner,
  Subtitle2,
  Text,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import { getUpcomingPublicHolidays } from '@modules/leave/services/leaveService';
import type { PublicHoliday } from '@modules/leave/types/leave';
import {
  formatHolidayListDate,
  formatHolidayRangeEndDate,
  resolveUpcomingHolidaysEndDate,
} from '@modules/leave/utils/holidayRange';

const YEAR_RANGE = 5;

export function UpcomingHolidaysList() {
  const currentYear = new Date().getFullYear();
  const [untilYear, setUntilYear] = useState(currentYear);
  const [holidays, setHolidays] = useState<PublicHoliday[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const rangeEndDate = useMemo(
    () => resolveUpcomingHolidaysEndDate(untilYear),
    [untilYear],
  );

  const loadHolidays = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getUpcomingPublicHolidays(untilYear);
      setHolidays(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load upcoming holidays.');
      setHolidays([]);
    } finally {
      setIsLoading(false);
    }
  }, [untilYear]);

  useEffect(() => {
    void loadHolidays();
  }, [loadHolidays]);

  return (
    <Card className="relative px-4 max-h-[80vh] overflow-y-scroll w-full">
      <div className="absolute top-0 left-0 right-0 flex flex-wrap items-end justify-between gap-3 mb-3 px-3 w-full! pt-3">
        <div className="flex flex-col gap-1">
          <Subtitle2>Upcoming holidays</Subtitle2>
          <Text size={200} className="text-neutral-foreground-3 max-w-[250px]">
            Showing holidays through {formatHolidayRangeEndDate(rangeEndDate)}
            {untilYear === currentYear ? ' (plus 3 months when needed)' : ''}
          </Text>
        </div>

        <Field label="Show Until Year" className="w-full 3xl:w-[120px]">
          <SpinButton
            value={untilYear}
            min={currentYear}
            max={currentYear + YEAR_RANGE}
            onChange={(_, data) => {
              if (typeof data.value === 'number' && Number.isFinite(data.value)) {
                setUntilYear(data.value);
              }
            }}
          />
        </Field>
      </div>

      {error ? (
        <MessageBar intent="error" className='mt-38'>
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {isLoading ? (
        <Spinner label="Loading holidays..." size="small"  className='mt-38'/>
      ) : holidays.length === 0 ? (
        <Text className="text-sm text-neutral-foreground-3">No upcoming holidays in this range.</Text>
      ) : (
        <ul className="flex flex-col overflow-y-scroll mt-36! 3xl:mt-22!">
          {holidays.map((holiday, index) => (
            <li
              key={`${holiday.id}-${holiday.date}`}
              className="flex flex-wrap items-center justify-between gap-2 py-2 first:pt-0 last:pb-0"
              style={
                index < holidays.length - 1
                  ? { borderBottom: 'var(--strokeWidthThin) solid var(--colorNeutralStroke3)' }
                  : undefined
              }
            >
              <div className="flex flex-col gap-0.5 min-w-0">
                <Text weight="semibold">{holiday.name}</Text>
                <Text size={200} className="text-neutral-foreground-3">
                  {formatHolidayListDate(holiday.date)}
                  {holiday.branch ? ` · ${holiday.branch}` : ' · All branches'}
                </Text>
              </div>

              <div className="flex items-center gap-2 shrink-0">
                {holiday.isRecurring ? (
                  <Badge appearance="outline" color="informative" size="small">
                    Recurring
                  </Badge>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
