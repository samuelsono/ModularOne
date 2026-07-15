import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, MessageBar, MessageBarBody, Spinner, createTableColumn, type TableColumnDefinition } from '@fluentui/react-components';
import { AddRegular, EditRegular } from '@fluentui/react-icons';
import AppTitle from '@platform/ui/AppTitle';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';
import { CompanyFormDialog } from '@modules/coreHr/components/CompanyFormDialog';
import { StatusBadge } from '@platform/ui/StatusBadge';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { usePermissions } from '@platform/permissions/usePermissions';
import { ApiError } from '@platform/api/apiClient';
import { getCompanies } from '@modules/coreHr/services/coreHrService';
import type { Company, SaveCompanyRequest } from '@modules/coreHr/types/coreHr';
import { matchesSearchQuery } from '@platform/search/searchText';

function filterItems(items: Company[], query: string): Company[] {
  const normalized = query.trim();
  if (!normalized) return items;
  return items.filter((item) => matchesSearchQuery(normalized, [item.name, item.code, item.description, item.isActive ? 'active' : 'inactive']));
}

export default function CompaniesPage() {
  const searchQuery = usePageSearchQuery();
  const { hasPermission } = usePermissions();
  const canWrite = hasPermission('core.companies.write');
  const [items, setItems] = useState<Company[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<(SaveCompanyRequest & { id?: string }) | null>(null);

  const loadItems = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setItems(await getCompanies());
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load companies.');
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => { void loadItems(); }, [loadItems]);

  const filteredItems = useMemo(() => filterItems(items, searchQuery), [items, searchQuery]);

  const columns = useMemo<TableColumnDefinition<Company>[]>(() => withAuditableColumns([
    createTableColumn<Company>({ columnId: 'name', renderHeaderCell: () => 'Name', renderCell: (item) => item.name }),
    createTableColumn<Company>({ columnId: 'code', renderHeaderCell: () => 'Code', renderCell: (item) => item.code }),
    createTableColumn<Company>({ columnId: 'description', renderHeaderCell: () => 'Description', renderCell: (item) => item.description ?? '—' }),
    createTableColumn<Company>({ columnId: 'sortOrder', renderHeaderCell: () => 'Sort', renderCell: (item) => item.sortOrder }),
    createTableColumn<Company>({ columnId: 'isActive', renderHeaderCell: () => 'Status', renderCell: (item) => <StatusBadge status={item.isActive ? 'Active' : 'Inactive'} /> }),
    ...(canWrite ? [createTableColumn<Company>({
      columnId: 'actions',
      renderHeaderCell: () => 'Actions',
      renderCell: (item) => (
        <Button appearance="subtle" icon={<EditRegular />} onClick={() => {
          setEditing({ id: item.id, name: item.name, code: item.code, description: item.description, isActive: item.isActive, sortOrder: item.sortOrder });
          setDialogOpen(true);
        }} />
      ),
    })] : []),
  ]), [canWrite]);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Companies" subtitle="Manage legal entities and company records used by departments and employees." />
        {canWrite ? <Button appearance="primary" icon={<AddRegular />} onClick={() => { setEditing(null); setDialogOpen(true); }}>Add company</Button> : null}
      </div>
      {error ? <MessageBar intent="error" className="mx-3"><MessageBarBody>{error}</MessageBarBody></MessageBar> : null}
      <div className="flex-1 min-h-0 overflow-auto">
        {isLoading ? <Spinner label="Loading companies..." /> : (
          <AutoFitDataGrid items={filteredItems} columns={columns} getRowId={(item) => item.id} size="small" />
        )}
      </div>
      <CompanyFormDialog open={dialogOpen} initial={editing} onClose={() => setDialogOpen(false)} onSaved={() => void loadItems()} />
    </div>
  );
}
