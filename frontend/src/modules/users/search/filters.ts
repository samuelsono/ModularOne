import type { UserListItem } from '@modules/users/types/user';
import { matchesSearchQuery } from '@platform/search/searchText';
import type { AppFilterSelection } from '@platform/ui/AppFilters';

export function filterEmployees(employees: UserListItem[], query: string): UserListItem[] {
  const normalized = query.trim();
  if (!normalized) return employees;
  return employees.filter((employee) => matchesSearchQuery(normalized, [
    employee.displayName, employee.username, employee.email, employee.managerDisplayName,
    employee.companyName, employee.departmentName, employee.positionName, ...employee.roles,
  ]));
}

export function applyEmployeeOptionFilters(
  employees: UserListItem[],
  selection: AppFilterSelection,
): UserListItem[] {
  let result = employees;

  const status = selection.status ?? [];
  if (status.length > 0) {
    result = result.filter((employee) => {
      const matchesActive = status.includes('active') && employee.isActive;
      const matchesInactive = status.includes('inactive') && !employee.isActive;
      return matchesActive || matchesInactive;
    });
  }

  const invite = selection.invite ?? [];
  if (invite.includes('pending')) {
    result = result.filter((employee) => Boolean(employee.invitePendingAt));
  }

  const driver = selection.driver ?? [];
  if (driver.length > 0) {
    result = result.filter((employee) => {
      const matchesLinked = driver.includes('linked') && Boolean(employee.linkedDriverId);
      const matchesUnlinked = driver.includes('unlinked') && !employee.linkedDriverId;
      return matchesLinked || matchesUnlinked;
    });
  }

  return result;
}
