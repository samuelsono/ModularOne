import { useCallback, useEffect, useMemo, useState } from 'react';
import type { TableColumnDefinition, TableColumnSizingOptions } from '@fluentui/react-components';
import {
  Button,
  Card,
  CardHeader,
  Dropdown,
  Field,
  MessageBar,
  MessageBarBody,
  Option,
  SpinButton,
  Spinner,
  Subtitle2,
  Text,
  Title3,
  createTableColumn,
} from '@fluentui/react-components';
import { ArrowDownloadRegular, CalendarRegular, ChevronDoubleLeftRegular, ChevronDoubleRightRegular, ChevronLeftRegular, ChevronRightRegular } from '@fluentui/react-icons';
import { usePageSearchQuery } from '../../context/PageSearchContext';
import { LeaveActionConfirmDialog } from '../../components/leave/LeaveActionConfirmDialog';
import { LeaveBulkActionBar } from '../../components/leave/LeaveBulkActionBar';
import { LeaveRowActions } from '../../components/leave/LeaveRowActions';
import { LeaveSelectableDataGrid } from '../../components/leave/LeaveSelectableDataGrid';
import {
  createLeaveActionsColumn,
  isLeaveCurrentlyRunning,
  leaveApprovalColumns,
  leaveHistoryColumns,
  sortLeaveHistoryItems,
} from '../../components/leave/leaveTableUtils';
import AppTitle from '../../components/common/AppTitle';
import { UpcomingHolidaysList } from '../../components/leave/UpcomingHolidaysList';
import { useLeaveActions } from '../../hooks/useLeaveActions';
import { usePermissions } from '../../hooks/usePermissions';
import { ApiError } from '../../services/apiClient';
import {
  downloadLeaveHistoryCsv,
  getLeaveHistory,
  getLeaveLiability,
  getLeavePendingReport,
  getLeaveReportSummary,
} from '../../services/leaveService';
import type {
  LeaveHistoryRow,
  LeaveLiabilityRow,
  LeaveReportSummary,
  LeaveRequest,
} from '../../types/leave';
import { LEAVE_STATUS_FILTERS } from '../../types/leave';
import FieldLabelInfo from '../../components/common/FieldLabelInfo';
import { InfoDrawer } from '../../components/common/InfoDrawer';
import {
  filterLeaveHistoryRows,
  filterLeaveLiabilityRows,
  filterLeaveRequests,
} from '../../utils/pageSearch';
import SummaryCard from '../../components/reportCards/SummaryCard';

function getLiabilityRowId(item: LeaveLiabilityRow): string {
  return `${item.userId}-${item.leaveTypeName}`;
}

const LIABILITY_PAGE_SIZES = [5, 10, 50] as const;
type LiabilityPageSize = (typeof LIABILITY_PAGE_SIZES)[number];

export default function LeaveReportsPage() {
  const searchQuery = usePageSearchQuery();
  const { user, hasPermission } = usePermissions();
  const leavePermissions = useMemo(() => ({
    canWriteRequests: hasPermission('leave.requests.write'),
    canWriteApprovals: hasPermission('leave.approvals.write'),
  }), [hasPermission]);

  const currentYear = new Date().getFullYear();
  const [year, setYear] = useState(currentYear);
  const [statusFilter, setStatusFilter] = useState('All');
  const [departmentFilter, setDepartmentFilter] = useState('');
  const [summary, setSummary] = useState<LeaveReportSummary | null>(null);
  const [history, setHistory] = useState<LeaveHistoryRow[]>([]);
  const [liability, setLiability] = useState<LeaveLiabilityRow[]>([]);
  const [pending, setPending] = useState<LeaveRequest[]>([]);
  const [selectedHistoryIds, setSelectedHistoryIds] = useState<string[]>([]);
  const [selectedLiabilityIds, setSelectedLiabilityIds] = useState<string[]>([]);
  const [liabilityTypeFilter, setLiabilityTypeFilter] = useState('');
  const [liabilityPage, setLiabilityPage] = useState(1);
  const [liabilityPageSize, setLiabilityPageSize] = useState<LiabilityPageSize>(10);
  const [selectedPendingIds, setSelectedPendingIds] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isExporting, setIsExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);


  const leaveTableColumnSizing: TableColumnSizingOptions = {
    department: { minWidth: 320, idealWidth: 400, defaultWidth: 360 },
    dates: { minWidth: 200, idealWidth: 250, defaultWidth: 250 },
    status: { minWidth: 200, idealWidth: 250, defaultWidth: 250 },
    workingDays: { minWidth: 60, idealWidth: 80, defaultWidth: 80 },
  };

  const loadReports = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [summaryData, historyData, liabilityData, pendingData] = await Promise.all([
        getLeaveReportSummary({
          year,
          department: departmentFilter || undefined,
        }),
        getLeaveHistory({
          year,
          status: statusFilter,
          department: departmentFilter || undefined,
        }),
        getLeaveLiability(),
        getLeavePendingReport(),
      ]);

      setSummary(summaryData);
      setHistory(historyData);
      setLiability(liabilityData);
      setPending(pendingData);
      setSelectedHistoryIds((current) => current.filter((id) => historyData.some((row) => row.id === id)));
      setSelectedLiabilityIds((current) => current.filter((id) => liabilityData.some((row) => getLiabilityRowId(row) === id)));
      setSelectedPendingIds((current) => current.filter((id) => pendingData.some((row) => row.id === id)));
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load leave reports.');
      setSummary(null);
      setHistory([]);
      setLiability([]);
      setPending([]);
      setSelectedHistoryIds([]);
      setSelectedLiabilityIds([]);
      setSelectedPendingIds([]);
    } finally {
      setIsLoading(false);
    }
  }, [departmentFilter, statusFilter, year]);

  const {
    actingId,
    actionError,
    pendingAction,
    requestAction,
    confirmPendingAction,
    dismissPendingAction,
    isWorking,
  } = useLeaveActions(async () => {
    await loadReports();
    setSelectedHistoryIds([]);
    setSelectedLiabilityIds([]);
    setSelectedPendingIds([]);
  });

  useEffect(() => {
    void loadReports();
  }, [loadReports]);

  const departmentOptions = useMemo(
    () => summary?.byDepartment.map((item) => item.label) ?? [],
    [summary?.byDepartment],
  );

  const selectedHistory = useMemo(
    () => history.filter((row) => selectedHistoryIds.includes(row.id)),
    [history, selectedHistoryIds],
  );

  const selectedPending = useMemo(
    () => pending.filter((row) => selectedPendingIds.includes(row.id)),
    [pending, selectedPendingIds],
  );

  const filteredHistory = useMemo(
    () => sortLeaveHistoryItems(filterLeaveHistoryRows(history, searchQuery)),
    [history, searchQuery],
  );

  const runningHistory = useMemo(
    () => filteredHistory.filter((row) => isLeaveCurrentlyRunning(
      row.status,
      row.startDate,
      row.endDate,
    )),
    [filteredHistory],
  );

  const filteredPending = useMemo(
    () => filterLeaveRequests(pending, searchQuery),
    [pending, searchQuery],
  );

  const showLeaveActions = leavePermissions.canWriteRequests || leavePermissions.canWriteApprovals;

  const historyColumns = useMemo(() => {
    const columns: TableColumnDefinition<LeaveHistoryRow>[] = [...leaveHistoryColumns];
    if (showLeaveActions) {
      columns.push(createLeaveActionsColumn((item) => (
        <LeaveRowActions
          item={item}
          user={user}
          permissions={leavePermissions}
          actingId={actingId}
          disabled={isWorking}
          onAction={(kind, id) => requestAction(kind, [id])}
        />
      )));
    }
    return columns;
  }, [actingId, isWorking, leavePermissions, requestAction, showLeaveActions, user]);

  const pendingColumns = useMemo(() => {
    const columns: TableColumnDefinition<LeaveRequest>[] = [...leaveApprovalColumns];
    if (showLeaveActions) {
      columns.push(createLeaveActionsColumn((item) => (
        <LeaveRowActions
          item={item}
          user={user}
          permissions={leavePermissions}
          actingId={actingId}
          disabled={isWorking}
          onAction={(kind, id) => requestAction(kind, [id])}
        />
      )));
    }
    return columns;
  }, [actingId, isWorking, leavePermissions, requestAction, showLeaveActions, user]);

  const liabilityColumns: TableColumnDefinition<LeaveLiabilityRow>[] = useMemo(
    () => [
      createTableColumn<LeaveLiabilityRow>({
        columnId: 'employee',
        renderHeaderCell: () => 'Employee',
        renderCell: (item) => item.displayName,
      }),
      createTableColumn<LeaveLiabilityRow>({
        columnId: 'department',
        renderHeaderCell: () => 'Department',
        renderCell: (item) => item.department ?? '—',
      }),
      createTableColumn<LeaveLiabilityRow>({
        columnId: 'leaveType',
        renderHeaderCell: () => 'Type',
        renderCell: (item) => (
          <span className="inline-flex items-center gap-2">
            {item.leaveTypeColor ? (
              <span
                className="inline-block w-3 h-3 rounded-full shrink-0"
                style={{ backgroundColor: item.leaveTypeColor }}
              />
            ) : null}
            {item.leaveTypeName}
          </span>
        ),
      }),
      createTableColumn<LeaveLiabilityRow>({
        columnId: 'remaining',
        renderHeaderCell: () => 'Remaining',
        renderCell: (item) => item.remaining.toFixed(1),
      }),
      createTableColumn<LeaveLiabilityRow>({
        columnId: 'used',
        renderHeaderCell: () => 'Used',
        renderCell: (item) => item.used.toFixed(1),
      }),
      createTableColumn<LeaveLiabilityRow>({
        columnId: 'pending',
        renderHeaderCell: () => 'Pending',
        renderCell: (item) => item.pending.toFixed(1),
      }),
    ],
    [],
  );

  const liabilityTypeOptions = useMemo(
    () => [...new Set(liability.map((row) => row.leaveTypeName))].sort((a, b) => a.localeCompare(b)),
    [liability],
  );

  const filteredLiability = useMemo(
    () => filterLeaveLiabilityRows(liability, searchQuery).filter((row) => {
      if (liabilityTypeFilter && row.leaveTypeName !== liabilityTypeFilter) {
        return false;
      }

      return true;
    }),
    [liability, liabilityTypeFilter, searchQuery],
  );

  const liabilityTotalPages = Math.max(1, Math.ceil(filteredLiability.length / liabilityPageSize));

  const paginatedLiability = useMemo(() => {
    const startIndex = (liabilityPage - 1) * liabilityPageSize;
    return filteredLiability.slice(startIndex, startIndex + liabilityPageSize);
  }, [filteredLiability, liabilityPage, liabilityPageSize]);

  const liabilityRangeStart = filteredLiability.length === 0
    ? 0
    : (liabilityPage - 1) * liabilityPageSize + 1;
  const liabilityRangeEnd = Math.min(liabilityPage * liabilityPageSize, filteredLiability.length);

  useEffect(() => {
    setLiabilityPage(1);
  }, [searchQuery, liabilityTypeFilter, liabilityPageSize]);

  useEffect(() => {
    if (liabilityPage > liabilityTotalPages) {
      setLiabilityPage(liabilityTotalPages);
    }
  }, [liabilityPage, liabilityTotalPages]);

  async function handleExportCsv() {
    setIsExporting(true);
    setError(null);

    try {
      await downloadLeaveHistoryCsv({
        year,
        status: statusFilter,
        department: departmentFilter || undefined,
      });
    } catch (exportError) {
      setError(exportError instanceof Error ? exportError.message : 'Failed to export leave history.');
    } finally {
      setIsExporting(false);
    }
  }

  const getLeaveItemColor = useCallback((item: { label: string, leaveType: string, requesterUserId: string } | null | undefined | any) => {
    if (!item) return 'default';

     let foundItem = liability.find((row) => row.leaveTypeName === item.label);
     return foundItem ? foundItem.leaveTypeColor : 'default';

  }, [liability])

  return (
    <div className="flex flex-col gap-6 h-full overflow-auto px-0 pb-20">
      <div className="flex flex-wrap items-end justify-between gap-4 w-full mx-auto px-6">
        <AppTitle title="Leave dashboard" subtitle="Operational summary, team leave history, balance liability, and pending approvals." />
        <div className='lg:hidden'>
          <FieldLabelInfo text="Leave dashboard" info="The butto shows the upcoming holidays for the next 5 years.">
              <InfoDrawer trigger={<Button icon={<CalendarRegular fontSize={28} />} appearance="subtle" />}>
                  <UpcomingHolidaysList />
              </InfoDrawer>
          </FieldLabelInfo>
        </div>

        <div className="flex justify-end items-end flex-wrap gap-2">
          <Field label="Year">
            <SpinButton className="w-[120px]" value={year} onChange={(_, data) => setYear(data.value)} />
          </Field>

          <Field label="Status">
            <Dropdown
              value={statusFilter}
              style={{ minWidth: 160, width: 160 }}
              selectedOptions={[statusFilter]}
              onOptionSelect={(_, data) => {
                if (data.optionValue) {
                  setStatusFilter(data.optionValue);
                }
              }}
            >
              {LEAVE_STATUS_FILTERS.map((status) => (
                <Option key={status} value={status}>{status}</Option>
              ))}
            </Dropdown>
          </Field>

          <Field label="Department">
            <Dropdown
              value={departmentFilter || 'All departments'}
              className="w-[180px]"
              style={{ minWidth: 180 }}
              selectedOptions={[departmentFilter || '']}
              onOptionSelect={(_, data) => setDepartmentFilter(data.optionValue ?? '')}
            >
              <Option value="">All departments</Option>
              {departmentOptions.map((department) => (
                <Option key={department} value={department}>{department}</Option>
              ))}
            </Dropdown>
          </Field>

          <Button
            appearance="primary"
            icon={<ArrowDownloadRegular />}
            disabled={isExporting}
            onClick={() => void handleExportCsv()}
          >
            {isExporting ? 'Exporting...' : 'Export CSV'}
          </Button>
        </div>
      </div>

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {actionError ? (
        <MessageBar intent="error">
          <MessageBarBody>{actionError}</MessageBarBody>
        </MessageBar>
      ) : null}


      {isLoading ? (
        <Spinner label="Loading reports..." />
      ) : (
        <div className='flex flex-col max-h-[80vh] overflow-y-scroll pb-32'>
         <div className='flex flex-col gap-4 px-4'>

          {summary ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 w-full max-w-6xl mx-auto mt-10">
              <SummaryCard label="Pending approvals" value={summary.pendingCount} />
              <SummaryCard label="On leave today" value={summary.onLeaveTodayCount} />
              <SummaryCard label="Annual leave remaining" value={summary.remainingAnnualDays.toFixed(1)}  />
              <SummaryCard label="Sick leave remaining" value={summary.remainingSickDays.toFixed(1)} other={{ label: "Other Leave", value: summary.remainingOtherDays.toFixed(1) }} />
            </div>
          ) : null}

         

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 w-full max-w-6xl mx-auto mb-10">
            {summary ? (
              <>
                <Card className="p-4">
                  <Subtitle2 className="mb-3">By leave type</Subtitle2>
                  <div className="flex flex-col gap-2">
                    {summary.byType.map((item) => (
                      <div key={item.label} className="flex justify-between text-sm">
                        <span>
                          
                          <span
                            className="inline-block w-3 h-3 rounded-full shrink-0 mr-2"
                            style={{ backgroundColor: getLeaveItemColor(item) ?? "default" }}
                          />
                        
                          {item.label}</span>
                        <span>{item.count}</span>
                      </div>
                    ))}
                    {summary.byType.length === 0 ? (
                      <Text className="text-sm text-neutral-foreground-3">No leave type data</Text>
                    ) : null}
                  </div>
                </Card>
                <Card className="p-4">
                  <Subtitle2 className="mb-3">By status</Subtitle2>
                  <div className="flex flex-col gap-2">
                    {summary.byStatus.map((item) => (
                      <div key={item.label} className="flex justify-between text-sm">
                        <span>{item.label}</span>
                        <span>{item.count}</span>
                      </div>
                    ))}
                    {summary.byStatus.length === 0 ? (
                      <Text className="text-sm text-neutral-foreground-3">No status data</Text>
                    ) : null}
                  </div>
                </Card>
                <Card className="p-4">
                  <Subtitle2 className="mb-3">By department</Subtitle2>
                  <div className="flex flex-col gap-2">
                    {summary.byDepartment.length === 0 ? (
                      <Text className="text-sm text-neutral-foreground-3">No department data</Text>
                    ) : summary.byDepartment.map((item) => (
                      <div key={item.label} className="flex justify-between text-sm">
                        <span>{item.label}</span>
                        <span>{item.count}</span>
                      </div>
                    ))}
                  </div>
                </Card>
              </>
            ) : null}
          </div>

          </div>

          


          <div className='flex flex-col w-full max-w-8xl mx-auto gap-5'>

          <div className="flex flex-col gap-3 max-w-8xl mx-auto w-full mb-3">
            <Subtitle2 className='px-6'>Leave history</Subtitle2>
            {runningHistory.length > 0 ? (
              <MessageBar intent="success" className="mx-6">
                <MessageBarBody>
                  {runningHistory.length === 1
                    ? `${runningHistory[0].requesterDisplayName} is currently on leave (${runningHistory[0].leaveType}).`
                    : `${runningHistory.length} employees are currently on approved leave.`}
                </MessageBarBody>
              </MessageBar>
            ) : null}
            <LeaveBulkActionBar
              selectedItems={selectedHistory}
              user={user}
              permissions={leavePermissions}
              disabled={isWorking}
              onBulkAction={requestAction}
              
            />
            <LeaveSelectableDataGrid
              items={filteredHistory}
              columns={historyColumns}
              selectedIds={selectedHistoryIds}
              onSelectionChange={setSelectedHistoryIds}
              getRowId={(item) => item.id}
              columnSizingOptions={leaveTableColumnSizing}
            />
          </div>

          <div className="flex flex-col gap-3 max-w-8xl mx-auto w-full mb-3">
            <Subtitle2 className='px-6'>Pending approvals</Subtitle2>
            <LeaveBulkActionBar
              selectedItems={selectedPending}
              user={user}
              permissions={leavePermissions}
              disabled={isWorking}
              onBulkAction={requestAction}
            />
            <LeaveSelectableDataGrid
              items={filteredPending}
              columns={pendingColumns}
              selectedIds={selectedPendingIds}
              onSelectionChange={setSelectedPendingIds}
              getRowId={(item) => item.id}
            />
          </div>

          <div className="flex flex-col gap-3 max-w-8xl mx-auto w-full mb-3">
            <Subtitle2 className='px-6'>Balance liability</Subtitle2>

            <div className="flex flex-wrap items-end gap-3 px-6">
              <Field label="Leave type">
                <Dropdown
                  className="w-[180px]"
                  value={liabilityTypeFilter || 'All leave types'}
                  selectedOptions={[liabilityTypeFilter || '']}
                  onOptionSelect={(_, data) => setLiabilityTypeFilter(data.optionValue ?? '')}
                >
                  <Option value="">All leave types</Option>
                  {liabilityTypeOptions.map((leaveType) => (
                    <Option key={leaveType} value={leaveType}>{leaveType}</Option>
                  ))}
                </Dropdown>
              </Field>
            </div>

            <LeaveSelectableDataGrid
              items={paginatedLiability}
              columns={liabilityColumns}
              selectedIds={selectedLiabilityIds}
              onSelectionChange={setSelectedLiabilityIds}
              getRowId={getLiabilityRowId}
              enableColumnSizing={false}
            />

            {filteredLiability.length > 0 ? (
              <div className="flex flex-wrap items-center justify-between gap-3 px-6">
                <div className="flex items-center gap-2">
                
                

                  <Text className="text-sm text-neutral-foreground-3">Rows per page</Text>
                  <Dropdown
                    style={{ width: 72, minWidth: 72 }}
                    className="shrink-0"
                    value={String(liabilityPageSize)}
                    selectedOptions={[String(liabilityPageSize)]}
                    onOptionSelect={(_, data) => {
                      const nextSize = Number(data.optionValue);
                      if (LIABILITY_PAGE_SIZES.includes(nextSize as LiabilityPageSize)) {
                        setLiabilityPageSize(nextSize as LiabilityPageSize);
                      }
                    }}
                  >
                    {LIABILITY_PAGE_SIZES.map((size) => (
                      <Option key={size} value={String(size)}>{size}</Option>
                    ))}
                  </Dropdown>

                  <Text className="text-sm text-neutral-foreground-3">
                  Showing {liabilityRangeStart}–{liabilityRangeEnd} of {filteredLiability.length}
                </Text>
                </div>

                 <div className="flex items-center gap-1">
                  <Button
                    appearance="outline"
                    icon={<ChevronDoubleLeftRegular />}
                    aria-label="First page"
                    disabled={liabilityPage <= 1}
                    onClick={() => setLiabilityPage(1)}
                  />
                  <Button
                    appearance="outline"
                    icon={<ChevronLeftRegular />}
                    aria-label="Previous page"
                    disabled={liabilityPage <= 1}
                    onClick={() => setLiabilityPage((page) => Math.max(1, page - 1))}
                  />
                  <Text className="text-sm text-neutral-foreground-3 px-2">
                    Page {liabilityPage} of {liabilityTotalPages}
                  </Text>
                  <Button
                    appearance="outline"
                    icon={<ChevronRightRegular />}
                    aria-label="Next page"
                    disabled={liabilityPage >= liabilityTotalPages}
                    onClick={() => setLiabilityPage((page) => Math.min(liabilityTotalPages, page + 1))}
                  />
                  <Button
                    appearance="outline"
                    icon={<ChevronDoubleRightRegular />}
                    aria-label="Last page"
                    disabled={liabilityPage >= liabilityTotalPages}
                    onClick={() => setLiabilityPage(liabilityTotalPages)}
                  />
                </div>
              </div>
            ) : (
              <Text className="px-6 text-sm text-neutral-foreground-3">
                No balance liability rows match the current filters.
              </Text>
            )}
          </div>

          

          </div>





        </div>
      )}

      <LeaveActionConfirmDialog
        pending={pendingAction}
        isWorking={isWorking}
        onConfirm={(reason) => void confirmPendingAction(reason)}
        onCancel={dismissPendingAction}
      />
    </div>
  );
}
