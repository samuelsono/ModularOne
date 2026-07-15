import * as React from 'react';
import type { JSXElement } from '@fluentui/react-components';
import {
  Button,
  Combobox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Checkbox,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Switch,
  Textarea,
  makeStyles,
} from '@fluentui/react-components';
import { DocumentBulletListRegular, PlayRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { getDashboard, getDashboards } from '@modules/reporting/services/dashboardService';
import { getSelectedDashboardId, notifyDashboardChanged } from '@modules/reporting/utils/dashboardStorage';
import { createReport, getReportMetadata, previewReport, updateReport } from '@modules/reporting/services/reportService';
import type {
  Report,
  ReportFilter,
  ReportMetadata,
  ReportSize,
  ReportType,
  SaveReportRequest,
} from '@modules/reporting/types/report';
import type { DashboardSection, DashboardSummary } from '@modules/reporting/types/dashboard';
import { buildSectionTree, flattenSectionTree, getSectionLabel } from '@modules/reporting/utils/sectionTree';
import {
  API_RESOURCE_LABELS,
  DEFAULT_COLUMN_MAPPINGS,
  apiResourceRequiresRegistration,
  apiResourceRequiresDateRange,
  buildApiTableChartOptionsJson,
  formatColumnMappingJson,
  parseApiTableChartOptionsJson,
  parseColumnMappingFromJson,
} from '@modules/reporting/utils/apiTableConfig';
import { ReportPreview } from './ReportPreview';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    rowGap: '16px',
    paddingTop: '16px',
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    columnGap: '24px',
    rowGap: '12px',
  },
  preview: {
    borderTop: `1px solid #e3e5e7`,
    paddingTop: '16px',
  },
});

interface ReportBuilderDialogProps {
  report?: Report;
  onSaved?: () => void;
  trigger?: React.ReactElement;
}

const REPORT_TYPE_LABELS: Record<string, string> = {
  MetricCard: 'Metric Card',
  Column: 'Column Chart',
  Pie: 'Pie Chart',
  Donut: 'Donut Chart',
  Line: 'Line Chart',
  ApiTable: 'API Table',
  Map: 'Map Report',
};

export function ReportBuilderDialog({ report, onSaved, trigger }: ReportBuilderDialogProps): JSXElement {
  const styles = useStyles();
  const isEdit = Boolean(report);
  const [open, setOpen] = React.useState(false);
  const [loading, setLoading] = React.useState(false);
  const [saving, setSaving] = React.useState(false);
  const [previewing, setPreviewing] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [metadata, setMetadata] = React.useState<ReportMetadata | null>(null);
  const [allSections, setAllSections] = React.useState<DashboardSection[]>([]);
  const [dashboards, setDashboards] = React.useState<DashboardSummary[]>([]);
  const [selectedSectionIds, setSelectedSectionIds] = React.useState<string[]>([]);
  const [placementSizes, setPlacementSizes] = React.useState<Record<string, ReportSize>>({});
  const [previewData, setPreviewData] = React.useState<import('../../types/report').ReportExecutionResult | null>(null);
  const [previewError, setPreviewError] = React.useState<string | null>(null);

  const [name, setName] = React.useState(report?.name ?? '');
  const [description, setDescription] = React.useState(report?.description ?? '');
  const [reportType, setReportType] = React.useState<ReportType>(report?.reportType ?? 'Column');
  const [size, setSize] = React.useState<ReportSize>(report?.size ?? 'Medium');
  const [sortOrder, setSortOrder] = React.useState(String(report?.sortOrder ?? 1));
  const [isVisible, setIsVisible] = React.useState(report?.isVisible ?? true);
  const [targetTable, setTargetTable] = React.useState(report?.targetTable ?? 'Vehicles');
  const [aggregateFunction, setAggregateFunction] = React.useState(report?.aggregateFunction ?? 'Count');
  const [aggregateField, setAggregateField] = React.useState(report?.aggregateField ?? '');
  const [groupBy1, setGroupBy1] = React.useState(report?.groupByColumns[0] ?? '');
  const [groupBy2, setGroupBy2] = React.useState(report?.groupByColumns[1] ?? '');
  const [groupBy3, setGroupBy3] = React.useState(report?.groupByColumns[2] ?? '');
  const [comparisonEnabled, setComparisonEnabled] = React.useState(report?.comparisonEnabled ?? false);
  const [filterField, setFilterField] = React.useState(report?.filters[0]?.field ?? '');
  const [filterOperator, setFilterOperator] = React.useState(report?.filters[0]?.operator ?? 'equals');
  const [filterValue, setFilterValue] = React.useState(report?.filters[0]?.value ?? '');
  const [apiColumnMappingJson, setApiColumnMappingJson] = React.useState(() => {
    const options = parseApiTableChartOptionsJson(report?.chartOptionsJson);
    const resource = report?.targetTable ?? 'vehicles';
    const mapping = Object.keys(options.columnMapping).length > 0
      ? options.columnMapping
      : (DEFAULT_COLUMN_MAPPINGS[resource] ?? {});
    return formatColumnMappingJson(mapping);
  });
  const [apiRegistration, setApiRegistration] = React.useState(
    () => parseApiTableChartOptionsJson(report?.chartOptionsJson).registration ?? '',
  );
  const [apiStartTimestamp, setApiStartTimestamp] = React.useState(
    () => parseApiTableChartOptionsJson(report?.chartOptionsJson).startTimestamp ?? '',
  );
  const [apiEndTimestamp, setApiEndTimestamp] = React.useState(
    () => parseApiTableChartOptionsJson(report?.chartOptionsJson).endTimestamp ?? '',
  );

  const isApiTable = reportType === 'ApiTable';
  const isMapReport = reportType === 'Map';

  const sectionOptions = React.useMemo(
    () => flattenSectionTree(buildSectionTree(allSections)),
    [allSections],
  );

  const sectionsByDashboard = React.useMemo(() => {
    const grouped = new Map<string, typeof sectionOptions>();

    for (const dashboard of dashboards) {
      grouped.set(
        dashboard.id,
        sectionOptions.filter((section) => section.dashboardId === dashboard.id),
      );
    }

    return grouped;
  }, [dashboards, sectionOptions]);

  const toggleSectionSelection = (id: string, checked: boolean) => {
    setSelectedSectionIds((current) => {
      if (checked) {
        if (current.includes(id)) {
          return current;
        }

        const existing = report?.placements?.find((placement) => placement.sectionId === id);
        setPlacementSizes((sizes) => ({
          ...sizes,
          [id]: sizes[id] ?? existing?.size ?? size,
        }));
        return [...current, id];
      }

      return current.filter((sectionId) => sectionId !== id);
    });
  };

  const setPlacementSize = (sectionId: string, nextSize: ReportSize) => {
    setPlacementSizes((current) => ({ ...current, [sectionId]: nextSize }));
  };

  const maxGroupBy = metadata?.reportTypeRules.find((rule) => rule.name === reportType)?.maxGroupByColumns ?? 0;
  const tableMeta = metadata?.tables.find((table) => table.name === targetTable);
  const groupableColumns = tableMeta?.columns.filter((column) => column.isGroupable) ?? [];
  const aggregatableColumns = tableMeta?.columns.filter((column) => column.isAggregatable) ?? [];
  const allowedAggregates = tableMeta?.allowedAggregates ?? [];
  const needsAggregateField = aggregateFunction !== 'Count';

  React.useEffect(() => {
    if (!open) {
      return;
    }

    if (report) {
      setName(report.name);
      setDescription(report.description ?? '');
      setSelectedSectionIds(report.placements?.map((placement) => placement.sectionId) ?? []);
      setPlacementSizes(Object.fromEntries(
        (report.placements ?? []).map((placement) => [placement.sectionId, placement.size]),
      ));
      setReportType(report.reportType);
      setSize(report.size);
      setSortOrder(String(report.sortOrder));
      setIsVisible(report.isVisible);
      setTargetTable(report.targetTable);
      setAggregateFunction(report.aggregateFunction);
      setAggregateField(report.aggregateField ?? '');
      setGroupBy1(report.groupByColumns[0] ?? '');
      setGroupBy2(report.groupByColumns[1] ?? '');
      setGroupBy3(report.groupByColumns[2] ?? '');
      setComparisonEnabled(report.comparisonEnabled);
      setFilterField(report.filters[0]?.field ?? '');
      setFilterOperator(report.filters[0]?.operator ?? 'equals');
      setFilterValue(report.filters[0]?.value ?? '');
      const apiOptions = parseApiTableChartOptionsJson(report.chartOptionsJson);
      const mapping = Object.keys(apiOptions.columnMapping).length > 0
        ? apiOptions.columnMapping
        : (DEFAULT_COLUMN_MAPPINGS[report.targetTable] ?? {});
      setApiColumnMappingJson(formatColumnMappingJson(mapping));
      setApiRegistration(apiOptions.registration ?? '');
      setApiStartTimestamp(apiOptions.startTimestamp ?? '');
      setApiEndTimestamp(apiOptions.endTimestamp ?? '');
    } else {
      setSelectedSectionIds([]);
      setPlacementSizes({});
    }

    let cancelled = false;

    const load = async () => {
      setLoading(true);
      setError(null);

      try {
        const [meta, dashboardsResponse] = await Promise.all([
          getReportMetadata(),
          getDashboards(),
        ]);

        if (cancelled) {
          return;
        }

        setMetadata(meta);
        setDashboards(dashboardsResponse.items);

        const dashboardDetails = await Promise.all(
          dashboardsResponse.items.map((dashboard) => getDashboard(dashboard.id)),
        );

        if (cancelled) {
          return;
        }

        setAllSections(dashboardDetails.flatMap((dashboard) => dashboard.sections));

        if (!report && selectedSectionIds.length === 0) {
          const initialDashboardId = getSelectedDashboardId()
            ?? dashboardsResponse.items.find((item) => item.isDefault)?.id
            ?? dashboardsResponse.items[0]?.id;

          const initialSection = dashboardDetails
            .find((dashboard) => dashboard.id === initialDashboardId)
            ?.sections[0];

          if (initialSection) {
            setSelectedSectionIds([initialSection.id]);
            setPlacementSizes({ [initialSection.id]: size });
          }
        }

        if (!report && !targetTable && meta.tables[0]) {
          setTargetTable(meta.tables[0].name);
        }
      } catch (loadError) {
        if (!cancelled) {
          setError(loadError instanceof ApiError ? loadError.message : 'Failed to load report builder.');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [open, report]);

  React.useEffect(() => {
    if (maxGroupBy < 1) {
      setGroupBy1('');
      setGroupBy2('');
      setGroupBy3('');
    } else if (maxGroupBy < 2) {
      setGroupBy2('');
      setGroupBy3('');
    } else if (maxGroupBy < 3) {
      setGroupBy3('');
    }
  }, [maxGroupBy, reportType]);

  React.useEffect(() => {
    if (!isApiTable) {
      return;
    }

    const resources = metadata?.apiResources ?? ['vehicles'];
    if (!resources.includes(targetTable)) {
      const nextResource = resources[0] ?? 'vehicles';
      setTargetTable(nextResource);
      setApiColumnMappingJson(formatColumnMappingJson(DEFAULT_COLUMN_MAPPINGS[nextResource] ?? {}));
    }
  }, [isApiTable, metadata?.apiResources, targetTable]);

  const buildApiChartOptionsJson = React.useCallback(() => {
    const columnMapping = parseColumnMappingFromJson(apiColumnMappingJson);
    return buildApiTableChartOptionsJson({
      columnMapping,
      registration: apiRegistration.trim() || undefined,
      startTimestamp: apiStartTimestamp.trim() || undefined,
      endTimestamp: apiEndTimestamp.trim() || undefined,
    });
  }, [apiColumnMappingJson, apiEndTimestamp, apiRegistration, apiStartTimestamp]);

  const buildRequest = React.useCallback((): SaveReportRequest => {
    const groupByColumns = [groupBy1, groupBy2, groupBy3].filter(Boolean);
    const filters: ReportFilter[] = filterField && filterValue
      ? [{ field: filterField, operator: filterOperator, value: filterValue }]
      : [];

    const defaultSortOrder = Number.parseInt(sortOrder, 10) || 1;
    const placements = selectedSectionIds.map((sectionIdValue, index) => {
      const existing = report?.placements?.find((placement) => placement.sectionId === sectionIdValue);

      return {
        sectionId: sectionIdValue,
        sortOrder: existing?.sortOrder ?? defaultSortOrder + index,
        size: placementSizes[sectionIdValue] ?? existing?.size ?? size,
        isVisible: existing?.isVisible ?? isVisible,
      };
    });

    return {
      name: name.trim(),
      description: description.trim() || null,
      reportType,
      size,
      sortOrder: defaultSortOrder,
      isVisible,
      targetTable: isMapReport ? 'Vehicles' : targetTable,
      aggregateFunction: isApiTable || isMapReport ? 'Count' : aggregateFunction,
      aggregateField: isApiTable || isMapReport ? null : (needsAggregateField ? aggregateField || null : null),
      groupByColumns: isApiTable || isMapReport ? [] : groupByColumns,
      filters: isApiTable || isMapReport ? [] : filters,
      comparisonEnabled: isMapReport ? false : reportType === 'MetricCard' ? comparisonEnabled : false,
      chartOptionsJson: isApiTable ? buildApiChartOptionsJson() : null,
      sectionId: selectedSectionIds[0] ?? null,
      placements,
    };
  }, [
    aggregateField,
    aggregateFunction,
    apiColumnMappingJson,
    apiEndTimestamp,
    apiRegistration,
    apiStartTimestamp,
    buildApiChartOptionsJson,
    comparisonEnabled,
    description,
    filterField,
    filterOperator,
    filterValue,
    groupBy1,
    groupBy2,
    groupBy3,
    isApiTable,
    isMapReport,
    isEdit,
    isVisible,
    name,
    needsAggregateField,
    report,
    reportType,
    placementSizes,
    selectedSectionIds,
    size,
    sortOrder,
    targetTable,
  ]);

  const handlePreview = async () => {
    setPreviewing(true);
    setPreviewError(null);

    try {
      if (isApiTable) {
        parseColumnMappingFromJson(apiColumnMappingJson);
      }

      const result = await previewReport(buildRequest());
      setPreviewData(result);
    } catch (previewErr) {
      setPreviewData(null);
      setPreviewError(previewErr instanceof ApiError ? previewErr.message : 'Preview failed.');
    } finally {
      setPreviewing(false);
    }
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError(null);

    try {
      if (isApiTable) {
        parseColumnMappingFromJson(apiColumnMappingJson);
      }

      const request = buildRequest();

      if (isEdit && report) {
        await updateReport(report.id, request);
      } else {
        await createReport(request);
      }

      setOpen(false);
      setPreviewData(null);
      notifyDashboardChanged();
      onSaved?.();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save report.');
    } finally {
      setSaving(false);
    }
  };

  const defaultTrigger = (
    <Button appearance="primary" icon={<DocumentBulletListRegular />}>
      {isEdit ? 'Edit Report' : 'New Report'}
    </Button>
  );

  return (
    <Dialog
      modalType="modal"
      open={open}
      onOpenChange={(_, data) => {
        setOpen(data.open);
        if (!data.open) {
          setPreviewData(null);
          setPreviewError(null);
          setError(null);
        }
      }}
    >
      <DialogTrigger disableButtonEnhancement>
        {trigger ?? defaultTrigger}
      </DialogTrigger>
      <DialogSurface aria-describedby={undefined} className="flex! flex-col! min-w-[760px]! max-w-[920px]!">
        <form onSubmit={(event) => void handleSubmit(event)}>
          <DialogBody>
            <DialogTitle>{isEdit ? 'Edit Report' : 'Create Report'}</DialogTitle>
            <DialogContent className={styles.content}>
              {error && (
                <MessageBar intent="error">
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              )}

              {loading ? (
                <div className="flex items-center gap-2 py-8">
                  <Spinner size="small" />
                  <span>Loading builder...</span>
                </div>
              ) : (
                <>
                  <div className={styles.grid}>
                    <Field label="Report Name" required className="col-span-2">
                      <Input
                        required
                        value={name}
                        onChange={(_, data) => setName(data.value)}
                        placeholder="e.g. Vehicles by Make"
                      />
                    </Field>

                    <Field label="Dashboard sections" className="col-span-2">
                      <div className="flex flex-col gap-3 rounded border border-[#e3e5e7] p-3 max-h-56 overflow-y-auto">
                        {isEdit && (
                          <span className="text-sm text-neutral-600">
                            Checked sections control where this report appears on dashboards. Add or remove sections, then save.
                          </span>
                        )}
                        {dashboards.map((dashboard) => {
                          const dashboardSections = sectionsByDashboard.get(dashboard.id) ?? [];
                          if (dashboardSections.length === 0) {
                            return null;
                          }

                          return (
                            <div key={dashboard.id} className="flex flex-col gap-2">
                              <span className="text-sm font-semibold">
                                {dashboard.name}{dashboard.isDefault ? ' (Default)' : ''}
                              </span>
                              {dashboardSections.map((section) => {
                                const isSelected = selectedSectionIds.includes(section.id);

                                return (
                                  <div key={section.id} className="flex flex-wrap items-center gap-2">
                                    <Checkbox
                                      label={`${getSectionLabel(section, section.treeDepth)} (level ${section.depth})`}
                                      checked={isSelected}
                                      onChange={(_, data) => toggleSectionSelection(section.id, Boolean(data.checked))}
                                    />
                                    {isSelected && (
                                      <Combobox
                                        className="min-w-[120px]"
                                        value={(placementSizes[section.id] ?? size) === 'FullWidth' ? 'Full Width' : (placementSizes[section.id] ?? size)}
                                        onOptionSelect={(_, data) => setPlacementSize(section.id, (data.optionValue as ReportSize) ?? size)}
                                      >
                                        {(metadata?.reportSizes ?? ['Small', 'Medium', 'Large', 'FullWidth']).map((item) => (
                                          <Option key={item} value={item} text={item === 'FullWidth' ? 'Full Width' : item}>
                                            {item === 'FullWidth' ? 'Full Width' : item}
                                          </Option>
                                        ))}
                                      </Combobox>
                                    )}
                                  </div>
                                );
                              })}
                            </div>
                          );
                        })}
                        {sectionOptions.length === 0 && (
                          <span className="text-sm text-neutral-600">No sections available. Create sections under Reports → Dashboards &amp; Sections.</span>
                        )}
                      </div>
                    </Field>

                    <Field label="Report Type" required>
                      <Combobox
                        value={REPORT_TYPE_LABELS[reportType]}
                        onOptionSelect={(_, data) => setReportType((data.optionValue as ReportType) ?? 'Column')}
                      >
                        {(metadata?.reportTypes ?? []).map((type) => (
                          <Option key={type} value={type} text={REPORT_TYPE_LABELS[type as ReportType] ?? type}>
                            {REPORT_TYPE_LABELS[type as ReportType] ?? type}
                          </Option>
                        ))}
                      </Combobox>
                    </Field>

                    <Field label="Default size" required>
                      <Combobox
                        value={size === 'FullWidth' ? 'Full Width' : size}
                        onOptionSelect={(_, data) => setSize((data.optionValue as ReportSize) ?? 'Medium')}
                      >
                        {(metadata?.reportSizes ?? []).map((item) => (
                          <Option key={item} value={item} text={item === 'FullWidth' ? 'Full Width' : item}>
                            {item === 'FullWidth' ? 'Full Width' : item}
                          </Option>
                        ))}
                      </Combobox>
                    </Field>

                    <Field label="Sort Order" required>
                      <Input
                        type="number"
                        min={1}
                        value={sortOrder}
                        onChange={(_, data) => setSortOrder(data.value)}
                      />
                    </Field>

                    <Field label="Visible on Dashboard">
                      <Switch checked={isVisible} onChange={(_, data) => setIsVisible(data.checked)} />
                    </Field>
                  </div>

                  <div className={styles.grid}>
                    {isApiTable ? (
                      <>

                        <Field label="API Resource" required>
                          <Combobox
                            value={API_RESOURCE_LABELS[targetTable] ?? targetTable}
                            onOptionSelect={(_, data) => {
                              const nextResource = data.optionValue ?? 'vehicles';
                              setTargetTable(nextResource);
                              setApiColumnMappingJson(
                                formatColumnMappingJson(DEFAULT_COLUMN_MAPPINGS[nextResource] ?? {}),
                              );
                            }}
                          >
                            {(metadata?.apiResources ?? ['vehicles']).map((resource) => (
                              <Option
                                key={resource}
                                value={resource}
                                text={API_RESOURCE_LABELS[resource] ?? resource}
                              >
                                {API_RESOURCE_LABELS[resource] ?? resource}
                              </Option>
                            ))}
                          </Combobox>
                        </Field>

                        {apiResourceRequiresRegistration(targetTable) && (
                          <Field label="Vehicle Registration" required>
                            <Input
                              value={apiRegistration}
                              onChange={(_, data) => setApiRegistration(data.value)}
                              placeholder="e.g. ABC123GP"
                            />
                          </Field>
                        )}

                        {apiResourceRequiresDateRange(targetTable) && (
                          <>
                            <Field label="Date From">
                              <Input
                                value={apiStartTimestamp}
                                onChange={(_, data) => setApiStartTimestamp(data.value)}
                                placeholder="Optional (defaults to 7 days ago)"
                              />
                            </Field>
                            <Field label="Date To">
                              <Input
                                value={apiEndTimestamp}
                                onChange={(_, data) => setApiEndTimestamp(data.value)}
                                placeholder="Optional (defaults to now)"
                              />
                            </Field>
                          </>
                        )}

                        {apiResourceRequiresRegistration(targetTable) && (
                          <>
                            <Field label="Start Timestamp">
                              <Input
                                value={apiStartTimestamp}
                                onChange={(_, data) => setApiStartTimestamp(data.value)}
                                placeholder="Optional ISO date (defaults to 7 days ago)"
                              />
                            </Field>
                            <Field label="End Timestamp">
                              <Input
                                value={apiEndTimestamp}
                                onChange={(_, data) => setApiEndTimestamp(data.value)}
                                placeholder="Optional ISO date (defaults to now)"
                              />
                            </Field>
                          </>
                        )}

                        <Field
                          label="Column Mapping (JSON)"
                          required
                          className="col-span-2"
                          hint='Maps API fields to column headers. Key order controls display order. Example: { "columnMapping": { "registration": "Registration", "manufacturer": "Make" } }'
                        >
                          <Textarea
                            value={apiColumnMappingJson}
                            onChange={(_, data) => setApiColumnMappingJson(data.value)}
                            rows={8}
                            resize="vertical"
                            className="font-mono text-sm"
                          />
                        </Field>

                        <Field label="Rows per page" className="col-span-2">
                          <Input value="6" readOnly />
                        </Field>
                      </>
                    ) : isMapReport ? (
                      <div className="col-span-2 rounded border border-blue-200 bg-blue-50 p-3 text-sm text-blue-900">
                        <strong>Map Report</strong> — displays all vehicles with known GPS coordinates as interactive POI markers on a Mapbox map.
                        Locations are sourced from the local Vehicles table and updated whenever a vehicle sync is run.
                        Markers are colour-coded: <span className="font-semibold text-green-700">green</span> = moving,{' '}
                        <span className="font-semibold text-amber-600">amber</span> = idling,{' '}
                        <span className="font-semibold text-slate-600">grey</span> = off / unknown.
                        Click any marker on the map to see the vehicle details popup.
                      </div>
                    ) : (
                      <>
                    <Field label="Target Table" required>
                      <Combobox
                        value={tableMeta?.label ?? targetTable}
                        onOptionSelect={(_, data) => {
                          setTargetTable(data.optionValue ?? 'Vehicles');
                          setGroupBy1('');
                          setGroupBy2('');
                          setGroupBy3('');
                          setAggregateField('');
                          setFilterField('');
                        }}
                      >
                        {(metadata?.tables ?? []).map((table) => (
                          <Option key={table.name} value={table.name} text={table.label}>
                            {table.label}
                          </Option>
                        ))}
                      </Combobox>
                    </Field>

                    <Field label="Aggregate" required>
                      <Combobox
                        value={aggregateFunction}
                        onOptionSelect={(_, data) => setAggregateFunction(data.optionValue ?? 'Count')}
                      >
                        {allowedAggregates.map((item) => (
                          <Option key={item} value={item} text={item}>
                            {item}
                          </Option>
                        ))}
                      </Combobox>
                    </Field>

                    {needsAggregateField && (
                      <Field label="Aggregate Field" required className="col-span-2">
                        <Combobox
                          value={aggregatableColumns.find((column) => column.name === aggregateField)?.label ?? aggregateField}
                          onOptionSelect={(_, data) => setAggregateField(data.optionValue ?? '')}
                          placeholder="Select a field"
                        >
                          {aggregatableColumns.map((column) => (
                            <Option key={column.name} value={column.name} text={column.label}>
                              {column.label}
                            </Option>
                          ))}
                        </Combobox>
                      </Field>
                    )}

                    {maxGroupBy >= 1 && (
                      <Field label="Group By (1)" required={maxGroupBy >= 1}>
                        <Combobox
                          value={groupableColumns.find((column) => column.name === groupBy1)?.label ?? groupBy1}
                          onOptionSelect={(_, data) => setGroupBy1(data.optionValue ?? '')}
                          placeholder="Select column"
                        >
                          {groupableColumns.map((column) => (
                            <Option key={column.name} value={column.name} text={column.label}>
                              {column.label}
                            </Option>
                          ))}
                        </Combobox>
                      </Field>
                    )}

                    {maxGroupBy >= 2 && (
                      <Field label="Group By (2)">
                        <Combobox
                          value={groupableColumns.find((column) => column.name === groupBy2)?.label ?? groupBy2}
                          onOptionSelect={(_, data) => setGroupBy2(data.optionValue ?? '')}
                          placeholder="Optional series"
                        >
                          <Option value="">None</Option>
                          {groupableColumns.map((column) => (
                            <Option key={column.name} value={column.name} text={column.label}>
                              {column.label}
                            </Option>
                          ))}
                        </Combobox>
                      </Field>
                    )}

                    {maxGroupBy >= 3 && (
                      <Field label="Group By (3)">
                        <Combobox
                          value={groupableColumns.find((column) => column.name === groupBy3)?.label ?? groupBy3}
                          onOptionSelect={(_, data) => setGroupBy3(data.optionValue ?? '')}
                          placeholder="Optional"
                        >
                          <Option value="">None</Option>
                          {groupableColumns.map((column) => (
                            <Option key={column.name} value={column.name} text={column.label}>
                              {column.label}
                            </Option>
                          ))}
                        </Combobox>
                      </Field>
                    )}
                      </>
                    )}
                  </div>

                  {!isApiTable && !isMapReport && (
                  <div className={styles.grid}>
                    <Field label="Filter Field">
                      <Combobox
                        value={tableMeta?.columns.find((column) => column.name === filterField)?.label ?? filterField}
                        onOptionSelect={(_, data) => setFilterField(data.optionValue ?? '')}
                        placeholder="Optional"
                      >
                        <Option value="">None</Option>
                        {(tableMeta?.columns ?? []).map((column) => (
                          <Option key={column.name} value={column.name} text={column.label}>
                            {column.label}
                          </Option>
                        ))}
                      </Combobox>
                    </Field>

                    <Field label="Filter Operator">
                      <Combobox
                        value={filterOperator}
                        onOptionSelect={(_, data) => setFilterOperator(data.optionValue ?? 'equals')}
                      >
                        <Option value="equals">Equals</Option>
                        <Option value="notEquals">Not equals</Option>
                        <Option value="contains">Contains</Option>
                      </Combobox>
                    </Field>

                    <Field label="Filter Value" className="col-span-2">
                      <Input
                        value={filterValue}
                        onChange={(_, data) => setFilterValue(data.value)}
                        placeholder="e.g. Diesel"
                      />
                    </Field>
                  </div>
                  )}

                  {reportType === 'MetricCard' && (
                    <Switch
                      label="Compare vs previous month"
                      checked={comparisonEnabled}
                      onChange={(_, data) => setComparisonEnabled(data.checked)}
                    />
                  )}

                  <Field label="Description">
                    <Textarea
                      value={description}
                      onChange={(_, data) => setDescription(data.value)}
                      resize="vertical"
                      placeholder="Optional description shown on the dashboard"
                    />
                  </Field>

                  <div className={styles.preview}>
                    <div className="flex items-center justify-between mb-3">
                      <strong>Preview</strong>
                      <Button
                        type="button"
                        appearance="secondary"
                        icon={<PlayRegular />}
                        disabled={previewing || !name.trim()}
                        onClick={() => void handlePreview()}
                      >
                        {previewing ? 'Previewing...' : 'Run Preview'}
                      </Button>
                    </div>
                    <ReportPreview
                      name={name || 'Report Preview'}
                      reportType={reportType}
                      data={previewData}
                      loading={previewing}
                      error={previewError}
                    />
                  </div>
                </>
              )}
            </DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary" type="button">Cancel</Button>
              </DialogTrigger>
              <Button type="submit" appearance="primary" disabled={saving || loading || !name.trim()}>
                {saving ? 'Saving...' : isEdit ? 'Save Changes' : 'Create Report'}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  );
}
