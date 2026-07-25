import type { AttendanceCompareRow } from '@modules/leave/types/leave';

export type AttendanceHealthLevel = 'healthy' | 'watch' | 'risk' | 'unknown';

export interface AttendanceHealthBreakdown {
  match: number;
  missing: number;
  mismatch: number;
  other: number;
  workDays: number;
  level: AttendanceHealthLevel;
  label: string;
  matchRate: number;
}

export interface AttendanceHealthScore {
  percentage: number;
  presentDays: number;
  countedDays: number;
  label: string;
  color: string;
}

const HEALTH_COLORS = {
  healthy: '#107C10',
  watch: '#FDE300',
  risk: '#D13438',
  unknown: '#8A8886',
} as const;

export const ATTENDANCE_HEALTH_COLORS = HEALTH_COLORS;

export function computeAttendanceHealth(rows: AttendanceCompareRow[]): AttendanceHealthBreakdown {
  const workRows = rows.filter((row) => row.plannedKind === 'Work');
  const match = workRows.filter((row) => row.isMatch).length;
  const missing = workRows.filter((row) => !row.hasActual).length;
  const mismatch = workRows.filter((row) => row.isMismatch).length;
  const other = Math.max(0, rows.length - workRows.length);
  const workDays = workRows.length;
  const matchRate = workDays === 0 ? 0 : match / workDays;
  const missingRate = workDays === 0 ? 0 : missing / workDays;

  let level: AttendanceHealthLevel = 'unknown';
  let label = 'No scheduled work days';

  if (workDays > 0) {
    if (matchRate >= 0.85 && missingRate <= 0.1) {
      level = 'healthy';
      label = 'Healthy';
    } else if (matchRate >= 0.6 || missingRate <= 0.3) {
      level = 'watch';
      label = 'Needs attention';
    } else {
      level = 'risk';
      label = 'At risk';
    }
  }

  return {
    match,
    missing,
    mismatch,
    other,
    workDays,
    level,
    label,
    matchRate,
  };
}

export function attendanceHealthDonutPoints(breakdown: AttendanceHealthBreakdown) {
  return [
    { legend: 'Match', data: breakdown.match, color: HEALTH_COLORS.healthy },
    { legend: 'Missing', data: breakdown.missing, color: HEALTH_COLORS.watch },
    { legend: 'Mismatch', data: breakdown.mismatch, color: HEALTH_COLORS.risk },
    { legend: 'Other', data: breakdown.other, color: HEALTH_COLORS.unknown },
  ].filter((point) => point.data > 0);
}

function getAttendanceHealthColor(percentage: number): string {
  if (percentage < 0.25) {
    return HEALTH_COLORS.risk;
  }

  if (percentage < 0.5) {
    return '#D83B01';
  }

  if (percentage < 0.75) {
    return HEALTH_COLORS.watch;
  }

  if (percentage < 0.9) {
    return '#0099BC';
  }

  return HEALTH_COLORS.healthy;
}

export function computeAttendanceHealthScore(
  rows: AttendanceCompareRow[],
  absentLocationTypeId: string | null,
): AttendanceHealthScore {
  const countedRows = rows.filter((row) => row.plannedKind === 'Work');
  const presentDays = countedRows.filter((row) => {
    if (!row.hasActual) {
      return false;
    }

    return row.actualLocationTypeId !== absentLocationTypeId;
  }).length;

  const countedDays = countedRows.length;
  const percentage = countedDays === 0 ? 0 : presentDays / countedDays;

  return {
    percentage,
    presentDays,
    countedDays,
    label: countedDays === 0
      ? 'No scheduled work days'
      : `${Math.round(percentage * 100)}% in selected range`,
    color: countedDays === 0 ? HEALTH_COLORS.unknown : getAttendanceHealthColor(percentage),
  };
}
