import * as React from "react";

import {
  ChartMultipleRegular,
  DataPieRegular,
  NumberRowRegular,
  DataBarVerticalRegular,
  DeleteRegular,
  TableRegular,
} from "@fluentui/react-icons";
import {
  Badge,
  Button,
  Spinner,
  Switch,
  Text,
  DataGridBody,
  DataGridRow,
  DataGrid,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridCell,
  TableCellLayout,
  createTableColumn,
} from "@fluentui/react-components";
import type {
  JSXElement,
  TableColumnDefinition,
} from "@fluentui/react-components";
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';
import { usePersistedColumnSizing } from '@platform/utils/usePersistedColumnSizing';
import { notifyDashboardChanged } from '@modules/reporting/utils/dashboardStorage';
import type { Report, ReportType } from '@modules/reporting/types/report';
import { ApiError } from '@platform/api/apiClient';
import { deleteReport, getReports, updateReport } from '@modules/reporting/services/reportService';
import { filterReports } from '@modules/reporting/search/filters';
import { ConfirmAction } from '@platform/ui/ConfirmAction';
import { ReportBuilderDialog } from "./reports/ReportBuilderDialog";

type DisplayReportType = "Metric Card" | "Column" | "Pie" | "Donut" | "Line" | "API Table";
type DisplayReportSize = "Small" | "Medium" | "Large" | "Full Width";

type ReportRow = {
  id: string;
  name: string;
  type: DisplayReportType;
  size: DisplayReportSize;
  order: number;
  visibleOnDashboard: boolean;
  placementCount: number;
  lastUpdated: string;
  source: Report;
};

function getPlacementSummary(report: Report): string {
  if (!report.placements?.length) {
    return "—";
  }

  const labels = report.placements.map((placement) => {
    const dashboard = placement.dashboardName ?? "Dashboard";
    const section = placement.sectionTitle ?? "Section";
    return `${dashboard} / ${section}`;
  });

  return labels.join(", ");
}

function isVisibleForDashboard(report: Report, dashboardId: string | null): boolean {
  if (!dashboardId) {
    return report.placements?.every((placement) => placement.isVisible) ?? report.isVisible;
  }

  const dashboardPlacements = report.placements?.filter((placement) => placement.dashboardId === dashboardId) ?? [];
  return dashboardPlacements.length > 0
    ? dashboardPlacements.every((placement) => placement.isVisible)
    : report.isVisible;
}

const typeIconMap: Record<DisplayReportType, JSXElement> = {
  "Metric Card": <DataBarVerticalRegular />,
  Column: <ChartMultipleRegular />,
  Pie: <DataPieRegular />,
  Donut: <DataPieRegular />,
  Line: <NumberRowRegular />,
  "API Table": <TableRegular />,
};

const nowrap: React.CSSProperties = { whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" };

const formatDate = (iso: string) => {
  const d = new Date(iso);
  return d.toLocaleDateString("en-ZA", { day: "2-digit", month: "short", year: "numeric" });
};

const sizeColour: Record<DisplayReportSize, "brand" | "informative" | "success" | "important"> = {
  Small: "informative",
  Medium: "brand",
  Large: "success",
  "Full Width": "important",
};

function toDisplayType(reportType: ReportType): DisplayReportType {
  switch (reportType) {
    case "MetricCard":
      return "Metric Card";
    case "Donut":
      return "Donut";
    case "Pie":
      return "Pie";
    case "Line":
      return "Line";
    case "ApiTable":
      return "API Table";
    case "Column":
    default:
      return "Column";
  }
}

function toDisplaySize(size: Report["size"]): DisplayReportSize {
  return size === "FullWidth" ? "Full Width" : size;
}

function toReportRow(report: Report): ReportRow {
  return {
    id: report.id,
    name: report.name,
    type: toDisplayType(report.reportType),
    size: toDisplaySize(report.size),
    order: report.sortOrder,
    visibleOnDashboard: report.isVisible,
    placementCount: report.placements?.length ?? 0,
    lastUpdated: report.updatedAt,
    source: report,
  };
}

export const ReportsTable = ({
  refreshToken = 0,
  dashboardId = null,
  searchQuery = '',
  onChanged,
}: {
  refreshToken?: number;
  dashboardId?: string | null;
  searchQuery?: string;
  onChanged?: () => void;
}): JSXElement => {
  const [items, setItems] = React.useState<ReportRow[]>([]);
  const [allItems, setAllItems] = React.useState<ReportRow[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [updatingId, setUpdatingId] = React.useState<string | null>(null);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = React.useState(false);
  const [pendingDelete, setPendingDelete] = React.useState<ReportRow | null>(null);
  const [isDeleting, setIsDeleting] = React.useState(false);

  const loadReports = React.useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await getReports();
      const rows = response.items.map(toReportRow);
      setAllItems(rows);
      setItems(rows);
    } catch {
      setError("Unable to load reports.");
      setAllItems([]);
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, []);

  React.useEffect(() => {
    void loadReports();
  }, [loadReports, refreshToken]);

  React.useEffect(() => {
    let nextItems = allItems;

    if (dashboardId) {
      nextItems = nextItems.filter((item) =>
        item.source.placements?.some((placement) => placement.dashboardId === dashboardId)
        ?? item.source.dashboardId === dashboardId,
      );
    }

    nextItems = nextItems.filter((item) =>
      filterReports([item.source], searchQuery).length > 0,
    );

    setItems(nextItems);
  }, [allItems, dashboardId, searchQuery]);

  const handleVisibilityChange = React.useCallback(async (row: ReportRow, checked: boolean) => {
    setUpdatingId(row.id);

    try {
      const placements = (row.source.placements ?? []).map((placement) => ({
        sectionId: placement.sectionId,
        sortOrder: placement.sortOrder,
        size: placement.size,
        isVisible: dashboardId
          ? placement.dashboardId === dashboardId
            ? checked
            : placement.isVisible
          : checked,
      }));

      const updated = await updateReport(row.id, {
        name: row.source.name,
        description: row.source.description,
        reportType: row.source.reportType,
        size: row.source.size,
        sortOrder: row.source.sortOrder,
        isVisible: checked,
        targetTable: row.source.targetTable,
        aggregateFunction: row.source.aggregateFunction,
        aggregateField: row.source.aggregateField,
        groupByColumns: row.source.groupByColumns,
        filters: row.source.filters,
        comparisonEnabled: row.source.comparisonEnabled,
        chartOptionsJson: row.source.chartOptionsJson,
        placements,
      });

      setAllItems((current) =>
        current.map((item) => (item.id === row.id ? toReportRow(updated) : item)),
      );
      notifyDashboardChanged();
      onChanged?.();
    } catch {
      setError("Failed to update report visibility.");
    } finally {
      setUpdatingId(null);
    }
  }, [onChanged]);

  const requestDelete = React.useCallback((row: ReportRow) => {
    setPendingDelete(row);
    setDeleteConfirmOpen(true);
  }, []);

  const handleConfirmDelete = React.useCallback(async () => {
    if (!pendingDelete) {
      return;
    }

    setIsDeleting(true);

    try {
      await deleteReport(pendingDelete.id);
      setAllItems((current) => current.filter((item) => item.id !== pendingDelete.id));
      setPendingDelete(null);
      setDeleteConfirmOpen(false);
      notifyDashboardChanged();
      onChanged?.();
    } catch (deleteError) {
      const message = deleteError instanceof ApiError
        ? deleteError.message
        : "Failed to delete report.";
      setError(message);
    } finally {
      setIsDeleting(false);
    }
  }, [onChanged, pendingDelete]);

  const columns = React.useMemo<TableColumnDefinition<ReportRow>[]>(() => [
    createTableColumn<ReportRow>({
      columnId: "name",
      compare: (a, b) => a.name.localeCompare(b.name),
      renderHeaderCell: () => <span style={nowrap}>Report Name</span>,
      renderCell: (item) => (
        <TableCellLayout media={typeIconMap[item.type]}>
          <span style={nowrap}>{item.name}</span>
        </TableCellLayout>
      ),
    }),
    createTableColumn<ReportRow>({
      columnId: "type",
      compare: (a, b) => a.type.localeCompare(b.type),
      renderHeaderCell: () => <span style={nowrap}>Type</span>,
      renderCell: (item) => <span style={nowrap}>{item.type}</span>,
    }),
    createTableColumn<ReportRow>({
      columnId: "size",
      compare: (a, b) => a.size.localeCompare(b.size),
      renderHeaderCell: () => <span style={nowrap}>Size</span>,
      renderCell: (item) => (
        <Badge color={sizeColour[item.size]} appearance="tint" style={{ whiteSpace: "nowrap" }}>
          {item.size}
        </Badge>
      ),
    }),
    createTableColumn<ReportRow>({
      columnId: "order",
      compare: (a, b) => a.order - b.order,
      renderHeaderCell: () => <span style={nowrap}>Order</span>,
      renderCell: (item) => <span style={nowrap}>{item.order}</span>,
    }),
    createTableColumn<ReportRow>({
      columnId: "placements",
      compare: (a, b) => a.placementCount - b.placementCount,
      renderHeaderCell: () => <span style={nowrap}>Placements</span>,
      renderCell: (item) => (
        <span style={nowrap} title={getPlacementSummary(item.source)}>
          {item.placementCount} {item.placementCount === 1 ? "section" : "sections"}
        </span>
      ),
    }),
    createTableColumn<ReportRow>({
      columnId: "visibleOnDashboard",
      compare: (a, b) => Number(b.visibleOnDashboard) - Number(a.visibleOnDashboard),
      renderHeaderCell: () => <span style={nowrap}>Dashboard Visible</span>,
      renderCell: (item) => (
        <Switch
          checked={isVisibleForDashboard(item.source, dashboardId)}
          disabled={updatingId === item.id}
          onChange={(_, data) => void handleVisibilityChange(item, data.checked)}
          label={isVisibleForDashboard(item.source, dashboardId) ? "Visible" : "Hidden"}
          style={{ whiteSpace: "nowrap" }}
        />
      ),
    }),
    createTableColumn<ReportRow>({
      columnId: "lastUpdated",
      compare: (a, b) => a.lastUpdated.localeCompare(b.lastUpdated),
      renderHeaderCell: () => <span style={nowrap}>Last Updated</span>,
      renderCell: (item) => <span style={nowrap}>{formatDate(item.lastUpdated)}</span>,
    }),
    createTableColumn<ReportRow>({
      columnId: "actions",
      renderHeaderCell: () => <span style={nowrap}>Actions</span>,
      renderCell: (item) => (
        <div className="flex gap-2">
          <ReportBuilderDialog
            report={item.source}
            onSaved={() => {
              void loadReports();
              onChanged?.();
            }}
            trigger={<Button size="small">Edit</Button>}
          />
          <Button
            size="small"
            appearance="secondary"
            icon={<DeleteRegular />}
            onClick={() => requestDelete(item)}
            disabled={isDeleting && pendingDelete?.id === item.id}
          >
            Delete
          </Button>
        </div>
      ),
    }),
  ], [dashboardId, handleVisibilityChange, isDeleting, loadReports, onChanged, pendingDelete?.id, requestDelete, updatingId]);

  const columnSizingOptions = {
    name: { minWidth: 200, defaultWidth: 240 },
    type: { minWidth: 90, defaultWidth: 110 },
    size: { minWidth: 100, defaultWidth: 110 },
    order: { minWidth: 70, defaultWidth: 80 },
    placements: { minWidth: 120, defaultWidth: 140 },
    visibleOnDashboard: { minWidth: 150, defaultWidth: 160 },
    lastUpdated: { minWidth: 130, defaultWidth: 140 },
    actions: { minWidth: 160, defaultWidth: 170 },
  };

  const { columnSizingOptions: persistedSizing, onColumnResize } = usePersistedColumnSizing(
    'reporting.reports',
    columns,
    columnSizingOptions,
  );

  if (loading) {
    return (
      <div className="flex h-[80vh] justify-center items-center gap-2 p-6">
        <Spinner size="small" />
        <Text>Loading reports...</Text>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6">
        <Text>{error}</Text>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="h-full w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
                <ChartMultipleRegular className='size-26 text-gray-300' />
                <Text>No reports yet. Create a dashboard section and add your first report.</Text>                
                <ReportBuilderDialog onSaved={onChanged} />
      </div>
    );
  }

  return (
    <>
      <div style={{ overflowX: "auto", width: "100%" }}>
        <DataGrid
        items={items}
        columns={columns}
        sortable
        resizableColumns
        columnSizingOptions={persistedSizing}
        onColumnResize={onColumnResize}
        selectionMode="multiselect"
        getRowId={(item) => item.id}
        focusMode="composite"
        size="medium"
        style={{ minWidth: "880px" }}
      >
        <DataGridHeader>
          <DataGridRow
            selectionCell={{
              checkboxIndicator: { "aria-label": "Select all rows" },
            }}
          >
            {({ renderHeaderCell }) => (
              <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
            )}
          </DataGridRow>
        </DataGridHeader>
        <DataGridBody<ReportRow>>
          {({ item, rowId }) => (
            <DataGridRow<ReportRow>
              key={rowId}
              selectionCell={{
                checkboxIndicator: { "aria-label": "Select row" },
              }}
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

      <ConfirmAction
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title="Delete report"
        message={`This will permanently remove "${pendingDelete?.name ?? "this report"}" from the dashboard`}
        actionName={isDeleting ? "Deleting..." : "Delete"}
        destructive
        onAction={() => void handleConfirmDelete()}
        onCancel={() => setPendingDelete(null)}
      />
    </>
  );
};
