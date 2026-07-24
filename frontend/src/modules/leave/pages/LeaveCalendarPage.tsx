import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  tokens,
  Badge,
  Button,
  Combobox,
  Dropdown,
  Field,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Tab,
  TabList,
  Text,
  Tooltip,
} from '@fluentui/react-components';
import { ChevronLeftRegular, ChevronRightRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { getLeaveCalendar, getResolvedSchedule } from '@modules/leave/services/leaveService';
import type { LeaveCalendarEntry, LeaveCalendarResponse, PublicHoliday, ResolvedScheduleDay } from '@modules/leave/types/leave';
import AppTitle from '@platform/ui/AppTitle';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { filterLeaveCalendarEntries } from '@modules/leave/search/filters';
import { LeaveRequestForm } from '@modules/leave/components/LeaveRequestForm';
import { usePermissions } from '@platform/permissions/usePermissions';
import { getUsers } from '@modules/users/services/userService';
import type { UserListItem } from '@modules/users/types/user';

const WEEKDAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const WFH_LOCATION_CODE = 'WFH';
const EMPLOYEE_SEARCH_LIMIT = 10;

type CalendarContentFilter = 'all' | 'leave' | 'holidays' | 'wfh';

const CONTENT_FILTER_OPTIONS: Array<{ value: CalendarContentFilter; label: string }> = [
  { value: 'all', label: 'All' },
  { value: 'leave', label: 'Leave' },
  { value: 'holidays', label: 'Holidays' },
  { value: 'wfh', label: 'WFH schedule' },
];

function toDateString(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function formatMonthLabel(date: Date): string {
  return date.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
}

function getMonthRange(month: Date): { start: string; end: string } {
  const start = new Date(month.getFullYear(), month.getMonth(), 1);
  const end = new Date(month.getFullYear(), month.getMonth() + 1, 0);
  return { start: toDateString(start), end: toDateString(end) };
}

function getSundayBasedDayIndex(date: Date): number {
  return date.getDay();
}

function statusBadgeColor(status: string): 'success' | 'informative' | 'warning' {
  if (status === 'Approved') {
    return 'success';
  }

  if (status === 'Pending') {
    return 'warning';
  }

  return 'informative';
}

interface CalendarWeek {
  days: Array<Date | null>;
}

function buildMonthWeeks(month: Date): CalendarWeek[] {
  const year = month.getFullYear();
  const monthIndex = month.getMonth();
  const daysInMonth = new Date(year, monthIndex + 1, 0).getDate();
  const leadingEmpty = getSundayBasedDayIndex(new Date(year, monthIndex, 1));

  const cells: Array<Date | null> = [
    ...Array.from({ length: leadingEmpty }, () => null),
    ...Array.from({ length: daysInMonth }, (_, index) => new Date(year, monthIndex, index + 1)),
  ];

  while (cells.length % 7 !== 0) {
    cells.push(null);
  }

  const weeks: CalendarWeek[] = [];
  for (let index = 0; index < cells.length; index += 7) {
    weeks.push({ days: cells.slice(index, index + 7) });
  }

  return weeks;
}

function LeaveDayCell({
  day,
  entries,
  holidays,
  scheduleDays,
  canCreate,
  onCreateLeave,
}: {
  day: Date;
  entries: LeaveCalendarEntry[];
  holidays: PublicHoliday[];
  scheduleDays: ResolvedScheduleDay[];
  canCreate: boolean;
  onCreateLeave: (date: Date) => void;
}) {
  const isWeekend = day.getDay() === 0 || day.getDay() === 6;
  const isToday = toDateString(day) === toDateString(new Date());
  const visibleEntries = entries.slice(0, 3);
  const hiddenCount = entries.length - visibleEntries.length;
  const workLocations = scheduleDays
    .filter((item) => item.kind === 'Work' && item.locationTypeName)
    .slice(0, 2);
  const hiddenLocations = Math.max(0, scheduleDays.filter((item) => item.kind === 'Work').length - workLocations.length);
  const showEmptyPlaceholder = entries.length === 0 && holidays.length === 0 && workLocations.length === 0;

  return (
    <td
      className={`align-top border p-2 min-w-[120px] h-[120px] vertical-align-top ${
        isToday ? 'ring-2 ring-inset ring-blue-500' : ''
      } ${canCreate ? 'cursor-pointer' : ''}`}
      style={{
        borderColor: tokens.colorNeutralStroke3,
        backgroundColor: isWeekend
          ? tokens.colorNeutralBackground2
          : tokens.colorNeutralBackground1,
      }}
      title={canCreate ? 'Double-click to create a leave application' : undefined}
      onDoubleClick={() => {
        if (canCreate) {
          onCreateLeave(day);
        }
      }}
    >
      <div className="flex items-start justify-between gap-1 mb-1">
        <Text
          weight="semibold"
          size={200}
          className="rounded-full"
          style={{
            padding: '0.125rem 0.125rem',
            backgroundColor: isToday ? tokens.colorBrandBackground : '',
            color: isToday ? tokens.colorNeutralForegroundInverted : '',
          }}
        >
          {day.getDate()}
        </Text>
        {holidays.length > 0 ? (
          <Tooltip
            content={holidays.map((holiday) => holiday.name).join(', ')}
            relationship="label"
          >
            <Badge appearance="outline" color="informative" size="small">
              Holiday
            </Badge>
          </Tooltip>
        ) : null}
      </div>

      <div className="flex flex-col gap-1">
        {workLocations.map((item) => (
          <Tooltip
            key={`${item.userId}-${item.date}-schedule`}
            content={`${item.userDisplayName} · ${item.locationTypeName}${item.fromOverride ? ' (override)' : ''}`}
            relationship="description"
          >
            <div
              className="flex items-center gap-1 rounded px-1.5 py-0.5 text-xs truncate"
              style={{ backgroundColor: `${item.locationTypeColor ?? '#605E5C'}22` }}
            >
              <span
                className="inline-block w-2 h-2 rounded-full shrink-0"
                style={{ backgroundColor: item.locationTypeColor ?? '#605E5C' }}
              />
              <span className="truncate">{item.locationTypeName}</span>
            </div>
          </Tooltip>
        ))}
        {hiddenLocations > 0 ? (
          <Text size={100} className="text-neutral-foreground-3 px-1">
            +{hiddenLocations} locations
          </Text>
        ) : null}

        {visibleEntries.map((entry) => (
          <Tooltip
            key={`${entry.requestId}-${toDateString(day)}`}
            content={`${entry.displayName} · ${entry.leaveTypeName} · ${entry.status}${
              entry.department ? ` · ${entry.department}` : ''
            }`}
            relationship="description"
          >
            <div
              className="flex items-center gap-1 rounded px-1.5 py-0.5 text-xs truncate cursor-default"
              style={{ backgroundColor: `${entry.leaveTypeColor}22` }}
            >
              <span
                className="inline-block w-2 h-2 rounded-full shrink-0"
                style={{ backgroundColor: entry.leaveTypeColor }}
              />
              <span className="truncate font-medium">{entry.displayName}</span>
              <Badge appearance="outline" color={statusBadgeColor(entry.status)} size="extra-small">
                {entry.status === 'Pending' ? 'P' : entry.status === 'Approved' ? 'A' : entry.status[0]}
              </Badge>
            </div>
          </Tooltip>
        ))}

        {hiddenCount > 0 ? (
          <Text size={100} className="text-neutral-foreground-3 px-1">
            +{hiddenCount} more
          </Text>
        ) : null}

        {showEmptyPlaceholder ? (
          <Text size={100} className="text-neutral-foreground-3">—</Text>
        ) : null}
      </div>
    </td>
  );
}

export default function LeaveCalendarPage() {
  const searchQuery = usePageSearchQuery();
  const { hasPermission } = usePermissions();
  const canCreateLeave = hasPermission('leave.requests.write');
  const canReadSchedule = hasPermission('leave.schedule.read');
  const [month, setMonth] = useState(() => {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), 1);
  });
  const [branchFilter, setBranchFilter] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState('');
  const [contentFilter, setContentFilter] = useState<CalendarContentFilter>('all');
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(null);
  const [employeeQuery, setEmployeeQuery] = useState('');
  const [employeeSearchActive, setEmployeeSearchActive] = useState(false);
  const [employees, setEmployees] = useState<UserListItem[]>([]);
  const [calendar, setCalendar] = useState<LeaveCalendarResponse | null>(null);
  const [scheduleDays, setScheduleDays] = useState<ResolvedScheduleDay[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [formStartDate, setFormStartDate] = useState<string | null>(null);

  const loadCalendar = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    const range = getMonthRange(month);

    try {
      const data = await getLeaveCalendar({
        start: range.start,
        end: range.end,
        branch: branchFilter || undefined,
        department: departmentFilter || undefined,
      });
      setCalendar(data);

      if (canReadSchedule) {
        try {
          const resolved = await getResolvedSchedule(range.start, range.end);
          setScheduleDays(resolved.filter((item) => item.kind === 'Work'));
        } catch {
          setScheduleDays([]);
        }
      } else {
        setScheduleDays([]);
      }
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load leave calendar.');
      setCalendar(null);
      setScheduleDays([]);
    } finally {
      setIsLoading(false);
    }
  }, [branchFilter, canReadSchedule, departmentFilter, month]);

  useEffect(() => {
    void loadCalendar();
  }, [loadCalendar]);

  useEffect(() => {
    void getUsers()
      .then((response) => setEmployees((response.items ?? []).filter((item) => item.isActive)))
      .catch(() => setEmployees([]));
  }, []);

  const weeks = useMemo(() => buildMonthWeeks(month), [month]);

  const selectedEmployee = useMemo(
    () => employees.find((item) => item.id === selectedEmployeeId) ?? null,
    [employees, selectedEmployeeId],
  );

  const selectedEmployeeLabel = selectedEmployee?.displayName
    ?? selectedEmployee?.email
    ?? selectedEmployee?.username
    ?? '';

  const employeeComboboxValue = employeeSearchActive
    ? employeeQuery
    : selectedEmployeeLabel;

  const employeeOptions = useMemo(() => {
    const query = employeeQuery.trim().toLowerCase();
    const matched = !query
      ? employees
      : employees.filter((item) => {
          const label = `${item.displayName ?? ''} ${item.email ?? ''} ${item.username ?? ''}`.toLowerCase();
          return label.includes(query);
        });

    return matched.slice(0, EMPLOYEE_SEARCH_LIMIT);
  }, [employeeQuery, employees]);

  const holidaysByDate = useMemo(() => {
    const map = new Map<string, PublicHoliday[]>();
    if (contentFilter === 'leave' || contentFilter === 'wfh') {
      return map;
    }

    for (const holiday of calendar?.holidays ?? []) {
      const items = map.get(holiday.date) ?? [];
      items.push(holiday);
      map.set(holiday.date, items);
    }
    return map;
  }, [calendar?.holidays, contentFilter]);

  const filteredEntries = useMemo(() => {
    if (contentFilter === 'holidays' || contentFilter === 'wfh') {
      return [];
    }

    let entries = filterLeaveCalendarEntries(calendar?.entries ?? [], searchQuery);
    if (selectedEmployeeId) {
      entries = entries.filter((entry) => entry.userId === selectedEmployeeId);
    }
    return entries;
  }, [calendar?.entries, contentFilter, searchQuery, selectedEmployeeId]);

  const entriesByDate = useMemo(() => {
    const map = new Map<string, LeaveCalendarEntry[]>();
    for (const entry of filteredEntries) {
      const start = new Date(`${entry.startDate}T00:00:00`);
      const end = new Date(`${entry.endDate}T00:00:00`);
      const cursor = new Date(start);

      while (cursor <= end) {
        const key = toDateString(cursor);
        const items = map.get(key) ?? [];
        if (!items.some((item) => item.requestId === entry.requestId)) {
          items.push(entry);
        }
        map.set(key, items);
        cursor.setDate(cursor.getDate() + 1);
      }
    }

    for (const [key, items] of map.entries()) {
      map.set(
        key,
        [...items].sort((a, b) => a.displayName.localeCompare(b.displayName)),
      );
    }

    return map;
  }, [filteredEntries]);

  const scheduleByDate = useMemo(() => {
    const map = new Map<string, ResolvedScheduleDay[]>();
    if (contentFilter === 'leave' || contentFilter === 'holidays' || !canReadSchedule) {
      return map;
    }

    for (const day of scheduleDays) {
      if (contentFilter === 'wfh'
        && day.locationTypeCode?.toUpperCase() !== WFH_LOCATION_CODE) {
        continue;
      }

      if (selectedEmployeeId && day.userId !== selectedEmployeeId) {
        continue;
      }

      const items = map.get(day.date) ?? [];
      items.push(day);
      map.set(day.date, items);
    }
    return map;
  }, [canReadSchedule, contentFilter, scheduleDays, selectedEmployeeId]);

  const leaveTypes = useMemo(() => {
    const types = new Map<string, { name: string; color: string }>();
    for (const entry of filteredEntries) {
      types.set(entry.leaveTypeId, {
        name: entry.leaveTypeName,
        color: entry.leaveTypeColor,
      });
    }
    return [...types.values()];
  }, [filteredEntries]);

  function handleCreateLeaveForDay(day: Date) {
    if (!canCreateLeave) {
      return;
    }

    const dateKey = toDateString(day);
    setFormStartDate(dateKey);
    setFormOpen(true);
  }

  return (
    <div className="flex flex-col gap-4 h-full overflow-auto px-3 pb-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <AppTitle title="Team Calendar" subtitle="View pending and approved leave for your team this month." />

        <div className="flex items-center justify-center gap-2">
          <Button
            appearance="subtle"
            icon={<ChevronLeftRegular />}
            onClick={() => setMonth((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))}
          />
          <Text className="min-w-[120px] text-center! font-semibold">{formatMonthLabel(month)}</Text>
          <Button
            appearance="subtle"
            icon={<ChevronRightRegular />}
            onClick={() => setMonth((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1))}
          />
        </div>
      </div>

      <div
        className="sticky top-0 z-10 flex flex-col gap-3 py-2"
        style={{ backgroundColor: tokens.colorNeutralBackground1 }}
      >
        <TabList
          selectedValue={contentFilter}
          onTabSelect={(_, data) => {
            const next = String(data.value) as CalendarContentFilter;
            setContentFilter(CONTENT_FILTER_OPTIONS.some((item) => item.value === next) ? next : 'all');
          }}
        >
          {CONTENT_FILTER_OPTIONS.map((option) => (
            <Tab key={option.value} value={option.value}>
              {option.label}
            </Tab>
          ))}
        </TabList>

        <div className="flex flex-wrap gap-2 items-end">
          <Field label="Employee" style={{ minWidth: 220 }}>
            <Combobox
              placeholder="All employees"
              freeform
              value={employeeComboboxValue}
              selectedOptions={selectedEmployeeId ? [selectedEmployeeId] : []}
              onFocus={() => {
                setEmployeeSearchActive(true);
                setEmployeeQuery('');
              }}
              onBlur={() => {
                // Defer so option click can commit before we leave search mode.
                window.setTimeout(() => {
                  setEmployeeSearchActive(false);
                  setEmployeeQuery('');
                }, 120);
              }}
              onChange={(event) => {
                setEmployeeSearchActive(true);
                setEmployeeQuery(event.target.value);
              }}
              onOptionSelect={(_, data) => {
                const value = data.optionValue;
                if (!value || value === '__all__') {
                  setSelectedEmployeeId(null);
                } else {
                  setSelectedEmployeeId(value);
                }
                setEmployeeQuery('');
                setEmployeeSearchActive(false);
              }}
            >
              <Option value="__all__" text="All employees">All employees</Option>
              {employeeOptions.map((employee) => (
                <Option
                  key={employee.id}
                  value={employee.id}
                  text={employee.displayName ?? employee.email ?? employee.username}
                >
                  {employee.displayName ?? employee.email ?? employee.username}
                </Option>
              ))}
            </Combobox>
          </Field>

          <Field label="Branch" style={{ minWidth: 160 }}>
            <Dropdown
              placeholder="All branches"
              value={branchFilter || 'All branches'}
              selectedOptions={branchFilter ? [branchFilter] : ['']}
              onOptionSelect={(_, data) => setBranchFilter(data.optionValue === '' ? '' : data.optionValue ?? '')}
            >
              <Option value="">All branches</Option>
              {(calendar?.branches ?? []).map((branch) => (
                <Option key={branch} value={branch}>{branch}</Option>
              ))}
            </Dropdown>
          </Field>

          <Field label="Department" style={{ minWidth: 160 }}>
            <Dropdown
              placeholder="All departments"
              value={departmentFilter || 'All departments'}
              selectedOptions={departmentFilter ? [departmentFilter] : ['']}
              onOptionSelect={(_, data) => setDepartmentFilter(data.optionValue === '' ? '' : data.optionValue ?? '')}
            >
              <Option value="">All departments</Option>
              {(calendar?.departments ?? []).map((department) => (
                <Option key={department} value={department}>{department}</Option>
              ))}
            </Dropdown>
          </Field>
        </div>
      </div>

      {leaveTypes.length > 0 || contentFilter === 'all' || contentFilter === 'holidays' || contentFilter === 'wfh' ? (
        <div className="flex flex-wrap gap-3">
          {leaveTypes.map((type) => (
            <span key={type.name} className="inline-flex items-center gap-2 text-sm">
              <span
                className="inline-block w-3 h-3 rounded-full shrink-0"
                style={{ backgroundColor: type.color }}
              />
              {type.name}
            </span>
          ))}
          {(contentFilter === 'all' || contentFilter === 'holidays') ? (
            <span className="inline-flex items-center gap-2 text-sm">
              <span className="inline-block w-3 h-3 rounded-full bg-neutral-300 shrink-0" />
              Public holiday
            </span>
          ) : null}
          {(contentFilter === 'all' || contentFilter === 'wfh') && canReadSchedule ? (
            <span className="inline-flex items-center gap-2 text-sm">
              Schedule locations
            </span>
          ) : null}
          {contentFilter === 'all' || contentFilter === 'leave' ? (
            <span className="inline-flex items-center gap-2 text-sm text-neutral-foreground-3">
              P = Pending · A = Approved
            </span>
          ) : null}
        </div>
      ) : null}

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {!isLoading && searchQuery.trim() && (calendar?.entries?.length ?? 0) > 0 && filteredEntries.length === 0
        && (contentFilter === 'all' || contentFilter === 'leave') ? (
        <Text className="text-sm text-neutral-foreground-3">
          No calendar entries match your search.
        </Text>
      ) : null}

      {isLoading ? (
        <Spinner label="Loading calendar..." />
      ) : (
        <div
          className="overflow-x-auto rounded border"
          style={{
            backgroundColor: tokens.colorNeutralBackground1,
            borderColor: tokens.colorNeutralStroke3,
          }}
        >
          <table className="w-full min-w-[840px] border-collapse table-fixed">
            <thead>
              <tr>
                {WEEKDAY_LABELS.map((label) => (
                  <th
                    key={label}
                    className="border px-2 py-2 text-left text-sm font-semibold"
                    style={{
                      backgroundColor: tokens.colorNeutralBackground2,
                      borderColor: tokens.colorNeutralStroke3,
                    }}
                  >
                    {label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {weeks.map((week, weekIndex) => (
                <tr key={`week-${weekIndex}`}>
                  {week.days.map((day, dayIndex) => {
                    if (!day) {
                      return (
                        <td
                          key={`empty-${weekIndex}-${dayIndex}`}
                          className="border min-w-[120px] h-[120px]"
                          style={{
                            backgroundColor: tokens.colorNeutralBackground3,
                            borderColor: tokens.colorNeutralStroke3,
                          }}
                        />
                      );
                    }

                    const dayKey = toDateString(day);
                    return (
                      <LeaveDayCell
                        key={dayKey}
                        day={day}
                        entries={entriesByDate.get(dayKey) ?? []}
                        holidays={holidaysByDate.get(dayKey) ?? []}
                        scheduleDays={scheduleByDate.get(dayKey) ?? []}
                        canCreate={canCreateLeave}
                        onCreateLeave={handleCreateLeaveForDay}
                      />
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {canCreateLeave ? (
        <LeaveRequestForm
          open={formOpen}
          initialStartDate={formStartDate}
          initialEndDate={formStartDate}
          onClose={() => {
            setFormOpen(false);
            setFormStartDate(null);
          }}
          onSubmitted={() => {
            void loadCalendar();
          }}
        />
      ) : null}
    </div>
  );
}
