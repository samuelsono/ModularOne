import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, MessageBar, MessageBarBody, Spinner, createTableColumn, type TableColumnDefinition } from '@fluentui/react-components';
import { AddRegular, EditRegular } from '@fluentui/react-icons';
import AppTitle from '../../components/common/AppTitle';
import { AutoFitDataGrid } from '../../components/common/AutoFitDataGrid';
import { withAuditableColumns } from '../../components/common/auditTableColumns';
import { DepartmentFormDialog } from '../../components/coreHr/DepartmentFormDialog';
import { ExpenseStatusBadge } from '../../components/expense/expenseBadges';
import { usePageSearchQuery } from '../../context/PageSearchContext';
import { usePermissions } from '../../hooks/usePermissions';
import { ApiError } from '../../services/apiClient';
import { getDepartments } from '../../services/coreHrService';
import type { Department, SaveDepartmentRequest } from '../../types/coreHr';
import { matchesSearchQuery } from '../../utils/searchText';

function filterItems(items: Department[], query: string): Department[] {
  const normalized = query.trim();
  if (!normalized) return items;
  return items.filter((item) => matchesSearchQuery(normalized, [item.name, item.code, item.companyName, item.description, item.isActive ? 'active' : 'inactive']));
}

export default function DepartmentsPage() {
  const searchQuery = usePageSearchQuery();
  const { hasPermission } = usePermissions();
  const canWrite = hasPermission('core.departments.write');
  const [items, setItems] = useState<Department[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<(SaveDepartmentRequest & { id?: string }) | null>(null);

  const loadItems = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setItems(await getDepartments());
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load departments.');
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => { void loadItems(); }, [loadItems]);

  const filteredItems = useMemo(() => filterItems(items, searchQuery), [items, searchQuery]);

  const columns = useMemo<TableColumnDefinition<Department>[]>(() => withAuditableColumns([
    createTableColumn<Department>({ columnId: 'companyName', renderHeaderCell: () => 'Company', renderCell: (item) => item.companyName }),
    createTableColumn<Department>({ columnId: 'name', renderHeaderCell: () => 'Department', renderCell: (item) => item.name }),
    createTableColumn<Department>({ columnId: 'code', renderHeaderCell: () => 'Code', renderCell: (item) => item.code }),
    createTableColumn<Department>({ columnId: 'description', renderHeaderCell: () => 'Description', renderCell: (item) => item.description ?? '—' }),
    createTableColumn<Department>({ columnId: 'sortOrder', renderHeaderCell: () => 'Sort', renderCell: (item) => item.sortOrder }),
    createTableColumn<Department>({ columnId: 'isActive', renderHeaderCell: () => 'Status', renderCell: (item) => <ExpenseStatusBadge status={item.isActive ? 'Active' : 'Inactive'} /> }),
    ...(canWrite ? [createTableColumn<Department>({
      columnId: 'actions',
      renderHeaderCell: () => 'Actions',
      renderCell: (item) => (
        <Button appearance="subtle" icon={<EditRegular />} onClick={() => {
          setEditing({ id: item.id, companyId: item.companyId, name: item.name, code: item.code, description: item.description, isActive: item.isActive, sortOrder: item.sortOrder });
          setDialogOpen(true);
        }} />
      ),
    })] : []),
  ]), [canWrite]);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Departments" subtitle="Organize employees into departments linked to companies." />
        {canWrite ? <Button appearance="primary" icon={<AddRegular />} onClick={() => { setEditing(null); setDialogOpen(true); }}>Add department</Button> : null}
      </div>
      {error ? <MessageBar intent="error" className="mx-3"><MessageBarBody>{error}</MessageBarBody></MessageBar> : null}
      <div className="flex-1 min-h-0 overflow-auto">
        {isLoading ? <Spinner label="Loading departments..." /> : (
          <AutoFitDataGrid items={filteredItems} columns={columns} getRowId={(item) => item.id} size="small" />
        )}
      </div>
      <DepartmentFormDialog open={dialogOpen} initial={editing} onClose={() => setDialogOpen(false)} onSaved={() => void loadItems()} />
    </div>
  );
}
