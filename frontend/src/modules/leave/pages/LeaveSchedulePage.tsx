import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Badge,
  Button,
  Dropdown,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Tab,
  TabList,
  Text,
  tokens,
  type SelectTabData,
  type SelectTabEvent,
} from '@fluentui/react-components';
import AppTitle from '@platform/ui/AppTitle';
import { ApiError } from '@platform/api/apiClient';
import { usePermissions } from '@platform/permissions/usePermissions';
import { getUsers } from '@modules/users/services/userService';
import type { UserListItem } from '@modules/users/types/user';
import { formatDateOnlyForApi, parseDateOnly } from '@platform/utils/dateOnly';
import {
  getResolvedSchedule,
  getScheduleTemplates,
  getWorkLocationTypes,
  upsertScheduleOverride,
  upsertScheduleTemplate,
} from '@modules/leave/services/leaveService';
import type { ResolvedScheduleDay, WorkLocationType } from '@modules/leave/types/leave';
import { DatePicker } from '@fluentui/react-datepicker-compat';
import { ArrowPreviousRegular, ArrowNextRegular } from '@fluentui/react-icons';

const WEEKDAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const EDIT_DAYS = [0, 1, 2, 3, 4, 5, 6];

type RangeMode = 'week' | 'month';

function toDateInputValue(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function startOfWeek(date: Date): Date {
  const copy = new Date(date);
  const day = copy.getDay();
  const diff = -day;
  copy.setDate(copy.getDate() + diff);
  copy.setHours(0, 0, 0, 0);
  return copy;
}

function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function endOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0);
}

function addDays(date: Date, days: number): Date {
  const copy = new Date(date);
  copy.setDate(copy.getDate() + days);
  return copy;
}

function formatMonthLabel(date: Date): string {
  return date.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
}

export default function LeaveSchedulePage() {
  const { user, hasPermission, isAdmin, isHr, isManager } = usePermissions();
  const canWrite = hasPermission('leave.schedule.write');
  const canManageOthers = (isAdmin || isHr || isManager) && canWrite;

  const [rangeMode, setRangeMode] = useState<RangeMode>('week');
  const [anchorDate, setAnchorDate] = useState(() => startOfWeek(new Date()));
  const [locations, setLocations] = useState<WorkLocationType[]>([]);
  const [employees, setEmployees] = useState<UserListItem[]>([]);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [resolved, setResolved] = useState<ResolvedScheduleDay[]>([]);
  const [daySelections, setDaySelections] = useState<Record<number, string>>({});
  const [effectiveFrom, setEffectiveFrom] = useState<Date | undefined>(new Date());
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const targetUserId = canManageOthers ? selectedUserId : (user?.id ?? null);

  const { from, to, rangeLabel } = useMemo(() => {
    if (rangeMode === 'month') {
      const start = startOfMonth(anchorDate);
      const end = endOfMonth(anchorDate);
      return {
        from: toDateInputValue(start),
        to: toDateInputValue(end),
        rangeLabel: formatMonthLabel(start),
      };
    }

    const start = startOfWeek(anchorDate);
    const end = addDays(start, 6);
    return {
      from: toDateInputValue(start),
      to: toDateInputValue(end),
      rangeLabel: `${toDateInputValue(start)} → ${toDateInputValue(end)}`,
    };
  }, [anchorDate, rangeMode]);

  const load = useCallback(async () => {
    if (!canManageOthers && !targetUserId) {
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const templateUserId = targetUserId ?? user?.id ?? null;
      const [locationItems, templates, resolvedDays] = await Promise.all([
        getWorkLocationTypes(true),
        templateUserId && canWrite ? getScheduleTemplates(templateUserId) : Promise.resolve([]),
        getResolvedSchedule(from, to, targetUserId),
      ]);
      setLocations(locationItems.filter((item) => item.code.toUpperCase() !== 'ABSENT'));
      setResolved(resolvedDays);

      const active = templates[0];
      if (active) {
        setEffectiveFrom(parseDateOnly(active.effectiveFrom));
        const next: Record<number, string> = {};
        for (const day of active.days) {
          next[day.dayOfWeek] = day.locationTypeId;
        }
        setDaySelections(next);
      } else if (locationItems[0]) {
        const office = locationItems.find((item) => item.code === 'OFFICE') ?? locationItems[0];
        const wfh = locationItems.find((item) => item.code === 'WFH') ?? office;
        setDaySelections({
          1: office.id,
          2: office.id,
          3: office.id,
          4: wfh.id,
          5: wfh.id,
        });
      }
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load schedule.');
      setResolved([]);
    } finally {
      setIsLoading(false);
    }
  }, [canManageOthers, canWrite, from, targetUserId, to, user?.id]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!canManageOthers) {
      return;
    }

    void getUsers()
      .then((response) => setEmployees(response.items ?? []))
      .catch(() => setEmployees([]));
  }, [canManageOthers]);

  const teamRows = useMemo(() => {
    const byUser = new Map<string, ResolvedScheduleDay[]>();
    for (const day of resolved) {
      const rows = byUser.get(day.userId) ?? [];
      rows.push(day);
      byUser.set(day.userId, rows);
    }
    return [...byUser.entries()].map(([userId, days]) => ({
      userId,
      displayName: days[0]?.userDisplayName ?? userId,
      days: days.sort((a, b) => a.date.localeCompare(b.date)),
    }));
  }, [resolved]);

  const shiftRange = (direction: -1 | 1) => {
    setAnchorDate((current) => {
      if (rangeMode === 'month') {
        return new Date(current.getFullYear(), current.getMonth() + direction, 1);
      }
      return addDays(startOfWeek(current), direction * 7);
    });
  };

  const saveTemplate = async () => {
    if (!canWrite) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);
    try {
      const days = EDIT_DAYS
        .filter((dayOfWeek) => daySelections[dayOfWeek])
        .map((dayOfWeek) => ({
          dayOfWeek,
          locationTypeId: daySelections[dayOfWeek],
        }));

      await upsertScheduleTemplate({
        userId: canManageOthers ? (targetUserId ?? user?.id ?? null) : null,
        effectiveFrom: effectiveFrom ? formatDateOnlyForApi(effectiveFrom) : '',
        days,
      });
      setMessage('Weekly schedule saved.');
      await load();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save schedule.');
    } finally {
      setIsSaving(false);
    }
  };

  const saveOverrideForDate = async (date: string, locationTypeId: string) => {
    if (!canWrite || !locationTypeId) {
      return;
    }

    setIsSaving(true);
    setError(null);
    try {
      await upsertScheduleOverride({
        userId: canManageOthers ? (targetUserId ?? user?.id ?? null) : null,
        date,
        locationTypeId,
      });
      setMessage(`Override saved for ${date}.`);
      await load();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save override.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="flex flex-col gap-4 h-full min-h-0 px-3">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <AppTitle
          title="Work schedule"
          subtitle={canWrite
            ? 'Create hybrid weekly templates and day overrides for your team. Approved leave overlays planned work.'
            : 'View your weekly and monthly planned work locations.'}
        />
        <div className="flex items-end gap-2 flex-wrap">
          {canManageOthers ? (
            <Field label="Employee" style={{ minWidth: 200 }}>
              <Dropdown
                placeholder="Team"
                style={{ minWidth: 100 }}
                value={employees.find((item) => item.id === selectedUserId)?.displayName
                  ?? employees.find((item) => item.id === selectedUserId)?.username
                  ?? (selectedUserId ? 'Selected' : 'Team (visible)')}
                selectedOptions={selectedUserId ? [selectedUserId] : ['__team__']}
                onOptionSelect={(_, data) => {
                  const value = data.optionValue;
                  setSelectedUserId(!value || value === '__team__' ? null : value);
                }}
              >
                <Option value="__team__" text="Team (visible)">Team (visible)</Option>
                {user?.id ? (
                  <Option value={user.id} text="Myself">Myself</Option>
                ) : null}
                {employees.map((employee) => (
                  <Option
                    key={employee.id}
                    value={employee.id}
                    text={employee.displayName ?? employee.username}
                  >
                    {employee.displayName ?? employee.username}
                  </Option>
                ))}
              </Dropdown>
            </Field>
          ) : null}
          <TabList
            selectedValue={rangeMode}
            onTabSelect={(_: SelectTabEvent, data: SelectTabData) => {
              const mode = data.value as RangeMode;
              setRangeMode(mode);
              setAnchorDate(mode === 'month' ? startOfMonth(new Date()) : startOfWeek(new Date()));
            }}
          >
            <Tab value="week">Week</Tab>
            <Tab value="month">Month</Tab>
          </TabList>
          <Button appearance="secondary" onClick={() => shiftRange(-1)} icon={<ArrowPreviousRegular />}>
          </Button>
          <Button
            appearance="secondary"
            onClick={() => setAnchorDate(rangeMode === 'month' ? startOfMonth(new Date()) : startOfWeek(new Date()))}
          >
            Today
          </Button>
          <Button appearance="secondary" onClick={() => shiftRange(1)} iconPosition="after" icon={<ArrowNextRegular />}>
          </Button>
        </div>
      </div>

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}
      {message ? (
        <MessageBar intent="success">
          <MessageBarBody>{message}</MessageBarBody>
        </MessageBar>
      ) : null}

      {isLoading ? (
        <Spinner label="Loading schedule..." />
      ) : (
        <>
          {canWrite ? (
            <section
              className="rounded border p-4 flex flex-col gap-3"
              style={{ borderColor: tokens.colorNeutralStroke3 }}
            >
              <Text weight="semibold">Weekly template</Text>
              <div className="flex flex-wrap gap-3 items-end">
                <Field label="Effective from">
                  <DatePicker
                    style={{ minWidth: 200 }}
                    value={effectiveFrom}
                    onSelectDate={(date) => setEffectiveFrom(date ?? undefined)}
                    disabled={isSaving}
                  />
                </Field>
                {EDIT_DAYS.map((dayOfWeek) => (
                  <Field key={dayOfWeek} label={WEEKDAY_LABELS[dayOfWeek]}>
                    <Dropdown
                      value={locations.find((item) => item.id === daySelections[dayOfWeek])?.name ?? '—'}
                      selectedOptions={daySelections[dayOfWeek] ? [daySelections[dayOfWeek]] : []}
                      onOptionSelect={(_, data) => {
                        const value = data.optionValue;
                        if (!value) {
                          return;
                        }
                        setDaySelections((current) => ({ ...current, [dayOfWeek]: value }));
                      }}
                      style={{ minWidth: 200 }}
                      disabled={isSaving}
                    >
                      {locations.map((location) => (
                        <Option key={location.id} value={location.id} text={location.name}>
                          {location.name}
                        </Option>
                      ))}
                    </Dropdown>
                  </Field>
                ))}
                <Button appearance="primary" disabled={isSaving} onClick={() => void saveTemplate()}>
                  {isSaving ? 'Saving...' : 'Save template'}
                </Button>
              </div>
            </section>
          ) : null}

          <section
            className="rounded border p-4 flex flex-col gap-3 min-h-0 overflow-auto pb-20"
            style={{ borderColor: tokens.colorNeutralStroke3 }}
          >
            <Text weight="semibold">
              {rangeMode === 'week' ? 'Weekly schedule' : 'Monthly schedule'} ({rangeLabel})
            </Text>
            {teamRows.length === 0 ? (
              <Text className="text-neutral-foreground-3">No schedule rows for this range.</Text>
            ) : (
              <div className="flex flex-col gap-4">
                {teamRows.map((row) => (
                  <div key={row.userId} className="flex flex-col gap-2">
                    {(canManageOthers || teamRows.length > 1) ? (
                      <Text weight="semibold">{row.displayName}</Text>
                    ) : null}
                    <div className="flex flex-col gap-2">
                      {rangeMode === 'week' ? (
                        <div className="hidden md:grid md:grid-cols-7 gap-2">
                          {WEEKDAY_LABELS.map((label) => (
                            <Text key={label} weight="semibold" size={200} className="px-1">
                              {label}
                            </Text>
                          ))}
                        </div>
                      ) : null}
                      <div className={`grid gap-2 ${rangeMode === 'week' ? 'grid-cols-1 md:grid-cols-7' : 'grid-cols-2 md:grid-cols-4 xl:grid-cols-7'}`}>
                        {row.days.map((day) => {
                          const weekdayName = new Date(`${day.date}T12:00:00`).toLocaleDateString(undefined, {
                            weekday: 'short',
                          });

                          return (
                        <div
                          key={`${row.userId}-${day.date}`}
                          className="rounded border p-2 flex flex-col gap-2 min-h-[120px]"
                          style={{
                            borderColor: tokens.colorNeutralStroke3,
                            borderBottom: `3px solid ${day.locationTypeColor ?? tokens.colorNeutralStroke3}`,
                          }}
                        >
                          <div className="flex items-baseline justify-between gap-1">
                            {rangeMode === 'week' ? (
                              <Text weight="semibold" size={200} className="md:hidden">{weekdayName}</Text>
                            ) : (
                              <Text weight="semibold" size={200}>{weekdayName}</Text>
                            )}
                            <Text size={200}>{day.date}</Text>
                          </div>
                          <div>
                          {day.kind === 'OnLeave' ? (
                            <Badge appearance="filled" color="informative">
                              On leave{day.leaveTypeName ? `: ${day.leaveTypeName}` : ''}
                            </Badge>
                          ) : day.kind === 'Unscheduled' ? (
                            <Badge appearance="outline">Unscheduled</Badge>
                          ) : (
                            <Badge appearance="tint" style={{ backgroundColor: `${day.locationTypeColor}22` }}>
                              {day.locationTypeName}
                              {day.fromOverride ? ' (override)' : ''}
                            </Badge>
                          )}
                          </div>
                          {canWrite && day.kind !== 'OnLeave' ? (
                            <Dropdown
                              size="small"
                              placeholder="Override"
                              onOptionSelect={(_, data) => {
                                if (data.optionValue) {
                                  void saveOverrideForDate(day.date, data.optionValue);
                                }
                              }}
                              style={{
                                minWidth: 100
                              }}
                              className="mt-auto"
                            >
                              {locations.map((location) => (
                                <Option key={location.id} value={location.id} text={location.name}>
                                  {location.name}
                                </Option>
                              ))}
                            </Dropdown>
                          ) : null}
                        </div>
                          );
                        })}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
