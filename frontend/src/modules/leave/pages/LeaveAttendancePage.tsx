import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  Badge,
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Field,
  Input,
  MessageBar,
  MessageBarActions,
  MessageBarBody,
  Option,
  Spinner,
  Tab,
  TabList,
  Text,
  tokens,
  type SelectTabEvent,
  type SelectTabData,
} from '@fluentui/react-components';
import { DonutChart } from '@fluentui/react-charts';
import { ArrowNextRegular, ArrowPreviousRegular, Checkmark12Regular, DismissRegular, Edit12Regular, PersonAvailableRegular, PersonProhibitedRegular } from '@fluentui/react-icons';
import AppTitle from '@platform/ui/AppTitle';
import { ApiError } from '@platform/api/apiClient';
import { usePermissions } from '@platform/permissions/usePermissions';
import { getUsers } from '@modules/users/services/userService';
import type { UserListItem } from '@modules/users/types/user';
import {
  getAttendanceCompare,
  getAttendancePolicy,
  getWorkLocationTypes,
  upsertAttendanceDay,
} from '@modules/leave/services/leaveService';
import type {
  AttendanceCompareRow,
  AttendanceDefaultAssumption,
  WorkLocationType,
} from '@modules/leave/types/leave';
import {
  ATTENDANCE_HEALTH_COLORS,
  computeAttendanceHealth,
  computeAttendanceHealthScore,
} from '@modules/leave/utils/attendanceHealth';

const WEEKDAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

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

function getSundayBasedDayIndex(date: Date): number {
  return date.getDay();
}

function buildMonthWeeks(month: Date): Array<Array<Date | null>> {
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

  const weeks: Array<Array<Date | null>> = [];
  for (let index = 0; index < cells.length; index += 7) {
    weeks.push(cells.slice(index, index + 7));
  }

  return weeks;
}

type FilterMode = 'all' | 'missing' | 'mismatch' | 'match';
type RangeMode = 'week' | 'month';

interface EmployeeAttendanceGroup {
  userId: string;
  displayName: string;
  days: AttendanceCompareRow[];
}

function resolveQuickMarkLocationId(
  row: AttendanceCompareRow,
  locations: WorkLocationType[],
  assumption: AttendanceDefaultAssumption,
): string | null {
  if (assumption === 'Absent') {
    return locations.find((item) => item.code.toUpperCase() === 'ABSENT')?.id ?? null;
  }

  if (row.plannedKind === 'Work' && row.plannedLocationTypeId) {
    return row.plannedLocationTypeId;
  }

  return locations.find((item) => item.code.toUpperCase() === 'OFFICE')?.id ?? null;
}

function AttendanceDayCell({
  row,
  dayNumber,
  canWrite,
  defaultAssumption,
  quickMarkBusyKey,
  onMark,
  onQuickMark,
}: {
  row: AttendanceCompareRow | undefined;
  dayNumber: number;
  canWrite: boolean;
  defaultAssumption: AttendanceDefaultAssumption;
  quickMarkBusyKey: string | null;
  onMark: (row: AttendanceCompareRow) => void;
  onQuickMark: (row: AttendanceCompareRow, toggle?: boolean) => void;
}) {
  if (!row) {
    return (
      <div
        className="rounded border min-h-[120px] p-2"
        style={{
          borderColor: tokens.colorNeutralStroke3,
          backgroundColor: tokens.colorNeutralBackground3,
        }}
      >
        <Text weight="semibold" size={200}>{dayNumber}</Text>
      </div>
    );
  }

  const day = new Date(`${row.date}T12:00:00`);
  const weekend = day.getDay() === 0 || day.getDay() === 6;
  const accent =
    row.actualLocationTypeColor
    ?? row.plannedLocationTypeColor
    ?? tokens.colorNeutralStroke3;
  const busyKey = `${row.userId}|${row.date}`;
  const isQuickBusy = quickMarkBusyKey === busyKey;

  return (
    <div
      className="group p-2 flex flex-col gap-1.5 min-h-[120px] rounded border"
      style={{
        borderColor: tokens.colorNeutralStroke3,
        borderBottom: `3px solid ${accent}`,
        backgroundColor: weekend ? tokens.colorNeutralBackground2 : tokens.colorNeutralBackground1,
      }}
    >
      <div className="flex items-center justify-between gap-1">
        <Text weight="semibold" size={200}>{dayNumber}</Text>
        <span>
          {row.isMatch ? (
            <Badge appearance="filled" color="success" size="small">Match</Badge>
          ) : row.isMismatch ? (
            <Badge appearance="filled" color="danger" size="small">Mismatch</Badge>
          ) : row.plannedKind === 'Work' && !row.hasActual ? (
            <Badge appearance="outline" color="warning" size="small">Missing</Badge>
          ) : null}
        </span>
      </div>
      <span>
        {row.plannedKind === 'OnLeave' ? (
          <Badge appearance="filled" color="informative" size="small">On leave</Badge>
        ) : row.plannedKind === 'Unscheduled' ? (
          <Badge appearance="outline" size="small">Unscheduled</Badge>
        ) : (
          <Text size={100} className="truncate" title={row.plannedLocationTypeName ?? undefined}>
            Planned: {row.plannedLocationTypeName}
          </Text>
        )}
      </span>

      {row.hasActual ? (
        <Text size={100} className="truncate" title={row.actualLocationTypeName ?? undefined}>
          Actual: {row.actualLocationTypeName}
        </Text>
      ) : row.plannedKind === 'Work' ? (
        <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>Not confirmed</Text>
      ) : null}

      {canWrite && row.plannedKind !== 'OnLeave' ? (<>
      
        <div className="hidden gap-1 mt-auto flex-wrap group-hover:flex group-focus-within:flex">
          {/* Action section */}
           <Button
            icon={defaultAssumption === 'Absent' ? <PersonProhibitedRegular /> : <PersonAvailableRegular />}
            size="small"
            appearance="primary"
            style={{ backgroundColor: defaultAssumption === 'Absent' ? tokens.colorStatusDangerBackground3 : tokens.colorStatusSuccessBackground3 }}
            disabled={isQuickBusy}
            title={defaultAssumption === 'Absent' ? 'Mark absent' : 'Mark present'}
            onClick={() => onQuickMark(row)}
          >
          </Button>
          <Button
            icon={defaultAssumption === 'Absent' ? <PersonAvailableRegular /> : <PersonProhibitedRegular />}
            size="small"
            appearance={"primary"}
            style={{ backgroundColor: defaultAssumption === 'Absent' ? tokens.colorStatusSuccessBackground3 : tokens.colorStatusDangerBackground3 }}
            disabled={isQuickBusy}
            title={defaultAssumption === 'Absent' ? 'Mark present' : 'Mark absent'}
            onClick={() => onQuickMark(row, true)}
          >
          </Button>
          <Button
            icon={row.hasActual ? <Edit12Regular /> : <Checkmark12Regular />}
            size="small"
            appearance="secondary"
            disabled={isQuickBusy}
            title="Mark with details"
            onClick={() => onMark(row)}
          />
        </div>

        <div className="flex justify-end gap-1 mt-auto flex-wrap group-focus:flex lg:hidden">
          <Button
            icon={row.hasActual ? <Edit12Regular /> : <Checkmark12Regular />}
            size="small"
            appearance="secondary"
            disabled={isQuickBusy}
            title="Mark with details"
            onClick={() => onMark(row)}
          />
        </div>

        </>
      ) : null}
    </div>
  );
}

function EmployeeAttendanceCalendar({
  group,
  rangeMode,
  anchorDate,
  canWrite,
  defaultAssumption,
  quickMarkBusyKey,
  onMark,
  onQuickMark,
}: {
  group: EmployeeAttendanceGroup;
  rangeMode: RangeMode;
  anchorDate: Date;
  canWrite: boolean;
  defaultAssumption: AttendanceDefaultAssumption;
  quickMarkBusyKey: string | null;
  onMark: (row: AttendanceCompareRow) => void;
  onQuickMark: (row: AttendanceCompareRow, toggle?: boolean) => void;
}) {
  const byDate = useMemo(() => {
    const map = new Map<string, AttendanceCompareRow>();
    for (const day of group.days) {
      map.set(day.date, day);
    }
    return map;
  }, [group.days]);

  if (rangeMode === 'week') {
    const weekStart = startOfWeek(anchorDate);
    const days = Array.from({ length: 7 }, (_, index) => addDays(weekStart, index));

    return (
      <div className="flex flex-col gap-2">
        <div className="grid grid-cols-7 gap-2">
          {WEEKDAY_LABELS.map((label) => (
            <Text key={label} weight="semibold" size={200} className="px-1">
              {label}
            </Text>
          ))}
        </div>
        <div className="grid grid-cols-7 gap-2">
          {days.map((day) => {
            const key = toDateInputValue(day);
            return (
              <AttendanceDayCell
                key={key}
                row={byDate.get(key)}
                dayNumber={day.getDate()}
                canWrite={canWrite}
                defaultAssumption={defaultAssumption}
                quickMarkBusyKey={quickMarkBusyKey}
                onMark={onMark}
                onQuickMark={onQuickMark}
              />
            );
          })}
        </div>
      </div>
    );
  }

  const weeks = buildMonthWeeks(startOfMonth(anchorDate));

  return (
    <div
      className="overflow-x-auto rounded border"
      style={{ borderColor: tokens.colorNeutralStroke3 }}
    >
      <table className="w-full min-w-[720px] border-collapse table-fixed">
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
              {week.map((day, dayIndex) => {
                if (!day) {
                  return (
                    <td
                      key={`empty-${weekIndex}-${dayIndex}`}
                      className="border align-top"
                      style={{
                        backgroundColor: tokens.colorNeutralBackground3,
                        borderColor: tokens.colorNeutralStroke3,
                      }}
                    />
                  );
                }

                const key = toDateInputValue(day);
                return (
                  <td
                    key={key}
                    className="border align-top"
                    style={{ borderColor: tokens.colorNeutralStroke3 }}
                  >
                    <AttendanceDayCell
                      row={byDate.get(key)}
                      dayNumber={day.getDate()}
                      canWrite={canWrite}
                      defaultAssumption={defaultAssumption}
                      quickMarkBusyKey={quickMarkBusyKey}
                      onMark={onMark}
                      onQuickMark={onQuickMark}
                    />
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default function LeaveAttendancePage() {
  const { user, hasPermission, isAdmin, isHr, isManager } = usePermissions();
  const canWrite = hasPermission('leave.attendance.write');
  const canManageOthers = (isAdmin || isHr || isManager) && canWrite;

  const [rangeMode, setRangeMode] = useState<RangeMode>('month');
  const [anchorDate, setAnchorDate] = useState(() => startOfMonth(new Date()));
  const [locations, setLocations] = useState<WorkLocationType[]>([]);
  const [employees, setEmployees] = useState<UserListItem[]>([]);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [rows, setRows] = useState<AttendanceCompareRow[]>([]);
  const [healthRows, setHealthRows] = useState<AttendanceCompareRow[]>([]);
  const [filterMode, setFilterMode] = useState<FilterMode>('all');
  const [openItems, setOpenItems] = useState<string[]>([]);
  const [defaultAssumption, setDefaultAssumption] = useState<AttendanceDefaultAssumption>('Present');
  const [editingDate, setEditingDate] = useState<string | null>(null);
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [actualLocationId, setActualLocationId] = useState<string>('');
  const [notes, setNotes] = useState('');
  const [collaboratorUserIds, setCollaboratorUserIds] = useState<string[]>([]);
  const [externalName, setExternalName] = useState('');
  const [quickMarkBusyKey, setQuickMarkBusyKey] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const targetUserId = canManageOthers
    ? selectedUserId
    : (user?.id ?? null);

  const { from, to, rangeLabel } = useMemo(() => {
    if (rangeMode === 'week') {
      const start = startOfWeek(anchorDate);
      const end = addDays(start, 6);
      return {
        from: toDateInputValue(start),
        to: toDateInputValue(end),
        rangeLabel: `${toDateInputValue(start)} → ${toDateInputValue(end)}`,
      };
    }

    const start = startOfMonth(anchorDate);
    const end = endOfMonth(anchorDate);
    return {
      from: toDateInputValue(start),
      to: toDateInputValue(end),
      rangeLabel: formatMonthLabel(start),
    };
  }, [anchorDate, rangeMode]);

  const { from: healthFrom, to: healthTo } = useMemo(() => {
    const end = new Date();
    const start = addDays(end, -29);
    return {
      from: toDateInputValue(start),
      to: toDateInputValue(end),
    };
  }, []);

  const selectedLocation = locations.find((item) => item.id === actualLocationId);

  const load = useCallback(async (options?: { silent?: boolean }) => {
    const silent = options?.silent === true;
    if (!silent) {
      setIsLoading(true);
    }
    try {
      const [locationItems, compareRows, monthlyRows, policy] = await Promise.all([
        getWorkLocationTypes(true),
        getAttendanceCompare(from, to, targetUserId),
        getAttendanceCompare(healthFrom, healthTo, targetUserId),
        getAttendancePolicy(),
      ]);
      setLocations(locationItems);
      setRows(compareRows);
      setHealthRows(monthlyRows);
      setDefaultAssumption(policy.defaultAssumption === 'Absent' ? 'Absent' : 'Present');
      if (!silent) {
        setError(null);
      }
    } catch (loadError) {
      if (!silent) {
        setError(loadError instanceof ApiError ? loadError.message : 'Failed to load attendance.');
        setRows([]);
      }
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, [from, healthFrom, healthTo, targetUserId, to]);

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

  const patchCompareRow = useCallback((
    userId: string,
    date: string,
    location: WorkLocationType,
    collaborators: AttendanceCompareRow['collaborators'] = [],
  ) => {
    setRows((current) => current.map((row) => {
      if (row.userId !== userId || row.date !== date) {
        return row;
      }

      const isMatch = row.plannedKind === 'Work'
        && Boolean(row.plannedLocationTypeId)
        && row.plannedLocationTypeId === location.id;
      const isMismatch = row.plannedKind === 'Work'
        && Boolean(row.plannedLocationTypeId)
        && row.plannedLocationTypeId !== location.id;

      return {
        ...row,
        hasActual: true,
        actualLocationTypeId: location.id,
        actualLocationTypeName: location.name,
        actualLocationTypeColor: location.color,
        isMatch,
        isMismatch,
        collaborators,
      };
    }));
  }, []);

  const health = useMemo(() => computeAttendanceHealth(rows), [rows]);
  const absentLocationTypeId = useMemo(() => (
    locations.find((item) => item.code.toUpperCase() === 'ABSENT')?.id ?? null
  ), [locations]);
  const attendanceHealthScore = useMemo(
    () => computeAttendanceHealthScore(healthRows, absentLocationTypeId),
    [absentLocationTypeId, healthRows],
  );

  const filteredRows = useMemo(() => {
    switch (filterMode) {
      case 'missing':
        return rows.filter((row) => !row.hasActual && row.plannedKind === 'Work');
      case 'mismatch':
        return rows.filter((row) => row.isMismatch);
      case 'match':
        return rows.filter((row) => row.isMatch);
      default:
        return rows;
    }
  }, [filterMode, rows]);

  const employeeGroups = useMemo(() => {
    const matchingUserIds = new Set(filteredRows.map((row) => row.userId));
    const map = new Map<string, EmployeeAttendanceGroup>();

    for (const row of rows) {
      if (filterMode !== 'all' && !matchingUserIds.has(row.userId)) {
        continue;
      }

      const existing = map.get(row.userId);
      if (existing) {
        existing.days.push(row);
      } else {
        map.set(row.userId, {
          userId: row.userId,
          displayName: row.userDisplayName,
          days: [row],
        });
      }
    }

    return [...map.values()]
      .map((group) => ({
        ...group,
        days: [...group.days].sort((a, b) => a.date.localeCompare(b.date)),
      }))
      .sort((a, b) => a.displayName.localeCompare(b.displayName));
  }, [filterMode, filteredRows, rows]);

  useEffect(() => {
    setOpenItems((current) => {
      if (employeeGroups.length === 0) {
        return [];
      }

      const valid = new Set(employeeGroups.map((group) => group.userId));
      const retained = current.filter((id) => valid.has(id));
      if (retained.length > 0) {
        return retained;
      }

      return [employeeGroups[0].userId];
    });
  }, [employeeGroups]);

  const shiftRange = (direction: -1 | 1) => {
    setAnchorDate((current) => {
      if (rangeMode === 'month') {
        return new Date(current.getFullYear(), current.getMonth() + direction, 1);
      }
      return addDays(startOfWeek(current), direction * 7);
    });
  };

  const beginEdit = (row: AttendanceCompareRow) => {
    setEditingDate(row.date);
    setEditingUserId(row.userId);
    setActualLocationId(row.actualLocationTypeId ?? row.plannedLocationTypeId ?? locations.find((item) => item.code.toUpperCase() === 'OFFICE')?.id ?? locations[0]?.id ?? '');
    setNotes('');
    setCollaboratorUserIds(
      row.collaborators
        .map((item) => item.collaboratorUserId)
        .filter((id): id is string => Boolean(id)),
    );
    setExternalName(row.collaborators.find((item) => item.externalName)?.externalName ?? '');
    setMessage(null);
  };

  const closeEdit = () => {
    setEditingDate(null);
    setEditingUserId(null);
  };

  const quickMark = async (row: AttendanceCompareRow, toggle?: boolean) => {
    if (!canWrite || row.plannedKind === 'OnLeave') {
      return;
    }

    const assumption = toggle ? (defaultAssumption === 'Present' ? 'Absent' : 'Present') : defaultAssumption;
    const locationId = resolveQuickMarkLocationId(row, locations, assumption);

    const location = locations.find((item) => item.id === locationId) ?? null;
    if (!locationId || !location) {
      setError(
        assumption === 'Absent'
          ? 'Absent location type is not configured.'
          : 'Office location type is not configured for unscheduled days.',
      );
      return;
    }

    const busyKey = `${row.userId}|${row.date}`;
    setQuickMarkBusyKey(busyKey);
    setError(null);
    try {
      await upsertAttendanceDay(row.date, {
        userId: canManageOthers ? row.userId : null,
        actualLocationTypeId: locationId,
        notes: null,
        collaborators: [],
      });
      patchCompareRow(row.userId, row.date, location, []);
      setMessage(`Marked ${assumption.toLowerCase()} for ${row.userDisplayName} on ${row.date}.`);
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : `Failed to mark ${assumption.toLowerCase()}.`);
    } finally {
      setQuickMarkBusyKey(null);
    }
  };

  const saveAttendance = async () => {
    if (!editingDate || !actualLocationId || !canWrite) {
      return;
    }

    const location = locations.find((item) => item.id === actualLocationId);
    if (!location) {
      setError('Selected location is invalid.');
      return;
    }

    setIsSaving(true);
    setError(null);
    try {
      const collaborators = [
        ...collaboratorUserIds.map((id) => ({ collaboratorUserId: id })),
        ...(externalName.trim() ? [{ externalName: externalName.trim() }] : []),
      ];

      const saved = await upsertAttendanceDay(editingDate, {
        userId: canManageOthers ? (editingUserId ?? selectedUserId ?? targetUserId) : null,
        actualLocationTypeId: actualLocationId,
        notes: notes.trim() || null,
        collaborators: selectedLocation?.tracksCollaborators ? collaborators : [],
      });

      const targetId = editingUserId ?? selectedUserId ?? targetUserId ?? saved.userId;
      patchCompareRow(
        targetId,
        editingDate,
        location,
        saved.collaborators.map((item) => ({
          collaboratorUserId: item.collaboratorUserId,
          collaboratorDisplayName: item.collaboratorDisplayName,
          externalName: item.externalName,
        })),
      );
      setMessage(`Attendance saved for ${editingDate}.`);
      closeEdit();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save attendance.');
    } finally {
      setIsSaving(false);
    }
  };

  const exportCsv = () => {
    const header = [
      'Employee',
      'Date',
      'Planned kind',
      'Planned',
      'Actual',
      'Match',
      'Mismatch',
      'Has actual',
      'Collaborators',
    ];
    const lines = filteredRows.map((row) => {
      const collaborators = row.collaborators
        .map((item) => item.collaboratorDisplayName ?? item.externalName ?? '')
        .filter(Boolean)
        .join('; ');
      return [
        row.userDisplayName,
        row.date,
        row.plannedKind,
        row.plannedLocationTypeName ?? '',
        row.actualLocationTypeName ?? '',
        row.isMatch ? 'yes' : 'no',
        row.isMismatch ? 'yes' : 'no',
        row.hasActual ? 'yes' : 'no',
        collaborators,
      ]
        .map((value) => `"${String(value).split('"').join('""')}"`)
        .join(',');
    });

    const blob = new Blob([[header.join(','), ...lines].join('\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `attendance-compare-${from}_${to}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  };

  const editingEmployeeName = employeeGroups.find((group) => group.userId === editingUserId)?.displayName
    ?? employees.find((item) => item.id === editingUserId)?.displayName
    ?? employees.find((item) => item.id === editingUserId)?.username
    ?? null;

  return (
    <div className="flex flex-col gap-4 h-full min-h-0 px-3 h-[80vh]">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <AppTitle
          title="Attendance"
          subtitle={canWrite
            ? 'Mark actual attendance for your team and review planned vs actual health.'
            : 'Review your attendance statistics and planned vs actual status.'}
        />
        <div className="flex items-end gap-2 flex-wrap">
          {canManageOthers ? (
            <Field label="Employee">
              <Dropdown
                placeholder="Team"
                value={employees.find((item) => item.id === selectedUserId)?.displayName
                  ?? employees.find((item) => item.id === selectedUserId)?.username
                  ?? (selectedUserId ? 'Selected' : 'Everyone visible')}
                selectedOptions={selectedUserId ? [selectedUserId] : ['__all__']}
                onOptionSelect={(_, data) => {
                  const value = data.optionValue;
                  setSelectedUserId(!value || value === '__all__' ? null : value);
                }}
              >
                <Option value="__all__" text="Everyone visible">Everyone visible</Option>
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
          <Button icon={<ArrowPreviousRegular />} appearance="secondary" onClick={() => shiftRange(-1)}></Button>
          <Button
            appearance="secondary"
            onClick={() => setAnchorDate(rangeMode === 'month' ? startOfMonth(new Date()) : startOfWeek(new Date()))}
          >
            Today
          </Button>
          <Button 
          icon={<ArrowNextRegular />}
          appearance="secondary" onClick={() => shiftRange(1)}></Button>
          <Button appearance="secondary" onClick={exportCsv} disabled={filteredRows.length === 0}>
            Export CSV
          </Button>
        </div>
      </div>

      <section className="h-[80vh] overflow-y-scroll relative pb-10">

      <section
        className="rounded border p-4 flex flex-col md:flex-row gap-4 items-center md:items-stretch"
        style={{ borderColor: tokens.colorNeutralStroke3 }}
      >
        <div className="flex flex-col gap-2 flex-1 min-w-0">
          {/* Health Title */}
          <Text weight="semibold">Attendance health · {rangeLabel}</Text>
          <div className="grid grid-cols-2 gap-2 text-sm">
            <Text><span className="font-semibold w-[80px] inline-block">Match</span>: {health.match}</Text>
            <Text><span className="font-semibold w-[80px] inline-block">Missing</span>: {health.missing}</Text>
            <Text><span className="font-semibold w-[80px] inline-block">Mismatch</span>: {health.mismatch}</Text>
            <Text><span className="font-semibold w-[80px] inline-block">Match rate</span>: {health.workDays === 0 ? '—' : `${Math.round(health.matchRate * 100)}%`}</Text>
          </div>
          <div className="flex flex-wrap gap-2 pt-1">
            <Badge appearance="filled" style={{ backgroundColor: ATTENDANCE_HEALTH_COLORS.healthy, color: '#fff' }}>Match</Badge>
            <Badge appearance="filled" style={{ backgroundColor: ATTENDANCE_HEALTH_COLORS.watch, color: '#323130' }}>Missing</Badge>
            <Badge appearance="filled" style={{ backgroundColor: ATTENDANCE_HEALTH_COLORS.risk, color: '#fff' }}>Mismatch</Badge>
            <Badge appearance="filled" style={{ backgroundColor: ATTENDANCE_HEALTH_COLORS.unknown, color: '#fff' }}>Other</Badge>
          </div>
        </div>
        <div className="flex flex-col items-center gap-2 shrink-0 z-50">
          {attendanceHealthScore.countedDays > 0 ? (
            <div className="flex min-w-[150px] min-h-[150px] items-center justify-center">
              <DonutChart
                culture={typeof window !== 'undefined' ? window.navigator.language : 'en-us'}
                data={{
                  chartTitle: 'Attendance health',
                  chartData: [
                    {
                      legend: 'Attendance',
                      data: Math.round(attendanceHealthScore.percentage * 100),
                      color: attendanceHealthScore.color,
                    },
                    {
                      legend: 'Absence',
                      data: 100 - Math.round(attendanceHealthScore.percentage * 100),
                      color: ATTENDANCE_HEALTH_COLORS.unknown,
                    },
                  ],
                }}
                height={160}
                width={160}
                innerRadius={45}
                valueInsideDonut={`${Math.round(attendanceHealthScore.percentage * 100)}%`}
                hideLegend
              />
            </div>
          ) : (
            <Text className="text-neutral-foreground-3 text-center">No attendance data in the last 30 days.</Text>
          )}
          <div className="flex flex-col items-center gap-1">
              <Text
                size={300}
                style={{ color: tokens.colorNeutralForeground3, fontWeight: 300 }}>
                  Attendance health score
                </Text>
              <Text
                size={500}
                style={{ color: tokens.colorNeutralForeground3, fontWeight: 300 }}
              >
                {attendanceHealthScore.label} 
              </Text>
          </div>
        </div>
      </section>

      <div className="sticky top-0 z-10 pt-2 pb-1" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
        <TabList
          selectedValue={filterMode}
          onTabSelect={(_: SelectTabEvent, data: SelectTabData) => setFilterMode(data.value as FilterMode)}
        >
          <Tab value="all">All</Tab>
          <Tab value="missing">Missing actual</Tab>
          <Tab value="mismatch">Mismatch</Tab>
          <Tab value="match">Match</Tab>
        </TabList>
      </div>

      {error ? (
        <MessageBar intent="error" className="mb-3">
          <MessageBarBody>{error}</MessageBarBody>
          <MessageBarActions
            containerAction={
              <Button
                appearance="transparent"
                icon={<DismissRegular />}
                aria-label="Dismiss error"
                onClick={() => setError(null)}
              />
            }
          />
        </MessageBar>
      ) : null}
      {message ? (
        <MessageBar intent="success" className="mb-3">
          <MessageBarBody>{message}</MessageBarBody>
          <MessageBarActions
            containerAction={
              <Button
                appearance="transparent"
                icon={<DismissRegular />}
                aria-label="Dismiss message"
                onClick={() => setMessage(null)}
              />
            }
          />
        </MessageBar>
      ) : null}

      {isLoading ? (
        <Spinner label="Loading attendance..." />
      ) : employeeGroups.length === 0 ? (
        <Text className="text-neutral-foreground-3">No attendance rows for this filter.</Text>
      ) : (
        <div className="overflow-auto min-h-0">
          <Accordion
            multiple
            collapsible
            openItems={openItems}
            onToggle={(_, data) => setOpenItems(data.openItems.map(String))}
          >
            {employeeGroups.map((group) => {
              const groupHealth = computeAttendanceHealth(group.days);
              return (
                <AccordionItem key={group.userId} value={group.userId}>
                  <AccordionHeader>
                    <div className="flex flex-wrap items-center gap-2 pr-2">
                      <Text weight="semibold">{group.displayName}</Text>
                      <Badge appearance="outline" size="small">
                        {groupHealth.workDays === 0
                          ? '—'
                          : `${Math.round(groupHealth.matchRate * 100)}%`}
                      </Badge>
                      <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                        {groupHealth.label}
                      </Text>
                    </div>
                  </AccordionHeader>
                  <AccordionPanel>
                    <div className="pb-3">
                      <EmployeeAttendanceCalendar
                        group={group}
                        rangeMode={rangeMode}
                        anchorDate={anchorDate}
                        canWrite={canWrite}
                        defaultAssumption={defaultAssumption}
                        quickMarkBusyKey={quickMarkBusyKey}
                        onMark={beginEdit}
                        onQuickMark={(row, toggle) => void quickMark(row, toggle)}
                      />
                    </div>
                  </AccordionPanel>
                </AccordionItem>
              );
            })}
          </Accordion>
        </div>
      )}

      <Dialog
        open={Boolean(editingDate) && canWrite}
        onOpenChange={(_, data) => {
          if (!data.open && !isSaving) {
            closeEdit();
          }
        }}
      >
        <DialogSurface>
          <DialogBody>
            <DialogTitle>
              Mark attendance for {editingDate}
              {editingEmployeeName ? ` · ${editingEmployeeName}` : ''}
            </DialogTitle>
            <DialogContent className="flex flex-col gap-3 pt-2">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <Field label="Actual location">
                  <Dropdown
                    value={locations.find((item) => item.id === actualLocationId)?.name ?? ''}
                    selectedOptions={actualLocationId ? [actualLocationId] : []}
                    onOptionSelect={(_, data) => setActualLocationId(data.optionValue ?? '')}
                  >
                    {locations.map((location) => (
                      <Option key={location.id} value={location.id} text={location.name}>
                        {location.name}
                      </Option>
                    ))}
                  </Dropdown>
                </Field>
                <Field label="Notes">
                  <Input value={notes} onChange={(_, data) => setNotes(data.value)} />
                </Field>
              </div>

              {selectedLocation?.tracksCollaborators ? (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <Field label="Internal collaborators">
                    <Dropdown
                      multiselect
                      placeholder="Select colleagues"
                      selectedOptions={collaboratorUserIds}
                      onOptionSelect={(_, data) => setCollaboratorUserIds(data.selectedOptions)}
                    >
                      {employees
                        .filter((employee) => employee.id !== (editingUserId ?? selectedUserId ?? user?.id))
                        .map((employee) => (
                          <Option key={employee.id} value={employee.id} text={employee.displayName ?? employee.username}>
                            {employee.displayName ?? employee.username}
                          </Option>
                        ))}
                    </Dropdown>
                  </Field>
                  <Field label="External collaborator">
                    <Input
                      value={externalName}
                      placeholder="Client or partner name"
                      onChange={(_, data) => setExternalName(data.value)}
                    />
                  </Field>
                </div>
              ) : null}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" disabled={isSaving} onClick={closeEdit}>
                Cancel
              </Button>
              <Button appearance="primary" disabled={isSaving || !actualLocationId} onClick={() => void saveAttendance()}>
                {isSaving ? 'Saving...' : 'Save attendance'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      </section>

    </div>
  );
}
