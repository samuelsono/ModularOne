import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Dropdown,
  Field,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Title3,
  createTableColumn,
  tokens,
  type JSXElement,
  type TableColumnDefinition,
} from '@fluentui/react-components';
import AppTitle from '@platform/ui/AppTitle';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import { ExpenseCategoryName } from '@modules/expense/components/expenseBadges';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { ApiError } from '@platform/api/apiClient';
import { getExpenseBalances } from '@modules/expense/services/expenseService';
import type { ExpenseCategoryBalance } from '@modules/expense/types/expense';
import { matchesSearchQuery } from '@platform/search/searchText';
import SummaryCard from '@modules/reporting/components/reportCards/SummaryCard';
import type { VerticalBarChartDataPoint } from "@fluentui/react-charts";
import { VerticalBarChart } from "@fluentui/react-charts";
import { REPORT_CHART_COLORS } from '@modules/reporting/utils/chartColors';
import DataReload from '../components/DataReload';

const formatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: 'ZAR',
  currencyDisplay: "symbol",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function filterBalances(items: ExpenseCategoryBalance[], query: string): ExpenseCategoryBalance[] {
  const normalized = query.trim();
  if (!normalized) {
    return items;
  }

  return items.filter((item) => matchesSearchQuery(normalized, [
    item.categoryName,
    item.categoryCode,
    item.pendingAmount,
    item.pendingPaymentAmount,
    item.paidAmount,
    item.draftAmount,
    item.rejectedAmount,
    item.cancelledAmount,
    item.totalSubmitted,
  ]));
}

export default function ExpenseBalancesPage() {
  const searchQuery = usePageSearchQuery();
  const currentYear = new Date().getFullYear();
  const [year, setYear] = useState(currentYear);
  const [balances, setBalances] = useState<ExpenseCategoryBalance[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadBalances = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getExpenseBalances(year);
      setBalances(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load expense balances.');
      setBalances([]);
    } finally {
      setIsLoading(false);
    }
  }, [year]);

  useEffect(() => {
    void loadBalances();
  }, [loadBalances]);

  const filteredBalances = useMemo(
    () => filterBalances(balances, searchQuery),
    [balances, searchQuery],
  );

  const columns = useMemo<TableColumnDefinition<ExpenseCategoryBalance>[]>(() => [
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'categoryName',
      renderHeaderCell: () => 'Category',
      renderCell: (item) => <ExpenseCategoryName name={item.categoryName} code={item.categoryCode} />,
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'pendingAmount',
      renderHeaderCell: () => 'In approval',
      renderCell: (item) => formatter.format(item.pendingAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'pendingPaymentAmount',
      renderHeaderCell: () => 'Pending payment',
      renderCell: (item) => formatter.format(item.pendingPaymentAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'paidAmount',
      renderHeaderCell: () => 'Paid',
      renderCell: (item) => formatter.format(item.paidAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'draftAmount',
      renderHeaderCell: () => 'Draft',
      renderCell: (item) => formatter.format(item.draftAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'rejectedAmount',
      renderHeaderCell: () => 'Rejected',
      renderCell: (item) => formatter.format(item.rejectedAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'cancelledAmount',
      renderHeaderCell: () => 'Cancelled',
      renderCell: (item) => formatter.format(item.cancelledAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'totalSubmitted',
      renderHeaderCell: () => 'Submitted total',
      renderCell: (item) => formatter.format(item.totalSubmitted),
    }),
  ], []);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0 py-1">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Expense balances" subtitle="See how much you have submitted in each claim category during the selected year." />
       <div className="flex items-end gap-2">
        <DataReload onReload={loadBalances} />
        <Field label="Year">
          <Dropdown 
               positioning={"below-end"}
               value={String(year)} 
               onOptionSelect={(_, event) => setYear(Number(event.optionValue))}
               selectedOptions={year ? [String(year)] : []}
               style={{ minWidth: 92, width: 92 }}>
              {[currentYear, currentYear - 1, currentYear - 2].map((value) => (
               <Option key={value} value={String(value)} text={String(value)}>{value}</Option>
            ))}
          </Dropdown>
        </Field>
        </div>
      </div>

      {error ? (
        <MessageBar intent="error" className="mx-3">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className="flex-1 min-h-0 overflow-auto">
        {isLoading ? (<div className="flex items-center justify-center h-[70vh]">
          <Spinner label="Loading expense balances..." />
          </div>) : 
         (
          //Do not delete the below code, it is commented out for now but will be used in future
          // <AutoFitDataGrid
          //       items={filteredBalances}
          //       columns={columns}
          //       getRowId={(item) => item.categoryId}
          //       size="small"
          //     />
          <></>
        )}
         {!isLoading &&  <div className="flex gap-3 grid grid-cols-3 p-5 gap-4">
            { filteredBalances.map((rowData) => 
                <SummaryVerticalBar key={rowData.categoryId} rowData={rowData} />
            )}
         </div> }
      </div>
    </div>
  );
}


export const SummaryVerticalBar = ({ rowData }:{ rowData: ExpenseCategoryBalance }): JSXElement => {
  const data: VerticalBarChartDataPoint[] = [
    { x: 'In Approval', y: rowData.pendingAmount, color: REPORT_CHART_COLORS[0], legend: 'In Approval' },
    { x: 'Payment', y: rowData.pendingPaymentAmount, color: REPORT_CHART_COLORS[1], legend: 'Pending Payment' },
    { x: 'Paid', y: rowData.paidAmount, color: REPORT_CHART_COLORS[2], legend: 'Paid' },
    { x: 'Draft', y: rowData.draftAmount, color: REPORT_CHART_COLORS[3], legend: 'Draft' },
    { x: 'Rejected', y: rowData.rejectedAmount, color: REPORT_CHART_COLORS[4], legend: 'Rejected' },
    { x: 'Cancelled', y: rowData.cancelledAmount, color: REPORT_CHART_COLORS[5], legend: 'Cancelled' },
  ];

  const isAllZero = data.length > 0 && data.every(point => point.y === 0);
  const processedData = React.useMemo(() => {
    if (isAllZero && data.length > 0) {
      // Deep copy data and inject a microscopic value into the first point to force axis rendering
      const modifiedData = [...data];
      modifiedData[0] = { ...modifiedData[0], y: 0.01 };
      return modifiedData;
    }
    return data;
  }, [data, isAllZero]);

  return (<div className='shadow rounded px-3 border pt-2' style={{ borderColor: tokens.colorNeutralStroke3 }} >
    <div className='font-bold mb-3 text-center'>{rowData.categoryName}</div>
    <div className='h-[260px] p-3'>
        <VerticalBarChart   
                yAxisTitle={`${rowData.categoryName} (R)`}
                xAxisTitle="Different Status amounts"
                data={processedData} 
                xAxisInnerPadding={0.1}
                yMaxValue={100}
                useSingleColor={false}
                barWidth={"auto"}
                hideLegend
          />
    </div>
  </div>)
}