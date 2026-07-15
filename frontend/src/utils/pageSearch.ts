import type { DashboardRender, DashboardSectionRender } from '../types/dashboard';
import type { Driver } from '../types/driver';
import type { ExpenseCategory, ExpenseCategoryBalance, ExpenseClaim } from '../types/expense';
import type {
  LeaveBalance,
  LeaveHistoryRow,
  LeaveLiabilityRow,
  LeaveRequest,
  LeaveType,
  PublicHoliday,
} from '../types/leave';
import type { Report } from '../types/report';
import type { Vehicle } from '../types/vehicle';
import type { UserListItem } from '../types/user';
import { matchesSearchQuery } from './searchText';
import type { Position } from '../types/coreHr';

function isCoreHrPath(pathname: string): boolean {
  return pathname === '/core' || pathname.startsWith('/core/');
}

function isLeavePath(pathname: string): boolean {
  return pathname === '/leave' || pathname.startsWith('/leave/');
}

function isExpensePath(pathname: string): boolean {
  return pathname === '/expense' || pathname.startsWith('/expense/');
}

export function getPageSearchPlaceholder(pathname: string): string {
  if (pathname === '/' || pathname === '') {
    return 'Search dashboard reports and sections';
  }

  if (pathname === '/leave' || pathname === '/leave/') {
    return 'Search leave history, pending approvals, or balance liability';
  }

  if (pathname.startsWith('/leave/requests')) {
    return 'Search by type, status, dates, or notes';
  }

  if (pathname.startsWith('/leave/approvals')) {
    return 'Search by employee, type, dates, or document';
  }

  if (pathname.startsWith('/leave/balances')) {
    return 'Search by leave type';
  }

  if (pathname.startsWith('/leave/policies')) {
    return 'Search leave types or public holidays';
  }

  if (pathname.startsWith('/leave/calendar')) {
    return 'Search by employee, department, or leave type';
  }

  if (isLeavePath(pathname)) {
    return 'Search leave records';
  }

  if (pathname === '/expense' || pathname === '/expense/') {
    return 'Search by category, amount, status, or description';
  }

  if (pathname.startsWith('/expense/approvals')) {
    return 'Search by employee, category, amount, or description';
  }

  if (pathname.startsWith('/expense/categories')) {
    return 'Search by category name or code';
  }

  if (pathname.startsWith('/expense/balances')) {
    return 'Search by category or amount';
  }

  if (pathname.startsWith('/expense/reports')) {
    return 'Search expense history by category, status, or description';
  }

  if (isExpensePath(pathname)) {
    return 'Search expense claims';
  }

  if (pathname.startsWith('/vehicle-list')) {
    return 'Search by registration, VIN, make, or model';
  }

  if (pathname.startsWith('/live-tracking')) {
    return 'Search by registration, VIN, make, or model';
  }

  if (pathname.startsWith('/drivers')) {
    return 'Search by first name, last name, or email';
  }

  if (pathname.startsWith('/employees')) {
    return 'Search by name, email, role, or manager';
  }

  if (pathname.startsWith('/core/employees')) {
    return 'Search by name, email, role, or manager';
  }

  if (pathname.startsWith('/reports')) {
    return 'Search by report or dashboard name';
  }

  if (pathname.startsWith('/settings')) {
    return 'Search settings';
  }

  return 'Search current page';
}

export function isPageSearchEnabled(pathname: string): boolean {
  return pathname === '/'
    || pathname.startsWith('/vehicle-list')
    || pathname.startsWith('/live-tracking')
    || pathname.startsWith('/drivers')
    || pathname.startsWith('/employees')
    || pathname.startsWith('/core/employees')
    || pathname.startsWith('/reports')
    || pathname.startsWith('/settings')
    || pathname.startsWith('/core/positions')
    || isLeavePath(pathname)
    || isExpensePath(pathname);
}

export function filterExpenseClaims(items: ExpenseClaim[], query: string): ExpenseClaim[] {
  const normalized = query.trim();
  if (!normalized) {
    return items;
  }

  return items.filter((item) => matchesSearchQuery(normalized, [
    item.requesterDisplayName,
    item.categoryName,
    item.categoryCode,
    item.description,
    item.notes,
    item.status,
    item.expenseDate,
    item.amount,
    item.currency,
  ]));
}

export function filterExpenseBalances(items: ExpenseCategoryBalance[], query: string): ExpenseCategoryBalance[] {
  const normalized = query.trim();
  if (!normalized) {
    return items;
  }

  return items.filter((item) => matchesSearchQuery(normalized, [
    item.categoryName,
    item.categoryCode,
    item.pendingAmount,
    item.pendingPaymentAmount,
    item.paidAmount,
    item.draftAmount,
    item.rejectedAmount,
    item.cancelledAmount,
    item.totalSubmitted,
  ]));
}

export function filterExpenseCategories(items: ExpenseCategory[], query: string): ExpenseCategory[] {
  const normalized = query.trim();
  if (!normalized) {
    return items;
  }

  return items.filter((item) => matchesSearchQuery(normalized, [
    item.name,
    item.code,
    item.description,
    item.isActive ? 'active' : 'inactive',
  ]));
}

export function filterLeaveRequests(requests: LeaveRequest[], query: string): LeaveRequest[] {
  const normalized = query.trim();
  if (!normalized) {
    return requests;
  }

  return requests.filter((request) => matchesSearchQuery(normalized, [
    request.requesterDisplayName,
    request.leaveType,
    request.status,
    request.notes,
    request.documentFileName,
    request.startDate,
    request.endDate,
    request.workingDays,
    request.startDayPortion,
    request.endDayPortion,
  ]));
}

export function filterLeaveHistoryRows(rows: LeaveHistoryRow[], query: string): LeaveHistoryRow[] {
  const normalized = query.trim();
  if (!normalized) {
    return rows;
  }

  return rows.filter((row) => matchesSearchQuery(normalized, [
    row.requesterDisplayName,
    row.department,
    row.branch,
    row.leaveType,
    row.status,
    row.notes,
    row.startDate,
    row.endDate,
    row.workingDays,
  ]));
}

export function filterLeaveLiabilityRows(rows: LeaveLiabilityRow[], query: string): LeaveLiabilityRow[] {
  const normalized = query.trim();
  if (!normalized) {
    return rows;
  }

  return rows.filter((row) => matchesSearchQuery(normalized, [
    row.displayName,
    row.department,
    row.leaveTypeName,
    row.leaveTypeCode,
    row.allocated,
    row.used,
    row.pending,
    row.remaining,
  ]));
}

export function filterLeaveBalances(balances: LeaveBalance[], query: string): LeaveBalance[] {
  const normalized = query.trim();
  if (!normalized) {
    return balances;
  }

  return balances.filter((balance) => matchesSearchQuery(normalized, [
    balance.leaveTypeName,
    balance.cycleStart,
    balance.cycleEnd,
    balance.remaining,
    balance.allocated,
    balance.used,
    balance.pending,
    balance.adjusted,
  ]));
}

export function filterLeaveTypes(types: LeaveType[], query: string): LeaveType[] {
  const normalized = query.trim();
  if (!normalized) {
    return types;
  }

  return types.filter((type) => matchesSearchQuery(normalized, [
    type.name,
    type.code,
    type.accrualMethod,
    type.isPaid ? 'paid' : 'unpaid',
    type.deductsBalance ? 'deducts balance' : '',
    type.requiresDocument ? 'document' : '',
    type.allowHalfDay ? 'half day' : '',
  ]));
}

export function filterPublicHolidays(holidays: PublicHoliday[], query: string): PublicHoliday[] {
  const normalized = query.trim();
  if (!normalized) {
    return holidays;
  }

  return holidays.filter((holiday) => matchesSearchQuery(normalized, [
    holiday.name,
    holiday.date,
    holiday.branch,
    holiday.isRecurring ? 'recurring' : 'once-off',
  ]));
}

export function filterLeaveCalendarEntries<T extends {
  displayName: string;
  department: string | null;
  branch: string | null;
  leaveTypeName: string;
  status: string;
  startDate: string;
  endDate: string;
}>(
  entries: T[],
  query: string,
): T[] {
  const normalized = query.trim();
  if (!normalized) {
    return entries;
  }

  return entries.filter((entry) => matchesSearchQuery(normalized, [
    entry.displayName,
    entry.department,
    entry.branch,
    entry.leaveTypeName,
    entry.status,
    entry.startDate,
    entry.endDate,
  ]));
}

export function filterPositions(positions: Position[], query: string): Position[] {
  const normalized = query.trim();
  if (!normalized) {
    return positions;
  }
  return positions.filter((position) => matchesSearchQuery(normalized, [
    position.name,
    position.code,
    position.departmentName,
    position.companyName
  ]));
}

export function filterVehicles(vehicles: Vehicle[], query: string): Vehicle[] {
  const normalized = query.trim();
  if (!normalized) {
    return vehicles;
  }

  return vehicles.filter((vehicle) => matchesSearchQuery(normalized, [
    vehicle.registrationNumber,
    vehicle.vin,
    vehicle.make,
    vehicle.model,
    vehicle.engineNumber,
    vehicle.colour,
    vehicle.vehicleType,
    vehicle.registeredOwner,
    vehicle.status?.location?.positionDescription,
    vehicle.status?.driver?.firstName,
    vehicle.status?.driver?.lastName,
  ]));
}

export function filterDrivers(drivers: Driver[], query: string): Driver[] {
  const normalized = query.trim();
  if (!normalized) {
    return drivers;
  }

  return drivers.filter((driver) => matchesSearchQuery(normalized, [
    driver.firstName,
    driver.lastName,
    driver.name,
    driver.email,
    driver.workEmail,
    driver.driverId,
    driver.employeeNumber,
    driver.licenceNumber,
    driver.contactNumber,
    driver.workPhone,
    driver.department,
    driver.branch,
    driver.jobTitle,
  ]));
}

export function filterEmployees(employees: UserListItem[], query: string): UserListItem[] {
  const normalized = query.trim();
  if (!normalized) {
    return employees;
  }

  return employees.filter((employee) => matchesSearchQuery(normalized, [
    employee.displayName,
    employee.username,
    employee.email,
    employee.managerDisplayName,
    employee.companyName,
    employee.departmentName,
    employee.positionName,
    ...employee.roles,
  ]));
}

export function filterReports(reports: Report[], query: string): Report[] {
  const normalized = query.trim();
  if (!normalized) {
    return reports;
  }

  return reports.filter((report) => {
    const placementLabels = (report.placements ?? []).flatMap((placement) => [
      placement.dashboardName,
      placement.sectionTitle,
    ]);

    return matchesSearchQuery(normalized, [
      report.name,
      report.description,
      report.reportType,
      report.targetTable,
      ...placementLabels,
    ]);
  });
}

export function filterDashboardSummaries<T extends { name: string; description?: string | null }>(
  items: T[],
  query: string,
): T[] {
  const normalized = query.trim();
  if (!normalized) {
    return items;
  }

  return items.filter((item) => matchesSearchQuery(normalized, [
    item.name,
    item.description,
  ]));
}

export function filterDashboardSections<T extends {
  title?: string | null;
  subtitle?: string | null;
  layoutDirection: string;
}>(
  sections: T[],
  query: string,
): T[] {
  const normalized = query.trim();
  if (!normalized) {
    return sections;
  }

  return sections.filter((section) => matchesSearchQuery(normalized, [
    section.title,
    section.subtitle,
    section.layoutDirection,
  ]));
}

export function filterDashboardRender(dashboard: DashboardRender, query: string): DashboardRender {
  const normalized = query.trim();
  if (!normalized) {
    return dashboard;
  }

  return {
    ...dashboard,
    sections: dashboard.sections
      .map((section) => filterSectionRender(section, normalized))
      .filter((section) => sectionHasVisibleContent(section)),
  };
}

function filterSectionRender(section: DashboardSectionRender, query: string): DashboardSectionRender {
  const filteredReports = section.reports.filter((item) => matchesSearchQuery(query, [
    item.report.name,
    item.report.description,
    item.report.reportType,
  ]));

  const filteredChildren = section.childSections
    .map((child) => filterSectionRender(child, query))
    .filter((child) => sectionHasVisibleContent(child));

  const sectionMatches = matchesSearchQuery(query, [
    section.title,
    section.subtitle,
  ]);

  return {
    ...section,
    reports: sectionMatches ? section.reports : filteredReports,
    childSections: sectionMatches ? section.childSections : filteredChildren,
  };
}

function sectionHasVisibleContent(section: DashboardSectionRender): boolean {
  return section.reports.length > 0
    || section.childSections.some((child) => sectionHasVisibleContent(child))
    || Boolean(section.title)
    || Boolean(section.subtitle);
}

export function filterSettingsSections(
  categories: Array<{
    id: string;
    label: string;
    sections: Array<{ id: string; label: string }>;
  }>,
  query: string,
) {
  const normalized = query.trim().toLowerCase();
  if (!normalized) {
    return categories;
  }

  return categories
    .map((category) => ({
      ...category,
      sections: category.sections.filter(
        (section) =>
          section.label.toLowerCase().includes(normalized)
          || category.label.toLowerCase().includes(normalized),
      ),
    }))
    .filter((category) =>
      category.label.toLowerCase().includes(normalized) || category.sections.length > 0);
}
