import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, MessageBar, MessageBarBody, Spinner, createTableColumn, type TableColumnDefinition, type TableColumnSizingOptions } from '@fluentui/react-components';
import { AddRegular, EditRegular } from '@fluentui/react-icons';
import AppTitle from '../../components/common/AppTitle';
import { AutoFitDataGrid } from '../../components/common/AutoFitDataGrid';
import { withAuditableColumns } from '../../components/common/auditTableColumns';
import { PositionFormDialog } from '../../components/coreHr/PositionFormDialog';
import { ExpenseStatusBadge } from '../../components/expense/expenseBadges';
import { usePageSearchQuery } from '../../context/PageSearchContext';
import { usePermissions } from '../../hooks/usePermissions';
import { ApiError } from '../../services/apiClient';
import { getPositions } from '../../services/coreHrService';
import type { Position, SavePositionRequest } from '../../types/coreHr';
import { matchesSearchQuery } from '../../utils/searchText';
import AppFilters from '../../components/AppFilters';
import { filterPositions } from '../../utils/pageSearch';

function filterItems(items: Position[], query: string): Position[] {
  return filterPositions(items, query);
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


const positionSizingOptions: TableColumnSizingOptions = {
  driverId: { minWidth: 110, idealWidth: 140, defaultWidth: 120 },
  description: { minWidth: 200, idealWidth: 200, defaultWidth: 200 },
  actions: { minWidth: 100, idealWidth: 200, defaultWidth: 200 },
  createdAt: { minWidth: 140, idealWidth: 200, defaultWidth: 140 },
  updatedAt: { minWidth: 140, idealWidth: 200, defaultWidth: 140 },
};


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
    createTableColumn<Position>({ columnId: 'isActive', renderHeaderCell: () => 'Status', renderCell: (item) => <ExpenseStatusBadge status={item.isActive ? 'Active' : 'Inactive'} /> }),
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
             items={filteredItems} 
             columns={columns} 
             getRowId={(item) => item.id} 
             size="small" 
             columnSizingOptions={positionSizingOptions}
             selectionMode="multiselect" />
        )}
      </div>
      <PositionFormDialog open={dialogOpen} initial={editing} onClose={() => setDialogOpen(false)} onSaved={() => void loadItems()} />
    </div>
  );
}
