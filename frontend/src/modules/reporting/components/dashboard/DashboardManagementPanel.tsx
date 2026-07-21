import * as React from 'react';
import type { JSXElement, TableColumnDefinition } from '@fluentui/react-components';
import { tokens, Badge, Button, DataGrid, DataGridBody, DataGridCell, DataGridHeader, DataGridHeaderCell, DataGridRow, Spinner, Subtitle2, TableCellLayout, Text, createTableColumn } from '@fluentui/react-components';
import { DeleteRegular, LayoutRowTwoRegular } from '@fluentui/react-icons';

import { ApiError } from '@platform/api/apiClient';
import {
  deleteDashboard,
  deleteDashboardSection,
  getDashboard,
  getDashboards,
} from '@modules/reporting/services/dashboardService';
import type { Dashboard, DashboardSection, DashboardSummary } from '@modules/reporting/types/dashboard';
import { MAX_SECTION_DEPTH } from '@modules/reporting/types/dashboard';
import { setSelectedDashboardId } from '@modules/reporting/utils/dashboardStorage';
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';
import { filterDashboardSections, filterDashboardSummaries } from '@modules/reporting/search/filters';
import { buildSectionTree, flattenSectionTree, getNextSiblingSortOrder, getSectionLabel } from '@modules/reporting/utils/sectionTree';
import { ConfirmAction } from '@platform/ui/ConfirmAction';
import { DashboardFormDialog } from './DashboardFormDialog';
import { DashboardSectionFormDialog } from './DashboardSectionFormDialog';

type SectionRow = DashboardSection & { treeDepth: number };

const nowrap: React.CSSProperties = { whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' };

const formatDate = (iso: string) => {
  const date = new Date(iso);
  return date.toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' });
};

interface DashboardManagementPanelProps {
  selectedDashboardId: string | null;
  onDashboardChange: (dashboardId: string) => void;
  refreshToken?: number;
  searchQuery?: string;
  onChanged?: () => void;
}

export function DashboardManagementPanel({
  selectedDashboardId,
  onDashboardChange,
  refreshToken = 0,
  searchQuery = '',
  onChanged,
}: DashboardManagementPanelProps): JSXElement {
  const [dashboards, setDashboards] = React.useState<DashboardSummary[]>([]);
  const [sections, setSections] = React.useState<DashboardSection[]>([]);
  const [loadingDashboards, setLoadingDashboards] = React.useState(true);
  const [loadingSections, setLoadingSections] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [deleteDashboardOpen, setDeleteDashboardOpen] = React.useState(false);
  const [deleteSectionOpen, setDeleteSectionOpen] = React.useState(false);
  const [pendingDashboard, setPendingDashboard] = React.useState<DashboardSummary | null>(null);
  const [pendingSection, setPendingSection] = React.useState<DashboardSection | null>(null);
  const [isDeleting, setIsDeleting] = React.useState(false);

  const loadDashboards = React.useCallback(async () => {
    setLoadingDashboards(true);
    setError(null);

    try {
      const response = await getDashboards();
      setDashboards(response.items);

      if (response.items.length === 0) {
        return;
      }

      const stillExists = selectedDashboardId
        && response.items.some((item) => item.id === selectedDashboardId);

      if (!stillExists) {
        const fallback = response.items.find((item) => item.isDefault) ?? response.items[0];
        onDashboardChange(fallback.id);
        setSelectedDashboardId(fallback.id);
      }
    } catch {
      setError('Unable to load dashboards.');
      setDashboards([]);
    } finally {
      setLoadingDashboards(false);
    }
  }, [onDashboardChange, selectedDashboardId]);

  const loadSections = React.useCallback(async () => {
    if (!selectedDashboardId) {
      setSections([]);
      return;
    }

    setLoadingSections(true);

    try {
      const dashboard = await getDashboard(selectedDashboardId);
      setSections([...dashboard.sections].sort((a, b) => a.sortOrder - b.sortOrder));
    } catch {
      setSections([]);
      setError('Unable to load dashboard sections.');
    } finally {
      setLoadingSections(false);
    }
  }, [selectedDashboardId]);

  React.useEffect(() => {
    void loadDashboards();
  }, [loadDashboards, refreshToken]);

  React.useEffect(() => {
    void loadSections();
  }, [loadSections, refreshToken]);

  const handleDashboardSaved = React.useCallback((dashboard: Dashboard) => {
    onDashboardChange(dashboard.id);
    setSelectedDashboardId(dashboard.id);
    onChanged?.();
    void loadDashboards();
    void loadSections();
  }, [loadDashboards, loadSections, onChanged, onDashboardChange]);

  const handleSectionSaved = React.useCallback(() => {
    onChanged?.();
    void loadDashboards();
    void loadSections();
  }, [loadDashboards, loadSections, onChanged]);

  const handleConfirmDeleteDashboard = async () => {
    if (!pendingDashboard) {
      return;
    }

    setIsDeleting(true);
    setError(null);

    try {
      await deleteDashboard(pendingDashboard.id);
      setPendingDashboard(null);
      setDeleteDashboardOpen(false);
      onChanged?.();
      await loadDashboards();
      setSections([]);
    } catch (deleteError) {
      setError(deleteError instanceof ApiError ? deleteError.message : 'Failed to delete dashboard.');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleConfirmDeleteSection = async () => {
    if (!pendingSection) {
      return;
    }

    setIsDeleting(true);
    setError(null);

    try {
      await deleteDashboardSection(pendingSection.id);
      setPendingSection(null);
      setDeleteSectionOpen(false);
      onChanged?.();
      await loadDashboards();
      await loadSections();
    } catch (deleteError) {
      setError(deleteError instanceof ApiError ? deleteError.message : 'Failed to delete section.');
    } finally {
      setIsDeleting(false);
    }
  };

  const nextDashboardSortOrder = dashboards.length > 0
    ? Math.max(...dashboards.map((item) => item.sortOrder)) + 1
    : 1;

  const nextSectionSortOrder = getNextSiblingSortOrder(sections);

  const sectionRows = React.useMemo<SectionRow[]>(
    () => flattenSectionTree(buildSectionTree(sections)),
    [sections],
  );

  const visibleDashboards = React.useMemo(
    () => filterDashboardSummaries(dashboards, searchQuery),
    [dashboards, searchQuery],
  );

  const visibleSectionRows = React.useMemo(
    () => filterDashboardSections(sectionRows, searchQuery),
    [searchQuery, sectionRows],
  );

  const dashboardColumns = React.useMemo<TableColumnDefinition<DashboardSummary>[]>(() => [
    createTableColumn<DashboardSummary>({
      columnId: 'name',
      compare: (a, b) => a.name.localeCompare(b.name),
      renderHeaderCell: () => <span style={nowrap}>Name</span>,
      renderCell: (item) => (
        <TableCellLayout>
          <button
            type="button"
            className={`text-left truncate ${selectedDashboardId === item.id ? 'font-semibold' : ''}`}
            onClick={() => {
              onDashboardChange(item.id);
              setSelectedDashboardId(item.id);
            }}
          >
            {item.name}
          </button>
        </TableCellLayout>
      ),
    }),
    createTableColumn<DashboardSummary>({
      columnId: 'description',
      renderHeaderCell: () => <span style={nowrap}>Description</span>,
      renderCell: (item) => <span style={nowrap}>{item.description ?? '—'}</span>,
    }),
    createTableColumn<DashboardSummary>({
      columnId: 'isDefault',
      compare: (a, b) => Number(b.isDefault) - Number(a.isDefault),
      renderHeaderCell: () => <span style={nowrap}>Default</span>,
      renderCell: (item) => (
        item.isDefault
          ? <Badge appearance="tint" color="brand">Default</Badge>
          : <span style={nowrap}>—</span>
      ),
    }),
    createTableColumn<DashboardSummary>({
      columnId: 'sortOrder',
      compare: (a, b) => a.sortOrder - b.sortOrder,
      renderHeaderCell: () => <span style={nowrap}>Order</span>,
      renderCell: (item) => <span style={nowrap}>{item.sortOrder}</span>,
    }),
    createTableColumn<DashboardSummary>({
      columnId: 'sectionCount',
      compare: (a, b) => a.sectionCount - b.sectionCount,
      renderHeaderCell: () => <span style={nowrap}>Sections</span>,
      renderCell: (item) => <span style={nowrap}>{item.sectionCount}</span>,
    }),
    createTableColumn<DashboardSummary>({
      columnId: 'reportCount',
      compare: (a, b) => a.reportCount - b.reportCount,
      renderHeaderCell: () => <span style={nowrap}>Reports</span>,
      renderCell: (item) => <span style={nowrap}>{item.reportCount}</span>,
    }),
    createTableColumn<DashboardSummary>({
      columnId: 'updatedAt',
      compare: (a, b) => a.updatedAt.localeCompare(b.updatedAt),
      renderHeaderCell: () => <span style={nowrap}>Updated</span>,
      renderCell: (item) => <span style={nowrap}>{formatDate(item.updatedAt)}</span>,
    }),
    createTableColumn<DashboardSummary>({
      columnId: 'actions',
      renderHeaderCell: () => <span style={nowrap}>Actions</span>,
      renderCell: (item) => (
        <div className="flex gap-2">
          <DashboardFormDialog
            dashboard={item}
            onSaved={handleDashboardSaved}
            trigger={<Button size="small">Edit</Button>}
          />
          <Button
            size="small"
            appearance="secondary"
            icon={<DeleteRegular />}
            onClick={() => {
              setPendingDashboard(item);
              setDeleteDashboardOpen(true);
            }}
          >
            Delete
          </Button>
        </div>
      ),
    }),
  ], [handleDashboardSaved, onDashboardChange, selectedDashboardId]);

  const sectionColumns = React.useMemo<TableColumnDefinition<SectionRow>[]>(() => [
    createTableColumn<SectionRow>({
      columnId: 'title',
      compare: (a, b) => (a.title ?? '').localeCompare(b.title ?? ''),
      renderHeaderCell: () => <span style={nowrap}>Title</span>,
      renderCell: (item) => (
        <span style={{ ...nowrap, paddingLeft: `${(item.treeDepth - 1) * 16}px` }}>
          {getSectionLabel(item, item.treeDepth)}
        </span>
      ),
    }),
    createTableColumn<SectionRow>({
      columnId: 'depth',
      compare: (a, b) => a.depth - b.depth,
      renderHeaderCell: () => <span style={nowrap}>Level</span>,
      renderCell: (item) => <span style={nowrap}>{item.depth}</span>,
    }),
    createTableColumn<SectionRow>({
      columnId: 'subtitle',
      renderHeaderCell: () => <span style={nowrap}>Subtitle</span>,
      renderCell: (item) => <span style={nowrap}>{item.subtitle ?? '—'}</span>,
    }),
    createTableColumn<SectionRow>({
      columnId: 'layoutDirection',
      renderHeaderCell: () => <span style={nowrap}>Layout</span>,
      renderCell: (item) => <span style={nowrap}>{item.layoutDirection}</span>,
    }),
    createTableColumn<SectionRow>({
      columnId: 'size',
      renderHeaderCell: () => <span style={nowrap}>Size</span>,
      renderCell: (item) => (
        <span style={nowrap}>{item.size === 'FullWidth' ? 'Full Width' : item.size}</span>
      ),
    }),
    createTableColumn<SectionRow>({
      columnId: 'sortOrder',
      compare: (a, b) => a.sortOrder - b.sortOrder,
      renderHeaderCell: () => <span style={nowrap}>Order</span>,
      renderCell: (item) => <span style={nowrap}>{item.sortOrder}</span>,
    }),
    createTableColumn<SectionRow>({
      columnId: 'reportCount',
      compare: (a, b) => a.reports.length - b.reports.length,
      renderHeaderCell: () => <span style={nowrap}>Reports</span>,
      renderCell: (item) => <span style={nowrap}>{item.reports.length}</span>,
    }),
    createTableColumn<SectionRow>({
      columnId: 'updatedAt',
      compare: (a, b) => a.updatedAt.localeCompare(b.updatedAt),
      renderHeaderCell: () => <span style={nowrap}>Updated</span>,
      renderCell: (item) => <span style={nowrap}>{formatDate(item.updatedAt)}</span>,
    }),
    createTableColumn<SectionRow>({
      columnId: 'actions',
      renderHeaderCell: () => <span style={nowrap}>Actions</span>,
      renderCell: (item) => (
        <div className="flex gap-2">
          <DashboardSectionFormDialog
            dashboardId={selectedDashboardId ?? ''}
            section={item}
            sections={sections}
            onSaved={handleSectionSaved}
            trigger={<Button size="small">Edit</Button>}
          />
          {item.depth < MAX_SECTION_DEPTH && (
            <DashboardSectionFormDialog
              dashboardId={selectedDashboardId ?? ''}
              sections={sections}
              parentSectionId={item.id}
              nextSortOrder={getNextSiblingSortOrder(sections, item.id)}
              onSaved={handleSectionSaved}
              trigger={<Button size="small" appearance="secondary">Add child</Button>}
            />
          )}
          <Button
            size="small"
            appearance="secondary"
            icon={<DeleteRegular />}
            onClick={() => {
              setPendingSection(item);
              setDeleteSectionOpen(true);
            }}
          >
            Delete
          </Button>
        </div>
      ),
    }),
  ], [handleSectionSaved, sections, selectedDashboardId]);

  if (loadingDashboards) {
    return (
      <div className="flex h-[80vh] justify-center items-center gap-2 p-6">
        <Spinner size="small" />
        <Text>Loading dashboards...</Text>
      </div>
    );
  }

  return (
    <div className="flex flex-col flex-1 min-h-0 gap-4 p-3 overflow-hidden h-full">
      {error && <Text className="text-red-600 px-1 shrink-0">{error}</Text>}

      <div className="rounded shadow shrink-0" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
        <div className="flex flex-wrap items-center justify-between gap-3 p-3 border-b border-neutral-stroke-2">
          <Subtitle2 className="mb-0">Dashboards</Subtitle2>
          <DashboardFormDialog
            nextSortOrder={nextDashboardSortOrder}
            onSaved={handleDashboardSaved}
          />
        </div>

        {dashboards.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 p-8 text-neutral-600">
            <LayoutRowTwoRegular className="size-16 text-gray-300" />
            <Text>No dashboards yet. Create your first dashboard to get started.</Text>
          </div>
        ) : visibleDashboards.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 p-8 text-neutral-600">
            <Text>No dashboards match your search.</Text>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <DataGrid
              items={visibleDashboards}
              columns={dashboardColumns}
              sortable
              getRowId={(item) => item.id}
              focusMode="composite"
              size="medium"
              style={{ minWidth: '900px' }}
            >
              <DataGridHeader>
                <DataGridRow>
                  {({ renderHeaderCell }) => (
                    <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                  )}
                </DataGridRow>
              </DataGridHeader>
              <DataGridBody<DashboardSummary>>
                {({ item, rowId }) => (
                  <DataGridRow<DashboardSummary>
                    key={rowId}
                    appearance={selectedDashboardId === item.id ? 'brand' : 'none'}
                  >
                    {({ renderCell }) => (
                      <DataGridCell onClick={stopDataGridRowSelection}>
                        {renderCell(item)}
                      </DataGridCell>
                    )}
                  </DataGridRow>
                )}
              </DataGridBody>
            </DataGrid>
          </div>
        )}
      </div>

      <div className="flex flex-col flex-1 min-h-0 rounded shadow" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
        <div className="flex flex-wrap items-center justify-between gap-3 p-3 border-b border-neutral-stroke-2 shrink-0">
          <div className="flex flex-col gap-1">
            <Subtitle2 className="mb-0">Sections</Subtitle2>
            <Text size={200} className="text-neutral-600">
              {selectedDashboardId
                ? `Managing sections for ${dashboards.find((item) => item.id === selectedDashboardId)?.name ?? 'selected dashboard'}`
                : 'Select a dashboard to manage its sections'}
            </Text>
          </div>
          {selectedDashboardId && (
            <DashboardSectionFormDialog
              dashboardId={selectedDashboardId}
              sections={sections}
              nextSortOrder={nextSectionSortOrder}
              onSaved={handleSectionSaved}
            />
          )}
        </div>

        <div className="flex flex-col flex-1 min-h-0 overflow-auto">
        {!selectedDashboardId ? (
          <Text className="p-6 text-neutral-600">Select a dashboard above to view and manage its sections.</Text>
        ) : loadingSections ? (
          <div className="flex items-center gap-2 p-6">
            <Spinner size="small" />
            <Text>Loading sections...</Text>
          </div>
        ) : sections.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 p-8 text-neutral-600">
            <Text>This dashboard has no sections yet. Add a section, then place reports into it.</Text>
            <DashboardSectionFormDialog
              dashboardId={selectedDashboardId}
              sections={sections}
              nextSortOrder={1}
              onSaved={handleSectionSaved}
            />
          </div>
        ) : visibleSectionRows.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 p-8 text-neutral-600">
            <Text>No sections match your search.</Text>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <DataGrid
              items={visibleSectionRows}
              columns={sectionColumns}
              sortable
              getRowId={(item) => item.id}
              focusMode="composite"
              size="medium"
              style={{ minWidth: '860px' }}
            >
              <DataGridHeader>
                <DataGridRow>
                  {({ renderHeaderCell }) => (
                    <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                  )}
                </DataGridRow>
              </DataGridHeader>
              <DataGridBody<SectionRow>>
                {({ item, rowId }) => (
                  <DataGridRow<SectionRow> key={rowId}>
                    {({ renderCell }) => (
                      <DataGridCell onClick={stopDataGridRowSelection}>
                        {renderCell(item)}
                      </DataGridCell>
                    )}
                  </DataGridRow>
                )}
              </DataGridBody>
            </DataGrid>
          </div>
        )}
        </div>
      </div>

      <ConfirmAction
        open={deleteDashboardOpen}
        onOpenChange={setDeleteDashboardOpen}
        title="Delete dashboard"
        message={`This will permanently remove "${pendingDashboard?.name ?? 'this dashboard'}" and all of its sections and reports`}
        actionName={isDeleting ? 'Deleting...' : 'Delete dashboard'}
        destructive
        onAction={() => void handleConfirmDeleteDashboard()}
        onCancel={() => setPendingDashboard(null)}
      />

      <ConfirmAction
        open={deleteSectionOpen}
        onOpenChange={setDeleteSectionOpen}
        title="Delete section"
        message={`This will permanently remove "${pendingSection?.title ?? 'this section'}" and all reports in it`}
        actionName={isDeleting ? 'Deleting...' : 'Delete section'}
        destructive
        onAction={() => void handleConfirmDeleteSection()}
        onCancel={() => setPendingSection(null)}
      />
    </div>
  );
}
