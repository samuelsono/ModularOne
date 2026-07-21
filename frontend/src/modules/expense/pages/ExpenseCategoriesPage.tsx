import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
  MessageBar,
  MessageBarBody,
  Spinner,
  createTableColumn,
  type TableColumnDefinition,
  type TableColumnSizingOptions,
} from '@fluentui/react-components';
import { AddRegular, EditRegular } from '@fluentui/react-icons';
import AppTitle from '@platform/ui/AppTitle';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';
import { ExpenseCategoryFormDialog } from '@modules/expense/components/ExpenseCategoryFormDialog';
import { ExpenseCategoryName, ExpenseStatusBadge } from '@modules/expense/components/expenseBadges';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { usePermissions } from '@platform/permissions/usePermissions';
import { ApiError } from '@platform/api/apiClient';
import { getAdminExpenseCategories } from '@modules/expense/services/expenseService';
import type { ExpenseCategory, SaveExpenseCategoryRequest } from '@modules/expense/types/expense';
import { matchesSearchQuery } from '@platform/search/searchText';

function filterCategories(items: ExpenseCategory[], query: string): ExpenseCategory[] {
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

const expenseCategoryColumnSizing: TableColumnSizingOptions = {
  description: { minWidth: 320, idealWidth: 400, defaultWidth: 360 },
  name: { minWidth: 200, idealWidth: 250, defaultWidth: 250 },
};

export default function ExpenseCategoriesPage() {
  const searchQuery = usePageSearchQuery();
  const { hasPermission } = usePermissions();
  const canWrite = hasPermission('expense.categories.write');
  const [categories, setCategories] = useState<ExpenseCategory[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<(SaveExpenseCategoryRequest & { id?: string }) | null>(null);

  const loadCategories = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getAdminExpenseCategories();
      setCategories(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load expense categories.');
      setCategories([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadCategories();
  }, [loadCategories]);

  const filteredCategories = useMemo(
    () => filterCategories(categories, searchQuery),
    [categories, searchQuery],
  );

  const columns = useMemo<TableColumnDefinition<ExpenseCategory>[]>(() => withAuditableColumns([
    createTableColumn<ExpenseCategory>({
      columnId: 'name',
      renderHeaderCell: () => 'Name',
      renderCell: (item) => <ExpenseCategoryName name={item.name} code={item.code} />,
    }),
    createTableColumn<ExpenseCategory>({
      columnId: 'code',
      renderHeaderCell: () => 'Code',
      renderCell: (item) => item.code,
    }),
    createTableColumn<ExpenseCategory>({
      columnId: 'description',
      renderHeaderCell: () => 'Description',
      renderCell: (item) => item.description ?? '—',
    }),
    createTableColumn<ExpenseCategory>({
      columnId: 'policy',
      renderHeaderCell: () => 'Policy',
      renderCell: (item) => {
        const flags = [
          item.requiresReceipt ? 'Receipt' : null,
          item.requiresTravelDetails ? 'Travel details' : null,
          item.paysByKilometer ? 'Per km rate' : null,
        ].filter(Boolean);

        return flags.length > 0 ? flags.join(', ') : '—';
      },
    }),
    createTableColumn<ExpenseCategory>({
      columnId: 'sortOrder',
      renderHeaderCell: () => 'Sort',
      renderCell: (item) => item.sortOrder,
    }),
    createTableColumn<ExpenseCategory>({
      columnId: 'isActive',
      renderHeaderCell: () => 'Status',
      renderCell: (item) => <ExpenseStatusBadge status={item.isActive ? 'Active' : 'Inactive'} />,
    }),
    ...(canWrite
      ? [
          createTableColumn<ExpenseCategory>({
            columnId: 'actions',
            renderHeaderCell: () => 'Actions',
            renderCell: (item) => (
              <Button
                appearance="subtle"
                icon={<EditRegular />}
                onClick={() => {
                  setEditingCategory({
                    id: item.id,
                    name: item.name,
                    code: item.code,
                    description: item.description,
                    isActive: item.isActive,
                    sortOrder: item.sortOrder,
                    requiresReceipt: item.requiresReceipt,
                    requiresTravelDetails: item.requiresTravelDetails,
                    paysByKilometer: item.paysByKilometer,
                  });
                  setDialogOpen(true);
                }}
              />
            ),
          }),
        ]
      : []),
  ]), [canWrite]);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Expense categories" subtitle="Default claim types available for employee expense submissions." />
        {canWrite ? (
          <Button
            appearance="primary"
            icon={<AddRegular />}
            onClick={() => {
              setEditingCategory(null);
              setDialogOpen(true);
            }}
          >
            Add category
          </Button>
        ) : null}
      </div>

      {error ? (
        <MessageBar intent="error" className="mx-3">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className="flex-1 min-h-0 overflow-auto">
        {isLoading ? (
          <Spinner label="Loading categories..." />
        ) : (
          <AutoFitDataGrid
            items={filteredCategories}
            columns={columns}
            getRowId={(item) => item.id}
            columnSizingOptions={expenseCategoryColumnSizing}
            size="small"
            storageKey="expense.categories"
          />
        )}
      </div>

      <ExpenseCategoryFormDialog
        open={dialogOpen}
        initial={editingCategory}
        onClose={() => setDialogOpen(false)}
        onSaved={() => void loadCategories()}
      />
    </div>
  );
}
