import { Button, Field } from '@fluentui/react-components';
import { DatePicker } from '@fluentui/react-datepicker-compat';
import { SearchRegular } from '@fluentui/react-icons';
import {
  getTodayDateInputValue,
  parseDateInputValue,
  toDateInputValue,
} from './historyDateRange';

interface VehicleHistoryDateFiltersProps {
  startDate: string;
  endDate: string;
  isLoading?: boolean;
  onStartDateChange: (value: string) => void;
  onEndDateChange: (value: string) => void;
  onApply: () => void;
}

export function VehicleHistoryDateFilters({
  startDate,
  endDate,
  isLoading = false,
  onStartDateChange,
  onEndDateChange,
  onApply,
}: VehicleHistoryDateFiltersProps) {
  const today = getTodayDateInputValue();
  const isInvalidRange = Boolean(startDate && endDate && endDate < startDate);
  const startDateValue = parseDateInputValue(startDate);
  const endDateValue = parseDateInputValue(endDate);
  const maxSelectableDate = parseDateInputValue(today);

  return (
    <div className="mt-3 flex flex-col gap-3 rounded border border-neutral-stroke-3 bg-neutral-background-2 p-3">
      <div className="grid grid-cols-2 gap-3">
        <Field label="Start date">
          <DatePicker
            placeholder="Select start date..."
            value={startDateValue}
            maxDate={endDateValue ?? maxSelectableDate}
            disabled={isLoading}
            onSelectDate={(date) => onStartDateChange(date ? toDateInputValue(date) : '')}
          />
        </Field>
        <Field label="End date">
          <DatePicker
            placeholder="Select end date..."
            value={endDateValue}
            minDate={startDateValue}
            maxDate={maxSelectableDate}
            disabled={isLoading}
            onSelectDate={(date) => onEndDateChange(date ? toDateInputValue(date) : '')}
          />
        </Field>
      </div>

      {isInvalidRange && (
        <span className="text-xs text-[#b10e1c]">End date must be on or after the start date.</span>
      )}

      <Button
        appearance="primary"
        icon={<SearchRegular />}
        disabled={isLoading || !startDate || !endDate || isInvalidRange}
        onClick={onApply}
      >
        Apply range
      </Button>
    </div>
  );
}
