import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';

import type { TableColumnDefinition, TableColumnSizingOptions } from '@fluentui/react-components';

import {
  Body1,
  Button,
  Card,
  CardHeader,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Subtitle2,
  Text,
  Title3,
} from '@fluentui/react-components';

import { ApiError } from '../../services/apiClient';

import { adjustLeaveBalance, getMyLeaveBalances, getMyLeaveRequests } from '../../services/leaveService';

import type { LeaveBalance, LeaveRequest } from '../../types/leave';
import { LEAVE_BALANCE_FILTERS } from '../../types/leave';

import { usePermissions } from '../../hooks/usePermissions';

import AppTitle from '../../components/common/AppTitle';

import { usePageSearchQuery } from '../../context/PageSearchContext';

import { filterLeaveBalances, filterLeaveRequests } from '../../utils/pageSearch';

import { AutoFitDataGrid } from '../../components/common/AutoFitDataGrid';

import { LeaveActionConfirmDialog } from '../../components/leave/LeaveActionConfirmDialog';

import { LeaveRowActions } from '../../components/leave/LeaveRowActions';

import {

  createLeaveRequestColumnsWithDetailPopover,

  formatLeaveDateRange,

  isLeaveCurrentlyRunning,

  leaveBalanceLiabilityColumns,

  leaveTableColumnSizing,

  sortLeaveHistoryItems,

} from '../../components/leave/leaveTableUtils';

import { useLeaveActions } from '../../hooks/useLeaveActions';
import { ScrollableDiv } from './LeaveReportsPage';
import { LeaveSummaryCard } from '../../components/reportCards/SummaryCard';


function BalanceCard({ balance }: { balance: LeaveBalance }) {

  return (

    <Card>

      <CardHeader

        header={(

          <div className="flex items-center gap-2">

            {balance.leaveTypeColor ? (

              <span

                className="inline-block w-3 h-3 rounded-full shrink-0"

                style={{ backgroundColor: balance.leaveTypeColor }}

              />

            ) : null}

            <Title3>{balance.leaveTypeName}</Title3>

          </div>

        )}

        description={`Cycle: ${balance.cycleStart} – ${balance.cycleEnd}`}

      />

      <div className="px-4 pb-4 grid grid-cols-2 gap-3 text-sm">

        <div>

          <Text className="text-neutral-foreground-3">Remaining</Text>

          <div className="text-lg font-semibold">{balance.remaining.toFixed(1)}</div>

        </div>

        <div>

          <Text className="text-neutral-foreground-3">Allocated</Text>

          <div>{balance.allocated.toFixed(1)}</div>

        </div>

        <div>

          <Text className="text-neutral-foreground-3">Used</Text>

          <div>{balance.used.toFixed(1)}</div>

        </div>

        <div>

          <Text className="text-neutral-foreground-3">Pending</Text>

          <div>{balance.pending.toFixed(1)}</div>

        </div>

        {balance.adjusted !== 0 ? (

          <div className="col-span-2">

            <Text className="text-neutral-foreground-3">Adjusted</Text>

            <div>{balance.adjusted.toFixed(1)}</div>

          </div>

        ) : null}

      </div>

    </Card>

  );

}



function AdjustBalanceDialog({

  open,

  balance,

  onClose,

  onSaved,

}: {

  open: boolean;

  balance: LeaveBalance | null;

  onClose: () => void;

  onSaved: () => void;

}) {

  const [allocatedDelta, setAllocatedDelta] = useState('0');

  const [adjustedDelta, setAdjustedDelta] = useState('0');

  const [error, setError] = useState<string | null>(null);

  const [isSaving, setIsSaving] = useState(false);



  useEffect(() => {

    setAllocatedDelta('0');

    setAdjustedDelta('0');

    setError(null);

  }, [balance, open]);



  async function handleSubmit(event: FormEvent) {

    event.preventDefault();

    if (!balance) {

      return;

    }



    setIsSaving(true);

    setError(null);



    try {

      await adjustLeaveBalance({

        userId: balance.userId,

        leaveTypeId: balance.leaveTypeId,

        cycleStart: balance.cycleStart,

        cycleEnd: balance.cycleEnd,

        allocatedDelta: Number.parseFloat(allocatedDelta) || 0,

        adjustedDelta: Number.parseFloat(adjustedDelta) || 0,

      });

      onSaved();

      onClose();

    } catch (saveError) {

      setError(saveError instanceof ApiError ? saveError.message : 'Failed to adjust balance.');

    } finally {

      setIsSaving(false);

    }

  }



  return (

    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>

      <DialogSurface>

        <form onSubmit={(event) => void handleSubmit(event)}>

          <DialogBody>

            <DialogTitle>Adjust leave balance</DialogTitle>

            <DialogContent className="flex flex-col gap-3 pt-2">

              {error ? (

                <MessageBar intent="error">

                  <MessageBarBody>{error}</MessageBarBody>

                </MessageBar>

              ) : null}



              <Text>{balance?.leaveTypeName ?? ''}</Text>



              <Field label="Allocation change (+/- days)">

                <Input

                  type="number"

                  step="0.5"

                  value={allocatedDelta}

                  onChange={(_, data) => setAllocatedDelta(data.value)}

                />

              </Field>



              <Field label="Manual adjustment (+/- days)">

                <Input

                  type="number"

                  step="0.5"

                  value={adjustedDelta}

                  onChange={(_, data) => setAdjustedDelta(data.value)}

                />

              </Field>

            </DialogContent>

          </DialogBody>

          <DialogActions className='mt-5'>

            <Button type="button" appearance="secondary" onClick={onClose}>Cancel</Button>

            <Button type="submit" appearance="primary" disabled={isSaving}>

              {isSaving ? 'Saving...' : 'Save adjustment'}

            </Button>

          </DialogActions>

        </form>

      </DialogSurface>

    </Dialog>

  );

}



function LeaveRequestSection({

  title,

  emptyMessage,

  items,

  columns,

}: {

  title: string;

  emptyMessage: string;

  items: LeaveRequest[];

  columns: TableColumnDefinition<LeaveRequest>[];

}) {

  return (

    <div className="flex flex-col gap-3">

      <Subtitle2>{title}</Subtitle2>

      {items.length === 0 ? (

        <Text className="text-sm text-neutral-foreground-3">{emptyMessage}</Text>

      ) : (
          <ScrollableDiv>
                  <AutoFitDataGrid
                    items={items}
                    columns={columns}
                    getRowId={(item) => item.id}
                    columnSizingOptions={leaveTableColumnSizing}
                    size="small"
                  />
          </ScrollableDiv>

      )}

    </div>

  );

}

function SummaryCard({ label, value, description, leaveTypeColor, onSelectTarget, canAdjust }: { label: string; value: string | number; description?: string; leaveTypeColor?: string | undefined; onSelectTarget?: () => void; canAdjust?: boolean }) {
  return (
   
    <Card className={"h-full"}>
          <CardHeader
            header={<Text weight="semibold">
              {leaveTypeColor ? (
              <span
                className="inline-block w-3 h-3 rounded-full shrink-0 mr-2"
                style={{ backgroundColor: leaveTypeColor }}
              />

            ) : null}
              
              {label}</Text>}
            description={<Body1 className={"font-thin!"}>{description}</Body1>}
            action={<>{ canAdjust && <Button size='small' appearance="subtle" onClick={onSelectTarget}>Adjust</Button> }</>}
          />
          <p className={"text-4xl text-left font-thin"}>
            {value}
          </p>
    </Card>
  );
}



function matchesBalanceFilter(balance: LeaveBalance, filter: string): boolean {
  switch (filter) {
    case 'Has remaining':
      return balance.remaining > 0;
    case 'Zero remaining':
      return balance.remaining <= 0;
    case 'Has pending':
      return balance.pending > 0;
    default:
      return true;
  }
}

function requestMatchesYear(request: LeaveRequest, year: number): boolean {
  const start = new Date(request.startDate);
  const end = new Date(request.endDate);
  const yearStart = new Date(year, 0, 1);
  const yearEnd = new Date(year, 11, 31, 23, 59, 59, 999);
  return start <= yearEnd && end >= yearStart;
}



export default function LeaveBalancesPage() {

  const searchQuery = usePageSearchQuery();
  const { user, hasPermission } = usePermissions();
  const leavePermissions = useMemo(() => ({
    canWriteRequests: hasPermission('leave.requests.write'),
    canWriteApprovals: hasPermission('leave.approvals.write'),
  }), [hasPermission]);

  const canAdjust = hasPermission('leave.balances.write');
  const showLeaveActions = leavePermissions.canWriteRequests || leavePermissions.canWriteApprovals;
  const currentYear = new Date().getFullYear();

  const [balances, setBalances] = useState<LeaveBalance[]>([]);
  const [requests, setRequests] = useState<LeaveRequest[]>([]);
  const [year, setYear] = useState(currentYear);
  const [balanceFilter, setBalanceFilter] = useState<string>('All');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [adjustTarget, setAdjustTarget] = useState<LeaveBalance | null>(null);
  const [selectedCard, setSelectedCard] = useState<LeaveBalance | null>(null);


  const loadPageData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [balanceItems, requestItems] = await Promise.all([
        getMyLeaveBalances(year),
        getMyLeaveRequests('All'),
      ]);

      setBalances(balanceItems);
      setRequests(requestItems);
      setSelectedCard(balanceItems.length > 0 ? balanceItems[0] : null);

    } catch (loadError) {

      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load leave balances.');
      setBalances([]);
      setRequests([]);
      setSelectedCard(null);

    } finally {
      setIsLoading(false);
    }

  }, [year]);



  useEffect(() => {
    void loadPageData();
  }, [loadPageData]);

  const balanceFilteredBalances = useMemo(
    () => balances.filter((balance) => matchesBalanceFilter(balance, balanceFilter)),
    [balanceFilter, balances],
  );

  useEffect(() => {
    setSelectedCard((current) => {
      if (current && balanceFilteredBalances.some((balance) => balance.id === current.id)) {
        return current;
      }

      return balanceFilteredBalances[0] ?? null;
    });
  }, [balanceFilteredBalances, year, balanceFilter]);



  const filteredBalances = useMemo(
    () => filterLeaveBalances(balanceFilteredBalances, searchQuery),
    [balanceFilteredBalances, searchQuery],
  );



  const yearFilteredRequests = useMemo(
    () => requests.filter((request) => requestMatchesYear(request, year)),
    [requests, year],
  );

  const filteredRequests = useMemo(
    () => filterLeaveRequests(yearFilteredRequests, searchQuery),
    [yearFilteredRequests, searchQuery],
  );

  const {
    actingId,
    actionError,
    pendingAction,
    requestAction,
    confirmPendingAction,
    dismissPendingAction,
    isWorking,
  } = useLeaveActions(loadPageData);

  const pendingRequests = useMemo(
    () => filteredRequests.filter((item) => item.status === 'Pending'),
    [filteredRequests],
  );

  const historyRequests = useMemo(
    () => sortLeaveHistoryItems(
      filteredRequests.filter((item) => item.status !== 'Pending'),
    ),
    [filteredRequests],
  );

  const runningLeave = useMemo(
    () => historyRequests.filter((item) => isLeaveCurrentlyRunning(
      item.status,
      item.startDate,
      item.endDate,
    )),
    [historyRequests],
  );



  const requestColumns = useMemo(

    () => createLeaveRequestColumnsWithDetailPopover({

      user,

      permissions: leavePermissions,

      onAction: (kind, id) => requestAction(kind, [id]),

      renderActions: showLeaveActions

        ? (item) => (

          <LeaveRowActions

            item={item}

            user={user}

            permissions={leavePermissions}

            actingId={actingId}

            disabled={isWorking}

            onAction={(kind, id) => requestAction(kind, [id])}

          />

        )

        : undefined,

    }),

    [actingId, isWorking, leavePermissions, requestAction, showLeaveActions, user],

  );



  return (

    <div className="flex flex-col gap-6 h-full overflow-auto pb-6 px-6">
      <div className="flex items-start justify-between gap-4 px-2s">
        <AppTitle
          title="Leave Balances"
          subtitle={`Your balances, pending requests, leave history, and eligibility for ${year}.`}
        />

        <div className='flex items-end gap-2 flex-wrap'>
          <Field label="Year">
            <Dropdown
              style={{ width: 120, minWidth: 120 }}
              className="shrink-0"
              value={String(year)}
              selectedOptions={[String(year)]}
              onOptionSelect={(_, data) => {
                if (data.optionValue) {
                  setYear(Number(data.optionValue));
                }
              }}
            >
              {[currentYear, currentYear - 1, currentYear - 2].map((value) => (
                <Option key={value} value={String(value)} text={String(value)}>
                  {value}
                </Option>
              ))}
            </Dropdown>
          </Field>

          <Field label="Balance">
            <Dropdown
              style={{ width: 160, minWidth: 160 }}
              className="shrink-0"
              value={balanceFilter}
              selectedOptions={[balanceFilter]}
              onOptionSelect={(_, data) => {
                if (data.optionValue) {
                  setBalanceFilter(data.optionValue);
                }
              }}
            >
              {LEAVE_BALANCE_FILTERS.map((filter) => (
                <Option key={filter} value={filter} text={filter}>
                  {filter}
                </Option>
              ))}
            </Dropdown>
          </Field>

          <Field label="Leave type">
            <Dropdown
              style={{ width: 180, minWidth: 180 }}
              className="shrink-0"
              placeholder="Select a leave type"
              value={selectedCard?.leaveTypeName ?? 'Select a leave type'}
              selectedOptions={selectedCard ? [selectedCard.id] : []}
              onOptionSelect={(_, data) => setSelectedCard(
                balanceFilteredBalances.find((balance) => balance.id === data.optionValue) ?? null,
              )}
            >
              {balanceFilteredBalances.map((balance) => (
                <Option
                  key={balance.id}
                  value={balance.id}
                  text={balance.leaveTypeName}
                >
                  {balance.leaveTypeName}
                </Option>
              ))}
            </Dropdown>
          </Field>

            {canAdjust ? (
              <Button
                appearance="secondary"
                onClick={() => {
                  const target = selectedCard ?? balanceFilteredBalances[0];
                  if (target) {
                    setAdjustTarget(target);
                  }
                }}
                disabled={balanceFilteredBalances.length === 0}
              >
                Adjust balance
              </Button>
            ) : null}
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

        <Spinner label="Loading leave balances..." />

      ) : (

        <>

          {balances.length === 0 ? (
            <Text className="text-sm text-neutral-foreground-3">
              No balance records for {year} yet. Balances are created when you apply for leave or when HR allocates entitlement.
            </Text>
          ) : balanceFilteredBalances.length === 0 ? (
            <Text className="text-sm text-neutral-foreground-3">
              No balances match the selected balance filter.
            </Text>
          ) : filteredBalances.length === 0 ? (
            <Text className="text-sm text-neutral-foreground-3">
              No balances match your search.
            </Text>
          ) : (

            selectedCard ? (
        <div className='grid grid-cols-4 w-full gap-3 mx-auto max-w-7xl py-6'>
           <LeaveSummaryCard label="Total Used" description={`${selectedCard?.leaveTypeName ?? ''} Leave`} value={selectedCard?.used ?? 0} leaveTypeColor={selectedCard?.leaveTypeColor ?? 'default'} />
           <LeaveSummaryCard label="Allocated Balance" description={`${selectedCard?.leaveTypeName ?? ''} Leave`} value={selectedCard?.allocated ?? 0} leaveTypeColor={selectedCard?.leaveTypeColor ?? 'default'} />
           <LeaveSummaryCard canAdjust={canAdjust} onSelectTarget={() => setAdjustTarget(selectedCard)} label="Remaining Balance" description={`${selectedCard?.leaveTypeName ?? ''} Leave`} value={selectedCard?.remaining ?? 0} leaveTypeColor={selectedCard?.leaveTypeColor ?? 'default'} />
           <LeaveSummaryCard label="Pending" description={`${selectedCard?.leaveTypeName ?? ''} Leave`} value={selectedCard?.pending ?? 0} leaveTypeColor={selectedCard?.leaveTypeColor ?? 'default'} />
        </div>) :
        (
          <div className='grid grid-cols-4 w-full gap-3 mx-auto max-w-7xl py-6'>
             {[0,1,2,3].map((index) => (
               <Card key={index}>
                 <CardHeader
                   header={<Text weight="semibold">
                     <span
                       className="inline-block w-3 h-3 rounded-full shrink-0 mr-2"
                       style={{ backgroundColor: "ActiveCaption" }}
                    />
                    Loading...
                    </Text>}
                  description={<Body1 className={"font-thin!"}>Loading...</Body1>}
                />
                <p className={"text-4xl text-left font-thin"}>
                  <Spinner size="extra-tiny" label="Loading..." />
                </p>
             </Card>))}
          </div>
        )
             //Do not delete this code, it is commented out for now. It will be used in the future to display balances in a grid format.
            // <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">

            //   {filteredBalances.map((balance) => (

            //     <div key={balance.id} className="relative">

            //       <BalanceCard balance={balance} />

            //       {canAdjust ? (

            //         <Button

            //           appearance="subtle"

            //           size="small"

            //           className="absolute top-3 right-3"

            //           onClick={() => setAdjustTarget(balance)}

            //         >

            //           Adjust

            //         </Button>

            //       ) : null}

            //     </div>

            //   ))}

            // </div>

          )}


          <ScrollableDiv>
              <LeaveRequestSection
                title="Pending approvals"
                emptyMessage="You have no leave requests awaiting approval."
                items={pendingRequests}
                columns={requestColumns}
              />
          </ScrollableDiv>

          {runningLeave.length > 0 ? (
            <MessageBar intent="success">
              <MessageBarBody>
                {runningLeave.length === 1
                  ? `Currently on leave: ${runningLeave[0].leaveType} (${formatLeaveDateRange(runningLeave[0])}).`
                  : `Currently on leave: ${runningLeave.length} approved requests in progress.`}
              </MessageBarBody>
            </MessageBar>
          ) : null}

          <LeaveRequestSection
            title="Leave history"
            emptyMessage="No leave history yet."
            items={historyRequests}
            columns={requestColumns}
          />

          <div className="flex flex-col gap-3">
            <Subtitle2>Balance eligibility</Subtitle2>
            {filteredBalances.length === 0 ? (
              <Text className="text-sm text-neutral-foreground-3">
                No balance eligibility records match your search for {year}.
              </Text>
            ) : (
              <AutoFitDataGrid
                items={filteredBalances}
                columns={leaveBalanceLiabilityColumns}
                getRowId={(item) => item.id}
                columnSizingOptions={leaveTableColumnSizing}
                size="small"
              />
            )}

          </div>

        </>

      )}



      {canAdjust ? (

        <AdjustBalanceDialog
          open={adjustTarget !== null}
          balance={adjustTarget}
          onClose={() => setAdjustTarget(null)}
          onSaved={() => void loadPageData()}
        />

      ) : null}



      <LeaveActionConfirmDialog
        pending={pendingAction}
        isWorking={isWorking}
        onConfirm={(reason) => void confirmPendingAction(reason)}
        onCancel={dismissPendingAction}
      />

    </div>

  );

}

