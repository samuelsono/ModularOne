import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, MessageBar, MessageBarBody, Spinner, createTableColumn, type TableColumnDefinition } from '@fluentui/react-components';
import { AddRegular, EditRegular } from '@fluentui/react-icons';
import AppTitle from '@platform/ui/AppTitle';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';
import { PositionFormDialog } from '@modules/coreHr/components/PositionFormDialog';
import { StatusBadge } from '@platform/ui/StatusBadge';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { usePermissions } from '@platform/permissions/usePermissions';
import { ApiError } from '@platform/api/apiClient';
import { getPositions } from '@modules/coreHr/services/coreHrService';
import type { Position, SavePositionRequest } from '@modules/coreHr/types/coreHr';
import { matchesSearchQuery } from '@platform/search/searchText';
import AppFilters from '@platform/ui/AppFilters';

function filterItems(items: Position[], query: string): Position[] {
  const normalized = query.trim();
  if (!normalized) return items;
  return items.filter((item) => matchesSearchQuery(normalized, [item.name, item.code, item.companyName, item.departmentName, item.description, item.isActive ? 'active' : 'inactive']));
}

const filters = [
  {
    id: 'status',
    label: 'Status',
    type: 'select',
    options: [
      { value: 'active', label: 'Active' },
      { value: 'inactive', label: 'Inactive' },
    ],
  },
];


export default function PositionsPage() {
  const searchQuery = usePageSearchQuery();
  const { hasPermission } = usePermissions();
  const canWrite = hasPermission('core.positions.write');
  const [items, setItems] = useState<Position[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<(SavePositionRequest & { id?: string }) | null>(null);

  const loadItems = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setItems(await getPositions());
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load positions.');
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => { void loadItems(); }, [loadItems]);

  const filteredItems = useMemo(() => filterItems(items, searchQuery), [items, searchQuery]);

  const columns = useMemo<TableColumnDefinition<Position>[]>(() => withAuditableColumns([
    createTableColumn<Position>({ columnId: 'companyName', renderHeaderCell: () => 'Company', renderCell: (item) => item.companyName }),
    createTableColumn<Position>({ columnId: 'departmentName', renderHeaderCell: () => 'Department', renderCell: (item) => item.departmentName }),
    createTableColumn<Position>({ columnId: 'name', renderHeaderCell: () => 'Position', renderCell: (item) => item.name }),
    createTableColumn<Position>({ columnId: 'code', renderHeaderCell: () => 'Code', renderCell: (item) => item.code }),
    createTableColumn<Position>({ columnId: 'description', renderHeaderCell: () => 'Description', renderCell: (item) => item.description ?? '—' }),
    createTableColumn<Position>({ columnId: 'sortOrder', renderHeaderCell: () => 'Sort', renderCell: (item) => item.sortOrder }),
    createTableColumn<Position>({ columnId: 'isActive', renderHeaderCell: () => 'Status', renderCell: (item) => <StatusBadge status={item.isActive ? 'Active' : 'Inactive'} /> }),
    ...(canWrite ? [createTableColumn<Position>({
      columnId: 'actions',
      renderHeaderCell: () => 'Actions',
      renderCell: (item) => (
        <Button appearance="subtle" icon={<EditRegular />} onClick={() => {
          setEditing({ id: item.id, departmentId: item.departmentId, name: item.name, code: item.code, description: item.description, isActive: item.isActive, sortOrder: item.sortOrder });
          setDialogOpen(true);
        }} />
      ),
    })] : []),
  ]), [canWrite]);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Positions" subtitle="Define job positions and tie them to departments." />
      <div className="flex items-start justify-end gap-1">
        <AppFilters filters={filters} onFilterChange={() => {}} />
        {canWrite ? <Button appearance="primary" icon={<AddRegular />} onClick={() => { setEditing(null); setDialogOpen(true); }}>Add position</Button> : null}
      </div>
      </div>
      {error ? <MessageBar intent="error" className="mx-3"><MessageBarBody>{error}</MessageBarBody></MessageBar> : null}
      <div className="flex-1 min-h-0 overflow-auto">
        {isLoading ? <Spinner label="Loading positions..." /> : (
          <AutoFitDataGrid
            enableColumnSizing
            selectionMode='multiselect'
            items={filteredItems}
            columns={columns}
            getRowId={(item) => item.id}
            size="small"
            storageKey="corehr.positions"
          />
        )}
      </div>
      <PositionFormDialog open={dialogOpen} initial={editing} onClose={() => setDialogOpen(false)} onSaved={() => void loadItems()} />
    </div>
  );
}
