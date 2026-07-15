import type { UserListItem } from '@modules/users/types/user';
import { matchesSearchQuery } from '@platform/search/searchText';

export function filterEmployees(employees: UserListItem[], query: string): UserListItem[] {
  const normalized = query.trim();
  if (!normalized) return employees;
  return employees.filter((employee) => matchesSearchQuery(normalized, [
    employee.displayName, employee.username, employee.email, employee.managerDisplayName,
    employee.companyName, employee.departmentName, employee.positionName, ...employee.roles,
  ]));
}
